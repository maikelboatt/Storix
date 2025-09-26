using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO;
using Storix.Application.DTO.Mappers;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Service responsible for product read operations.
    /// </summary>
    public class ProductReadService(
        IProductRepository productRepository,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<ProductReadService> logger ):IProductReadService
    {
        public ProductDto? GetProductById( int productId )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided", productId);
                return null;
            }

            logger.LogDebug("Retrieving product with ID {ProductId} from store", productId);
            ProductDto? product = productStore.GetById(productId);
            return product;
        }

        public ProductDto? GetProductBySku( string sku )
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                logger.LogWarning("Invalid SKU provided");
                return null;
            }

            logger.LogDebug("Retrieving product with SKU {SKU} from store", sku);
            List<ProductDto>? product = productStore.GetBySKU(sku.Trim());
            return product.FirstOrDefault();
            // TODO: Clarify why SKU is not unique in the store
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetAllAsync(),
                "Retrieving all products"
            );

            if (result.IsSuccess && result.Value != null)
            {
                IEnumerable<ProductDto> productDtos = ProductDtoMapper.ToDto(result.Value);

                productStore.Initialize(result.Value.ToList());
                logger.LogInformation("Successfully loaded {ProductCount} products", result.Value.Count());

                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllActiveProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetAllActiveAsync(),
                "Retrieving all active products"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {ActiveProductCount} active products", result.Value.Count());
                IEnumerable<ProductDto> productDtos = DtoMapper.ToDto(result.Value);
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve active products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync( int categoryId )
        {
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided", categoryId);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    "Category ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetByCategoryAsync(categoryId),
                $"Retrieving products for category {categoryId}"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductCount} products for category {CategoryId}",
                    result.Value.Count(),
                    categoryId);
                IEnumerable<ProductDto> productDtos = DtoMapper.ToDto(result.Value);
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to retrieve products for category {CategoryId}: {ErrorMessage}",
                categoryId,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsBySupplierAsync( int supplierId )
        {
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid supplier ID {SupplierId} provided", supplierId);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(
                    "Supplier ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetBySupplierAsync(supplierId),
                $"Retrieving products for supplier {supplierId}"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductCount} products for supplier {SupplierId}",
                    result.Value.Count(),
                    supplierId);
                IEnumerable<ProductDto> productDtos = DtoMapper.ToDto(result.Value);
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to retrieve products for supplier {SupplierId}: {ErrorMessage}",
                supplierId,
                result.ErrorMessage);
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
                IEnumerable<ProductDto> productDtos = DtoMapper.ToDto(result.Value);
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve low stock products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductWithDetailsDto>>> GetProductsWithDetailsAsync()
        {
            DatabaseResult<IEnumerable<ProductWithDetailsDto>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetProductsWithDetailsAsync(),
                "Retrieving products with details"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {ProductWithDetailsCount} products with details", result.Value.Count());
                return DatabaseResult<IEnumerable<ProductWithDetailsDto>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve products with details: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductWithDetailsDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> SearchProductsAsync( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogDebug("Empty search term provided, returning empty result");
                return DatabaseResult<IEnumerable<ProductDto>>.Success(Enumerable.Empty<ProductDto>());
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.SearchAsync(searchTerm.Trim()),
                $"Searching products with term '{searchTerm}'"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully found {SearchResultCount} products for search term '{SearchTerm}'",
                    result.Value.Count(),
                    searchTerm);
                IEnumerable<ProductDto> productDtos = DtoMapper.ToDto(result.Value);
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to search products with term '{SearchTerm}': {ErrorMessage}",
                searchTerm,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetProductsPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0 ? "Page number must be positive" : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting products page {pageNumber} with size {pageSize}"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of products ({ProductCount} items)",
                    pageNumber,
                    result.Value.Count());
                IEnumerable<ProductDto> productDtos = DtoMapper.ToDto(result.Value);
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning(
                "Failed to retrieve products page {PageNumber}: {ErrorMessage}",
                pageNumber,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalProductCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetTotalCountAsync(),
                "Getting total product count",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetActiveProductCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetActiveCountAsync(),
                "Getting active product count",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
