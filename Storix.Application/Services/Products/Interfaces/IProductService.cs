using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    public interface IProductService
    {
        Task<DatabaseResult<ProductDto>> GetProductById( int productId );

        Task<DatabaseResult<ProductDto>> GetProductBySku( string sku );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync();

        Task<DatabaseResult<IEnumerable<Product>>> GetAllActiveProductsAsync();

        Task<DatabaseResult<IEnumerable<TopProductDto>>> GetTop5BestSellersAsync( int topCounts = 5, int monthsBack = 3 );

        Task<DatabaseResult<IEnumerable<ProductListDto>>> GetAllActiveProductsForListAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync(
            int categoryId );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync(
            int supplierId );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync();

        Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync();

        Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync(
            string searchTerm );

        Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync(
            int pageNumber,
            int pageSize );

        Task<DatabaseResult<int>> GetTotalProductCountAsync();

        Task<DatabaseResult<int>> GetActiveProductCountAsync();

        Task<DatabaseResult<int>> GetDeletedProductCountAsync();

        Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto );

        Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto );

        Task<DatabaseResult> SoftDeleteProductAsync( int productId );

        Task<DatabaseResult> RestoreProductAsync( int productId );

        Task<DatabaseResult> HardDeleteProductAsync( int productId );

        Task<DatabaseResult<bool>> ProductExistsAsync( int productId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsSkuAvailableAsync(
            string sku,
            int? excludeProductId = null,
            bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsProductSoftDeleted( int productId );

        Task<DatabaseResult> ValidateForDeletion( int productId );

        Task<DatabaseResult> ValidateForHardDeletion( int productId );

        Task<DatabaseResult> ValidateForRestore( int productId );

        /// <summary>
        ///     Soft deletes multiple products in bulk.
        ///     Each product is validated and deleted individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> BulkSoftDeleteAsync( IEnumerable<int> productIds );

        /// <summary>
        ///     Restores multiple soft-deleted products in bulk.
        ///     Each product is validated and restored individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<ProductDto>>> BulkRestoreAsync( IEnumerable<int> productIds );
    }
}
