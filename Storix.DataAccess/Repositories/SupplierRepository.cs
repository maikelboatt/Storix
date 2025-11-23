using System.Text;
using Dapper;
using Storix.Application.Common;
using Storix.Application.DataAccess;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class SupplierRepository( ISqlDataAccess sqlDataAccess ):ISupplierRepository
    {
        #region Validation

        /// <summary>
        ///     Check if a supplier exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int supplierId, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Supplier WHERE SupplierId = @SupplierId"
                // language=tsql
                : "SELECT COUNT(1) FROM Supplier WHERE SupplierId = @SupplierId AND IsDeleted = 0";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    SupplierId = supplierId
                });
        }

        /// <summary>
        ///     Check if a supplier exists by email.
        /// </summary>
        public async Task<bool> ExistsByEmailAsync( string email, int? excludeUserId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Supplier WHERE Email = @Email AND (@ExcludeUserId IS NULL OR SupplierId != @ExcludeUserId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Supplier WHERE Email = @Email AND IsDeleted = 0 AND (@ExcludeUserId IS NULL OR SupplierId != @ExcludeUserId)";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    Email = email,
                    ExcludeUserId = excludeUserId
                });
        }

        /// <summary>
        ///     Check if a supplier exists by phone.
        /// </summary>
        public async Task<bool> ExistsByPhoneAsync( string phone, int? excludeUserId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Supplier WHERE Phone = @Phone AND (@ExcludeUserId IS NULL OR SupplierId != @ExcludeUserId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Supplier WHERE Phone = @Phone AND IsDeleted = 0 AND (@ExcludeUserId IS NULL OR SupplierId != @ExcludeUserId)";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    Phone = phone,
                    ExcludeUserId = excludeUserId
                });
        }

        #endregion

        #region Count Operations

        /// <summary>
        ///     Gets the total count of suppliers (including deleted).
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Supplier";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of active suppliers.
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Supplier WHERE IsDeleted = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of deleted suppliers.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Supplier WHERE IsDeleted = 1";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets a supplier by ID (includes deleted).
        /// </summary>
        public async Task<Supplier?> GetByIdAsync( int supplierId, bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Supplier WHERE SupplierId = @SupplierId"
                // language=tsql
                : "SELECT * FROM Supplier WHERE SupplierId = @SupplierId AND IsDeleted = 0";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Supplier>(
                sql,
                new
                {
                    SupplierId = supplierId
                });
        }

        /// <summary>
        ///     Gets all suppliers (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Supplier>> GetAllAsync( bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Supplier ORDER BY Name"
                // language=tsql
                : "SELECT * FROM Supplier WHERE  IsDeleted = 0 ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Supplier>(sql);
        }

        /// <summary>
        ///     Gets a supplier by email (includes deleted).
        /// </summary>
        public async Task<Supplier?> GetByEmailAsync( string email )
        {
            // language=tsql
            const string sql = "SELECT * FROM Supplier WHERE Email = @Email";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Supplier>(
                sql,
                new
                {
                    Email = email
                });
        }

        /// <summary>
        ///     Gets a supplier by phone (includes deleted).
        /// </summary>
        public async Task<Supplier?> GetByPhoneAsync( string phone )
        {
            // language=tsql
            const string sql = "SELECT * FROM Supplier WHERE Phone = @Phone";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Supplier>(
                sql,
                new
                {
                    Phone = phone
                });
        }

        /// <summary>
        ///     Gets a paged list of suppliers (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Supplier>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Supplier 
                ORDER BY Name 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<Supplier>(
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
        ///     Searches suppliers with optional filters (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Supplier>> SearchAsync(
            string? searchTerm = null,
            bool? isDeleted = null )
        {
            // language=tsql
            StringBuilder sql = new("SELECT * FROM Supplier WHERE 1=1");
            DynamicParameters parameters = new();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql.Append(" AND (Name LIKE @SearchTerm OR Email LIKE @SearchTerm OR Phone LIKE @SearchTerm OR Address LIKE @SearchTerm)");
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            if (isDeleted.HasValue)
            {
                sql.Append(" AND IsDeleted = @IsDeleted");
                parameters.Add("IsDeleted", isDeleted.Value);
            }

            sql.Append(" ORDER BY Name");

            return await sqlDataAccess.QueryAsync<Supplier>(sql.ToString(), parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new supplier and returns it with its generated ID.
        /// </summary>
        public async Task<Supplier> CreateAsync( Supplier supplier )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Supplier (
                    Name, Email, Phone, Address, IsDeleted, DeletedAt
                )
                VALUES (
                    @Name, @Email, @Phone, @Address, @IsDeleted, @DeletedAt
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int supplierId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, supplier);

            return supplier with
            {
                SupplierId = supplierId
            };
        }

        /// <summary>
        ///     Updates an existing supplier.
        /// </summary>
        public async Task<Supplier> UpdateAsync( Supplier supplier )
        {
            // language=tsql
            const string sql = @"
                UPDATE Supplier 
                SET Name = @Name,
                    Email = @Email,
                    Phone = @Phone,
                    Address = @Address
                WHERE SupplierId = @SupplierId";

            await sqlDataAccess.ExecuteAsync(sql, supplier);
            return supplier;
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Soft deletes a supplier by ID.
        /// </summary>
        public async Task<DatabaseResult> SoftDeleteAsync( int supplierId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Supplier 
                    SET IsDeleted = 1,
                        DeletedAt = @DeletedAt
                    WHERE SupplierId = @SupplierId AND IsDeleted = 0";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        SupplierId = supplierId,
                        DeletedAt = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Supplier with ID {supplierId} not found or already deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting supplier: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Restores a soft-deleted supplier.
        /// </summary>
        public async Task<DatabaseResult> RestoreAsync( int supplierId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Supplier 
                    SET IsDeleted = 0,
                        DeletedAt = NULL
                    WHERE SupplierId = @SupplierId AND IsDeleted = 1";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        SupplierId = supplierId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Supplier with ID {supplierId} not found or not deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error restoring supplier: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Permanently deletes a supplier by ID.
        ///     WARNING: This permanently removes the supplier from the database.
        /// </summary>
        public async Task<DatabaseResult> HardDeleteAsync( int supplierId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Supplier WHERE SupplierId = @SupplierId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        SupplierId = supplierId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Supplier with ID {supplierId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error permanently deleting supplier: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
