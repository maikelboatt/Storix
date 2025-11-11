using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryReadService
    {
        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalCategoryCountAsync();

        Task<DatabaseResult<IEnumerable<CategoryListDto>>> GetAllActiveCategoriesForListAsync();

        CategoryDto? GetCategoryById( int categoryId );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();

        Task<DatabaseResult<int>> GetActiveCategoryCountAsync();

        Task<DatabaseResult<int>> GetDeletedCategoryCountAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllDeletedCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm );
    }
}
