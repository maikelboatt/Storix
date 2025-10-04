using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IOrderItemRepository
    {
        /// <summary>
        /// Checks if an order item exists by its ID.
        /// </summary>
        Task<bool> ExistsAsync( int orderItemId );

        /// <summary>
        /// Checks if an order has any items.
        /// </summary>
        Task<bool> OrderHasItemsAsync( int orderId );

        /// <summary>
        /// Checks if a specific product exists in any order.
        /// </summary>
        Task<bool> ProductExistsInOrdersAsync( int productId );

        /// <summary>
        /// Checks if a specific product exists in a specific order.
        /// </summary>
        Task<bool> ProductExistsInOrderAsync( int orderId, int productId );

        /// <summary>
        /// Creates a new order item record.
        /// </summary>
        Task<OrderItem> CreateAsync( OrderItem orderItem );

        /// <summary>
        /// Updates an existing order item record.
        /// </summary>
        Task<OrderItem> UpdateAsync( OrderItem orderItem );

        /// <summary>
        /// Deletes an existing order item record by its ID.
        /// </summary>
        /// <param name="orderItemId"></param>
        /// <returns></returns>
        Task<DatabaseResult> DeleteAsync( int orderItemId );

        /// <summary>
        /// Get an order item by its ID.
        /// </summary>
        Task<OrderItem?> GetByIdAsync( int orderItemId );

        /// <summary>
        /// Gets all order items.
        /// </summary>
        Task<IEnumerable<OrderItem>> GetAllAsync();

        /// <summary>
        /// Gets all order items for a specific order by Order ID.
        /// </summary>
        Task<IEnumerable<OrderItem>> GetByOrderIdAsync( int orderId );

        /// <summary>
        /// Gets all order items for a specific product by Product ID (order history for that product).
        /// </summary>
        Task<IEnumerable<OrderItem>> GetByProductIdAsync( int productId );

        /// <summary>
        /// Get total count of order items in an order by the Order ID.
        /// </summary>
        /// <returns></returns>
        Task<int> GetOrderItemsCountAsync( int orderId );

        /// <summary>
        /// Get the total quantity of all items in an order by the Order ID.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        Task<int> GetOrderTotalQuantityAsync( int orderId );

        /// <summary>
        /// Get the total price of all items in an order by the Order ID.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        Task<decimal> GetOrderTotalPriceAsync( int orderId );

        /// <summary>
        ///     Creates multiple order items in a single transaction.
        /// </summary>
        Task<IEnumerable<OrderItem>> CreateBulkAsync( IEnumerable<OrderItem> orderItems );

        /// <summary>
        /// Deletes all order items associated with a specific order by the Order ID.
        /// </summary>
        Task<DatabaseResult> DeleteByOrderIdAsync( int orderId );
    }
}
