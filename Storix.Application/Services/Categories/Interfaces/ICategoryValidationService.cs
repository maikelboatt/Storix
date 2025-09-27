using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Categories
{
    public interface ICategoryValidationService
    {
        Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId, bool includeDeleted = false );


        Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false );


        Task<DatabaseResult> ValidateForDeletion( int categoryId );

        Task<DatabaseResult> ValidateForHardDeletion( int categoryId );

        Task<DatabaseResult> ValidateForRestore( int categoryId );

        Task<DatabaseResult<bool>> IsCategorySoftDeleted( int categoryId );
    }
}
