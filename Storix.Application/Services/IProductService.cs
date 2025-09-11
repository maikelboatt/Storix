using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.DTO;
using Storix.Application.DTO.Product;

namespace Storix.Application.Services
{
    /// <summary>
    ///     Service for managing products with business logic and validation.
    /// </summary>
    public interface IProductService
    {
        // Read Operations
        Task<ProductDto?> GetProductByIdAsync( int productId );

        Task<ProductDto?> GetProductBySkuAsync( string sku );

        Task<IEnumerable<ProductDto>> GetAllProductsAsync();

        Task<IEnumerable<ProductDto>> GetAllActiveProductsAsync();

        Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync( int categoryId );

        Task<IEnumerable<ProductDto>> GetProductsBySupplierAsync( int supplierId );

        Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();

        Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync();

        Task<IEnumerable<ProductDto>> SearchProductsAsync( string searchTerm );

        // Write Operations
        Task<ProductDto> CreateProductAsync( CreateProductDto createProductDto );

        Task<ProductDto> UpdateProductAsync( UpdateProductDto updateProductDto );

        Task<bool> DeleteProductAsync( int productId );

        Task<bool> SoftDeleteProductAsync( int productId );

        // Validation
        Task<bool> ProductExistsAsync( int productId );

        Task<bool> IsSkuAvailableAsync( string sku, int? excludeProductId = null );

        // Pagination
        Task<int> GetTotalProductCountAsync();

        Task<int> GetActiveProductCountAsync();

        Task<IEnumerable<ProductDto>> GetProductsPagedAsync( int pageNumber, int pageSize );
    }
}
