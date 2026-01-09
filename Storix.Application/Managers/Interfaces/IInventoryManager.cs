using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Managers.Interfaces
{
    public interface IInventoryManager
    {
        Task<DatabaseResult<Inventory?>> GetInventoryByIdAsync( int inventoryId );

        Task<DatabaseResult<Inventory?>> GetInventoryByProductAndLocationAsync(
            int productId,
            int locationId );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByProductIdAsync( int productId );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByLocationIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetAllInventoryAsync();

        Task<DatabaseResult<IEnumerable<Inventory>>> GetLowStockItemsAsync( int threshold = 10 );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetOutOfStockItemsAsync();

        Task<DatabaseResult<Inventory>> CreateInventoryAsync(
            int productId,
            int locationId,
            int initialStock );

        Task<DatabaseResult<Inventory>> UpdateInventoryAsync( Inventory inventory );

        Task<DatabaseResult> AdjustStockAsync(
            int inventoryId,
            int quantityChange,
            string? notes,
            int userId );

        Task<DatabaseResult<InventoryMovement>> TransferStockAsync(
            int productId,
            int fromLocationId,
            int toLocationId,
            int quantity,
            string? notes,
            int userId );

        Task<DatabaseResult> ReserveStockAsync( int inventoryId, int quantity );

        Task<DatabaseResult> ReleaseReservedStockAsync( int inventoryId, int quantity );

        Task<DatabaseResult<InventoryMovement?>> GetMovementByIdAsync( int movementId );

        Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByProductIdAsync( int productId );

        Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByLocationIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate );

        Task<DatabaseResult<InventoryTransaction?>> GetTransactionByIdAsync( int transactionId );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByProductIdAsync( int productId );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByLocationIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByTypeAsync( TransactionType type );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate );

        Task<DatabaseResult<InventoryTransaction>> CreateTransactionAsync(
            int productId,
            int locationId,
            TransactionType type,
            int quantity,
            decimal? unitCost,
            string? reference,
            string? notes,
            int userId );

        Task<DatabaseResult<bool>> InventoryExistsAsync( int inventoryId );

        Task<DatabaseResult<bool>> InventoryExistsForProductAndLocationAsync( int productId, int locationId );

        Task<DatabaseResult> ValidateStockAdjustment( int inventoryId, int quantityChange );

        Task<DatabaseResult> ValidateStockTransfer(
            int productId,
            int fromLocationId,
            int toLocationId,
            int quantity );

        void RefreshCache();

        Task<DatabaseResult<int>> GetTotalInventoryCountAsync();

        int GetCurrentStockForProduct( int productId );

        Dictionary<int, int> GetAllProductStockLevels();

        public int GetProductStockAtLocation( int productId, int locationId );

        public int GetAvailableStockForProduct( int productId );

        public int GetReservedStockForProduct( int productId );

        public int GetAvailableStockAtLocation( int productId, int locationId );

        Dictionary<int, int> GetStockByLocationInCache();
    }
}
