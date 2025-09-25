using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Products.Interfaces
{
    /// <summary>
    ///     Interface for product validation operations.
    /// </summary>
    public interface IProductValidationService
    {
        Task<DatabaseResult<bool>> ProductExistsAsync( int productId );

        Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null );

        Task<DatabaseResult> ValidateForDeletion( int productId );
    }
}
