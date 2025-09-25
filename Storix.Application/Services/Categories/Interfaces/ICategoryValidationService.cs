using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryValidationService
    {
        Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId );

        Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null );

        Task<DatabaseResult> ValidateForDeletion( int categoryId );
    }
}
