using System.Collections.Generic;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    public interface IProductStore
    {
        // Store Initialization
        void Initialize( List<Product> products );

        void Clear();

        // Basic CRUD - Database provides the ID for Create
        ProductDto? Create( int productId, ProductDto productDto );

        ProductDto? GetById( int productId );

        ProductDto? Update( ProductDto productDto );

        bool Delete( int productId );

        // Query Operations
        List<ProductDto> GetAll( int? categoryId = null, int? supplierId = null, string? search = null, bool? isActive = null, int skip = 0, int take = 100 );

        List<ProductDto> GetByCategory( int categoryId );

        List<ProductDto> GetBySupplier( int supplierId );

        List<ProductDto> GetBySKU( string sku );

        List<ProductDto> GetByBarcode( string barcode );

        List<ProductDto> GetActiveProducts();

        List<ProductDto> GetInactiveProducts();

        // Utility Operations
        bool Exists( int productId );

        bool SKUExists( string sku, int? excludeProductId = null );

        bool BarcodeExists( string barcode, int? excludeProductId = null );

        int GetCount( int? categoryId = null, int? supplierId = null, bool? isActive = null );

        // Category-specific operations
        bool HasProductsInCategory( int categoryId );

        int GetProductCountInCategory( int categoryId );

        IEnumerable<Product> SearchProducts( string? searchTerm = null, int? categoryId = null, bool? isActive = null );

        IEnumerable<Product> GetLowStockProducts();

        List<ProductDto> GetActiveProductsInCategory( int categoryId );

        // Supplier-specific operations
        bool HasProductsFromSupplier( int supplierId );

        int GetProductCountFromSupplier( int supplierId );
    }
}
