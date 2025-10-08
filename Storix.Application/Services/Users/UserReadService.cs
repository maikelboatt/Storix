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
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    /// <summary>
    ///     Service responsible for user read operations with ISoftDeletable support.
    /// </summary>
    public class UserReadService(
        IUserRepository userRepository,
        IUserStore userStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<UserReadService> logger ):IUserReadService
    {
        public async Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId, bool includeDeleted = false )
        {
            if (userId <= 0)
            {
                logger.LogWarning("Invalid user ID {UserId} provided", userId);
                return DatabaseResult<UserDto?>.Failure(
                    "User ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<User?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(userId, includeDeleted),
                $"Retrieving user {userId} (includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved user with ID {UserId}", userId);
                return DatabaseResult<UserDto?>.Success(result.Value.ToDto());
            }

            if (result is { IsSuccess: true, Value: null })
            {
                logger.LogWarning("User with ID {UserId} not found", userId);
                return DatabaseResult<UserDto?>.Failure(
                    $"User with ID {userId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            logger.LogWarning("Failed to retrieve user {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
            return DatabaseResult<UserDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                logger.LogWarning("Null or empty username provided");
                return DatabaseResult<UserDto?>.Failure(
                    "Username cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<User?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByUsernameAsync(username, includeDeleted),
                $"Retrieving user by username '{username}' (includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved user with username '{Username}'", username);
                return DatabaseResult<UserDto?>.Success(result.Value.ToDto());
            }

            if (result.IsSuccess && result.Value == null)
            {
                logger.LogWarning("User with username '{Username}' not found", username);
                return DatabaseResult<UserDto?>.Failure(
                    $"User with username '{username}' not found.",
                    DatabaseErrorCode.NotFound);
            }

            logger.LogWarning("Failed to retrieve user by username '{Username}': {ErrorMessage}", username, result.ErrorMessage);
            return DatabaseResult<UserDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Null or empty email provided");
                return DatabaseResult<UserDto?>.Failure(
                    "Email cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<User?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByEmailAsync(email, includeDeleted),
                $"Retrieving user by email '{email}' (includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved user with email '{Email}'", email);
                return DatabaseResult<UserDto?>.Success(result.Value.ToDto());
            }

            if (result is { IsSuccess: true, Value: null })
            {
                logger.LogWarning("User with email '{Email}' not found", email);
                return DatabaseResult<UserDto?>.Failure(
                    $"User with email '{email}' not found.",
                    DatabaseErrorCode.NotFound);
            }

            logger.LogWarning("Failed to retrieve user by email '{Email}': {ErrorMessage}", email, result.ErrorMessage);
            return DatabaseResult<UserDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllAsync( bool includeDeleted = false )
        {
            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetAllAsync(includeDeleted),
                "Retrieving all users"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<UserDto> userDtos = result.Value.ToDto();

                if (!includeDeleted)
                    userStore.Initialize(result.Value.ToList());

                logger.LogInformation(
                    "Successfully retrieved {UserCount} users (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    includeDeleted);

                return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve users: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllActiveAsync()
        {
            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetAllActiveAsync,
                "Retrieving all active users"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {UserCount} active users", result.Value.Count());
                return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve active users: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync()
        {
            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                userRepository.GetAllDeletedAsync,
                "Retrieving all deleted users"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {UserCount} deleted users", result.Value.Count());
                return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve deleted users: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                logger.LogWarning("Null or empty role provided");
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByRoleAsync(role, includeDeleted),
                $"Retrieving users with role '{role}' (includeDeleted: {includeDeleted})"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {UserCount} users with role '{Role}' (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    role,
                    includeDeleted);
                return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve users by role '{Role}': {ErrorMessage}", role, result.ErrorMessage);
            return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            bool includeDeleted = false )
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
                () => userRepository.GetPagedAsync(pageNumber, pageSize, includeDeleted),
                $"Getting users page {pageNumber} with size {pageSize} (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of users ({UserCount} items, includeDeleted: {IncludeDeleted})",
                    pageNumber,
                    result.Value.Count(),
                    includeDeleted);
                return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve users page {PageNumber}: {ErrorMessage}", pageNumber, result.ErrorMessage);
            return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<UserDto>>.Success(Enumerable.Empty<UserDto>());
            }

            DatabaseResult<IEnumerable<User>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.SearchAsync(searchTerm.Trim(), includeDeleted),
                $"Searching users with term '{searchTerm}' (includeDeleted: {includeDeleted})"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Search for '{SearchTerm}' returned {UserCount} users (includeDeleted: {IncludeDeleted})",
                    searchTerm,
                    result.Value.Count(),
                    includeDeleted);
                return DatabaseResult<IEnumerable<UserDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to search users with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
            return DatabaseResult<IEnumerable<UserDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalCountAsync( bool includeDeleted = false )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetTotalCountAsync(includeDeleted),
                $"Getting total user count (includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total user count: {UserCount} (includeDeleted: {IncludeDeleted})", result.Value, includeDeleted);

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

        public async Task<DatabaseResult<int>> GetCountByRoleAsync( string role, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                logger.LogWarning("Null or empty role provided");
                return DatabaseResult<int>.Success(0);
            }

            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetCountByRoleAsync(role, includeDeleted),
                $"Getting user count for role '{role}' (includeDeleted: {includeDeleted})",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation(
                    "User count for role '{Role}': {UserCount} (includeDeleted: {IncludeDeleted})",
                    role,
                    result.Value,
                    includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
