using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Users.Interfaces;
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    /// <summary>
    /// Service responsible for user validation service with ISoftDeletable support.
    /// </summary>
    public class UserValidationService(
        IUserRepository userRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<UserValidationService> logger ):IUserValidationService
    {
        public async Task<DatabaseResult<bool>> UserExistAsync( int userId, bool includeDeleted = false )
        {
            if (userId <= 0) return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> exists = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.ExistsAsync(userId, includeDeleted),
                $"Checking if user {userId} exists in the database (includeDeleted: {includeDeleted})");

            return exists.IsSuccess
                ? DatabaseResult<bool>.Success(exists.Value)
                : DatabaseResult<bool>.Failure(exists.ErrorMessage!, exists.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> UsernameExistsAsync( string username, int? excludedId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(username)) return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> exists = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.UsernameExistsAsync(username, excludedId, includeDeleted),
                $"Checks if user with Username {username} exists in the database (excluded Id: {excludedId} include deleted: {includeDeleted})");

            return exists.IsSuccess
                ? DatabaseResult<bool>.Success(exists.Value)
                : DatabaseResult<bool>.Failure(exists.ErrorMessage!, exists.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> EmailExistsAsync( string email, int? excludedId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(email)) return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> exists = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.EmailExistsAsync(email, excludedId, includeDeleted),
                $"Checks if user with Email {email} exists in the database (excluded Id: {excludedId} include deleted: {includeDeleted})");

            return exists.IsSuccess
                ? DatabaseResult<bool>.Success(exists.Value)
                : DatabaseResult<bool>.Failure(exists.ErrorMessage!, exists.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForDeletion( int userId )
        {
            // Checks existence - only active users can be deleted.
            DatabaseResult exists = await ProofOfExistence(userId, false);
            if (!exists.IsSuccess)
                return exists;

            // Check business rules for soft deletion.
            DatabaseResult businessRulesResult = await ValidateUserDeletionBusinessRules(userId);

            return !businessRulesResult.IsSuccess
                ? businessRulesResult
                : DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int userId )
        {
            // Checks existence - including soft-deleted users for hard deletion
            DatabaseResult exists = await ProofOfExistence(userId, true);
            if (!exists.IsSuccess)
                return exists;

            // Check business rules for soft deletion.
            DatabaseResult businessRulesResult = await ValidateUserHardDeletionBusinessRules(userId);

            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogInformation("Hard deletion validation passed for user {UserId} - THIS WILL BE PERMANENT", userId);

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForRestore( int userId )
        {
            // Checks if user exists and is soft-deleted
            DatabaseResult existsResult = await ProofOfExistence(userId, true);
            if (!existsResult.IsSuccess)
                return existsResult;

            // Checks if user is actually deleted
            DatabaseResult<bool> isSoftDeletedResult = await IsUserSoftDeleted(userId);
            if (!isSoftDeletedResult.IsSuccess) return DatabaseResult.Failure(isSoftDeletedResult.ErrorMessage!, isSoftDeletedResult.ErrorCode);

            if (!isSoftDeletedResult.Value)
            {
                logger.LogWarning("Attempted to restore active user with ID {UserId}", userId);
                return DatabaseResult.Failure(
                    $"User with ID {userId} is not deleted and cannot be restored.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Check business rules for restoration
            DatabaseResult businessValidation = await ValidateUserRestorationBusinessRules(userId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult<bool>> IsUserSoftDeleted( int userId )
        {
            if (userId <= 0)
                return DatabaseResult<bool>.Success(false);

            // If user exists with includeDeleted = true but not with includedDeleted = false, it's soft-deleted
            DatabaseResult<bool> existsIncludingDeleted = await UserExistAsync(userId, true);
            if (!existsIncludingDeleted.IsSuccess || !existsIncludingDeleted.Value)
            {
                return existsIncludingDeleted.IsSuccess
                    ? DatabaseResult<bool>.Success(false)
                    : DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);
            }

            DatabaseResult<bool> existActiveOnly = await UserExistAsync(userId, false);
            if (!existActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existActiveOnly.ErrorMessage!, existActiveOnly.ErrorCode);

            // User exist in database but not in active results = soft deleted.
            bool isSoftDeleted = !existActiveOnly.Value;
            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #region Private Helper Methods

        private async Task<DatabaseResult> ProofOfExistence( int userId, bool includeDeleted = false )
        {
            // Checks existence
            DatabaseResult<bool> existResult = await UserExistAsync(userId, includeDeleted);

            // Query fails
            if (!existResult.IsSuccess)
                return DatabaseResult.Failure(existResult.ErrorMessage!, existResult.ErrorCode);

            // Query succeeds but user is not found
            if (!existResult.Value)
            {
                string statusMessage = includeDeleted
                    ? ""
                    : "or is deleted";
                logger.LogWarning("Attempted operation on non-existent user with ID {UserId} {StatusMessage}", userId, statusMessage);

                return DatabaseResult.Failure($"User with ID {userId} not found {statusMessage}", DatabaseErrorCode.NotFound);
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

            if (!userResult.IsSuccess || userResult.Value == null)
                return DatabaseResult.Failure("User not found for deletion validation", DatabaseErrorCode.NotFound);

            // Checks if user has role 'Admin'
            if (userResult.Value.Role == "Admin")
            {
                DatabaseResult<int> adminCountResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => userRepository.GetCountByRoleAsync("Admin", false),
                    "Checking admin user count");

                // Check if user is the last 'Admin'
                if (adminCountResult is { IsSuccess: true, Value: <= 1 })
                {
                    logger.LogWarning("Cannot deleted user {UserId} - last admin user", userId);

                    return DatabaseResult.Failure(
                        "Cannot delete the last administrator user. At least one admin user must exist",
                        DatabaseErrorCode.ConstraintViolation);
                }
            }
            return DatabaseResult.Success();

        }

        private async Task<DatabaseResult> ValidateUserHardDeletionBusinessRules( int userId )
        {
            logger.LogWarning("Validating hard deletion business rules for user {UserId}", userId);

            return await ValidateUserDeletionBusinessRules(userId);
        }

        private async Task<DatabaseResult> ValidateUserRestorationBusinessRules( int userId )
        {
            // Get user details
            DatabaseResult<User?> userResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(userId, true),
                $"Retrieving user {userId} for deletion validation",
                enableRetry: false);

            if (!userResult.IsSuccess || userResult.Value == null)
                return DatabaseResult.Failure("User not found for restoration validation", DatabaseErrorCode.NotFound);

            // Check for username conflicts with active users.
            DatabaseResult usernameCheck = await CheckUsernameConflict(userId, userResult);
            if (!usernameCheck.IsSuccess) return usernameCheck;

            // Check for email conflicts with active users.
            DatabaseResult emailCheck = await CheckEmailConflict(userId, userResult);
            if (!emailCheck.IsSuccess) return emailCheck;

            logger.LogInformation("User {UserId} passed all restoration business rule validations", userId);

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckUsernameConflict( int userId, DatabaseResult<User?> userResult )
        {

            DatabaseResult<bool> usernameConflictResult = await UsernameExistsAsync(userResult.Value!.Username, userId, false);

            if (!usernameConflictResult.IsSuccess)
                return DatabaseResult.Failure(usernameConflictResult.ErrorMessage!, usernameConflictResult.ErrorCode);

            // Active user exists with username.
            if (usernameConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore user {UserId} - username '{Username}' conflicts with existing active user",
                    userId,
                    userResult.Value.Username);
                return DatabaseResult.Failure(
                    $"Cannot restore user: Another active user with username '{userResult.Value.Username}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> CheckEmailConflict( int userId, DatabaseResult<User?> userResult )
        {
            string? email = userResult.Value!.Email;
            DatabaseResult<bool> emailConflictResult = await EmailExistsAsync(email!, userId, false);

            if (!emailConflictResult.IsSuccess)
                return DatabaseResult.Failure(emailConflictResult.ErrorMessage!, emailConflictResult.ErrorCode);

            // Active user exists with email.
            if (emailConflictResult.Value)
            {
                logger.LogWarning("Cannot restore user {UserId} - email '{Email}' conflicts with existing active user", userId, email);

                return DatabaseResult.Failure($"Cannot restore user: Another active user with email '{email}' already exists.", DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        #endregion
    }
}
