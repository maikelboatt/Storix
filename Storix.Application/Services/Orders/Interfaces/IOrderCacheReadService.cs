using System.Collections.Generic;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderCacheReadService
    {
        /// <summary>
        /// Retrieves an order by its ID from cache.
        /// </summary>
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

        IEnumerable<OrderDto> GetFulfilledOrdersInCache();

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
