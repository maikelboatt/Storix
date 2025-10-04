using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Service responsible for product read operations with ISoftDeletable support.
    /// </summary>
    public class ProductReadService(
        IProductRepository productRepository,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<ProductReadService> logger ):IProductReadService
    {
        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetAllActiveAsync(),
                "Retrieving all active products"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {ActiveProductCount} active products", result.Value.Count());
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve active products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetLowStockProductsAsync(),
                "Retrieving low stock products"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {LowStockProductCount} low stock products", result.Value.Count());
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve low stock products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetActiveProductCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetActiveCountAsync(),
                "Getting active product count",
                false
            );

            if (result.IsSuccess)
            {
                logger.LogDebug("Active product count: {Count}", result.Value);
            }

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }


        public ProductDto? GetProductById( int productId, bool includeDeleted = false )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided", productId);
                return null;
            }

            logger.LogDebug("Retrieving product with ID {ProductId} from store (includeDeleted: {IncludeDeleted})", productId, includeDeleted);
            ProductDto? product = productStore.GetById(productId, includeDeleted);
            return product;
        }

        public ProductDto? GetProductBySku( string sku, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                logger.LogWarning("Invalid SKU provided");
                return null;
            }

            logger.LogDebug("Retrieving product with SKU {SKU} from store (includeDeleted: {IncludeDeleted})", sku, includeDeleted);
            return productStore.GetBySKU(sku.Trim(), includeDeleted);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync( bool includeDeleted = false )
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetAllAsync(includeDeleted),
                $"Retrieving all products (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();

                // Only initialize store with non-deleted products for caching
                if (!includeDeleted)
                {
                    productStore.Initialize(result.Value.ToList());
                }

                logger.LogInformation(
                    "Successfully loaded {ProductCount} products (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    includeDeleted);

                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetAllDeletedAsync(),
                "Retrieving all deleted products"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {DeletedProductCount} deleted products", result.Value.Count());
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve deleted products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId, bool includeDeleted = false )
        {
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided", categoryId);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    "Category ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetByCategoryAsync(categoryId, includeDeleted),
                $"Retrieving products for category {categoryId} (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductCount} products for category {CategoryId} (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    categoryId,
                    includeDeleted);
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to retrieve products for category {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId, bool includeDeleted = false )
        {
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid supplier ID {SupplierId} provided", supplierId);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    "Supplier ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetBySupplierAsync(supplierId, includeDeleted),
                $"Retrieving products for supplier {supplierId} (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductCount} products for supplier {SupplierId} (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    supplierId,
                    includeDeleted);
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to retrieve products for supplier {SupplierId}: {ErrorMessage}",
                supplierId,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync( bool includeDeleted = false )
        {
            DatabaseResult<IEnumerable<ProductWithDetailsDto>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetProductsWithDetailsAsync(includeDeleted),
                $"Retrieving products with details (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductWithDetailsCount} products with details (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    includeDeleted);
                return DatabaseResult<IEnumerable<ProductWithDetailsDto>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve products with details: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductWithDetailsDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogDebug("Empty search term provided, returning empty result");
                return DatabaseResult<IEnumerable<ProductDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.SearchAsync(searchTerm.Trim(), includeDeleted),
                $"Searching products with term '{searchTerm}' (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully found {SearchResultCount} products for search term '{SearchTerm}' (includeDeleted: {IncludeDeleted})",
                    result.Value.Count(),
                    searchTerm,
                    includeDeleted);
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to search products with term '{SearchTerm}': {ErrorMessage}",
                searchTerm,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetPagedAsync(pageNumber, pageSize, includeDeleted),
                $"Getting products page {pageNumber} with size {pageSize} (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of products ({ProductCount} items, includeDeleted: {IncludeDeleted})",
                    pageNumber,
                    result.Value.Count(),
                    includeDeleted);
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to retrieve products page {PageNumber}: {ErrorMessage}",
                pageNumber,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalProductCountAsync( bool includeDeleted = false )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetTotalCountAsync(includeDeleted),
                $"Getting total product count (includeDeleted: {includeDeleted})",
                false
            );

            if (result.IsSuccess)
            {
                logger.LogInformation("Total product count: {Count} (includeDeleted: {IncludeDeleted})", result.Value, includeDeleted);
            }

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetDeletedProductCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                productRepository.GetDeletedCountAsync,
                "Getting deleted product count",
                false
            );

            if (result.IsSuccess)
            {
                logger.LogDebug("Deleted product count: {Count}", result.Value);
            }

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
