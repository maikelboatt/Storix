using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Products;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Products
{
    public class ProductStore:IProductStore
    {
        private readonly Dictionary<int, Product> _deletedProducts;
        private readonly Dictionary<int, Product> _products;

        public ProductStore( List<Product>? initialProducts = null )
        {
            _products = new Dictionary<int, Product>();
            _deletedProducts = new Dictionary<int, Product>();

            if (initialProducts == null) return;

            foreach (Product product in initialProducts)
            {
                if (product.IsDeleted)
                {
                    _deletedProducts[product.ProductId] = product;
                }
                else
                {
                    _products[product.ProductId] = product;
                }
            }
        }

        public void Initialize( List<Product> products )
        {
            // Clear existing data first
            _products.Clear();
            _deletedProducts.Clear();

            // Add products to appropriate dictionaries based on deletion status
            foreach (Product product in products)
            {
                if (product.IsDeleted)
                {
                    _deletedProducts[product.ProductId] = product;
                }
                else
                {
                    _products[product.ProductId] = product;
                }
            }
        }

        public void Clear()
        {
            _products.Clear();
            _deletedProducts.Clear();
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

            if (SKUExists(productDto.SKU, includeDeleted: false))
            {
                return null; // SKU must be unique among active products
            }

            if (!string.IsNullOrEmpty(productDto.Barcode) && BarcodeExists(productDto.Barcode, includeDeleted: false))
            {
                return null; // Barcode must be unique among active products if provided
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
                DateTime.UtcNow // DeletedAt
            );

            _products[productId] = product;
            return product.ToDto();
        }

        public ProductDto? GetById( int productId, bool includeDeleted = false )
        {
            if (_products.TryGetValue(productId, out Product? product))
            {
                return product.ToDto();
            }

            if (includeDeleted && _deletedProducts.TryGetValue(productId, out Product? deletedProduct))
            {
                return deletedProduct.ToDto();
            }

            return null;
        }


        public ProductDto? Update( ProductDto productDto )
        {
            // Try to find the product in either collection
            Product? existingProduct = null;
            bool isCurrentlyDeleted = false;

            if (_products.TryGetValue(productDto.ProductId, out existingProduct))
            {
                isCurrentlyDeleted = false;
            }
            else if (_deletedProducts.TryGetValue(productDto.ProductId, out existingProduct))
            {
                isCurrentlyDeleted = true;
            }
            else
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
                return null; // SKU already exists for another active product
            }

            if (!string.IsNullOrEmpty(productDto.Barcode) && BarcodeExists(productDto.Barcode, productDto.ProductId))
            {
                return null; // Barcode already exists for another active product
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
                UpdatedDate = DateTime.UtcNow,
                // Preserve ISoftDeletable properties from DTO
                IsDeleted = productDto.IsDeleted,
                DeletedAt = productDto.DeletedAt
            };

            // Move product between collections if deletion status changed
            if (isCurrentlyDeleted && !updatedProduct.IsDeleted)
            {
                // Moving from deleted to active
                _deletedProducts.Remove(productDto.ProductId);
                _products[productDto.ProductId] = updatedProduct;
            }
            else if (!isCurrentlyDeleted && updatedProduct.IsDeleted)
            {
                // Moving from active to deleted
                _products.Remove(productDto.ProductId);
                _deletedProducts[productDto.ProductId] = updatedProduct;
            }
            else
            {
                // Status hasn't changed, update in current collection
                if (isCurrentlyDeleted)
                {
                    _deletedProducts[productDto.ProductId] = updatedProduct;
                }
                else
                {
                    _products[productDto.ProductId] = updatedProduct;
                }
            }

            return updatedProduct.ToDto();
        }

        public bool Delete( int productId )
        {
            // Remove from both collections to handle any case
            bool removedFromActive = _products.Remove(productId);
            bool removedFromDeleted = _deletedProducts.Remove(productId);

            return removedFromActive || removedFromDeleted;
        }

        public bool SoftDelete( int productId )
        {
            if (!_products.TryGetValue(productId, out Product? product))
            {
                return false; // Product not found or already deleted
            }

            // Move product to deleted collection
            Product softDeletedProduct = product with
            {
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _products.Remove(productId);
            _deletedProducts[productId] = softDeletedProduct;
            return true;
        }

        public bool Restore( int productId )
        {
            if (!_deletedProducts.TryGetValue(productId, out Product? product))
            {
                return false; // Product not found or not deleted
            }

            // Check for SKU conflicts before restoring
            if (SKUExists(product.SKU, productId))
            {
                return false; // Cannot restore due to SKU conflict
            }

            // Move product back to active collection
            Product restoredProduct = product with
            {
                IsDeleted = false,
                DeletedAt = null,
                UpdatedDate = DateTime.UtcNow
            };

            _deletedProducts.Remove(productId);
            _products[productId] = restoredProduct;
            return true;
        }

        public List<ProductDto> GetAll(
            int? categoryId = null,
            int? supplierId = null,
            string? search = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 100 )
        {
            IEnumerable<Product> products = includeDeleted
                ? _products.Values.Concat(_deletedProducts.Values)
                : _products.Values.Where(p => !p.IsDeleted);

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
                                              !string.IsNullOrEmpty(p.Barcode) &&
                                              p
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


        public List<ProductDto> GetByCategory( int categoryId, bool includeDeleted = false ) => GetAll(categoryId, includeDeleted: includeDeleted);

        public List<ProductDto> GetBySupplier( int supplierId, bool includeDeleted = false ) => GetAll(supplierId: supplierId, includeDeleted: includeDeleted);

        public ProductDto? GetBySKU( string sku, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            List<Product> matchingProducts = products
                                             .Where(p => p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase))
                                             .ToList();

            if (matchingProducts.Count > 1)
            {
                // This should NEVER happen - log as critical error
                throw new InvalidOperationException(
                    $"Data integrity violation: Multiple products found with SKU '{sku}'. SKUs must be unique.");
            }

            return matchingProducts
                   .FirstOrDefault()
                   ?.ToDto();
        }

        public List<ProductDto> GetByBarcode( string barcode, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            return products
                   .Where(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                   .Select(p => p.ToDto())
                   .ToList();
        }

        public List<ProductDto> GetActiveProducts() => GetAll(includeDeleted: false);

        public List<ProductDto> GetDeletedProducts()
        {
            return _deletedProducts
                   .Values
                   .OrderBy(p => p.Name)
                   .Select(p => p.ToDto())
                   .ToList();
        }

        public bool Exists( int productId, bool includeDeleted = false )
        {
            bool existsInActive = _products.ContainsKey(productId);
            return existsInActive || includeDeleted && _deletedProducts.ContainsKey(productId);
        }

        public bool IsDeleted( int productId ) => _deletedProducts.ContainsKey(productId);

        public bool SKUExists( string sku, int? excludeProductId = null, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            return products.Any(p =>
                                    p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase) &&
                                    p.ProductId != excludeProductId);
        }

        public bool BarcodeExists( string barcode, int? excludeProductId = null, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            return products.Any(p =>
                                    !string.IsNullOrEmpty(p.Barcode) &&
                                    p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase) &&
                                    p.ProductId != excludeProductId);
        }

        public int GetCount( int? categoryId = null, int? supplierId = null, bool includeDeleted = false )
        {
            // Fix: Remove isActive parameter since you removed IsActive property
            IEnumerable<Product> products = includeDeleted
                ? _products.Values.Concat(_deletedProducts.Values)
                : _products.Values;

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

        public int GetDeletedCount() => _deletedProducts.Count;

        public int GetTotalCount() => _products.Count + _deletedProducts.Count;

        // Category-specific operations
        public bool HasProductsInCategory( int categoryId, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            return products.Any(p => p.CategoryId == categoryId);
        }

        public int GetProductCountInCategory( int categoryId, bool includeDeleted = false ) => GetCount(categoryId, includeDeleted: includeDeleted);

        public List<ProductDto> GetActiveProductsInCategory( int categoryId ) => GetAll(categoryId, includeDeleted: false);

        // Supplier-specific operations
        public bool HasProductsFromSupplier( int supplierId, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            return products.Any(p => p.SupplierId == supplierId);
        }

        public int GetProductCountFromSupplier( int supplierId, bool includeDeleted = false ) =>
            GetCount(supplierId: supplierId, includeDeleted: includeDeleted);

        public IEnumerable<Product> GetLowStockProducts( bool includeDeleted = false )
        {
            IEnumerable<Product> products = includeDeleted
                ? _products.Values.Concat(_deletedProducts.Values)
                : _products.Values;

            return products
                   .OrderBy(p => p.Name)
                   .ToList();
        }

        public List<ProductDto> GetLowStockProducts( Dictionary<int, int> currentStockLevels, bool includeDeleted = false )
        {
            IEnumerable<Product> products = _products.Values.AsEnumerable();

            if (includeDeleted)
            {
                products = products.Concat(_deletedProducts.Values);
            }

            return products
                   .Where(p => currentStockLevels.ContainsKey(p.ProductId) && p.IsLowStock(currentStockLevels[p.ProductId]))
                   .Select(p => p.ToDto())
                   .ToList();
        }

        public IEnumerable<Product> SearchProducts( string? searchTerm = null, int? categoryId = null, bool includeDeleted = false )
        {
            // Fix: Remove isActive parameter since you removed IsActive property
            IEnumerable<Product> query = includeDeleted
                ? _products.Values.Concat(_deletedProducts.Values)
                : _products.Values;

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
