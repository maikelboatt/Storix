// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Dapper;
// using Storix.Application.Common;
// using Storix.Application.DTO.Inventory;
// using Storix.Application.Enums;
// using Storix.Application.Repositories;
// using Storix.DataAccess.DBAccess;
// using Storix.Domain.Enums;
// using Storix.Domain.Models;
//
// namespace Storix.DataAccess.Repositories
// {
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
//         #region Read Operations - Inventory
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
//         #endregion
//
//         #region Read Operations - Inventory Movements
//
//         /// <summary>
//         ///     Gets an inventory movement by its ID.
//         /// </summary>
//         public async Task<InventoryMovement?> GetMovementByIdAsync( int movementId )
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM InventoryMovement WHERE MovementId = @MovementId";
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     MovementId = movementId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets all inventory movements.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetAllMovementsAsync()
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM InventoryMovement ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(sql);
//         }
//
//         /// <summary>
//         ///     Gets inventory movements for a specific product.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetMovementsByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 WHERE ProductId = @ProductId 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory movements from a specific location.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetMovementsByFromLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 WHERE FromLocationId = @LocationId 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory movements to a specific location.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetMovementsByToLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 WHERE ToLocationId = @LocationId 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory movements by date range.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetMovementsByDateRangeAsync(
//             DateTime startDate,
//             DateTime endDate )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 WHERE CreatedDate BETWEEN @StartDate AND @EndDate 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     StartDate = startDate,
//                     EndDate = endDate
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory movements created by a specific user.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetMovementsByCreatedByAsync( int createdBy )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 WHERE CreatedBy = @CreatedBy 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     CreatedBy = createdBy
//                 });
//         }
//
//         #endregion
//
//         #region Read Operations - Inventory Transactions
//
//         /// <summary>
//         ///     Gets an inventory transaction by its ID.
//         /// </summary>
//         public async Task<InventoryTransaction?> GetTransactionByIdAsync( int transactionId )
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM InventoryTransaction WHERE TransactionId = @TransactionId";
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     TransactionId = transactionId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets all inventory transactions.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetAllTransactionsAsync()
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM InventoryTransaction ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(sql);
//         }
//
//         /// <summary>
//         ///     Gets inventory transactions for a specific product.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryTransaction 
//                 WHERE ProductId = @ProductId 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory transactions for a specific location.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryTransaction 
//                 WHERE LocationId = @LocationId 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory transactions by type.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByTypeAsync( TransactionType type )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryTransaction 
//                 WHERE Type = @Type 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     Type = (int)type
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory transactions by date range.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByDateRangeAsync(
//             DateTime startDate,
//             DateTime endDate )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryTransaction 
//                 WHERE CreatedDate BETWEEN @StartDate AND @EndDate 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     StartDate = startDate,
//                     EndDate = endDate
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory transactions by reference (e.g., order number).
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByReferenceAsync( string reference )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryTransaction 
//                 WHERE Reference = @Reference 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     Reference = reference
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory transactions created by a specific user.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> GetTransactionsByCreatedByAsync( int createdBy )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryTransaction 
//                 WHERE CreatedBy = @CreatedBy 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(
//                 sql,
//                 new
//                 {
//                     CreatedBy = createdBy
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
//         public async Task<IEnumerable<Inventory>> SearchInventoryAsync(
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
//         /// <summary>
//         ///     Searches inventory transactions with multiple optional filters.
//         /// </summary>
//         public async Task<IEnumerable<InventoryTransaction>> SearchTransactionsAsync(
//             int? productId = null,
//             int? locationId = null,
//             TransactionType? type = null,
//             string? reference = null,
//             DateTime? startDate = null,
//             DateTime? endDate = null )
//         {
//             StringBuilder sql = new("SELECT * FROM InventoryTransaction WHERE 1=1");
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
//             if (type.HasValue)
//             {
//                 sql.Append(" AND Type = @Type");
//                 parameters.Add("Type", (int)type.Value);
//             }
//
//             if (!string.IsNullOrWhiteSpace(reference))
//             {
//                 sql.Append(" AND Reference = @Reference");
//                 parameters.Add("Reference", reference);
//             }
//
//             if (startDate.HasValue)
//             {
//                 sql.Append(" AND CreatedDate >= @StartDate");
//                 parameters.Add("StartDate", startDate.Value);
//             }
//
//             if (endDate.HasValue)
//             {
//                 sql.Append(" AND CreatedDate <= @EndDate");
//                 parameters.Add("EndDate", endDate.Value);
//             }
//
//             sql.Append(" ORDER BY CreatedDate DESC");
//
//             return await sqlDataAccess.QueryAsync<InventoryTransaction>(sql.ToString(), parameters);
//         }
//
//         #endregion
//
//         #region Statistics & Reporting
//
//         /// <summary>
//         ///     Gets inventory statistics for a specific location.
//         /// </summary>
//         public async Task<InventoryStatisticsDto?> GetInventoryStatisticsByLocationAsync( int locationId )
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
//         ///     Gets overall inventory statistics.
//         /// </summary>
//         public async Task<InventoryStatisticsDto?> GetOverallInventoryStatisticsAsync()
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
//         ///     Gets transaction statistics by type for a date range.
//         /// </summary>
//         public async Task<TransactionStatisticsDto?> GetTransactionStatisticsAsync(
//             DateTime startDate,
//             DateTime endDate )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT 
//                     COUNT(*) as TotalTransactions,
//                     COUNT(CASE WHEN Type = @StockIn THEN 1 END) as StockInCount,
//                     COUNT(CASE WHEN Type = @StockOut THEN 1 END) as StockOutCount,
//                     COUNT(CASE WHEN Type = @Adjustment THEN 1 END) as AdjustmentCount,
//                     COUNT(CASE WHEN Type = @Transfer THEN 1 END) as TransferCount,
//                     SUM(CASE WHEN Type = @StockIn THEN Quantity ELSE 0 END) as TotalStockIn,
//                     SUM(CASE WHEN Type = @StockOut THEN Quantity ELSE 0 END) as TotalStockOut,
//                     SUM(CASE WHEN Type = @StockIn AND UnitCost IS NOT NULL THEN Quantity * UnitCost ELSE 0 END) as TotalValue
//                 FROM InventoryTransaction
//                 WHERE CreatedDate BETWEEN @StartDate AND @EndDate";
//
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<TransactionStatisticsDto>(
//                 sql,
//                 new
//                 {
//                     StartDate = startDate,
//                     EndDate = endDate,
//                     StockIn = (int)TransactionType.StockIn,
//                     StockOut = (int)TransactionType.StockOut,
//                     Adjustment = (int)TransactionType.Adjustment,
//                     Transfer = (int)TransactionType.Transfer
//                 });
//         }
//
//         #endregion
//
//         #region Write Operations - Inventory
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
//         #endregion
//
//         #region Write Operations - Inventory Movements
//
//         /// <summary>
//         ///     Creates a new inventory movement record and returns it with its generated ID.
//         /// </summary>
//         public async Task<InventoryMovement> CreateMovementAsync( InventoryMovement movement )
//         {
//             // language=tsql
//             const string sql = @"
//                 INSERT INTO InventoryMovement (
//                     ProductId, FromLocationId, ToLocationId, Quantity, Notes, CreatedBy, CreatedDate
//                 )
//                 VALUES (
//                     @ProductId, @FromLocationId, @ToLocationId, @Quantity, @Notes, @CreatedBy, @CreatedDate
//                 );
//                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
//
//             int movementId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, movement);
//
//             return movement with
//             {
//                 MovementId = movementId
//             };
//         }
//
//         /// <summary>
//         ///     Transfers inventory between locations.
//         ///     This is a transactional operation that:
//         ///     1. Decreases stock at the source location
//         ///     2. Increases stock at the destination location
//         ///     3. Creates a movement record
//         ///     4. Creates transaction records for both locations
//         /// </summary>
//         public async Task<DatabaseResult> TransferInventoryAsync(
//             int productId,
//             int fromLocationId,
//             int toLocationId,
//             int quantity,
//             string? notes,
//             int createdBy )
//         {
//             try
//             {
//                 // Start a transaction for the multi-step operation
//                 // Note: This assumes your ISqlDataAccess supports transactions
//                 // If not, you may need to modify this to use a stored procedure
//
//                 // Check if source has sufficient stock
//                 bool hasSufficientStock = await HasSufficientStockAsync(productId, fromLocationId, quantity);
//                 if (!hasSufficientStock)
//                 {
//                     return DatabaseResult.Failure(
//                         $"Insufficient stock at source location {fromLocationId}",
//                         DatabaseErrorCode.InvalidInput);
//                 }
//
//                 // Decrease stock at source location
//                 DatabaseResult adjustSourceResult = await AdjustStockAsync(productId, fromLocationId, -quantity);
//                 if (!adjustSourceResult.IsSuccess)
//                 {
//                     return adjustSourceResult;
//                 }
//
//                 // Check if destination inventory record exists
//                 bool destinationExists = await ExistsForProductAndLocationAsync(productId, toLocationId);
//                 if (!destinationExists)
//                 {
//                     // Create inventory record at destination if it doesn't exist
//                     await CreateAsync(
//                         new Inventory(
//                             0,
//                             productId,
//                             toLocationId,
//                             quantity,
//                             0,
//                             DateTime.UtcNow
//                         ));
//                 }
//                 else
//                 {
//                     // Increase stock at destination location
//                     DatabaseResult adjustDestResult = await AdjustStockAsync(productId, toLocationId, quantity);
//                     if (!adjustDestResult.IsSuccess)
//                     {
//                         // Rollback source adjustment (add back the quantity)
//                         await AdjustStockAsync(productId, fromLocationId, quantity);
//                         return adjustDestResult;
//                     }
//                 }
//
//                 // Create movement record
//                 await CreateMovementAsync(
//                     new InventoryMovement(
//                         0,
//                         productId,
//                         fromLocationId,
//                         toLocationId,
//                         quantity,
//                         notes,
//                         createdBy,
//                         DateTime.UtcNow
//                     ));
//
//                 return DatabaseResult.Success();
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error transferring inventory: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         #endregion
//
//         #region Write Operations - Inventory Transactions
//
//         /// <summary>
//         ///     Creates a new inventory transaction record and returns it with its generated ID.
//         /// </summary>
//         public async Task<InventoryTransaction> CreateTransactionAsync( InventoryTransaction transaction )
//         {
//             // language=tsql
//             const string sql = @"
//                 INSERT INTO InventoryTransaction (
//                     ProductId, LocationId, Type, Quantity, UnitCost, Reference, Notes, CreatedBy, CreatedDate
//                 )
//                 VALUES (
//                     @ProductId, @LocationId, @Type, @Quantity, @UnitCost, @Reference, @Notes, @CreatedBy, @CreatedDate
//                 );
//                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
//
//             int transactionId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, transaction);
//
//             return transaction with
//             {
//                 TransactionId = transactionId
//             };
//         }
//
//         /// <summary>
//         ///     Records a stock-in transaction and updates inventory.
//         /// </summary>
//         public async Task<DatabaseResult> RecordStockInAsync(
//             int productId,
//             int locationId,
//             int quantity,
//             decimal? unitCost = null,
//             string? reference = null,
//             string? notes = null,
//             int createdBy = 0 )
//         {
//             try
//             {
//                 // Adjust stock
//                 DatabaseResult adjustResult = await AdjustStockAsync(productId, locationId, quantity);
//                 if (!adjustResult.IsSuccess)
//                 {
//                     return adjustResult;
//                 }
//
//                 // Create transaction record
//                 await CreateTransactionAsync(
//                     new InventoryTransaction(
//                         0,
//                         productId,
//                         locationId,
//                         TransactionType.StockIn,
//                         quantity,
//                         unitCost,
//                         reference,
//                         notes,
//                         createdBy,
//                         DateTime.UtcNow
//                     ));
//
//                 return DatabaseResult.Success();
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error recording stock in: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Records a stock-out transaction and updates inventory.
//         /// </summary>
//         public async Task<DatabaseResult> RecordStockOutAsync(
//             int productId,
//             int locationId,
//             int quantity,
//             string? reference = null,
//             string? notes = null,
//             int createdBy = 0 )
//         {
//             try
//             {
//                 // Check sufficient stock
//                 bool hasSufficientStock = await HasSufficientStockAsync(productId, locationId, quantity);
//                 if (!hasSufficientStock)
//                 {
//                     return DatabaseResult.Failure(
//                         $"Insufficient stock for Product {productId} at Location {locationId}",
//                         DatabaseErrorCode.InvalidInput);
//                 }
//
//                 // Adjust stock (negative quantity)
//                 DatabaseResult adjustResult = await AdjustStockAsync(productId, locationId, -quantity);
//                 if (!adjustResult.IsSuccess)
//                 {
//                     return adjustResult;
//                 }
//
//                 // Create transaction record
//                 await CreateTransactionAsync(
//                     new InventoryTransaction(
//                         0,
//                         productId,
//                         locationId,
//                         TransactionType.StockOut,
//                         quantity,
//                         null,
//                         reference,
//                         notes,
//                         createdBy,
//                         DateTime.UtcNow
//                     ));
//
//                 return DatabaseResult.Success();
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error recording stock out: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Records an inventory adjustment and updates inventory.
//         /// </summary>
//         public async Task<DatabaseResult> RecordAdjustmentAsync(
//             int productId,
//             int locationId,
//             int quantityChange,
//             string? notes = null,
//             int createdBy = 0 )
//         {
//             try
//             {
//                 // Adjust stock
//                 DatabaseResult adjustResult = await AdjustStockAsync(productId, locationId, quantityChange);
//                 if (!adjustResult.IsSuccess)
//                 {
//                     return adjustResult;
//                 }
//
//                 // Create transaction record
//                 await CreateTransactionAsync(
//                     new InventoryTransaction(
//                         0,
//                         productId,
//                         locationId,
//                         TransactionType.Adjustment,
//                         Math.Abs(quantityChange),
//                         null,
//                         null,
//                         notes,
//                         createdBy,
//                         DateTime.UtcNow
//                     ));
//
//                 return DatabaseResult.Success();
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error recording adjustment: {ex.Message}",
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
//         ///     Permanently deletes an inventory movement by ID.
//         ///     WARNING: This does not reverse the movement. Use only for cleaning up test data.
//         /// </summary>
//         public async Task<DatabaseResult> DeleteMovementAsync( int movementId )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = "DELETE FROM InventoryMovement WHERE MovementId = @MovementId";
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         MovementId = movementId
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Movement with ID {movementId} not found",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error deleting movement: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         /// <summary>
//         ///     Permanently deletes an inventory transaction by ID.
//         ///     WARNING: This does not reverse the transaction. Use only for cleaning up test data.
//         /// </summary>
//         public async Task<DatabaseResult> DeleteTransactionAsync( int transactionId )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = "DELETE FROM InventoryTransaction WHERE TransactionId = @TransactionId";
//                 int affectedRows = await sqlDataAccess.ExecuteAsync(
//                     sql,
//                     new
//                     {
//                         TransactionId = transactionId
//                     });
//
//                 return affectedRows > 0
//                     ? DatabaseResult.Success()
//                     : DatabaseResult.Failure(
//                         $"Transaction with ID {transactionId} not found",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error deleting transaction: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         #endregion
//     }
// }



