using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Main service for managing product operations with enhanced error handling.
    /// </summary>
    public class ProductService(
        IProductReadService productReadService,
        IProductWriteService productWriteService,
        IProductValidationService productValidationService,
        IProductStore productStore,
        ILogger<ProductService> logger ):IProductService
    {
        #region Read Operations

        public ProductDto? GetProductById( int productId ) => productReadService.GetProductById(productId);

        public ProductDto? GetProductBySku( string sku ) => productReadService.GetProductBySku(sku);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync() => await productReadService.GetAllProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync() => await productReadService.GetAllActiveProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId ) => await productReadService.GetProductsByCategoryAsync(categoryId);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId ) => await productReadService.GetProductsBySupplierAsync(supplierId);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync() => await productReadService.GetLowStockProductsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync() => await productReadService.GetProductsWithDetailsAsync();

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm ) => await productReadService.SearchProductsAsync(searchTerm);

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize ) =>
            await productReadService.GetProductsPagedAsync(pageNumber, pageSize);

        public async Task<DatabaseResult<int>> GetTotalProductCountAsync() => await productReadService.GetTotalProductCountAsync();

        public async Task<DatabaseResult<int>> GetActiveProductCountAsync() => await productReadService.GetActiveProductCountAsync();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto ) => await productWriteService.CreateProductAsync(createProductDto);

        public async Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto ) => await productWriteService.UpdateProductAsync(updateProductDto);

        public async Task<DatabaseResult> DeleteProductAsync( int productId ) => await productWriteService.DeleteProductAsync(productId);

        public async Task<DatabaseResult> SoftDeleteProductAsync( int productId ) => await productWriteService.SoftDeleteProductAsync(productId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> ProductExistsAsync( int productId ) => await productValidationService.ProductExistsAsync(productId);

        public async Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null ) =>
            await productValidationService.IsSkuAvailableAsync(sku, excludeProductId);

        #endregion

        #region Store Operations

        public IEnumerable<ProductDto> SearchProducts( string? searchTerm = null, int? categoryId = null, bool? isActive = null )
        {
            logger.LogDebug(
                "Searching products with term '{SearchTerm}', categoryId {CategoryId}, isActive {IsActive}",
                searchTerm,
                categoryId,
                isActive);

            IEnumerable<Product> products = productStore.SearchProducts(searchTerm, categoryId, isActive);
            return products.ToDto();
        }

        public IEnumerable<ProductDto> GetLowStockProducts()
        {
            logger.LogDebug("Retrieving low stock products from store");
            IEnumerable<Product> products = productStore.GetLowStockProducts();
            return products.ToDto();
        }

        #endregion
    }
}
