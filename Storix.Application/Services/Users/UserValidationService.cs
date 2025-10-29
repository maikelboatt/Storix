using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Users.Interfaces;
using Storix.DataAccess.Repositories;
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    /// <summary>
    /// Service responsible for user validation with ISoftDeletable support.
    /// Handles all validation logic including existence checks, business rules, and constraints.
    /// </summary>
    public class UserValidationService(
        IUserRepository userRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<UserValidationService> logger ):IUserValidationService
    {
        #region Existence Validation

        public async Task<DatabaseResult<bool>> UserExistAsync( int userId, bool includeDeleted = false )
        {
            if (userId <= 0)
            {
                logger.LogDebug("Invalid user ID {UserId} provided for existence check", userId);
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.ExistsAsync(userId, includeDeleted),
                $"Checking if user {userId} exists (includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (result.IsSuccess)
                logger.LogDebug(
                    "User {UserId} exists: {Exists} (includeDeleted: {IncludeDeleted})",
                    userId,
                    result.Value,
                    includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> UsernameExistsAsync( string username, int? excludedId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                logger.LogDebug("Empty username provided for existence check");
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.UsernameExistsAsync(username, excludedId, includeDeleted),
                $"Checking if username '{username}' exists (excludedId: {excludedId}, includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (result.IsSuccess)
                logger.LogDebug(
                    "Username '{Username}' exists: {Exists} (excludedId: {ExcludedId}, includeDeleted: {IncludeDeleted})",
                    username,
                    result.Value,
                    excludedId,
                    includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> EmailExistsAsync( string email, int? excludedId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogDebug("Empty email provided for existence check");
                return DatabaseResult<bool>.Success(false);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.EmailExistsAsync(email, excludedId, includeDeleted),
                $"Checking if email '{email}' exists (excludedId: {excludedId}, includeDeleted: {includeDeleted})",
                enableRetry: false);

            if (result.IsSuccess)
                logger.LogDebug(
                    "Email '{Email}' exists: {Exists} (excludedId: {ExcludedId}, includeDeleted: {IncludeDeleted})",
                    email,
                    result.Value,
                    excludedId,
                    includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> IsUserSoftDeleted( int userId )
        {
            if (userId <= 0)
            {
                logger.LogDebug("Invalid user ID {UserId} provided for soft-delete check", userId);
                return DatabaseResult<bool>.Success(false);
            }

            // Check if user exists at all (including deleted)
            DatabaseResult<bool> existsIncludingDeleted = await UserExistAsync(userId, true);
            if (!existsIncludingDeleted.IsSuccess)
                return DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);

            if (!existsIncludingDeleted.Value)
            {
                logger.LogDebug("User {UserId} does not exist in database", userId);
                return DatabaseResult<bool>.Success(false);
            }

            // Check if user exists in active records
            DatabaseResult<bool> existsActiveOnly = await UserExistAsync(userId, false);
            if (!existsActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existsActiveOnly.ErrorMessage!, existsActiveOnly.ErrorCode);

            // User exists in database but not in active results = soft deleted
            bool isSoftDeleted = !existsActiveOnly.Value;
            logger.LogDebug("User {UserId} soft-delete status: {IsSoftDeleted}", userId, isSoftDeleted);

            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #endregion

        #region Deletion Validation

        public async Task<DatabaseResult> ValidateForDeletion( int userId )
        {
            logger.LogInformation("Validating soft deletion for user {UserId}", userId);

            // Check existence - only active users can be soft-deleted
            DatabaseResult existsResult = await ValidateExistence(userId, false);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check business rules for soft deletion
            DatabaseResult businessRulesResult = await ValidateUserDeletionBusinessRules(userId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Soft deletion validation passed for user {UserId}", userId);
            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int userId )
        {
            logger.LogWarning("Validating hard deletion for user {UserId} - THIS WILL BE PERMANENT", userId);

            // Check existence - including soft-deleted users for hard deletion
            DatabaseResult existsResult = await ValidateExistence(userId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check business rules for hard deletion
            DatabaseResult businessRulesResult = await ValidateUserDeletionBusinessRules(userId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Hard deletion validation passed for user {UserId}", userId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Restoration Validation

        public async Task<DatabaseResult> ValidateForRestore( int userId )
        {
            logger.LogInformation("Validating restoration for user {UserId}", userId);

            // Check if user exists (including deleted)
            DatabaseResult existsResult = await ValidateExistence(userId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Check if user is actually soft-deleted
            DatabaseResult<bool> isSoftDeletedResult = await IsUserSoftDeleted(userId);
            if (!isSoftDeletedResult.IsSuccess)
                return DatabaseResult.Failure(isSoftDeletedResult.ErrorMessage!, isSoftDeletedResult.ErrorCode);

            if (!isSoftDeletedResult.Value)
            {
                logger.LogWarning("Attempted to restore active user {UserId}", userId);
                return DatabaseResult.Failure(
                    $"User with ID {userId} is not deleted and cannot be restored.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Check business rules for restoration
            DatabaseResult businessValidation = await ValidateUserRestorationBusinessRules(userId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            logger.LogInformation("Restoration validation passed for user {UserId}", userId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Private Helper Methods

        private async Task<DatabaseResult> ValidateExistence( int userId, bool includeDeleted )
        {
            DatabaseResult<bool> existResult = await UserExistAsync(userId, includeDeleted);

            if (!existResult.IsSuccess)
            {
                logger.LogError("Failed to check existence for user {UserId}: {ErrorMessage}", userId, existResult.ErrorMessage);
                return DatabaseResult.Failure(existResult.ErrorMessage!, existResult.ErrorCode);
            }

            if (!existResult.Value)
            {
                string statusMessage = includeDeleted
                    ? ""
                    : " or is deleted";
                logger.LogWarning("User {UserId} not found{StatusMessage}", userId, statusMessage);
                return DatabaseResult.Failure(
                    $"User with ID {userId} not found{statusMessage}.",
                    DatabaseErrorCode.NotFound);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateUserDeletionBusinessRules( int userId )
        {
            // Get user details
            DatabaseResult<User?> userResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(userId),
                $"Retrieving user {userId} for deletion validation",
                enableRetry: false);

            if (!userResult.IsSuccess)
            {
                logger.LogError("Failed to retrieve user {UserId} for deletion validation: {ErrorMessage}", userId, userResult.ErrorMessage);
                return DatabaseResult.Failure(userResult.ErrorMessage!, userResult.ErrorCode);
            }

            if (userResult.Value == null)
            {
                logger.LogWarning("User {UserId} not found during deletion validation", userId);
                return DatabaseResult.Failure(
                    $"User with ID {userId} not found for deletion validation.",
                    DatabaseErrorCode.NotFound);
            }

            // Check if user is an Admin
            if (userResult.Value.Role != "Admin")
            {
                logger.LogDebug("User {UserId} is not an admin, deletion allowed", userId);
                return DatabaseResult.Success();
            }

            // User is an Admin - check if they're the last one
            DatabaseResult<int> adminCountResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetCountByRoleAsync("Admin"),
                "Checking admin user count for deletion validation",
                enableRetry: false);

            if (!adminCountResult.IsSuccess)
            {
                logger.LogError("Failed to get admin count: {ErrorMessage}", adminCountResult.ErrorMessage);
                return DatabaseResult.Failure(
                    "Unable to verify admin count before deletion. Operation cancelled for safety.",
                    DatabaseErrorCode.UnexpectedError);
            }

            if (adminCountResult.Value <= 1)
            {
                logger.LogWarning("Cannot delete user {UserId} - last admin user (count: {AdminCount})", userId, adminCountResult.Value);
                return DatabaseResult.Failure(
                    "Cannot delete the last administrator user. At least one admin must exist in the system.",
                    DatabaseErrorCode.ConstraintViolation);
            }

            logger.LogDebug("Admin deletion allowed for user {UserId} (remaining admins: {AdminCount})", userId, adminCountResult.Value - 1);
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateUserRestorationBusinessRules( int userId )
        {
            // Get user details
            DatabaseResult<User?> userResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(userId),
                $"Retrieving user {userId} for restoration validation",
                enableRetry: false);

            if (!userResult.IsSuccess)
            {
                logger.LogError("Failed to retrieve user {UserId} for restoration validation: {ErrorMessage}", userId, userResult.ErrorMessage);
                return DatabaseResult.Failure(userResult.ErrorMessage!, userResult.ErrorCode);
            }

            if (userResult.Value == null)
            {
                logger.LogWarning("User {UserId} not found during restoration validation", userId);
                return DatabaseResult.Failure(
                    $"User with ID {userId} not found for restoration validation.",
                    DatabaseErrorCode.NotFound);
            }

            // Check for username conflicts with active users
            DatabaseResult usernameCheck = await CheckUsernameConflict(userId, userResult.Value);
            if (!usernameCheck.IsSuccess)
                return usernameCheck;

            // Check for email conflicts with active users
            DatabaseResult emailCheck = await CheckEmailConflict(userId, userResult.Value);
            if (!emailCheck.IsSuccess)
                return emailCheck;

            logger.LogInformation("User {UserId} passed all restoration business rule validations", userId);
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckUsernameConflict( int userId, User user )
        {
            DatabaseResult<bool> usernameConflictResult = await UsernameExistsAsync(
                user.Username,
                userId,
                false);

            if (!usernameConflictResult.IsSuccess)
            {
                logger.LogError("Failed to check username conflict for user {UserId}: {ErrorMessage}", userId, usernameConflictResult.ErrorMessage);
                return DatabaseResult.Failure(usernameConflictResult.ErrorMessage!, usernameConflictResult.ErrorCode);
            }

            if (usernameConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore user {UserId} - username '{Username}' conflicts with existing active user",
                    userId,
                    user.Username);
                return DatabaseResult.Failure(
                    $"Cannot restore user: Another active user with username '{user.Username}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckEmailConflict( int userId, User user )
        {
            // Email is optional, so if it's null or empty, there's no conflict
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                logger.LogDebug("User {UserId} has no email, skipping email conflict check", userId);
                return DatabaseResult.Success();
            }

            DatabaseResult<bool> emailConflictResult = await EmailExistsAsync(
                user.Email,
                userId,
                false);

            if (!emailConflictResult.IsSuccess)
            {
                logger.LogError("Failed to check email conflict for user {UserId}: {ErrorMessage}", userId, emailConflictResult.ErrorMessage);
                return DatabaseResult.Failure(emailConflictResult.ErrorMessage!, emailConflictResult.ErrorCode);
            }

            if (emailConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore user {UserId} - email '{Email}' conflicts with existing active user",
                    userId,
                    user.Email);
                return DatabaseResult.Failure(
                    $"Cannot restore user: Another active user with email '{user.Email}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        #endregion
    }
}
