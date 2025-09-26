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

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Service responsible for category validation operations.
    /// </summary>
    public class CategoryValidationService(
        ICategoryRepository categoryRepository,
        ICategoryStore categoryStore,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<CategoryValidationService> logger ):ICategoryValidationService
    {
        public async Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId )
        {
            if (categoryId <= 0)
                return DatabaseResult<bool>.Success(false);

            // Check store first
            CategoryDto? categoryInStore = categoryStore.GetById(categoryId);
            if (categoryInStore != null)
                return DatabaseResult<bool>.Success(true);

            // Check database
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.ExistsAsync(categoryId),
                $"Checking if category {categoryId} exists",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null )
        {
            if (string.IsNullOrWhiteSpace(name))
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.NameExistsAsync(name, excludeCategoryId),
                "Checking if category name exists",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForDeletion( int categoryId )
        {
            // Check existence
            DatabaseResult<bool> existsResult = await CategoryExistsAsync(categoryId);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to delete non-existent category with ID {CategoryId}", categoryId);
                return DatabaseResult.Failure($"Category with ID {categoryId} not found.", DatabaseErrorCode.NotFound);
            }

            // Check for subcategories
            DatabaseResult<bool> hasSubcategoriesResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () => (await categoryRepository.GetSubCategoriesAsync(categoryId)).Any(),
                $"Checking for subcategories of category {categoryId}",
                false
            );

            if (hasSubcategoriesResult.IsSuccess && hasSubcategoriesResult.Value)
            {
                logger.LogWarning("Cannot delete category {CategoryId} - it has subcategories", categoryId);
                return DatabaseResult.Failure(
                    "Cannot delete category because it contains subcategories. Please move or delete the subcategories first.",
                    DatabaseErrorCode.ForeignKeyViolation);
            }

            // Check for products
            bool hasProducts = productStore.HasProductsInCategory(categoryId);
            if (!hasProducts) return DatabaseResult.Success();
            logger.LogWarning("Cannot delete category {CategoryId} - it contains products", categoryId);
            return DatabaseResult.Failure(
                "Cannot delete category because it contains products. Please move or delete the products first.",
                DatabaseErrorCode.ForeignKeyViolation);

        }
    }
}
