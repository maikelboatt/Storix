using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Users;
using Storix.Application.Services.Users.Interfaces;
using Storix.Application.Stores.Users;

namespace Storix.Application.Services.Users
{
    public class UserCacheReadService( IUserStore userStore, IUserReadService userReadService, ILogger<UserCacheReadService> logger ):IUserCacheReadService
    {
        /// <summary>
        ///     Gets a user by ID from cache (fast).
        ///     Only returns if user is active (non-deleted).
        /// </summary>
        public UserDto? GetByIdFromCache( int userId )
        {
            logger.LogDebug("Retrieving user {UserId} from cache", userId);
            return userStore.GetById(userId);
        }

        /// <summary>
        ///     Gets a user by username from cache (fast).
        ///     Only returns if user is active (non-deleted).
        /// </summary>
        public UserDto? GetByUsernameFromCache( string username )
        {
            logger.LogDebug("Retrieving user with username '{Username}' from cache", username);
            return userStore.GetByUsername(username);
        }

        /// <summary>
        ///     Gets a user by email from cache (fast).
        ///     Only returns if user is active (non-deleted).
        /// </summary>
        public UserDto? GetByEmailFromCache( string email )
        {
            logger.LogDebug("Retrieving user with email '{Email}' from cache", email);
            return userStore.GetByEmail(email);
        }

        /// <summary>
        ///     Gets all active users from cache (fast).
        /// </summary>
        public List<UserDto> GetAllFromCache()
        {
            logger.LogDebug("Retrieving all active users from cache");
            return userStore.GetAll();
        }

        /// <summary>
        ///     Gets active users by role from cache (fast).
        /// </summary>
        public List<UserDto> GetByRoleFromCache( string role )
        {
            logger.LogDebug("Retrieving active users from cache for role '{Role}'", role);
            return userStore.GetByRole(role);
        }

        /// <summary>
        ///     Searches active users in the in-memory cache (fast).
        /// </summary>
        public List<UserDto> SearchInCache( string searchTerm )
        {
            logger.LogDebug("Searching active users in cache with term '{SearchTerm}'", searchTerm);
            return userStore.Search(searchTerm);
        }

        /// <summary>
        ///     Checks if a user exists in the active cache (fast).
        /// </summary>
        public bool ExistsInCache( int userId ) => userStore.Exists(userId);

        /// <summary>
        ///     Checks if a username exists in the active cache (fast).
        /// </summary>
        public bool UsernameExistsInCache( string username, int? excludeUserId = null ) => userStore.UsernameExists(username, excludeUserId);

        /// <summary>
        ///     Checks if an email exists in the active cache (fast).
        /// </summary>
        public bool EmailExistsInCache( string email, int? excludeUserId = null ) => userStore.EmailExists(email, excludeUserId);

        /// <summary>
        ///     Gets the count of active users in cache (fast).
        /// </summary>
        public int GetCountFromCache() => userStore.GetCount();

        /// <summary>
        ///     Gets the count of active users by role from cache (fast).
        /// </summary>
        public int GetCountByRoleFromCache( string role ) => userStore.GetCountByRole(role);

        /// <summary>
        ///     Refreshes the user cache from the database.
        ///     Loads only active users into memory.
        /// </summary>
        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating user store cache refresh (active users only)");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Get only active users from database
                    DatabaseResult<IEnumerable<UserDto>> result = await userReadService.GetAllActiveAsync();

                    if (result.IsSuccess && result.Value != null)
                    {
                        logger.LogInformation(
                            "User store cache refreshed successfully with {Count} active users",
                            result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning(
                            "Failed to refresh user store cache: {Error}",
                            result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing user store cache");
                }
            });
        }
    }
}
