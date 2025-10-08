using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    /// <summary>
    ///     In-memory cache for active (non-deleted) products.
    ///     Provides fast lookup for frequently accessed product data.
    /// </summary>
    public class ProductStore:IProductStore
    {
        private readonly Dictionary<int, Product> _products;

        public ProductStore( List<Product>? initialProducts = null )
        {
            _products = new Dictionary<int, Product>();

            if (initialProducts == null) return;

            // Only cache active products
            foreach (Product product in initialProducts.Where(p => !p.IsDeleted))
            {
                _products[product.ProductId] = product;
            }
        }

        public void Initialize( List<Product> products )
        {
            _products.Clear();

            // Only cache active products
            foreach (Product product in products.Where(p => !p.IsDeleted))
            {
                _products[product.ProductId] = product;
            }
        }

        public void Clear()
        {
            _products.Clear();
        }

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
                createDto.Description?.Trim() ?? string.Empty,
                createDto.Barcode?.Trim(),
                createDto.Price,
                createDto.Cost,
                createDto.MinStockLevel,
                createDto.MaxStockLevel,
                createDto.SupplierId,
                createDto.CategoryId,
                DateTime.UtcNow,
                null,
                false,
                null
            );

            _products[productId] = product;
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
                Description = updateDto.Description?.Trim() ?? string.Empty,
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
            return updatedProduct.ToDto();
        }

        public bool Delete( int productId ) =>
            // Remove from active cache (soft delete removes from cache, hard delete calls this too)
            _products.Remove(productId);

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

        public List<ProductDto> GetByCategory( int categoryId ) => GetAll(categoryId);

        public List<ProductDto> GetBySupplier( int supplierId ) => GetAll(supplierId: supplierId);

        public List<ProductDto> GetActiveProducts()
        {
            return _products
                   .Values
                   .OrderBy(p => p.Name)
                   .Select(p => p.ToDto())
                   .ToList();
        }

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
    }
}
