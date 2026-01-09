using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderService
    {
        OrderDto? GetOrderById( int orderId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetAllOrdersAsync();

        Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrderListAsync();

        Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrderListAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByTypeAsync( OrderType type );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByStatusAsync( OrderStatus status );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersBySupplierAsync( int supplierId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync( int customerId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAsync( int locationId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndStatusAsync( int locationId, OrderStatus status );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndTypeAsync( int locationId, OrderType type );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetActiveOrdersByLocationAsync( int locationId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndDateRangeAsync(
            int locationId,
            DateTime startDate,
            DateTime endDate );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersByLocationAsync( int locationId );

        Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrdersByLocationAsync( int locationId );

        Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrdersByLocationAsync( int locationId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationIdsAsync( IEnumerable<int> locationIds );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync();

        Task<DatabaseResult<int>> GetOrderCountByLocationAsync( int locationId );

        Task<DatabaseResult<Dictionary<int, int>>> GetOrderCountsByLocationAsync();

        Task<DatabaseResult<decimal>> GetTotalRevenueByLocationAsync( int locationId );

        Task<DatabaseResult<Dictionary<OrderStatus, int>>> GetOrderStatusCountByLocationAsync( int locationId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int locationId = 0,
            DateTime? startDate = null,
            DateTime? endDate = null );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalOrderCountAsync();

        Task<DatabaseResult<int>> GetOrderCountByStatusAsync( OrderStatus status );

        Task<DatabaseResult<int>> GetOrderCountByTypeAsync( OrderType type );

        Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto );

        Task<DatabaseResult<OrderDto>> UpdateOrderAsync( UpdateOrderDto updateOrderDto );

        Task<DatabaseResult> TransferOrderToLocationAsync( int orderId, int newLocationId, string? reason = null );

        Task<DatabaseResult> RevertToDraftOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> ActivateOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> FulfillOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> CompleteOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> CancelOrderAsync( int orderId, OrderStatus originalStatus, string? reason = null );

        Task<DatabaseResult> DeleteOrderAsync( int orderId );

        Task<DatabaseResult<bool>> OrderExistsAsync( int orderId );

        Task<DatabaseResult<bool>> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false );

        Task<DatabaseResult<bool>> CustomerHasOrdersAsync( int customerId, bool activeOnly = false );

        OrderDto? GetOrderByIdInCache( int orderId );

        /// <summary>
        /// Searches cached orders by optional filters.
        /// </summary>
        IEnumerable<OrderDto> SearchOrdersInCache( OrderType? type = null, OrderStatus? status = null );

        /// <summary>
        /// Gets all cached orders with optional filtering.
        /// </summary>
        IEnumerable<OrderDto> GetAllOrdersInCache(
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int locationId = 0,
            int skip = 0,
            int take = 100 );

        IEnumerable<OrderDto> GetOrdersByTypeInCache( OrderType type );

        IEnumerable<OrderDto> GetOrdersByStatusInCache( OrderStatus status );

        IEnumerable<OrderDto> GetOrdersBySupplierInCache( int supplierId );

        IEnumerable<OrderDto> GetOrdersByCustomerInCache( int customerId );

        IEnumerable<OrderDto> GetOverdueOrdersInCache();

        IEnumerable<OrderDto> GetOrderByCreatedByInCache( int createdBy );

        IEnumerable<OrderDto> GetDraftOrdersInCache();

        IEnumerable<OrderDto> GetActiveOrdersInCache();

        IEnumerable<OrderDto> GetFulfilledOrdersInCache();

        IEnumerable<OrderDto> GetCompletedOrdersInCache();

        IEnumerable<OrderDto> GetCancelledOrdersInCache();

        bool OrderExistsInCache( int orderId );

        int GetTotalOrderCountInCache();

        int GetOrderCountByTypeInCache( OrderType type );

        int GetOrderCountByStatusInCache( OrderStatus status );

        bool SupplierHasOrdersInCache( int supplierId, bool activeOnly = false );

        bool CustomerHasOrdersInCache( int customerId, bool activeOnly = false );

        IEnumerable<OrderDto> GetOrdersByLocationInCache( int locationId );

        IEnumerable<OrderDto> GetOrdersByLocationAndStatusInCache( int locationId, OrderStatus orderStatus );

        IEnumerable<OrderDto> GetOrdersByLocationAndTypeInCache( int locationId, OrderType orderType );

        IEnumerable<OrderDto> GetActiveOrderByLocationInCache( int locationId );

        IEnumerable<OrderDto> GetSalesOrdersByLocationInCache( int locationId );

        IEnumerable<OrderDto> GetPurchaseOrdersByLocationInCache( int locationId );

        Task<DatabaseResult<bool>> LocationHasOrdersAsync( int locationId, bool activeOnly = false );

        Task<DatabaseResult<bool>> CanDeleteLocationAsync( int locationId );

        Task<DatabaseResult> ValidateLocationForOrderAsync( int locationId, OrderType orderType );

        Task<DatabaseResult> ValidateOrderTransferAsync( int orderId, int newLocationId );

        Task<DatabaseResult<bool>> CanTransferOrderToLocationAsync( int orderId, int newLocationId );

        void RefreshStoreCache();
    }
}
