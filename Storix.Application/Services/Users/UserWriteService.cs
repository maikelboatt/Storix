using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
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
    ///     Service responsible for user write operations with ISoftDeletable support.
    /// </summary>
    public class UserWriteService(
        IUserRepository userRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        IUserValidationService userValidationService,
        IUserStore userStore,
        IPasswordHasher passwordHasher,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator,
        IValidator<ChangePasswordDto> changePasswordValidator,
        ILogger<UserWriteService> logger ):IUserWriteService
    {
        public async Task<DatabaseResult<UserDto>> CreateUserAsync( CreateUserDto createUserDto )
        {
            // Input validation
            DatabaseResult<UserDto> inputValidation = ValidateCreateInput(createUserDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<UserDto> businessValidation = await ValidateCreateBusiness(createUserDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create user
            return await PerformCreate(createUserDto);
        }

        public async Task<DatabaseResult<UserDto>> UpdateUserAsync( UpdateUserDto updateUserDto )
        {
            // Input validation
            DatabaseResult<UserDto> inputValidation = ValidateUpdateInput(updateUserDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<UserDto> businessValidation = await ValidateUpdateBusiness(updateUserDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform update
            return await PerformUpdate(updateUserDto);
        }

        public async Task<DatabaseResult> ChangePasswordAsync( ChangePasswordDto changePasswordDto )
        {
            // Input validation
            DatabaseResult inputValidation = ValidateChangePasswordInput(changePasswordDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Perform password change
            return await PerformChangePassword(changePasswordDto);
        }

        public async Task<DatabaseResult> SoftDeleteUserAsync( int userId )
        {
            // Input validation
            if (userId <= 0)
            {
                logger.LogWarning("Invalid user ID {UserId} provided for soft deletion", userId);
                return DatabaseResult.Failure("User ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await userValidationService.ValidateForDeletion(userId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform soft deletion
            return await PerformSoftDelete(userId);
        }

        public async Task<DatabaseResult> RestoreUserAsync( int userId )
        {
            // Input validation
            if (userId <= 0)
            {
                logger.LogWarning("Invalid user ID {UserId} provided for restoration", userId);
                return DatabaseResult.Failure("User ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await userValidationService.ValidateForRestore(userId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform restoration
            return await PerformRestore(userId);
        }

        public async Task<DatabaseResult> HardDeleteUserAsync( int userId )
        {
            // Input validation
            if (userId <= 0)
            {
                logger.LogWarning("Invalid user ID {UserId} provided for hard deletion", userId);
                return DatabaseResult.Failure("User ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await userValidationService.ValidateForHardDeletion(userId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform hard deletion
            return await PerformHardDelete(userId);
        }

        #region Helper Methods

        private async Task<DatabaseResult<UserDto>> PerformCreate( CreateUserDto createUserDto )
        {
            // Hash the password
            string hashedPassword = passwordHasher.Hash(createUserDto.Password);

            // Convert DTO to domain model with hashed password
            User user = createUserDto.ToDomain();
            user = user.WithNewPassword(hashedPassword);

            DatabaseResult<User> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.CreateAsync(user),
                "Creating new user"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                int userId = result.Value.UserId;

                // Add to in-memory store
                UserDto? storeResult = userStore.Create(userId, createUserDto);


                if (storeResult == null)
                {
                    logger.LogWarning("User created in database (ID: {UserId}) but failed to ad to cache", userId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully created user with ID {UserId} and username '{Username}'",
                        result.Value.UserId,
                        result.Value.Username);
                }

                UserDto userDto = result.Value.ToDto();

                return DatabaseResult<UserDto>.Success(userDto);
            }

            logger.LogWarning("Failed to create user: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<UserDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<UserDto>> PerformUpdate( UpdateUserDto updateUserDto )
        {
            // Get existing user (only active ones - can't update deleted users)
            DatabaseResult<User?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(updateUserDto.UserId, false),
                $"Retrieving user {updateUserDto.UserId} for update",
                enableRetry: false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning(
                    "Cannot update user {UserId}: {ErrorMessage}",
                    updateUserDto.UserId,
                    getResult.ErrorMessage ?? "User not found or is deleted");
                return DatabaseResult<UserDto>.Failure(
                    getResult.ErrorMessage ?? "User not found or is deleted. Restore the user first if it was deleted.",
                    getResult.ErrorCode);
            }

            User existingUser = getResult.Value;

            // Update user while preserving password and soft delete properties
            User updatedUser = updateUserDto.ToDomain(
                existingUser.Password,
                existingUser.IsDeleted,
                existingUser.DeletedAt);

            DatabaseResult<User> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.UpdateAsync(updatedUser),
                "Updating user"
            );

            if (updateResult is { IsSuccess: true, Value: not null })
            {
                // Updates in active cache
                UserDto? storeResult = userStore.Update(updateUserDto);

                if (storeResult == null)
                {
                    logger.LogWarning("User updated in database (ID: {UserId}) but failed to ad to cache", updateUserDto.UserId);
                }
                else
                {
                    logger.LogInformation("Successfully updated user with ID {UserId}", updateUserDto.UserId);
                }

                return DatabaseResult<UserDto>.Success(updateResult.Value.ToDto());
            }

            logger.LogWarning(
                "Failed to update user with ID {UserId}: {ErrorMessage}",
                updateUserDto.UserId,
                updateResult.ErrorMessage);
            return DatabaseResult<UserDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformChangePassword( ChangePasswordDto changePasswordDto )
        {
            // Get existing user
            DatabaseResult<User?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.GetByIdAsync(changePasswordDto.UserId, false),
                $"Retrieving user {changePasswordDto.UserId} for password change",
                enableRetry: false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning(
                    "Cannot change password for user {UserId}: User not found or is deleted",
                    changePasswordDto.UserId);
                return DatabaseResult.Failure(
                    "User not found or is deleted.",
                    getResult.ErrorCode);
            }

            User existingUser = getResult.Value;

            // Verify current password
            if (!passwordHasher.Verify(changePasswordDto.CurrentPassword, existingUser.Password))
            {
                logger.LogWarning("Invalid current password provided for user {UserId}", changePasswordDto.UserId);
                return DatabaseResult.Failure(
                    "Current password is incorrect.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Hash new password
            string newHashedPassword = passwordHasher.Hash(changePasswordDto.NewPassword);

            // Update user with new password
            User updatedUser = existingUser.WithNewPassword(newHashedPassword);

            DatabaseResult<User> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => userRepository.UpdateAsync(updatedUser),
                "Updating user password"
            );

            if (updateResult.IsSuccess)
            {
                logger.LogInformation("Successfully changed password for user {UserId}", changePasswordDto.UserId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to change password for user {UserId}: {ErrorMessage}",
                changePasswordDto.UserId,
                updateResult.ErrorMessage);
            return DatabaseResult.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformSoftDelete( int userId )
        {
            DatabaseResult result = await userRepository.SoftDeleteAsync(userId);

            if (result.IsSuccess)
            {
                // Updates in active cache
                bool removed = userStore.Delete(userId);

                if (!removed)
                {
                    logger.LogWarning("User soft deleted in database (ID: {UserId}) but wasn't found in active cache", userId);
                }
                else
                {

                    logger.LogInformation("Successfully soft deleted user with ID {UserId}", userId);
                }
                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to soft delete user with ID {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to soft delete user",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int userId )
        {
            DatabaseResult result = await userRepository.RestoreAsync(userId);

            if (result.IsSuccess)
            {
                // Fetch the restored user from database
                DatabaseResult<User?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => userRepository.GetByIdAsync(userId, false),
                    $"Retrieving restored user {userId}");

                if (getResult is { IsSuccess: true, Value: not null })
                {
                    // Add back to active cache
                    CreateUserDto createdDto = new()
                    {
                        Username = getResult.Value.Username,
                        Password = getResult.Value.Password,
                        Role = getResult.Value.Role,
                        FullName = getResult.Value.FullName,
                        Email = getResult.Value.Email,
                        IsActive = getResult.Value.IsActive
                    };

                    UserDto? cached = userStore.Create(getResult.Value.UserId, createdDto);

                    if (cached != null)
                    {
                        logger.LogInformation(
                            "Successfully restored user with ID {UserId} and added back to cache",
                            userId);
                    }
                    else
                    {
                        logger.LogWarning(
                            "User restored in database (ID: {UserId}) but failed to add to cache",
                            userId);
                    }

                }

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to restore user with ID {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to restore user",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int userId )
        {
            DatabaseResult result = await userRepository.HardDeleteAsync(userId);

            if (result.IsSuccess)
            {
                // Remove from cache (works for both active and deleted
                bool removed = userStore.Delete(userId);

                if (!removed)
                {
                    logger.LogWarning("User hard deleted in database (ID: {UserId}) but wasn't found in active cache", userId);
                }

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to hard delete user with ID {UserId}: {ErrorMessage}", userId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to hard delete user",
                result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<UserDto> ValidateCreateInput( CreateUserDto? createUserDto )
        {
            if (createUserDto == null)
            {
                logger.LogWarning("Null CreateUserDto provided");
                return DatabaseResult<UserDto>.Failure(
                    "User data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = createValidator.Validate(createUserDto);

            if (validationResult.IsValid)
                return DatabaseResult<UserDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("User creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<UserDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<UserDto>> ValidateCreateBusiness( CreateUserDto createUserDto )
        {
            // Check username availability (excluding soft-deleted users)
            DatabaseResult<bool> usernameExistsResult = await userValidationService.UsernameExistsAsync(
                createUserDto.Username,
                includeDeleted: false);

            if (!usernameExistsResult.IsSuccess)
                return DatabaseResult<UserDto>.Failure(
                    usernameExistsResult.ErrorMessage!,
                    usernameExistsResult.ErrorCode);

            if (usernameExistsResult.Value)
            {
                logger.LogWarning("Attempted to create user with duplicate username: {Username}", createUserDto.Username);
                return DatabaseResult<UserDto>.Failure(
                    $"A user with the username '{createUserDto.Username}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            // Check email availability if provided
            if (!string.IsNullOrWhiteSpace(createUserDto.Email))
            {
                DatabaseResult<bool> emailExistsResult = await userValidationService.EmailExistsAsync(
                    createUserDto.Email,
                    includeDeleted: false);

                if (!emailExistsResult.IsSuccess)
                    return DatabaseResult<UserDto>.Failure(
                        emailExistsResult.ErrorMessage!,
                        emailExistsResult.ErrorCode);

                if (emailExistsResult.Value)
                {
                    logger.LogWarning("Attempted to create user with duplicate email: {Email}", createUserDto.Email);
                    return DatabaseResult<UserDto>.Failure(
                        $"A user with the email '{createUserDto.Email}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            return DatabaseResult<UserDto>.Success(null!);
        }

        private DatabaseResult<UserDto> ValidateUpdateInput( UpdateUserDto? updateUserDto )
        {
            if (updateUserDto == null)
            {
                logger.LogWarning("Null UpdateUserDto provided");
                return DatabaseResult<UserDto>.Failure(
                    "User data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = updateValidator.Validate(updateUserDto);

            if (validationResult.IsValid)
                return DatabaseResult<UserDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "User update validation failed for ID {UserId}: {ValidationErrors}",
                updateUserDto.UserId,
                errors);
            return DatabaseResult<UserDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<UserDto>> ValidateUpdateBusiness( UpdateUserDto updateUserDto )
        {
            // Check existence (only active users can be updated)
            DatabaseResult<bool> existsResult = await userValidationService.UserExistAsync(
                updateUserDto.UserId,
                false);

            if (!existsResult.IsSuccess)
                return DatabaseResult<UserDto>.Failure(
                    existsResult.ErrorMessage!,
                    existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update non-existent or deleted user with ID {UserId}",
                    updateUserDto.UserId);
                return DatabaseResult<UserDto>.Failure(
                    $"User with ID {updateUserDto.UserId} not found or is deleted. " +
                    "Restore the user first if it was deleted.",
                    DatabaseErrorCode.NotFound);
            }

            // Check username availability (excluding this user and soft-deleted users)
            DatabaseResult<bool> usernameExistsResult = await userValidationService.UsernameExistsAsync(
                updateUserDto.Username,
                updateUserDto.UserId,
                false);

            if (!usernameExistsResult.IsSuccess)
                return DatabaseResult<UserDto>.Failure(
                    usernameExistsResult.ErrorMessage!,
                    usernameExistsResult.ErrorCode);

            if (usernameExistsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update user {UserId} with duplicate username: {Username}",
                    updateUserDto.UserId,
                    updateUserDto.Username);
                return DatabaseResult<UserDto>.Failure(
                    $"A user with the username '{updateUserDto.Username}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            // Check email availability if provided
            if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
            {
                DatabaseResult<bool> emailExistsResult = await userValidationService.EmailExistsAsync(
                    updateUserDto.Email,
                    updateUserDto.UserId,
                    false);

                if (!emailExistsResult.IsSuccess)
                    return DatabaseResult<UserDto>.Failure(
                        emailExistsResult.ErrorMessage!,
                        emailExistsResult.ErrorCode);

                if (!emailExistsResult.Value) return DatabaseResult<UserDto>.Success(null!);

                logger.LogWarning(
                    "Attempted to update user {UserId} with duplicate email: {Email}",
                    updateUserDto.UserId,
                    updateUserDto.Email);
                return DatabaseResult<UserDto>.Failure(
                    $"A user with the email '{updateUserDto.Email}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<UserDto>.Success(null!);
        }

        private DatabaseResult ValidateChangePasswordInput( ChangePasswordDto? changePasswordDto )
        {
            if (changePasswordDto == null)
            {
                logger.LogWarning("Null ChangePasswordDto provided");
                return DatabaseResult.Failure(
                    "Password change data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = changePasswordValidator.Validate(changePasswordDto);

            if (validationResult.IsValid)
                return DatabaseResult.Success();

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "Password change validation failed for user {UserId}: {ValidationErrors}",
                changePasswordDto.UserId,
                errors);
            return DatabaseResult.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        #endregion
    }
}
