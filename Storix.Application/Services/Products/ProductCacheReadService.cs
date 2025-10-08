using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    public class ProductCacheReadService(
        IProductStore productStore,
        IProductReadService productReadService,
        ILogger<ProductCacheReadService> logger ):IProductCacheReadService
    {
        /// <summary>
        ///     Searches active products in the in-memory cache (fast).
        ///     Only returns active (non-deleted) products.
        /// </summary>
        public IEnumerable<ProductDto> SearchProductsInCache( string? searchTerm = null, int? categoryId = null )
        {
            logger.LogDebug(
                "Searching active products in cache with term '{SearchTerm}', categoryId {CategoryId}",
                searchTerm,
                categoryId);

            IEnumerable<Product> products = productStore.SearchProducts(searchTerm, categoryId);
            return products.Select(p => p.ToDto());
        }

        /// <summary>
        ///     Gets a product by ID from cache (fast).
        ///     Only returns if product is active (non-deleted).
        /// </summary>
        public ProductDto? GetProductByIdFromCache( int productId )
        {
            logger.LogDebug("Retrieving product {ProductId} from cache", productId);
            return productStore.GetById(productId);
        }

        /// <summary>
        ///     Gets a product by SKU from cache (fast).
        ///     Only returns if product is active (non-deleted).
        /// </summary>
        public ProductDto? GetProductBySkuFromCache( string sku )
        {
            logger.LogDebug("Retrieving product with SKU '{SKU}' from cache", sku);
            return productStore.GetBySKU(sku);
        }

        /// <summary>
        ///     Gets all active products from cache (fast).
        /// </summary>
        public IEnumerable<ProductDto> GetActiveProductsFromCache()
        {
            logger.LogDebug("Retrieving all active products from cache");
            return productStore.GetActiveProducts();
        }

        /// <summary>
        ///     Gets active products by category from cache (fast).
        /// </summary>
        public List<ProductDto> GetProductsByCategoryFromCache( int categoryId )
        {
            logger.LogDebug("Retrieving active products from cache for category {CategoryId}", categoryId);
            return productStore.GetByCategory(categoryId);
        }

        /// <summary>
        ///     Gets active products by supplier from cache (fast).
        /// </summary>
        public List<ProductDto> GetProductsBySupplierFromCache( int supplierId )
        {
            logger.LogDebug("Retrieving active products from cache for supplier {SupplierId}", supplierId);
            return productStore.GetBySupplier(supplierId);
        }

        /// <summary>
        ///     Checks if a product exists in the active cache (fast).
        /// </summary>
        public bool ProductExistsInCache( int productId ) => productStore.Exists(productId);

        /// <summary>
        ///     Gets the count of active products in cache (fast).
        /// </summary>
        public int GetActiveCountFromCache() => productStore.GetActiveCount();

        /// <summary>
        ///     Refreshes the product cache from the database.
        ///     Loads only active products into memory.
        /// </summary>
        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating product store cache refresh (active products only)");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Get only active products from database
                    DatabaseResult<IEnumerable<ProductDto>> result = await productReadService.GetAllActiveProductsAsync();

                    if (result.IsSuccess && result.Value != null)
                    {
                        logger.LogInformation(
                            "Product store cache refreshed successfully with {Count} active products",
                            result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning(
                            "Failed to refresh product store cache: {Error}",
                            result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing product store cache");
                }
            });
        }
    }
}
