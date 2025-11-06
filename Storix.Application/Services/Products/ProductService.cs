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
    ///     Main facade service for managing product operations with ISoftDeletable support.
    ///     Combines read, write, and validation services with in-memory cache for performance.
    /// </summary>
    public class ProductService(
        IProductReadService productReadService,
        IProductCacheReadService productCacheReadService,
        IProductWriteService productWriteService,
        IProductValidationService productValidationService,
        ILogger<ProductService> logger ):IProductService
    {
        #region Read Operations

        public async Task<DatabaseResult<ProductDto>> GetProductById( int productId )
        {
            ProductDto? cached = productCacheReadService.GetProductByIdFromCache(productId);
            if (cached != null)
                return DatabaseResult<ProductDto>.Success(cached);

            return await productReadService.GetProductById(productId);
        }

        public async Task<DatabaseResult<ProductDto>> GetProductBySku( string sku )
        {
            ProductDto? cached = productCacheReadService.GetProductBySkuFromCache(sku);
            if (cached != null)
                return DatabaseResult<ProductDto>.Success(cached);

            return await productReadService.GetProductBySku(sku);
        }

        public async Task<DatabaseResult<IEnumerable<TopProductDto>>> GetTop5BestSellersAsync( int topCounts = 5, int monthsBack = 3 )
        {
            List<TopProductDto> cached = productCacheReadService.GetTopBestSellersFromCache(topCounts);
            if (cached.Count == topCounts)
                return DatabaseResult<IEnumerable<TopProductDto>>.Success(cached);

            return await productReadService.GetTop5BestSellersAsync(topCounts, monthsBack);
        }


        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync() => await productReadService.GetAllProductsAsync();

        public async Task<DatabaseResult<IEnumerable<Product>>> GetAllActiveProductsAsync() => await productReadService.GetAllActiveProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductListDto>>> GetAllActiveProductsForListAsync() =>
            await productReadService.GetAllActiveProductsForListAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync() => await productReadService.GetAllDeletedProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync(
            int categoryId ) => await productReadService.GetProductsByCategoryAsync(categoryId);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync(
            int supplierId ) => await productReadService.GetProductsBySupplierAsync(supplierId);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync() => await productReadService.GetLowStockProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync() =>
            await productReadService.GetProductsWithDetailsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync(
            string searchTerm ) => await productReadService.SearchProductsAsync(searchTerm);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync(
            int pageNumber,
            int pageSize ) => await productReadService.GetProductsPagedAsync(pageNumber, pageSize);

        public async Task<DatabaseResult<int>> GetTotalProductCountAsync() => await productReadService.GetTotalProductCountAsync();

        public async Task<DatabaseResult<int>> GetActiveProductCountAsync() => await productReadService.GetActiveProductCountAsync();

        public async Task<DatabaseResult<int>> GetDeletedProductCountAsync() => await productReadService.GetDeletedProductCountAsync();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto ) =>
            await productWriteService.CreateProductAsync(createProductDto);

        public async Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto ) =>
            await productWriteService.UpdateProductAsync(updateProductDto);

        public async Task<DatabaseResult> SoftDeleteProductAsync( int productId ) => await productWriteService.SoftDeleteProductAsync(productId);

        public async Task<DatabaseResult> RestoreProductAsync( int productId ) => await productWriteService.RestoreProductAsync(productId);

        public async Task<DatabaseResult> HardDeleteProductAsync( int productId ) => await productWriteService.HardDeleteProductAsync(productId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false ) =>
            await productValidationService.ProductExistsAsync(productId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsSkuAvailableAsync(
            string sku,
            int? excludeProductId = null,
            bool includeDeleted = false ) => await productValidationService.IsSkuAvailableAsync(sku, excludeProductId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId ) => await productValidationService.IsProductSoftDeleted(productId);

        public async Task<DatabaseResult> ValidateForDeletion( int productId ) => await productValidationService.ValidateForDeletion(productId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int productId ) => await productValidationService.ValidateForHardDeletion(productId);

        public async Task<DatabaseResult> ValidateForRestore( int productId ) => await productValidationService.ValidateForRestore(productId);

        #endregion

        #region Bulk Operations

        /// <summary>
        ///     Soft deletes multiple products in bulk.
        ///     Each product is validated and deleted individually.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<ProductDto>>> BulkSoftDeleteAsync( IEnumerable<int> productIds )
        {
            List<int> productIdList = productIds.ToList();
            logger.LogInformation("Starting bulk soft delete for {Count} products", productIdList.Count);

            List<string> errors = new();

            foreach (int productId in productIdList)
            {
                DatabaseResult result = await SoftDeleteProductAsync(productId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Product {productId}: {result.ErrorMessage}");
                    logger.LogWarning(
                        "Failed to soft delete product {ProductId}: {Error}",
                        productId,
                        result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    $"Bulk soft delete completed with {errors.Count} error(s): {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation(
                "Bulk soft delete completed successfully for {Count} products",
                productIdList.Count);
            return DatabaseResult<IEnumerable<ProductDto>>.Success(Enumerable.Empty<ProductDto>());
        }

        /// <summary>
        ///     Restores multiple soft-deleted products in bulk.
        ///     Each product is validated and restored individually.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<ProductDto>>> BulkRestoreAsync( IEnumerable<int> productIds )
        {
            List<int> productIdList = productIds.ToList();
            logger.LogInformation("Starting bulk restore for {Count} products", productIdList.Count);

            List<ProductDto> restored = new();
            List<string> errors = new();

            foreach (int productId in productIdList)
            {
                DatabaseResult result = await RestoreProductAsync(productId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Product {productId}: {result.ErrorMessage}");
                    logger.LogWarning(
                        "Failed to restore product {ProductId}: {Error}",
                        productId,
                        result.ErrorMessage);
                }
                else
                {
                    // Get the restored product from cache
                    ProductDto? restoredProduct = productCacheReadService.GetProductByIdFromCache(productId);
                    if (restoredProduct != null)
                    {
                        restored.Add(restoredProduct);
                    }
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    $"Bulk restore completed with {errors.Count} error(s): {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation(
                "Bulk restore completed successfully for {Count} products",
                productIdList.Count);
            return DatabaseResult<IEnumerable<ProductDto>>.Success(restored);
        }

        #endregion
    }
}
