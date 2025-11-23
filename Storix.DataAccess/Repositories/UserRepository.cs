using Storix.Application.Common;
using Storix.Application.DataAccess;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    /// <summary>
    /// Repository implementation for <see cref="User"/> entity operations.
    /// 
    /// Provides direct database access for user data.
    /// This repository reflects the database state fully — including soft-deleted records.
    /// Business logic and active/deleted filtering should be handled in the service layer.
    /// </summary>
    public class UserRepository( ISqlDataAccess sqlDataAccess ):IUserRepository
    {
        #region Read Operations

        public async Task<User?> GetByIdAsync( int userId, bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Users WHERE UserId = @UserId"
                // language=tsql
                : "SELECT * FROM Users WHERE UserId = @UserId AND IsDeleted = 0";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<User>(
                sql,
                new
                {
                    UserId = userId
                });
        }

        public async Task<User?> GetByUsernameAsync( string username )
        {
            // language=tsql
            const string sql = "SELECT * FROM Users WHERE Username = @Username";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<User>(
                sql,
                new
                {
                    Username = username
                });
        }

        public async Task<User?> GetByEmailAsync( string email )
        {
            // language=tsql
            const string sql = "SELECT * FROM Users WHERE Email = @Email";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<User>(
                sql,
                new
                {
                    Email = email
                });
        }

        public async Task<IEnumerable<User>> GetAllAsync( bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Users ORDER BY Username"
                // language=tsql
                : "SELECT * FROM Users WHERE IsDeleted = 0 ORDER BY Username";
            return await sqlDataAccess.QueryAsync<User>(sql);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync( string role )
        {
            // language=tsql
            const string sql = "SELECT * FROM Users WHERE Role = @Role ORDER BY Username";
            return await sqlDataAccess.QueryAsync<User>(
                sql,
                new
                {
                    Role = role
                });
        }

        #endregion

        #region Pagination

        public async Task<IEnumerable<User>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Users 
                ORDER BY Username 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<User>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Users";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetActiveCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Users WHERE IsDeleted = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetDeletedCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Users WHERE IsDeleted = 1";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetCountByRoleAsync( string role )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Users WHERE Role = @Role";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Role = role
                });
        }

        #endregion

        #region Search & Filter

        public async Task<IEnumerable<User>> SearchAsync( string searchTerm )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Users 
                WHERE Username LIKE @SearchTerm 
                   OR FullName LIKE @SearchTerm 
                   OR Email LIKE @SearchTerm 
                ORDER BY Username";

            return await sqlDataAccess.QueryAsync<User>(
                sql,
                new
                {
                    SearchTerm = $"%{searchTerm}%"
                });
        }

        #endregion

        #region Write Operations

        public async Task<User> CreateAsync( User user )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Users (Username, Password, Role, FullName, Email, IsActive, IsDeleted, DeletedAt)
                VALUES (@Username, @Password, @Role, @FullName, @Email, @IsActive, @IsDeleted, @DeletedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int userId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, user);
            return user with
            {
                UserId = userId
            };
        }

        public async Task<User> UpdateAsync( User user )
        {
            // language=tsql
            const string sql = @"
                UPDATE Users 
                SET Username = @Username,
                    Password = @Password,
                    Role = @Role,
                    FullName = @FullName,
                    Email = @Email,
                    IsActive = @IsActive,
                    IsDeleted = @IsDeleted,
                    DeletedAt = @DeletedAt
                WHERE UserId = @UserId";

            await sqlDataAccess.ExecuteAsync(sql, user);
            return user;
        }

        public async Task<DatabaseResult> SoftDeleteAsync( int userId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Users 
                    SET IsDeleted = 1, DeletedAt = @DeletedAt 
                    WHERE UserId = @UserId AND IsDeleted = 0";

                int rowsAffected = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        UserId = userId,
                        DeletedAt = DateTime.UtcNow
                    });

                return rowsAffected > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure($"User with ID {userId} not found", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure($"Error soft deleting user with ID {userId}: {ex.Message}", DatabaseErrorCode.UnexpectedError);
            }
        }

        public async Task<DatabaseResult> RestoreAsync( int userId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Users 
                    SET IsDeleted = 0, DeletedAt = NULL 
                    WHERE UserId = @UserId AND IsDeleted = 1";

                int rowsAffected = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        UserId = userId
                    });

                return rowsAffected > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"User with ID {userId} cannot be restored because it doesn't exist or has not been soft-deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure($"Error restoring user with ID {userId}: {ex.Message}", DatabaseErrorCode.UnexpectedError);
            }
        }

        public async Task<DatabaseResult> HardDeleteAsync( int userId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Users WHERE UserId = @UserId";
                int rowsAffected = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        UserId = userId
                    });

                return rowsAffected > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure($"User with ID {userId} not found", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure($"Error hard deleting user with ID {userId}: {ex.Message}", DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Validation

        public async Task<bool> ExistsAsync( int userId, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Users WHERE UserId = @UserId"
                // language=tsql
                : "SELECT COUNT(1) FROM Users WHERE UserId = @UserId AND IsDeleted = 0";

            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    UserId = userId
                });
            return count > 0;
        }

        public async Task<bool> UsernameExistsAsync( string username, int? excludeUserId = null, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Users WHERE Username = @Username AND (@ExcludeUserId IS NULL OR UserId != @ExcludeUserId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Users WHERE Username = @Username AND IsDeleted = 0 AND (@ExcludeUserId IS NULL OR UserId != @ExcludeUserId)";

            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Username = username,
                    ExcludeUserId = excludeUserId
                });
            return count > 0;
        }

        public async Task<bool> EmailExistsAsync( string email, int? excludeUserId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Users WHERE Email = @Email AND (@ExcludeUserId IS NULL OR UserId != @ExcludeUserId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Users WHERE Email = @Email AND IsDeleted = 0 AND (@ExcludeUserId IS NULL OR UserId != @ExcludeUserId)";

            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Email = email,
                    ExcludeUserId = excludeUserId
                });
            return count > 0;
        }

        #endregion
    }
}
