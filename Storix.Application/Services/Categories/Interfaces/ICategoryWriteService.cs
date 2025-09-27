using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryWriteService
    {
        Task<DatabaseResult<CategoryDto>> CreateCategoryAsync( CreateCategoryDto createCategoryDto );

        Task<DatabaseResult<CategoryDto>> UpdateCategoryAsync( UpdateCategoryDto updateCategoryDto );

        Task<DatabaseResult> SoftDeleteCategoryAsync( int categoryId );

        Task<DatabaseResult> RestoreCategoryAsync( int categoryId );

        Task<DatabaseResult> HardDeleteCategoryAsync( int categoryId );
    }
}
