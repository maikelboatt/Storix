using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryService
    {
        CategoryDto? GetCategoryById( int categoryId, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync( bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllDeletedCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync( bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false );

        Task<DatabaseResult<int>> GetTotalCategoryCountAsync( bool includeDeleted = false );

        Task<DatabaseResult<int>> GetActiveCategoryCountAsync();

        Task<DatabaseResult<int>> GetDeletedCategoryCountAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm, bool includeDeleted = false );

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

        IEnumerable<CategoryDto> SearchCategories( string? searchTerm = null, bool includeDeleted = false );

        IEnumerable<CategoryDto> GetActiveCategoriesFromStore();

        IEnumerable<CategoryDto> GetDeletedCategoriesFromStore();

        void RefreshStoreCache();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> BulkSoftDeleteAsync( IEnumerable<int> categoryIds );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> BulkRestoreAsync( IEnumerable<int> categoryIds );
    }
}
