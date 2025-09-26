using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Products;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Service responsible for product validation operations.
    /// </summary>
    public class ProductValidationService(
        IProductRepository productRepository,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<ProductValidationService> logger ):IProductValidationService
    {
        public async Task<DatabaseResult<bool>> ProductExistsAsync( int productId )
        {
            if (productId <= 0)
                return DatabaseResult<bool>.Success(false);

            // Check store first
            ProductDto? productInStore = productStore.GetById(productId);
            if (productInStore != null)
                return DatabaseResult<bool>.Success(true);

            // Check database
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.ExistsAsync(productId),
                $"Checking if product {productId} exists",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> IsSkuAvailableAsync( string sku, int? excludeProductId = null )
        {
            if (string.IsNullOrWhiteSpace(sku))
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.SkuExistsAsync(sku.Trim(), excludeProductId),
                "Checking if SKU exists",
                false
            );

            if (result.IsSuccess)
            {
                // Return true if SKU doesn't exist (available)
                return DatabaseResult<bool>.Success(!result.Value);
            }

            return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForDeletion( int productId )
        {
            // Check existence
            DatabaseResult<bool> existsResult = await ProductExistsAsync(productId);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to delete non-existent product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found.", DatabaseErrorCode.NotFound);
            }

            // Additional business rules for product deletion could go here
            // For example: check if product has pending orders, stock movements, etc.

            return DatabaseResult.Success();
        }
    }
}
