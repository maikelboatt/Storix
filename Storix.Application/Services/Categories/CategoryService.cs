using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.Enums;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Main service for managing category operations with ISoftDeletable support and enhanced error handling.
    /// </summary>
    public class CategoryService(
        ICategoryReadService categoryReadService,
        ICategoryWriteService categoryWriteService,
        ICategoryValidationService categoryValidationService,
        ICategoryStore categoryStore,
        ILogger<CategoryService> logger ):ICategoryService
    {
        #region Read Operations

        public CategoryDto? GetCategoryById( int categoryId, bool includeDeleted = false ) => categoryReadService.GetCategoryById(categoryId, includeDeleted);

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync( bool includeDeleted = false ) =>
            await categoryReadService.GetAllCategoriesAsync(includeDeleted);

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync() => await categoryReadService.GetAllActiveCategoriesAsync();

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllDeletedCategoriesAsync() => await categoryReadService.GetAllDeletedCategoriesAsync();

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync( bool includeDeleted = false ) =>
            await categoryReadService.GetRootCategoriesAsync(includeDeleted);

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false ) =>
            await categoryReadService.GetSubCategoriesAsync(parentCategoryId, includeDeleted);

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false ) =>
            await categoryReadService.GetCategoryPagedAsync(pageNumber, pageSize, includeDeleted);

        public async Task<DatabaseResult<int>> GetTotalCategoryCountAsync( bool includeDeleted = false ) =>
            await categoryReadService.GetTotalCategoryCountAsync(includeDeleted);

        public async Task<DatabaseResult<int>> GetActiveCategoryCountAsync() => await categoryReadService.GetActiveCategoryCountAsync();

        public async Task<DatabaseResult<int>> GetDeletedCategoryCountAsync() => await categoryReadService.GetDeletedCategoryCountAsync();

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm, bool includeDeleted = false ) =>
            await categoryReadService.SearchAsync(searchTerm, includeDeleted);

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<CategoryDto>> CreateCategoryAsync( CreateCategoryDto createCategoryDto ) =>
            await categoryWriteService.CreateCategoryAsync(createCategoryDto);

        public async Task<DatabaseResult<CategoryDto>> UpdateCategoryAsync( UpdateCategoryDto updateCategoryDto ) =>
            await categoryWriteService.UpdateCategoryAsync(updateCategoryDto);

        public async Task<DatabaseResult> SoftDeleteCategoryAsync( int categoryId ) => await categoryWriteService.SoftDeleteCategoryAsync(categoryId);

        public async Task<DatabaseResult> RestoreCategoryAsync( int categoryId ) => await categoryWriteService.RestoreCategoryAsync(categoryId);

        public async Task<DatabaseResult> HardDeleteCategoryAsync( int categoryId ) => await categoryWriteService.HardDeleteCategoryAsync(categoryId);

        // Legacy method - now uses SoftDeleteCategoryAsync for backward compatibility
        [Obsolete("Use SoftDeleteCategoryAsync instead. This method will be removed in a future version.")]
        public async Task<DatabaseResult> DeleteCategoryAsync( int categoryId ) => await SoftDeleteCategoryAsync(categoryId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId, bool includeDeleted = false ) =>
            await categoryValidationService.CategoryExistsAsync(categoryId, includeDeleted);

        public async Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false ) =>
            await categoryValidationService.CategoryNameExistsAsync(name, excludeCategoryId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsCategorySoftDeleted( int categoryId ) => await categoryValidationService.IsCategorySoftDeleted(categoryId);

        public async Task<DatabaseResult> ValidateForDeletion( int categoryId ) => await categoryValidationService.ValidateForDeletion(categoryId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int categoryId ) => await categoryValidationService.ValidateForHardDeletion(categoryId);

        public async Task<DatabaseResult> ValidateForRestore( int categoryId ) => await categoryValidationService.ValidateForRestore(categoryId);

        #endregion

        #region Store Operations

        public IEnumerable<CategoryDto> SearchCategories( string? searchTerm = null, bool includeDeleted = false )
        {
            logger.LogDebug(
                "Searching categories with term '{SearchTerm}', includeDeleted {IncludeDeleted}",
                searchTerm,
                includeDeleted);

            IEnumerable<Category> categories = categoryStore.SearchCategories(searchTerm);
            return categories.ToDto();
        }

        public IEnumerable<CategoryDto> GetActiveCategoriesFromStore()
        {
            logger.LogDebug("Retrieving active categories from store");
            IEnumerable<Category> categories = categoryStore.GetActiveCategories();
            return categories.ToDto();
        }


        public void RefreshStoreCache()
        {
            logger.LogInformation("Refreshing category store cache");
            _ = Task.Run(async () =>
            {
                try
                {
                    DatabaseResult<IEnumerable<CategoryDto>> result = await GetAllCategoriesAsync();
                    if (result.IsSuccess && result.Value != null)
                    {
                        logger.LogInformation("Category store cache refreshed successfully");
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh category store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing category store cache");
                }
            });
        }

        #endregion

        #region Bulk Operations

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> BulkSoftDeleteAsync( IEnumerable<int> categoryIds )
        {
            IEnumerable<int> enumerable = categoryIds.ToList();
            logger.LogInformation("Starting bulk soft delete for {Count} categories", enumerable.Count());

            List<CategoryDto> processedCategories = [];
            List<string> errors = new();

            foreach (int categoryId in enumerable)
            {
                DatabaseResult result = await SoftDeleteCategoryAsync(categoryId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Category {categoryId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to soft delete category {CategoryId}: {Error}", categoryId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(
                    $"Bulk soft delete completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk soft delete completed successfully for {Count} categories", enumerable.Count());
            return DatabaseResult<IEnumerable<CategoryDto>>.Success(processedCategories);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> BulkRestoreAsync( IEnumerable<int> categoryIds )
        {
            IEnumerable<int> enumerable = categoryIds.ToList();
            logger.LogInformation("Starting bulk restore for {Count} categories", enumerable.Count());

            List<CategoryDto> processedCategories = [];
            List<string> errors = [];

            foreach (int categoryId in enumerable)
            {
                DatabaseResult result = await RestoreCategoryAsync(categoryId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Category {categoryId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to restore category {CategoryId}: {Error}", categoryId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(
                    $"Bulk restore completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk restore completed successfully for {Count} categories", enumerable.Count());
            return DatabaseResult<IEnumerable<CategoryDto>>.Success(processedCategories);
        }

        #endregion
    }
}
