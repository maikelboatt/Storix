using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryService
    {
        CategoryDto? GetCategoryById( int categoryId );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryListDto>>> GetAllActiveCategoriesForListAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllDeletedCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalCategoryCountAsync();

        Task<DatabaseResult<int>> GetActiveCategoryCountAsync();

        Task<DatabaseResult<int>> GetDeletedCategoryCountAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm );

        Task<DatabaseResult<CategoryDto>> CreateCategoryAsync( CreateCategoryDto createCategoryDto );

        Task<DatabaseResult<CategoryDto>> UpdateCategoryAsync( UpdateCategoryDto updateCategoryDto );

        Task<DatabaseResult> SoftDeleteCategoryAsync( int categoryId );

        Task<DatabaseResult> RestoreCategoryAsync( int categoryId );

        Task<DatabaseResult> HardDeleteCategoryAsync( int categoryId );

        Task<DatabaseResult> DeleteCategoryAsync( int categoryId );

        Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsCategorySoftDeleted( int categoryId );

        Task<DatabaseResult> ValidateForDeletion( int categoryId );

        Task<DatabaseResult> ValidateForHardDeletion( int categoryId );

        Task<DatabaseResult> ValidateForRestore( int categoryId );

        IEnumerable<CategoryDto> SearchCategoriesInCache( string? searchTerm = null );

        CategoryDto? GetCategoryByIdInCache( int categoryId );

        IEnumerable<CategoryDto> GetSubCategoriesInCache( int parentCategoryId );

        IEnumerable<CategoryDto> GetRootCategoriesInCache();

        IEnumerable<CategoryDto> GetAllActiveCategoriesInCache();

        bool CategoryExistsInCache( int categoryId );

        int GetCategoryActiveCountInCache();

        bool CategoryHasSubCategoriesInCache( int categoryId );

        void RefreshStoreCache();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> BulkSoftDeleteAsync( IEnumerable<int> categoryIds );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> BulkRestoreAsync( IEnumerable<int> categoryIds );
    }
}
