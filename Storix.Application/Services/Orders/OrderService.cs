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

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAsync( int locationId ) =>
            await orderReadService.GetOrdersByLocationAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndStatusAsync( int locationId, OrderStatus status ) =>
            await orderReadService.GetOrdersByLocationAndStatusAsync(locationId, status);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndTypeAsync( int locationId, OrderType type ) =>
            await orderReadService.GetOrdersByLocationAndTypeAsync(locationId, type);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetActiveOrdersByLocationAsync( int locationId ) =>
            await orderReadService.GetActiveOrdersByLocationAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndDateRangeAsync( int locationId, DateTime startDate, DateTime endDate ) =>
            await orderReadService.GetOrdersByLocationAndDateRangeAsync(locationId, startDate, endDate);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersByLocationAsync( int locationId ) =>
            await orderReadService.GetOverdueOrdersByLocationAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrdersByLocationAsync( int locationId ) =>
            await orderReadService.GetSalesOrdersByLocationAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrdersByLocationAsync( int locationId ) =>
            await orderReadService.GetPurchaseOrdersByLocationAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationIdsAsync( IEnumerable<int> locationIds ) =>
            await orderReadService.GetOrdersByLocationIdsAsync(locationIds);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate ) =>
            await orderReadService.GetOrdersByDateRangeAsync(startDate, endDate);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy ) =>
            await orderReadService.GetOrderByCreatedByAsync(createdBy);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync() => await orderReadService.GetOverdueOrdersAsync();

        public async Task<DatabaseResult<int>> GetOrderCountByLocationAsync( int locationId ) =>
            await orderReadService.GetOrderCountByLocationAsync(locationId);

        public async Task<DatabaseResult<Dictionary<int, int>>> GetOrderCountsByLocationAsync() => await orderReadService.GetOrderCountsByLocationAsync();

        public async Task<DatabaseResult<decimal>> GetTotalRevenueByLocationAsync( int locationId ) =>
            await orderReadService.GetTotalRevenueByLocationAsync(locationId);

        public async Task<DatabaseResult<Dictionary<OrderStatus, int>>> GetOrderStatusCountByLocationAsync( int locationId ) =>
            await orderReadService.GetOrderStatusCountByLocationAsync(locationId);

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int locationId = 0,
            DateTime? startDate = null,
            DateTime? endDate = null ) => await orderReadService.SearchOrdersAsync(
            searchTerm,
            type,
            status,
            supplierId,
            customerId,
            locationId,
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

        public async Task<DatabaseResult> TransferOrderToLocationAsync( int orderId, int newLocationId, string? reason = null ) =>
            await orderWriteService.TransferOrderToLocationAsync(orderId, newLocationId, reason);

        public async Task<DatabaseResult> RevertToDraftOrderAsync( int orderId, OrderStatus originalStatus ) =>
            await orderWriteService.RevertToDraftOrderAsync(orderId, originalStatus);

        public async Task<DatabaseResult> ActivateOrderAsync( int orderId, OrderStatus originalStatus ) =>
            await orderWriteService.ActivateOrderAsync(orderId, originalStatus);

        public async Task<DatabaseResult> FulfillOrderAsync( int orderId, OrderStatus originalStatus ) =>
            await orderWriteService.FulfillOrderAsync(orderId, originalStatus);

        public async Task<DatabaseResult> CompleteOrderAsync( int orderId, OrderStatus originalStatus ) =>
            await orderWriteService.CompleteOrderAsync(orderId, originalStatus);

        public async Task<DatabaseResult> CancelOrderAsync( int orderId, OrderStatus originalStatus, string? reason = null ) =>
            await orderWriteService.CancelOrderAsync(orderId, originalStatus, reason);

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
            int locationId = 0,
            int skip = 0,
            int take = 100 ) => orderCacheReadService.GetAllOrdersInCache(
            type,
            status,
            supplierId,
            customerId,
            locationId,
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

        public IEnumerable<OrderDto> GetOrdersByLocationInCache( int locationId ) => orderCacheReadService.GetOrdersByLocationInCache(locationId);

        public IEnumerable<OrderDto> GetOrdersByLocationAndStatusInCache( int locationId, OrderStatus orderStatus ) =>
            orderCacheReadService.GetOrdersByLocationAndStatusInCache(locationId, orderStatus);

        public IEnumerable<OrderDto> GetOrdersByLocationAndTypeInCache( int locationId, OrderType orderType ) =>
            orderCacheReadService.GetOrdersByLocationAndTypeInCache(locationId, orderType);

        public IEnumerable<OrderDto> GetActiveOrderByLocationInCache( int locationId ) => orderCacheReadService.GetActiveOrderByLocationInCache(locationId);

        public IEnumerable<OrderDto> GetSalesOrdersByLocationInCache( int locationId ) => orderCacheReadService.GetSalesOrdersByLocationInCache(locationId);

        public IEnumerable<OrderDto> GetPurchaseOrdersByLocationInCache( int locationId ) =>
            orderCacheReadService.GetPurchaseOrdersByLocationInCache(locationId);

        public async Task<DatabaseResult<bool>> LocationHasOrdersAsync( int locationId, bool activeOnly = false ) =>
            await orderValidationService.LocationHasOrdersAsync(locationId, activeOnly);

        public async Task<DatabaseResult<bool>> CanDeleteLocationAsync( int locationId ) => await orderValidationService.CanDeleteLocationAsync(locationId);

        public async Task<DatabaseResult> ValidateLocationForOrderAsync( int locationId, OrderType orderType ) =>
            await orderValidationService.ValidateLocationForOrderAsync(locationId, orderType);

        public async Task<DatabaseResult> ValidateOrderTransferAsync( int orderId, int newLocationId ) =>
            await orderValidationService.ValidateOrderTransferAsync(orderId, newLocationId);

        public async Task<DatabaseResult<bool>> CanTransferOrderToLocationAsync( int orderId, int newLocationId ) =>
            await orderValidationService.CanTransferOrderToLocationAsync(orderId, newLocationId);

        public void RefreshStoreCache() => orderCacheReadService.RefreshStoreCache();

        #endregion
    }
}
