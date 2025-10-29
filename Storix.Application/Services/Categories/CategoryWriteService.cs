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
            // Convert DTO to domain model - always creates non-deleted categories
            Category category = createCategoryDto.ToDomain();

            DatabaseResult<Category> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.CreateAsync(category),
                "Creating new category"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                int createdCategoryId = result.Value.CategoryId;

                // Add to in-memory store
                CategoryDto? storeResult = categoryStore.Create(createdCategoryId, createCategoryDto);

                if (storeResult == null)
                {
                    logger.LogWarning(
                        "Category created in database (ID: {CategoryId}) but failed to add to cache",
                        createdCategoryId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully created category with ID {CategoryId} and name '{CategoryName}'",
                        createdCategoryId,
                        result.Value.Name);
                }

                return DatabaseResult<CategoryDto>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to create category: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<CategoryDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<CategoryDto>> PerformUpdate( UpdateCategoryDto updateCategoryDto )
        {
            // Get existing category (only active ones - can't update deleted categories)
            DatabaseResult<Category?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetByIdAsync(updateCategoryDto.CategoryId),
                $"Retrieving category {updateCategoryDto.CategoryId} for update",
                enableRetry: false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning(
                    "Cannot update category {CategoryId}: {ErrorMessage}",
                    updateCategoryDto.CategoryId,
                    getResult.ErrorMessage ?? "Category not found or is deleted");
                return DatabaseResult<CategoryDto>.Failure(
                    getResult.ErrorMessage ?? "Category not found or is deleted. Restore the category first if it was deleted.",
                    getResult.ErrorCode);
            }

            Category existingCategory = getResult.Value;

            // Update category while preserving ISoftDeletable properties
            Category updatedCategory = existingCategory with
            {
                Name = updateCategoryDto.Name,
                Description = updateCategoryDto.Description,
                ParentCategoryId = updateCategoryDto.ParentCategoryId,
                ImageUrl = updateCategoryDto.ImageUrl
                // IsDeleted and DeletedAt are preserved from existingCategory
            };

            DatabaseResult<Category> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.UpdateAsync(updatedCategory),
                "Updating category"
            );

            if (updateResult is { IsSuccess: true, Value: not null })
            {
                // Update in-memory store
                CategoryDto? storeResult = categoryStore.Update(updateCategoryDto);

                if (storeResult == null)
                {
                    logger.LogWarning(
                        "Category updated in database (ID: {CategoryId}) but failed to update in cache",
                        updateCategoryDto.CategoryId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully updated category with ID {CategoryId}",
                        updateCategoryDto.CategoryId);
                }

                return DatabaseResult<CategoryDto>.Success(updateResult.Value.ToDto());
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

            if (result.IsSuccess)
            {
                bool storeResult = categoryStore.Delete(categoryId);

                if (!storeResult)
                {
                    logger.LogWarning(
                        "Category soft deleted in database (ID: {CategoryId}) but failed to update cache",
                        categoryId);
                }
                else
                {
                    logger.LogInformation(
                        "Successfully soft deleted category with ID {CategoryId}",
                        categoryId);
                }

                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to soft delete category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to soft delete category",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int categoryId )
        {
            DatabaseResult result = await categoryRepository.RestoreAsync(categoryId);

            if (result.IsSuccess)
            {
                // Fetch the restored category from database
                DatabaseResult<Category?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => categoryRepository.GetByIdAsync(categoryId),
                    $"Retrieving restored category {categoryId}",
                    enableRetry: false
                );

                if (getResult is { IsSuccess: true, Value: not null })
                {
                    // Add back to active cache
                    CreateCategoryDto createDto = new()
                    {
                        Name = getResult.Value.Name,
                        Description = getResult.Value.Description,
                        ParentCategoryId = getResult.Value.ParentCategoryId,
                        ImageUrl = getResult.Value.ImageUrl
                    };

                    CategoryDto? cached = categoryStore.Create(getResult.Value.CategoryId, createDto);

                    if (cached != null)
                    {
                        logger.LogInformation(
                            "Successfully restored category with ID {CategoryId} and added back to cache",
                            categoryId);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Category restored in database (ID: {CategoryId}) but failed to add to cache",
                            categoryId);
                    }
                }

                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to restore category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to restore category",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int categoryId )
        {
            DatabaseResult result = await categoryRepository.HardDeleteAsync(categoryId);

            if (result.IsSuccess)
            {
                // Remove from store completely (checks both active and deleted collections)
                bool storeResult = categoryStore.Delete(categoryId);

                if (!storeResult)
                {
                    logger.LogWarning(
                        "Category hard deleted in database (ID: {CategoryId}) but wasn't found in cache",
                        categoryId);
                }

                logger.LogWarning(
                    "Successfully hard deleted category with ID {CategoryId} - THIS IS PERMANENT",
                    categoryId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to hard delete category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to hard delete category",
                result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<CategoryDto> ValidateCreateInput( CreateCategoryDto? createCategoryDto )
        {
            if (createCategoryDto == null)
            {
                logger.LogWarning("Null CreateCategoryDto provided");
                return DatabaseResult<CategoryDto>.Failure(
                    "Category data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = createValidator.Validate(createCategoryDto);

            if (validationResult.IsValid)
                return DatabaseResult<CategoryDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Category creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<CategoryDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<CategoryDto>> ValidateCreateBusiness( CreateCategoryDto createCategoryDto )
        {
            // Check name availability (excluding soft-deleted categories)
            DatabaseResult<bool> nameExistsResult = await categoryValidationService.CategoryNameExistsAsync(
                createCategoryDto.Name,
                includeDeleted: false);

            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(
                    nameExistsResult.ErrorMessage!,
                    nameExistsResult.ErrorCode);

            if (nameExistsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to create category with duplicate name: {CategoryName}",
                    createCategoryDto.Name);
                return DatabaseResult<CategoryDto>.Failure(
                    $"A category with the name '{createCategoryDto.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            // Validate parent category exists and is active if specified
            if (createCategoryDto.ParentCategoryId.HasValue)
            {
                DatabaseResult<bool> parentExistsResult = await categoryValidationService.CategoryExistsAsync(
                    createCategoryDto.ParentCategoryId.Value,
                    false);

                if (!parentExistsResult.IsSuccess)
                    return DatabaseResult<CategoryDto>.Failure(
                        parentExistsResult.ErrorMessage!,
                        parentExistsResult.ErrorCode);

                if (!parentExistsResult.Value)
                {
                    logger.LogWarning(
                        "Attempted to create category with non-existent or deleted parent ID: {ParentId}",
                        createCategoryDto.ParentCategoryId.Value);
                    return DatabaseResult<CategoryDto>.Failure(
                        $"Parent category with ID {createCategoryDto.ParentCategoryId.Value} not found or is deleted.",
                        DatabaseErrorCode.NotFound);
                }
            }

            return DatabaseResult<CategoryDto>.Success(null!);
        }

        private DatabaseResult<CategoryDto> ValidateUpdateInput( UpdateCategoryDto? updateCategoryDto )
        {
            if (updateCategoryDto == null)
            {
                logger.LogWarning("Null UpdateCategoryDto provided");
                return DatabaseResult<CategoryDto>.Failure(
                    "Category data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = updateValidator.Validate(updateCategoryDto);

            if (validationResult.IsValid)
                return DatabaseResult<CategoryDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "Category update validation failed for ID {CategoryId}: {ValidationErrors}",
                updateCategoryDto.CategoryId,
                errors);
            return DatabaseResult<CategoryDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<CategoryDto>> ValidateUpdateBusiness( UpdateCategoryDto updateCategoryDto )
        {
            // Check existence (only active categories can be updated)
            DatabaseResult<bool> existsResult = await categoryValidationService.CategoryExistsAsync(
                updateCategoryDto.CategoryId,
                false);

            if (!existsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(
                    existsResult.ErrorMessage!,
                    existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update non-existent or deleted category with ID {CategoryId}",
                    updateCategoryDto.CategoryId);
                return DatabaseResult<CategoryDto>.Failure(
                    $"Category with ID {updateCategoryDto.CategoryId} not found or is deleted. " +
                    "Restore the category first if it was deleted.",
                    DatabaseErrorCode.NotFound);
            }

            // Check name availability (excluding this category and soft-deleted categories)
            DatabaseResult<bool> nameExistsResult = await categoryValidationService.CategoryNameExistsAsync(
                updateCategoryDto.Name,
                updateCategoryDto.CategoryId,
                false);

            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(
                    nameExistsResult.ErrorMessage!,
                    nameExistsResult.ErrorCode);

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

            // Validate parent category exists and is active if specified
            if (updateCategoryDto.ParentCategoryId.HasValue)
            {
                // Can't set itself as parent
                if (updateCategoryDto.ParentCategoryId.Value == updateCategoryDto.CategoryId)
                {
                    logger.LogWarning(
                        "Attempted to set category {CategoryId} as its own parent",
                        updateCategoryDto.CategoryId);
                    return DatabaseResult<CategoryDto>.Failure(
                        "A category cannot be its own parent.",
                        DatabaseErrorCode.InvalidInput);
                }

                DatabaseResult<bool> parentExistsResult = await categoryValidationService.CategoryExistsAsync(
                    updateCategoryDto.ParentCategoryId.Value,
                    false);

                if (!parentExistsResult.IsSuccess)
                    return DatabaseResult<CategoryDto>.Failure(
                        parentExistsResult.ErrorMessage!,
                        parentExistsResult.ErrorCode);

                if (!parentExistsResult.Value)
                {
                    logger.LogWarning(
                        "Attempted to update category with non-existent or deleted parent ID: {ParentId}",
                        updateCategoryDto.ParentCategoryId.Value);
                    return DatabaseResult<CategoryDto>.Failure(
                        $"Parent category with ID {updateCategoryDto.ParentCategoryId.Value} not found or is deleted.",
                        DatabaseErrorCode.NotFound);
                }
            }

            return DatabaseResult<CategoryDto>.Success(null!);
        }

        #endregion
    }
}
