using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Inventories.Interfaces
{
    public interface IInventoryWriteService
    {
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

        Task<DatabaseResult<Inventory>> CreateInventoryAsync(
            int productId,
            int locationId,
            int initialStock );

        Task<DatabaseResult<Inventory>> UpdateInventoryAsync( Inventory inventory );

        Task<DatabaseResult<InventoryTransaction>> CreateTransactionAsync(
            int productId,
            int locationId,
            TransactionType type,
            int quantity,
            decimal? unitCost,
            string? reference,
            string? notes,
            int userId );
    }
}
