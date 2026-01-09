using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    /// <summary>
    ///     In-memory cache for active (non-deleted) products.
    ///     Provides fast lookup for frequently accessed product data.
    /// </summary>
    public class ProductStore:IProductStore
    {
        private readonly ICategoryCacheReadService _categoryCacheReadService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        private readonly Dictionary<int, Product> _products;
        private readonly Dictionary<int, ProductListDto> _productListDtos;
        private readonly Dictionary<int, TopProductDto> _topProductDtos;

        private readonly Dictionary<int, int> _productStockCache;

        public ProductStore(
            ICategoryCacheReadService categoryCacheReadService,
            ISupplierCacheReadService supplierCacheReadService,
            IInventoryCacheReadService inventoryCacheReadService,
            List<Product>? initialProducts = null )
        {
            _categoryCacheReadService = categoryCacheReadService;
            _supplierCacheReadService = supplierCacheReadService;
            _inventoryCacheReadService = inventoryCacheReadService;
            _products = new Dictionary<int, Product>();
            _productListDtos = new Dictionary<int, ProductListDto>();
            _topProductDtos = new Dictionary<int, TopProductDto>();
            _productStockCache = new Dictionary<int, int>();

            if (initialProducts == null) return;

            // Only cache active products
            foreach (Product product in initialProducts.Where(p => !p.IsDeleted))
            {
                _products[product.ProductId] = product;
            }
        }

        public void Initialize( List<Product> products )
        {
            Clear();

            // Only cache active products
            foreach (Product product in products.Where(p => !p.IsDeleted))
            {
                _products[product.ProductId] = product;
            }
        }

        public void InitializeProductList( List<ProductListDto> productListDtos )
        {
            _productListDtos.Clear();

            foreach (ProductListDto productListDto in productListDtos)
            {
                _productListDtos[productListDto.ProductId] = productListDto;
                _productStockCache[productListDto.ProductId] = productListDto.CurrentStock;
            }
        }

        public string GetCategoryName( int categoryId )
        {
            CategoryDto? category = _categoryCacheReadService.GetCategoryByIdInCache(categoryId);
            return category?.Name ?? "Unknown";
        }

        public string GetSupplierName( int supplierId )
        {
            SupplierDto? supplier = _supplierCacheReadService.GetSupplierByIdInCache(supplierId);
            return supplier?.Name ?? "Unknown";
        }

        public void InitializeTopProducts( List<TopProductDto> topProducts )
        {
            _topProductDtos.Clear();

            foreach (TopProductDto topProduct in topProducts)
            {
                _topProductDtos[topProduct.ProductId] = topProduct;
            }
        }

        public void Clear()
        {
            _products.Clear();
            _topProductDtos.Clear();
            _productStockCache.Clear();
        }

        #region Events

        /// <summary>
        ///     Event triggered when a product is added.
        /// </summary>
        public event Action<Product>? ProductAdded;

        /// <summary>
        ///     Event triggered when a product is updated.
        /// </summary>
        public event Action<Product>? ProductUpdated;

        /// <summary>
        ///     Event triggered when a product is deleted.
        /// </summary>
        public event Action<int>? ProductDeleted;

        /// <summary>
        ///    Event triggered when product stock changes at any location
        /// </summary>
        public event Action<int, int>? ProductStockChanged; // (ProductId, TotalStock)

        #endregion

        #region Write Operations

        public ProductDto? Create( int productId, CreateProductDto createDto )
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(createDto.SKU))
            {
                return null;
            }

            if (SKUExists(createDto.SKU))
            {
                return null; // SKU must be unique among active products
            }

            if (!string.IsNullOrEmpty(createDto.Barcode) && BarcodeExists(createDto.Barcode))
            {
                return null; // Barcode must be unique among active products if provided
            }

            Product product = new(
                productId,
                createDto.Name.Trim(),
                createDto.SKU.Trim(),
                createDto.Description.Trim(),
                createDto.Barcode?.Trim(),
                createDto.Price,
                createDto.Cost,
                createDto.MinStockLevel,
                createDto.MaxStockLevel,
                createDto.SupplierId,
                createDto.CategoryId,
                DateTime.UtcNow
            );

            _products[productId] = product;
            ProductAdded?.Invoke(product);
            return product.ToDto();
        }

        public ProductDto? Update( UpdateProductDto updateDto )
        {
            // Only update active products
            if (!_products.TryGetValue(updateDto.ProductId, out Product? existingProduct))
            {
                return null; // Product not found in active cache
            }

            if (string.IsNullOrWhiteSpace(updateDto.Name))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(updateDto.SKU))
            {
                return null;
            }

            if (SKUExists(updateDto.SKU, updateDto.ProductId))
            {
                return null; // SKU already exists for another active product
            }

            if (!string.IsNullOrEmpty(updateDto.Barcode) && BarcodeExists(updateDto.Barcode, updateDto.ProductId))
            {
                return null; // Barcode already exists for another active product
            }

            Product updatedProduct = existingProduct with
            {
                Name = updateDto.Name.Trim(),
                SKU = updateDto.SKU.Trim(),
                Description = updateDto.Description.Trim(),
                Barcode = updateDto.Barcode?.Trim(),
                Price = updateDto.Price,
                Cost = updateDto.Cost,
                MinStockLevel = updateDto.MinStockLevel,
                MaxStockLevel = updateDto.MaxStockLevel,
                SupplierId = updateDto.SupplierId,
                CategoryId = updateDto.CategoryId,
                UpdatedDate = DateTime.UtcNow
            };

            _products[updateDto.ProductId] = updatedProduct;
            ProductUpdated?.Invoke(updatedProduct);
            return updatedProduct.ToDto();
        }

        public bool Delete( int productId )
        {
            // Remove from active cache (soft delete removes from cache, hard delete calls this too)
            ProductDeleted?.Invoke(productId);
            _productListDtos.Remove(productId);
            _productStockCache.Remove(productId);
            return _products.Remove(productId);
        }

        #endregion

        #region Stock Update Methods

        /// <summary>
        /// Updates the cached stock for a product and notifies subscribers.
        /// Call this after inventory adjustments to keep UI in sync.
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="totalStock">New total stock across all locations</param>
        public void UpdateProductStock( int productId, int totalStock )
        {
            // Update stock cache
            _productStockCache[productId] = totalStock;

            // Update ProductListDto if it exists
            if (_productListDtos.TryGetValue(productId, out ProductListDto? productListDto))
            {
                // Create updated DTO with new stock
                ProductListDto updatedDto = productListDto with
                {
                    CurrentStock = totalStock
                };
                _productListDtos[productId] = updatedDto;
            }

            // Notify subscribers (ViewModels listening for changes)
            ProductStockChanged?.Invoke(productId, totalStock);
        }

        /// <summary>
        /// Gets the current total stock for a product (cached value).
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <returns>Total stock across all locations, or 0 if not found</returns>
        public int GetProductTotalStock( int productId )
        {
            if (_productStockCache.TryGetValue(productId, out int stock))
            {
                return stock;
            }

            // Fallback to inventory cache read service
            return _inventoryCacheReadService.GetCurrentStockForProductInCache(productId);
        }

        /// <summary>
        /// Gets the product name (helper for order items and displays).
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <returns>Product name or null if not found</returns>
        public string? GetProductName( int productId ) => _products.TryGetValue(productId, out Product? product)
            ? product.Name
            : null;

        /// <summary>
        /// Gets the product SKU (helper for order items and displays).
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <returns>Product SKU or null if not found</returns>
        public string? GetProductSku( int productId ) => _products.TryGetValue(productId, out Product? product)
            ? product.SKU
            : null;

        #endregion

        #region Read Operations

        public ProductDto? GetById( int productId ) =>
            // Only searches active products
            _products.TryGetValue(productId, out Product? product)
                ? product.ToDto()
                : null;

        public ProductDto? GetBySKU( string sku )
        {
            Product? product = _products.Values
                                        .FirstOrDefault(p => p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase));

            return product?.ToDto();
        }

        public List<ProductDto> GetByBarcode( string barcode )
        {
            return _products
                   .Values
                   .Where(p => !string.IsNullOrEmpty(p.Barcode) &&
                               p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                   .Select(p => p.ToDto())
                   .ToList();
        }

        public List<ProductDto> GetAll(
            int? categoryId = null,
            int? supplierId = null,
            string? search = null,
            int skip = 0,
            int take = 100 )
        {
            IEnumerable<Product> products = _products.Values;

            // Filter by category
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by supplier
            if (supplierId.HasValue)
            {
                products = products.Where(p => p.SupplierId == supplierId.Value);
            }

            // Filter by search
            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLowerInvariant();
                products = products.Where(p =>
                                              p
                                                  .Name.ToLowerInvariant()
                                                  .Contains(searchLower) ||
                                              p
                                                  .SKU.ToLowerInvariant()
                                                  .Contains(searchLower) ||
                                              p
                                                  .Description.ToLowerInvariant()
                                                  .Contains(searchLower) ||
                                              !string.IsNullOrEmpty(p.Barcode) && p
                                                                                  .Barcode.ToLowerInvariant()
                                                                                  .Contains(searchLower));
            }

            return products
                   .OrderBy(p => p.Name)
                   .Skip(skip)
                   .Take(take)
                   .Select(p => p.ToDto())
                   .ToList();
        }

        public List<TopProductDto> GetTop5BestSellersAsync( int topCounts )
        {
            return _topProductDtos
                   .Take(topCounts)
                   .Select(p => p.Value)
                   .ToList();
        }

        public List<ProductListDto> GetProductListDto()
        {
            return _productListDtos
                   .Select(p => p.Value)
                   .ToList();
        }

        public List<ProductDto> GetByCategory( int categoryId ) => GetAll(categoryId);

        public List<ProductListDto> GetProductListByCategory( int categoryId )
        {
            List<ProductDto> products = GetByCategory(categoryId);

            List<ProductListDto> productListDtos = [];
            productListDtos.AddRange(
                from dto in products
                let supplierName = GetSupplierName(dto.SupplierId)
                let currentStock = GetProductTotalStock(dto.ProductId)
                let categoryName = GetCategoryName(categoryId)
                select dto.ToListDto(categoryName, supplierName, currentStock));

            return productListDtos;
        }

        public List<ProductListDto> GetProductListBySupplier( int supplierId )
        {
            List<ProductDto> products = GetBySupplier(supplierId);

            List<ProductListDto> productListDtos = [];
            productListDtos.AddRange(
                from dto in products
                let categoryName = GetCategoryName(dto.CategoryId)
                let currentStock = GetProductTotalStock(dto.ProductId)
                let supplierName = GetSupplierName(supplierId)
                select dto.ToListDto(categoryName, supplierName, currentStock));

            return productListDtos;
        }

        public List<ProductDto> GetBySupplier( int supplierId ) => GetAll(supplierId: supplierId);

        public List<ProductDto> GetActiveProducts()
        {
            return _products
                   .Values
                   .OrderBy(p => p.Name)
                   .Select(p => p.ToDto())
                   .ToList();
        }

        #endregion

        #region Validation

        public bool Exists( int productId ) =>
            // Only checks active products
            _products.ContainsKey(productId);

        public bool SKUExists( string sku, int? excludeProductId = null )
        {
            return _products.Values.Any(p =>
                                            p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase) &&
                                            p.ProductId != excludeProductId);
        }

        public bool BarcodeExists( string barcode, int? excludeProductId = null )
        {
            return _products.Values.Any(p =>
                                            !string.IsNullOrEmpty(p.Barcode) &&
                                            p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase) &&
                                            p.ProductId != excludeProductId);
        }

        #endregion

        #region Statistics

        public int GetCount( int? categoryId = null, int? supplierId = null )
        {
            IEnumerable<Product> products = _products.Values;

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue)
            {
                products = products.Where(p => p.SupplierId == supplierId.Value);
            }

            return products.Count();
        }

        public int GetActiveCount() => _products.Count;

        // Category-specific operations
        public bool HasProductsInCategory( int categoryId )
        {
            return _products.Values.Any(p => p.CategoryId == categoryId);
        }

        public int GetProductCountInCategory( int categoryId ) => GetCount(categoryId);

        public List<ProductDto> GetActiveProductsInCategory( int categoryId ) => GetAll(categoryId);

        // Supplier-specific operations
        public bool HasProductsFromSupplier( int supplierId )
        {
            return _products.Values.Any(p => p.SupplierId == supplierId);
        }

        public int GetProductCountFromSupplier( int supplierId ) => GetCount(supplierId: supplierId);

        public List<ProductDto> GetLowStockProducts( Dictionary<int, int> currentStockLevels )
        {
            return _products
                   .Values
                   .Where(p => currentStockLevels.ContainsKey(p.ProductId) &&
                               p.IsLowStock(currentStockLevels[p.ProductId]))
                   .Select(p => p.ToDto())
                   .ToList();
        }

        public IEnumerable<Product> SearchProducts( string? searchTerm = null, int? categoryId = null )
        {
            IEnumerable<Product> query = _products.Values;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                                        p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                        p.SKU.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                        (p.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            return query
                   .OrderBy(p => p.Name)
                   .ToList();
        }

        #endregion
    }
}
