using System.Text;
using Dapper;
using Storix.Application.Common;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class CustomerRepository( ISqlDataAccess sqlDataAccess ):ICustomerRepository
    {
        #region Validation

        /// <summary>
        ///     Check if a customer exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int customerId, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Customer WHERE CustomerId = @CustomerId"
                // language=tsql
                : "SELECT COUNT(1) FROM Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    CustomerId = customerId
                });
        }

        /// <summary>
        ///     Check if a customer exists by email.
        /// </summary>
        public async Task<bool> ExistsByEmailAsync( string email, int? excludeUserId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Customer WHERE Email = @Email AND (@ExcludeUserId IS NULL OR CustomerId != @ExcludeUserId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Customer WHERE Email = @Email AND IsDeleted = 0 AND (@ExcludeUserId IS NULL OR CustomerId != @ExcludeUserId)";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    Email = email
                });
        }

        /// <summary>
        ///     Check if a customer exists by phone.
        /// </summary>
        public async Task<bool> ExistsByPhoneAsync( string phone, int? excludeUserId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Customer WHERE Phone = @Phone AND (@ExcludeUserId IS NULL OR CustomerId != @ExcludeUserId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Customer WHERE Phone = @Phone AND IsDeleted = 0 AND (@ExcludeUserId IS NULL OR CustomerId != @ExcludeUserId)";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    Phone = phone
                });
        }

        #endregion

        #region CountOperations

        /// <summary>
        ///     Gets the total count of customers (including deleted).
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Customer";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of active customers.
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Customer WHERE IsDeleted = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of deleted customers.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Customer WHERE IsDeleted = 1";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets a customer by ID (includes deleted).
        /// </summary>
        public async Task<Customer?> GetByIdAsync( int customerId )
        {
            // language=tsql
            const string sql = "SELECT * FROM Customer WHERE CustomerId = @CustomerId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Customer>(
                sql,
                new
                {
                    CustomerId = customerId
                });
        }

        /// <summary>
        ///     Gets all customers (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Customer ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Customer>(sql);
        }

        /// <summary>
        ///     Gets a customer by email (includes deleted).
        /// </summary>
        public async Task<Customer?> GetByEmailAsync( string email )
        {
            // language=tsql
            const string sql = "SELECT * FROM Customer WHERE Email = @Email";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Customer>(
                sql,
                new
                {
                    Email = email
                });
        }

        /// <summary>
        ///     Gets a customer by phone (includes deleted).
        /// </summary>
        public async Task<Customer?> GetByPhoneAsync( string phone )
        {
            // language=tsql
            const string sql = "SELECT * FROM Customer WHERE Phone = @Phone";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Customer>(
                sql,
                new
                {
                    Phone = phone
                });
        }

        /// <summary>
        ///     Gets a paged list of customers (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Customer>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Customer 
                ORDER BY Name 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<Customer>(
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
        ///     Searches customers with optional filters (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Customer>> SearchAsync(
            string? searchTerm = null,
            bool? isDeleted = null )
        {
            // language=tsql
            StringBuilder sql = new("SELECT * FROM Customer WHERE 1=1");
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

            return await sqlDataAccess.QueryAsync<Customer>(sql.ToString(), parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new customer and returns it with its generated ID.
        /// </summary>
        public async Task<Customer> CreateAsync( Customer customer )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Customer (
                    Name, Email, Phone, Address, IsDeleted, DeletedAt
                )
                VALUES (
                    @Name, @Email, @Phone, @Address, @IsDeleted, @DeletedAt
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int customerId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, customer);

            return customer with
            {
                CustomerId = customerId
            };
        }

        /// <summary>
        ///     Updates an existing customer.
        /// </summary>
        public async Task<Customer> UpdateAsync( Customer customer )
        {
            // language=tsql
            const string sql = @"
                UPDATE Customer 
                SET Name = @Name,
                    Email = @Email,
                    Phone = @Phone,
                    Address = @Address
                WHERE CustomerId = @CustomerId";

            await sqlDataAccess.ExecuteAsync(sql, customer);
            return customer;
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Soft deletes a customer by ID.
        /// </summary>
        public async Task<DatabaseResult> SoftDeleteAsync( int customerId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Customer 
                    SET IsDeleted = 1,
                        DeletedAt = @DeletedAt
                    WHERE CustomerId = @CustomerId AND IsDeleted = 0";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        CustomerId = customerId,
                        DeletedAt = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Customer with ID {customerId} not found or already deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting customer: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Restores a soft-deleted customer.
        /// </summary>
        public async Task<DatabaseResult> RestoreAsync( int customerId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Customer 
                    SET IsDeleted = 0,
                        DeletedAt = NULL
                    WHERE CustomerId = @CustomerId AND IsDeleted = 1";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        CustomerId = customerId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Customer with ID {customerId} not found or not deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error restoring customer: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Permanently deletes a customer by ID.
        ///     WARNING: This permanently removes the customer from the database.
        /// </summary>
        public async Task<DatabaseResult> HardDeleteAsync( int customerId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Customer WHERE CustomerId = @CustomerId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        CustomerId = customerId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Customer with ID {customerId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error permanently deleting customer: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
