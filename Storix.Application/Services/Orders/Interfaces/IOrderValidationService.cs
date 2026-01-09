using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderValidationService
    {
        Task<DatabaseResult<bool>> OrderExistsAsync( int orderId );

        Task<DatabaseResult> ValidateForRevertToDraft( int orderId );

        Task<DatabaseResult> ValidateForActivation( int orderId );

        Task<DatabaseResult> ValidateForFulfillment( int orderId );

        Task<DatabaseResult> ValidateForCompletion( int orderId );

        Task<DatabaseResult> ValidateForCancellation( int orderId );

        Task<DatabaseResult> ValidateForDeletion( int orderId );

        Task<DatabaseResult<bool>> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false );

        Task<DatabaseResult<bool>> CustomerHasOrdersAsync( int customerId, bool activeOnly = false );

        /// <summary>
        /// Checks if a location has any orders (active or historical).
        /// </summary>
        Task<DatabaseResult<bool>> LocationHasOrdersAsync( int locationId, bool activeOnly = false );

        /// <summary>
        /// Checks if a location can be deleted (no active orders associated).
        /// </summary>
        Task<DatabaseResult<bool>> CanDeleteLocationAsync( int locationId );

        /// <summary>
        /// Validates if a location exists and is valid for creating an order.
        /// </summary>
        Task<DatabaseResult> ValidateLocationForOrderAsync( int locationId, OrderType orderType );

        /// <summary>
        /// Validates if an order can be transferred to a new location.
        /// </summary>
        Task<DatabaseResult> ValidateOrderTransferAsync( int orderId, int newLocationId );

        /// <summary>
        /// Checks if an order can be transferred to a different location.
        /// </summary>
        Task<DatabaseResult<bool>> CanTransferOrderToLocationAsync( int orderId, int newLocationId );
    }
}
