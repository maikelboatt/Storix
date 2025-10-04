using System.Collections.Generic;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    public interface IProductStore
    {
        void Initialize( List<Product> products );

        void Clear();

        ProductDto? Create( int productId, ProductDto productDto );

        ProductDto? GetById( int productId, bool includeDeleted = false );

        ProductDto? Update( ProductDto productDto );

        bool Delete( int productId );

        bool SoftDelete( int productId );

        bool Restore( int productId );

        List<ProductDto> GetAll( int? categoryId = null,
            int? supplierId = null,
            string? search = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 100 );

        List<ProductDto> GetByCategory( int categoryId, bool includeDeleted = false );

        List<ProductDto> GetBySupplier( int supplierId, bool includeDeleted = false );

        ProductDto? GetBySKU( string sku, bool includeDeleted = false );

        List<ProductDto> GetByBarcode( string barcode, bool includeDeleted = false );

        List<ProductDto> GetActiveProducts();

        List<ProductDto> GetDeletedProducts();

        bool Exists( int productId, bool includeDeleted = false );

        bool IsDeleted( int productId );

        bool SKUExists( string sku, int? excludeProductId = null, bool includeDeleted = false );

        bool BarcodeExists( string barcode, int? excludeProductId = null, bool includeDeleted = false );

        int GetCount( int? categoryId = null, int? supplierId = null, bool includeDeleted = false );

        int GetActiveCount();

        int GetDeletedCount();

        int GetTotalCount();

        bool HasProductsInCategory( int categoryId, bool includeDeleted = false );

        int GetProductCountInCategory( int categoryId, bool includeDeleted = false );

        List<ProductDto> GetActiveProductsInCategory( int categoryId );

        bool HasProductsFromSupplier( int supplierId, bool includeDeleted = false );

        int GetProductCountFromSupplier( int supplierId, bool includeDeleted = false );

        IEnumerable<Product> SearchProducts( string? searchTerm = null, int? categoryId = null, bool includeDeleted = false );

        IEnumerable<Product> GetLowStockProducts( bool includeDeleted = false );

        List<ProductDto> GetLowStockProducts( Dictionary<int, int> currentStockLevels, bool includeDeleted = false );
    }
}
