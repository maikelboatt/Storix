using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Users;

namespace Storix.Application.Services.Users.Interfaces
{
    public interface IUserService
    {
        Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId, bool includeDeleted = false );

        Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username, bool includeDeleted = false );

        Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllAsync( bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllActiveAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm, bool includeDeleted = false );

        Task<DatabaseResult<int>> GetTotalCountAsync( bool includeDeleted = false );

        Task<DatabaseResult<int>> GetActiveCountAsync();

        Task<DatabaseResult<int>> GetDeletedCountAsync();

        Task<DatabaseResult<int>> GetCountByRoleAsync( string role, bool includeDeleted = false );

        Task<DatabaseResult<UserDto>> CreateUserAsync( CreateUserDto createUserDto );

        Task<DatabaseResult<UserDto>> UpdateUserAsync( UpdateUserDto updateUserDto );

        Task<DatabaseResult> ChangePasswordAsync( ChangePasswordDto changePasswordDto );

        Task<DatabaseResult> SoftDeleteUserAsync( int userId );

        Task<DatabaseResult> RestoreUserAsync( int userId );

        Task<DatabaseResult> HardDeleteUserAsync( int userId );

        Task<DatabaseResult<bool>> UserExistsAsync( int userId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> UsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            bool includeDeleted = false );

        Task<DatabaseResult<bool>> EmailExistsAsync(
            string email,
            int? excludeUserId = null,
            bool includeDeleted = false );

        Task<DatabaseResult> ValidateForDeletion( int userId );

        Task<DatabaseResult> ValidateForHardDeletion( int userId );

        Task<DatabaseResult> ValidateForRestore( int userId );

        Task<DatabaseResult<bool>> IsUserSoftDeleted( int userId );

        /// <summary>
        ///     Gets a user by ID from cache (fast).
        ///     Only returns if user is active (non-deleted).
        /// </summary>
        UserDto? GetByIdFromCache( int userId );

        /// <summary>
        ///     Gets a user by username from cache (fast).
        ///     Only returns if user is active (non-deleted).
        /// </summary>
        UserDto? GetByUsernameFromCache( string username );

        /// <summary>
        ///     Gets a user by email from cache (fast).
        ///     Only returns if user is active (non-deleted).
        /// </summary>
        UserDto? GetByEmailFromCache( string email );

        /// <summary>
        ///     Gets all active users from cache (fast).
        /// </summary>
        List<UserDto> GetAllFromCache();

        /// <summary>
        ///     Gets active users by role from cache (fast).
        /// </summary>
        List<UserDto> GetByRoleFromCache( string role );

        /// <summary>
        ///     Searches active users in the in-memory cache (fast).
        /// </summary>
        List<UserDto> SearchInCache( string searchTerm );

        /// <summary>
        ///     Checks if a user exists in the active cache (fast).
        /// </summary>
        bool ExistsInCache( int userId );

        /// <summary>
        ///     Checks if a username exists in the active cache (fast).
        /// </summary>
        bool UsernameExistsInCache( string username, int? excludeUserId = null );

        /// <summary>
        ///     Checks if an email exists in the active cache (fast).
        /// </summary>
        bool EmailExistsInCache( string email, int? excludeUserId = null );

        /// <summary>
        ///     Gets the count of active users in cache (fast).
        /// </summary>
        int GetCountFromCache();

        /// <summary>
        ///     Gets the count of active users by role from cache (fast).
        /// </summary>
        int GetCountByRoleFromCache( string role );

        /// <summary>
        ///     Refreshes the user cache from the database.
        ///     Loads only active users into memory.
        /// </summary>
        void RefreshStoreCache();

        /// <summary>
        ///     Soft deletes multiple users in bulk.
        ///     Each user is validated and deleted individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<UserDto>>> BulkSoftDeleteAsync( IEnumerable<int> userIds );

        /// <summary>
        ///     Restores multiple soft-deleted users in bulk.
        ///     Each user is validated and restored individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<UserDto>>> BulkRestoreAsync( IEnumerable<int> userIds );
    }
}
