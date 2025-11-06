using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    /// <summary>
    /// Repository implementation for <see cref="Product"/> entity operations.
    /// 
    /// This class provides database access logic for reading, creating, updating,
    /// and deleting product records using <see cref="ISqlDataAccess"/> (Dapper abstraction).
    /// 
    /// The repository reflects the full database state — all queries include both active
    /// and soft-deleted records unless explicitly filtered by validation logic.
    /// Filtering for active-only records is handled in the service layer.
    /// </summary>
    public class ProductRepository:IProductRepository
    {
        private readonly ISqlDataAccess _sqlDataAccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductRepository"/> class.
        /// </summary>
        /// <param name="sqlDataAccess">The SQL data access abstraction for executing queries and commands.</param>
        public ProductRepository( ISqlDataAccess sqlDataAccess )
        {
            _sqlDataAccess = sqlDataAccess;
        }

        #region Validation

        /// <summary>
        /// Checks whether a product with the specified ID exists.
        /// Optionally includes soft-deleted records based on <paramref name="includeDeleted"/>.
        /// </summary>
        /// <param name="productId">The unique identifier of the product to check.</param>
        /// <param name="includeDeleted">Whether to include soft-deleted records in the check.</param>
        /// <returns><c>true</c> if the product exists; otherwise, <c>false</c>.</returns>
        public async Task<bool> ExistsAsync( int productId, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Product WHERE ProductId = @ProductId"
                : "SELECT COUNT(1) FROM Product WHERE ProductId = @ProductId AND IsDeleted = 0";

            return await _sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        /// Checks whether a SKU already exists in the database.
        /// Optionally excludes a specific product ID (for update operations) and
        /// includes soft-deleted records if requested.
        /// </summary>
        /// <param name="sku">The product SKU to check for uniqueness.</param>
        /// <param name="excludeProductId">An optional product ID to exclude from the check.</param>
        /// <param name="includeDeleted">Whether to include soft-deleted records in the check.</param>
        /// <returns><c>true</c> if the SKU exists; otherwise, <c>false</c>.</returns>
        public async Task<bool> SkuExistsAsync( string sku, int? excludeProductId = null, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? @"SELECT COUNT(1) FROM Product 
                    WHERE SKU = @SKU 
                    AND (@ExcludeProductId IS NULL OR ProductId != @ExcludeProductId)"
                : @"SELECT COUNT(1) FROM Product 
                    WHERE SKU = @SKU 
                    AND IsDeleted = 0 
                    AND (@ExcludeProductId IS NULL OR ProductId != @ExcludeProductId)";

            return await _sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    SKU = sku,
                    ExcludeProductId = excludeProductId
                });
        }

        #endregion

        #region Count Operations

        /// <summary>
        /// Retrieves the total number of product records (active + soft-deleted).
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Product";
            return await _sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        /// Retrieves the number of active (non-deleted) product records.
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Product WHERE IsDeleted = 0";
            return await _sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        /// Retrieves the number of soft-deleted product records.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Product WHERE IsDeleted = 1";
            return await _sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        #endregion

        #region Read Operations

        /// <summary>
        /// Retrieves a product by its unique identifier (includes both active and deleted).
        /// </summary>
        public async Task<Product?> GetByIdAsync( int productId )
        {
            // language=tsql
            const string sql = "SELECT * FROM Product WHERE ProductId = @ProductId";
            return await _sqlDataAccess.QuerySingleOrDefaultAsync<Product>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        /// Retrieves a product by its SKU (includes both active and deleted).
        /// </summary>
        public async Task<Product?> GetBySkuAsync( string sku )
        {
            // language=tsql
            const string sql = "SELECT * FROM Product WHERE SKU = @SKU";
            return await _sqlDataAccess.QuerySingleOrDefaultAsync<Product>(
                sql,
                new
                {
                    SKU = sku
                });
        }

        /// <summary>
        /// Retrieves all products (active and soft-deleted), ordered by name.
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Product ORDER BY Name";
            return await _sqlDataAccess.QueryAsync<Product>(sql);
        }

        /// <summary>
        /// Retrieves all active (non-deleted) products.
        /// This method is typically used to initialize the in-memory store or cache.
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllActiveAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Product WHERE IsDeleted = 0 ORDER BY Name";
            return await _sqlDataAccess.QueryAsync<Product>(sql);
        }

        /// <summary>
        /// Retrieves all soft-deleted products.
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllDeletedAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Product WHERE IsDeleted = 1 ORDER BY Name";
            return await _sqlDataAccess.QueryAsync<Product>(sql);
        }

        /// <summary>
        /// Retrieves products by their category ID (active + deleted).
        /// </summary>
        public async Task<IEnumerable<Product>> GetByCategoryAsync( int categoryId )
        {
            // language=tsql
            const string sql = "SELECT * FROM Product WHERE CategoryId = @CategoryId ORDER BY Name";
            return await _sqlDataAccess.QueryAsync<Product>(
                sql,
                new
                {
                    CategoryId = categoryId
                });
        }

        /// <summary>
        /// Retrieves products by their supplier ID (active + deleted).
        /// </summary>
        public async Task<IEnumerable<Product>> GetBySupplierAsync( int supplierId )
        {
            // language=tsql
            const string sql = "SELECT * FROM Product WHERE SupplierId = @SupplierId ORDER BY Name";
            return await _sqlDataAccess.QueryAsync<Product>(
                sql,
                new
                {
                    SupplierId = supplierId
                });
        }

        /// <summary>
        /// Retrieves all active products with stock below the minimum threshold.
        /// Uses ISNULL for SQL Server NULL handling.
        /// </summary>
        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            // language=tsql
            const string sql = @"
                SELECT p.* 
                FROM Product p
                LEFT JOIN (
                    SELECT ProductId, SUM(CurrentStock) AS TotalStock
                    FROM Inventory
                    GROUP BY ProductId
                ) s ON p.ProductId = s.ProductId
                WHERE p.IsDeleted = 0
                AND ISNULL(s.TotalStock, 0) < p.MinStockLevel
                ORDER BY p.Name";

            return await _sqlDataAccess.QueryAsync<Product>(sql);
        }

        /// <summary>
        /// Retrieves products along with category, supplier, and stock details.
        /// Uses ISNULL for SQL Server NULL handling.
        /// </summary>
        public async Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync()
        {
            // language=tsql
            const string sql = @"
                SELECT 
                    p.*,
                    c.Name AS CategoryName,
                    s.Name AS SupplierName,
                    ISNULL(st.TotalStock, 0) AS CurrentStock
                FROM Product p
                LEFT JOIN Category c ON p.CategoryId = c.CategoryId
                LEFT JOIN Supplier s ON p.SupplierId = s.SupplierId
                LEFT JOIN (
                    SELECT ProductId, SUM(CurrentStock) AS TotalStock
                    FROM Inventory
                    GROUP BY ProductId
                ) st ON p.ProductId = st.ProductId
                ORDER BY p.Name";

            return await _sqlDataAccess.QueryAsync<ProductWithDetailsDto>(sql);
        }

        /// <summary>
        /// Retrieves a paginated list of products.
        /// Uses SQL Server OFFSET-FETCH syntax.
        /// </summary>
        public async Task<IEnumerable<Product>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Product 
                ORDER BY Name 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await _sqlDataAccess.QueryAsync<Product>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        /// <summary>
        /// Searches for products by name, SKU, or description (active + deleted).
        /// </summary>
        public async Task<IEnumerable<Product>> SearchAsync( string searchTerm )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Product 
                WHERE Name LIKE @SearchTerm 
                      OR SKU LIKE @SearchTerm 
                      OR Description LIKE @SearchTerm 
                ORDER BY Name";

            return await _sqlDataAccess.QueryAsync<Product>(
                sql,
                new
                {
                    SearchTerm = $"%{searchTerm}%"
                });
        }

        /// <summary>
        /// Retrieves the top best-selling products based on total units sold within a date range.
        /// Includes only completed orders.
        /// Products are ordered by units sold descending.
        /// </summary>
        public async Task<IEnumerable<TopProductDto>> GetTopBestSellersAsync( int topCount = 5, int monthsBack = 3 )
        {
            // language=tsql
            const string sql = @"
        SELECT TOP (@TopCount)
            p.ProductId,
            p.Name AS ProductName,
            p.SKU,
            ISNULL(SUM(oi.Quantity), 0) AS UnitsSold,
            ISNULL(SUM(oi.Quantity * oi.UnitPrice), 0) AS TotalRevenue
        FROM Product p
        INNER JOIN OrderItem oi ON p.ProductId = oi.ProductId
        INNER JOIN [Order] o ON oi.OrderId = o.OrderId
        WHERE p.IsDeleted = 0
            AND o.OrderDate >= DATEADD(MONTH, -@MonthsBack, GETDATE())
        GROUP BY p.ProductId, p.Name, p.SKU
        ORDER BY UnitsSold DESC";

            return await _sqlDataAccess.QueryAsync<TopProductDto>(
                sql,
                new
                {
                    TopCount = topCount,
                    MonthsBack = monthsBack,
                    CompletedStatus = (int)OrderStatus.Completed
                });
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Creates a new product record and returns the created entity.
        /// Uses SQL Server SCOPE_IDENTITY() to retrieve the newly inserted ID.
        /// </summary>
        public async Task<Product> CreateAsync( Product product )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Product (
                    Name, SKU, Description, Barcode, Price, Cost, 
                    MinStockLevel, MaxStockLevel, SupplierId, CategoryId, 
                    CreatedDate, IsDeleted, DeletedAt
                )
                VALUES (
                    @Name, @SKU, @Description, @Barcode, @Price, @Cost,
                    @MinStockLevel, @MaxStockLevel, @SupplierId, @CategoryId,
                    @CreatedDate, 0, NULL
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newProductId = await _sqlDataAccess.ExecuteScalarAsync<int>(sql, product);
            return product with
            {
                ProductId = newProductId,
                IsDeleted = false,
                DeletedAt = null
            };
        }

        /// <summary>
        /// Updates an existing product record.
        /// </summary>
        public async Task<Product> UpdateAsync( Product product )
        {
            // language=tsql
            const string sql = @"
                UPDATE Product 
                SET Name = @Name,
                    SKU = @SKU,
                    Description = @Description,
                    Barcode = @Barcode,
                    Price = @Price,
                    Cost = @Cost,
                    MinStockLevel = @MinStockLevel,
                    MaxStockLevel = @MaxStockLevel,
                    SupplierId = @SupplierId,
                    CategoryId = @CategoryId,
                    UpdatedDate = @UpdatedDate,
                    IsDeleted = @IsDeleted,
                    DeletedAt = @DeletedAt
                WHERE ProductId = @ProductId";

            Product updated = product with
            {
                UpdatedDate = DateTime.UtcNow
            };
            await _sqlDataAccess.ExecuteAsync(sql, updated);
            return updated;
        }

        /// <summary>
        /// Soft deletes a product by marking it as deleted instead of removing it permanently.
        /// </summary>
        public async Task<DatabaseResult> SoftDeleteAsync( int productId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Product 
                    SET IsDeleted = 1, 
                        DeletedAt = @DeletedAt,
                        UpdatedDate = @UpdatedDate
                    WHERE ProductId = @ProductId AND IsDeleted = 0";

                int affected = await _sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        ProductId = productId,
                        DeletedAt = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    });

                return affected > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure($"Product with ID {productId} not found or already deleted", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure($"Error soft deleting product {productId}: {ex.Message}", DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        /// Restores a soft-deleted product record by marking it as active again.
        /// </summary>
        public async Task<DatabaseResult> RestoreAsync( int productId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Product 
                    SET IsDeleted = 0, 
                        DeletedAt = NULL,
                        UpdatedDate = @UpdatedDate
                    WHERE ProductId = @ProductId AND IsDeleted = 1";

                int affected = await _sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        ProductId = productId,
                        UpdatedDate = DateTime.UtcNow
                    });

                return affected > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure($"Product with ID {productId} cannot be restored", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure($"Error restoring product {productId}: {ex.Message}", DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        /// Permanently deletes a product record from the database.
        /// </summary>
        public async Task<DatabaseResult> HardDeleteAsync( int productId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Product WHERE ProductId = @ProductId";
                int affected = await _sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        ProductId = productId
                    });

                return affected > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure($"Product with ID {productId} not found", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure($"Error permanently deleting product {productId}: {ex.Message}", DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
