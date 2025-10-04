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

        IEnumerable<OrderDto> SearchOrders( OrderType? type = null, OrderStatus? status = null );

        void RefreshStoreCache();
    }
}
