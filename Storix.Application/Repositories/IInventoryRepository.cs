using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IInventoryRepository
    {
        /// <summary>
        ///     Check if an inventory record exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int inventoryId );

        /// <summary>
        ///     Check if inventory exists for a product at a specific location.
        /// </summary>
        Task<bool> ExistsByProductAndLocationAsync( int productId, int locationId );

        /// <summary>
        ///     Gets the total count of inventory records.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of low stock items (where CurrentStock <= threshold).
        /// </summary>
        Task<int> GetLowStockCountAsync( int threshold = 10 );

        /// <summary>
        ///     Gets the count of out-of-stock items.
        /// </summary>
        Task<int> GetOutOfStockCountAsync();

        /// <summary>
        ///     Gets an inventory record by ID.
        /// </summary>
        Task<Inventory?> GetByIdAsync( int inventoryId );

        /// <summary>
        ///     Gets inventory for a specific product at a specific location.
        /// </summary>
        Task<Inventory?> GetByProductAndLocationAsync( int productId, int locationId );

        /// <summary>
        ///     Gets all inventory records for a specific product across all locations.
        /// </summary>
        Task<IEnumerable<Inventory>> GetByProductIdAsync( int productId );

        /// <summary>
        ///     Gets all inventory records for a specific location.
        /// </summary>
        Task<IEnumerable<Inventory>> GetByLocationIdAsync( int locationId );

        /// <summary>
        ///     Gets all inventory records.
        /// </summary>
        Task<IEnumerable<Inventory>> GetAllAsync();

        /// <summary>
        ///     Gets low stock items (where CurrentStock <= threshold).
        /// </summary>
        Task<IEnumerable<Inventory>> GetLowStockItemsAsync( int threshold = 10 );

        /// <summary>
        ///     Gets out-of-stock items.
        /// </summary>
        Task<IEnumerable<Inventory>> GetOutOfStockItemsAsync();

        /// <summary>
        ///     Gets a paged list of inventory records.
        /// </summary>
        Task<IEnumerable<Inventory>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches inventory with optional filters.
        /// </summary>
        Task<IEnumerable<Inventory>> SearchAsync(
            int? productId = null,
            int? locationId = null,
            int? minStock = null,
            int? maxStock = null );

        /// <summary>
        ///     Creates a new inventory record and returns it with its generated ID.
        /// </summary>
        Task<Inventory> CreateAsync( Inventory inventory );

        /// <summary>
        ///     Updates an existing inventory record.
        /// </summary>
        Task<Inventory> UpdateAsync( Inventory inventory );

        /// <summary>
        ///     Adjusts stock level for a specific inventory record.
        /// </summary>
        Task<DatabaseResult> AdjustStockAsync( int inventoryId, int quantityChange );

        /// <summary>
        ///     Reserves stock for an inventory record.
        /// </summary>
        Task<DatabaseResult> ReserveStockAsync( int inventoryId, int quantity );

        /// <summary>
        ///     Releases reserved stock.
        /// </summary>
        Task<DatabaseResult> ReleaseReservedStockAsync( int inventoryId, int quantity );

        /// <summary>
        ///     Permanently deletes an inventory record by ID.
        ///     WARNING: This permanently removes the inventory record from the database.
        /// </summary>
        Task<DatabaseResult> DeleteAsync( int inventoryId );
    }
}
