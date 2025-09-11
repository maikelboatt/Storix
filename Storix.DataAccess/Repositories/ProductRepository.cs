using Storix.Application.DTO;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    /// <summary>
    ///     Repository implementation for Product operations using Dapper and stored procedures.
    /// </summary>
    /// <param name="sqlDataAccess" >The SQL data access instance.</param>
    public class ProductRepository( ISqlDataAccess sqlDataAccess ):IProductRepository
    {
        #region Pagination

        /// <summary>
        ///     Gets the total count of products.
        /// </summary>
        public async Task<int> GetTotalCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetProductCount");

        /// <summary>
        ///     Gets the count of active products.
        /// </summary>
        public async Task<int> GetActiveCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetActiveProductCount");

        /// <summary>
        ///     Gets a paged list of products.
        /// </summary>
        public async Task<IEnumerable<Product>> GetPagedAsync( int pageNumber, int pageSize )
        {
            var parameters = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize
            };

            return await sqlDataAccess.QueryAsync<Product>(
                "sp_GetProductsPaged",
                parameters);
        }

        #endregion

        #region Validation

        /// <summary>
        ///     Checks if a product exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int productId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckProductExists",
                new { ProductId = productId });

            return count > 0;
        }

        /// <summary>
        ///     Checks if a SKU already exists (optionally excluding a specific product ID).
        /// </summary>
        public async Task<bool> SkuExistsAsync( string sku, int? excludeProductId = null )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckSkuExists",
                new { SKU = sku, ExcludeProductId = excludeProductId });

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
                product.IsActive,
                product.CreatedDate
            };

            int newProductId = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CreateProduct",
                parameters);

            return product with { ProductId = newProductId };
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
                product.IsActive,
                UpdatedDate = DateTime.UtcNow
            };

            await sqlDataAccess.CommandAsync("sp_UpdateProduct", parameters);

            return product with { UpdatedDate = DateTime.UtcNow };
        }

        /// <summary>
        ///     Permanently deletes a product by ID.
        /// </summary>
        public async Task<bool> DeleteAsync( int productId )
        {
            try
            {
                await sqlDataAccess.CommandAsync("sp_DeleteProduct", new { ProductId = productId });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Soft deletes a product (sets IsActive = false).
        /// </summary>
        public async Task<bool> SoftDeleteAsync( int productId )
        {
            try
            {
                await sqlDataAccess.CommandAsync(
                    "sp_SoftDeleteProduct",
                    new { ProductId = productId, UpdatedDate = DateTime.UtcNow });
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
        public async Task<Product?> GetByIdAsync( int productId ) => await sqlDataAccess.QuerySingleOrDefaultAsync<Product>(
            "sp_GetProductById",
            new { ProductId = productId });

        /// <summary>
        ///     Gets a product by its SKU.
        /// </summary>
        public async Task<Product?> GetBySkuAsync( string sku ) => await sqlDataAccess.QuerySingleOrDefaultAsync<Product>(
            "sp_GetProductBySku",
            new { SKU = sku });

        /// <summary>
        ///     Gets all products.
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllAsync() => await sqlDataAccess.QueryAsync<Product>("sp_GetAllProducts");

        /// <summary>
        ///     Gets all active products.
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllActiveAsync() => await sqlDataAccess.QueryAsync<Product>("sp_GetAllActiveProducts");

        /// <summary>
        ///     Gets products by category ID.
        /// </summary>
        public async Task<IEnumerable<Product>> GetByCategoryAsync( int categoryId ) => await sqlDataAccess.QueryAsync<Product>(
            "sp_GetProductsByCategory",
            new { CategoryId = categoryId });

        /// <summary>
        ///     Gets products by supplier ID.
        /// </summary>
        public async Task<IEnumerable<Product>> GetBySupplierAsync( int supplierId ) => await sqlDataAccess.QueryAsync<Product>(
            "sp_GetProductsBySupplier",
            new { SupplierId = supplierId });

        /// <summary>
        ///     Gets products that are below their minimum stock level.
        /// </summary>
        public async Task<IEnumerable<Product>> GetLowStockProductsAsync() => await sqlDataAccess.QueryAsync<Product>("sp_GetLowStockProducts");

        /// <summary>
        ///     Gets products with extended details (joins supplier, category, stock info).
        ///     Returns a flattened DTO.
        /// </summary>
        public async Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync() => await sqlDataAccess.QueryAsync<ProductWithDetailsDto>("sp_GetProductsWithDetails");

        /// <summary>
        ///     Searches products by name, SKU, or description.
        /// </summary>
        public async Task<IEnumerable<Product>> SearchAsync( string searchTerm ) => await sqlDataAccess.QueryAsync<Product>(
            "sp_SearchProducts",
            new { SearchTerm = $"%{searchTerm}%" });

        #endregion
    }
}
