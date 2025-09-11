using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.DTO;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync( int productId );

        Task<Product?> GetBySkuAsync( string sku );

        Task<IEnumerable<Product>> GetAllAsync();

        Task<IEnumerable<Product>> GetAllActiveAsync();

        Task<IEnumerable<Product>> GetByCategoryAsync( int categoryId );

        Task<IEnumerable<Product>> GetBySupplierAsync( int supplierId );

        Task<IEnumerable<Product>> GetLowStockProductsAsync();

        Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync(); // Special query DTO

        Task<IEnumerable<Product>> SearchAsync( string searchTerm );

        Task<Product> CreateAsync( Product product );

        Task<Product> UpdateAsync( Product product );

        Task<bool> DeleteAsync( int productId );

        Task<bool> SoftDeleteAsync( int productId );

        Task<bool> ExistsAsync( int productId );

        Task<bool> SkuExistsAsync( string sku, int? excludeProductId = null );

        Task<int> GetTotalCountAsync();

        Task<int> GetActiveCountAsync();

        Task<IEnumerable<Product>> GetPagedAsync( int pageNumber, int pageSize );
    }
}
