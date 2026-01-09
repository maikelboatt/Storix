using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IOrderRepository
    {
        /// <summary>
        ///     Check if an order exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int orderId );

        /// <summary>
        ///     Checks if a supplier has any orders (active or historical).
        /// </summary>
        Task<bool> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false );

        /// <summary>
        ///     Checks if a customer has orders (active or historical).
        /// </summary>
        Task<bool> CustomerHasOrdersAsync( int customerId, bool activeOnly = false );

        /// <summary>
        /// Checks if a location has any orders.
        /// </summary>
        Task<bool> LocationHasOrdersAsync( int locationId, bool activeOnly = false );

        /// <summary>
        /// Checks if a location can be deleted (no active orders).
        /// </summary>
        Task<bool> CanDeleteLocationAsync( int locationId );

        /// <summary>
        ///     Checks if an order can be reverted to draft (only active orders can be reverted to draft).
        /// </summary>
        Task<bool> CanBeRevertedToDraft( int orderId );

        /// <summary>
        ///     Checks if an order can be activated (only Draft orders can be activated).
        /// </summary>
        Task<bool> CanBeActivated( int orderId );

        /// <summary>
        ///     Checks if an order can be fulfilled (only Active orders can be fulfilled).
        /// </summary>
        Task<bool> CanBeFulfilled( int orderId );

        /// <summary>
        ///     Checks if an order can be cancelled (only Draft and Active orders can be cancelled).
        /// </summary>
        Task<bool> CanBeCancelled( int orderId );

        /// <summary>
        ///     Checks if an order can be completed (only Active orders can be completed).
        /// </summary>
        Task<bool> CanBeCompleted( int orderId );

        /// <summary>
        ///     Gets the total count of orders.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of orders by type.
        /// </summary>
        Task<int> GetCountByTypeAsync( OrderType type );

        /// <summary>
        ///     Gets the count of orders by status.
        /// </summary>
        Task<int> GetCountByStatusAsync( OrderStatus status );

        /// <summary>
        /// Gets the count of orders by location.
        /// </summary>
        Task<int> GetOrderCountByLocationAsync( int locationId );

        /// <summary>
        ///  Gets order counts for all locations.
        /// </summary>
        Task<Dictionary<int, int>> GetOrderCountsByLocationAsync();

        /// <summary>
        ///     Gets an order by its ID.
        /// </summary>
        Task<Order?> GetByIdAsync( int orderId );

        /// <summary>
        ///     Gets all orders.
        /// </summary>
        Task<IEnumerable<Order>> GetAllAsync();

        /// <summary>
        ///     Gets orders by Type (Purchase or Sale).
        /// </summary>
        Task<IEnumerable<Order>> GetByTypeAsync( OrderType type );

        /// <summary>
        ///     Gets orders by Status (Draft, Active, Completed, Cancelled).
        /// </summary>
        Task<IEnumerable<Order>> GetByStatusAsync( OrderStatus status );

        /// <summary>
        ///     Gets purchase orders by supplier ID.
        /// </summary>
        Task<IEnumerable<Order>> GetBySupplierAsync( int supplierId );

        /// <summary>
        ///     Gets sale orders by customer ID.
        /// </summary>
        Task<IEnumerable<Order>> GetByCustomerAsync( int customerId );

        /// <summary>
        ///     Gets sale orders by location ID.
        /// </summary>
        Task<IEnumerable<Order>> GetByLocationAsync( int locationId );

        /// <summary>
        /// Gets orders by location and status.
        /// </summary>
        Task<IEnumerable<Order>> GetByLocationIdAndStatusAsync( int locationId, OrderStatus status );

        /// <summary>
        /// Gets orders by location and type.
        /// </summary>
        Task<IEnumerable<Order>> GetByLocationIdAndTypeAsync( int locationId, OrderType type );

        /// <summary>
        /// Gets active orders by location (Draft, Active, Fulfilled).
        /// </summary>
        Task<IEnumerable<Order>> GetActiveOrdersByLocationAsync( int locationId );

        /// <summary>
        /// Gets orders by multiple location IDs.
        /// </summary>
        Task<IEnumerable<Order>> GetByLocationIdsAsync( IEnumerable<int> locationIds );

        /// <summary>
        ///     Gets orders by date range.
        /// </summary>
        Task<IEnumerable<Order>> GetByDateRangeAsync( DateTime startDate, DateTime endDate );

        /// <summary>
        /// Gets orders by location and date range.
        /// </summary>
        Task<IEnumerable<Order>> GetByLocationAndDateRangeAsync(
            int locationId,
            DateTime startDate,
            DateTime endDate );

        /// <summary>
        ///     Gets overdue orders (DeliveryDate passed but status is still Draft or Active).
        /// </summary>
        Task<IEnumerable<Order>> GetOverdueOrdersAsync();

        /// <summary>
        /// Gets overdue orders by location.
        /// </summary>
        Task<IEnumerable<Order>> GetOverdueOrdersByLocationAsync( int locationId );

        /// <summary>
        ///     Gets orders created by a specific user.
        /// </summary>
        Task<IEnumerable<Order>> GetByCreatedByAsync( int createdBy );

        /// <summary>
        ///     Gets a paged list of orders.
        ///     Uses SQL Server OFFSET-FETCH syntax.
        /// </summary>
        Task<IEnumerable<Order>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches orders with multiple optional filters.
        /// </summary>
        Task<IEnumerable<Order>> SearchAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int locationId = 0,
            DateTime? startDate = null,
            DateTime? endDate = null );

        /// <summary>
        ///     Gets order statistics for a date range.
        /// </summary>
        Task<OrderStatisticsDto?> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate );

        /// <summary>
        /// Gets total revenue by location (from completed sale orders).
        /// </summary>
        Task<decimal> GetTotalRevenueByLocationAsync( int locationId );

        /// <summary>
        /// Gets order status distribution by location.
        /// </summary>
        Task<Dictionary<OrderStatus, int>> GetOrderStatusCountByLocationAsync( int locationId );

        /// <summary>
        ///     Gets the total value of orders by status.
        /// </summary>
        Task<decimal> GetTotalValueByStatusAsync( OrderStatus status );

        /// <summary>
        ///     Gets the total value of orders by type.
        /// </summary>
        Task<decimal> GetTotalValueByTypeAsync( OrderType type );

        /// <summary>
        ///     Creates a new order and returns it with its generated ID.
        ///     Uses SQL Server SCOPE_IDENTITY() to retrieve the newly inserted ID.
        /// </summary>
        Task<Order> CreateAsync( Order order );

        /// <summary>
        ///     Updates an existing order (status, delivery date, and notes only).
        /// </summary>
        Task<Order> UpdateAsync( Order order );

        /// <summary>
        ///     Updates the status of an order.
        /// </summary>
        Task<DatabaseResult> UpdateStatusAsync( int orderId, OrderStatus status );

        /// <summary>
        /// ✅ NEW: Transfers an order to a different location.
        /// </summary>
        Task<DatabaseResult> TransferOrderToLocationAsync( int orderId, int newLocationId );

        /// <summary>
        ///     Reverts an order to draft by setting its status from Active to Draft.
        /// </summary>
        Task<DatabaseResult> RevertToDraftOrderAsync( int orderId );

        /// <summary>
        ///     Activates an order by setting its status from Draft to Active.
        /// </summary>
        Task<DatabaseResult> ActivateOrderAsync( int orderId );

        /// <summary>
        ///     Fulfills an order by setting its status from Active to Fulfilled.
        /// </summary>
        Task<DatabaseResult> FulfillOrderAsync( int orderId );

        /// <summary>
        ///     Completes an order by setting its status to Completed.
        /// </summary>
        Task<DatabaseResult> CompleteOrderAsync( int orderId );

        /// <summary>
        ///     Cancels an order by setting its status to Cancelled.
        ///     Uses SQL Server string concatenation with + operator and CHAR(13)+CHAR(10) for newline.
        /// </summary>
        Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null );

        /// <summary>
        ///     Permanently deletes an order by ID.
        ///     WARNING: Orders should typically NOT be deleted. Use status changes instead.
        ///     This method should only be used for cleaning up test data or by administrators.
        /// </summary>
        Task<DatabaseResult> DeleteAsync( int orderId );
    }
}
