using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductReadService
    {
        Task<DatabaseResult<IEnumerable<Product>>> GetAllActiveProductsAsync();

        Task<DatabaseResult<IEnumerable<TopProductDto>>> GetTop5BestSellersAsync( int topCounts = 5, int monthsBack = 3 );

        Task<DatabaseResult<IEnumerable<ProductListDto>>> GetAllActiveProductsForListAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        Task<DatabaseResult<int>> GetActiveProductCountAsync();


        Task<DatabaseResult<ProductDto>> GetProductById( int productId );


        Task<DatabaseResult<ProductDto>> GetProductBySku( string sku );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync();


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId );


        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync();


        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize );


        Task<DatabaseResult<int>> GetTotalProductCountAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync();

        Task<DatabaseResult<int>> GetDeletedProductCountAsync();
    }
}
