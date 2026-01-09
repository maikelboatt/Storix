using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderReadService
    {
        OrderDto? GetOrderById( int orderId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetAllOrdersAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByTypeAsync( OrderType type );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByStatusAsync( OrderStatus status );

        Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrderListAsync();

        Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrderListAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersBySupplierAsync( int supplierId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync( int customerId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate );

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

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy );

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

        Task<DatabaseResult<int>> GetTotalOrderCountsAsync();

        Task<DatabaseResult<int>> GetOrderCountsByTypeAsync( OrderType type );

        Task<DatabaseResult<int>> GetOrderCountByStatusAsync( OrderStatus status );

        Task<DatabaseResult<int>> GetOrderCountByLocationAsync( int locationId );

        Task<DatabaseResult<Dictionary<int, int>>> GetOrderCountsByLocationAsync();

        Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<decimal>> GetTotalRevenueByLocationAsync( int locationId );

        Task<DatabaseResult<Dictionary<OrderStatus, int>>> GetOrderStatusCountByLocationAsync( int locationId );
    }
}
