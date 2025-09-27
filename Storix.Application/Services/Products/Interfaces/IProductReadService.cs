using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductReadService
    {
        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        Task<DatabaseResult<int>> GetActiveProductCountAsync();


        ProductDto? GetProductById( int productId, bool includeDeleted = false );


        ProductDto? GetProductBySku( string sku, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync( bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync( bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false );


        Task<DatabaseResult<int>> GetTotalProductCountAsync( bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync();

        Task<DatabaseResult<int>> GetDeletedProductCountAsync();
    }
}
