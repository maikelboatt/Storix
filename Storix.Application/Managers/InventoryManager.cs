using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Managers
{
    /// <summary>
    ///     Main manager for all inventory operations - coordinates inventory, movements, and transactions.
    /// </summary>
    public class InventoryManager(
        IInventoryReadService inventoryReadService,
        IInventoryWriteService inventoryWriteService,
        IInventoryValidationService inventoryValidationService,
        IInventoryCacheService inventoryCacheService,
        ILogger<InventoryManager> logger ):IInventoryManager
    {
        #region Inventory Operations

        public async Task<DatabaseResult<Inventory?>> GetInventoryByIdAsync( int inventoryId )
        {
            // Try cache first
            Inventory? cached = inventoryCacheService.GetInventoryByIdInCache(inventoryId);
            if (cached != null)
                return DatabaseResult<Inventory?>.Success(cached);

            return await inventoryReadService.GetInventoryByIdAsync(inventoryId);
        }

        public async Task<DatabaseResult<Inventory?>> GetInventoryByProductAndLocationAsync(
            int productId,
            int locationId )
        {
            // Try cache first
            Inventory? cached = inventoryCacheService.GetInventoryByProductAndLocationInCache(productId, locationId);
            if (cached != null)
                return DatabaseResult<Inventory?>.Success(cached);

            return await inventoryReadService.GetInventoryByProductAndLocationAsync(productId, locationId);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByProductIdAsync( int productId )
        {
            // Try cache first
            List<Inventory> cached = inventoryCacheService.GetInventoryByProductIdInCache(productId);
            if (cached.Count > 0)
                return DatabaseResult<IEnumerable<Inventory>>.Success(cached);

            return await inventoryReadService.GetInventoryByProductIdAsync(productId);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByLocationIdAsync( int locationId )
        {
            // Try cache first
            List<Inventory> cached = inventoryCacheService.GetInventoryByLocationIdInCache(locationId);
            if (cached.Count > 0)
                return DatabaseResult<IEnumerable<Inventory>>.Success(cached);

            return await inventoryReadService.GetInventoryByLocationIdAsync(locationId);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetAllInventoryAsync() => await inventoryReadService.GetAllInventoryAsync();

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetLowStockItemsAsync( int threshold = 10 ) =>
            await inventoryReadService.GetLowStockItemsAsync(threshold);

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetOutOfStockItemsAsync() => await inventoryReadService.GetOutOfStockItemsAsync();

        public async Task<DatabaseResult<Inventory>> CreateInventoryAsync(
            int productId,
            int locationId,
            int initialStock ) => await inventoryWriteService.CreateInventoryAsync(productId, locationId, initialStock);

        public async Task<DatabaseResult<Inventory>> UpdateInventoryAsync( Inventory inventory ) => await inventoryWriteService.UpdateInventoryAsync(inventory);

        #endregion

        #region Stock Operations

        public async Task<DatabaseResult> AdjustStockAsync(
            int inventoryId,
            int quantityChange,
            string? notes,
            int userId )
        {
            DatabaseResult result = await inventoryWriteService.AdjustStockAsync(
                inventoryId,
                quantityChange,
                notes,
                userId);

            if (result.IsSuccess)
            {
                // Refresh cache after successful adjustment
                inventoryCacheService.RefreshStoreCache();
            }

            return result;
        }

        public async Task<DatabaseResult<InventoryMovement>> TransferStockAsync(
            int productId,
            int fromLocationId,
            int toLocationId,
            int quantity,
            string? notes,
            int userId )
        {
            DatabaseResult<InventoryMovement> result = await inventoryWriteService.TransferStockAsync(
                productId,
                fromLocationId,
                toLocationId,
                quantity,
                notes,
                userId);

            if (result.IsSuccess)
            {
                // Refresh cache after successful transfer
                inventoryCacheService.RefreshStoreCache();
            }

            return result;
        }

        public async Task<DatabaseResult> ReserveStockAsync( int inventoryId, int quantity )
        {
            DatabaseResult result = await inventoryWriteService.ReserveStockAsync(inventoryId, quantity);

            if (result.IsSuccess)
            {
                inventoryCacheService.RefreshStoreCache();
            }

            return result;
        }

        public async Task<DatabaseResult> ReleaseReservedStockAsync( int inventoryId, int quantity )
        {
            DatabaseResult result = await inventoryWriteService.ReleaseReservedStockAsync(inventoryId, quantity);

            if (result.IsSuccess)
            {
                inventoryCacheService.RefreshStoreCache();
            }

            return result;
        }

        #endregion

        #region Movement Operations

        public async Task<DatabaseResult<InventoryMovement?>> GetMovementByIdAsync( int movementId ) =>
            await inventoryReadService.GetMovementByIdAsync(movementId);

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByProductIdAsync( int productId ) =>
            await inventoryReadService.GetMovementsByProductIdAsync(productId);

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByLocationIdAsync( int locationId ) =>
            await inventoryReadService.GetMovementsByLocationIdAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate ) => await inventoryReadService.GetMovementsByDateRangeAsync(startDate, endDate);

        #endregion

        #region Transaction Operations

        public async Task<DatabaseResult<InventoryTransaction?>> GetTransactionByIdAsync( int transactionId ) =>
            await inventoryReadService.GetTransactionByIdAsync(transactionId);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByProductIdAsync( int productId ) =>
            await inventoryReadService.GetTransactionsByProductIdAsync(productId);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByLocationIdAsync( int locationId ) =>
            await inventoryReadService.GetTransactionsByLocationIdAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByTypeAsync( TransactionType type ) =>
            await inventoryReadService.GetTransactionsByTypeAsync(type);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate ) => await inventoryReadService.GetTransactionsByDateRangeAsync(startDate, endDate);

        public async Task<DatabaseResult<InventoryTransaction>> CreateTransactionAsync(
            int productId,
            int locationId,
            TransactionType type,
            int quantity,
            decimal? unitCost,
            string? reference,
            string? notes,
            int userId ) => await inventoryWriteService.CreateTransactionAsync(
            productId,
            locationId,
            type,
            quantity,
            unitCost,
            reference,
            notes,
            userId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> InventoryExistsAsync( int inventoryId ) => await inventoryValidationService.InventoryExistsAsync(inventoryId);

        public async Task<DatabaseResult<bool>> InventoryExistsForProductAndLocationAsync( int productId, int locationId ) =>
            await inventoryValidationService.InventoryExistsForProductAndLocationAsync(productId, locationId);

        public async Task<DatabaseResult> ValidateStockAdjustment( int inventoryId, int quantityChange ) =>
            await inventoryValidationService.ValidateStockAdjustment(inventoryId, quantityChange);

        public async Task<DatabaseResult> ValidateStockTransfer(
            int productId,
            int fromLocationId,
            int toLocationId,
            int quantity ) => await inventoryValidationService.ValidateStockTransfer(
            productId,
            fromLocationId,
            toLocationId,
            quantity);

        #endregion

        #region Cache Management

        public void RefreshCache() => inventoryCacheService.RefreshStoreCache();

        #endregion

        #region Statistics

        public async Task<DatabaseResult<int>> GetTotalInventoryCountAsync() => await inventoryReadService.GetTotalInventoryCountAsync();

        public int GetTotalStockForProduct( int productId ) => inventoryCacheService.GetTotalStockForProductInCache(productId);

        #endregion
    }
}
