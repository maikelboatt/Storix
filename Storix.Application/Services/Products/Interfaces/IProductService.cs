using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductService
    {
        ProductDto? GetProductById( int productId );

        ProductDto? GetProductBySku( string sku );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync();

        Task<DatabaseResult<IEnumerable<Product>>> GetAllActiveProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync(
            int categoryId );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync(
            int supplierId );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync(
            string searchTerm );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync(
            int pageNumber,
            int pageSize );

        Task<DatabaseResult<int>> GetTotalProductCountAsync();

        Task<DatabaseResult<int>> GetActiveProductCountAsync();

        Task<DatabaseResult<int>> GetDeletedProductCountAsync();

        Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto );

        Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto );

        Task<DatabaseResult> SoftDeleteProductAsync( int productId );

        Task<DatabaseResult> RestoreProductAsync( int productId );

        Task<DatabaseResult> HardDeleteProductAsync( int productId );

        Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsSkuAvailableAsync(
            string sku,
            int? excludeProductId = null,
            bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId );

        Task<DatabaseResult> ValidateForDeletion( int productId );

        Task<DatabaseResult> ValidateForHardDeletion( int productId );

        Task<DatabaseResult> ValidateForRestore( int productId );

        /// <summary>
        ///     Searches active products in the in-memory cache (fast).
        ///     Only returns active (non-deleted) products.
        /// </summary>
        IEnumerable<ProductDto> SearchProductsInCache( string? searchTerm = null, int? categoryId = null );

        /// <summary>
        ///     Gets a product by ID from cache (fast).
        ///     Only returns if product is active (non-deleted).
        /// </summary>
        ProductDto? GetProductByIdFromCache( int productId );

        /// <summary>
        ///     Gets a product by SKU from cache (fast).
        ///     Only returns if product is active (non-deleted).
        /// </summary>
        ProductDto? GetProductBySkuFromCache( string sku );

        /// <summary>
        ///     Gets all active products from cache (fast).
        /// </summary>
        IEnumerable<ProductDto> GetActiveProductsFromCache();

        /// <summary>
        ///     Gets active products by category from cache (fast).
        /// </summary>
        List<ProductDto> GetProductsByCategoryFromCache( int categoryId );

        /// <summary>
        ///     Gets active products by supplier from cache (fast).
        /// </summary>
        List<ProductDto> GetProductsBySupplierFromCache( int supplierId );

        /// <summary>
        ///     Checks if a product exists in the active cache (fast).
        /// </summary>
        bool ProductExistsInCache( int productId );

        /// <summary>
        ///     Gets the count of active products in cache (fast).
        /// </summary>
        int GetActiveCountFromCache();

        /// <summary>
        ///     Refreshes the product cache from the database.
        ///     Loads only active products into memory.
        /// </summary>
        void RefreshStoreCache();

        /// <summary>
        ///     Soft deletes multiple products in bulk.
        ///     Each product is validated and deleted individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> BulkSoftDeleteAsync( IEnumerable<int> productIds );

        /// <summary>
        ///     Restores multiple soft-deleted products in bulk.
        ///     Each product is validated and restored individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> BulkRestoreAsync( IEnumerable<int> productIds );
    }
}
