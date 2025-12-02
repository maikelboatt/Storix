using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Inventories
{
    /// <summary>
    ///     Service responsible for inventory write operations with transaction coordination.
    /// </summary>
    public class InventoryWriteService(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IInventoryTransactionRepository inventoryTransactionRepository,
        IInventoryValidationService inventoryValidationService,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<InventoryWriteService> logger ):IInventoryWriteService
    {
        #region Stock Adjustments

        public async Task<DatabaseResult> AdjustStockAsync(
            int inventoryId,
            int quantityChange,
            string? notes,
            int userId )
        {
            // Validation
            DatabaseResult validationResult = await inventoryValidationService.ValidateStockAdjustment(inventoryId, quantityChange);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Get inventory to determine product and location
            DatabaseResult<Inventory?> inventoryResult =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => inventoryRepository.GetByIdAsync(inventoryId),
                    $"Retrieving inventory {inventoryId} for adjustment",
                    enableRetry: false
                );

            if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                return DatabaseResult.Failure(
                    $"Inventory with ID {inventoryId} not found.",
                    DatabaseErrorCode.NotFound);

            Inventory inventory = inventoryResult.Value;

            try
            {
                // Adjust stock
                DatabaseResult adjustResult = await inventoryRepository.AdjustStockAsync(inventoryId, quantityChange);
                if (!adjustResult.IsSuccess)
                    return adjustResult;

                // Create transaction record
                TransactionType transactionType = quantityChange > 0
                    ? TransactionType.StockIn
                    : TransactionType.StockOut;
                InventoryTransaction transaction = new(
                    0,
                    inventory.ProductId,
                    inventory.LocationId,
                    transactionType,
                    Math.Abs(quantityChange),
                    null,
                    $"Manual Adjustment",
                    notes,
                    userId,
                    DateTime.UtcNow
                );

                await inventoryTransactionRepository.CreateAsync(transaction);

                logger.LogInformation(
                    "Successfully adjusted stock for inventory {InventoryId} by {Change}",
                    inventoryId,
                    quantityChange);

                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adjusting stock for inventory {InventoryId}", inventoryId);
                return DatabaseResult.Failure(
                    $"Error adjusting stock: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Stock Transfers

        public async Task<DatabaseResult<InventoryMovement>> TransferStockAsync(
            int productId,
            int fromLocationId,
            int toLocationId,
            int quantity,
            string? notes,
            int userId )
        {
            // Validation
            DatabaseResult validationResult = await inventoryValidationService.ValidateStockTransfer(
                productId,
                fromLocationId,
                toLocationId,
                quantity);
            if (!validationResult.IsSuccess)
                return DatabaseResult<InventoryMovement>.Failure(validationResult.ErrorMessage!, validationResult.ErrorCode);

            try
            {
                // Get source inventory
                Inventory? sourceInventory =
                    await inventoryRepository.GetByProductAndLocationAsync(productId, fromLocationId);

                if (sourceInventory == null)
                    return DatabaseResult<InventoryMovement>.Failure(
                        "Source inventory not found.",
                        DatabaseErrorCode.NotFound);

                // Reduce stock at source
                await inventoryRepository.AdjustStockAsync(sourceInventory.InventoryId, -quantity);

                // Get or create destination inventory
                Inventory? destInventory =
                    await inventoryRepository.GetByProductAndLocationAsync(productId, toLocationId);

                if (destInventory == null)
                {
                    // Create new inventory record at destination
                    destInventory = new Inventory(
                        0,
                        productId,
                        toLocationId,
                        quantity,
                        0,
                        DateTime.UtcNow
                    );
                    await inventoryRepository.CreateAsync(destInventory);
                }
                else
                {
                    // Increase stock at destination
                    await inventoryRepository.AdjustStockAsync(destInventory.InventoryId, quantity);
                }

                // Create movement record
                InventoryMovement movement = new(
                    0,
                    productId,
                    fromLocationId,
                    toLocationId,
                    quantity,
                    notes,
                    userId,
                    DateTime.UtcNow
                );

                InventoryMovement createdMovement = await inventoryMovementRepository.CreateAsync(movement);

                // Create transaction records (outbound and inbound)
                InventoryTransaction outboundTransaction = new(
                    0,
                    productId,
                    fromLocationId,
                    TransactionType.Transfer,
                    quantity,
                    null,
                    $"Transfer to Location {toLocationId}",
                    notes,
                    userId,
                    DateTime.UtcNow
                );

                InventoryTransaction inboundTransaction = new(
                    0,
                    productId,
                    toLocationId,
                    TransactionType.Transfer,
                    quantity,
                    null,
                    $"Transfer from Location {fromLocationId}",
                    notes,
                    userId,
                    DateTime.UtcNow
                );

                await inventoryTransactionRepository.CreateAsync(outboundTransaction);
                await inventoryTransactionRepository.CreateAsync(inboundTransaction);

                logger.LogInformation(
                    "Successfully transferred {Quantity} units of product {ProductId} from location {FromLocation} to {ToLocation}",
                    quantity,
                    productId,
                    fromLocationId,
                    toLocationId);

                return DatabaseResult<InventoryMovement>.Success(createdMovement);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error transferring stock for product {ProductId}", productId);
                return DatabaseResult<InventoryMovement>.Failure(
                    $"Error transferring stock: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Stock Reservations

        public async Task<DatabaseResult> ReserveStockAsync( int inventoryId, int quantity )
        {
            // Validation
            DatabaseResult validationResult = await inventoryValidationService.ValidateStockReservation(inventoryId, quantity);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await inventoryRepository.ReserveStockAsync(inventoryId, quantity);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Successfully reserved {Quantity} units for inventory {InventoryId}",
                    quantity,
                    inventoryId);
            }

            return result;
        }

        public async Task<DatabaseResult> ReleaseReservedStockAsync( int inventoryId, int quantity )
        {
            // Validation
            DatabaseResult validationResult = await inventoryValidationService.ValidateReservedStockRelease(inventoryId, quantity);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await inventoryRepository.ReleaseReservedStockAsync(inventoryId, quantity);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Successfully released {Quantity} reserved units for inventory {InventoryId}",
                    quantity,
                    inventoryId);
            }

            return result;
        }

        #endregion

        #region Inventory CRUD

        public async Task<DatabaseResult<Inventory>> CreateInventoryAsync(
            int productId,
            int locationId,
            int initialStock )
        {
            // Check if inventory already exists
            DatabaseResult<bool> existsResult =
                await inventoryValidationService.InventoryExistsForProductAndLocationAsync(productId, locationId);

            if (existsResult is { IsSuccess: true, Value: true })
            {
                logger.LogWarning(
                    "Inventory already exists for product {ProductId} at location {LocationId}",
                    productId,
                    locationId);
                return DatabaseResult<Inventory>.Failure(
                    "Inventory already exists for this product at this location.",
                    DatabaseErrorCode.DuplicateKey);
            }

            Inventory inventory = new(
                0,
                productId,
                locationId,
                initialStock,
                0,
                DateTime.UtcNow
            );

            DatabaseResult<Inventory> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.CreateAsync(inventory),
                "Creating new inventory record"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully created inventory for product {ProductId} at location {LocationId} with stock {Stock}",
                    productId,
                    locationId,
                    initialStock);
                return DatabaseResult<Inventory>.Success(result.Value);
            }

            logger.LogWarning("Failed to create inventory: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<Inventory>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<Inventory>> UpdateInventoryAsync( Inventory inventory )
        {
            DatabaseResult<bool> existsResult = await inventoryValidationService.InventoryExistsAsync(inventory.InventoryId);
            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to update non-existent inventory {InventoryId}", inventory.InventoryId);
                return DatabaseResult<Inventory>.Failure(
                    $"Inventory with ID {inventory.InventoryId} not found.",
                    DatabaseErrorCode.NotFound);
            }

            DatabaseResult<Inventory> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.UpdateAsync(
                    inventory with
                    {
                        LastUpdated = DateTime.UtcNow
                    }),
                "Updating inventory record"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully updated inventory {InventoryId}", inventory.InventoryId);
                return DatabaseResult<Inventory>.Success(result.Value);
            }

            logger.LogWarning("Failed to update inventory {InventoryId}: {ErrorMessage}", inventory.InventoryId, result.ErrorMessage);
            return DatabaseResult<Inventory>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Transaction Creation

        public async Task<DatabaseResult<InventoryTransaction>> CreateTransactionAsync(
            int productId,
            int locationId,
            TransactionType type,
            int quantity,
            decimal? unitCost,
            string? reference,
            string? notes,
            int userId )
        {
            InventoryTransaction transaction = new(
                0,
                productId,
                locationId,
                type,
                quantity,
                unitCost,
                reference,
                notes,
                userId,
                DateTime.UtcNow
            );

            DatabaseResult<InventoryTransaction> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryTransactionRepository.CreateAsync(transaction),
                "Creating inventory transaction"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully created {Type} transaction for product {ProductId} at location {LocationId}",
                    type,
                    productId,
                    locationId);
                return DatabaseResult<InventoryTransaction>.Success(result.Value);
            }

            logger.LogWarning("Failed to create transaction: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<InventoryTransaction>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion
    }
}
