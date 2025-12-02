using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Inventories.Interfaces
{
    public interface IInventoryValidationService
    {
        Task<DatabaseResult<bool>> InventoryExistsAsync( int inventoryId );

        Task<DatabaseResult<bool>> InventoryExistsForProductAndLocationAsync( int productId, int locationId );

        Task<DatabaseResult> ValidateStockAdjustment( int inventoryId, int quantityChange );

        Task<DatabaseResult> ValidateStockTransfer( int productId,
            int fromLocationId,
            int toLocationId,
            int quantity );

        Task<DatabaseResult> ValidateStockReservation( int inventoryId, int quantity );

        Task<DatabaseResult> ValidateReservedStockRelease( int inventoryId, int quantity );
    }
}
