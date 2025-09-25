using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Products.Interfaces;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Service responsible for product write operations.
    /// </summary>
    public class ProductWriteService(
        IProductRepository productRepository,
        IProductStore productStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        IProductValidationService productValidationService,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator,
        ILogger<ProductWriteService> logger ):IProductWriteService
    {
        public async Task<DatabaseResult<ProductDto>> CreateProductAsync( CreateProductDto createProductDto )
        {
            // Input validation
            DatabaseResult<ProductDto> inputValidation = ValidateCreateInput(createProductDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<ProductDto> businessValidation = await ValidateCreateBusiness(createProductDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create product
            Product product = createProductDto.ToDomain();
            DatabaseResult<Product> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.CreateAsync(product),
                "Creating new product"
            );

            if (result.IsSuccess && result.Value != null)
            {
                productStore.AddProduct(result.Value);
                logger.LogInformation(
                    "Successfully created product with ID {ProductId} and SKU '{SKU}'",
                    result.Value.ProductId,
                    result.Value.SKU);

                ProductDto productDto = result.Value.ToDto();
                return DatabaseResult<ProductDto>.Success(productDto);
            }

            logger.LogWarning("Failed to create product: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<ProductDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<ProductDto>> UpdateProductAsync( UpdateProductDto updateProductDto )
        {
            // Input validation
            DatabaseResult<ProductDto> inputValidation = ValidateUpdateInput(updateProductDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<ProductDto> businessValidation = await ValidateUpdateBusiness(updateProductDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform update
            return await PerformUpdate(updateProductDto);
        }

        public async Task<DatabaseResult> DeleteProductAsync( int productId )
        {
            // Input validation
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided for deletion", productId);
                return DatabaseResult.Failure("Product ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await productValidationService.ValidateForDeletion(productId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform deletion
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.DeleteAsync(productId),
                "Deleting product",
                enableRetry: false
            );

            if (result.IsSuccess && result.Value)
            {
                productStore.DeleteProduct(productId);
                logger.LogInformation("Successfully deleted product with ID {ProductId}", productId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to delete product with ID {ProductId}: {ErrorMessage}",
                productId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to delete product", result.ErrorCode);
        }

        public async Task<DatabaseResult> SoftDeleteProductAsync( int productId )
        {
            // Input validation
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided for soft deletion", productId);
                return DatabaseResult.Failure("Product ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult<bool> existsResult = await productValidationService.ProductExistsAsync(productId);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to soft delete non-existent product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found.", DatabaseErrorCode.NotFound);
            }

            // Perform soft deletion
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.SoftDeleteAsync(productId),
                "Soft deleting product",
                enableRetry: false
            );

            if (result.IsSuccess && result.Value)
            {
                // Update the product in store to reflect soft deletion
                var product = productStore.GetProductById(productId);
                if (product != null)
                {
                    var updatedProduct = product with { IsActive = false };
                    productStore.UpdateProduct(updatedProduct);
                }

                logger.LogInformation("Successfully soft deleted product with ID {ProductId}", productId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to soft delete product with ID {ProductId}: {ErrorMessage}",
                productId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to soft delete product", result.ErrorCode);
        }

        // Private helper methods
        private DatabaseResult<ProductDto> ValidateCreateInput( CreateProductDto createProductDto )
        {
            if (createProductDto == null)
            {
                logger.LogWarning("Null CreateProductDto provided");
                return DatabaseResult<ProductDto>.Failure("Product data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidator.Validate(createProductDto);
            if (!validationResult.IsValid)
            {
                string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                logger.LogWarning("Product creation validation failed: {ValidationErrors}", errors);
                return DatabaseResult<ProductDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);
            }

            return DatabaseResult<ProductDto>.Success(null!);
        }

        private async Task<DatabaseResult<ProductDto>> ValidateCreateBusiness( CreateProductDto createProductDto )
        {
            DatabaseResult<bool> skuAvailableResult = await productValidationService.IsSkuAvailableAsync(createProductDto.SKU);
            if (!skuAvailableResult.IsSuccess)
                return DatabaseResult<ProductDto>.Failure(skuAvailableResult.ErrorMessage!, skuAvailableResult.ErrorCode);

            if (!skuAvailableResult.Value)
            {
                logger.LogWarning("Attempted to create product with duplicate SKU: {SKU}", createProductDto.SKU);
                return DatabaseResult<ProductDto>.Failure(
                    $"A product with SKU '{createProductDto.SKU}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<ProductDto>.Success(null!);
        }

        private DatabaseResult<ProductDto> ValidateUpdateInput( UpdateProductDto updateProductDto )
        {
            if (updateProductDto == null)
            {
                logger.LogWarning("Null UpdateProductDto provided");
                return DatabaseResult<ProductDto>.Failure("Product data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = updateValidator.Validate(updateProductDto);
            if (!validationResult.IsValid)
            {
                string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                logger.LogWarning(
                    "Product update validation failed for ID {ProductId}: {ValidationErrors}",
                    updateProductDto.ProductId,
                    errors);
                return DatabaseResult<ProductDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);
            }

            return DatabaseResult<ProductDto>.Success(null!);
        }

        private async Task<DatabaseResult<ProductDto>> ValidateUpdateBusiness( UpdateProductDto updateProductDto )
        {
            // Check existence
            DatabaseResult<bool> existsResult = await productValidationService.ProductExistsAsync(updateProductDto.ProductId);
            if (!existsResult.IsSuccess)
                return DatabaseResult<ProductDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to update non-existent product with ID {ProductId}", updateProductDto.ProductId);
                return DatabaseResult<ProductDto>.Failure(
                    $"Product with ID {updateProductDto.ProductId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // Check SKU availability
            DatabaseResult<bool> skuAvailableResult = await productValidationService.IsSkuAvailableAsync(
                updateProductDto.SKU,
                updateProductDto.ProductId);
            if (!skuAvailableResult.IsSuccess)
                return DatabaseResult<ProductDto>.Failure(skuAvailableResult.ErrorMessage!, skuAvailableResult.ErrorCode);

            if (!skuAvailableResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update product {ProductId} with duplicate SKU: {SKU}",
                    updateProductDto.ProductId,
                    updateProductDto.SKU);
                return DatabaseResult<ProductDto>.Failure(
                    $"Another product with SKU '{updateProductDto.SKU}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult<ProductDto>.Success(null!);
        }

        private async Task<DatabaseResult<ProductDto>> PerformUpdate( UpdateProductDto updateProductDto )
        {
            // Get existing product
            DatabaseResult<Product?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetByIdAsync(updateProductDto.ProductId),
                $"Retrieving product {updateProductDto.ProductId} for update",
                false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return DatabaseResult<ProductDto>.Failure(
                    getResult.ErrorMessage ?? "Product not found",
                    getResult.ErrorCode);
            }

            // Update product
            Product updatedProduct = getResult.Value with
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

            DatabaseResult<Product> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.UpdateAsync(updatedProduct),
                "Updating product"
            );

            if (updateResult.IsSuccess && updateResult.Value != null)
            {
                productStore.UpdateProduct(updateResult.Value);
                logger.LogInformation("Successfully updated product with ID {ProductId}", updateProductDto.ProductId);

                ProductDto productDto = updateResult.Value.ToDto();
                return DatabaseResult<ProductDto>.Success(productDto);
            }

            logger.LogWarning(
                "Failed to update product with ID {ProductId}: {ErrorMessage}",
                updateProductDto.ProductId,
                updateResult.ErrorMessage);
            return DatabaseResult<ProductDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }
    }
}
