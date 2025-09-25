using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    /// <summary>
    ///     Main interface for product service operations with enhanced error handling.
    /// </summary>
    public interface IProductService
    {
        #region Read Operations

        /// <summary>
        ///     Gets a product by its ID from the store (no database call needed).
        /// </summary>
        ProductDto? GetProductById( int productId );

        /// <summary>
        ///     Gets a product by its SKU from the store (no database call needed).
        /// </summary>
        ProductDto? GetProductBySku( string sku );

        /// <summary>
        ///     Retrieves all products from database and loads them into the store.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync();

        /// <summary>
        ///     Gets all active products from the database.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync();

        /// <summary>
        ///     Gets products by category ID.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId );

        /// <summary>
        ///     Gets products by supplier ID.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId );

        /// <summary>
        ///     Gets products that are below their minimum stock level.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        /// <summary>
        ///     Gets products with extended details (joins supplier, category, stock info).
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync();

        /// <summary>
        ///     Searches products by name, SKU, or description.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm );

        /// <summary>
        ///     Gets a paged list of products.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Gets the total count of products.
        /// </summary>
        Task<DatabaseResult<int>> GetTotalProductCountAsync();

        /// <summary>
        ///     Gets the count of active products.
        /// </summary>
        Task<DatabaseResult<int>> GetActiveProductCountAsync();

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new product with business validation.
        /// </summary>
        Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto );

        /// <summary>
        ///     Updates an existing product with business validation.
        /// </summary>
        Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto );

        /// <summary>
        ///     Permanently deletes a product.
        /// </summary>
        Task<DatabaseResult> DeleteProductAsync( int productId );

        /// <summary>
        ///     Soft deletes a product (sets IsActive = false).
        /// </summary>
        Task<DatabaseResult> SoftDeleteProductAsync( int productId );

        #endregion

        #region Validation

        /// <summary>
        ///     Checks if a product exists by ID (from store first, then database if needed).
        /// </summary>
        Task<DatabaseResult<bool>> ProductExistsAsync( int productId );

        /// <summary>
        ///     Checks if a SKU is available for use.
        /// </summary>
        Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null );

        #endregion

        #region Store Operations

        /// <summary>
        ///     Searches products by various criteria from the store.
        /// </summary>
        IEnumerable<ProductDto> SearchProducts( string? searchTerm = null, int? categoryId = null, bool? isActive = null );

        /// <summary>
        ///     Gets products that are below their minimum stock level from the store.
        /// </summary>
        IEnumerable<ProductDto> GetLowStockProducts();

        #endregion
    }
}
