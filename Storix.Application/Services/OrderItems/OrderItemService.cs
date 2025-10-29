using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.Repositories;
using Storix.Application.Services.OrderItems.Interfaces;

namespace Storix.Application.Services.OrderItems
{
    /// <summary>
    /// Basic service for single order item operations
    /// </summary>
    public class OrderItemService(
        IOrderItemReadService orderItemReadService,
        IOrderItemCacheReadService orderItemCacheReadService,
        IOrderItemWriteService orderItemWriteService,
        IOrderItemValidationService orderItemValidationService,
        ILogger<OrderItemService> logger ):IOrderItemService
    {
        #region Read Operations

        public OrderItemDto? GetOrderItemById( int orderItemId ) => orderItemReadService.GetOrderItemById(orderItemId);

        public async Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderIdAsync( int orderId ) =>
            await orderItemReadService.GetOrderItemsByOrderIdAsync(orderId);

        public async Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByProductIdAsync( int productId ) =>
            await orderItemReadService.GetOrderItemsByProductIdAsync(productId);

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<OrderItemDto>> CreateOrderItemAsync( CreateOrderItemDto createOrderItemDto ) =>
            await orderItemWriteService.CreateOrderItemAsync(createOrderItemDto);

        public async Task<DatabaseResult<OrderItemDto>> UpdateOrderItemAsync( UpdateOrderItemDto updateOrderItemDto ) =>
            await orderItemWriteService.UpdateOrderItemAsync(updateOrderItemDto);

        public async Task<DatabaseResult> DeleteOrderItemAsync( int orderItemId ) => await orderItemWriteService.DeleteOrderItemAsync(orderItemId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> OrderItemExistsAsync( int orderItemId ) => await orderItemValidationService.OrderItemExistsAsync(orderItemId);

        public async Task<DatabaseResult<bool>> ProductExistsInOrdersAsync( int productId ) =>
            await orderItemValidationService.ProductExistsInOrdersAsync(productId);

        #endregion

        #region Store Operations

        public OrderItemDto? GetOrderItemByIdInCache( int orderItemId ) => orderItemCacheReadService.GetOrderItemByIdInCache(orderItemId);

        public IEnumerable<OrderItemDto> GetOrderItemsByOrderIdInCache( int orderId ) => orderItemCacheReadService.GetOrderItemsByOrderIdInCache(orderId);

        public IEnumerable<OrderItemDto> GetOrderItemsByProductIdInCache( int productId ) =>
            orderItemCacheReadService.GetOrderItemsByProductIdInCache(productId);

        public bool OrderItemExistsInCache( int orderItemId ) => orderItemCacheReadService.OrderItemExistsInCache(orderItemId);

        public bool OrderHasItemsInCache( int orderId ) => orderItemCacheReadService.OrderHasItemsInCache(orderId);

        public bool ProductExistsInOrdersCache( int productId ) => orderItemCacheReadService.ProductExistsInOrdersCache(productId);

        public bool ProductExistsInOrderCache( int orderId, int productId ) => orderItemCacheReadService.ProductExistsInOrderCache(productId, orderId);

        public int GetOrderItemTotalCountInCache() => orderItemCacheReadService.GetOrderItemTotalCountInCache();

        public int GetOrderItemCountInCache( int orderId ) => orderItemCacheReadService.GetOrderItemCountInCache(orderId);

        public int GetOrderTotalQuantityInCache( int orderId ) => orderItemCacheReadService.GetOrderTotalQuantityInCache(orderId);

        public decimal GetOrderTotalValueInCache( int orderId ) => orderItemCacheReadService.GetOrderTotalValueInCache(orderId);

        public void RefreshStoreCache( int orderId ) => orderItemCacheReadService.RefreshStoreCache(orderId);

        #endregion
    }
}
