using Storix.Application.DTO;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    /// <summary>
    ///     Repository implementation for Product operations using Dapper and stored procedures.
    ///     Updated to support ISoftDeletable interface with IsDeleted and DeletedAt properties.
    /// </summary>
    /// <param name="sqlDataAccess" >The SQL data access instance.</param>
    public class ProductRepository( ISqlDataAccess sqlDataAccess ):IProductRepository
    {
        #region Pagination

        /// <summary>
        ///     Gets the total count of products.
        /// </summary>
        /// <param name="includeDeleted" >Whether to include soft-deleted products in the count.</param>
        public async Task<int> GetTotalCountAsync( bool includeDeleted = false )
        {
            string storedProcedure = includeDeleted ? "sp_GetProductCountIncludeDeleted" : "sp_GetProductCount";
            return await sqlDataAccess.ExecuteScalarAsync<int>(storedProcedure);
        }

        /// <summary>
        ///     Gets the count of active products (non-deleted and IsActive = true).
        /// </summary>
        public async Task<int> GetActiveCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetActiveProductCount");

        /// <summary>
        ///     Gets the count of soft-deleted products.
        /// </summary>
        public async Task<int> GetDeletedCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetDeletedProductCount");

        /// <summary>
        ///     Gets a paged list of products.
        /// </summary>
        /// <param name="pageNumber" >The page number (1-based).</param>
        /// <param name="pageSize" >The number of items per page.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<IEnumerable<Product>> GetPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false )
        {
            var parameters = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize,
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_GetProductsPagedIncludeDeleted" : "sp_GetProductsPaged";

            return await sqlDataAccess.QueryAsync<Product>(storedProcedure, parameters);
        }

        #endregion

        #region Validation

        /// <summary>
        ///     Checks if a product exists by ID.
        /// </summary>
        /// <param name="productId" >The product ID to check.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products in the check.</param>
        public async Task<bool> ExistsAsync( int productId, bool includeDeleted = false )
        {
            var parameters = new { ProductId = productId, IncludeDeleted = includeDeleted };

            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckProductExists",
                parameters);

            return count > 0;
        }

        /// <summary>
        ///     Checks if a SKU already exists (optionally excluding a specific product ID).
        /// </summary>
        /// <param name="sku" >The SKU to check.</param>
        /// <param name="excludeProductId" >Product ID to exclude from the check (for updates).</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products in the check.</param>
        public async Task<bool> SkuExistsAsync( string sku, int? excludeProductId = null, bool includeDeleted = false )
        {
            var parameters = new
            {
                SKU = sku,
                ExcludeProductId = excludeProductId,
                IncludeDeleted = includeDeleted
            };

            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckSkuExists",
                parameters);

            return count > 0;
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new product and returns it with its generated ID.
        /// </summary>
        public async Task<Product> CreateAsync( Product product )
        {
            var parameters = new
            {
                product.Name,
                product.SKU,
                product.Description,
                product.Barcode,
                product.Price,
                product.Cost,
                product.MinStockLevel,
                product.MaxStockLevel,
                product.SupplierId,
                product.CategoryId,
                product.CreatedDate,
                // ISoftDeletable properties - ensure new products are not deleted
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            };

            int newProductId = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CreateProduct",
                parameters);

            return product with
            {
                ProductId = newProductId,
                IsDeleted = false,
                DeletedAt = null
            };
        }

        /// <summary>
        ///     Updates an existing product and returns it with the updated timestamp.
        /// </summary>
        public async Task<Product> UpdateAsync( Product product )
        {
            var parameters = new
            {
                product.ProductId,
                product.Name,
                product.SKU,
                product.Description,
                product.Barcode,
                product.Price,
                product.Cost,
                product.MinStockLevel,
                product.MaxStockLevel,
                product.SupplierId,
                product.CategoryId,
                UpdatedDate = DateTime.UtcNow,
                // ISoftDeletable properties
                product.IsDeleted,
                product.DeletedAt
            };

            await sqlDataAccess.CommandAsync("sp_UpdateProduct", parameters);

            return product with { UpdatedDate = DateTime.UtcNow };
        }

        /// <summary>
        ///     Permanently deletes a product by ID (hard delete).
        /// </summary>
        public async Task<bool> HardDeleteAsync( int productId )
        {
            try
            {
                await sqlDataAccess.CommandAsync("sp_HardDeleteProduct", new { ProductId = productId });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Soft deletes a product (sets IsDeleted = true and DeletedAt = current timestamp).
        /// </summary>
        public async Task<bool> SoftDeleteAsync( int productId )
        {
            try
            {
                var parameters = new
                {
                    ProductId = productId,
                    DeletedAt = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await sqlDataAccess.CommandAsync("sp_SoftDeleteProduct", parameters);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Restores a soft-deleted product (sets IsDeleted = false and DeletedAt = null).
        /// </summary>
        public async Task<bool> RestoreAsync( int productId )
        {
            try
            {
                var parameters = new
                {
                    ProductId = productId,
                    UpdatedDate = DateTime.UtcNow
                };

                await sqlDataAccess.CommandAsync("sp_RestoreProduct", parameters);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets a product by its ID.
        /// </summary>
        /// <param name="productId" >The product ID.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<Product?> GetByIdAsync( int productId, bool includeDeleted = false )
        {
            var parameters = new { ProductId = productId, IncludeDeleted = includeDeleted };
            string storedProcedure = includeDeleted ? "sp_GetProductByIdIncludeDeleted" : "sp_GetProductById";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Product>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Gets a product by its SKU.
        /// </summary>
        /// <param name="sku" >The product SKU.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<Product?> GetBySkuAsync( string sku, bool includeDeleted = false )
        {
            var parameters = new { SKU = sku, IncludeDeleted = includeDeleted };
            string storedProcedure = includeDeleted ? "sp_GetProductBySkuIncludeDeleted" : "sp_GetProductBySku";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Product>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Gets all products.
        /// </summary>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<IEnumerable<Product>> GetAllAsync( bool includeDeleted = false )
        {
            string storedProcedure = includeDeleted ? "sp_GetAllProductsIncludeDeleted" : "sp_GetAllProducts";
            return await sqlDataAccess.QueryAsync<Product>(storedProcedure);
        }

        /// <summary>
        ///     Gets all active products (IsActive = true and IsDeleted = false).
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllActiveAsync() => await sqlDataAccess.QueryAsync<Product>("sp_GetAllActiveProducts");

        /// <summary>
        ///     Gets all soft-deleted products.
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllDeletedAsync() => await sqlDataAccess.QueryAsync<Product>("sp_GetAllDeletedProducts");

        /// <summary>
        ///     Gets products by category ID.
        /// </summary>
        /// <param name="categoryId" >The category ID.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<IEnumerable<Product>> GetByCategoryAsync( int categoryId, bool includeDeleted = false )
        {
            var parameters = new { CategoryId = categoryId, IncludeDeleted = includeDeleted };
            string storedProcedure = includeDeleted ? "sp_GetProductsByCategoryIncludeDeleted" : "sp_GetProductsByCategory";

            return await sqlDataAccess.QueryAsync<Product>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Gets products by supplier ID.
        /// </summary>
        /// <param name="supplierId" >The supplier ID.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<IEnumerable<Product>> GetBySupplierAsync( int supplierId, bool includeDeleted = false )
        {
            var parameters = new { SupplierId = supplierId, IncludeDeleted = includeDeleted };
            string storedProcedure = includeDeleted ? "sp_GetProductsBySupplierIncludeDeleted" : "sp_GetProductsBySupplier";

            return await sqlDataAccess.QueryAsync<Product>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Gets products that are below their minimum stock level (active products only).
        /// </summary>
        public async Task<IEnumerable<Product>> GetLowStockProductsAsync() => await sqlDataAccess.QueryAsync<Product>("sp_GetLowStockProducts");

        /// <summary>
        ///     Gets products with extended details (joins supplier, category, stock info).
        /// </summary>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync( bool includeDeleted = false )
        {
            var parameters = new { IncludeDeleted = includeDeleted };
            string storedProcedure = includeDeleted ? "sp_GetProductsWithDetailsIncludeDeleted" : "sp_GetProductsWithDetails";

            return await sqlDataAccess.QueryAsync<ProductWithDetailsDto>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Searches products by name, SKU, or description.
        /// </summary>
        /// <param name="searchTerm" >The search term.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted products.</param>
        public async Task<IEnumerable<Product>> SearchAsync( string searchTerm, bool includeDeleted = false )
        {
            var parameters = new
            {
                SearchTerm = $"%{searchTerm}%",
                IncludeDeleted = includeDeleted
            };
            string storedProcedure = includeDeleted ? "sp_SearchProductsIncludeDeleted" : "sp_SearchProducts";

            return await sqlDataAccess.QueryAsync<Product>(storedProcedure, parameters);
        }

        #endregion
    }
}
