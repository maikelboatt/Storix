using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Products.Interfaces
{
    public interface IProductService
    {
        ProductDto? GetProductById( int productId, bool includeDeleted = false );


        ProductDto? GetProductBySku( string sku, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync( bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync( bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm, bool includeDeleted = false );


        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false );


        Task<DatabaseResult<int>> GetTotalProductCountAsync( bool includeDeleted = false );


        Task<DatabaseResult<int>> GetActiveProductCountAsync();

        Task<DatabaseResult<int>> GetDeletedProductCountAsync();

        Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto );

        Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto );

        Task<DatabaseResult> SoftDeleteProductAsync( int productId );

        Task<DatabaseResult> RestoreProductAsync( int productId );

        Task<DatabaseResult> HardDeleteProductAsync( int productId );

        Task<DatabaseResult> DeleteProductAsync( int productId );

        Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false );


        Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId );

        Task<DatabaseResult> ValidateForDeletion( int productId );

        Task<DatabaseResult> ValidateForHardDeletion( int productId );

        Task<DatabaseResult> ValidateForRestore( int productId );

        IEnumerable<ProductDto> SearchProducts( string? searchTerm = null, int? categoryId = null, bool includeDeleted = false );


        IEnumerable<ProductDto> GetLowStockProducts( bool includeDeleted = false );


        IEnumerable<ProductDto> GetDeletedProductsFromStore();

        void RefreshStoreCache();

        Task<DatabaseResult<IEnumerable<ProductDto>>> BulkSoftDeleteAsync( IEnumerable<int> productIds );

        Task<DatabaseResult<IEnumerable<ProductDto>>> BulkRestoreAsync( IEnumerable<int> productIds );
    }
}
