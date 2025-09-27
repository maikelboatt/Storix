using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductWriteService
    {
        Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto );

        Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto );

        Task<DatabaseResult> SoftDeleteProductAsync( int productId );

        Task<DatabaseResult> RestoreProductAsync( int productId );

        Task<DatabaseResult> HardDeleteProductAsync( int productId );
    }
}
