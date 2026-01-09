using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Stores.Inventories;
using Storix.Application.Stores.Products;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Managers
{
    /// <summary>
    ///     Main manager for all inventory operations - coordinates inventory, movements, and transactions.
    ///     Now also updates ProductStore to keep UI in sync.
    /// </summary>
    public class InventoryManager:IInventoryManager
    {
        private readonly IInventoryReadService _inventoryReadService;
        private readonly IInventoryWriteService _inventoryWriteService;
        private readonly IInventoryValidationService _inventoryValidationService;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        private readonly IInventoryStore _inventoryStore;
        private readonly IProductStore _productStore;
        private readonly ILogger<InventoryManager> _logger;

        public InventoryManager(
            IInventoryReadService inventoryReadService,
            IInventoryWriteService inventoryWriteService,
            IInventoryValidationService inventoryValidationService,
            IInventoryCacheReadService inventoryCacheReadService,
            IInventoryStore inventoryStore,
            IProductStore productStore,
            ILogger<InventoryManager> logger )
        {
            _inventoryReadService = inventoryReadService;
            _inventoryWriteService = inventoryWriteService;
            _inventoryValidationService = inventoryValidationService;
            _inventoryCacheReadService = inventoryCacheReadService;
            _inventoryStore = inventoryStore;
            _productStore = productStore;
            _logger = logger;
        }

        #region Inventory Operations

        public async Task<DatabaseResult<Inventory?>> GetInventoryByIdAsync( int inventoryId )
        {
            // Try cache first
            Inventory? cached = _inventoryCacheReadService.GetInventoryByIdInCache(inventoryId);
            if (cached != null)
                return DatabaseResult<Inventory?>.Success(cached);

            return await _inventoryReadService.GetInventoryByIdAsync(inventoryId);
        }

        public async Task<DatabaseResult<Inventory?>> GetInventoryByProductAndLocationAsync(
            int productId,
            int locationId )
        {
            // Try cache first
            Inventory? cached = _inventoryCacheReadService.GetInventoryByProductAndLocationInCache(productId, locationId);
            if (cached != null)
                return DatabaseResult<Inventory?>.Success(cached);

            return await _inventoryReadService.GetInventoryByProductAndLocationAsync(productId, locationId);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByProductIdAsync( int productId )
        {
            // Try cache first
            List<Inventory> cached = _inventoryCacheReadService.GetInventoryByProductIdInCache(productId);
            if (cached.Count > 0)
                return DatabaseResult<IEnumerable<Inventory>>.Success(cached);

            return await _inventoryReadService.GetInventoryByProductIdAsync(productId);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByLocationIdAsync( int locationId )
        {
            // Try cache first
            List<Inventory> cached = _inventoryCacheReadService.GetInventoryByLocationIdInCache(locationId);
            if (cached.Count > 0)
                return DatabaseResult<IEnumerable<Inventory>>.Success(cached);

            return await _inventoryReadService.GetInventoryByLocationIdAsync(locationId);
        }

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetAllInventoryAsync() => await _inventoryReadService.GetAllInventoryAsync();

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetLowStockItemsAsync( int threshold = 10 ) =>
            await _inventoryReadService.GetLowStockItemsAsync(threshold);

        public async Task<DatabaseResult<IEnumerable<Inventory>>> GetOutOfStockItemsAsync() => await _inventoryReadService.GetOutOfStockItemsAsync();

        public async Task<DatabaseResult<Inventory>> CreateInventoryAsync(
            int productId,
            int locationId,
            int initialStock )
        {
            DatabaseResult<Inventory> result = await _inventoryWriteService.CreateInventoryAsync(
                productId,
                locationId,
                initialStock);

            if (result.IsSuccess)
            {
                // Refresh inventory cache
                _inventoryCacheReadService.RefreshStoreCache();

                int totalStock = _inventoryStore.GetTotalStockForProduct(productId);
                _productStore.UpdateProductStock(productId, totalStock);

                _logger.LogInformation(
                    "✅ Inventory created and caches updated - Product: {ProductId}, Location: {LocationId}, Total Stock: {Total}",
                    productId,
                    locationId,
                    totalStock);
            }

            return result;
        }

        public async Task<DatabaseResult<Inventory>> UpdateInventoryAsync( Inventory inventory ) =>
            await _inventoryWriteService.UpdateInventoryAsync(inventory);

        #endregion

        #region Stock Operations

        public async Task<DatabaseResult> AdjustStockAsync(
            int inventoryId,
            int quantityChange,
            string? notes,
            int userId )
        {
            Inventory? inventory = _inventoryCacheReadService.GetInventoryByIdInCache(inventoryId);
            int? productId = inventory?.ProductId;

            DatabaseResult result = await _inventoryWriteService.AdjustStockAsync(
                inventoryId,
                quantityChange,
                notes,
                userId);

            if (result.IsSuccess)
            {
                // Refresh inventory cache
                _inventoryCacheReadService.RefreshStoreCache();

                if (productId.HasValue)
                {
                    int totalStock = _inventoryStore.GetTotalStockForProduct(productId.Value);
                    _productStore.UpdateProductStock(productId.Value, totalStock);

                    _logger.LogInformation(
                        "✅ Stock adjusted and caches updated - Product: {ProductId}, Change: {Change}, Total Stock: {Total}",
                        productId.Value,
                        quantityChange,
                        totalStock);
                }
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
            DatabaseResult<InventoryMovement> result = await _inventoryWriteService.TransferStockAsync(
                productId,
                fromLocationId,
                toLocationId,
                quantity,
                notes,
                userId);

            if (result.IsSuccess)
            {
                // Refresh inventory cache
                _inventoryCacheReadService.RefreshStoreCache();

                // But we still update to ensure cache consistency
                int totalStock = _inventoryStore.GetTotalStockForProduct(productId);
                _productStore.UpdateProductStock(productId, totalStock);

                _logger.LogInformation(
                    "✅ Stock transferred and caches updated - Product: {ProductId}, From: {From}, To: {To}, Quantity: {Quantity}",
                    productId,
                    fromLocationId,
                    toLocationId,
                    quantity);
            }

            return result;
        }

        public async Task<DatabaseResult> ReserveStockAsync( int inventoryId, int quantity )
        {
            DatabaseResult result = await _inventoryWriteService.ReserveStockAsync(inventoryId, quantity);

            if (result.IsSuccess)
            {
                _inventoryCacheReadService.RefreshStoreCache();
            }

            return result;
        }

        public async Task<DatabaseResult> ReleaseReservedStockAsync( int inventoryId, int quantity )
        {
            DatabaseResult result = await _inventoryWriteService.ReleaseReservedStockAsync(inventoryId, quantity);

            if (result.IsSuccess)
            {
                _inventoryCacheReadService.RefreshStoreCache();
            }

            return result;
        }

        #endregion

        #region Movement Operations

        public async Task<DatabaseResult<InventoryMovement?>> GetMovementByIdAsync( int movementId ) =>
            await _inventoryReadService.GetMovementByIdAsync(movementId);

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByProductIdAsync( int productId ) =>
            await _inventoryReadService.GetMovementsByProductIdAsync(productId);

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByLocationIdAsync( int locationId ) =>
            await _inventoryReadService.GetMovementsByLocationIdAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate ) => await _inventoryReadService.GetMovementsByDateRangeAsync(startDate, endDate);

        #endregion

        #region Transaction Operations

        public async Task<DatabaseResult<InventoryTransaction?>> GetTransactionByIdAsync( int transactionId ) =>
            await _inventoryReadService.GetTransactionByIdAsync(transactionId);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByProductIdAsync( int productId ) =>
            await _inventoryReadService.GetTransactionsByProductIdAsync(productId);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByLocationIdAsync( int locationId ) =>
            await _inventoryReadService.GetTransactionsByLocationIdAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByTypeAsync( TransactionType type ) =>
            await _inventoryReadService.GetTransactionsByTypeAsync(type);

        public async Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate ) => await _inventoryReadService.GetTransactionsByDateRangeAsync(startDate, endDate);

        public async Task<DatabaseResult<InventoryTransaction>> CreateTransactionAsync(
            int productId,
            int locationId,
            TransactionType type,
            int quantity,
            decimal? unitCost,
            string? reference,
            string? notes,
            int userId ) => await _inventoryWriteService.CreateTransactionAsync(
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

        public async Task<DatabaseResult<bool>> InventoryExistsAsync( int inventoryId ) => await _inventoryValidationService.InventoryExistsAsync(inventoryId);

        public async Task<DatabaseResult<bool>> InventoryExistsForProductAndLocationAsync( int productId, int locationId ) =>
            await _inventoryValidationService.InventoryExistsForProductAndLocationAsync(productId, locationId);

        public async Task<DatabaseResult> ValidateStockAdjustment( int inventoryId, int quantityChange ) =>
            await _inventoryValidationService.ValidateStockAdjustment(inventoryId, quantityChange);

        public async Task<DatabaseResult> ValidateStockTransfer(
            int productId,
            int fromLocationId,
            int toLocationId,
            int quantity ) => await _inventoryValidationService.ValidateStockTransfer(
            productId,
            fromLocationId,
            toLocationId,
            quantity);

        #endregion

        #region Cache Management

        public void RefreshCache() => _inventoryCacheReadService.RefreshStoreCache();

        #endregion

        #region Statistics

        public async Task<DatabaseResult<int>> GetTotalInventoryCountAsync() => await _inventoryReadService.GetTotalInventoryCountAsync();

        public int GetCurrentStockForProduct( int productId ) => _inventoryCacheReadService.GetCurrentStockForProductInCache(productId);

        public Dictionary<int, int> GetAllProductStockLevels() => _inventoryCacheReadService.GetAllProductStockLevelsInCache();

        public int GetProductStockAtLocation( int productId, int locationId ) =>
            _inventoryCacheReadService.GetProductStockAtLocationInCache(productId, locationId);

        public int GetAvailableStockForProduct( int productId ) => _inventoryCacheReadService.GetAvailableStockForProductInCache(productId);

        public int GetReservedStockForProduct( int productId ) => _inventoryCacheReadService.GetReservedStockForProductInCache(productId);

        public int GetAvailableStockAtLocation( int productId, int locationId ) =>
            _inventoryCacheReadService.GetAvailableStockAtLocationInCache(productId, locationId);

        public Dictionary<int, int> GetStockByLocationInCache() => _inventoryCacheReadService.GetStockByLocationInCache();

        #endregion
    }
}
