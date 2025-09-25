using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    /// <summary>
    ///     Interface for product write operations.
    /// </summary>
    public interface IProductWriteService
    {
        Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto );

        Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto );

        Task<DatabaseResult> DeleteProductAsync( int productId );

        Task<DatabaseResult> SoftDeleteProductAsync( int productId );
    }
}
