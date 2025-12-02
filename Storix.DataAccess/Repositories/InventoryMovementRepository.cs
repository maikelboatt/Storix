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
    public class InventoryMovementRepository( ISqlDataAccess sqlDataAccess ):IInventoryMovementRepository
    {
        #region Validation

        /// <summary>
        ///     Check if a movement exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int movementId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM InventoryMovement WHERE MovementId = @MovementId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    MovementId = movementId
                });
        }

        #endregion

        #region Count Operations

        /// <summary>
        ///     Gets the total count of inventory movements.
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM InventoryMovement";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of movements for a specific product.
        /// </summary>
        public async Task<int> GetCountByProductIdAsync( int productId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM InventoryMovement WHERE ProductId = @ProductId";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        ///     Gets the count of movements involving a specific location.
        /// </summary>
        public async Task<int> GetCountByLocationIdAsync( int locationId )
        {
            // language=tsql
            const string sql = @"
                SELECT COUNT(*) FROM InventoryMovement 
                WHERE FromLocationId = @LocationId OR ToLocationId = @LocationId";

            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    LocationId = locationId
                });
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets a movement by ID.
        /// </summary>
        public async Task<InventoryMovement?> GetByIdAsync( int movementId )
        {
            // language=tsql
            const string sql = "SELECT * FROM InventoryMovement WHERE MovementId = @MovementId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<InventoryMovement>(
                sql,
                new
                {
                    MovementId = movementId
                });
        }

        /// <summary>
        ///     Gets all movements for a specific product.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetByProductIdAsync( int productId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                WHERE ProductId = @ProductId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        ///     Gets all movements involving a specific location (from or to).
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetByLocationIdAsync( int locationId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                WHERE FromLocationId = @LocationId OR ToLocationId = @LocationId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
                sql,
                new
                {
                    LocationId = locationId
                });
        }

        /// <summary>
        ///     Gets movements from a specific location.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetByFromLocationAsync( int fromLocationId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                WHERE FromLocationId = @FromLocationId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
                sql,
                new
                {
                    FromLocationId = fromLocationId
                });
        }

        /// <summary>
        ///     Gets movements to a specific location.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetByToLocationAsync( int toLocationId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                WHERE ToLocationId = @ToLocationId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
                sql,
                new
                {
                    ToLocationId = toLocationId
                });
        }

        /// <summary>
        ///     Gets movements by user who created them.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetByCreatedByAsync( int userId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                WHERE CreatedBy = @UserId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
                sql,
                new
                {
                    UserId = userId
                });
        }

        /// <summary>
        ///     Gets movements within a date range.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                WHERE CreatedDate >= @StartDate AND CreatedDate <= @EndDate 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
                sql,
                new
                {
                    StartDate = startDate,
                    EndDate = endDate
                });
        }

        /// <summary>
        ///     Gets all inventory movements.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM InventoryMovement ORDER BY CreatedDate DESC";
            return await sqlDataAccess.QueryAsync<InventoryMovement>(sql);
        }

        /// <summary>
        ///     Gets a paged list of movements.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryMovement 
                ORDER BY CreatedDate DESC 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<InventoryMovement>(
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
        ///     Searches movements with optional filters.
        /// </summary>
        public async Task<IEnumerable<InventoryMovement>> SearchAsync(
            int? productId = null,
            int? fromLocationId = null,
            int? toLocationId = null,
            int? createdBy = null,
            DateTime? startDate = null,
            DateTime? endDate = null )
        {
            StringBuilder sql = new("SELECT * FROM InventoryMovement WHERE 1=1");
            DynamicParameters parameters = new();

            if (productId.HasValue)
            {
                sql.Append(" AND ProductId = @ProductId");
                parameters.Add("ProductId", productId.Value);
            }

            if (fromLocationId.HasValue)
            {
                sql.Append(" AND FromLocationId = @FromLocationId");
                parameters.Add("FromLocationId", fromLocationId.Value);
            }

            if (toLocationId.HasValue)
            {
                sql.Append(" AND ToLocationId = @ToLocationId");
                parameters.Add("ToLocationId", toLocationId.Value);
            }

            if (createdBy.HasValue)
            {
                sql.Append(" AND CreatedBy = @CreatedBy");
                parameters.Add("CreatedBy", createdBy.Value);
            }

            if (startDate.HasValue)
            {
                sql.Append(" AND CreatedDate >= @StartDate");
                parameters.Add("StartDate", startDate.Value);
            }

            if (endDate.HasValue)
            {
                sql.Append(" AND CreatedDate <= @EndDate");
                parameters.Add("EndDate", endDate.Value);
            }

            sql.Append(" ORDER BY CreatedDate DESC");

            return await sqlDataAccess.QueryAsync<InventoryMovement>(sql.ToString(), parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new inventory movement and returns it with its generated ID.
        /// </summary>
        public async Task<InventoryMovement> CreateAsync( InventoryMovement movement )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO InventoryMovement (
                    ProductId, FromLocationId, ToLocationId, Quantity, Notes, CreatedBy, CreatedDate
                )
                VALUES (
                    @ProductId, @FromLocationId, @ToLocationId, @Quantity, @Notes, @CreatedBy, @CreatedDate
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int movementId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, movement);

            return movement with
            {
                MovementId = movementId
            };
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Permanently deletes a movement by ID.
        ///     WARNING: This permanently removes the movement record.
        /// </summary>
        public async Task<DatabaseResult> DeleteAsync( int movementId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM InventoryMovement WHERE MovementId = @MovementId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        MovementId = movementId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Movement with ID {movementId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting movement: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
