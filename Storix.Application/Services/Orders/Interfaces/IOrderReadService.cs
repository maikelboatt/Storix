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

        Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrderListAsync();

        Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrderListAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByTypeAsync( OrderType type );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByStatusAsync( OrderStatus status );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersBySupplierAsync( int supplierId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync( int customerId );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync();

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy );

        Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync( string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null );

        Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalOrderCountsAsync();

        Task<DatabaseResult<int>> GetOrderCountsByTypeAsync( OrderType type );

        Task<DatabaseResult<int>> GetOrderCountByStatusAsync( OrderStatus status );

        Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate );
    }
}
