using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Users;
using Storix.Application.Enums;
using Storix.Application.Services.Users.Interfaces;
using Storix.Application.Stores.Users;
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    /// <summary>
    ///     Main facade service for managing user operations with ISoftDeletable support.
    ///     Combines read, write, and validation services with in-memory cache for performance.
    /// </summary>
    public class UserService(
        IUserReadService userReadService,
        IUserCacheReadService userCacheReadService,
        IUserWriteService userWriteService,
        IUserValidationService userValidationService,
        ILogger<UserService> logger ):IUserService
    {
        #region Read Operations (Database Queries)

        public async Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId ) => await userReadService.GetByIdAsync(userId);

        public async Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username ) => await userReadService.GetByUsernameAsync(username);

        public async Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email, bool includeDeleted = false ) =>
            await userReadService.GetByEmailAsync(email);

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllAsync() => await userReadService.GetAllAsync();

        public async Task<DatabaseResult<IEnumerable<User>>> GetAllActiveAsync() => await userReadService.GetAllActiveUsersAsync();

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync() => await userReadService.GetAllDeletedAsync();

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role ) => await userReadService.GetByRoleAsync(role);

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync(
            int pageNumber,
            int pageSize
        ) => await userReadService.GetPagedAsync(pageNumber, pageSize);

        public async Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm ) => await userReadService.SearchAsync(searchTerm);

        public async Task<DatabaseResult<int>> GetTotalCountAsync() => await userReadService.GetTotalCountAsync();

        public async Task<DatabaseResult<int>> GetActiveCountAsync() => await userReadService.GetActiveCountAsync();

        public async Task<DatabaseResult<int>> GetDeletedCountAsync() => await userReadService.GetDeletedCountAsync();

        public async Task<DatabaseResult<int>> GetCountByRoleAsync( string role ) => await userReadService.GetCountByRoleAsync(role);

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<UserDto>> CreateUserAsync( CreateUserDto createUserDto ) => await userWriteService.CreateUserAsync(createUserDto);

        public async Task<DatabaseResult<UserDto>> UpdateUserAsync( UpdateUserDto updateUserDto ) => await userWriteService.UpdateUserAsync(updateUserDto);

        public async Task<DatabaseResult> ChangePasswordAsync( ChangePasswordDto changePasswordDto ) =>
            await userWriteService.ChangePasswordAsync(changePasswordDto);

        public async Task<DatabaseResult> SoftDeleteUserAsync( int userId ) => await userWriteService.SoftDeleteUserAsync(userId);

        public async Task<DatabaseResult> RestoreUserAsync( int userId ) => await userWriteService.RestoreUserAsync(userId);

        public async Task<DatabaseResult> HardDeleteUserAsync( int userId ) => await userWriteService.HardDeleteUserAsync(userId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> UserExistsAsync( int userId, bool includeDeleted = false ) =>
            await userValidationService.UserExistAsync(userId, includeDeleted);

        public async Task<DatabaseResult<bool>> UsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            bool includeDeleted = false
        ) => await userValidationService.UsernameExistsAsync(username, excludeUserId, includeDeleted);

        public async Task<DatabaseResult<bool>> EmailExistsAsync(
            string email,
            int? excludeUserId = null,
            bool includeDeleted = false
        ) => await userValidationService.EmailExistsAsync(email, excludeUserId, includeDeleted);

        public async Task<DatabaseResult> ValidateForDeletion( int userId ) => await userValidationService.ValidateForDeletion(userId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int userId ) => await userValidationService.ValidateForHardDeletion(userId);

        public async Task<DatabaseResult> ValidateForRestore( int userId ) => await userValidationService.ValidateForRestore(userId);

        public async Task<DatabaseResult<bool>> IsUserSoftDeleted( int userId ) => await userValidationService.IsUserSoftDeleted(userId);

        #endregion

        #region Cache Operations (Fast In-Memory Queries - Active Users Only)

        public UserDto? GetByIdFromCache( int userId ) => userCacheReadService.GetByIdFromCache(userId);

        public UserDto? GetByUsernameFromCache( string username ) => userCacheReadService.GetByUsernameFromCache(username);

        public UserDto? GetByEmailFromCache( string email ) => userCacheReadService.GetByEmailFromCache(email);

        public List<UserDto> GetAllFromCache() => userCacheReadService.GetAllFromCache();

        public List<UserDto> GetByRoleFromCache( string role ) => userCacheReadService.GetByRoleFromCache(role);

        public List<UserDto> SearchInCache( string searchTerm ) => userCacheReadService.SearchInCache(searchTerm);

        public bool ExistsInCache( int userId ) => userCacheReadService.ExistsInCache(userId);

        public bool UsernameExistsInCache( string username, int? excludeUserId = null ) => userCacheReadService.UsernameExistsInCache(username, excludeUserId);

        public bool EmailExistsInCache( string email, int? excludeUserId = null ) => userCacheReadService.EmailExistsInCache(email, excludeUserId);

        public int GetCountFromCache() => userCacheReadService.GetCountFromCache();

        public int GetCountByRoleFromCache( string role ) => userCacheReadService.GetCountByRoleFromCache(role);

        public void RefreshStoreCache() => userCacheReadService.RefreshStoreCache();

        #endregion

        #region Bulk Operations

        /// <summary>
        ///     Soft deletes multiple users in bulk.
        ///     Each user is validated and deleted individually.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<UserDto>>> BulkSoftDeleteAsync( IEnumerable<int> userIds )
        {
            List<int> userIdList = userIds.ToList();
            logger.LogInformation("Starting bulk soft delete for {Count} users", userIdList.Count);

            List<string> errors = new();

            foreach (int userId in userIdList)
            {
                DatabaseResult result = await SoftDeleteUserAsync(userId);
                if (!result.IsSuccess)
                {
                    errors.Add($"User {userId}: {result.ErrorMessage}");
                    logger.LogWarning(
                        "Failed to soft delete user {UserId}: {Error}",
                        userId,
                        result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(
                    $"Bulk soft delete completed with {errors.Count} error(s): {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation(
                "Bulk soft delete completed successfully for {Count} users",
                userIdList.Count);
            return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
        }

        /// <summary>
        ///     Restores multiple soft-deleted users in bulk.
        ///     Each user is validated and restored individually.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<UserDto>>> BulkRestoreAsync( IEnumerable<int> userIds )
        {
            List<int> userIdList = userIds.ToList();
            logger.LogInformation("Starting bulk restore for {Count} users", userIdList.Count);

            List<UserDto> restored = new();
            List<string> errors = new();

            foreach (int userId in userIdList)
            {
                DatabaseResult result = await RestoreUserAsync(userId);
                if (!result.IsSuccess)
                {
                    errors.Add($"User {userId}: {result.ErrorMessage}");
                    logger.LogWarning(
                        "Failed to restore user {UserId}: {Error}",
                        userId,
                        result.ErrorMessage);
                }
                else
                {
                    // Get the restored user from cache
                    UserDto? restoredUser = GetByIdFromCache(userId);
                    if (restoredUser != null)
                    {
                        restored.Add(restoredUser);
                    }
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(
                    $"Bulk restore completed with {errors.Count} error(s): {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation(
                "Bulk restore completed successfully for {Count} users",
                userIdList.Count);
            return DatabaseResult<IEnumerable<UserDto>>.Success(restored);
        }

        #endregion
    }
}
