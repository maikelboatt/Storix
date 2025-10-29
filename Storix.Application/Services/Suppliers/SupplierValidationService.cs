using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Domain.Models;

namespace Storix.Application.Services.Suppliers
{
    /// <summary>
    /// Service responsible for supplier validation with ISoftDeletable support.
    /// Handles all validation logic including existence checks, business rules, and constraints.
    /// </summary>
    public class SupplierValidationService(
        ISupplierRepository supplierRepository,
        IOrderRepository orderRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<SupplierValidationService> logger ):ISupplierValidationService
    {
        #region Existence Validation

        public async Task<DatabaseResult<bool>> SupplierExistsAsync( int supplierId, bool includeDeleted = false )
        {
            if (supplierId <= 0)
            {
                logger.LogDebug("Invalid supplier ID {SupplierId} provided for existence check", supplierId);
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.ExistsAsync(supplierId, includeDeleted),
                $"Checking if supplier {supplierId} exists (includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (result.IsSuccess)
                logger.LogDebug(
                    "Supplier {SupplierId} exists: {Exists} (includeDeleted: {IncludeDeleted})",
                    supplierId,
                    result.Value,
                    includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> EmailExistsAsync( string email, int? excludedId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogDebug("Empty email provided for existence check");
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.ExistsByEmailAsync(email, excludedId, includeDeleted),
                $"Checking if email '{email}' exists (excludedId: {excludedId}, includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (!result.IsSuccess)
                return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);

            // If email exists, check if it belongs to the excluded supplier
            if (result.Value && excludedId.HasValue)
            {
                DatabaseResult<Supplier?> supplierResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => supplierRepository.GetByEmailAsync(email),
                    $"Retrieving supplier by email '{email}' for exclusion check",
                    enableRetry: false);

                if (!supplierResult.IsSuccess)
                    return DatabaseResult<bool>.Failure(supplierResult.ErrorMessage!, supplierResult.ErrorCode);

                // If the email belongs to the excluded supplier, return false (no conflict)
                if (supplierResult.Value?.SupplierId == excludedId.Value)
                {
                    logger.LogDebug(
                        "Email '{Email}' belongs to excluded supplier {ExcludedId}, no conflict",
                        email,
                        excludedId.Value);
                    return DatabaseResult<bool>.Success(false);
                }
            }

            if (result.IsSuccess)
                logger.LogDebug(
                    "Email '{Email}' exists: {Exists} (excludedId: {ExcludedId}, includeDeleted: {IncludeDeleted})",
                    email,
                    result.Value,
                    excludedId,
                    includeDeleted);

            return DatabaseResult<bool>.Success(result.Value);
        }

        public async Task<DatabaseResult<bool>> PhoneExistsAsync( string phone, int? excludedId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                logger.LogDebug("Empty phone provided for existence check");
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.ExistsByPhoneAsync(phone, excludedId, includeDeleted),
                $"Checking if phone '{phone}' exists (excludedId: {excludedId}, includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (!result.IsSuccess)
                return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);

            // If phone exists, check if it belongs to the excluded supplier
            if (result.Value && excludedId.HasValue)
            {
                DatabaseResult<Supplier?> supplierResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => supplierRepository.GetByPhoneAsync(phone),
                    $"Retrieving supplier by phone '{phone}' for exclusion check",
                    enableRetry: false);

                if (!supplierResult.IsSuccess)
                    return DatabaseResult<bool>.Failure(supplierResult.ErrorMessage!, supplierResult.ErrorCode);

                // If the phone belongs to the excluded supplier, return false (no conflict)
                if (supplierResult.Value?.SupplierId == excludedId.Value)
                {
                    logger.LogDebug(
                        "Phone '{Phone}' belongs to excluded supplier {ExcludedId}, no conflict",
                        phone,
                        excludedId.Value);
                    return DatabaseResult<bool>.Success(false);
                }
            }

            if (result.IsSuccess)
                logger.LogDebug(
                    "Phone '{Phone}' exists: {Exists} (excludedId: {ExcludedId}, includeDeleted: {IncludeDeleted})",
                    phone,
                    result.Value,
                    excludedId,
                    includeDeleted);

            return DatabaseResult<bool>.Success(result.Value);
        }

