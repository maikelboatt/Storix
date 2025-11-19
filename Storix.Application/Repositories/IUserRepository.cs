using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by its unique ID.
        /// </summary>
        Task<User?> GetByIdAsync( int userId, bool includeDeleted = true );

        /// <summary>
        /// Retrieves a user by username (includes deleted users).
        /// </summary>
        Task<User?> GetByUsernameAsync( string username );

        /// <summary>
        /// Retrieves a user by email (includes deleted users).
        /// </summary>
        Task<User?> GetByEmailAsync( string email );

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        Task<IEnumerable<User>> GetAllAsync( bool includeDeleted = true );

        /// <summary>
        /// Retrieves all users for a given role (includes deleted).
        /// </summary>
        Task<IEnumerable<User>> GetByRoleAsync( string role );

        /// <summary>
        /// Retrieves a paginated list of users (includes deleted).
        /// </summary>
        Task<IEnumerable<User>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        /// Returns the total number of users (active + deleted).
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Returns the number of active (non-deleted) users.
        /// </summary>
        Task<int> GetActiveCountAsync();

        /// <summary>
        /// Returns the number of soft-deleted users.
        /// </summary>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        /// Returns the total number of users within a specific role (active + deleted).
        /// </summary>
        Task<int> GetCountByRoleAsync( string role );

        /// <summary>
        /// Searches users by username, full name, or email (includes deleted).
        /// </summary>
        Task<IEnumerable<User>> SearchAsync( string searchTerm );

        /// <summary>
        /// Creates a new user record in the database.
        /// </summary>
        Task<User> CreateAsync( User user );

        /// <summary>
        /// Updates an existing user record in the database.
        /// </summary>
        Task<User> UpdateAsync( User user );

        /// <summary>
        /// Marks a user as deleted without removing the record (soft delete).
        /// </summary>
        Task<DatabaseResult> SoftDeleteAsync( int userId );

        /// <summary>
        /// Restores a soft-deleted user (sets IsDeleted to false).
        /// </summary>
        Task<DatabaseResult> RestoreAsync( int userId );

        /// <summary>
        /// Permanently removes a user from the database.
        /// </summary>
        Task<DatabaseResult> HardDeleteAsync( int userId );

        /// <summary>
        /// Checks if a user exists by ID. 
        /// Optionally includes soft-deleted users.
        /// </summary>
        Task<bool> ExistsAsync( int userId, bool includeDeleted = false );

        /// <summary>
        /// Checks if a username is already in use. 
        /// Optionally includes deleted users (used in validation).
        /// </summary>
        Task<bool> UsernameExistsAsync( string username, int? excludeUserId = null, bool includeDeleted = false );

        /// <summary>
        /// Checks if an email address is already associated with another user.
        /// Optionally includes deleted users (used in validation).
        /// </summary>
        Task<bool> EmailExistsAsync( string email, int? excludeUserId = null, bool includeDeleted = false );
    }
}
