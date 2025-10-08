using System.Collections.Generic;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    public interface IProductStore
    {
        void Initialize( List<Product> products );

        void Clear();

        ProductDto? Create( int productId, CreateProductDto createDto );

        ProductDto? Update( UpdateProductDto updateDto );

        bool Delete( int productId );

        ProductDto? GetById( int productId );

        ProductDto? GetBySKU( string sku );

        List<ProductDto> GetByBarcode( string barcode );

        List<ProductDto> GetAll(
            int? categoryId = null,
            int? supplierId = null,
            string? search = null,
            int skip = 0,
            int take = 100 );

        List<ProductDto> GetByCategory( int categoryId );

        List<ProductDto> GetBySupplier( int supplierId );

        List<ProductDto> GetActiveProducts();

        bool Exists( int productId );

        bool SKUExists( string sku, int? excludeProductId = null );

        bool BarcodeExists( string barcode, int? excludeProductId = null );

        int GetCount( int? categoryId = null, int? supplierId = null );

        int GetActiveCount();

        bool HasProductsInCategory( int categoryId );

        int GetProductCountInCategory( int categoryId );

        List<ProductDto> GetActiveProductsInCategory( int categoryId );

        bool HasProductsFromSupplier( int supplierId );

        int GetProductCountFromSupplier( int supplierId );

        List<ProductDto> GetLowStockProducts( Dictionary<int, int> currentStockLevels );

        IEnumerable<Product> SearchProducts( string? searchTerm = null, int? categoryId = null );
    }
}
