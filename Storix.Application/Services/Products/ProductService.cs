using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Main service for managing product operations with ISoftDeletable support and enhanced error handling.
    /// </summary>
    public class ProductService(
        IProductReadService productReadService,
        IProductWriteService productWriteService,
        IProductValidationService productValidationService,
        IProductStore productStore,
        ILogger<ProductService> logger ):IProductService
    {
        #region Read Operations

        public ProductDto? GetProductById( int productId, bool includeDeleted = false ) => productReadService.GetProductById(productId, includeDeleted);

        public ProductDto? GetProductBySku( string sku, bool includeDeleted = false ) => productReadService.GetProductBySku(sku, includeDeleted);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync( bool includeDeleted = false ) => await productReadService.GetAllProductsAsync(includeDeleted);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync() => await productReadService.GetAllActiveProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync() => await productReadService.GetAllDeletedProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId, bool includeDeleted = false ) =>
            await productReadService.GetProductsByCategoryAsync(categoryId, includeDeleted);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId, bool includeDeleted = false ) =>
            await productReadService.GetProductsBySupplierAsync(supplierId, includeDeleted);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync() => await productReadService.GetLowStockProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync( bool includeDeleted = false ) =>
            await productReadService.GetProductsWithDetailsAsync(includeDeleted);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm, bool includeDeleted = false ) =>
            await productReadService.SearchProductsAsync(searchTerm, includeDeleted);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false ) =>
            await productReadService.GetProductsPagedAsync(pageNumber, pageSize, includeDeleted);

        public async Task<DatabaseResult<int>> GetTotalProductCountAsync( bool includeDeleted = false ) => await productReadService.GetTotalProductCountAsync(includeDeleted);

        public async Task<DatabaseResult<int>> GetActiveProductCountAsync() => await productReadService.GetActiveProductCountAsync();

        public async Task<DatabaseResult<int>> GetDeletedProductCountAsync() => await productReadService.GetDeletedProductCountAsync();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto ) => await productWriteService.CreateProductAsync(createProductDto);

        public async Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto ) => await productWriteService.UpdateProductAsync(updateProductDto);

        public async Task<DatabaseResult> SoftDeleteProductAsync( int productId ) => await productWriteService.SoftDeleteProductAsync(productId);

        public async Task<DatabaseResult> RestoreProductAsync( int productId ) => await productWriteService.RestoreProductAsync(productId);

        public async Task<DatabaseResult> HardDeleteProductAsync( int productId ) => await productWriteService.HardDeleteProductAsync(productId);

        // Legacy method - now uses SoftDeleteProductAsync for backward compatibility
        [Obsolete("Use SoftDeleteProductAsync instead. This method will be removed in a future version.")]
        public async Task<DatabaseResult> DeleteProductAsync( int productId ) => await SoftDeleteProductAsync(productId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false ) =>
            await productValidationService.ProductExistsAsync(productId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null, bool includeDeleted = false ) =>
            await productValidationService.IsSkuAvailableAsync(sku, excludeProductId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId ) => await productValidationService.IsProductSoftDeleted(productId);

        public async Task<DatabaseResult> ValidateForDeletion( int productId ) => await productValidationService.ValidateForDeletion(productId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int productId ) => await productValidationService.ValidateForHardDeletion(productId);

        public async Task<DatabaseResult> ValidateForRestore( int productId ) => await productValidationService.ValidateForRestore(productId);

        #endregion

        #region Store Operations

        public IEnumerable<ProductDto> SearchProducts( string? searchTerm = null, int? categoryId = null, bool includeDeleted = false )
        {
            logger.LogDebug(
                "Searching products with term '{SearchTerm}', categoryId {CategoryId}, includeDeleted {IncludeDeleted}",
                searchTerm,
                categoryId,
                includeDeleted);

            IEnumerable<Product> products = productStore.SearchProducts(searchTerm, categoryId, includeDeleted);
            return products.ToDto();
        }

        public IEnumerable<ProductDto> GetLowStockProducts( bool includeDeleted = false )
        {
            logger.LogDebug("Retrieving low stock products from store (includeDeleted: {IncludeDeleted})", includeDeleted);
            IEnumerable<Product> products = productStore.GetLowStockProducts(includeDeleted);
            return products.ToDto();
        }

        public IEnumerable<ProductDto> GetDeletedProductsFromStore()
        {
            logger.LogDebug("Retrieving deleted products from store cache");
            List<ProductDto> products = productStore.GetDeletedProducts();
            return products;
        }

        public void RefreshStoreCache()
        {
            logger.LogInformation("Refreshing product store cache");
            _ = Task.Run(async () =>
            {
                try
                {
                    DatabaseResult<IEnumerable<ProductDto>> result = await GetAllProductsAsync();
                    if (result.IsSuccess && result.Value != null)
                    {
                        logger.LogInformation("Product store cache refreshed successfully");
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh product store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing product store cache");
                }
            });
        }

        #endregion

        #region Bulk Operations

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> BulkSoftDeleteAsync( IEnumerable<int> productIds )
        {
            logger.LogInformation("Starting bulk soft delete for {Count} products", productIds.Count());

            List<ProductDto> processedProducts = new();
            List<string> errors = new();

            foreach (int productId in productIds)
            {
                DatabaseResult result = await SoftDeleteProductAsync(productId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Product {productId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to soft delete product {ProductId}: {Error}", productId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    $"Bulk soft delete completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk soft delete completed successfully for {Count} products", productIds.Count());
            return DatabaseResult<IEnumerable<ProductDto>>.Success(processedProducts);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> BulkRestoreAsync( IEnumerable<int> productIds )
        {
            logger.LogInformation("Starting bulk restore for {Count} products", productIds.Count());

            List<ProductDto> processedProducts = new();
            List<string> errors = new();

            foreach (int productId in productIds)
            {
                DatabaseResult result = await RestoreProductAsync(productId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Product {productId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to restore product {ProductId}: {Error}", productId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    $"Bulk restore completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk restore completed successfully for {Count} products", productIds.Count());
            return DatabaseResult<IEnumerable<ProductDto>>.Success(processedProducts);
        }

        #endregion
    }
}
