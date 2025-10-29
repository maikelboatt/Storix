using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Stores.OrderItems;
using Storix.Domain.Models;

namespace Storix.Application.Services.OrderItems
{
    public class OrderItemCacheReadService(
        IOrderItemStore orderItemStore,
        IOrderItemReadService orderItemReadService,
        ILogger<OrderItemCacheReadService> logger ):IOrderItemCacheReadService
    {
        /// <summary>
        /// Retrieves an order item by its ID from the cache (fast).
        /// </summary>
        public OrderItemDto? GetOrderItemByIdInCache( int orderItemId )
        {
            logger.LogInformation("Retrieving OrderItem with ID: {OrderItemId} from cache", orderItemId);
            return orderItemStore.GetById(orderItemId);
        }

        /// <summary>
        /// Gets all order items belonging to a specific order from cache.
        /// </summary>
        public IEnumerable<OrderItemDto> GetOrderItemsByOrderIdInCache( int orderId )
        {
            logger.LogInformation("Retrieving all OrderItems for Order ID: {OrderId} from cache", orderId);
            return orderItemStore.GetByOrderId(orderId);
        }

        /// <summary>
        /// Gets all order items for a specific product from cache.
        /// </summary>
        public IEnumerable<OrderItemDto> GetOrderItemsByProductIdInCache( int productId )
        {
            logger.LogInformation("Retrieving all OrderItems for Product ID: {ProductId} from cache", productId);
            return orderItemStore.GetByProductId(productId);
        }

        /// <summary>
        /// Checks if a specific OrderItem exists in cache.
        /// </summary>
        public bool OrderItemExistsInCache( int orderItemId ) => orderItemStore.Exists(orderItemId);

        /// <summary>
        /// Checks if an Order has any associated OrderItems in cache.
        /// </summary>
        public bool OrderHasItemsInCache( int orderId ) => orderItemStore.OrderHasItems(orderId);

        /// <summary>
        /// Checks if a Product is referenced in any OrderItems in cache.
        /// </summary>
        public bool ProductExistsInOrdersCache( int productId ) => orderItemStore.ProductExistsInOrders(productId);

        /// <summary>
        /// Checks if a Product exists in a specific Order in cache.
        /// </summary>
        public bool ProductExistsInOrderCache( int orderId, int productId ) => orderItemStore.ProductExistsInOrder(orderId, productId);

        /// <summary>
        /// Gets total number of OrderItems in cache.
        /// </summary>
        public int GetOrderItemTotalCountInCache() => orderItemStore.GetTotalCount();

        /// <summary>
        /// Gets number of items within a specific order.
        /// </summary>
        public int GetOrderItemCountInCache( int orderId ) => orderItemStore.GetOrderItemCount(orderId);

        /// <summary>
        /// Gets total quantity of products in a specific order.
        /// </summary>
        public int GetOrderTotalQuantityInCache( int orderId ) => orderItemStore.GetOrderTotalQuantity(orderId);

        /// <summary>
        /// Gets total monetary value of a specific order.
        /// </summary>
        public decimal GetOrderTotalValueInCache( int orderId ) => orderItemStore.GetOrderTotalValue(orderId);

        /// <summary>
        /// Refreshes the OrderItem cache from the database (active items only).
        /// </summary>
        public void RefreshStoreCache( int orderId )
        {
            logger.LogInformation("Initiating OrderItem store cache refresh");
            _ = Task.Run(async () =>
            {
                try
                {
                    DatabaseResult<IEnumerable<OrderItemDto>> result =
                        await orderItemReadService.GetOrderItemsByOrderIdAsync(orderId);

                    if (result is { IsSuccess: true, Value: not null })
                    {
                        logger.LogInformation(
                            "OrderItem store cache refreshed successfully with {Count} items",
                            result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh OrderItem store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing OrderItem store cache");
                }
            });
        }
    }
}
