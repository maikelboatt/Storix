using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
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
        IInventoryManager inventoryManager,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<ProductReadService> logger ):IProductReadService
    {
        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetLowStockProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                productRepository.GetLowStockProductsAsync,
                "Retrieving low stock products"
            );

            if (result is { IsSuccess: true, Value: not null })
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
                productRepository.GetActiveCountAsync,
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


        public async Task<DatabaseResult<ProductDto>> GetProductById( int productId )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided", productId);
                return DatabaseResult<ProductDto>.Failure("Product ID must be a positive integer", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Product?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetByIdAsync(productId),
                $"Retrieving product {productId}");

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve product {ProductId}: {ErrorMessage}", productId, result.ErrorMessage);
                return DatabaseResult<ProductDto>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Product with ID {ProductId} not found", productId);
                return DatabaseResult<ProductDto>.Failure("Product not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved product with ID {CustomerId}", productId);
            return DatabaseResult<ProductDto>.Success(result.Value.ToDto());
        }


        public async Task<DatabaseResult<ProductDto>> GetProductBySku( string sku )
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                logger.LogWarning("Null or empty sku provided");
                return DatabaseResult<ProductDto>.Failure("Sku cannot be null or empty", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Product?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetBySkuAsync(sku),
                $"Retrieving product {sku}");

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve product {Sku}: {ErrorMessage}", sku, result.ErrorMessage);
                return DatabaseResult<ProductDto>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Product with Sku {Sku} not found", sku);
                return DatabaseResult<ProductDto>.Failure("Product not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved product with Sku {CustomerId}", sku);
            return DatabaseResult<ProductDto>.Success(result.Value.ToDto());
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () =>
                    productRepository.GetAllAsync(true),
                $"Retrieving all products."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();

                logger.LogInformation(
                    "Successfully loaded {ProductCount} products.)",
                    result.Value.Count());

                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<TopProductDto>>> GetTop5BestSellersAsync( int topCounts = 5, int monthsBack = 3 )
        {
            DatabaseResult<IEnumerable<TopProductDto>> result =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => productRepository.GetTopBestSellersAsync(topCounts, monthsBack),
                    "Retrieving best-sellers");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {ProductCount} of the Top 5 best-sellers", result.Value.Count());
                List<TopProductDto> rankedProducts = result
                                                     .Value
                                                     .Select(( p, index ) =>
                                                     {
                                                         p.Rank = index + 1;
                                                         return p;
                                                     })
                                                     .ToList();
                productStore.InitializeTopProducts(rankedProducts);
                return DatabaseResult<IEnumerable<TopProductDto>>.Success(rankedProducts);
            }
            logger.LogError("Failed to retrieve top 5 best-selling products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<TopProductDto>>.Failure(result.ErrorMessage!, result.ErrorCode);

        }

        public async Task<DatabaseResult<IEnumerable<Product>>> GetAllActiveProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetAllAsync(false),
                "Retrieving active products");

            if (result is { IsSuccess: true, Value: not null })
            {
                List<Product> active = result.Value.ToList();
                IEnumerable<ProductDto> productDtos = active.ToDto();

                logger.LogInformation("Successfully retrieved {ActiveProductCount} active products", active.Count);
                productStore.Initialize(active);

                return DatabaseResult<IEnumerable<Product>>.Success(active);
            }

            logger.LogWarning("Failed to retrieve active products: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<Product>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductListDto>>> GetAllActiveProductsForListAsync()
        {
            DatabaseResult<IEnumerable<ProductWithDetailsDto>> result = await GetProductsWithDetailsAsync(false);


            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<ProductListDto> productListDtos = result
                                                              .Value
                                                              .Select(dto => new ProductListDto
                                                              {
                                                                  ProductId = dto.ProductId,
                                                                  Name = dto.Name,
                                                                  SKU = dto.SKU,
                                                                  Barcode = dto.Barcode,
                                                                  Price = dto.Price,
                                                                  Cost = dto.Cost,
                                                                  MinStockLevel = dto.MinStockLevel,
                                                                  MaxStockLevel = dto.MaxStockLevel,
                                                                  CategoryName = dto.CategoryName,
                                                                  SupplierName = dto.SupplierName,
                                                                  CurrentStock = inventoryManager.GetCurrentStockForProduct(dto.ProductId),
                                                                  IsLowStock = dto.TotalStock < dto.MinStockLevel
                                                              });

                IEnumerable<ProductListDto> dtos = productListDtos.ToList();
                productStore.InitializeProductList(dtos.ToList());

                logger.LogInformation(
                    "Successfully mapped {ProductCount} active products to ProductListDto",
                    dtos.Count());

                return DatabaseResult<IEnumerable<ProductListDto>>.Success(dtos);
            }

            logger.LogWarning("Failed to retrieve active products for list: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<ProductListDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<ProductDto>>> GetAllDeletedProductsAsync()
        {
            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                productRepository.GetAllDeletedAsync,
                "Retrieving all deleted products"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {DeletedProductCount} deleted products", result.Value.Count());
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<ProductDto>>.Success(productDtos);
            }

            logger.LogWarning("Failed to retrieve deleted products: {ErrorMessage}", result.ErrorMessage);
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
                $"Retrieving products for category {categoryId}."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductCount} products for category {CategoryId}.",
                    result.Value.Count(),
                    categoryId);
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
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
                $"Retrieving products for supplier {supplierId}."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductCount} products for supplier {SupplierId}.",
                    result.Value.Count(),
                    supplierId);
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
                "Retrieving products with details."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {ProductWithDetailsCount} products with details.",
                    result.Value.Count());
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
                return DatabaseResult<IEnumerable<ProductDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.SearchAsync(searchTerm.Trim()),
                $"Searching products with term '{searchTerm}'."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully found {SearchResultCount} products for search term '{SearchTerm}'.",
                    result.Value.Count(),
                    searchTerm);
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
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
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<ProductDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Product>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting products page {pageNumber} with size {pageSize}."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of products ({ProductCount} items.",
                    pageNumber,
                    result.Value.Count());
                IEnumerable<ProductDto> productDtos = result.Value.ToDto();
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
                productRepository.GetTotalCountAsync,
                $"Getting total product count.",
                false
            );

            if (result.IsSuccess)
            {
                logger.LogInformation("Total product count: {Count}.", result.Value);
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
