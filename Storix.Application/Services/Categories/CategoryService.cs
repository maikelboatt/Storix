using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.Services.Categories.Interfaces;

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Main service for managing category operations with enhanced error handling.
    /// </summary>
    public class CategoryService(
        ICategoryReadService categoryReadService,
        ICategoryWriteService categoryWriteService,
        ICategoryValidationService categoryValidationService,
        ICategoryStore categoryStore,
        ILogger<CategoryService> logger ):ICategoryService
    {
        #region Read Operations

        public CategoryDto? GetCategoryById( int categoryId ) => categoryReadService.GetCategoryById(categoryId);

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync() => await categoryReadService.GetAllCategoriesAsync();

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync() => await categoryReadService.GetRootCategoriesAsync();

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId ) => await categoryReadService.GetSubCategoriesAsync(parentCategoryId);

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize ) =>
            await categoryReadService.GetCategoryPagedAsync(pageNumber, pageSize);

        public async Task<DatabaseResult<int>> GetTotalCategoryCountAsync() => await categoryReadService.GetTotalCategoryCountAsync();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<CategoryDto>> CreateCategoryAsync( CreateCategoryDto createCategoryDto ) => await categoryWriteService.CreateCategoryAsync(createCategoryDto);

        public async Task<DatabaseResult<CategoryDto>> UpdateCategoryAsync( UpdateCategoryDto updateCategoryDto ) => await categoryWriteService.UpdateCategoryAsync(updateCategoryDto);

        public async Task<DatabaseResult> DeleteCategoryAsync( int categoryId ) => await categoryWriteService.DeleteCategoryAsync(categoryId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId ) => await categoryValidationService.CategoryExistsAsync(categoryId);

        public async Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null ) =>
            await categoryValidationService.CategoryNameExistsAsync(name, excludeCategoryId);

        #endregion

        #region Store Operations

        public IEnumerable<CategoryDto> SearchCategories( string? searchTerm = null, bool? isActive = null )
        {
            logger.LogDebug("Searching categories with term '{SearchTerm}', isActive {IsActive}", searchTerm, isActive);
            var categories = categoryStore.SearchCategories(searchTerm, isActive);
            return categories.ToDto();
        }

        public IEnumerable<CategoryDto> GetActiveCategories()
        {
            logger.LogDebug("Retrieving active categories from store");
            var categories = categoryStore.GetActiveCategories();
            return categories.ToDto();
        }

        #endregion
    }
}
