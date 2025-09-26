using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    public class ProductStore:IProductStore
    {
        private readonly Dictionary<int, Product> _products;

        public ProductStore( List<Product>? initialProducts = null )
        {
            _products = new Dictionary<int, Product>();

            if (initialProducts == null) return;

            foreach (Product product in initialProducts)
            {
                _products[product.ProductId] = product;
            }
        }

        public void Initialize( List<Product> products )
        {
            // Clear existing data first
            _products.Clear();

            // Add all products to the store
            foreach (Product product in products)
            {
                _products[product.ProductId] = product;
            }
        }

        public void Clear()
        {
            _products.Clear();
        }

        public ProductDto? Create( int productId, ProductDto productDto )
        {
            // Simple validation - return null if invalid
            if (string.IsNullOrWhiteSpace(productDto.Name))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(productDto.SKU))
            {
                return null;
            }

            if (SKUExists(productDto.SKU))
            {
                return null; // SKU must be unique
            }

            if (!string.IsNullOrEmpty(productDto.Barcode) && BarcodeExists(productDto.Barcode))
            {
                return null; // Barcode must be unique if provided
            }

            Product product = new(
                productId,
                productDto.Name.Trim(),
                productDto.SKU.Trim(),
                productDto.Description?.Trim() ?? string.Empty,
                productDto.Barcode?.Trim(),
                productDto.Price,
                productDto.Cost,
                productDto.MinStockLevel,
                productDto.MaxStockLevel,
                productDto.SupplierId,
                productDto.CategoryId,
                productDto.IsActive,
                DateTime.UtcNow
            );

            _products[productId] = product;
            return product.ToDto();
        }

        public ProductDto? GetById( int productId )
        {
            _products.TryGetValue(productId, out Product? product);
            return product?.ToDto();
        }

        public ProductDto? Update( ProductDto productDto )
        {
            if (!_products.TryGetValue(productDto.ProductId, out Product? existingProduct))
            {
                return null; // Product not found
            }

            if (string.IsNullOrWhiteSpace(productDto.Name))
            {
                return null; // Invalid name
            }

            if (string.IsNullOrWhiteSpace(productDto.SKU))
            {
                return null; // Invalid SKU
            }

            if (SKUExists(productDto.SKU, productDto.ProductId))
            {
                return null; // SKU already exists for another product
            }

            if (!string.IsNullOrEmpty(productDto.Barcode) && BarcodeExists(productDto.Barcode, productDto.ProductId))
            {
                return null; // Barcode already exists for another product
            }

            Product updatedProduct = existingProduct with
            {
                Name = productDto.Name.Trim(),
                SKU = productDto.SKU.Trim(),
                Description = productDto.Description?.Trim() ?? string.Empty,
                Barcode = productDto.Barcode?.Trim(),
                Price = productDto.Price,
                Cost = productDto.Cost,
                MinStockLevel = productDto.MinStockLevel,
                MaxStockLevel = productDto.MaxStockLevel,
                SupplierId = productDto.SupplierId,
                CategoryId = productDto.CategoryId,
                IsActive = productDto.IsActive,
                UpdatedDate = DateTime.UtcNow
            };

            _products[productDto.ProductId] = updatedProduct;
            return updatedProduct.ToDto();
        }

        public bool Delete( int productId )
        {
            if (!_products.ContainsKey(productId))
            {
                return false; // Product not found
            }

            _products.Remove(productId);
            return true; // Successfully deleted
        }

        public List<ProductDto> GetAll( int? categoryId = null, int? supplierId = null, string? search = null, bool? isActive = null, int skip = 0, int take = 100 )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

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

            // Filter by active status
            if (isActive.HasValue)
            {
                products = products.Where(p => p.IsActive == isActive.Value);
            }

            // Filter by search
            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLowerInvariant();
                products = products.Where(p =>
                                              p.Name.ToLowerInvariant().Contains(searchLower) ||
                                              p.SKU.ToLowerInvariant().Contains(searchLower) ||
                                              p.Description.ToLowerInvariant().Contains(searchLower) ||
                                              !string.IsNullOrEmpty(p.Barcode) && p.Barcode.ToLowerInvariant().Contains(searchLower));
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

        public List<ProductDto> GetBySKU( string sku )
        {
            return _products.Values
                            .Where(p => p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase))
                            .Select(p => p.ToDto())
                            .ToList();
        }

        public List<ProductDto> GetByBarcode( string barcode )
        {
            return _products.Values
                            .Where(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                            .Select(p => p.ToDto())
                            .ToList();
        }

        public List<ProductDto> GetActiveProducts() => GetAll(isActive: true);

        public List<ProductDto> GetInactiveProducts() => GetAll(isActive: false);

        public bool Exists( int productId ) => _products.ContainsKey(productId);

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

        public int GetCount( int? categoryId = null, int? supplierId = null, bool? isActive = null )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue)
            {
                products = products.Where(p => p.SupplierId == supplierId.Value);
            }

            if (isActive.HasValue)
            {
                products = products.Where(p => p.IsActive == isActive.Value);
            }

            return products.Count();
        }

        // Category-specific operations
        public bool HasProductsInCategory( int categoryId )
        {
            return _products.Values.Any(p => p.CategoryId == categoryId);
        }

        public int GetProductCountInCategory( int categoryId ) => GetCount(categoryId);

        public List<ProductDto> GetActiveProductsInCategory( int categoryId ) => GetAll(categoryId, isActive: true);

        // Supplier-specific operations
        public bool HasProductsFromSupplier( int supplierId )
        {
            return _products.Values.Any(p => p.SupplierId == supplierId);
        }

        public int GetProductCountFromSupplier( int supplierId ) => GetCount(supplierId: supplierId);

        public IEnumerable<Product> SearchProducts( string? searchTerm = null, int? categoryId = null, bool? isActive = null )
        {
            IEnumerable<Product> query = _products.Values.AsEnumerable();

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

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            return query.OrderBy(p => p.Name).ToList();
        }

        public IEnumerable<Product> GetLowStockProducts()
        {
            return _products.Values
                            .Where(p => p.IsActive)
                            .OrderBy(p => p.Name)
                            .ToList();
        }

        public List<ProductDto> GetLowStockProducts( Dictionary<int, int> currentStockLevels )
        {
            return _products.Values
                            .Where(p => currentStockLevels.ContainsKey(p.ProductId) && p.IsLowStock(currentStockLevels[p.ProductId]))
                            .Select(p => p.ToDto())
                            .ToList();
        }
    }
}
