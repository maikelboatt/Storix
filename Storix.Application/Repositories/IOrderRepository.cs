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
        Task<IEnumerable<Order>> SearchAsync( string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null );

        /// <summary>
        ///     Permanently deletes an order by ID.
        ///     WARNING: Orders should typically NOT be deleted. Use status changes instead.
        ///     This method should only be used for cleaning up test data or by administrators.
        /// </summary>
        Task<DatabaseResult> DeleteAsync( int orderId );

        Task<OrderStatisticsDto?> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate );

        /// <summary>
        ///     Gets the total value of orders by the status.
        /// </summary>
        Task<decimal> GetTotalValueByStatusAsync( OrderStatus status );

        /// <summary>
        ///     Gets the total value of orders by the type.
        /// </summary>
        Task<decimal> GetTotalValueByTypeAsync( OrderType type );

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
        ///     Checks if an order can be activated (only Draft orders can be activated).
        /// </summary>
        Task<bool> CanBeActivated( int orderId );

        /// <summary>
        ///     Checks if an order can be cancelled (only Draft and Active orders can be cancelled).
        /// </summary>
        Task<bool> CanBeCancelled( int orderId );

        /// <summary>
        ///     Checks if an order can be completed (only Active orders can be completed).
        /// </summary>
        /// <param name="orderId" ></param>
        /// <returns></returns>
        Task<bool> CanBeCompleted( int orderId );

        /// <summary>
        ///     Gets a paged list of orders.
        /// </summary>
        Task<IEnumerable<Order>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Gets the total counts of orders.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the counts of orders by type.
        /// </summary>
        Task<int> GetCountByTypeAsync( OrderType type );

        /// <summary>
        ///     Gets the count of orders by status.
        /// </summary>
        Task<int> GetCountByStatusAsync( OrderStatus status );

        /// <summary>
        ///     Gets an order by its ID.
        /// </summary>
        Task<Order?> GetByIdAsync( int orderId );

        /// <summary>
        ///     Gets all order.
        /// </summary>
        Task<IEnumerable<Order>> GetAllAsync();

        /// <summary>
        ///     Gets orders by Type (Purchase or Sale)
        /// </summary>
        Task<IEnumerable<Order>> GetByTypeAsync( OrderType type );

        /// <summary>
        ///     Gets orders by Status (Draft, Active, Completed, Cancelled)
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
        ///     Gets orders by date range.
        /// </summary>
        Task<IEnumerable<Order>> GetByDateRangeAsync( DateTime startDate, DateTime endDate );

        /// <summary>
        ///     Gets overdue orders (DeliveryDate passed but status is still Draft or Active).
        /// </summary>
        Task<IEnumerable<Order>> GetOverdueOrdersAsync();

        /// <summary>
        ///     Gets orders by created by a specific user.
        /// </summary>
        /// <param name="createdBy" ></param>
        /// <returns></returns>
        Task<IEnumerable<Order>> GetByCreatedByAsync( int createdBy );

        /// <summary>
        ///     Creates a new order and returns it with its generated ID.
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
        ///     Activates an Order by setting its status from Draft to Active.
        /// </summary>
        Task<DatabaseResult> ActivateOrderAsync( int orderId );

        /// <summary>
        ///     Complete an order by setting its status to completed.
        /// </summary>
        Task<DatabaseResult> CompleteOrderAsync( int orderId );

        /// <summary>
        ///     Cancels an order by setting its status to cancelled.
        /// </summary>
        Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null );
    }
}
