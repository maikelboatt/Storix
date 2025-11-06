using System;
using System.Collections.Generic;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    public interface IProductStore
    {
        void Initialize( List<Product> products );

        void InitializeProductList( List<ProductListDto> productListDtos );

        void InitializeTopProducts( List<TopProductDto> topProducts );

        void Clear();

        /// <summary>
        ///     Event triggered when a product is added.
        /// </summary>
        event Action<Product> ProductAdded;

        /// <summary>
        ///     Event triggered when a product is updated.
        /// </summary>
        event Action<Product> ProductUpdated;

        /// <summary>
        ///     Event triggered when a product is deleted.
        /// </summary>
        event Action<int> ProductDeleted;


        ProductDto? Create( int productId, CreateProductDto createDto );

        ProductDto? Update( UpdateProductDto updateDto );

        bool Delete( int productId );

        string GetCategoryName( int categoryId );

        string GetSupplierName( int supplierId );

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

        List<TopProductDto> GetTop5BestSellersAsync( int topCounts );

        List<ProductListDto> GetProductListDto();

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
