using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    ///     Main service for managing order operations.
    /// </summary>
    public class OrderService(
        IOrderReadService orderReadService,
        IOrderItemManager orderItemManager,
        IOrderCacheReadService orderCacheReadService,
        IOrderWriteService orderWriteService,
        IOrderValidationService orderValidationService,
        IOrderStore orderStore,
        ILogger<OrderService> logger ):IOrderService
    {
        #region Read Operations

        public OrderDto? GetOrderById( int orderId ) => orderReadService.GetOrderById(orderId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetAllOrdersAsync() => await orderReadService.GetAllOrdersAsync();

        public async Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrderListAsync() => await orderReadService.GetSalesOrderListAsync();

        public async Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrderListAsync() => await orderReadService.GetPurchaseOrderListAsync();

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByTypeAsync( OrderType type ) => await orderReadService.GetOrdersByTypeAsync(type);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByStatusAsync( OrderStatus status ) =>
            await orderReadService.GetOrdersByStatusAsync(status);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersBySupplierAsync( int supplierId ) =>
            await orderReadService.GetOrdersBySupplierAsync(supplierId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync( int customerId ) =>
            await orderReadService.GetOrdersByCustomerAsync(customerId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate ) =>
            await orderReadService.GetOrdersByDateRangeAsync(startDate, endDate);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy ) =>
            await orderReadService.GetOrderByCreatedByAsync(createdBy);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync() => await orderReadService.GetOverdueOrdersAsync();

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null ) => await orderReadService.SearchOrdersAsync(
            searchTerm,
            type,
            status,
            supplierId,
            customerId,
            startDate,
            endDate);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersPagedAsync( int pageNumber, int pageSize ) =>
            await orderReadService.GetOrdersPagedAsync(pageNumber, pageSize);

        public async Task<DatabaseResult<int>> GetTotalOrderCountAsync() => await orderReadService.GetTotalOrderCountsAsync();

        public async Task<DatabaseResult<int>> GetOrderCountByStatusAsync( OrderStatus status ) => await orderReadService.GetOrderCountByStatusAsync(status);

        public async Task<DatabaseResult<int>> GetOrderCountByTypeAsync( OrderType type ) => await orderReadService.GetOrderCountsByTypeAsync(type);

        public async Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate ) =>
            await orderReadService.GetOrderStatisticsAsync(startDate, endDate);

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto ) =>
            await orderWriteService.CreateOrderAsync(createOrderDto);

        public async Task<DatabaseResult<OrderDto>> UpdateOrderAsync( UpdateOrderDto updateOrderDto ) =>
            await orderWriteService.UpdateOrderAsync(updateOrderDto);

        public async Task<DatabaseResult> ActivateOrderAsync( int orderId ) => await orderWriteService.ActivateOrderAsync(orderId);

        public async Task<DatabaseResult> FulfillOrderAsync( int orderId ) => await orderWriteService.FulfillOrderAsync(orderId);

        public async Task<DatabaseResult> CompleteOrderAsync( int orderId ) => await orderWriteService.CompleteOrderAsync(orderId);

        public async Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null ) => await orderWriteService.CancelOrderAsync(orderId, reason);

        public async Task<DatabaseResult> DeleteOrderAsync( int orderId ) => await orderWriteService.DeleteOrderAsync(orderId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> OrderExistsAsync( int orderId ) => await orderValidationService.OrderExistsAsync(orderId);

        public async Task<DatabaseResult<bool>> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false ) =>
            await orderValidationService.SupplierHasOrdersAsync(supplierId, activeOnly);

        public async Task<DatabaseResult<bool>> CustomerHasOrdersAsync( int customerId, bool activeOnly = false ) =>
            await orderValidationService.CustomerHasOrdersAsync(customerId, activeOnly);

        #endregion

        #region Store Operations (Cache Reads)

        public IEnumerable<OrderDto> SearchOrdersInCache( OrderType? type = null, OrderStatus? status = null ) =>
            orderCacheReadService.SearchOrdersInCache(type, status);

        public OrderDto? GetOrderByIdInCache( int orderId ) => orderCacheReadService.GetOrderByIdInCache(orderId);

        public IEnumerable<OrderDto> GetAllOrdersInCache(
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int skip = 0,
            int take = 100 ) => orderCacheReadService.GetAllOrdersInCache(
            type,
            status,
            supplierId,
            customerId,
            skip,
            take);

        public IEnumerable<OrderDto> GetOrdersByTypeInCache( OrderType type ) => orderCacheReadService.GetOrdersByTypeInCache(type);

        public IEnumerable<OrderDto> GetOrdersByStatusInCache( OrderStatus status ) => orderCacheReadService.GetOrdersByStatusInCache(status);

        public IEnumerable<OrderDto> GetOrdersBySupplierInCache( int supplierId ) => orderCacheReadService.GetOrdersBySupplierInCache(supplierId);

        public IEnumerable<OrderDto> GetOrdersByCustomerInCache( int customerId ) => orderCacheReadService.GetOrdersByCustomerInCache(customerId);

        public IEnumerable<OrderDto> GetOverdueOrdersInCache() => orderCacheReadService.GetOverdueOrdersInCache();

        public IEnumerable<OrderDto> GetOrderByCreatedByInCache( int createdBy ) => orderCacheReadService.GetOrderByCreatedByInCache(createdBy);

        public IEnumerable<OrderDto> GetDraftOrdersInCache() => orderCacheReadService.GetDraftOrdersInCache();

        public IEnumerable<OrderDto> GetActiveOrdersInCache() => orderCacheReadService.GetActiveOrdersInCache();

        public IEnumerable<OrderDto> GetFulfilledOrdersInCache() => orderCacheReadService.GetFulfilledOrdersInCache();

        public IEnumerable<OrderDto> GetCompletedOrdersInCache() => orderCacheReadService.GetCompletedOrdersInCache();

        public IEnumerable<OrderDto> GetCancelledOrdersInCache() => orderCacheReadService.GetCancelledOrdersInCache();

        public bool OrderExistsInCache( int orderId ) => orderCacheReadService.OrderExistsInCache(orderId);

        public int GetTotalOrderCountInCache() => orderCacheReadService.GetTotalOrderCountInCache();

        public int GetOrderCountByTypeInCache( OrderType type ) => orderCacheReadService.GetOrderCountByTypeInCache(type);

        public int GetOrderCountByStatusInCache( OrderStatus status ) => orderCacheReadService.GetOrderCountByStatusInCache(status);

        public bool SupplierHasOrdersInCache( int supplierId, bool activeOnly = false ) =>
            orderCacheReadService.SupplierHasOrdersInCache(supplierId, activeOnly);

        public bool CustomerHasOrdersInCache( int customerId, bool activeOnly = false ) =>
            orderCacheReadService.CustomerHasOrdersInCache(customerId, activeOnly);

        public void RefreshStoreCache() => orderCacheReadService.RefreshStoreCache();

        #endregion
    }
}
