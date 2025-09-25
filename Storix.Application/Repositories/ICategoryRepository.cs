using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync( int categoryId );

        Task<IEnumerable<Category>> GetAllAsync();

        Task<IEnumerable<Category>> GetRootCategoriesAsync();

        Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId );

        Task<Category> CreateAsync( Category category );

        Task<Category> UpdateAsync( Category category );

        Task<bool> DeleteAsync( int categoryId );

        Task<bool> ExistsAsync( int categoryId );

        Task<bool> NameExistsAsync( string name, int? excludeCategoryId = null );

        Task<int> GetTotalCountAsync();

        Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize );
    }
}
