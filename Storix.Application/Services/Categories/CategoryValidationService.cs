using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Categories;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Service responsible for category validation operations with ISoftDeletable support.
    /// </summary>
    public class CategoryValidationService(
        ICategoryRepository categoryRepository,
        ICategoryStore categoryStore,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<CategoryValidationService> logger ):ICategoryValidationService
    {
        public async Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId, bool includeDeleted = false )
        {
            if (categoryId <= 0)
                return DatabaseResult<bool>.Success(false);

            // Check store first (store only contains active/non-deleted categories)
            if (!includeDeleted)
            {
                CategoryDto? categoryInStore = categoryStore.GetById(categoryId);
                if (categoryInStore != null)
                    return DatabaseResult<bool>.Success(true);
            }

            // Check database
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.ExistsAsync(categoryId, includeDeleted),
                $"Checking if category {categoryId} exists (includeDeleted: {includeDeleted})",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(name))
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.NameExistsAsync(name, excludeCategoryId, includeDeleted),
                $"Checking if category name '{name}' exists (excludeCategoryId: {excludeCategoryId}, includeDeleted: {includeDeleted})",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForDeletion( int categoryId )
        {
            // Check existence (only active categories can be deleted)
            DatabaseResult existenceResult = await ProofOfExistence(categoryId);
            if (!existenceResult.IsSuccess)
                return existenceResult;

            // Check business rules for soft deletion
            DatabaseResult businessRulesResult = await ValidateCategoryDeletionBusinessRules(categoryId);
            return !businessRulesResult.IsSuccess
                ? businessRulesResult
                : DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForHardDeletion( int categoryId )
        {
            // Check existence (including soft-deleted categories for hard deletion)
            DatabaseResult existenceResult = await ProofOfExistence(categoryId, true);
            if (!existenceResult.IsSuccess)
                return existenceResult;

            // Check business rules for hard deletion (more restrictive)
            DatabaseResult businessRulesResult = await ValidateCategoryHardDeletionBusinessRules(categoryId);
            if (!businessRulesResult.IsSuccess)
                return businessRulesResult;

            logger.LogWarning("Hard deletion validation passed for category {CategoryId} - THIS WILL BE PERMANENT", categoryId);
            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForRestore( int categoryId )
        {
            // Check if category exists and is soft deleted
            DatabaseResult existenceResult = await ProofOfExistence(categoryId, true);
            if (!existenceResult.IsSuccess)
                return existenceResult;

            // Check if category is actually deleted
            DatabaseResult<bool> isSoftDeletedResult = await IsCategorySoftDeleted(categoryId);
            if (!isSoftDeletedResult.IsSuccess)
                return DatabaseResult.Failure(isSoftDeletedResult.ErrorMessage!, isSoftDeletedResult.ErrorCode);

            if (!isSoftDeletedResult.Value)
            {
                logger.LogWarning("Attempted to restore active category with ID {CategoryId}", categoryId);
                return DatabaseResult.Failure($"Category with ID {categoryId} is not deleted and cannot be restored.", DatabaseErrorCode.InvalidInput);
            }

            // Check business rules for restoration
            DatabaseResult businessValidation = await ValidateCategoryRestorationBusinessRules(categoryId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult<bool>> IsCategorySoftDeleted( int categoryId )
        {
            if (categoryId <= 0)
                return DatabaseResult<bool>.Success(false);

            // If category exists with includeDeleted=true but not with includeDeleted=false, it's soft deleted
            DatabaseResult<bool> existsIncludingDeleted = await CategoryExistsAsync(categoryId, true);
            if (!existsIncludingDeleted.IsSuccess || !existsIncludingDeleted.Value)
            {
                return existsIncludingDeleted.IsSuccess
                    ? DatabaseResult<bool>.Success(false)
                    : DatabaseResult<bool>.Failure(existsIncludingDeleted.ErrorMessage!, existsIncludingDeleted.ErrorCode);
            }

            DatabaseResult<bool> existsActiveOnly = await CategoryExistsAsync(categoryId);
            if (!existsActiveOnly.IsSuccess)
                return DatabaseResult<bool>.Failure(existsActiveOnly.ErrorMessage!, existsActiveOnly.ErrorCode);

            // Category exists in database but not in active results = soft deleted
            bool isSoftDeleted = !existsActiveOnly.Value;
            return DatabaseResult<bool>.Success(isSoftDeleted);
        }

        #region Private Helper Methods

        private async Task<DatabaseResult> ProofOfExistence( int categoryId, bool includeDeleted = false )
        {
            // Check existence
            DatabaseResult<bool> existsResult = await CategoryExistsAsync(categoryId, includeDeleted);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                string statusMessage = includeDeleted
                    ? ""
                    : " or already deleted";
                logger.LogWarning("Attempted operation on non-existent category with ID {CategoryId}{StatusMessage}", categoryId, statusMessage);
                return DatabaseResult.Failure($"Category with ID {categoryId} not found{statusMessage}.", DatabaseErrorCode.NotFound);
            }
            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateCategoryDeletionBusinessRules( int categoryId )
        {
            // Check for subcategories (only active subcategories should prevent deletion)
            DatabaseResult<bool> hasSubcategoriesResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () => (await categoryRepository.GetSubCategoriesAsync(categoryId)).Any(),
                $"Checking for active subcategories of category {categoryId}",
                false
            );

            if (hasSubcategoriesResult.IsSuccess && hasSubcategoriesResult.Value)
            {
                logger.LogWarning("Cannot delete category {CategoryId} - it has active subcategories", categoryId);
                return DatabaseResult.Failure(
                    "Cannot delete category because it contains active subcategories. Please move or delete the subcategories first.",
                    DatabaseErrorCode.ForeignKeyViolation);
            }

            // Check for products (only active products should prevent deletion)
            bool hasProducts = productStore.HasProductsInCategory(categoryId);
            if (!hasProducts) return DatabaseResult.Success();

            logger.LogWarning("Cannot delete category {CategoryId} - it contains active products", categoryId);
            return DatabaseResult.Failure(
                "Cannot delete category because it contains active products. Please move or delete the products first.",
                DatabaseErrorCode.ForeignKeyViolation);
        }

        private async Task<DatabaseResult> ValidateCategoryHardDeletionBusinessRules( int categoryId )
        {
            // Hard deletion should be more restrictive - check for ANY subcategories or products

            // Check for ANY subcategories (including deleted ones)
            DatabaseResult<bool> hasAnySubcategoriesResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () => (await categoryRepository.GetSubCategoriesAsync(categoryId, true)).Any(),
                $"Checking for any subcategories of category {categoryId}",
                false
            );

            if (hasAnySubcategoriesResult.IsSuccess && hasAnySubcategoriesResult.Value)
            {
                logger.LogWarning("Cannot hard delete category {CategoryId} - it has subcategories (including deleted)", categoryId);
                return DatabaseResult.Failure(
                    "Cannot permanently delete category because it has historical subcategories. This would break data integrity.",
                    DatabaseErrorCode.ForeignKeyViolation);
            }

            // Check for ANY products (including deleted ones)
            bool hasAnyProducts = productStore.HasProductsInCategory(categoryId, true);
            if (!hasAnyProducts) return DatabaseResult.Success();

            logger.LogWarning("Cannot hard delete category {CategoryId} - it contains products (including deleted)", categoryId);
            return DatabaseResult.Failure(
                "Cannot permanently delete category because it has historical products. This would break data integrity.",
                DatabaseErrorCode.ForeignKeyViolation);
        }

        private async Task<DatabaseResult> ValidateCategoryRestorationBusinessRules( int categoryId )
        {
            // Check if parent category still exists and is active (if this category has a parent)
            DatabaseResult<Category?> categoryResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetByIdAsync(categoryId, true),
                $"Retrieving category {categoryId} for restore validation",
                false
            );

            if (!categoryResult.IsSuccess || categoryResult.Value == null)
                return DatabaseResult.Failure("Category not found for restoration validation.", DatabaseErrorCode.NotFound);

            // If category has a parent, ensure parent is still active
            if (categoryResult.Value.ParentCategoryId.HasValue)
            {
                DatabaseResult<bool> parentExistsResult = await CategoryExistsAsync(categoryResult.Value.ParentCategoryId.Value);
                if (!parentExistsResult.IsSuccess)
                    return DatabaseResult.Failure(parentExistsResult.ErrorMessage!, parentExistsResult.ErrorCode);

                if (!parentExistsResult.Value)
                {
                    logger.LogWarning(
                        "Cannot restore category {CategoryId} - parent category {ParentCategoryId} is not active",
                        categoryId,
                        categoryResult.Value.ParentCategoryId.Value);
                    return DatabaseResult.Failure(
                        "Cannot restore category because its parent category is no longer active.",
                        DatabaseErrorCode.ConstraintViolation);
                }
            }

            // Check for name conflicts with active categories
            DatabaseResult<bool> nameConflictResult = await CategoryNameExistsAsync(
                categoryResult.Value.Name,
                categoryId);

            if (!nameConflictResult.IsSuccess)
                return DatabaseResult.Failure(nameConflictResult.ErrorMessage!, nameConflictResult.ErrorCode);

            if (nameConflictResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore category {CategoryId} - name '{Name}' conflicts with existing active category",
                    categoryId,
                    categoryResult.Value.Name);
                return DatabaseResult.Failure(
                    $"Cannot restore category: Another active category with name '{categoryResult.Value.Name}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            logger.LogDebug("Category {CategoryId} passed all restoration business rule validations", categoryId);
            return DatabaseResult.Success();
        }

        #endregion
    }
}
