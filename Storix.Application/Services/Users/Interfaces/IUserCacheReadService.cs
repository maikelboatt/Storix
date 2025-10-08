using System.Collections.Generic;
using Storix.Application.DTO.Users;

namespace Storix.Application.Services.Users.Interfaces
{
    public interface IUserCacheReadService
    {
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
    }
}
