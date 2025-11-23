using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Storix.Application.Common;
using Storix.Application.DataAccess;
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
        #region Validation

        /// <summary>
        ///     Check if an order exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int orderId )
        {
            // language=tsql
            const string sql = "SELECT COUNT(1) FROM [Order] WHERE OrderId = @OrderId";
            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        /// <summary>
        ///     Checks if a supplier has any orders (active or historical).
        /// </summary>
        public async Task<bool> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false )
        {
            // language=tsql
            string sql = activeOnly
                ? @"SELECT COUNT(1) FROM [Order] 
                    WHERE SupplierId = @SupplierId 
                    AND Status IN (@Draft, @Active)"
                : "SELECT COUNT(1) FROM [Order] WHERE SupplierId = @SupplierId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    SupplierId = supplierId,
                    Draft = (int)OrderStatus.Draft,
                    Active = (int)OrderStatus.Active
                });
        }

        /// <summary>
        ///     Checks if a customer has orders (active or historical).
        /// </summary>
        public async Task<bool> CustomerHasOrdersAsync( int customerId, bool activeOnly = false )
        {
            // language=tsql
            string sql = activeOnly
                ? @"SELECT COUNT(1) FROM [Order] 
                    WHERE CustomerId = @CustomerId 
                    AND Status IN (@Draft, @Active)"
                : "SELECT COUNT(1) FROM [Order] WHERE CustomerId = @CustomerId";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    CustomerId = customerId,
                    Draft = (int)OrderStatus.Draft,
                    Active = (int)OrderStatus.Active
                });
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
        public async Task<bool> CanBeCompleted( int orderId )
        {
            Order? order = await GetByIdAsync(orderId);
            return order is { Status: OrderStatus.Active };
        }

        #endregion

        #region Count Operations

        /// <summary>
        ///     Gets the total count of orders.
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM [Order]";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of orders by type.
        /// </summary>
        public async Task<int> GetCountByTypeAsync( OrderType type )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM [Order] WHERE Type = @Type";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Type = (int)type
                });
        }

        /// <summary>
        ///     Gets the count of orders by status.
        /// </summary>
        public async Task<int> GetCountByStatusAsync( OrderStatus status )
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM [Order] WHERE Status = @Status";
            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Status = (int)status
                });
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets an order by its ID.
        /// </summary>
        public async Task<Order?> GetByIdAsync( int orderId )
        {
            // language=tsql
            const string sql = "SELECT * FROM [Order] WHERE OrderId = @OrderId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Order>(
                sql,
                new
                {
                    OrderId = orderId
                });
        }

        /// <summary>
        ///     Gets all orders.
        /// </summary>
        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM [Order] ORDER BY OrderDate DESC";
            return await sqlDataAccess.QueryAsync<Order>(sql);
        }

        /// <summary>
        ///     Gets orders by Type (Purchase or Sale).
        /// </summary>
        public async Task<IEnumerable<Order>> GetByTypeAsync( OrderType type )
        {
            // language=tsql
            const string sql = "SELECT * FROM [Order] WHERE Type = @Type ORDER BY OrderDate DESC";
            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    Type = (int)type
                });
        }

        /// <summary>
        ///     Gets orders by Status (Draft, Active, Completed, Cancelled).
        /// </summary>
        public async Task<IEnumerable<Order>> GetByStatusAsync( OrderStatus status )
        {
            // language=tsql
            const string sql = "SELECT * FROM [Order] WHERE Status = @Status ORDER BY OrderDate DESC";
            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    Status = (int)status
                });
        }

        /// <summary>
        ///     Gets purchase orders by supplier ID.
        /// </summary>
        public async Task<IEnumerable<Order>> GetBySupplierAsync( int supplierId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM [Order] 
                WHERE SupplierId = @SupplierId 
                AND Type = @PurchaseType
                ORDER BY OrderDate DESC";

            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    SupplierId = supplierId,
                    PurchaseType = (int)OrderType.Purchase
                });
        }

        /// <summary>
        ///     Gets sale orders by customer ID.
        /// </summary>
        public async Task<IEnumerable<Order>> GetByCustomerAsync( int customerId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM [Order] 
                WHERE CustomerId = @CustomerId 
                AND Type = @SaleType
                ORDER BY OrderDate DESC";

            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    CustomerId = customerId,
                    SaleType = (int)OrderType.Sale
                });
        }

        /// <summary>
        ///     Gets orders by date range.
        /// </summary>
        public async Task<IEnumerable<Order>> GetByDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM [Order] 
                WHERE OrderDate BETWEEN @StartDate AND @EndDate 
                ORDER BY OrderDate DESC";

            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    StartDate = startDate,
                    EndDate = endDate
                });
        }

        /// <summary>
        ///     Gets overdue orders (DeliveryDate passed but status is still Draft or Active).
        /// </summary>
        public async Task<IEnumerable<Order>> GetOverdueOrdersAsync()
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM [Order] 
                WHERE DeliveryDate < @CurrentDate 
                AND Status IN (@Draft, @Active)
                ORDER BY DeliveryDate";

            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    CurrentDate = DateTime.UtcNow.Date,
                    Draft = (int)OrderStatus.Draft,
                    Active = (int)OrderStatus.Active
                });
        }

        /// <summary>
        ///     Gets orders created by a specific user.
        /// </summary>
        public async Task<IEnumerable<Order>> GetByCreatedByAsync( int createdBy )
        {
            // language=tsql
            const string sql = "SELECT * FROM [Order] WHERE CreatedBy = @CreatedBy ORDER BY OrderDate DESC";
            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    CreatedBy = createdBy
                });
        }

        /// <summary>
        ///     Gets a paged list of orders.
        ///     Uses SQL Server OFFSET-FETCH syntax.
        /// </summary>
        public async Task<IEnumerable<Order>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM [Order] 
                ORDER BY OrderDate DESC 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<Order>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        #endregion

        #region Search & Filter

        /// <summary>
        ///     Searches orders with multiple optional filters.
        /// </summary>
        public async Task<IEnumerable<Order>> SearchAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null )
        {
            // language=tsql
            StringBuilder sql = new("SELECT * FROM [Order] WHERE 1=1");
            DynamicParameters parameters = new();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql.Append(" AND (Notes LIKE @SearchTerm)");
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            if (type.HasValue)
            {
                sql.Append(" AND Type = @Type");
                parameters.Add("Type", (int)type.Value);
            }

            if (status.HasValue)
            {
                sql.Append(" AND Status = @Status");
                parameters.Add("Status", (int)status.Value);
            }

            if (supplierId.HasValue)
            {
                sql.Append(" AND SupplierId = @SupplierId");
                parameters.Add("SupplierId", supplierId.Value);
            }

            if (customerId.HasValue)
            {
                sql.Append(" AND CustomerId = @CustomerId");
                parameters.Add("CustomerId", customerId.Value);
            }

            if (startDate.HasValue)
            {
                sql.Append(" AND OrderDate >= @StartDate");
                parameters.Add("StartDate", startDate.Value);
            }

            if (endDate.HasValue)
            {
                sql.Append(" AND OrderDate <= @EndDate");
                parameters.Add("EndDate", endDate.Value);
            }

            sql.Append(" ORDER BY OrderDate DESC");

            return await sqlDataAccess.QueryAsync<Order>(sql.ToString(), parameters);
        }

        #endregion

        #region Statistics & Reporting

        /// <summary>
        ///     Gets order statistics for a date range.
        /// </summary>
        public async Task<OrderStatisticsDto?> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate )
        {
            // language=tsql
            const string sql = @"
                SELECT 
                    COUNT(*) as TotalOrders,
                    COUNT(CASE WHEN Type = @Purchase THEN 1 END) as PurchaseOrders,
                    COUNT(CASE WHEN Type = @Sale THEN 1 END) as SaleOrders,
                    COUNT(CASE WHEN Status = @Completed THEN 1 END) as CompletedOrders,
                    COUNT(CASE WHEN Status = @Cancelled THEN 1 END) as CancelledOrders,
                    SUM(CASE WHEN Type = @Purchase AND Status = @Completed THEN TotalPrice ELSE 0 END) as TotalPurchaseValue,
                    SUM(CASE WHEN Type = @Sale AND Status = @Completed THEN TotalPrice ELSE 0 END) as TotalSaleValue
                FROM [Order]
                WHERE OrderDate BETWEEN @StartDate AND @EndDate";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<OrderStatisticsDto>(
                sql,
                new
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Purchase = (int)OrderType.Purchase,
                    Sale = (int)OrderType.Sale,
                    Completed = (int)OrderStatus.Completed,
                    Cancelled = (int)OrderStatus.Cancelled
                });
        }

        /// <summary>
        ///     Gets the total value of orders by status.
        /// </summary>
        public async Task<decimal> GetTotalValueByStatusAsync( OrderStatus status )
        {
            // language=tsql
            const string sql = @"
                SELECT ISNULL(SUM(TotalPrice), 0) 
                FROM [Order] 
                WHERE Status = @Status";

            return await sqlDataAccess.ExecuteScalarAsync<decimal>(
                sql,
                new
                {
                    Status = (int)status
                });
        }

        /// <summary>
        ///     Gets the total value of orders by type.
        /// </summary>
        public async Task<decimal> GetTotalValueByTypeAsync( OrderType type )
        {
            // language=tsql
            const string sql = @"
                SELECT ISNULL(SUM(TotalPrice), 0) 
                FROM [Order] 
                WHERE Type = @Type";

            return await sqlDataAccess.ExecuteScalarAsync<decimal>(
                sql,
                new
                {
                    Type = (int)type
                });
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new order and returns it with its generated ID.
        ///     Uses SQL Server SCOPE_IDENTITY() to retrieve the newly inserted ID.
        /// </summary>
        public async Task<Order> CreateAsync( Order order )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO [Order] (
                    Type, Status, SupplierId, CustomerId, 
                    OrderDate, DeliveryDate, Notes, CreatedBy
                )
                VALUES (
                    @Type, @Status, @SupplierId, @CustomerId,
                    @OrderDate, @DeliveryDate, @Notes, @CreatedBy
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int orderId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, order);

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
            // language=tsql
            const string sql = @"
                UPDATE [Order] 
                SET Status = @Status,
                    DeliveryDate = @DeliveryDate,
                    Notes = @Notes
                WHERE OrderId = @OrderId";

            await sqlDataAccess.ExecuteAsync(sql, order);
            return order;
        }

        /// <summary>
        ///     Updates the status of an order.
        /// </summary>
        public async Task<DatabaseResult> UpdateStatusAsync( int orderId, OrderStatus status )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE [Order] 
                    SET Status = @Status 
                    WHERE OrderId = @OrderId";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        Status = (int)status
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Order with ID {orderId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error updating order status: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Activates an order by setting its status from Draft to Active.
        /// </summary>
        public async Task<DatabaseResult> ActivateOrderAsync( int orderId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE [Order] 
                    SET Status = @Active 
                    WHERE OrderId = @OrderId AND Status = @Draft";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        Active = (int)OrderStatus.Active,
                        Draft = (int)OrderStatus.Draft
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Order with ID {orderId} not found or is not in Draft status",
                        DatabaseErrorCode.InvalidInput);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error activating order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Completes an order by setting its status to Completed.
        /// </summary>
        public async Task<DatabaseResult> CompleteOrderAsync( int orderId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE [Order] 
                    SET Status = @Completed 
                    WHERE OrderId = @OrderId AND Status = @Active";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        Completed = (int)OrderStatus.Completed,
                        Active = (int)OrderStatus.Active
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Order with ID {orderId} not found or is not in Active status",
                        DatabaseErrorCode.InvalidInput);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error completing order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Cancels an order by setting its status to Cancelled.
        ///     Uses SQL Server string concatenation with + operator and CHAR(13)+CHAR(10) for newline.
        /// </summary>
        public async Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE [Order] 
                    SET Status = @Cancelled,
                        Notes = CASE 
                            WHEN @Reason IS NOT NULL THEN ISNULL(Notes, '') + CHAR(13) + CHAR(10) + 'Cancellation reason: ' + @Reason
                            ELSE Notes 
                        END
                    WHERE OrderId = @OrderId 
                    AND Status IN (@Draft, @Active)";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        Cancelled = (int)OrderStatus.Cancelled,
                        Draft = (int)OrderStatus.Draft,
                        Active = (int)OrderStatus.Active,
                        Reason = reason
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Order with ID {orderId} not found or cannot be cancelled (must be Draft or Active)",
                        DatabaseErrorCode.InvalidInput);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error cancelling order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Permanently deletes an order by ID.
        ///     WARNING: Orders should typically NOT be deleted. Use status changes instead.
        ///     This method should only be used for cleaning up test data or by administrators.
        /// </summary>
        public async Task<DatabaseResult> DeleteAsync( int orderId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM [Order] WHERE OrderId = @OrderId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        OrderId = orderId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Order with ID {orderId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
