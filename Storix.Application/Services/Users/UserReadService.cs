using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Users;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Users.Interfaces;
using Storix.Application.Stores.Users;
using Storix.DataAccess.Repositories;
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    /// <summary>
    ///     Service responsible for user read operations with ISoftDeletable support.
    ///     This service returns all records (active + deleted) from the database.
    ///     For active-only records, use UserCacheReadService instead.
    /// </summary>
    public class UserReadService(
        IUserRepository userRepository,
        IUserStore userStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<UserReadService> logger ):IUserReadService
    {
        public async Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId )
        {
            if (userId <= 0)
            {
                logger.LogWarning("Invalid user ID {UserId} provided", userId);
                return DatabaseResult<UserDto?>.Failure(
                    "User ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<User?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(userId),
                $"Retrieving user {userId}",
                enableRetry: false
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve user {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
                return DatabaseResult<UserDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("User with ID {UserId} not found", userId);
                return DatabaseResult<UserDto?>.Failure(
                    $"User with ID {userId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved user with ID {UserId}", userId);
            return DatabaseResult<UserDto?>.Success(result.Value.ToDto());
        }

        public async Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username )
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                logger.LogWarning("Null or empty username provided");
                return DatabaseResult<UserDto?>.Failure(
                    "Username cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<User?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByUsernameAsync(username),
                $"Retrieving user by username '{username}'",
                enableRetry: false
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve user by username '{Username}': {ErrorMessage}", username, result.ErrorMessage);
                return DatabaseResult<UserDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("User with username '{Username}' not found", username);
                return DatabaseResult<UserDto?>.Failure(
                    $"User with username '{username}' not found.",
                    DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved user with username '{Username}'", username);
            return DatabaseResult<UserDto?>.Success(result.Value.ToDto());
        }

        public async Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Null or empty email provided");
                return DatabaseResult<UserDto?>.Failure(
                    "Email cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<User?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByEmailAsync(email),
                $"Retrieving user by email '{email}'",
                enableRetry: false
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve user by email '{Email}': {ErrorMessage}", email, result.ErrorMessage);
                return DatabaseResult<UserDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("User with email '{Email}' not found", email);
                return DatabaseResult<UserDto?>.Failure(
                    $"User with email '{email}' not found.",
                    DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved user with email '{Email}'", email);
            return DatabaseResult<UserDto?>.Success(result.Value.ToDto());
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllAsync()
        {
            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetAllAsync,
                "Retrieving all users"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve users: {ErrorMessage}", result.ErrorMessage);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No users found");
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            logger.LogInformation("Successfully retrieved {UserCount} users", result.Value.Count());
            return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.Select(u => u.ToDto()));
        }

        /// <summary>
        /// Retrieves all active (non-deleted) users from persistence
        /// and initializes the in-memory store with them.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<User>>> GetAllActiveUsersAsync()
        {
            return await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () =>
                {
                    // Fetch all users from persistence
                    IEnumerable<User> allUsers = await userRepository.GetAllAsync();

                    // Filter only active (non-deleted) users at the service layer
                    List<User> activeUsers = allUsers
                                             .Where(u => !u.IsDeleted)
                                             .ToList();

                    // Initialize the in-memory store with active users only
                    userStore.Initialize(activeUsers);

                    logger.LogInformation("Retrieved and cached {Count} active users", activeUsers.Count);
                    return (IEnumerable<User>)activeUsers;
                },
                "Retrieving active users");
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync()
        {
            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetAllAsync,
                "Retrieving all deleted users"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve deleted users: {ErrorMessage}", result.ErrorMessage);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No deleted users found");
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            // Filter to only deleted users at service layer
            IEnumerable<User> deletedUsers = result
                                             .Value.Where(u => u.IsDeleted)
                                             .ToList();

            logger.LogInformation("Successfully retrieved {UserCount} deleted users", deletedUsers.Count());
            return DatabaseResult<IEnumerable<UserDto>>.Success(deletedUsers.Select(u => u.ToDto()));
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role )
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                logger.LogWarning("Null or empty role provided");
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByRoleAsync(role),
                $"Retrieving users with role '{role}'"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve users by role '{Role}': {ErrorMessage}", role, result.ErrorMessage);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No users found for role '{Role}'", role);
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            logger.LogInformation(
                "Successfully retrieved {UserCount} users with role '{Role}'",
                result.Value.Count(),
                role);

            return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.Select(u => u.ToDto()));
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting users page {pageNumber} with size {pageSize}"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve users page {PageNumber}: {ErrorMessage}", pageNumber, result.ErrorMessage);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No users found for page {PageNumber}", pageNumber);
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            logger.LogInformation(
                "Successfully retrieved page {PageNumber} of users ({UserCount} items)",
                pageNumber,
                result.Value.Count());

            return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.Select(u => u.ToDto()));
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.SearchAsync(searchTerm.Trim()),
                $"Searching users with term '{searchTerm}'"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to search users with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
                return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No users found for search term '{SearchTerm}'", searchTerm);
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            logger.LogInformation(
                "Search for '{SearchTerm}' returned {UserCount} users",
                searchTerm,
                result.Value.Count());

            return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.Select(u => u.ToDto()));
        }

        public async Task<DatabaseResult<int>> GetTotalCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetTotalCountAsync,
                "Getting total user count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total user count: {UserCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetActiveCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetActiveCountAsync,
                "Getting active user count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Active user count: {UserCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetDeletedCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetDeletedCountAsync,
                "Getting deleted user count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Deleted user count: {UserCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetCountByRoleAsync( string role )
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                logger.LogWarning("Null or empty role provided");
                return DatabaseResult<int>.Success(0);
            }

            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetCountByRoleAsync(role),
                $"Getting user count for role '{role}'",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("User count for role '{Role}': {UserCount}", role, result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
