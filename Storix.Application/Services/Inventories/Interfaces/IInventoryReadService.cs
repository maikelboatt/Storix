using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Inventories.Interfaces
{
    public interface IInventoryReadService
    {
        Task<DatabaseResult<Inventory?>> GetInventoryByIdAsync( int inventoryId );

        Task<DatabaseResult<Inventory?>> GetInventoryByProductAndLocationAsync( int productId, int locationId );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByProductIdAsync( int productId );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetInventoryByLocationIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetAllInventoryAsync();

        Task<DatabaseResult<IEnumerable<Inventory>>> GetLowStockItemsAsync( int threshold = 10 );

        Task<DatabaseResult<IEnumerable<Inventory>>> GetOutOfStockItemsAsync();

        Task<DatabaseResult<int>> GetTotalInventoryCountAsync();

        Task<DatabaseResult<InventoryMovement?>> GetMovementByIdAsync( int movementId );

        Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByProductIdAsync( int productId );

        Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByLocationIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<InventoryMovement>>> GetMovementsByDateRangeAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<InventoryTransaction?>> GetTransactionByIdAsync( int transactionId );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByProductIdAsync( int productId );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByLocationIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByTypeAsync( TransactionType type );

        Task<DatabaseResult<IEnumerable<InventoryTransaction>>> GetTransactionsByDateRangeAsync( DateTime startDate, DateTime endDate );
    }
}
