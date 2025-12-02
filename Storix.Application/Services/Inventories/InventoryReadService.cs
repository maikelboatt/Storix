using System;
using System.Collections.Generic;
using System.Linq;
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
    ///     Service responsible for inventory read operations across all inventory entities.
    /// </summary>
    public class InventoryReadService(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IInventoryTransactionRepository inventoryTransactionRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<InventoryReadService> logger ):IInventoryReadService
    {
        #region Inventory Read Operations

        public async Task<DatabaseResult<Inventory?>> GetInventoryByIdAsync( int inventoryId )
        {
            if (inventoryId <= 0)
            {
                logger.LogWarning("Invalid inventory ID {InventoryId} provided", inventoryId);
                return DatabaseResult<Inventory?>.Failure(
                    "Inventory ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Inventory?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.GetByIdAsync(inventoryId),
                $"Retrieving inventory {inventoryId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved inventory {InventoryId}", inventoryId);
                return DatabaseResult<Inventory?>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve inventory {InventoryId}: {ErrorMessage}", inventoryId, result.ErrorMessage);
            return DatabaseResult<Inventory?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<Inventory?>> GetInventoryByProductAndLocationAsync( int productId, int locationId )
        {
            if (productId <= 0 || locationId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} or location ID {LocationId}", productId, locationId);
                return DatabaseResult<Inventory?>.Failure(
                    "Product ID and Location ID must be positive integers.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Inventory?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.GetByProductAndLocationAsync(productId, locationId),
                $"Retrieving inventory for product {productId} at location {locationId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved inventory for product {ProductId} at location {LocationId}",
                    productId,
                    locationId);
                return DatabaseResult<Inventory?>.Success(result.Value);
            }

            logger.LogWarning(
                "No inventory found for product {ProductId} at location {LocationId}",
                productId,
                locationId);
            return DatabaseResult<Inventory?>.Failure(
                result.ErrorMessage ?? "Inventory not found",
                result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByProductIdAsync( int productId )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided", productId);
                return DatabaseResult<IEnumerable<Inventory>>.Failure(
                    "Product ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Inventory>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.GetByProductIdAsync(productId),
                $"Retrieving inventory for product {productId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {Count} inventory records for product {ProductId}",
                    result.Value.Count(),
                    productId);
                return DatabaseResult<IEnumerable<Inventory>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve inventory for product {ProductId}: {ErrorMessage}", productId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<Inventory>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByLocationIdAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<Inventory>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Inventory>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.GetByLocationIdAsync(locationId),
                $"Retrieving inventory at location {locationId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {Count} inventory records at location {LocationId}",
                    result.Value.Count(),
                    locationId);
                return DatabaseResult<IEnumerable<Inventory>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve inventory at location {LocationId}: {ErrorMessage}", locationId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<Inventory>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetAllInventoryAsync()
        {
            DatabaseResult<IEnumerable<Inventory>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                inventoryRepository.GetAllAsync,
                "Retrieving all inventory records"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} inventory records", result.Value.Count());
                return DatabaseResult<IEnumerable<Inventory>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve all inventory: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<Inventory>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetLowStockItemsAsync( int threshold = 10 )
        {
            DatabaseResult<IEnumerable<Inventory>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryRepository.GetLowStockItemsAsync(threshold),
                $"Retrieving low stock items (threshold: {threshold})"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} low stock items", result.Value.Count());
                return DatabaseResult<IEnumerable<Inventory>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve low stock items: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<Inventory>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetOutOfStockItemsAsync()
        {
            DatabaseResult<IEnumerable<Inventory>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                inventoryRepository.GetOutOfStockItemsAsync,
                "Retrieving out of stock items"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} out of stock items", result.Value.Count());
                return DatabaseResult<IEnumerable<Inventory>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve out of stock items: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<Inventory>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalInventoryCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                inventoryRepository.GetTotalCountAsync,
                "Getting total inventory count",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total inventory count: {Count}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Movement Read Operations

        public async Task<DatabaseResult<InventoryMovement?>> GetMovementByIdAsync( int movementId )
        {
            if (movementId <= 0)
            {
                logger.LogWarning("Invalid movement ID {MovementId} provided", movementId);
                return DatabaseResult<InventoryMovement?>.Failure(
                    "Movement ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<InventoryMovement?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryMovementRepository.GetByIdAsync(movementId),
                $"Retrieving movement {movementId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved movement {MovementId}", movementId);
                return DatabaseResult<InventoryMovement?>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve movement {MovementId}: {ErrorMessage}", movementId, result.ErrorMessage);
            return DatabaseResult<InventoryMovement?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByProductIdAsync( int productId )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided", productId);
                return DatabaseResult<IEnumerable<InventoryMovement>>.Failure(
                    "Product ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<InventoryMovement>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryMovementRepository.GetByProductIdAsync(productId),
                $"Retrieving movements for product {productId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {Count} movements for product {ProductId}",
                    result.Value.Count(),
                    productId);
                return DatabaseResult<IEnumerable<InventoryMovement>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve movements for product {ProductId}: {ErrorMessage}", productId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryMovement>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByLocationIdAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<InventoryMovement>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<InventoryMovement>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryMovementRepository.GetByLocationIdAsync(locationId),
                $"Retrieving movements for location {locationId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {Count} movements for location {LocationId}",
                    result.Value.Count(),
                    locationId);
                return DatabaseResult<IEnumerable<InventoryMovement>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve movements for location {LocationId}: {ErrorMessage}", locationId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryMovement>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            DatabaseResult<IEnumerable<InventoryMovement>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryMovementRepository.GetByDateRangeAsync(startDate, endDate),
                $"Retrieving movements between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} movements in date range", result.Value.Count());
                return DatabaseResult<IEnumerable<InventoryMovement>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve movements in date range: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryMovement>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Transaction Read Operations

        public async Task<DatabaseResult<InventoryTransaction?>> GetTransactionByIdAsync( int transactionId )
        {
            if (transactionId <= 0)
            {
                logger.LogWarning("Invalid transaction ID {TransactionId} provided", transactionId);
                return DatabaseResult<InventoryTransaction?>.Failure(
                    "Transaction ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<InventoryTransaction?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryTransactionRepository.GetByIdAsync(transactionId),
                $"Retrieving transaction {transactionId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved transaction {TransactionId}", transactionId);
                return DatabaseResult<InventoryTransaction?>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve transaction {TransactionId}: {ErrorMessage}", transactionId, result.ErrorMessage);
            return DatabaseResult<InventoryTransaction?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByProductIdAsync( int productId )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided", productId);
                return DatabaseResult<IEnumerable<InventoryTransaction>>.Failure(
                    "Product ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<InventoryTransaction>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryTransactionRepository.GetByProductIdAsync(productId),
                $"Retrieving transactions for product {productId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {Count} transactions for product {ProductId}",
                    result.Value.Count(),
                    productId);
                return DatabaseResult<IEnumerable<InventoryTransaction>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve transactions for product {ProductId}: {ErrorMessage}", productId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryTransaction>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByLocationIdAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<InventoryTransaction>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<InventoryTransaction>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryTransactionRepository.GetByLocationIdAsync(locationId),
                $"Retrieving transactions for location {locationId}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {Count} transactions for location {LocationId}",
                    result.Value.Count(),
                    locationId);
                return DatabaseResult<IEnumerable<InventoryTransaction>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve transactions for location {LocationId}: {ErrorMessage}", locationId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryTransaction>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByTypeAsync( TransactionType type )
        {
            DatabaseResult<IEnumerable<InventoryTransaction>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryTransactionRepository.GetByTypeAsync(type),
                $"Retrieving transactions of type {type}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} transactions of type {Type}", result.Value.Count(), type);
                return DatabaseResult<IEnumerable<InventoryTransaction>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve transactions of type {Type}: {ErrorMessage}", type, result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryTransaction>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            DatabaseResult<IEnumerable<InventoryTransaction>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => inventoryTransactionRepository.GetByDateRangeAsync(startDate, endDate),
                $"Retrieving transactions between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} transactions in date range", result.Value.Count());
                return DatabaseResult<IEnumerable<InventoryTransaction>>.Success(result.Value);
            }

            logger.LogWarning("Failed to retrieve transactions in date range: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<InventoryTransaction>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion
    }
}
