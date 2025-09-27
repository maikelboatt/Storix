using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryReadService
    {
        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync( bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false );

        Task<DatabaseResult<int>> GetTotalCategoryCountAsync( bool includeDeleted = false );

        CategoryDto? GetCategoryById( int categoryId, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync( bool includeDeleted = false );

        Task<DatabaseResult<int>> GetActiveCategoryCountAsync();

        Task<DatabaseResult<int>> GetDeletedCategoryCountAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllDeletedCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm, bool includeDeleted = false );
    }
}
