using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IInventoryMovementRepository
    {
        /// <summary>
        ///     Check if a movement exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int movementId );

        /// <summary>
        ///     Gets the total count of inventory movements.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of movements for a specific product.
        /// </summary>
        Task<int> GetCountByProductIdAsync( int productId );

        /// <summary>
        ///     Gets the count of movements involving a specific location.
        /// </summary>
        Task<int> GetCountByLocationIdAsync( int locationId );

        /// <summary>
        ///     Gets a movement by ID.
        /// </summary>
        Task<InventoryMovement?> GetByIdAsync( int movementId );

        /// <summary>
        ///     Gets all movements for a specific product.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetByProductIdAsync( int productId );

        /// <summary>
        ///     Gets all movements involving a specific location (from or to).
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetByLocationIdAsync( int locationId );

        /// <summary>
        ///     Gets movements from a specific location.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetByFromLocationAsync( int fromLocationId );

        /// <summary>
        ///     Gets movements to a specific location.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetByToLocationAsync( int toLocationId );

        /// <summary>
        ///     Gets movements by user who created them.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetByCreatedByAsync( int userId );

        /// <summary>
        ///     Gets movements within a date range.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync( DateTime startDate, DateTime endDate );

        /// <summary>
        ///     Gets all inventory movements.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetAllAsync();

        /// <summary>
        ///     Gets a paged list of movements.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches movements with optional filters.
        /// </summary>
        Task<IEnumerable<InventoryMovement>> SearchAsync(
            int? productId = null,
            int? fromLocationId = null,
            int? toLocationId = null,
            int? createdBy = null,
            DateTime? startDate = null,
            DateTime? endDate = null );

        /// <summary>
        ///     Creates a new inventory movement and returns it with its generated ID.
        /// </summary>
        Task<InventoryMovement> CreateAsync( InventoryMovement movement );

        /// <summary>
        ///     Permanently deletes a movement by ID.
        ///     WARNING: This permanently removes the movement record.
        /// </summary>
        Task<DatabaseResult> DeleteAsync( int movementId );
    }
}
