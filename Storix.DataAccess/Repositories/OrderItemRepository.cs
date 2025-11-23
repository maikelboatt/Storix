using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Storix.Application.Common;
using Storix.Application.DataAccess;
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
        ///     Checks if an order item exists by its ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int orderItemId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM OrderItem WHERE OrderItemId = @OrderItemId";
            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    OrderItemId = orderItemId
                });
        }

        /// <summary>
        ///     Checks if an order has any items.
        /// </summary>
        public async Task<bool> OrderHasItemsAsync( int orderId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM OrderItem WHERE OrderId = @OrderId";
            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        /// <summary>
        ///     Checks if a specific product exists in any order.
        /// </summary>
        public async Task<bool> ProductExistsInOrdersAsync( int productId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM OrderItem WHERE ProductId = @ProductId";
            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        /// <summary>
        ///     Checks if a specific product exists in a specific order.
        /// </summary>
        public async Task<bool> ProductExistsInOrderAsync( int orderId, int productId )
        {
            // language=tsql
            const string sql = @"
                SELECT COUNT(1) FROM OrderItem 
                WHERE OrderId = @OrderId AND ProductId = @ProductId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    OrderId = orderId,
                    ProductId = productId
                });
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets an order item by its ID.
        /// </summary>
        public async Task<OrderItem?> GetByIdAsync( int orderItemId )
        {
            // language=tsql
            const string sql = "SELECT * FROM OrderItem WHERE OrderItemId = @OrderItemId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<OrderItem>(
                sql,
                new
                {
                    OrderItemId = orderItemId
                });
        }

        /// <summary>
        ///     Gets all order items.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM OrderItem ORDER BY OrderId, OrderItemId";
            return await sqlDataAccess.QueryAsync<OrderItem>(sql);
        }

        /// <summary>
        ///     Gets all order items for a specific order by Order ID.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync( int orderId )
        {
            // language=tsql
            const string sql = "SELECT * FROM OrderItem WHERE OrderId = @OrderId ORDER BY OrderItemId";
            return await sqlDataAccess.QueryAsync<OrderItem>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        /// <summary>
        ///     Gets all order items for a specific product (order history for that product).
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetByProductIdAsync( int productId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM OrderItem 
                WHERE ProductId = @ProductId 
                ORDER BY OrderId DESC, OrderItemId";

            return await sqlDataAccess.QueryAsync<OrderItem>(
                sql,
                new
                {
                    ProductId = productId
                });
        }

        #endregion

        #region Statistics

        /// <summary>
        ///     Gets total count of order items in an order by the Order ID.
        /// </summary>
        public async Task<int> GetOrderItemsCountAsync( int orderId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM OrderItem WHERE OrderId = @OrderId";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        /// <summary>
        ///     Gets the total quantity of all items in an order by the Order ID.
        /// </summary>
        public async Task<int> GetOrderTotalQuantityAsync( int orderId )
        {
            // language=tsql
            const string sql = @"
                SELECT ISNULL(SUM(Quantity), 0) 
                FROM OrderItem 
                WHERE OrderId = @OrderId";

            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        /// <summary>
        ///     Gets the total price of all items in an order by the Order ID.
        /// </summary>
        public async Task<decimal> GetOrderTotalPriceAsync( int orderId )
        {
            // language=tsql
            const string sql = @"
                SELECT ISNULL(SUM(TotalPrice), 0) 
                FROM OrderItem 
                WHERE OrderId = @OrderId";

            return await sqlDataAccess.ExecuteScalarAsync<decimal>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new order item record.
        ///     Uses SQL Server SCOPE_IDENTITY() to retrieve the newly inserted ID.
        /// </summary>
        public async Task<OrderItem> CreateAsync( OrderItem orderItem )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice, TotalPrice)
                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @TotalPrice);
                
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int orderItemId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, orderItem);

            return orderItem with
            {
                OrderItemId = orderItemId
            };
        }

        /// <summary>
        ///     Updates an existing order item record.
        /// </summary>
        public async Task<OrderItem> UpdateAsync( OrderItem orderItem )
        {
            // language=tsql
            const string sql = @"
                UPDATE OrderItem 
                SET OrderId = @OrderId,
                    ProductId = @ProductId,
                    Quantity = @Quantity,
                    UnitPrice = @UnitPrice,
                    TotalPrice = @TotalPrice
                WHERE OrderItemId = @OrderItemId";

            await sqlDataAccess.ExecuteAsync(sql, orderItem);
            return orderItem;
        }

        /// <summary>
        ///     Deletes an existing order item record by its ID.
        /// </summary>
        public async Task<DatabaseResult> DeleteAsync( int orderItemId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM OrderItem WHERE OrderItemId = @OrderItemId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderItemId = orderItemId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Order item with ID {orderItemId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting order item: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        ///     Creates multiple order items in a single transaction.
        ///     Uses SQL Server SCOPE_IDENTITY() for each insert.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> CreateBulkAsync( IEnumerable<OrderItem> orderItems )
        {
            return await sqlDataAccess.ExecuteInTransactionAsync(async ( connection, transaction ) =>
            {
                // language=tsql
                const string sql = @"
                    INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice, TotalPrice)
                    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @TotalPrice);
                    
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                List<OrderItem> createdItems = new();

                foreach (OrderItem item in orderItems)
                {
                    int orderItemId = await connection.ExecuteScalarAsync<int>(
                        sql,
                        item,
                        transaction,
                        commandType: CommandType.Text);

                    createdItems.Add(
                        item with
                        {
                            OrderItemId = orderItemId
                        });
                }

                return createdItems.AsEnumerable();
            });
        }

        /// <summary>
        ///     Deletes all order items associated with a specific order by the Order ID.
        /// </summary>
        public async Task<DatabaseResult> DeleteByOrderIdAsync( int orderId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM OrderItem WHERE OrderId = @OrderId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderId = orderId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        "No order items found to delete",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting order items: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
