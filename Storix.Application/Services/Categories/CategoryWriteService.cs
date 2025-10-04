using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Categories;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Service responsible for category write operations with ISoftDeletable support.
    /// </summary>
    public class CategoryWriteService(
        ICategoryRepository categoryRepository,
        ICategoryStore categoryStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ICategoryValidationService categoryValidationService,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator,
        ILogger<CategoryWriteService> logger ):ICategoryWriteService
    {
        public async Task<DatabaseResult<CategoryDto>> CreateCategoryAsync( CreateCategoryDto createCategoryDto )
        {
            // Input validation
            DatabaseResult<CategoryDto> inputValidation = ValidateCreateInput(createCategoryDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<CategoryDto> businessValidation = await ValidateCreateBusiness(createCategoryDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create category
            return await PerformCreate(createCategoryDto);
        }

        public async Task<DatabaseResult<CategoryDto>> UpdateCategoryAsync( UpdateCategoryDto updateCategoryDto )
        {
            // Input validation
            DatabaseResult<CategoryDto> inputValidation = ValidateUpdateInput(updateCategoryDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<CategoryDto> businessValidation = await ValidateUpdateBusiness(updateCategoryDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform update
            return await PerformUpdate(updateCategoryDto);
        }

        public async Task<DatabaseResult> SoftDeleteCategoryAsync( int categoryId )
        {
            // Input validation
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided for soft deletion", categoryId);
                return DatabaseResult.Failure("Category ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await categoryValidationService.ValidateForDeletion(categoryId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform soft deletion
            return await PerformSoftDelete(categoryId);
        }

        public async Task<DatabaseResult> RestoreCategoryAsync( int categoryId )
        {
            // Input validation
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided for restoration", categoryId);
                return DatabaseResult.Failure("Category ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await categoryValidationService.ValidateForRestore(categoryId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform restoration
            return await PerformRestore(categoryId);
        }

        public async Task<DatabaseResult> HardDeleteCategoryAsync( int categoryId )
        {
            // Input validation
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided for hard deletion", categoryId);
                return DatabaseResult.Failure("Category ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await categoryValidationService.ValidateForHardDeletion(categoryId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform hard deletion
            return await PerformHardDelete(categoryId);
        }

        #region Helper Methods

        private async Task<DatabaseResult<CategoryDto>> PerformCreate( CreateCategoryDto createCategoryDto )
        {
            Category category = createCategoryDto.ToDomain();

            // Ensure new categories are not marked as deleted
            category = category with
            {
                IsDeleted = false,
                DeletedAt = null
            };

            DatabaseResult<Category> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.CreateAsync(category),
                "Creating new category"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                // Convert to DTO for store operations
                CategoryDto categoryDto = result.Value.ToDto();
                int createdCategoryId = result.Value.CategoryId;

                // Add to in-memory store using the database-generated ID
                CategoryDto? storeResult = categoryStore.Create(createdCategoryId, categoryDto);

                if (storeResult == null)
                {
                    // Log warning but don't fail the operation since database succeeded
                    logger.LogWarning(
                        "Category created in database (ID: {CategoryId}) but failed to add to cache",
                        result.Value.CategoryId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully created category with ID {CategoryId} and name '{CategoryName}' in both database and cache",
                        result.Value.CategoryId,
                        result.Value.Name);
                }

                return DatabaseResult<CategoryDto>.Success(categoryDto);
            }

            logger.LogWarning("Failed to create category: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<CategoryDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<CategoryDto>> PerformUpdate( UpdateCategoryDto updateCategoryDto )
        {
            // Get existing category (including soft-deleted ones for update operations)
            DatabaseResult<Category?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetByIdAsync(updateCategoryDto.CategoryId, true),
                $"Retrieving category {updateCategoryDto.CategoryId} for update",
                false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return DatabaseResult<CategoryDto>.Failure(
                    getResult.ErrorMessage ?? "Category not found",
                    getResult.ErrorCode);
            }

            // Update category while preserving ISoftDeletable properties
            Category updatedCategory = getResult.Value with
            {
                Name = updateCategoryDto.Name,
                Description = updateCategoryDto.Description,
                ParentCategoryId = updateCategoryDto.ParentCategoryId,
                ImageUrl = updateCategoryDto.ImageUrl
                // ISoftDeletable properties are preserved from existing category
            };

            DatabaseResult<Category> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.UpdateAsync(updatedCategory),
                "Updating category"
            );

            if (updateResult.IsSuccess && updateResult.Value != null)
            {
                CategoryDto categoryDto = updateResult.Value.ToDto();

                // Update in-memory store
                categoryStore.Update(categoryDto);
                logger.LogInformation("Successfully updated category with ID {CategoryId}", updateCategoryDto.CategoryId);

                return DatabaseResult<CategoryDto>.Success(categoryDto);
            }

            logger.LogWarning(
                "Failed to update category with ID {CategoryId}: {ErrorMessage}",
                updateCategoryDto.CategoryId,
                updateResult.ErrorMessage);
            return DatabaseResult<CategoryDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformSoftDelete( int categoryId )
        {
            DatabaseResult result = await categoryRepository.SoftDeleteAsync(categoryId);

            if (result is { IsSuccess: true })
            {
                // Remove from store cache since it's now soft deleted
                categoryStore.Delete(categoryId);
                logger.LogInformation("Successfully soft deleted category with ID {CategoryId}", categoryId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to soft delete category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to soft delete category", result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int categoryId )
        {
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.RestoreAsync(categoryId),
                "Restoring category",
                enableRetry: false
            );

            if (result.IsSuccess && result.Value)
            {
                // Get the restored category and add it back to the store cache
                DatabaseResult<Category?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => categoryRepository.GetByIdAsync(categoryId),
                    $"Retrieving restored category {categoryId}",
                    false
                );

                if (getResult.IsSuccess && getResult.Value != null)
                {
                    CategoryDto categoryDto = getResult.Value.ToDto();
                    categoryStore.Update(categoryDto);
                }

                logger.LogInformation("Successfully restored category with ID {CategoryId}", categoryId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to restore category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to restore category", result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int categoryId )
        {
            DatabaseResult result = await categoryRepository.HardDeleteAsync(categoryId);

            if (result is { IsSuccess: true })
            {
                categoryStore.Delete(categoryId);
                logger.LogWarning("Successfully hard deleted category with ID {CategoryId} - THIS IS PERMANENT", categoryId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to hard delete category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to hard delete category", result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<CategoryDto> ValidateCreateInput( CreateCategoryDto? createCategoryDto )
        {
            if (createCategoryDto == null)
            {
                logger.LogWarning("Null CreateCategoryDto provided");
                return DatabaseResult<CategoryDto>.Failure("Category data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidator.Validate(createCategoryDto);

            if (validationResult.IsValid) return DatabaseResult<CategoryDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Category creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<CategoryDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<CategoryDto>> ValidateCreateBusiness( CreateCategoryDto createCategoryDto )
        {
            // Check name availability (excluding soft-deleted categories by default)
            DatabaseResult<bool> nameExistsResult = await categoryValidationService.CategoryNameExistsAsync(createCategoryDto.Name, includeDeleted: false);
            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(nameExistsResult.ErrorMessage!, nameExistsResult.ErrorCode);

            if (nameExistsResult.Value)
            {
                logger.LogWarning("Attempted to create category with duplicate name: {CategoryName}", createCategoryDto.Name);
                return DatabaseResult<CategoryDto>.Failure(
                    $"A category with the name '{createCategoryDto.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<CategoryDto>.Success(null!);
        }

        private DatabaseResult<CategoryDto> ValidateUpdateInput( UpdateCategoryDto? updateCategoryDto )
        {
            if (updateCategoryDto == null)
            {
                logger.LogWarning("Null UpdateCategoryDto provided");
                return DatabaseResult<CategoryDto>.Failure("Category data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = updateValidator.Validate(updateCategoryDto);
            if (!validationResult.IsValid)
            {
                string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                logger.LogWarning(
                    "Category update validation failed for ID {CategoryId}: {ValidationErrors}",
                    updateCategoryDto.CategoryId,
                    errors);
                return DatabaseResult<CategoryDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);
            }

            return DatabaseResult<CategoryDto>.Success(null!);
        }

        private async Task<DatabaseResult<CategoryDto>> ValidateUpdateBusiness( UpdateCategoryDto updateCategoryDto )
        {
            // Check existence (including soft-deleted categories for updates)
            DatabaseResult<bool> existsResult = await categoryValidationService.CategoryExistsAsync(updateCategoryDto.CategoryId, true);
            if (!existsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to update non-existent category with ID {CategoryId}", updateCategoryDto.CategoryId);
                return DatabaseResult<CategoryDto>.Failure(
                    $"Category with ID {updateCategoryDto.CategoryId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // Check name availability (excluding soft-deleted categories)
            DatabaseResult<bool> nameExistsResult = await categoryValidationService.CategoryNameExistsAsync(
                updateCategoryDto.Name,
                updateCategoryDto.CategoryId);
            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(nameExistsResult.ErrorMessage!, nameExistsResult.ErrorCode);

            if (nameExistsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update category {CategoryId} with duplicate name: {CategoryName}",
                    updateCategoryDto.CategoryId,
                    updateCategoryDto.Name);
                return DatabaseResult<CategoryDto>.Failure(
                    $"A category with the name '{updateCategoryDto.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<CategoryDto>.Success(null!);
        }

        #endregion
    }
}
