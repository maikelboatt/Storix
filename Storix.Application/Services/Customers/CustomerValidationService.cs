using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers
{
    /// <summary>
    /// Service responsible for customer validation with ISoftDeletable support.
    /// Handles all validation logic including existence checks, business rules, and constraints.
    /// </summary>
    public class CustomerValidationService(
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<CustomerValidationService> logger ):ICustomerValidationService
    {
        #region Existence Validation

        public async Task<DatabaseResult<bool>> CustomerExistsAsync( int customerId, bool includeDeleted = false )
        {
            if (customerId <= 0)
            {
                logger.LogDebug("Invalid customer ID {CustomerId} provided for existence check", customerId);
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.ExistsAsync(customerId, includeDeleted),
                $"Checking if customer {customerId} exists (includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (result.IsSuccess)
                logger.LogDebug(
                    "Customer {CustomerId} exists: {Exists} (includeDeleted: {IncludeDeleted})",
                    customerId,
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
                () => customerRepository.ExistsByEmailAsync(email, excludedId, includeDeleted),
                $"Checking if email '{email}' exists (excludedId: {excludedId}, includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (!result.IsSuccess)
                return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);

            // If email exists, check if it belongs to the excluded customer
            if (result.Value && excludedId.HasValue)
            {
                DatabaseResult<Customer?> customerResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => customerRepository.GetByEmailAsync(email),
                    $"Retrieving customer by email '{email}' for exclusion check",
                    enableRetry: false);

                if (!customerResult.IsSuccess)
                    return DatabaseResult<bool>.Failure(customerResult.ErrorMessage!, customerResult.ErrorCode);

                // If the email belongs to the excluded customer, return false (no conflict)
                if (customerResult.Value?.CustomerId == excludedId.Value)
                {
                    logger.LogDebug(
                        "Email '{Email}' belongs to excluded customer {ExcludedId}, no conflict",
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
                () => customerRepository.ExistsByPhoneAsync(phone, excludedId, includeDeleted),
                $"Checking if phone '{phone}' exists (excludedId: {excludedId}, includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (!result.IsSuccess)
                return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);

            // If phone exists, check if it belongs to the excluded customer
            if (result.Value && excludedId.HasValue)
            {
                DatabaseResult<Customer?> customerResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => customerRepository.GetByPhoneAsync(phone),
                    $"Retrieving customer by phone '{phone}' for exclusion check",
                    enableRetry: false);

                if (!customerResult.IsSuccess)
                    return DatabaseResult<bool>.Failure(customerResult.ErrorMessage!, customerResult.ErrorCode);

                // If the phone belongs to the excluded customer, return false (no conflict)
                if (customerResult.Value?.CustomerId == excludedId.Value)
                {
                    logger.LogDebug(
                        "Phone '{Phone}' belongs to excluded customer {ExcludedId}, no conflict",
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

        public async Task<DatabaseResult<bool>> IsCustomerSoftDeleted( int customerId )
        {
            if (customerId <= 0)
            {
                logger.LogDebug("Invalid customer ID {CustomerId} provided for soft-delete check", customerId);
                return DatabaseResult<bool>.Success(false);
            }

            // Check if customer exists at all (including deleted)
            DatabaseResult<bool> existsIncludingDeleted = await CustomerExistsAsync(customerId, true);
            if (!existsIncludingDeleted.IsSuccess)
                return DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);

            if (!existsIncludingDeleted.Value)
            {
                logger.LogDebug("Customer {CustomerId} does not exist in database", customerId);
                return DatabaseResult<bool>.Success(false);
            }

            // Check if customer exists in active records
            DatabaseResult<bool> existsActiveOnly = await CustomerExistsAsync(customerId, false);
            if (!existsActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existsActiveOnly.ErrorMessage!, existsActiveOnly.ErrorCode);

            // Customer exists in database but not in active results = soft deleted
            bool isSoftDeleted = !existsActiveOnly.Value;
            logger.LogDebug("Customer {CustomerId} soft-delete status: {IsSoftDeleted}", customerId, isSoftDeleted);

            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #endregion

        #region Deletion Validation

        public async Task<DatabaseResult> ValidateForDeletion( int customerId )
        {
            logger.LogInformation("Validating soft deletion for customer {CustomerId}", customerId);

            // Check existence - only active customers can be soft-deleted
            DatabaseResult existsResult = await ValidateExistence(customerId, false);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check business rules for soft deletion
            DatabaseResult businessRulesResult = await ValidateCustomerDeletionBusinessRules(customerId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Soft deletion validation passed for customer {CustomerId}", customerId);
            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int customerId )
        {
            logger.LogWarning("Validating hard deletion for customer {CustomerId} - THIS WILL BE PERMANENT", customerId);

            // Check existence - including soft-deleted customers for hard deletion
            DatabaseResult existsResult = await ValidateExistence(customerId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check business rules for hard deletion
            DatabaseResult businessRulesResult = await ValidateCustomerDeletionBusinessRules(customerId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Hard deletion validation passed for customer {CustomerId}", customerId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Restoration Validation

        public async Task<DatabaseResult> ValidateForRestore( int customerId )
        {
            logger.LogInformation("Validating restoration for customer {CustomerId}", customerId);

            // Check if customer exists (including deleted)
            DatabaseResult existsResult = await ValidateExistence(customerId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check if customer is actually soft-deleted
            DatabaseResult<bool> isSoftDeletedResult = await IsCustomerSoftDeleted(customerId);
            if (!isSoftDeletedResult.IsSuccess)
                return DatabaseResult.Failure(isSoftDeletedResult.ErrorMessage!, isSoftDeletedResult.ErrorCode);

            if (!isSoftDeletedResult.Value)
            {
                logger.LogWarning("Attempted to restore active customer {CustomerId}", customerId);
                return DatabaseResult.Failure(
                    $"Customer with ID {customerId} is not deleted and cannot be restored.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Check business rules for restoration
            DatabaseResult businessValidation = await ValidateCustomerRestorationBusinessRules(customerId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            logger.LogInformation("Restoration validation passed for customer {CustomerId}", customerId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Private Helper Methods

        private async Task<DatabaseResult> ValidateExistence( int customerId, bool includeDeleted )
        {
            DatabaseResult<bool> existResult = await CustomerExistsAsync(customerId, includeDeleted);

            if (!existResult.IsSuccess)
            {
                logger.LogError("Failed to check existence for customer {CustomerId}: {ErrorMessage}", customerId, existResult.ErrorMessage);
                return DatabaseResult.Failure(existResult.ErrorMessage!, existResult.ErrorCode);
            }

            if (!existResult.Value)
            {
                string statusMessage = includeDeleted
                    ? ""
                    : " or is deleted";
                logger.LogWarning("Customer {CustomerId} not found{StatusMessage}", customerId, statusMessage);
                return DatabaseResult.Failure(
                    $"Customer with ID {customerId} not found{statusMessage}.",
                    DatabaseErrorCode.NotFound);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateCustomerDeletionBusinessRules( int customerId )
        {
            // Check if customer has any active orders
            DatabaseResult<bool> hasActiveOrdersResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CustomerHasOrdersAsync(customerId, true),
                $"Checking if customer {customerId} has active orders",
                enableRetry: false);

            if (!hasActiveOrdersResult.IsSuccess)
            {
                logger.LogError("Failed to check active orders for customer {CustomerId}: {ErrorMessage}", customerId, hasActiveOrdersResult.ErrorMessage);
                return DatabaseResult.Failure(hasActiveOrdersResult.ErrorMessage!, hasActiveOrdersResult.ErrorCode);
            }

            if (hasActiveOrdersResult.Value)
            {
                logger.LogWarning("Cannot delete customer {CustomerId} - has active orders", customerId);
                return DatabaseResult.Failure(
                    $"Cannot delete customer with ID {customerId} because they have active orders. Complete or cancel orders first.",
                    DatabaseErrorCode.ConstraintViolation);
            }

            logger.LogDebug("Customer {CustomerId} has no active orders, deletion allowed", customerId);
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateCustomerRestorationBusinessRules( int customerId )
        {
            // Get customer details
            DatabaseResult<Customer?> customerResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.GetByIdAsync(customerId),
                $"Retrieving customer {customerId} for restoration validation",
                enableRetry: false);

            if (!customerResult.IsSuccess)
            {
                logger.LogError("Failed to retrieve customer {CustomerId} for restoration validation: {ErrorMessage}", customerId, customerResult.ErrorMessage);
                return DatabaseResult.Failure(customerResult.ErrorMessage!, customerResult.ErrorCode);
            }

            if (customerResult.Value == null)
            {
                logger.LogWarning("Customer {CustomerId} not found during restoration validation", customerId);
                return DatabaseResult.Failure(
                    $"Customer with ID {customerId} not found for restoration validation.",
                    DatabaseErrorCode.NotFound);
            }

            // Check for email conflicts with active customers (if email is provided)
            if (!string.IsNullOrWhiteSpace(customerResult.Value.Email))
            {
                DatabaseResult emailCheck = await CheckEmailConflict(customerId, customerResult.Value);
                if (!emailCheck.IsSuccess)
                    return emailCheck;
            }

            // Check for phone conflicts with active customers (if phone is provided)
            if (!string.IsNullOrWhiteSpace(customerResult.Value.Phone))
            {
                DatabaseResult phoneCheck = await CheckPhoneConflict(customerId, customerResult.Value);
                if (!phoneCheck.IsSuccess)
                    return phoneCheck;
            }

            logger.LogInformation("Customer {CustomerId} passed all restoration business rule validations", customerId);
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckEmailConflict( int customerId, Customer customer )
        {
            DatabaseResult<bool> emailConflictResult = await EmailExistsAsync(
                customer.Email!,
                customerId,
                false);

            if (!emailConflictResult.IsSuccess)
            {
                logger.LogError("Failed to check email conflict for customer {CustomerId}: {ErrorMessage}", customerId, emailConflictResult.ErrorMessage);
                return DatabaseResult.Failure(emailConflictResult.ErrorMessage!, emailConflictResult.ErrorCode);
            }

            if (emailConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore customer {CustomerId} - email '{Email}' conflicts with existing active customer",
                    customerId,
                    customer.Email);
                return DatabaseResult.Failure(
                    $"Cannot restore customer: Another active customer with email '{customer.Email}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckPhoneConflict( int customerId, Customer customer )
        {
            DatabaseResult<bool> phoneConflictResult = await PhoneExistsAsync(
                customer.Phone!,
                customerId,
                false);

            if (!phoneConflictResult.IsSuccess)
            {
                logger.LogError("Failed to check phone conflict for customer {CustomerId}: {ErrorMessage}", customerId, phoneConflictResult.ErrorMessage);
                return DatabaseResult.Failure(phoneConflictResult.ErrorMessage!, phoneConflictResult.ErrorCode);
            }

            if (phoneConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore customer {CustomerId} - phone '{Phone}' conflicts with existing active customer",
                    customerId,
                    customer.Phone);
                return DatabaseResult.Failure(
                    $"Cannot restore customer: Another active customer with phone '{customer.Phone}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        #endregion
    }
}
