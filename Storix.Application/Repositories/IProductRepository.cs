using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IProductRepository
    {
        /// <summary>
        ///     Gets the total count of products.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of active products (non-deleted and IsActive = true).
        /// </summary>
        Task<int> GetActiveCountAsync();

        /// <summary>
        ///     Gets the count of soft-deleted products.
        /// </summary>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        ///     Gets a paged list of products.
        /// </summary>
        /// <param name="pageNumber" >The page number (1-based).</param>
        /// <param name="pageSize" >The number of items per page.</param>
        Task<IEnumerable<Product>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Checks if a product exists by ID.
        /// </summary>
        /// <param name="productId" >The product ID to check.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products in the check.</param>
        Task<bool> ExistsAsync( int productId, bool includeDeleted = false );

        /// <summary>
        ///     Checks if a SKU already exists (optionally excluding a specific product ID).
        /// </summary>
        /// <param name="sku" >The SKU to check.</param>
        /// <param name="excludeProductId" >Product ID to exclude from the check (for updates).</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products in the check.</param>
        Task<bool> SkuExistsAsync( string sku, int? excludeProductId = null, bool includeDeleted = false );

        /// <summary>
        ///     Creates a new product and returns it with its generated ID.
        /// </summary>
        Task<Product> CreateAsync( Product product );

        /// <summary>
        ///     Updates an existing product and returns it with the updated timestamp.
        /// </summary>
        Task<Product> UpdateAsync( Product product );

        /// <summary>
        ///     Permanently deletes a product by ID (hard delete).
        /// </summary>
        Task<DatabaseResult> HardDeleteAsync( int productId );

        /// <summary>
        ///     Soft deletes a product (sets IsDeleted = true and DeletedAt = current timestamp).
        /// </summary>
        Task<DatabaseResult> SoftDeleteAsync( int productId );

        /// <summary>
        ///     Restores a soft-deleted product (sets IsDeleted = false and DeletedAt = null).
        /// </summary>
        Task<DatabaseResult> RestoreAsync( int productId );

        /// <summary>
        ///     Gets a product by its ID.
        /// </summary>
        /// <param name="productId" >The product ID.</param>
        Task<Product?> GetByIdAsync( int productId );

        /// <summary>
        ///     Gets a product by its SKU.
        /// </summary>
        /// <param name="sku" >The product SKU.</param>
        Task<Product?> GetBySkuAsync( string sku );

        /// <summary>
        ///     Gets all products.
        /// </summary>
        Task<IEnumerable<Product>> GetAllAsync( bool includeDeleted = true );

        /// <summary>
        ///     Gets all active products (IsActive = true and IsDeleted = false).
        /// </summary>
        Task<IEnumerable<Product>> GetAllActiveAsync();

        Task<IEnumerable<TopProductDto>> GetTopBestSellersAsync( int topCount = 5, int monthsBack = 3 );

        /// <summary>
        ///     Gets all soft-deleted products.
        /// </summary>
        Task<IEnumerable<Product>> GetAllDeletedAsync();

        /// <summary>
        ///     Gets products by category ID.
        /// </summary>
        /// <param name="categoryId" >The category ID.</param>
        Task<IEnumerable<Product>> GetByCategoryAsync( int categoryId );

        /// <summary>
        ///     Gets products by supplier ID.
        /// </summary>
        /// <param name="supplierId" >The supplier ID.</param>
        Task<IEnumerable<Product>> GetBySupplierAsync( int supplierId );

        /// <summary>
        ///     Gets products that are below their minimum stock level (active products only).
        /// </summary>
        Task<IEnumerable<Product>> GetLowStockProductsAsync();

        /// <summary>
        ///     Gets products with extended details (joins supplier, category, stock info).
        /// </summary>
        Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync( bool includeDeleted = false );

        /// <summary>
        ///     Searches products by name, SKU, or description.
        /// </summary>
        /// <param name="searchTerm" >The search term.</param>
        Task<IEnumerable<Product>> SearchAsync( string searchTerm );
    }
}
