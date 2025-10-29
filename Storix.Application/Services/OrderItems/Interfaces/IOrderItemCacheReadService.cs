using System.Collections.Generic;
using Storix.Application.DTO.OrderItems;

namespace Storix.Application.Services.OrderItems.Interfaces
{
    public interface IOrderItemCacheReadService
    {
        /// <summary>
        /// Retrieves an order item by its ID from the cache (fast).
        /// </summary>
        OrderItemDto? GetOrderItemByIdInCache( int orderItemId );

        /// <summary>
        /// Gets all order items belonging to a specific order from cache.
        /// </summary>
        IEnumerable<OrderItemDto> GetOrderItemsByOrderIdInCache( int orderId );

        /// <summary>
        /// Gets all order items for a specific product from cache.
        /// </summary>
        IEnumerable<OrderItemDto> GetOrderItemsByProductIdInCache( int productId );

        /// <summary>
        /// Checks if a specific OrderItem exists in cache.
        /// </summary>
        bool OrderItemExistsInCache( int orderItemId );

        /// <summary>
        /// Checks if an Order has any associated OrderItems in cache.
        /// </summary>
        bool OrderHasItemsInCache( int orderId );

        /// <summary>
        /// Checks if a Product is referenced in any OrderItems in cache.
        /// </summary>
        bool ProductExistsInOrdersCache( int productId );

        /// <summary>
        /// Checks if a Product exists in a specific Order in cache.
        /// </summary>
        bool ProductExistsInOrderCache( int orderId, int productId );

        /// <summary>
        /// Gets total number of OrderItems in cache.
        /// </summary>
        int GetOrderItemTotalCountInCache();

        /// <summary>
        /// Gets number of items within a specific order.
        /// </summary>
        int GetOrderItemCountInCache( int orderId );

        /// <summary>
        /// Gets total quantity of products in a specific order.
        /// </summary>
        int GetOrderTotalQuantityInCache( int orderId );

        /// <summary>
        /// Gets total monetary value of a specific order.
        /// </summary>
        decimal GetOrderTotalValueInCache( int orderId );

        /// <summary>
        /// Refreshes the OrderItem cache from the database (active items only).
        /// </summary>
        void RefreshStoreCache( int orderId );
    }
}
