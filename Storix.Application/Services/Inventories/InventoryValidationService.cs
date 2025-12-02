using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Inventories.Interfaces;

namespace Storix.Application.Services.Inventories
{
    /// <summary>
    ///     Service responsible for inventory validation operations.
    /// </summary>
    public class InventoryValidationService(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IInventoryTransactionRepository inventoryTransactionRepository,
        IProductRepository productRepository,
        ILocationRepository locationRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<InventoryValidationService> logger ):IInventoryValidationService
    {
        public async Task<DatabaseResult<bool>> InventoryExistsAsync( int inventoryId )
        {
            if (inventoryId <= 0)
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.ExistsAsync(inventoryId),
                $"Checking if inventory {inventoryId} exists in database",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogDebug("Inventory {InventoryId} exists: {Exists}", inventoryId, result.Value);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> InventoryExistsForProductAndLocationAsync( int productId, int locationId )
        {
            if (productId <= 0 || locationId <= 0)
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.ExistsByProductAndLocationAsync(productId, locationId),
                $"Checking if inventory exists for product {productId} at location {locationId}",
                enableRetry: false
            );

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateStockAdjustment( int inventoryId, int quantityChange )
        {
            // Check inventory exists
            DatabaseResult<bool> existsResult = await InventoryExistsAsync(inventoryId);
            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to adjust non-existent inventory {InventoryId}", inventoryId);
                return DatabaseResult.Failure(
                    $"Inventory with ID {inventoryId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // If reducing stock, check if there's enough available
            if (quantityChange < 0)
            {
                DatabaseResult<Domain.Models.Inventory?> inventoryResult =
                    await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                        () => inventoryRepository.GetByIdAsync(inventoryId),
                        $"Retrieving inventory {inventoryId} for validation",
                        enableRetry: false
                    );

                if (inventoryResult is { IsSuccess: true, Value: not null })
                {
                    int availableStock = inventoryResult.Value.AvailableStock;
                    int requiredReduction = Math.Abs(quantityChange);

                    if (availableStock < requiredReduction)
                    {
                        logger.LogWarning(
                            "Insufficient stock for inventory {InventoryId}: Available={Available}, Required={Required}",
                            inventoryId,
                            availableStock,
                            requiredReduction);
                        return DatabaseResult.Failure(
                            $"Insufficient available stock. Available: {availableStock}, Required: {requiredReduction}",
                            DatabaseErrorCode.ConstraintViolation);
                    }
                }
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateStockTransfer( int productId,
            int fromLocationId,
            int toLocationId,
            int quantity )
        {
            if (quantity <= 0)
            {
                logger.LogWarning("Invalid transfer quantity: {Quantity}", quantity);
                return DatabaseResult.Failure("Transfer quantity must be positive.", DatabaseErrorCode.InvalidInput);
            }

            if (fromLocationId == toLocationId)
            {
                logger.LogWarning("Attempted to transfer stock to same location: {LocationId}", fromLocationId);
                return DatabaseResult.Failure(
                    "Cannot transfer stock to the same location.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Validate product exists
            DatabaseResult<bool> productExistsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.ExistsAsync(productId, false),
                $"Checking if product {productId} exists",
                enableRetry: false
            );

            if (!productExistsResult.IsSuccess || !productExistsResult.Value)
            {
                logger.LogWarning("Attempted to transfer non-existent product {ProductId}", productId);
                return DatabaseResult.Failure(
                    $"Product with ID {productId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // Validate locations exist
            DatabaseResult<bool> fromLocationExistsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.ExistsAsync(fromLocationId, false),
                $"Checking if from location {fromLocationId} exists",
                enableRetry: false
            );

            if (!fromLocationExistsResult.IsSuccess || !fromLocationExistsResult.Value)
            {
                logger.LogWarning("Attempted to transfer from non-existent location {LocationId}", fromLocationId);
                return DatabaseResult.Failure(
                    $"Source location with ID {fromLocationId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> toLocationExistsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.ExistsAsync(toLocationId, false),
                $"Checking if to location {toLocationId} exists",
                enableRetry: false
            );

            if (!toLocationExistsResult.IsSuccess || !toLocationExistsResult.Value)
            {
                logger.LogWarning("Attempted to transfer to non-existent location {LocationId}", toLocationId);
                return DatabaseResult.Failure(
                    $"Destination location with ID {toLocationId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            // Check if source has enough stock
            DatabaseResult<Domain.Models.Inventory?> sourceInventoryResult =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => inventoryRepository.GetByProductAndLocationAsync(productId, fromLocationId),
                    $"Retrieving source inventory for product {productId} at location {fromLocationId}",
                    enableRetry: false
                );

            if (sourceInventoryResult is { IsSuccess: true, Value: not null })
            {
                if (sourceInventoryResult.Value.AvailableStock < quantity)
                {
                    logger.LogWarning(
                        "Insufficient stock for transfer: Product {ProductId}, Location {LocationId}, Available={Available}, Required={Required}",
                        productId,
                        fromLocationId,
                        sourceInventoryResult.Value.AvailableStock,
                        quantity);
                    return DatabaseResult.Failure(
                        $"Insufficient available stock at source location. Available: {sourceInventoryResult.Value.AvailableStock}, Required: {quantity}",
                        DatabaseErrorCode.ConstraintViolation);
                }
            }
            else
            {
                logger.LogWarning(
                    "No inventory found for product {ProductId} at location {LocationId}",
                    productId,
                    fromLocationId);
                return DatabaseResult.Failure(
                    "No inventory found at source location.",
                    DatabaseErrorCode.NotFound);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateStockReservation( int inventoryId, int quantity )
        {
            if (quantity <= 0)
            {
                logger.LogWarning("Invalid reservation quantity: {Quantity}", quantity);
                return DatabaseResult.Failure("Reservation quantity must be positive.", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Domain.Models.Inventory?> inventoryResult =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => inventoryRepository.GetByIdAsync(inventoryId),
                    $"Retrieving inventory {inventoryId} for reservation validation",
                    enableRetry: false
                );

            if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
            {
                logger.LogWarning("Attempted to reserve stock for non-existent inventory {InventoryId}", inventoryId);
                return DatabaseResult.Failure(
                    $"Inventory with ID {inventoryId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            if (inventoryResult.Value.AvailableStock < quantity)
            {
                logger.LogWarning(
                    "Insufficient available stock for reservation: Inventory {InventoryId}, Available={Available}, Required={Required}",
                    inventoryId,
                    inventoryResult.Value.AvailableStock,
                    quantity);
                return DatabaseResult.Failure(
                    $"Insufficient available stock. Available: {inventoryResult.Value.AvailableStock}, Required: {quantity}",
                    DatabaseErrorCode.ConstraintViolation);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateReservedStockRelease( int inventoryId, int quantity )
        {
            if (quantity <= 0)
            {
                logger.LogWarning("Invalid release quantity: {Quantity}", quantity);
                return DatabaseResult.Failure("Release quantity must be positive.", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Domain.Models.Inventory?> inventoryResult =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => inventoryRepository.GetByIdAsync(inventoryId),
                    $"Retrieving inventory {inventoryId} for release validation",
                    enableRetry: false
                );

            if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
            {
                logger.LogWarning("Attempted to release stock for non-existent inventory {InventoryId}", inventoryId);
                return DatabaseResult.Failure(
                    $"Inventory with ID {inventoryId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            if (inventoryResult.Value.ReservedStock < quantity)
            {
                logger.LogWarning(
                    "Insufficient reserved stock for release: Inventory {InventoryId}, Reserved={Reserved}, Required={Required}",
                    inventoryId,
                    inventoryResult.Value.ReservedStock,
                    quantity);
                return DatabaseResult.Failure(
                    $"Insufficient reserved stock. Reserved: {inventoryResult.Value.ReservedStock}, Required: {quantity}",
                    DatabaseErrorCode.ConstraintViolation);
            }

            return DatabaseResult.Success();
        }
    }
}
