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
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class InventoryTransactionRepository( ISqlDataAccess sqlDataAccess ):IInventoryTransactionRepository
    {
        #region Validation

        /// <summary>
        ///     Check if a transaction exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int transactionId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM InventoryTransaction WHERE TransactionId = @TransactionId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    TransactionId = transactionId
                });
        }

        #endregion

        #region Count Operations

        /// <summary>
        ///     Gets the total count of transactions.
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM InventoryTransaction";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of transactions by type.
        /// </summary>
        public async Task<int> GetCountByTypeAsync( TransactionType type )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM InventoryTransaction WHERE Type = @Type";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Type = type
                });
        }

        /// <summary>
        ///     Gets the count of transactions for a specific product.
        /// </summary>
        public async Task<int> GetCountByProductIdAsync( int productId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM InventoryTransaction WHERE ProductId = @ProductId";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        ///     Gets the count of transactions for a specific location.
        /// </summary>
        public async Task<int> GetCountByLocationIdAsync( int locationId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM InventoryTransaction WHERE LocationId = @LocationId";
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
        ///     Gets a transaction by ID.
        /// </summary>
        public async Task<InventoryTransaction?> GetByIdAsync( int transactionId )
        {
            // language=tsql
            const string sql = "SELECT * FROM InventoryTransaction WHERE TransactionId = @TransactionId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<InventoryTransaction>(
                sql,
                new
                {
                    TransactionId = transactionId
                });
        }

        /// <summary>
        ///     Gets all transactions for a specific product.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync( int productId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryTransaction 
                WHERE ProductId = @ProductId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        ///     Gets all transactions for a specific location.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetByLocationIdAsync( int locationId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryTransaction 
                WHERE LocationId = @LocationId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
                sql,
                new
                {
                    LocationId = locationId
                });
        }

        /// <summary>
        ///     Gets transactions by type.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetByTypeAsync( TransactionType type )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryTransaction 
                WHERE Type = @Type 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
                sql,
                new
                {
                    Type = type
                });
        }

        /// <summary>
        ///     Gets transactions by user who created them.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetByCreatedByAsync( int userId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryTransaction 
                WHERE CreatedBy = @UserId 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
                sql,
                new
                {
                    UserId = userId
                });
        }

        /// <summary>
        ///     Gets transactions within a date range.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetByDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM InventoryTransaction 
                WHERE CreatedDate >= @StartDate AND CreatedDate <= @EndDate 
                ORDER BY CreatedDate DESC";

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
                sql,
                new
                {
                    StartDate = startDate,
                    EndDate = endDate
                });
        }

        ///     Gets transactions by reference.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetByReferenceAsync( string reference )
        {
// language=tsql
            const string sql = @"
SELECT * FROM InventoryTransaction
WHERE Reference = @Reference
ORDER BY CreatedDate DESC";
            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
                sql,
                new
                {
                    Reference = reference
                });
        }

        /// <summary>
        ///     Gets all inventory transactions.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM InventoryTransaction ORDER BY CreatedDate DESC";
            return await sqlDataAccess.QueryAsync<InventoryTransaction>(sql);
        }

        /// <summary>
        ///     Gets a paged list of transactions.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
            SELECT * FROM InventoryTransaction 
            ORDER BY CreatedDate DESC 
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(
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
        ///     Searches transactions with optional filters.
        /// </summary>
        public async Task<IEnumerable<InventoryTransaction>> SearchAsync(
            int? productId = null,
            int? locationId = null,
            TransactionType? type = null,
            int? createdBy = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? reference = null )
        {
            StringBuilder sql = new("SELECT * FROM InventoryTransaction WHERE 1=1");
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

            if (type.HasValue)
            {
                sql.Append(" AND Type = @Type");
                parameters.Add("Type", type.Value);
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

            if (!string.IsNullOrWhiteSpace(reference))
            {
                sql.Append(" AND Reference LIKE @Reference");
                parameters.Add("Reference", $"%{reference}%");
            }

            sql.Append(" ORDER BY CreatedDate DESC");

            return await sqlDataAccess.QueryAsync<InventoryTransaction>(sql.ToString(), parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new inventory transaction and returns it with its generated ID.
        /// </summary>
        public async Task<InventoryTransaction> CreateAsync( InventoryTransaction transaction )
        {
            // language=tsql
            const string sql = @"
            INSERT INTO InventoryTransaction (
                ProductId, LocationId, Type, Quantity, UnitCost, Reference, Notes, CreatedBy, CreatedDate
            )
            VALUES (
                @ProductId, @LocationId, @Type, @Quantity, @UnitCost, @Reference, @Notes, @CreatedBy, @CreatedDate
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int transactionId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, transaction);

            return transaction with
            {
                TransactionId = transactionId
            };
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Permanently deletes a transaction by ID.
        ///     WARNING: This permanently removes the transaction record.
        /// </summary>
        public async Task<DatabaseResult> DeleteAsync( int transactionId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM InventoryTransaction WHERE TransactionId = @TransactionId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        TransactionId = transactionId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Transaction with ID {transactionId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting transaction: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Aggregation Operations

        /// <summary>
        ///     Gets total quantity by transaction type for a product.
        /// </summary>
        public async Task<Dictionary<TransactionType, int>> GetQuantityByTypeForProductAsync( int productId )
        {
            // language=tsql
            const string sql = @"
            SELECT Type, SUM(Quantity) as TotalQuantity
            FROM InventoryTransaction
            WHERE ProductId = @ProductId
            GROUP BY Type";

            IEnumerable<dynamic> results = await sqlDataAccess.QueryAsync<dynamic>(
                sql,
                new
                {
                    ProductId = productId
                });

            Dictionary<TransactionType, int> summary = new();
            foreach (dynamic result in results)
            {
                summary[(TransactionType)result.Type] = (int)result.TotalQuantity;
            }

            return summary;
        }

        /// <summary>
        ///     Gets total value of transactions by type.
        /// </summary>
        public async Task<Dictionary<TransactionType, decimal>> GetTotalValueByTypeAsync()
        {
            // language=tsql
            const string sql = @"
            SELECT Type, SUM(Quantity * ISNULL(UnitCost, 0)) as TotalValue
            FROM InventoryTransaction
            GROUP BY Type";

            IEnumerable<dynamic> results = await sqlDataAccess.QueryAsync<dynamic>(sql);

            Dictionary<TransactionType, decimal> summary = new();
            foreach (dynamic result in results)
            {
                summary[(TransactionType)result.Type] = (decimal)result.TotalValue;
            }

            return summary;
        }

        #endregion
    }
}
