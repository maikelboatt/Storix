using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
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

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalOrderCountAsync();

        Task<DatabaseResult<int>> GetOrderCountByStatusAsync( OrderStatus status );

        Task<DatabaseResult<int>> GetOrderCountByTypeAsync( OrderType type );

        Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto );

        Task<DatabaseResult<OrderDto>> UpdateOrderAsync( UpdateOrderDto updateOrderDto );

        Task<DatabaseResult> ActivateOrderAsync( int orderId );

        Task<DatabaseResult> CompleteOrderAsync( int orderId );

        Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null );

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

        IEnumerable<OrderDto> GetCompletedOrdersInCache();

        IEnumerable<OrderDto> GetCancelledOrdersInCache();

        bool OrderExistsInCache( int orderId );

        int GetTotalOrderCountInCache();

        int GetOrderCountByTypeInCache( OrderType type );

        int GetOrderCountByStatusInCache( OrderStatus status );

        bool SupplierHasOrdersInCache( int supplierId, bool activeOnly = false );

        bool CustomerHasOrdersInCache( int customerId, bool activeOnly = false );

        /// <summary>
        /// Refreshes the order cache from database asynchronously.
        /// </summary>
        void RefreshStoreCache();
    }
}
