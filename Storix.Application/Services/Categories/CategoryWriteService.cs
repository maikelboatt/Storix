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
using Storix.Domain.Models;

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Service responsible for category write operations.
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
            Category category = createCategoryDto.ToDomain();
            DatabaseResult<Category> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.CreateAsync(category),
                "Creating new category"
            );

            if (result.IsSuccess && result.Value != null)
            {
                categoryStore.AddCategory(result.Value);
                logger.LogInformation(
                    "Successfully created category with ID {CategoryId} and name '{CategoryName}'",
                    result.Value.CategoryId,
                    result.Value.Name);

                CategoryDto categoryDto = result.Value.ToDto();
                return DatabaseResult<CategoryDto>.Success(categoryDto);
            }

            logger.LogWarning("Failed to create category: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<CategoryDto>.Failure(result.ErrorMessage!, result.ErrorCode);
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

        public async Task<DatabaseResult> DeleteCategoryAsync( int categoryId )
        {
            // Input validation
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided for deletion", categoryId);
                return DatabaseResult.Failure("Category ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await categoryValidationService.ValidateForDeletion(categoryId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform deletion
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.DeleteAsync(categoryId),
                "Deleting category",
                enableRetry: false
            );

            if (result.IsSuccess && result.Value)
            {
                categoryStore.DeleteCategory(categoryId);
                logger.LogInformation("Successfully deleted category with ID {CategoryId}", categoryId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to delete category with ID {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to delete category", result.ErrorCode);
        }

        // Private helper methods to keep main methods focused
        private DatabaseResult<CategoryDto> ValidateCreateInput( CreateCategoryDto? createCategoryDto )
        {
            if (createCategoryDto == null)
            {
                logger.LogWarning("Null CreateCategoryDto provided");
                return DatabaseResult<CategoryDto>.Failure("Category data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidator.Validate(createCategoryDto);

            if (validationResult.IsValid) return DatabaseResult<CategoryDto>.Success(null!); // Valid input

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Category creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<CategoryDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);

        }

        public async Task<DatabaseResult<CategoryDto>> ValidateCreateBusiness( CreateCategoryDto createCategoryDto )
        {
            DatabaseResult<bool> nameExistsResult = await categoryValidationService.CategoryNameExistsAsync(createCategoryDto.Name);
            if (!nameExistsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(nameExistsResult.ErrorMessage!, nameExistsResult.ErrorCode);

            if (!nameExistsResult.Value) return DatabaseResult<CategoryDto>.Success(null!); // Valid
            logger.LogWarning("Attempted to create category with duplicate name: {CategoryName}", createCategoryDto.Name);
            return DatabaseResult<CategoryDto>.Failure(
                $"A category with the name '{createCategoryDto.Name}' already exists.",
                DatabaseErrorCode.DuplicateKey);
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

            return DatabaseResult<CategoryDto>.Success(null!); // Valid
        }

        public async Task<DatabaseResult<CategoryDto>> ValidateUpdateBusiness( UpdateCategoryDto updateCategoryDto )
        {
            // Check existence
            DatabaseResult<bool> existsResult = await categoryValidationService.CategoryExistsAsync(updateCategoryDto.CategoryId);
            if (!existsResult.IsSuccess)
                return DatabaseResult<CategoryDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to update non-existent category with ID {CategoryId}", updateCategoryDto.CategoryId);
                return DatabaseResult<CategoryDto>.Failure(
                    $"Category with ID {updateCategoryDto.CategoryId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // Check duplicate name
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

            return DatabaseResult<CategoryDto>.Success(null!); // Valid
        }

        public async Task<DatabaseResult<CategoryDto>> PerformUpdate( UpdateCategoryDto updateCategoryDto )
        {
            // Get existing category
            DatabaseResult<Category?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetByIdAsync(updateCategoryDto.CategoryId),
                $"Retrieving category {updateCategoryDto.CategoryId} for update",
                false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return DatabaseResult<CategoryDto>.Failure(
                    getResult.ErrorMessage ?? "Category not found",
                    getResult.ErrorCode);
            }

            // Update category
            Category updatedCategory = getResult.Value with
            {
                Name = updateCategoryDto.Name,
                Description = updateCategoryDto.Description,
                ParentCategoryId = updateCategoryDto.ParentCategoryId,
                ImageUrl = updateCategoryDto.ImageUrl
            };

            DatabaseResult<Category> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.UpdateAsync(updatedCategory),
                "Updating category"
            );

            if (updateResult.IsSuccess && updateResult.Value != null)
            {
                categoryStore.UpdateCategory(updateResult.Value);
                logger.LogInformation("Successfully updated category with ID {CategoryId}", updateCategoryDto.CategoryId);

                CategoryDto categoryDto = updateResult.Value.ToDto();
                return DatabaseResult<CategoryDto>.Success(categoryDto);
            }

            logger.LogWarning(
                "Failed to update category with ID {CategoryId}: {ErrorMessage}",
                updateCategoryDto.CategoryId,
                updateResult.ErrorMessage);
            return DatabaseResult<CategoryDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }
    }
}
