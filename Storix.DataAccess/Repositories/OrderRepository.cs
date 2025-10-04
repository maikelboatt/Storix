using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class OrderRepository( ISqlDataAccess sqlDataAccess ):IOrderRepository
    {
        #region Search & Filter

        public async Task<IEnumerable<Order>> SearchAsync( string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null )
        {
            var parameters = new
            {
                SearchTerm = searchTerm ?? "",
                Type = type,
                Status = status,
                SupplierId = supplierId,
                CustomerId = customerId,
                StartDate = startDate,
                EndDate = endDate
            };

            return await sqlDataAccess.QueryAsync<Order>("sp_SearchOrders", parameters);
        }

        #endregion

        #region Delete (Rarely Used - Orders Should Not Be Deleted)

        /// <summary>
        ///     Permanently deletes an order by ID.
        ///     WARNING: Orders should typically NOT be deleted. Use status changes instead.
        ///     This method should only be used for cleaning up test data or by administrators.
        /// </summary>
        public async Task<DatabaseResult> DeleteAsync( int orderId )
        {
            try
            {
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    "sp_DeleteOrder",
                    new
                    {
                        OrderId = orderId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure("No order found with the specified ID.", DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(ex.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Statistics & Reporting

        public async Task<OrderStatisticsDto?> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate ) =>
            await sqlDataAccess.QuerySingleOrDefaultAsync<OrderStatisticsDto>(
                "sp_GetOrderStatistics",
                new
                {
                    StartDate = startDate,
                    EndDate = endDate
                });

        /// <summary>
        ///     Gets the total value of orders by the status.
        /// </summary>
        public async Task<decimal> GetTotalValueByStatusAsync( OrderStatus status ) => await sqlDataAccess.ExecuteScalarAsync<decimal>(
            "sp_GetTotalValueByStatus",
            new
            {
                Status = status
            });

        /// <summary>
        ///     Gets the total value of orders by the type.
        /// </summary>
        public async Task<decimal> GetTotalValueByTypeAsync( OrderType type ) => await sqlDataAccess.ExecuteScalarAsync<decimal>(
            "sp_GetTotalValueByType",
            new
            {
                Type = type
            });

        #endregion

        #region Validation

        /// <summary>
        ///     Check if an order exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int orderId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckOrderExists",
                new
                {
                    Order = orderId
                });

            return count > 0;
        }

        /// <summary>
        ///     Checks if a supplier has any orders (active or historical).
        /// </summary>
        public async Task<bool> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false ) =>
            await EntityHasOrdersAsync(supplierId, "Supplier", activeOnly);

        /// <summary>
        ///     Checks if a customer has orders (active or historical).
        /// </summary>
        public async Task<bool> CustomerHasOrdersAsync( int customerId, bool activeOnly = false ) =>
            await EntityHasOrdersAsync(customerId, "Customer", activeOnly);

        /// <summary>
        ///     Checks if entity (customer or supplier) has orders (active or historical).
        /// </summary>
        private async Task<bool> EntityHasOrdersAsync( int orderId, string entityType, bool activeOnly = false )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckEntityHasOrders",
                new
                {
                    OrderId = orderId,
                    EntityType = entityType,
                    ActiveOnly = activeOnly
                });

            return count > 0;
        }

        /// <summary>
        ///     Checks if an order can be activated (only Draft orders can be activated).
        /// </summary>
        public async Task<bool> CanBeActivated( int orderId )
        {
            Order? order = await GetByIdAsync(orderId);

            return order is { Status: OrderStatus.Draft };
        }

        /// <summary>
        ///     Checks if an order can be cancelled (only Draft and Active orders can be cancelled).
        /// </summary>
        public async Task<bool> CanBeCancelled( int orderId )
        {
            Order? order = await GetByIdAsync(orderId);

            return order is { Status: OrderStatus.Draft or OrderStatus.Active };
        }

        /// <summary>
        ///     Checks if an order can be completed (only Active orders can be completed).
        /// </summary>
        /// <param name="orderId" ></param>
        /// <returns></returns>
        public async Task<bool> CanBeCompleted( int orderId )
        {
            Order? order = await GetByIdAsync(orderId);

            return order is { Status: OrderStatus.Active };
        }

        #endregion

        #region Pagination

        /// <summary>
        ///     Gets a paged list of orders.
        /// </summary>
        public async Task<IEnumerable<Order>> GetPagedAsync( int pageNumber, int pageSize )
        {
            var parameters = new
            {
                Page = pageNumber,
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize
            };

            return await sqlDataAccess.QueryAsync<Order>("sp_GetOrdersPaged", parameters);
        }

        /// <summary>
        ///     Gets the total counts of orders.
        /// </summary>
        public async Task<int> GetTotalCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetTotalCount");

        /// <summary>
        ///     Gets the counts of orders by type.
        /// </summary>
        public async Task<int> GetCountByTypeAsync( OrderType type ) => await sqlDataAccess.ExecuteScalarAsync<int>(
            "sp_GetCountByType",
            new
            {
                Type = type
            });

        /// <summary>
        ///     Gets the count of orders by status.
        /// </summary>
        public async Task<int> GetCountByStatusAsync( OrderStatus status ) => await sqlDataAccess.ExecuteScalarAsync<int>(
            "sp_GetCountByStatus",
            new
            {
                Status = status
            });

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets an order by its ID.
        /// </summary>
        public async Task<Order?> GetByIdAsync( int orderId ) => await sqlDataAccess.QuerySingleOrDefaultAsync<Order>(
            "sp_GetOrderById",
            new
            {
                OrderId = orderId
            });

        /// <summary>
        ///     Gets all order.
        /// </summary>
        public async Task<IEnumerable<Order>> GetAllAsync() => await sqlDataAccess.QueryAsync<Order>("sp_GetAllOrders");

        /// <summary>
        ///     Gets orders by Type (Purchase or Sale)
        /// </summary>
        public async Task<IEnumerable<Order>> GetByTypeAsync( OrderType type ) => await sqlDataAccess.QueryAsync<Order>(
            "sp_GetOrdersByType",
            new
            {
                Type = type
            });

        /// <summary>
        ///     Gets orders by Status (Draft, Active, Completed, Cancelled)
        /// </summary>
        public async Task<IEnumerable<Order>> GetByStatusAsync( OrderStatus status ) => await sqlDataAccess.QueryAsync<Order>(
            "sp_GetOrdersByStatus",
            new
            {
                Status = status
            });

        /// <summary>
        ///     Gets purchase orders by supplier ID.
        /// </summary>
        public async Task<IEnumerable<Order>> GetBySupplierAsync( int supplierId ) => await sqlDataAccess.QueryAsync<Order>(
            "sp_GetOrdersBySupplier",
            new
            {
                SupplierId = supplierId
            });

        /// <summary>
        ///     Gets sale orders by customer ID.
        /// </summary>
        public async Task<IEnumerable<Order>> GetByCustomerAsync( int customerId ) => await sqlDataAccess.QueryAsync<Order>(
            "sp_GetOrdersByCustomer",
            new
            {
                CustomerId = customerId
            });

        /// <summary>
        ///     Gets orders by date range.
        /// </summary>
        public async Task<IEnumerable<Order>> GetByDateRangeAsync( DateTime startDate, DateTime endDate ) => await sqlDataAccess.QueryAsync<Order>(
            "sp_GetOrdersByDateRange",
            new
            {
                StartDate = startDate,
                EndDate = endDate
            });

        /// <summary>
        ///     Gets overdue orders (DeliveryDate passed but status is still Draft or Active).
        /// </summary>
        public async Task<IEnumerable<Order>> GetOverdueOrdersAsync() => await sqlDataAccess.QueryAsync<Order>("sp_GetOverdueOrders");

        /// <summary>
        ///     Gets orders by created by a specific user.
        /// </summary>
        /// <param name="createdBy" ></param>
        /// <returns></returns>
        public async Task<IEnumerable<Order>> GetByCreatedByAsync( int createdBy ) => await sqlDataAccess.QueryAsync<Order>(
            "sp_GetOrdersByCreatedBy",
            new
            {
                CreatedById = createdBy
            });

        #endregion

        #region Create & Update

        /// <summary>
        ///     Creates a new order and returns it with its generated ID.
        /// </summary>
        public async Task<Order> CreateAsync( Order order )
        {
            var parameters = new
            {
                order.Type,
                order.Status,
                order.SupplierId,
                order.CustomerId,
                order.OrderDate,
                order.DeliveryDate,
                order.Notes,
                order.CreatedBy
            };

            int orderId = await sqlDataAccess.ExecuteScalarAsync<int>("sp_CreateOrder", parameters);

            return order with
            {
                OrderId = orderId
            };
        }


        /// <summary>
        ///     Updates an existing order (status, delivery date, and notes only).
        /// </summary>
        public async Task<Order> UpdateAsync( Order order )
        {
            var parameters = new
            {
                order.OrderId,
                order.Status,
                order.DeliveryDate,
                order.Notes
            };

            await sqlDataAccess.CommandAsync("sp_UpdateOrder", parameters);

            return order;
        }

        /// <summary>
        ///     Updates the status of an order.
        /// </summary>
        public async Task<DatabaseResult> UpdateStatusAsync( int orderId, OrderStatus status )
        {
            try
            {
                await sqlDataAccess.CommandAsync(
                    "sp_UpdateOrderStatus",
                    new
                    {
                        OrderId = orderId,
                        Status = status
                    });
                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(ex.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Activates an Order by setting its status from Draft to Active.
        /// </summary>
        public async Task<DatabaseResult> ActivateOrderAsync( int orderId )
        {
            try
            {
                await sqlDataAccess.CommandAsync(
                    "sp_ActivateOrder",
                    new
                    {
                        OrderId = orderId
                    });
                return DatabaseResult.Success();
            }
            catch (Exception e)
            {
                return DatabaseResult.Failure(e.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Complete an order by setting its status to completed.
        /// </summary>
        public async Task<DatabaseResult> CompleteOrderAsync( int orderId )
        {
            try
            {
                await sqlDataAccess.CommandAsync(
                    "sp_CompleteOrder",
                    new
                    {
                        OrderId = orderId
                    });
                return DatabaseResult.Success();
            }
            catch (Exception e)
            {
                return DatabaseResult.Failure(e.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Cancels an order by setting its status to cancelled.
        /// </summary>
        public async Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null )
        {
            try
            {
                await sqlDataAccess.CommandAsync(
                    "sp_CancelOrder",
                    new
                    {
                        OrderId = orderId,
                        Reason = reason
                    });
                return DatabaseResult.Success();
            }
            catch (Exception e)
            {
                return DatabaseResult.Failure(e.Message, DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