        public async Task<DatabaseResult<bool>> IsSupplierSoftDeleted( int supplierId )
        {
            if (supplierId <= 0)
            {
                logger.LogDebug("Invalid supplier ID {SupplierId} provided for soft-delete check", supplierId);
                return DatabaseResult<bool>.Success(false);
            }

            // Check if supplier exists at all (including deleted)
            DatabaseResult<bool> existsIncludingDeleted = await SupplierExistsAsync(supplierId, true);
            if (!existsIncludingDeleted.IsSuccess)
                return DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);

            if (!existsIncludingDeleted.Value)
            {
                logger.LogDebug("Supplier {SupplierId} does not exist in database", supplierId);
                return DatabaseResult<bool>.Success(false);
            }

            // Check if supplier exists in active records
            DatabaseResult<bool> existsActiveOnly = await SupplierExistsAsync(supplierId, false);
            if (!existsActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existsActiveOnly.ErrorMessage!, existsActiveOnly.ErrorCode);

            // Supplier exists in database but not in active results = soft deleted
            bool isSoftDeleted = !existsActiveOnly.Value;
            logger.LogDebug("Supplier {SupplierId} soft-delete status: {IsSoftDeleted}", supplierId, isSoftDeleted);

            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #endregion

        #region Deletion Validation

