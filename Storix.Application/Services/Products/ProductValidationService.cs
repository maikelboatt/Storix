using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Service responsible for product validation operations with ISoftDeletable support.
    /// </summary>
    public class ProductValidationService(
        IProductRepository productRepository,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<ProductValidationService> logger ):IProductValidationService
    {
        public async Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false )
        {
            if (productId <= 0)
                return DatabaseResult<bool>.Success(false);

            // Check store first (store only contains active/non-deleted products)
            if (!includeDeleted)
            {
                ProductDto? productInStore = productStore.GetById(productId);
                if (productInStore != null)
                    return DatabaseResult<bool>.Success(true);
            }

            // Check database
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.ExistsAsync(productId, includeDeleted),
                $"Checking if product {productId} exists (includeDeleted: {includeDeleted})",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(sku))
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.SkuExistsAsync(sku.Trim(), excludeProductId, includeDeleted),
                $"Checking if SKU '{sku}' exists (excludeProductId: {excludeProductId}, includeDeleted: {includeDeleted})",
                false
            );

            if (result.IsSuccess)
            {
                // Return true if SKU doesn't exist (available)
                return DatabaseResult<bool>.Success(!result.Value);
            }

            return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForDeletion( int productId )
        {
            // Check existence (only active products can be deleted)
            DatabaseResult<bool> existsResult = await ProductExistsAsync(productId);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to delete non-existent or already deleted product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found or already deleted.", DatabaseErrorCode.NotFound);
            }

            // Additional business rules for product deletion
            DatabaseResult businessValidation = await ValidateProductDeletionBusinessRules(productId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int productId )
        {
            // Check existence (including soft-deleted products for hard deletion)
            DatabaseResult<bool> existsResult = await ProductExistsAsync(productId, true);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to hard delete non-existent product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found.", DatabaseErrorCode.NotFound);
            }

            // Additional business rules for hard deletion (more restrictive)
            DatabaseResult businessValidation = await ValidateProductHardDeletionBusinessRules(productId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            logger.LogWarning("Hard deletion validation passed for product {ProductId} - THIS WILL BE PERMANENT", productId);
            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForRestore( int productId )
        {
            // Check if product exists and is soft deleted
            DatabaseResult<bool> existsResult = await ProductExistsAsync(productId, true);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to restore non-existent product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found.", DatabaseErrorCode.NotFound);
            }

            // Check if product is actually deleted
            DatabaseResult<bool> activeExistsResult = await ProductExistsAsync(productId);
            if (!activeExistsResult.IsSuccess)
                return DatabaseResult.Failure(activeExistsResult.ErrorMessage!, activeExistsResult.ErrorCode);

            if (activeExistsResult.Value)
            {
                logger.LogWarning("Attempted to restore active product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} is not deleted and cannot be restored.", DatabaseErrorCode.InvalidInput);
            }

            // Additional business rules for restoration
            DatabaseResult businessValidation = await ValidateProductRestorationBusinessRules(productId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId )
        {
            if (productId <= 0)
                return DatabaseResult<bool>.Success(false);

            // If product exists with includeDeleted=true but not with includeDeleted=false, it's soft deleted
            DatabaseResult<bool> existsIncludingDeleted = await ProductExistsAsync(productId, true);
            if (!existsIncludingDeleted.IsSuccess || !existsIncludingDeleted.Value)
            {
                return existsIncludingDeleted.IsSuccess
                    ? DatabaseResult<bool>.Success(false)
                    : DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);
            }

            DatabaseResult<bool> existsActiveOnly = await ProductExistsAsync(productId);
            if (!existsActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existsActiveOnly.ErrorMessage!, existsActiveOnly.ErrorCode);

            // Product exists in database but not in active results = soft deleted
            bool isSoftDeleted = !existsActiveOnly.Value;
            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #region Business Rule Validation Methods

        /// <summary>
        ///     Validates business rules for soft deletion operations.
        /// </summary>
        private async Task<DatabaseResult> ValidateProductDeletionBusinessRules( int productId )
        {
            // Example business rules - customize based on your requirements:

            // 1. Check if product has active orders
            // DatabaseResult<bool> hasActiveOrdersResult = await CheckForActiveOrders(productId);
            // if (hasActiveOrdersResult.IsSuccess && hasActiveOrdersResult.Value)
            // {
            //     logger.LogWarning("Cannot delete product {ProductId}: has active orders", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot delete product: it has active orders.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            // 2. Check if product has recent stock movements
            // DatabaseResult<bool> hasRecentMovementsResult = await CheckForRecentStockMovements(productId);
            // if (hasRecentMovementsResult.IsSuccess && hasRecentMovementsResult.Value)
            // {
            //     logger.LogWarning("Cannot delete product {ProductId}: has recent stock movements", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot delete product: it has recent stock movements.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            // 3. Check if product is part of any bundles or kits
            // DatabaseResult<bool> isPartOfBundleResult = await CheckIfPartOfBundle(productId);
            // if (isPartOfBundleResult.IsSuccess && isPartOfBundleResult.Value)
            // {
            //     logger.LogWarning("Cannot delete product {ProductId}: is part of product bundles", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot delete product: it is part of one or more product bundles.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            logger.LogDebug("Product {ProductId} passed all soft deletion business rule validations", productId);
            return DatabaseResult.Success();
        }

        /// <summary>
        ///     Validates business rules for hard deletion operations (more restrictive).
        /// </summary>
        private async Task<DatabaseResult> ValidateProductHardDeletionBusinessRules( int productId )
        {
            // Hard deletion should be more restrictive than soft deletion

            // 1. Check if product has ANY historical orders
            // DatabaseResult<bool> hasHistoricalOrdersResult = await CheckForHistoricalOrders(productId);
            // if (hasHistoricalOrdersResult.IsSuccess && hasHistoricalOrdersResult.Value)
            // {
            //     logger.LogWarning("Cannot hard delete product {ProductId}: has historical orders", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot permanently delete product: it has historical orders.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            // 2. Check if product has ANY stock movement history
            // DatabaseResult<bool> hasStockHistoryResult = await CheckForStockMovementHistory(productId);
            // if (hasStockHistoryResult.IsSuccess && hasStockHistoryResult.Value)
            // {
            //     logger.LogWarning("Cannot hard delete product {ProductId}: has stock movement history", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot permanently delete product: it has stock movement history.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            // 3. Require special permissions or approval for hard deletion
            // DatabaseResult<bool> hasPermissionResult = await CheckHardDeletionPermissions();
            // if (!hasPermissionResult.IsSuccess || !hasPermissionResult.Value)
            // {
            //     logger.LogWarning("Hard deletion of product {ProductId} requires special permissions", productId);
            //     return DatabaseResult.Failure(
            //         $"Hard deletion requires special permissions.",
            //         DatabaseErrorCode.InsufficientPermissions);
            // }

            logger.LogDebug("Product {ProductId} passed all hard deletion business rule validations", productId);
            return DatabaseResult.Success();
        }

        /// <summary>
        ///     Validates business rules for product restoration operations.
        /// </summary>
        private async Task<DatabaseResult> ValidateProductRestorationBusinessRules( int productId )
        {
            // Example business rules for restoration:

            // 1. Check if supplier is still active
            // DatabaseResult<bool> supplierActiveResult = await CheckSupplierIsActive(productId);
            // if (!supplierActiveResult.IsSuccess || !supplierActiveResult.Value)
            // {
            //     logger.LogWarning("Cannot restore product {ProductId}: supplier is inactive", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot restore product: supplier is no longer active.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            // 2. Check if category still exists
            // DatabaseResult<bool> categoryExistsResult = await CheckCategoryExists(productId);
            // if (!categoryExistsResult.IsSuccess || !categoryExistsResult.Value)
            // {
            //     logger.LogWarning("Cannot restore product {ProductId}: category no longer exists", productId);
            //     return DatabaseResult.Failure(
            //         $"Cannot restore product: category no longer exists.",
            //         DatabaseErrorCode.ConstraintViolation);
            // }

            // 3. Check for data integrity issues that occurred while deleted
            // DatabaseResult dataIntegrityResult = await CheckDataIntegrityForRestore(productId);
            // if (!dataIntegrityResult.IsSuccess)
            //     return dataIntegrityResult;

            logger.LogDebug("Product {ProductId} passed all restoration business rule validations", productId);
            return DatabaseResult.Success();
        }

        #endregion
    }
}
