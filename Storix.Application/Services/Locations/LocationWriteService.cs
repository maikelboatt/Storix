using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Locations;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Stores.Locations;
using Storix.Domain.Models;

namespace Storix.Application.Services.Locations
{
    /// <summary>
    ///     Service responsible for location write operations with ISoftDeletable support.
    /// </summary>
    public class LocationWriteService(
        ILocationRepository locationRepository,
        ILocationStore locationStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILocationValidationService locationValidationService,
        IValidator<CreateLocationDto> createValidator,
        IValidator<UpdateLocationDto> updateValidator,
        ILogger<LocationWriteService> logger ):ILocationWriteService
    {
        public async Task<DatabaseResult<LocationDto>> CreateLocationAsync( CreateLocationDto createLocationDto )
        {
            // Input validation
            DatabaseResult<LocationDto> inputValidation = ValidateCreateInput(createLocationDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<LocationDto> businessValidation = await ValidateCreateBusiness(createLocationDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create location
            return await PerformCreate(createLocationDto);
        }

        public async Task<DatabaseResult<LocationDto>> UpdateLocationAsync( UpdateLocationDto updateLocationDto )
        {
            // Input validation
            DatabaseResult<LocationDto> inputValidation = ValidateUpdateInput(updateLocationDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<LocationDto> businessValidation = await ValidateUpdateBusiness(updateLocationDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform update
            return await PerformUpdate(updateLocationDto);
        }

        public async Task<DatabaseResult> SoftDeleteLocationAsync( int locationId )
        {
            // Input validation
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided for soft deletion", locationId);
                return DatabaseResult.Failure("Location ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await locationValidationService.ValidateForDeletion(locationId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform soft deletion
            return await PerformSoftDelete(locationId);
        }

        public async Task<DatabaseResult> RestoreLocationAsync( int locationId )
        {
            // Input validation
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided for restoration", locationId);
                return DatabaseResult.Failure("Location ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await locationValidationService.ValidateForRestore(locationId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform restoration
            return await PerformRestore(locationId);
        }

        public async Task<DatabaseResult> HardDeleteLocationAsync( int locationId )
        {
            // Input validation
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided for hard deletion", locationId);
                return DatabaseResult.Failure("Location ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await locationValidationService.ValidateForHardDeletion(locationId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform hard deletion
            return await PerformHardDelete(locationId);
        }

        #region Helper Methods

        private async Task<DatabaseResult<LocationDto>> PerformCreate( CreateLocationDto createLocationDto )
        {
            // Convert DTO to domain model - always creates non-deleted locations
            Location location = createLocationDto.ToDomain();

            DatabaseResult<Location> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.CreateAsync(location),
                "Creating new location"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                int createdLocationId = result.Value.LocationId;

                // Add to in-memory store
                LocationDto? storeResult = locationStore.Create(createdLocationId, createLocationDto);

                if (storeResult == null)
                {
                    logger.LogWarning(
                        "Location created in database (ID: {LocationId}) but failed to add to cache",
                        createdLocationId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully created location with ID {LocationId} and name '{LocationName}'",
                        createdLocationId,
                        result.Value.Name);
                }

                return DatabaseResult<LocationDto>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to create location: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<LocationDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<LocationDto>> PerformUpdate( UpdateLocationDto updateLocationDto )
        {
            // Get existing location (only active ones - can't update deleted locations)
            DatabaseResult<Location?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetByIdAsync(updateLocationDto.LocationId, false),
                $"Retrieving location {updateLocationDto.LocationId} for update",
                enableRetry: false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning(
                    "Cannot update location {LocationId}: {ErrorMessage}",
                    updateLocationDto.LocationId,
                    getResult.ErrorMessage ?? "Location not found or is deleted");
                return DatabaseResult<LocationDto>.Failure(
                    getResult.ErrorMessage ?? "Location not found or is deleted. Restore the location first if it was deleted.",
                    getResult.ErrorCode);
            }

            Location existingLocation = getResult.Value;

            // Update location while preserving ISoftDeletable properties
            Location updatedLocation = existingLocation with
            {
                Name = updateLocationDto.Name,
                Description = updateLocationDto.Description,
                Type = updateLocationDto.Type,
                Address = updateLocationDto.Address
                // IsDeleted and DeletedAt are preserved from existingLocation
            };

            DatabaseResult<Location> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.UpdateAsync(updatedLocation),
                "Updating location"
            );

            if (updateResult is { IsSuccess: true, Value: not null })
            {
                // Update in-memory store
                LocationDto? storeResult = locationStore.Update(updateLocationDto);

                if (storeResult == null)
                {
                    logger.LogWarning(
                        "Location updated in database (ID: {LocationId}) but failed to update in cache",
                        updateLocationDto.LocationId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully updated location with ID {LocationId}",
                        updateLocationDto.LocationId);
                }

                return DatabaseResult<LocationDto>.Success(updateResult.Value.ToDto());
            }

            logger.LogWarning(
                "Failed to update location with ID {LocationId}: {ErrorMessage}",
                updateLocationDto.LocationId,
                updateResult.ErrorMessage);
            return DatabaseResult<LocationDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformSoftDelete( int locationId )
        {
            DatabaseResult result = await locationRepository.SoftDeleteAsync(locationId);

            if (result.IsSuccess)
            {
                bool storeResult = locationStore.Delete(locationId);

                if (!storeResult)
                {
                    logger.LogWarning(
                        "Location soft deleted in database (ID: {LocationId}) but failed to update cache",
                        locationId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully soft deleted location with ID {LocationId}",
                        locationId);
                }

                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to soft delete location with ID {LocationId}: {ErrorMessage}",
                locationId,
                result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to soft delete location",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int locationId )
        {
            DatabaseResult result = await locationRepository.RestoreAsync(locationId);

            if (result.IsSuccess)
            {
                // Fetch the restored location from database
                DatabaseResult<Location?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => locationRepository.GetByIdAsync(locationId),
                    $"Retrieving restored location {locationId}",
                    enableRetry: false
                );

                if (getResult is { IsSuccess: true, Value: not null })
                {
                    // Add back to active cache
                    CreateLocationDto createDto = new()
                    {
                        Name = getResult.Value.Name,
                        Description = getResult.Value.Description,
                        Type = getResult.Value.Type,
                        Address = getResult.Value.Address
                    };

                    LocationDto? cached = locationStore.Create(getResult.Value.LocationId, createDto);

                    if (cached != null)
                    {
                        logger.LogInformation(
                            "Successfully restored location with ID {LocationId} and added back to cache",
                            locationId);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Location restored in database (ID: {LocationId}) but failed to add to cache",
                            locationId);
                    }
                }

                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to restore location with ID {LocationId}: {ErrorMessage}",
                locationId,
                result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to restore location",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int locationId )
        {
            DatabaseResult result = await locationRepository.HardDeleteAsync(locationId);

            if (result.IsSuccess)
            {
                // Remove from store completely (checks both active and deleted collections)
                bool storeResult = locationStore.Delete(locationId);

                if (!storeResult)
                {
                    logger.LogWarning(
                        "Location hard deleted in database (ID: {LocationId}) but wasn't found in cache",
                        locationId);
                }

                logger.LogWarning(
                    "Successfully hard deleted location with ID {LocationId} - THIS IS PERMANENT",
                    locationId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to hard delete location with ID {LocationId}: {ErrorMessage}",
                locationId,
                result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to hard delete location",
                result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<LocationDto> ValidateCreateInput( CreateLocationDto? createLocationDto )
        {
            if (createLocationDto == null)
            {
                logger.LogWarning("Null CreateLocationDto provided");
                return DatabaseResult<LocationDto>.Failure(
                    "Location data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = createValidator.Validate(createLocationDto);

            if (validationResult.IsValid)
                return DatabaseResult<LocationDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Location creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<LocationDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<LocationDto>> ValidateCreateBusiness( CreateLocationDto createLocationDto )
        {
            // Check name availability (excluding soft-deleted locations)
            DatabaseResult<bool> nameExistsResult = await locationValidationService.LocationNameExistsAsync(
                createLocationDto.Name,
                includeDeleted: false);

            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<LocationDto>.Failure(
                    nameExistsResult.ErrorMessage!,
                    nameExistsResult.ErrorCode);

            if (nameExistsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to create location with duplicate name: {LocationName}",
                    createLocationDto.Name);
                return DatabaseResult<LocationDto>.Failure(
                    $"A location with the name '{createLocationDto.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<LocationDto>.Success(null!);
        }

        private DatabaseResult<LocationDto> ValidateUpdateInput( UpdateLocationDto? updateLocationDto )
        {
            if (updateLocationDto == null)
            {
                logger.LogWarning("Null UpdateLocationDto provided");
                return DatabaseResult<LocationDto>.Failure(
                    "Location data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = updateValidator.Validate(updateLocationDto);

            if (validationResult.IsValid)
                return DatabaseResult<LocationDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "Location update validation failed for ID {LocationId}: {ValidationErrors}",
                updateLocationDto.LocationId,
                errors);
            return DatabaseResult<LocationDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<LocationDto>> ValidateUpdateBusiness( UpdateLocationDto updateLocationDto )
        {
            // Check existence (only active locations can be updated)
            DatabaseResult<bool> existsResult = await locationValidationService.LocationExistsAsync(
                updateLocationDto.LocationId,
                false);

            if (!existsResult.IsSuccess)
                return DatabaseResult<LocationDto>.Failure(
                    existsResult.ErrorMessage!,
                    existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update non-existent or deleted location with ID {LocationId}",
                    updateLocationDto.LocationId);
                return DatabaseResult<LocationDto>.Failure(
                    $"Location with ID {updateLocationDto.LocationId} not found or is deleted. " +
                    "Restore the location first if it was deleted.",
                    DatabaseErrorCode.NotFound);
            }

            // Check name availability (excluding this location and soft-deleted locations)
            DatabaseResult<bool> nameExistsResult = await locationValidationService.LocationNameExistsAsync(
                updateLocationDto.Name,
                updateLocationDto.LocationId,
                false);

            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<LocationDto>.Failure(
                    nameExistsResult.ErrorMessage!,
                    nameExistsResult.ErrorCode);

            if (nameExistsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update location {LocationId} with duplicate name: {LocationName}",
                    updateLocationDto.LocationId,
                    updateLocationDto.Name);
                return DatabaseResult<LocationDto>.Failure(
                    $"A location with the name '{updateLocationDto.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<LocationDto>.Success(null!);
        }

        #endregion
    }
}
