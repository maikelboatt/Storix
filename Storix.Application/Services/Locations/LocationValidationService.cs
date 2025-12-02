using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Stores.Locations;
using Storix.Domain.Models;

namespace Storix.Application.Services.Locations
{
    /// <summary>
    ///     Service responsible for location validation operations with ISoftDeletable support.
    /// </summary>
    public class LocationValidationService(
        ILocationRepository locationRepository,
        ILocationStore locationStore,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<LocationValidationService> logger ):ILocationValidationService
    {
        public async Task<DatabaseResult<bool>> LocationExistsAsync( int locationId, bool includeDeleted = false )
        {
            if (locationId <= 0)
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.ExistsAsync(locationId, includeDeleted),
                $"Checking if location {locationId} exists in database (includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogDebug(
                    "Location {LocationId} exists: {Exists} (includeDeleted: {IncludeDeleted})",
                    locationId,
                    result.Value,
                    includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> LocationNameExistsAsync( string name, int? excludeLocationId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(name))
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.ExistsByNameAsync(name, excludeLocationId, includeDeleted),
                $"Checking if location name '{name}' exists (excludeLocationId: {excludeLocationId}, includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForDeletion( int locationId )
        {
            // Check existence (only active locations can be deleted)
            DatabaseResult existenceResult = await ProofOfExistence(locationId, false);
            if (!existenceResult.IsSuccess)
                return existenceResult;

            // Check business rules for soft deletion
            DatabaseResult businessRulesResult = await ValidateLocationDeletionBusinessRules(locationId);
            return !businessRulesResult.IsSuccess
                ? businessRulesResult
                : DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int locationId )
        {
            // Check existence (including soft-deleted locations for hard deletion)
            DatabaseResult existenceResult = await ProofOfExistence(locationId, true);
            if (!existenceResult.IsSuccess)
                return existenceResult;

            // Check business rules for hard deletion (more restrictive)
            DatabaseResult businessRulesResult = await ValidateLocationHardDeletionBusinessRules(locationId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogWarning(
                "Hard deletion validation passed for location {LocationId} - THIS WILL BE PERMANENT",
                locationId);
            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForRestore( int locationId )
        {
            // Check if location exists and is soft deleted
            DatabaseResult existenceResult = await ProofOfExistence(locationId, true);
            if (!existenceResult.IsSuccess)
                return existenceResult;

            // Check if location is actually deleted
            DatabaseResult<bool> isSoftDeletedResult = await IsLocationSoftDeleted(locationId);
            if (!isSoftDeletedResult.IsSuccess)
                return DatabaseResult.Failure(isSoftDeletedResult.ErrorMessage!, isSoftDeletedResult.ErrorCode);

            if (!isSoftDeletedResult.Value)
            {
                logger.LogWarning("Attempted to restore active location with ID {LocationId}", locationId);
                return DatabaseResult.Failure(
                    $"Location with ID {locationId} is not deleted and cannot be restored.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Check business rules for restoration
            DatabaseResult businessValidation = await ValidateLocationRestorationBusinessRules(locationId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult<bool>> IsLocationSoftDeleted( int locationId )
        {
            if (locationId <= 0)
                return DatabaseResult<bool>.Success(false);

            // Check store first - it manages active vs deleted collections
            bool existsInStore = locationStore.Exists(locationId);

            if (existsInStore)
                return DatabaseResult<bool>.Success(false);

            // Not in store, check database to be sure
            DatabaseResult<bool> existsIncludingDeleted = await LocationExistsAsync(locationId, true);
            if (!existsIncludingDeleted.IsSuccess || !existsIncludingDeleted.Value)
            {
                return existsIncludingDeleted.IsSuccess
                    ? DatabaseResult<bool>.Success(false)
                    : DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);
            }

            DatabaseResult<bool> existsActiveOnly = await LocationExistsAsync(locationId, false);
            if (!existsActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existsActiveOnly.ErrorMessage!, existsActiveOnly.ErrorCode);

            // Location exists in database but not in active results = soft deleted
            bool isSoftDeleted = !existsActiveOnly.Value;
            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #region Private Helper Methods

        private async Task<DatabaseResult> ProofOfExistence( int locationId, bool includeDeleted = false )
        {
            // Check existence
            DatabaseResult<bool> existsResult = await LocationExistsAsync(locationId, includeDeleted);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                string statusMessage = includeDeleted
                    ? ""
                    : " or is deleted";
                logger.LogWarning(
                    "Attempted operation on non-existent location with ID {LocationId}{StatusMessage}",
                    locationId,
                    statusMessage);
                return DatabaseResult.Failure(
                    $"Location with ID {locationId} not found{statusMessage}.",
                    DatabaseErrorCode.NotFound);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateLocationDeletionBusinessRules( int locationId )
        {
            // Check for active inventory at this location
            DatabaseResult<bool> hasInventoryResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () => (await inventoryRepository.GetByLocationIdAsync(locationId)).Any(),
                $"Checking for active inventory at location {locationId}",
                enableRetry: false
            );

            if (hasInventoryResult.IsSuccess && hasInventoryResult.Value)
            {
                logger.LogWarning("Cannot delete location {LocationId} - it has active inventory", locationId);
                return DatabaseResult.Failure(
                    "Cannot delete location because it contains active inventory. Please transfer or remove the inventory first.",
                    DatabaseErrorCode.ForeignKeyViolation);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateLocationHardDeletionBusinessRules( int locationId )
        {
            // Check for ANY inventory (including historical records)
            DatabaseResult<bool> hasAnyInventoryResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () => (await inventoryRepository.GetByLocationIdAsync(locationId)).Any(),
                $"Checking for any inventory at location {locationId}",
                enableRetry: false
            );

            if (hasAnyInventoryResult.IsSuccess && hasAnyInventoryResult.Value)
            {
                logger.LogWarning(
                    "Cannot hard delete location {LocationId} - it has historical inventory records",
                    locationId);
                return DatabaseResult.Failure(
                    "Cannot permanently delete location because it has historical inventory records. This would break data integrity.",
                    DatabaseErrorCode.ForeignKeyViolation);
            }

            // Check for inventory movements (from or to this location)
            DatabaseResult<bool> hasMovementsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () => (await inventoryMovementRepository.GetByLocationIdAsync(locationId)).Any(),
                $"Checking for inventory movements at location {locationId}",
                enableRetry: false
            );

            if (hasMovementsResult is { IsSuccess: true, Value: true })
            {
                logger.LogWarning(
                    "Cannot hard delete location {LocationId} - it has historical inventory movements",
                    locationId);
                return DatabaseResult.Failure(
                    "Cannot permanently delete location because it has historical inventory movements. This would break data integrity.",
                    DatabaseErrorCode.ForeignKeyViolation);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateLocationRestorationBusinessRules( int locationId )
        {
            // Check if location name conflicts with active locations
            DatabaseResult<Location?> locationResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetByIdAsync(locationId),
                $"Retrieving location {locationId} for restore validation",
                enableRetry: false
            );

            if (!locationResult.IsSuccess || locationResult.Value == null)
                return DatabaseResult.Failure(
                    "Location not found for restoration validation.",
                    DatabaseErrorCode.NotFound);

            // Check for name conflicts with active locations
            DatabaseResult<bool> nameConflictResult = await LocationNameExistsAsync(
                locationResult.Value.Name,
                locationId,
                false);

            if (!nameConflictResult.IsSuccess)
                return DatabaseResult.Failure(nameConflictResult.ErrorMessage!, nameConflictResult.ErrorCode);

            if (nameConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore location {LocationId} - name '{Name}' conflicts with existing active location",
                    locationId,
                    locationResult.Value.Name);
                return DatabaseResult.Failure(
                    $"Cannot restore location: Another active location with name '{locationResult.Value.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            logger.LogDebug("Location {LocationId} passed all restoration business rule validations", locationId);
            return DatabaseResult.Success();
        }

        #endregion
    }
}
