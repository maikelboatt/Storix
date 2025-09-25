using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    /// <summary>
    ///     Interface for product read operations.
    /// </summary>
    public interface IProductReadService
    {
        ProductDto? GetProductById( int productId );

        ProductDto? GetProductBySku( string sku );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalProductCountAsync();

        Task<DatabaseResult<int>> GetActiveProductCountAsync();
    }
}