        public async Task<DatabaseResult> ValidateForDeletion( int supplierId )
        {
            logger.LogInformation("Validating soft deletion for supplier {SupplierId}", supplierId);

            // Check existence - only active suppliers can be soft-deleted
            DatabaseResult existsResult = await ValidateExistence(supplierId, false);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check business rules for soft deletion
            DatabaseResult businessRulesResult = await ValidateSupplierDeletionBusinessRules(supplierId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Soft deletion validation passed for supplier {SupplierId}", supplierId);
            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int supplierId )
        {
            logger.LogWarning("Validating hard deletion for supplier {SupplierId} - THIS WILL BE PERMANENT", supplierId);

            // Check existence - including soft-deleted suppliers for hard deletion
            DatabaseResult existsResult = await ValidateExistence(supplierId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check business rules for hard deletion
            DatabaseResult businessRulesResult = await ValidateSupplierDeletionBusinessRules(supplierId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Hard deletion validation passed for supplier {SupplierId}", supplierId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Restoration Validation

        public async Task<DatabaseResult> ValidateForRestore( int supplierId )
        {
            logger.LogInformation("Validating restoration for supplier {SupplierId}", supplierId);

            // Check if supplier exists (including deleted)
            DatabaseResult existsResult = await ValidateExistence(supplierId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check if supplier is actually soft-deleted
            DatabaseResult<bool> isSoftDeletedResult = await IsSupplierSoftDeleted(supplierId);
            if (!isSoftDeletedResult.IsSuccess)
                return DatabaseResult.Failure(isSoftDeletedResult.ErrorMessage!, isSoftDeletedResult.ErrorCode);

            if (!isSoftDeletedResult.Value)
            {
                logger.LogWarning("Attempted to restore active supplier {SupplierId}", supplierId);
                return DatabaseResult.Failure(
                    $"Supplier with ID {supplierId} is not deleted and cannot be restored.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Check business rules for restoration
            DatabaseResult businessValidation = await ValidateSupplierRestorationBusinessRules(supplierId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            logger.LogInformation("Restoration validation passed for supplier {SupplierId}", supplierId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Private Helper Methods

        private async Task<DatabaseResult> ValidateExistence( int supplierId, bool includeDeleted )
        {
            DatabaseResult<bool> existResult = await SupplierExistsAsync(supplierId, includeDeleted);

            if (!existResult.IsSuccess)
            {
                logger.LogError("Failed to check existence for supplier {SupplierId}: {ErrorMessage}", supplierId, existResult.ErrorMessage);
                return DatabaseResult.Failure(existResult.ErrorMessage!, existResult.ErrorCode);
            }

            if (!existResult.Value)
            {
                string statusMessage = includeDeleted
                    ? ""
                    : " or is deleted";
                logger.LogWarning("Supplier {SupplierId} not found{StatusMessage}", supplierId, statusMessage);
                return DatabaseResult.Failure(
                    $"Supplier with ID {supplierId} not found{statusMessage}.",
                    DatabaseErrorCode.NotFound);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateSupplierDeletionBusinessRules( int supplierId )
        {
            // Check if supplier has any active orders
            DatabaseResult<bool> hasActiveOrdersResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.SupplierHasOrdersAsync(supplierId, true),
                $"Checking if supplier {supplierId} has active orders",
                enableRetry: false);

            if (!hasActiveOrdersResult.IsSuccess)
            {
                logger.LogError("Failed to check active orders for supplier {SupplierId}: {ErrorMessage}", supplierId, hasActiveOrdersResult.ErrorMessage);
                return DatabaseResult.Failure(hasActiveOrdersResult.ErrorMessage!, hasActiveOrdersResult.ErrorCode);
            }

            if (hasActiveOrdersResult.Value)
            {
                logger.LogWarning("Cannot delete supplier {SupplierId} - has active orders", supplierId);
                return DatabaseResult.Failure(
                    $"Cannot delete supplier with ID {supplierId} because they have active orders. Complete or cancel orders first.",
                    DatabaseErrorCode.ConstraintViolation);
            }

            logger.LogDebug("Supplier {SupplierId} has no active orders, deletion allowed", supplierId);
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateSupplierRestorationBusinessRules( int supplierId )
        {
            // Get supplier details
            DatabaseResult<Supplier?> supplierResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.GetByIdAsync(supplierId),
                $"Retrieving supplier {supplierId} for restoration validation",
                enableRetry: false);

            if (!supplierResult.IsSuccess)
            {
                logger.LogError("Failed to retrieve supplier {SupplierId} for restoration validation: {ErrorMessage}", supplierId, supplierResult.ErrorMessage);
                return DatabaseResult.Failure(supplierResult.ErrorMessage!, supplierResult.ErrorCode);
            }

            if (supplierResult.Value == null)
            {
                logger.LogWarning("Supplier {SupplierId} not found during restoration validation", supplierId);
                return DatabaseResult.Failure(
                    $"Supplier with ID {supplierId} not found for restoration validation.",
                    DatabaseErrorCode.NotFound);
            }

            // Check for email conflicts with active suppliers
            DatabaseResult emailCheck = await CheckEmailConflict(supplierId, supplierResult.Value);
            if (!emailCheck.IsSuccess)
                return emailCheck;

            // Check for phone conflicts with active suppliers
            DatabaseResult phoneCheck = await CheckPhoneConflict(supplierId, supplierResult.Value);
            if (!phoneCheck.IsSuccess)
                return phoneCheck;

            logger.LogInformation("Supplier {SupplierId} passed all restoration business rule validations", supplierId);
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckEmailConflict( int supplierId, Supplier supplier )
        {
            DatabaseResult<bool> emailConflictResult = await EmailExistsAsync(
                supplier.Email,
                supplierId,
                false);

            if (!emailConflictResult.IsSuccess)
            {
                logger.LogError("Failed to check email conflict for supplier {SupplierId}: {ErrorMessage}", supplierId, emailConflictResult.ErrorMessage);
                return DatabaseResult.Failure(emailConflictResult.ErrorMessage!, emailConflictResult.ErrorCode);
            }

            if (emailConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore supplier {SupplierId} - email '{Email}' conflicts with existing active supplier",
                    supplierId,
                    supplier.Email);
                return DatabaseResult.Failure(
                    $"Cannot restore supplier: Another active supplier with email '{supplier.Email}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckPhoneConflict( int supplierId, Supplier supplier )
        {
            DatabaseResult<bool> phoneConflictResult = await PhoneExistsAsync(
                supplier.Phone,
                supplierId,
                false);

            if (!phoneConflictResult.IsSuccess)
            {
                logger.LogError("Failed to check phone conflict for supplier {SupplierId}: {ErrorMessage}", supplierId, phoneConflictResult.ErrorMessage);
                return DatabaseResult.Failure(phoneConflictResult.ErrorMessage!, phoneConflictResult.ErrorCode);
            }

            if (phoneConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore supplier {SupplierId} - phone '{Phone}' conflicts with existing active supplier",
                    supplierId,
                    supplier.Phone);
                return DatabaseResult.Failure(
                    $"Cannot restore supplier: Another active supplier with phone '{supplier.Phone}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        #endregion
    }
}
