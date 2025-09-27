using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductValidationService
    {
        Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false );


        Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null, bool includeDeleted = false );

        Task<DatabaseResult> ValidateForDeletion( int productId );

        Task<DatabaseResult> ValidateForHardDeletion( int productId );

        Task<DatabaseResult> ValidateForRestore( int productId );

        Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId );
    }
}
