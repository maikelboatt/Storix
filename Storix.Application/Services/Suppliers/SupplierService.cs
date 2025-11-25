using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Enums;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Domain.Models;

namespace Storix.Application.Services.Suppliers
{
    public class SupplierService(
        ISupplierReadService supplierReadService,
        ISupplierCacheReadService supplierCacheReadService,
        ISupplierValidationService supplierValidationService,
        ISupplierWriteService supplierWriteService,
        ILogger<SupplierService> logger ):ISupplierService
    {
        #region Read Operations

        public async Task<DatabaseResult<SupplierDto?>> GetSupplierById( int supplierId )
        {
            SupplierDto? cached = supplierCacheReadService.GetSupplierByIdInCache(supplierId);
            if (cached != null)
                return DatabaseResult<SupplierDto?>.Success(cached);

            DatabaseResult<SupplierDto?> result = await supplierReadService.GetSupplierByIdAsync(supplierId);
            return result!;
        }

        public async Task<DatabaseResult<SupplierDto?>> GetSupplierByEmail( string email )
        {
            SupplierDto? cached = supplierCacheReadService.GetSupplierByEmailInCache(email);
            if (cached != null)
                return DatabaseResult<SupplierDto?>.Success(cached);

            DatabaseResult<SupplierDto?> result = await supplierReadService.GetSupplierByEmailAsync(email);
            return result;
        }

        public async Task<DatabaseResult<SupplierDto?>> GetSupplierByPhone( string phone )
        {
            SupplierDto? cached = supplierCacheReadService.GetSupplierByPhoneInCache(phone);
            if (cached != null)
                return DatabaseResult<SupplierDto?>.Success(cached);

            DatabaseResult<SupplierDto?> result = await supplierReadService.GetSupplierByPhoneAsync(phone);
            return result;
        }

        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> GetAllAsync() => await supplierReadService.GetAllSuppliersAsync();

        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> GetAllActiveSuppliersAsync() => await supplierReadService.GetsAllActiveSuppliersAsync();

        public async Task<DatabaseResult<IEnumerable<Supplier>>> GetAllDeletedAsync() => await supplierReadService.GetsAllDeletedSuppliersAsync();

        public async Task<DatabaseResult<int>> GetTotalCountAsync() => await supplierReadService.GetTotalCountAsync();

        public async Task<DatabaseResult<int>> GetTotalActiveCountAsync() => await supplierReadService.GetActiveCountAsync();

        public async Task<DatabaseResult<int>> GetTotalDeletedCountAsync() => await supplierReadService.GetDeletedCountAsync();

        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> SearchSuppliersAsync( string searchTerm ) =>
            await supplierReadService.SearchAsync(searchTerm);

        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> GetSuppliersPagedAsync( int pageNumber, int pageSize ) => await
            supplierReadService.GetPagedAsync(pageNumber, pageSize);

        public void RefreshStoreCache() => supplierCacheReadService.RefreshStoreCache();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<SupplierDto>> CreateSupplierAsync( CreateSupplierDto createDto ) =>
            await supplierWriteService.CreateSupplierAsync(createDto);

        public async Task<DatabaseResult<SupplierDto>> UpdateSupplierAsync( UpdateSupplierDto updateDto ) =>
            await supplierWriteService.UpdateSupplierAsync(updateDto);

        public async Task<DatabaseResult> SoftDeleteSupplierAsync( int supplierId ) => await supplierWriteService.SoftDeleteSupplierAsync(supplierId);

        public async Task<DatabaseResult> RestoreSupplierAsync( int supplierId ) => await supplierWriteService.RestoreSupplierAsync(supplierId);

        // Legacy method - now uses SoftDeleteSupplierAsync for backward compatibility
        [Obsolete("Use SoftDeleteSupplierAsync instead. This method will be removed in a future version.")]
        public async Task<DatabaseResult> HardDeleteSupplierAsync( int supplierId ) => await supplierWriteService.HardDeleteSupplierAsync(supplierId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> SupplierExistsAsync( int supplierId, bool includeDeleted = false ) =>
            await supplierValidationService.SupplierExistsAsync(supplierId, includeDeleted);

        public async Task<DatabaseResult<bool>> EmailExistAsync( string email, int? excludedId = null, bool includeDeleted = false ) =>
            await supplierValidationService.EmailExistsAsync(email, excludedId, includeDeleted);

        public async Task<DatabaseResult<bool>> PhoneExistAsync( string email, int? excludedId = null, bool includeDeleted = false ) =>
            await supplierValidationService.PhoneExistsAsync(email, excludedId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsSupplierSoftDeleted( int supplierId ) => await supplierValidationService.IsSupplierSoftDeleted(supplierId);

        public async Task<DatabaseResult> ValidateForDeletion( int supplierId ) => await supplierValidationService.ValidateForDeletion(supplierId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int supplierId ) => await supplierValidationService.ValidateForHardDeletion(supplierId);

        public async Task<DatabaseResult> ValidateForRestore( int supplierId ) => await supplierValidationService.ValidateForRestore(supplierId);

        #endregion

        #region Bulk Operations

        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> BulkSoftDeleteAsync( IEnumerable<int> supplierIds )
        {
            IEnumerable<int> enumerable = supplierIds.ToList();
            logger.LogInformation("Starting bulk soft delete for {Count} suppliers", enumerable.Count());

            List<SupplierDto> processedSuppliers = [];
            List<string> errors = [];

            foreach (int supplierId in enumerable)
            {
                DatabaseResult result = await SoftDeleteSupplierAsync(supplierId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Supplier {supplierId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to soft delete supplier {SupplierId}: {Error}", supplierId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<SupplierDto>>.Failure(
                    $"Bulk soft delete completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk soft delete completed successfully for {Count} suppliers", enumerable.Count());
            return DatabaseResult<IEnumerable<SupplierDto>>.Success(processedSuppliers);
        }

        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> BulkRestoreAsync( IEnumerable<int> supplierIds )
        {
            IEnumerable<int> enumerable = supplierIds.ToList();
            logger.LogInformation("Starting bulk restore for {Count} suppliers", enumerable.Count());

            List<SupplierDto> processedSuppliers = [];
            List<string> errors = [];

            foreach (int supplierId in enumerable)
            {
                DatabaseResult result = await RestoreSupplierAsync(supplierId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Supplier {supplierId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to restore supplier {SupplierId}: {Error}", supplierId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<SupplierDto>>.Failure(
                    $"Bulk restore completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk restore completed successfully for {Count} suppliers", enumerable.Count());
            return DatabaseResult<IEnumerable<SupplierDto>>.Success(processedSuppliers);
        }

        #endregion
    }
}
