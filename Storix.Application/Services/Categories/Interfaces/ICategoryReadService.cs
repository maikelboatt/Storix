using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryReadService
    {
        CategoryDto? GetCategoryById( int categoryId );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync();

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId );

        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalCategoryCountAsync();
    }
}
