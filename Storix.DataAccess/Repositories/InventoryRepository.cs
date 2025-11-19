// using System;
// using System.Collections.Generic;
// using System.Text;
// using System.Threading.Tasks;
// using Dapper;
// using Storix.Application.Common;
// using Storix.Application.DTO.Inventory;
// using Storix.Application.Enums;
// using Storix.Application.Repositories;
// using Storix.DataAccess.DBAccess;
// using Storix.Domain.Models;
//
// namespace Storix.DataAccess.Repositories
// {
//     /// <summary>
//     ///     Repository for managing inventory stock levels at locations.
//     /// </summary>
//     public class InventoryRepository( ISqlDataAccess sqlDataAccess )
//     {
//         #region Validation
//
//         /// <summary>
//         ///     Check if an inventory record exists by ID.
//         /// </summary>
//         public async Task<bool> ExistsAsync( int inventoryId )
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(1) FROM Inventory WHERE InventoryId = @InventoryId";
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     InventoryId = inventoryId
//                 });
//         }
//
//         /// <summary>
//         ///     Check if an inventory record exists for a product at a specific location.
//         /// </summary>
//         public async Task<bool> ExistsForProductAndLocationAsync( int productId, int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT COUNT(1) FROM Inventory 
//                 WHERE ProductId = @ProductId AND LocationId = @LocationId";
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId,
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Check if there is sufficient available stock for a product at a location.
//         /// </summary>
//         public async Task<bool> HasSufficientStockAsync( int productId, int locationId, int requiredQuantity )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT CASE WHEN (CurrentStock - ReservedStock) >= @RequiredQuantity 
//                        THEN 1 ELSE 0 END
//                 FROM Inventory 
//                 WHERE ProductId = @ProductId AND LocationId = @LocationId";
//
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId,
//                     LocationId = locationId,
//                     RequiredQuantity = requiredQuantity
//                 });
//         }
//
//         /// <summary>
//         ///     Checks if a product has any inventory records across all locations.
//         /// </summary>
//         public async Task<bool> ProductHasInventoryAsync( int productId )
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(1) FROM Inventory WHERE ProductId = @ProductId";
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Checks if a location has any inventory records.
//         /// </summary>
//         public async Task<bool> LocationHasInventoryAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(1) FROM Inventory WHERE LocationId = @LocationId";
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         #endregion
//
//         #region Count Operations
//
//         /// <summary>
//         ///     Gets the total count of inventory records.
//         /// </summary>
//         public async Task<int> GetTotalCountAsync()
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(*) FROM Inventory";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
//         }
//
//         /// <summary>
//         ///     Gets the count of products that are out of stock.
//         /// </summary>
//         public async Task<int> GetOutOfStockCountAsync()
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT COUNT(*) FROM Inventory 
//                 WHERE (CurrentStock - ReservedStock) <= 0";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
//         }
//
//         /// <summary>
//         ///     Gets the count of products with low stock at a location.
//         /// </summary>
//         public async Task<int> GetLowStockCountAsync( int threshold )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT COUNT(*) FROM Inventory 
//                 WHERE (CurrentStock - ReservedStock) > 0 
//                 AND (CurrentStock - ReservedStock) <= @Threshold";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(
//                 sql,
//                 new
//                 {
//                     Threshold = threshold
//                 });
//         }
//
//         #endregion
//
//         #region Read Operations
//
//         /// <summary>
//         ///     Gets an inventory record by its ID.
//         /// </summary>
//         public async Task<Inventory?> GetByIdAsync( int inventoryId )
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM Inventory WHERE InventoryId = @InventoryId";
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<Inventory>(
//                 sql,
//                 new
//                 {
//                     InventoryId = inventoryId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets all inventory records.
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetAllCustomersAsync()
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM Inventory ORDER BY ProductId, LocationId";
//             return await sqlDataAccess.QueryAsync<Inventory>(sql);
//         }
//
//         /// <summary>
//         ///     Gets inventory for a specific product across all locations.
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 WHERE ProductId = @ProductId 
//                 ORDER BY LocationId";
//             return await sqlDataAccess.QueryAsync<Inventory>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory for a specific location across all products.
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetByLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 WHERE LocationId = @LocationId 
//                 ORDER BY ProductId";
//             return await sqlDataAccess.QueryAsync<Inventory>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory for a specific product at a specific location.
//         /// </summary>
//         public async Task<Inventory?> GetByProductAndLocationAsync( int productId, int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 WHERE ProductId = @ProductId AND LocationId = @LocationId";
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<Inventory>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId,
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets all products that are out of stock (available stock <= 0).
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetOutOfStockAsync()
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 WHERE (CurrentStock - ReservedStock) <= 0
//                 ORDER BY ProductId, LocationId";
//             return await sqlDataAccess.QueryAsync<Inventory>(sql);
//         }
//
//         /// <summary>
//         ///     Gets all products with low stock (available stock > 0 but <= threshold).
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetLowStockAsync( int threshold )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 WHERE (CurrentStock - ReservedStock) > 0 
//                 AND (CurrentStock - ReservedStock) <= @Threshold
//                 ORDER BY (CurrentStock - ReservedStock), ProductId";
//             return await sqlDataAccess.QueryAsync<Inventory>(
//                 sql,
//                 new
//                 {
//                     Threshold = threshold
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory records with reserved stock.
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetWithReservedStockAsync()
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 WHERE ReservedStock > 0
//                 ORDER BY ReservedStock DESC, ProductId";
//             return await sqlDataAccess.QueryAsync<Inventory>(sql);
//         }
//
//         /// <summary>
//         ///     Gets total stock for a product across all locations.
//         /// </summary>
//         public async Task<int> GetTotalStockByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT ISNULL(SUM(CurrentStock), 0) 
//                 FROM Inventory 
//                 WHERE ProductId = @ProductId";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets total available stock for a product across all locations.
//         /// </summary>
//         public async Task<int> GetTotalAvailableStockByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT ISNULL(SUM(CurrentStock - ReservedStock), 0) 
//                 FROM Inventory 
//                 WHERE ProductId = @ProductId";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets a paged list of inventory records.
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> GetPagedAsync( int pageNumber, int pageSize )
//         {
//             int offset = (pageNumber - 1) * pageSize;
//
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM Inventory 
//                 ORDER BY ProductId, LocationId 
//                 OFFSET @Offset ROWS
//                 FETCH NEXT @PageSize ROWS ONLY";
//
//             return await sqlDataAccess.QueryAsync<Inventory>(
//                 sql,
//                 new
//                 {
//                     PageSize = pageSize,
//                     Offset = offset
//                 });
//         }
//
//         #endregion
//
//         #region Search & Filter
//
//         /// <summary>
//         ///     Searches inventory with multiple optional filters.
//         /// </summary>
//         public async Task<IEnumerable<Inventory>> SearchAsync(
//             int? productId = null,
//             int? locationId = null,
//             bool? inStockOnly = null,
//             int? minStock = null,
//             int? maxStock = null )
//         {
//             StringBuilder sql = new("SELECT * FROM Inventory WHERE 1=1");
//             DynamicParameters parameters = new();
//
//             if (productId.HasValue)
//             {
//                 sql.Append(" AND ProductId = @ProductId");
//                 parameters.Add("ProductId", productId.Value);
//             }
//
//             if (locationId.HasValue)
//             {
//                 sql.Append(" AND LocationId = @LocationId");
//                 parameters.Add("LocationId", locationId.Value);
//             }
//
//             if (inStockOnly.HasValue && inStockOnly.Value)
//             {
//                 sql.Append(" AND (CurrentStock - ReservedStock) > 0");
//             }
//
//             if (minStock.HasValue)
//             {
//                 sql.Append(" AND (CurrentStock - ReservedStock) >= @MinStock");
//                 parameters.Add("MinStock", minStock.Value);
//             }
//
//             if (maxStock.HasValue)
//             {
//                 sql.Append(" AND (CurrentStock - ReservedStock) <= @MaxStock");
//                 parameters.Add("MaxStock", maxStock.Value);
//             }
//
//             sql.Append(" ORDER BY ProductId, LocationId");
//
//             return await sqlDataAccess.QueryAsync<Inventory>(sql.ToString(), parameters);
//         }
//
//         #endregion
//
//         #region Statistics & Reporting
//
//         /// <summary>
//         ///     Gets inventory statistics for a specific location.
//         /// </summary>
//         public async Task<InventoryStatisticsDto?> GetStatisticsByLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT 
//                     COUNT(*) as TotalProducts,
//                     SUM(CurrentStock) as TotalStock,
//                     SUM(CurrentStock - ReservedStock) as TotalAvailableStock,
//                     SUM(ReservedStock) as TotalReservedStock,
//                     COUNT(CASE WHEN (CurrentStock - ReservedStock) <= 0 THEN 1 END) as OutOfStockCount,
//                     AVG(CAST(CurrentStock as DECIMAL(10,2))) as AverageStock
//                 FROM Inventory
//                 WHERE LocationId = @LocationId";
//
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<InventoryStatisticsDto>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets overall inventory statistics across all locations.
//         /// </summary>
//         public async Task<InventoryStatisticsDto?> GetOverallStatisticsAsync()
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT 
//                     COUNT(*) as TotalProducts,
//                     SUM(CurrentStock) as TotalStock,
//                     SUM(CurrentStock - ReservedStock) as TotalAvailableStock,
//                     SUM(ReservedStock) as TotalReservedStock,
//                     COUNT(CASE WHEN (CurrentStock - ReservedStock) <= 0 THEN 1 END) as OutOfStockCount,
//                     AVG(CAST(CurrentStock as DECIMAL(10,2))) as AverageStock
//                 FROM Inventory";
//
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<InventoryStatisticsDto>(sql);
//         }
//
//         /// <summary>
//         ///     Gets inventory value at a location (requires unit cost from product table).
//         /// </summary>
//         public async Task<decimal> GetTotalValueByLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT ISNULL(SUM(i.CurrentStock * p.UnitCost), 0)
//                 FROM Inventory i
//                 INNER JOIN Product p ON i.ProductId = p.ProductId
//                 WHERE i.LocationId = @LocationId";
//
//             return await sqlDataAccess.ExecuteScalarAsync<decimal>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         #endregion
//
//         #region Write Operations
//
//         /// <summary>
//         ///     Creates a new inventory record and returns it with its generated ID.
//         /// </summary>
//         public async Task<Inventory> CreateAsync( Inventory inventory )
//         {
//             // language=tsql
//             const string sql = @"
//                 INSERT INTO Inventory (
//                     ProductId, LocationId, CurrentStock, ReservedStock, LastUpdated
//                 )
//                 VALUES (
//                     @ProductId, @LocationId, @CurrentStock, @ReservedStock, @LastUpdated
//                 );
//                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
//
//             int inventoryId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, inventory);
//
//             return inventory with
//             {
//                 InventoryId = inventoryId
//             };
//         }
//
//         /// <summary>
//         ///     Updates an existing inventory record.
//         /// </summary>
//         public async Task<Inventory> UpdateAsync( Inventory inventory )
//         {
//             // language=tsql
//             const string sql = @"
//                 UPDATE Inventory 
//                 SET CurrentStock = @CurrentStock,
//                     ReservedStock = @ReservedStock,
//                     LastUpdated = @LastUpdated
//                 WHERE InventoryId = @InventoryId";
//
//             await sqlDataAccess.ExecuteAsync(sql, inventory);
//             return inventory;
//         }
//
//         /// <summary>
//         ///     Adjusts stock for a product at a location (adds or removes stock).
//         /// </summary>
//         public async Task<DatabaseResult> AdjustStockAsync(
//             int productId,
//             int locationId,
//             int quantityChange )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = @"
//                     UPDATE Inventory 
//                     SET CurrentStock = CurrentStock + @QuantityChange,
//                         LastUpdated = @LastUpdated
//                     WHERE ProductId = @ProductId AND LocationId = @LocationId";
//
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         ProductId = productId,
//                         LocationId = locationId,
//                         QuantityChange = quantityChange,
//                         LastUpdated = DateTime.UtcNow
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Inventory for Product {productId} at Location {locationId} not found",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error adjusting stock: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Reserves stock for a product at a location.
//         /// </summary>
//         public async Task<DatabaseResult> ReserveStockAsync(
//             int productId,
//             int locationId,
//             int quantity )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = @"
//                     UPDATE Inventory 
//                     SET ReservedStock = ReservedStock + @Quantity,
//                         LastUpdated = @LastUpdated
//                     WHERE ProductId = @ProductId 
//                     AND LocationId = @LocationId
//                     AND (CurrentStock - ReservedStock) >= @Quantity";
//
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         ProductId = productId,
//                         LocationId = locationId,
//                         Quantity = quantity,
//                         LastUpdated = DateTime.UtcNow
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Insufficient available stock for Product {productId} at Location {locationId}",
//                         DatabaseErrorCode.InvalidInput);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error reserving stock: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Releases reserved stock for a product at a location.
//         /// </summary>
//         public async Task<DatabaseResult> ReleaseReservedStockAsync(
//             int productId,
//             int locationId,
//             int quantity )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = @"
//                     UPDATE Inventory 
//                     SET ReservedStock = ReservedStock - @Quantity,
//                         LastUpdated = @LastUpdated
//                     WHERE ProductId = @ProductId 
//                     AND LocationId = @LocationId
//                     AND ReservedStock >= @Quantity";
//
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         ProductId = productId,
//                         LocationId = locationId,
//                         Quantity = quantity,
//                         LastUpdated = DateTime.UtcNow
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Insufficient reserved stock for Product {productId} at Location {locationId}",
//                         DatabaseErrorCode.InvalidInput);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error releasing reserved stock: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Sets the stock level for a product at a location to a specific value.
//         /// </summary>
//         public async Task<DatabaseResult> SetStockLevelAsync(
//             int productId,
//             int locationId,
//             int newStockLevel )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = @"
//                     UPDATE Inventory 
//                     SET CurrentStock = @NewStockLevel,
//                         LastUpdated = @LastUpdated
//                     WHERE ProductId = @ProductId AND LocationId = @LocationId";
//
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         ProductId = productId,
//                         LocationId = locationId,
//                         NewStockLevel = newStockLevel,
//                         LastUpdated = DateTime.UtcNow
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Inventory for Product {productId} at Location {locationId} not found",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error setting stock level: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         #endregion
//
//         #region Delete Operations
//
//         /// <summary>
//         ///     Permanently deletes an inventory record by ID.
//         ///     WARNING: This will delete inventory data. Use with caution.
//         /// </summary>
//         public async Task<DatabaseResult> DeleteAsync( int inventoryId )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = "DELETE FROM Inventory WHERE InventoryId = @InventoryId";
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         InventoryId = inventoryId
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Inventory with ID {inventoryId} not found",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error deleting inventory: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Permanently deletes all inventory records for a product across all locations.
//         ///     WARNING: This will delete inventory data. Use with caution.
//         /// </summary>
//         public async Task<DatabaseResult> DeleteByProductAsync( int productId )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = "DELETE FROM Inventory WHERE ProductId = @ProductId";
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         ProductId = productId
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"No inventory found for Product {productId}",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error deleting inventory by product: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         #endregion
//     }
// }



