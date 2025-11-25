using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;
using Storix.Application.Enums;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers
{
    /// <summary>
    /// Main service for managing customer operations with ISoftDeletable support and enhanced error handling.
    /// </summary>
    public class CustomerService(
        ICustomerReadService customerReadService,
        ICustomerCacheReadService customerCacheReadService,
        ICustomerValidationService customerValidationService,
        ICustomerWriteService customerWriteService,
        ILogger<CustomerService> logger ):ICustomerService
    {
        #region Read Operations

        public async Task<DatabaseResult<CustomerDto?>> GetCustomerById( int customerId )
        {
            CustomerDto? cached = customerCacheReadService.GetCustomerByIdInCache(customerId);
            if (cached != null)
                return DatabaseResult<CustomerDto?>.Success(cached);

            return (await customerReadService.GetCustomerByIdAsync(customerId))!;
        }

        public async Task<DatabaseResult<CustomerDto?>> GetCustomerByEmail( string email )
        {
            CustomerDto? cached = customerCacheReadService.GetCustomerByEmailInCache(email);
            if (cached != null)
                return DatabaseResult<CustomerDto?>.Success(cached);

            return await customerReadService.GetCustomerByEmailAsync(email);
        }

        public async Task<DatabaseResult<CustomerDto?>> GetCustomerByPhone( string phone )
        {
            CustomerDto? cached = customerCacheReadService.GetCustomerByPhoneInCache(phone);
            if (cached != null)
                return DatabaseResult<CustomerDto?>.Success(cached);

            return await customerReadService.GetCustomerByPhoneAsync(phone);
        }

        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> GetAllAsync() => await customerReadService.GetAllCustomersAsync();

        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> GetAllActiveCustomersAsync() => await customerReadService.GetsAllActiveCustomersAsync();

        public async Task<DatabaseResult<IEnumerable<Customer>>> GetAllDeletedAsync() => await customerReadService.GetsAllDeletedCustomersAsync();

        public async Task<DatabaseResult<int>> GetTotalCountAsync() => await customerReadService.GetTotalCountAsync();

        public async Task<DatabaseResult<int>> GetTotalActiveCountAsync() => await customerReadService.GetActiveCountAsync();

        public async Task<DatabaseResult<int>> GetTotalDeletedCountAsync() => await customerReadService.GetDeletedCountAsync();

        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> SearchCustomersAsync( string searchTerm ) =>
            await customerReadService.SearchAsync(searchTerm);

        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> GetCustomersPagedAsync( int pageNumber, int pageSize ) => await
            customerReadService.GetPagedAsync(pageNumber, pageSize);

        public void RefreshStoreCache() => customerCacheReadService.RefreshStoreCache();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<CustomerDto>> CreateCustomerAsync( CreateCustomerDto createDto ) =>
            await customerWriteService.CreateCustomerAsync(createDto);

        public async Task<DatabaseResult<CustomerDto>> UpdateCustomerAsync( UpdateCustomerDto updateDto ) =>
            await customerWriteService.UpdateCustomerAsync(updateDto);

        public async Task<DatabaseResult> SoftDeleteCustomerAsync( int customerId ) => await customerWriteService.SoftDeleteCustomerAsync(customerId);

        public async Task<DatabaseResult> RestoreCustomerAsync( int customerId ) => await customerWriteService.RestoreCustomerAsync(customerId);

        // Legacy method - now uses SoftDeleteCustomerAsync for backward compatibility
        [Obsolete("Use SoftDeleteCustomerAsync instead. This method will be removed in a future version.")]
        public async Task<DatabaseResult> HardDeleteCustomerAsync( int customerId ) => await customerWriteService.HardDeleteCustomerAsync(customerId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> CustomerExistsAsync( int customerId, bool includeDeleted = false ) =>
            await customerValidationService.CustomerExistsAsync(customerId, includeDeleted);

        public async Task<DatabaseResult<bool>> EmailExistAsync( string email, int? excludedId = null, bool includeDeleted = false ) =>
            await customerValidationService.EmailExistsAsync(email, excludedId, includeDeleted);

        public async Task<DatabaseResult<bool>> PhoneExistAsync( string email, int? excludedId = null, bool includeDeleted = false ) =>
            await customerValidationService.PhoneExistsAsync(email, excludedId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsCustomerSoftDeleted( int customerId ) => await customerValidationService.IsCustomerSoftDeleted(customerId);

        public async Task<DatabaseResult> ValidateForDeletion( int customerId ) => await customerValidationService.ValidateForDeletion(customerId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int customerId ) => await customerValidationService.ValidateForHardDeletion(customerId);

        public async Task<DatabaseResult> ValidateForRestore( int customerId ) => await customerValidationService.ValidateForRestore(customerId);

        #endregion

        #region Bulk Operations

        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> BulkSoftDeleteAsync( IEnumerable<int> customerIds )
        {
            IEnumerable<int> enumerable = customerIds.ToList();
            logger.LogInformation("Starting bulk soft delete for {Count} customers", enumerable.Count());

            List<CustomerDto> processedCustomers = [];
            List<string> errors = [];

            foreach (int customerId in enumerable)
            {
                DatabaseResult result = await SoftDeleteCustomerAsync(customerId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Customer {customerId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to soft delete customer {CustomerId}: {Error}", customerId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<CustomerDto>>.Failure(
                    $"Bulk soft delete completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk soft delete completed successfully for {Count} customers", enumerable.Count());
            return DatabaseResult<IEnumerable<CustomerDto>>.Success(processedCustomers);
        }

        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> BulkRestoreAsync( IEnumerable<int> customerIds )
        {
            IEnumerable<int> enumerable = customerIds.ToList();
            logger.LogInformation("Starting bulk restore for {Count} customers", enumerable.Count());

            List<CustomerDto> processedCustomers = [];
            List<string> errors = [];

            foreach (int customerId in enumerable)
            {
                DatabaseResult result = await RestoreCustomerAsync(customerId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Customer {customerId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to restore customer {CustomerId}: {Error}", customerId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<CustomerDto>>.Failure(
                    $"Bulk restore completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk restore completed successfully for {Count} customers", enumerable.Count());
            return DatabaseResult<IEnumerable<CustomerDto>>.Success(processedCustomers);
        }

        #endregion
    }
}
