using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Storix.Application.DTO;
using Storix.Application.DTO.Mappers;
using Storix.Application.DTO.Product;
using Storix.Application.Repositories;
using Storix.Domain.Models;

namespace Storix.Application.Services
{
    /// <summary>
    ///     Implementation of product service with business logic and validation.
    /// </summary>
    public class ProductService( IProductRepository productRepository ):IProductService
    {
        #region Read Operations

        /// <summary>
        ///     Gets a product by its ID.
        /// </summary>
        /// <param name="productId" >The product ID.</param>
        /// <returns>The product DTO or null if not found.</returns>
        public async Task<ProductDto?> GetProductByIdAsync( int productId )
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));

            Product? product = await productRepository.GetByIdAsync(productId);
            return product?.ToDto();
        }

        /// <summary>
        ///     Gets a product by its SKU.
        /// </summary>
        /// <param name="sku" >The product SKU.</param>
        /// <returns>The product DTO or null if not found.</returns>
        public async Task<ProductDto?> GetProductBySkuAsync( string sku )
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));

            Product? product = await productRepository.GetBySkuAsync(sku.Trim());
            return product?.ToDto();
        }

        /// <summary>
        ///     Gets all products.
        /// </summary>
        /// <returns>All products as DTOs.</returns>
        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetAllAsync();
            return products.ToDto();
        }

        /// <summary>
        ///     Gets all active products.
        /// </summary>
        /// <returns>All active products as DTOs.</returns>
        public async Task<IEnumerable<ProductDto>> GetAllActiveProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetAllActiveAsync();
            return products.ToDto();
        }

        /// <summary>
        ///     Gets products by category ID.
        /// </summary>
        /// <param name="categoryId" >The category ID.</param>
        /// <returns>Products in the specified category.</returns>
        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync( int categoryId )
        {
            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));

            IEnumerable<Product> products = await productRepository.GetByCategoryAsync(categoryId);
            return products.ToDto();
        }

        /// <summary>
        ///     Gets products by supplier ID.
        /// </summary>
        /// <param name="supplierId" >The supplier ID.</param>
        /// <returns>Products from the specified supplier.</returns>
        public async Task<IEnumerable<ProductDto>> GetProductsBySupplierAsync( int supplierId )
        {
            if (supplierId <= 0)
                throw new ArgumentException("Supplier ID must be greater than zero.", nameof(supplierId));

            IEnumerable<Product> products = await productRepository.GetBySupplierAsync(supplierId);
            return products.ToDto();
        }

        /// <summary>
        ///     Gets products that are below their minimum stock level.
        /// </summary>
        /// <returns>Products with low stock.</returns>
        public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetLowStockProductsAsync();
            return products.ToDto();
        }

        /// <summary>
        ///     Gets products with extended details (joins supplier, category, stock info).
        /// </summary>
        /// <returns>Products with extended details.</returns>
        public async Task<IEnumerable<ProductWithDetailsDto>> GetProductsWithDetailsAsync() => await productRepository.GetProductsWithDetailsAsync();

        /// <summary>
        ///     Searches products by name, SKU, or description.
        /// </summary>
        /// <param name="searchTerm" >The search term.</param>
        /// <returns>Products matching the search criteria.</returns>
        public async Task<IEnumerable<ProductDto>> SearchProductsAsync( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductDto>();

            IEnumerable<Product> products = await productRepository.SearchAsync(searchTerm.Trim());
            return products.ToDto();
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new product with business validation.
        /// </summary>
        /// <param name="createProductDto" >The product creation data.</param>
        /// <returns>The created product DTO.</returns>
        /// <exception cref="ArgumentException" >Thrown when validation fails.</exception>
        /// <exception cref="InvalidOperationException" >Thrown when SKU already exists.</exception>
        public async Task<ProductDto> CreateProductAsync( CreateProductDto createProductDto )
        {
            await ValidateCreateProductAsync(createProductDto);

            Product product = createProductDto.ToDomain();
            Product createdProduct = await productRepository.CreateAsync(product);

            return createdProduct.ToDto();
        }

        /// <summary>
        ///     Updates an existing product with business validation.
        /// </summary>
        /// <param name="updateProductDto" >The product update data.</param>
        /// <returns>The updated product DTO.</returns>
        /// <exception cref="ArgumentException" >Thrown when validation fails.</exception>
        /// <exception cref="InvalidOperationException" >Thrown when product doesn't exist or SKU conflict.</exception>
        public async Task<ProductDto> UpdateProductAsync( UpdateProductDto updateProductDto )
        {
            await ValidateUpdateProductAsync(updateProductDto);

            Product? existingProduct = await productRepository.GetByIdAsync(updateProductDto.ProductId);
            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {updateProductDto.ProductId} not found.");

            // Merge existing data with updates using record 'with' expression
            Product updatedProduct = existingProduct with
            {
                Name = updateProductDto.Name,
                SKU = updateProductDto.SKU,
                Description = updateProductDto.Description,
                Barcode = updateProductDto.Barcode,
                Price = updateProductDto.Price,
                Cost = updateProductDto.Cost,
                MinStockLevel = updateProductDto.MinStockLevel,
                MaxStockLevel = updateProductDto.MaxStockLevel,
                SupplierId = updateProductDto.SupplierId,
                CategoryId = updateProductDto.CategoryId,
                IsActive = updateProductDto.IsActive,
                UpdatedDate = DateTime.UtcNow
            };

            Product result = await productRepository.UpdateAsync(updatedProduct);
            return result.ToDto();
        }

        /// <summary>
        ///     Permanently deletes a product.
        /// </summary>
        /// <param name="productId" >The product ID to delete.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <exception cref="ArgumentException" >Thrown when product ID is invalid.</exception>
        /// <exception cref="InvalidOperationException" >Thrown when product doesn't exist.</exception>
        public async Task<bool> DeleteProductAsync( int productId )
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));

            bool exists = await productRepository.ExistsAsync(productId);
            if (!exists)
                throw new InvalidOperationException($"Product with ID {productId} not found.");

            return await productRepository.DeleteAsync(productId);
        }

        /// <summary>
        ///     Soft deletes a product (sets IsActive = false).
        /// </summary>
        /// <param name="productId" >The product ID to soft delete.</param>
        /// <returns>True if soft deleted successfully.</returns>
        /// <exception cref="ArgumentException" >Thrown when product ID is invalid.</exception>
        /// <exception cref="InvalidOperationException" >Thrown when product doesn't exist.</exception>
        public async Task<bool> SoftDeleteProductAsync( int productId )
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));

            bool exists = await productRepository.ExistsAsync(productId);
            if (!exists)
                throw new InvalidOperationException($"Product with ID {productId} not found.");

            return await productRepository.SoftDeleteAsync(productId);
        }

        #endregion

        #region Validation

        /// <summary>
        ///     Checks if a product exists.
        /// </summary>
        /// <param name="productId" >The product ID to check.</param>
        /// <returns>True if the product exists.</returns>
        public async Task<bool> ProductExistsAsync( int productId )
        {
            if (productId <= 0) return false;
            return await productRepository.ExistsAsync(productId);
        }

        /// <summary>
        ///     Checks if a SKU is available for use.
        /// </summary>
        /// <param name="sku" >The SKU to check.</param>
        /// <param name="excludeProductId" >Optional product ID to exclude from check (for updates).</param>
        /// <returns>True if the SKU is available.</returns>
        public async Task<bool> IsSkuAvailableAsync( string sku, int? excludeProductId = null )
        {
            if (string.IsNullOrWhiteSpace(sku)) return false;

            bool exists = await productRepository.SkuExistsAsync(sku.Trim(), excludeProductId);
            return !exists; // Return true if SKU doesn't exist (available)
        }

        #endregion

        #region Pagination

        /// <summary>
        ///     Gets the total count of products.
        /// </summary>
        /// <returns>The total number of products.</returns>
        public async Task<int> GetTotalProductCountAsync() => await productRepository.GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of active products.
        /// </summary>
        /// <returns>The number of active products.</returns>
        public async Task<int> GetActiveProductCountAsync() => await productRepository.GetActiveCountAsync();

        /// <summary>
        ///     Gets a paged list of products.
        /// </summary>
        /// <param name="pageNumber" >The page number (1-based).</param>
        /// <param name="pageSize" >The number of items per page.</param>
        /// <returns>A paged list of products.</returns>
        public async Task<IEnumerable<ProductDto>> GetProductsPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0)
                throw new ArgumentException("Page number must be greater than zero.", nameof(pageNumber));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));

            IEnumerable<Product> products = await productRepository.GetPagedAsync(pageNumber, pageSize);
            return products.ToDto();
        }

        #endregion

        #region Private Validation Methods

        private async Task ValidateCreateProductAsync( CreateProductDto dto )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            ValidateProductData(dto.Name, dto.SKU, dto.Price, dto.Cost, dto.MinStockLevel, dto.MaxStockLevel);

            // Check SKU uniqueness
            bool skuExists = await productRepository.SkuExistsAsync(dto.SKU.Trim());
            if (skuExists)
                throw new InvalidOperationException($"A product with SKU '{dto.SKU}' already exists.");
        }

        private async Task ValidateUpdateProductAsync( UpdateProductDto dto )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.ProductId <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(dto.ProductId));

            ValidateProductData(dto.Name, dto.SKU, dto.Price, dto.Cost, dto.MinStockLevel, dto.MaxStockLevel);

            // Check SKU uniqueness (excluding current product)
            bool skuExists = await productRepository.SkuExistsAsync(dto.SKU.Trim(), dto.ProductId);
            if (skuExists)
                throw new InvalidOperationException($"Another product with SKU '{dto.SKU}' already exists.");
        }

        private static void ValidateProductData( string name, string sku, decimal price, decimal cost, int minStock, int maxStock )
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("Product SKU cannot be null or empty.");

            if (price < 0)
                throw new ArgumentException("Product price cannot be negative.");

            if (cost < 0)
                throw new ArgumentException("Product cost cannot be negative.");

            if (minStock < 0)
                throw new ArgumentException("Minimum stock level cannot be negative.");

            if (maxStock < 0)
                throw new ArgumentException("Maximum stock level cannot be negative.");

            if (minStock > maxStock)
                throw new ArgumentException("Minimum stock level cannot be greater than maximum stock level.");
        }

        #endregion
    }
}
