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
using Storix.Application.Stores.Products;
using Storix.Domain.Models;

namespace Storix.Application.Services.Products
{
    /// <summary>
    ///     Service responsible for product write operations with ISoftDeletable support.
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
            return await PerformCreate(createProductDto);
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

        public async Task<DatabaseResult> SoftDeleteProductAsync( int productId )
        {
            // Input validation
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided for soft deletion", productId);
                return DatabaseResult.Failure("Product ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult businessValidation = await ValidateSoftDeleteBusiness(productId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform soft deletion
            return await PerformSoftDelete(productId);
        }

        public async Task<DatabaseResult> RestoreProductAsync( int productId )
        {
            // Input validation
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided for restoration", productId);
                return DatabaseResult.Failure("Product ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult businessValidation = await ValidateRestoreBusiness(productId);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform restoration
            return await PerformRestore(productId);
        }

        public async Task<DatabaseResult> HardDeleteProductAsync( int productId )
        {
            // Input validation
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided for hard deletion", productId);
                return DatabaseResult.Failure("Product ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation - check if product exists (including deleted ones)
            DatabaseResult validationResult = await productValidationService.ValidateForHardDeletion(productId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform hard deletion
            return await PerformHardDelete(productId);
        }


        #region Helper Methods

        private async Task<DatabaseResult<ProductDto>> PerformCreate( CreateProductDto createProductDto )
        {
            Product product = createProductDto.ToDomain();

            // Ensure new products are not marked as deleted
            product = product with
            {
                IsDeleted = false,
                DeletedAt = null,
                CreatedDate = DateTime.UtcNow
            };

            DatabaseResult<Product> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.CreateAsync(product),
                "Creating new product"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                ProductDto productDto = result.Value.ToDto();
                int productId = result.Value.ProductId;

                productStore.Create(productId, productDto);
                logger.LogInformation(
                    "Successfully created product with ID {ProductId} and SKU '{SKU}'",
                    result.Value.ProductId,
                    result.Value.SKU);

                return DatabaseResult<ProductDto>.Success(productDto);
            }

            logger.LogWarning("Failed to create product: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<ProductDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<ProductDto>> PerformUpdate( UpdateProductDto updateProductDto )
        {
            // Get existing product (including soft-deleted ones for update operations)
            DatabaseResult<Product?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetByIdAsync(updateProductDto.ProductId, true),
                $"Retrieving product {updateProductDto.ProductId} for update",
                false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return DatabaseResult<ProductDto>.Failure(
                    getResult.ErrorMessage ?? "Product not found",
                    getResult.ErrorCode);
            }

            // Update product while preserving ISoftDeletable properties
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
                UpdatedDate = DateTime.UtcNow
                // ISoftDeletable properties are preserved from existing product
            };

            DatabaseResult<Product> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.UpdateAsync(updatedProduct),
                "Updating product"
            );

            if (updateResult.IsSuccess && updateResult.Value != null)
            {
                ProductDto productDto = updateResult.Value.ToDto();

                productStore.Update(productDto);
                logger.LogInformation("Successfully updated product with ID {ProductId}", updateProductDto.ProductId);

                return DatabaseResult<ProductDto>.Success(productDto);
            }

            logger.LogWarning(
                "Failed to update product with ID {ProductId}: {ErrorMessage}",
                updateProductDto.ProductId,
                updateResult.ErrorMessage);
            return DatabaseResult<ProductDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformSoftDelete( int productId )
        {
            DatabaseResult result = await productRepository.SoftDeleteAsync(productId);

            if (result.IsSuccess)
            {
                // Remove from store cache since it's now soft deleted
                productStore.SoftDelete(productId);
                logger.LogInformation("Successfully soft deleted product with ID {ProductId}", productId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to soft delete product with ID {ProductId}: {ErrorMessage}",
                productId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to soft delete product", result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int productId )
        {
            DatabaseResult result = await productRepository.RestoreAsync(productId);

            if (result.IsSuccess)
            {
                // Get the restored product and add it back to the store cache
                DatabaseResult<Product?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => productRepository.GetByIdAsync(productId),
                    $"Retrieving restored product {productId}",
                    false
                );

                if (getResult.IsSuccess && getResult.Value != null)
                {
                    ProductDto productDto = getResult.Value.ToDto();
                    productStore.Update(productDto);
                }

                logger.LogInformation("Successfully restored product with ID {ProductId}", productId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to restore product with ID {ProductId}: {ErrorMessage}",
                productId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to restore product", result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int productId )
        {
            DatabaseResult result = await productRepository.HardDeleteAsync(productId);

            if (result.IsSuccess)
            {
                productStore.Delete(productId);
                logger.LogWarning("Successfully hard deleted product with ID {ProductId} - THIS IS PERMANENT", productId);
                return DatabaseResult.Success();
            }

            logger.LogWarning(
                "Failed to hard delete product with ID {ProductId}: {ErrorMessage}",
                productId,
                result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage ?? "Failed to hard delete product", result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<ProductDto> ValidateCreateInput( CreateProductDto? createProductDto )
        {
            if (createProductDto == null)
            {
                logger.LogWarning("Null CreateProductDto provided");
                return DatabaseResult<ProductDto>.Failure("Product data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidator.Validate(createProductDto);

            if (validationResult.IsValid) return DatabaseResult<ProductDto>.Success(null!);
            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Product creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<ProductDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<ProductDto>> ValidateCreateBusiness( CreateProductDto createProductDto )
        {
            // Check SKU availability (excluding soft-deleted products by default)
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

        private DatabaseResult<ProductDto> ValidateUpdateInput( UpdateProductDto? updateProductDto )
        {
            if (updateProductDto == null)
            {
                logger.LogWarning("Null UpdateProductDto provided");
                return DatabaseResult<ProductDto>.Failure("Product data cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = updateValidator.Validate(updateProductDto);

            if (validationResult.IsValid) return DatabaseResult<ProductDto>.Success(null!);
            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "Product update validation failed for ID {ProductId}: {ValidationErrors}",
                updateProductDto.ProductId,
                errors);
            return DatabaseResult<ProductDto>.Failure($"Validation failed: {errors}", DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<ProductDto>> ValidateUpdateBusiness( UpdateProductDto updateProductDto )
        {
            // Check existence (including soft-deleted products for updates)
            DatabaseResult<bool> existsResult = await productValidationService.ProductExistsAsync(updateProductDto.ProductId, true);
            if (!existsResult.IsSuccess)
                return DatabaseResult<ProductDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to update non-existent product with ID {ProductId}", updateProductDto.ProductId);
                return DatabaseResult<ProductDto>.Failure(
                    $"Product with ID {updateProductDto.ProductId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // Check SKU availability (excluding soft-deleted products)
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

        private async Task<DatabaseResult> ValidateSoftDeleteBusiness( int productId )
        {
            // Check existence (only active products can be soft deleted)
            DatabaseResult<bool> existsResult = await productValidationService.ProductExistsAsync(productId);
            if (!existsResult.IsSuccess)
                return DatabaseResult.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to soft delete non-existent or already deleted product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found or already deleted.", DatabaseErrorCode.NotFound);
            }

            // Additional business validations can be added here
            DatabaseResult validationResult = await productValidationService.ValidateForDeletion(productId);
            if (!validationResult.IsSuccess)
                return validationResult;

            return DatabaseResult.Success();
        }

        private async Task<DatabaseResult> ValidateRestoreBusiness( int productId )
        {
            // Check if product exists and is soft deleted
            DatabaseResult<Product?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.GetByIdAsync(productId, true),
                $"Retrieving product {productId} for restore validation",
                false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning("Attempted to restore non-existent product with ID {ProductId}", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found.", DatabaseErrorCode.NotFound);
            }

            if (!getResult.Value.IsDeleted)
            {
                logger.LogWarning("Attempted to restore product with ID {ProductId} that is not deleted", productId);
                return DatabaseResult.Failure($"Product with ID {productId} is not deleted and cannot be restored.", DatabaseErrorCode.InvalidInput);
            }

            // Check SKU conflicts before restoration
            DatabaseResult<bool> skuAvailableResult = await productValidationService.IsSkuAvailableAsync(getResult.Value.SKU, productId);
            if (!skuAvailableResult.IsSuccess)
                return DatabaseResult.Failure(skuAvailableResult.ErrorMessage!, skuAvailableResult.ErrorCode);

            if (!skuAvailableResult.Value)
            {
                logger.LogWarning(
                    "Cannot restore product {ProductId} due to SKU conflict: {SKU}",
                    productId,
                    getResult.Value.SKU);
                return DatabaseResult.Failure(
                    $"Cannot restore product: Another active product with SKU '{getResult.Value.SKU}' already exists.",
                    DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        #endregion
    }
}
