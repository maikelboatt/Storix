using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Storix.Application.Common;
using Storix.Application.DataAccess;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class InventoryRepository( ISqlDataAccess sqlDataAccess ):IInventoryRepository
    {
        #region Validation

        /// <summary>
        ///     Check if an inventory record exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int inventoryId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM Inventory WHERE InventoryId = @InventoryId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    InventoryId = inventoryId
                });
        }

        /// <summary>
        ///     Check if inventory exists for a product at a specific location.
        /// </summary>
        public async Task<bool> ExistsByProductAndLocationAsync( int productId, int locationId )
        {
            // language=tsql
            const string sql = @"
                SELECT COUNT(1) 
                FROM Inventory 
                WHERE ProductId = @ProductId AND LocationId = @LocationId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    ProductId = productId,
                    LocationId = locationId
                });
        }

        #endregion

        #region Count Operations

        /// <summary>
        ///     Gets the total count of inventory records.
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Inventory";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of low stock items (where CurrentStock <= threshold).
        /// </summary>
        public async Task<int> GetLowStockCountAsync( int threshold = 10 )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Inventory WHERE CurrentStock <= @Threshold";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Threshold = threshold
                });
        }

        /// <summary>
        ///     Gets the count of out-of-stock items.
        /// </summary>
        public async Task<int> GetOutOfStockCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Inventory WHERE CurrentStock = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets an inventory record by ID.
        /// </summary>
        public async Task<Inventory?> GetByIdAsync( int inventoryId )
        {
            // language=tsql
            const string sql = "SELECT * FROM Inventory WHERE InventoryId = @InventoryId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Inventory>(
                sql,
                new
                {
                    InventoryId = inventoryId
                });
        }

        /// <summary>
        ///     Gets inventory for a specific product at a specific location.
        /// </summary>
        public async Task<Inventory?> GetByProductAndLocationAsync( int productId, int locationId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Inventory 
                WHERE ProductId = @ProductId AND LocationId = @LocationId";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Inventory>(
                sql,
                new
                {
                    ProductId = productId,
                    LocationId = locationId
                });
        }

        /// <summary>
        ///     Gets all inventory records for a specific product across all locations.
        /// </summary>
        public async Task<IEnumerable<Inventory>> GetByProductIdAsync( int productId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Inventory 
                WHERE ProductId = @ProductId 
                ORDER BY LocationId";

            return await sqlDataAccess.QueryAsync<Inventory>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        ///     Gets all inventory records for a specific location.
        /// </summary>
        public async Task<IEnumerable<Inventory>> GetByLocationIdAsync( int locationId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Inventory 
                WHERE LocationId = @LocationId 
                ORDER BY ProductId";

            return await sqlDataAccess.QueryAsync<Inventory>(
                sql,
                new
                {
                    LocationId = locationId
                });
        }

        /// <summary>
        ///     Gets all inventory records.
        /// </summary>
        public async Task<IEnumerable<Inventory>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Inventory ORDER BY ProductId, LocationId";
            return await sqlDataAccess.QueryAsync<Inventory>(sql);
        }

        /// <summary>
        ///     Gets low stock items (where CurrentStock <= threshold).
        /// </summary>
        public async Task<IEnumerable<Inventory>> GetLowStockItemsAsync( int threshold = 10 )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Inventory 
                WHERE CurrentStock <= @Threshold 
                ORDER BY CurrentStock";

            return await sqlDataAccess.QueryAsync<Inventory>(
                sql,
                new
                {
                    Threshold = threshold
                });
        }

        /// <summary>
        ///     Gets out-of-stock items.
        /// </summary>
        public async Task<IEnumerable<Inventory>> GetOutOfStockItemsAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Inventory WHERE CurrentStock = 0 ORDER BY ProductId";
            return await sqlDataAccess.QueryAsync<Inventory>(sql);
        }

        /// <summary>
        ///     Gets a paged list of inventory records.
        /// </summary>
        public async Task<IEnumerable<Inventory>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Inventory 
                ORDER BY ProductId, LocationId 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<Inventory>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        #endregion

        #region Search & Filter

        /// <summary>
        ///     Searches inventory with optional filters.
        /// </summary>
        public async Task<IEnumerable<Inventory>> SearchAsync(
            int? productId = null,
            int? locationId = null,
            int? minStock = null,
            int? maxStock = null )
        {
            StringBuilder sql = new("SELECT * FROM Inventory WHERE 1=1");
            DynamicParameters parameters = new();

            if (productId.HasValue)
            {
                sql.Append(" AND ProductId = @ProductId");
                parameters.Add("ProductId", productId.Value);
            }

            if (locationId.HasValue)
            {
                sql.Append(" AND LocationId = @LocationId");
                parameters.Add("LocationId", locationId.Value);
            }

            if (minStock.HasValue)
            {
                sql.Append(" AND CurrentStock >= @MinStock");
                parameters.Add("MinStock", minStock.Value);
            }

            if (maxStock.HasValue)
            {
                sql.Append(" AND CurrentStock <= @MaxStock");
                parameters.Add("MaxStock", maxStock.Value);
            }

            sql.Append(" ORDER BY ProductId, LocationId");

            return await sqlDataAccess.QueryAsync<Inventory>(sql.ToString(), parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new inventory record and returns it with its generated ID.
        /// </summary>
        public async Task<Inventory> CreateAsync( Inventory inventory )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Inventory (
                    ProductId, LocationId, CurrentStock, ReservedStock, LastUpdated
                )
                VALUES (
                    @ProductId, @LocationId, @CurrentStock, @ReservedStock, @LastUpdated
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int inventoryId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, inventory);

            return inventory with
            {
                InventoryId = inventoryId
            };
        }

        /// <summary>
        ///     Updates an existing inventory record.
        /// </summary>
        public async Task<Inventory> UpdateAsync( Inventory inventory )
        {
            // language=tsql
            const string sql = @"
                UPDATE Inventory 
                SET CurrentStock = @CurrentStock,
                    ReservedStock = @ReservedStock,
                    LastUpdated = @LastUpdated
                WHERE InventoryId = @InventoryId";

            await sqlDataAccess.ExecuteAsync(sql, inventory);
            return inventory;
        }

        /// <summary>
        ///     Adjusts stock level for a specific inventory record.
        /// </summary>
        public async Task<DatabaseResult> AdjustStockAsync( int inventoryId, int quantityChange )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Inventory 
                    SET CurrentStock = CurrentStock + @QuantityChange,
                        LastUpdated = @LastUpdated
                    WHERE InventoryId = @InventoryId";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        InventoryId = inventoryId,
                        QuantityChange = quantityChange,
                        LastUpdated = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Inventory with ID {inventoryId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error adjusting stock: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Reserves stock for an inventory record.
        /// </summary>
        public async Task<DatabaseResult> ReserveStockAsync( int inventoryId, int quantity )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Inventory 
                    SET ReservedStock = ReservedStock + @Quantity,
                        CurrentStock = CurrentStock - @Quantity,
                        LastUpdated = @LastUpdated
                    WHERE InventoryId = @InventoryId 
                    AND (CurrentStock - ReservedStock) >= @Quantity";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        InventoryId = inventoryId,
                        Quantity = quantity,
                        LastUpdated = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Insufficient available stock for inventory ID {inventoryId}",
                        DatabaseErrorCode.ConstraintViolation);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error reserving stock: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Releases reserved stock.
        /// </summary>
        public async Task<DatabaseResult> ReleaseReservedStockAsync( int inventoryId, int quantity )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Inventory 
                    SET ReservedStock = ReservedStock - @Quantity,
                        CurrentStock = CurrentStock + @Quantity,
                        LastUpdated = @LastUpdated
                    WHERE InventoryId = @InventoryId 
                    AND ReservedStock >= @Quantity";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        InventoryId = inventoryId,
                        Quantity = quantity,
                        LastUpdated = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Insufficient reserved stock for inventory ID {inventoryId}",
                        DatabaseErrorCode.ConstraintViolation);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error releasing reserved stock: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Permanently deletes an inventory record by ID.
        ///     WARNING: This permanently removes the inventory record from the database.
        /// </summary>
        public async Task<DatabaseResult> DeleteAsync( int inventoryId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Inventory WHERE InventoryId = @InventoryId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        InventoryId = inventoryId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Inventory with ID {inventoryId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting inventory: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
