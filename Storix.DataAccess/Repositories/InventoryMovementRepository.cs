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
//     ///     Repository for managing inventory movements between locations.
//     /// </summary>
//     public class InventoryMovementRepository( ISqlDataAccess sqlDataAccess )
//     {
//         #region Validation
//
//         /// <summary>
//         ///     Check if an inventory movement exists by ID.
//         /// </summary>
//         public async Task<bool> ExistsAsync( int movementId )
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(1) FROM InventoryMovement WHERE MovementId = @MovementId";
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     MovementId = movementId
//                 });
//         }
//
//         /// <summary>
//         ///     Checks if a product has any movement history.
//         /// </summary>
//         public async Task<bool> ProductHasMovementsAsync( int productId )
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(1) FROM InventoryMovement WHERE ProductId = @ProductId";
//             return await sqlDataAccess.ExecuteScalarAsync<bool>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Checks if a location has any movement history (as source or destination).
//         /// </summary>
//         public async Task<bool> LocationHasMovementsAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT COUNT(1) FROM InventoryMovement 
//                 WHERE FromLocationId = @LocationId OR ToLocationId = @LocationId";
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
//         ///     Gets the total count of inventory movements.
//         /// </summary>
//         public async Task<int> GetTotalCountAsync()
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(*) FROM InventoryMovement";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
//         }
//
//         /// <summary>
//         ///     Gets the count of movements for a specific product.
//         /// </summary>
//         public async Task<int> GetCountByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = "SELECT COUNT(*) FROM InventoryMovement WHERE ProductId = @ProductId";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets the count of movements from a specific location.
//         /// </summary>
//         public async Task<int> GetCountByFromLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT COUNT(*) FROM InventoryMovement 
//                 WHERE FromLocationId = @LocationId";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets the count of movements to a specific location.
//         /// </summary>
//         public async Task<int> GetCountByToLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT COUNT(*) FROM InventoryMovement 
//                 WHERE ToLocationId = @LocationId";
//             return await sqlDataAccess.ExecuteScalarAsync<int>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         #endregion
//
//         #region Read Operations
//
//         /// <summary>
//         ///     Gets an inventory movement by its ID.
//         /// </summary>
//         public async Task<InventoryMovement?> GetByIdAsync( int movementId )
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
//         public async Task<IEnumerable<InventoryMovement>> GetAllCustomersAsync()
//         {
//             // language=tsql
//             const string sql = "SELECT * FROM InventoryMovement ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(sql);
//         }
//
//         /// <summary>
//         ///     Gets inventory movements for a specific product.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetByProductAsync( int productId )
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
//         public async Task<IEnumerable<InventoryMovement>> GetByFromLocationAsync( int locationId )
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
//         public async Task<IEnumerable<InventoryMovement>> GetByToLocationAsync( int locationId )
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
//         ///     Gets inventory movements between two specific locations.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetBetweenLocationsAsync( 
//             int fromLocationId, 
//             int toLocationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 WHERE FromLocationId = @FromLocationId 
//                 AND ToLocationId = @ToLocationId 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     FromLocationId = fromLocationId,
//                     ToLocationId = toLocationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets inventory movements by date range.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync( 
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
//         public async Task<IEnumerable<InventoryMovement>> GetByCreatedByAsync( int createdBy )
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
//         /// <summary>
//         ///     Gets recent inventory movements (last N records).
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetRecentAsync( int count )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT TOP (@Count) * FROM InventoryMovement 
//                 ORDER BY CreatedDate DESC";
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
//                 sql,
//                 new
//                 {
//                     Count = count
//                 });
//         }
//
//         /// <summary>
//         ///     Gets a paged list of inventory movements.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> GetPagedAsync( int pageNumber, int pageSize )
//         {
//             int offset = (pageNumber - 1) * pageSize;
//
//             // language=tsql
//             const string sql = @"
//                 SELECT * FROM InventoryMovement 
//                 ORDER BY CreatedDate DESC 
//                 OFFSET @Offset ROWS
//                 FETCH NEXT @PageSize ROWS ONLY";
//
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(
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
//         ///     Searches inventory movements with multiple optional filters.
//         /// </summary>
//         public async Task<IEnumerable<InventoryMovement>> SearchAsync(
//             int? productId = null,
//             int? fromLocationId = null,
//             int? toLocationId = null,
//             DateTime? startDate = null,
//             DateTime? endDate = null,
//             int? createdBy = null,
//             string? searchTerm = null )
//         {
//             StringBuilder sql = new("SELECT * FROM InventoryMovement WHERE 1=1");
//             DynamicParameters parameters = new();
//
//             if (productId.HasValue)
//             {
//                 sql.Append(" AND ProductId = @ProductId");
//                 parameters.Add("ProductId", productId.Value);
//             }
//
//             if (fromLocationId.HasValue)
//             {
//                 sql.Append(" AND FromLocationId = @FromLocationId");
//                 parameters.Add("FromLocationId", fromLocationId.Value);
//             }
//
//             if (toLocationId.HasValue)
//             {
//                 sql.Append(" AND ToLocationId = @ToLocationId");
//                 parameters.Add("ToLocationId", toLocationId.Value);
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
//             if (createdBy.HasValue)
//             {
//                 sql.Append(" AND CreatedBy = @CreatedBy");
//                 parameters.Add("CreatedBy", createdBy.Value);
//             }
//
//             if (!string.IsNullOrWhiteSpace(searchTerm))
//             {
//                 sql.Append(" AND Notes LIKE @SearchTerm");
//                 parameters.Add("SearchTerm", $"%{searchTerm}%");
//             }
//
//             sql.Append(" ORDER BY CreatedDate DESC");
//
//             return await sqlDataAccess.QueryAsync<InventoryMovement>(sql.ToString(), parameters);
//         }
//
//         #endregion
//
//         #region Statistics & Reporting
//
//         /// <summary>
//         ///     Gets movement statistics for a specific product.
//         /// </summary>
//         public async Task<MovementStatisticsDto?> GetStatisticsByProductAsync( int productId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT 
//                     COUNT(*) as TotalMovements,
//                     SUM(Quantity) as TotalQuantityMoved,
//                     AVG(CAST(Quantity as DECIMAL(10,2))) as AverageQuantity,
//                     MIN(CreatedDate) as FirstMovementDate,
//                     MAX(CreatedDate) as LastMovementDate
//                 FROM InventoryMovement
//                 WHERE ProductId = @ProductId";
//
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<MovementStatisticsDto>(
//                 sql,
//                 new
//                 {
//                     ProductId = productId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets movement statistics for a specific location (in and out).
//         /// </summary>
//         public async Task<LocationMovementStatisticsDto?> GetStatisticsByLocationAsync( int locationId )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT 
//                     COUNT(CASE WHEN FromLocationId = @LocationId THEN 1 END) as MovementsOut,
//                     COUNT(CASE WHEN ToLocationId = @LocationId THEN 1 END) as MovementsIn,
//                     SUM(CASE WHEN FromLocationId = @LocationId THEN Quantity ELSE 0 END) as QuantityOut,
//                     SUM(CASE WHEN ToLocationId = @LocationId THEN Quantity ELSE 0 END) as QuantityIn
//                 FROM InventoryMovement
//                 WHERE FromLocationId = @LocationId OR ToLocationId = @LocationId";
//
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<LocationMovementStatisticsDto>(
//                 sql,
//                 new
//                 {
//                     LocationId = locationId
//                 });
//         }
//
//         /// <summary>
//         ///     Gets movement statistics for a date range.
//         /// </summary>
//         public async Task<MovementStatisticsDto?> GetStatisticsByDateRangeAsync( 
//             DateTime startDate, 
//             DateTime endDate )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT 
//                     COUNT(*) as TotalMovements,
//                     SUM(Quantity) as TotalQuantityMoved,
//                     AVG(CAST(Quantity as DECIMAL(10,2))) as AverageQuantity,
//                     MIN(CreatedDate) as FirstMovementDate,
//                     MAX(CreatedDate) as LastMovementDate
//                 FROM InventoryMovement
//                 WHERE CreatedDate BETWEEN @StartDate AND @EndDate";
//
//             return await sqlDataAccess.QuerySingleOrDefaultAsync<MovementStatisticsDto>(
//                 sql,
//                 new
//                 {
//                     StartDate = startDate,
//                     EndDate = endDate
//                 });
//         }
//
//         /// <summary>
//         ///     Gets the most active movement routes (location pairs).
//         /// </summary>
//         public async Task<IEnumerable<MovementRouteDto>> GetTopMovementRoutesAsync( int topCount = 10 )
//         {
//             // language=tsql
//             const string sql = @"
//                 SELECT TOP (@TopCount)
//                     FromLocationId,
//                     ToLocationId,
//                     COUNT(*) as MovementCount,
//                     SUM(Quantity) as TotalQuantity
//                 FROM InventoryMovement
//                 GROUP BY FromLocationId, ToLocationId
//                 ORDER BY COUNT(*) DESC";
//
//             return await sqlDataAccess.QueryAsync<MovementRouteDto>(
//                 sql,
//                 new
//                 {
//                     TopCount = topCount
//                 });
//         }
//
//         #endregion
//
//         #region Write Operations
//
//         /// <summary>
//         ///     Creates a new inventory movement record and returns it with its generated ID.
//         /// </summary>
//         public async Task<InventoryMovement> CreateAsync( InventoryMovement movement )
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
//         ///     Updates an existing inventory movement's notes.
//         ///     Note: Quantity and location changes are typically not allowed after creation.
//         /// </summary>
//         public async Task<InventoryMovement> UpdateNotesAsync( int movementId, string? notes )
//         {
//             // language=tsql
//             const string sql = @"
//                 UPDATE InventoryMovement 
//                 SET Notes = @Notes
//                 WHERE MovementId = @MovementId";
//
//             await sqlDataAccess.ExecuteAsync(
//                 sql,
//                 new
//                 {
//                     MovementId = movementId,
//                     Notes = notes
//                 });
//
//             var movement = await GetByIdAsync(movementId);
//             return movement!;
//         }
//
//         #endregion
//
//         #region Delete Operations
//
//         /// <summary>
//         ///     Permanently deletes an inventory movement by ID.
//         ///     WARNING: This does not reverse the movement. Use only for cleaning up test data.
//         /// </summary>
//         public async Task<DatabaseResult> DeleteAsync( int movementId )
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
//         ///     Permanently deletes all movements for a specific product.
//         ///     WARNING: This does not reverse the movements. Use only for cleaning up test data.
//         /// </summary>
//         public async Task<DatabaseResult> DeleteByProductAsync( int productId )
//         {
//             try
//             {
//                 // language=tsql
//                 const string sql = "DELETE FROM InventoryMovement WHERE ProductId = @ProductId";
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
//                         $"No movements found for Product {productId}",
//                         DatabaseErrorCode.NotFound);
//             }
//             catch (Exception ex)
//             {
//                 return DatabaseResult.Failure(
//                     $"Error deleting movements by product: {ex.Message}",
//                     DatabaseErrorCode.UnexpectedError);
//             }
//         }
//
//         #endregion
//     }
// }



