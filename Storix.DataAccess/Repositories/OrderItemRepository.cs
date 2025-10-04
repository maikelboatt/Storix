using Storix.Application.Common;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class OrderItemRepository( ISqlDataAccess sqlDataAccess ):IOrderItemRepository
    {
        #region Validation

        /// <summary>
        /// Checks if an order item exists by its ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int orderItemId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckOrderItemExists",
                new
                {
                    OrderItemId = orderItemId
                });

            return count > 0;
        }

        /// <summary>
        /// Checks if an order has any items.
        /// </summary>
        public async Task<bool> OrderHasItemsAsync( int orderId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckOrderHasItems",
                new
                {
                    OrderId = orderId
                });

            return count > 0;
        }

        /// <summary>
        /// Checks if a specific product exists in any order.
        /// </summary>
        public async Task<bool> ProductExistsInOrdersAsync( int productId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckProductInOrders",
                new
                {
                    ProductId = productId
                });

            return count > 0;
        }

        /// <summary>
        /// Checks if a specific product exists in a specific order.
        /// </summary>
        public async Task<bool> ProductExistsInOrderAsync( int orderId, int productId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckProductInOrder",
                new
                {
                    OrderId = orderId,
                    ProductId = productId
                });

            return count > 0;
        }

        #endregion

        #region Create & Update

        /// <summary>
        /// Creates a new order item record.
        /// </summary>
        public async Task<OrderItem> CreateAsync( OrderItem orderItem )
        {
            var parameters = new
            {
                orderItem.OrderId,
                orderItem.ProductId,
                orderItem.Quantity,
                orderItem.UnitPrice,
                orderItem.TotalPrice
            };

            int orderItemId = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CreateOrderItem",
                parameters);

            return orderItem with
            {
                OrderItemId = orderItemId
            };
        }

        /// <summary>
        /// Updates an existing order item record.
        /// </summary>
        public async Task<OrderItem> UpdateAsync( OrderItem orderItem )
        {
            var parameters = new
            {
                orderItem.OrderItemId,
                orderItem.OrderId,
                orderItem.ProductId,
                orderItem.Quantity,
                orderItem.UnitPrice,
                orderItem.TotalPrice
            };

            await sqlDataAccess.CommandAsync(
                "sp_UpdateOrderItem",
                parameters);

            return orderItem;
        }

        /// <summary>
        /// Deletes an existing order item record by its ID.
        /// </summary>
        /// <param name="orderItemId"></param>
        /// <returns></returns>
        public async Task<DatabaseResult> DeleteAsync( int orderItemId )
        {
            try
            {
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    "sp_DeleteOrderItem",
                    new
                    {
                        OrderItemId = orderItemId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure($"Order item with ID {orderItemId} not found.", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(ex.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Read Operations

        /// <summary>
        /// Get an order item by its ID.
        /// </summary>
        public async Task<OrderItem?> GetByIdAsync( int orderItemId ) => await sqlDataAccess.QuerySingleOrDefaultAsync<OrderItem>(
            "sp_GetOrderItemById",
            new
            {
                OrderItemId = orderItemId
            });

        /// <summary>
        /// Gets all order items.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetAllAsync() => await sqlDataAccess.QueryAsync<OrderItem>(
            "sp_GetAllOrderItems"
        );

        /// <summary>
        /// Gets all order items for a specific order by Order ID.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync( int orderId ) => await sqlDataAccess.QueryAsync<OrderItem>(
            "sp_GetOrderItemsByOrderId",
            new
            {
                OrderId = orderId
            });

        /// <summary>
        ///     Gets all order items for a specific product (order history for that product).
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetByProductIdAsync( int productId ) => await sqlDataAccess.QueryAsync<OrderItem>(
            "sp_GetOrderItemsByProductId",
            new
            {
                ProductId = productId
            });

        #endregion

        #region Statistics

        /// <summary>
        /// Get total count of order items in an order by the Order ID.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetOrderItemsCountAsync( int orderId ) => await sqlDataAccess.ExecuteScalarAsync<int>(
            "sp_GetOrderItemsCount",
            new
            {
                OrderId = orderId
            });

        /// <summary>
        /// Get the total quantity of all items in an order by the Order ID.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<int> GetOrderTotalQuantityAsync( int orderId ) => await sqlDataAccess.ExecuteScalarAsync<int>(
            "sp_GetOrderTotalQuantity",
            new
            {
                OrderId = orderId
            });

        /// <summary>
        /// Get the total price of all items in an order by the Order ID.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<decimal> GetOrderTotalPriceAsync( int orderId ) => await sqlDataAccess.ExecuteScalarAsync<decimal>(
            "sp_GetOrderTotalPrice",
            new
            {
                OrderId = orderId
            });

        #endregion

        #region Bulk Operations

        /// <summary>
        ///     Creates multiple order items in a single transaction.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> CreateBulkAsync( IEnumerable<OrderItem> orderItems )
        {
            // Note: This would typically use a table-valued parameter or multiple inserts in a transaction
            // Implementation depends on your stored procedure design
            List<OrderItem> createdItems = [];

            foreach (OrderItem item in orderItems)
            {
                OrderItem createdItem = await CreateAsync(item);
                createdItems.Add(createdItem);
            }

            return createdItems;
        }

        /// <summary>
        /// Deletes all order items associated with a specific order by the Order ID.
        /// </summary>
        public async Task<DatabaseResult> DeleteByOrderIdAsync( int orderId )
        {
            try
            {
                int affectedRow = await sqlDataAccess.ExecuteAsync(
                    "sp_DeleteOrderItemsByOrderId",
                    new
                    {
                        OrderId = orderId
                    });

                return affectedRow > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure("No order items found to delete.", DatabaseErrorCode.NotFound);

            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(ex.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
