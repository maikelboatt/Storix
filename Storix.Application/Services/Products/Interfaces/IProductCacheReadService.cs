using System.Collections.Generic;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductCacheReadService
    {
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

        List<TopProductDto> GetTopBestSellersFromCache( int topCounts );

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
    }
}
