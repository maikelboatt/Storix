using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DataAccess;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Domain.Models;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    /// Coordinator service for complex order operations requiring transactions
    /// </summary>
    public class OrderCoordinatorService:IOrderCoordinatorService
    {
        private readonly ISqlDataAccess _sqlDataAccess;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IOrderStore _orderStore;
        private readonly ILogger<OrderCoordinatorService> _logger;

        public OrderCoordinatorService(
            ISqlDataAccess sqlDataAccess,
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            IOrderStore orderStore,
            ILogger<OrderCoordinatorService> logger )
        {
            _sqlDataAccess = sqlDataAccess;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _orderStore = orderStore;
            _logger = logger;
        }

        /// <summary>
        /// Updates an order and its items within a single transaction
        /// Uses smart merge strategy to minimize database operations
        /// </summary>
        public async Task<DatabaseResult<OrderDto>> UpdateOrderWithItemsAsync(
            UpdateOrderDto updateOrderDto,
            IEnumerable<OrderItemUpdateDto> orderItems )
        {
            using IDbConnection connection = await _sqlDataAccess.GetOpenConnectionAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                // 1. Update Order Header
                DatabaseResult<OrderDto> orderUpdateResult = await UpdateOrderInTransactionAsync(
                    updateOrderDto,
                    connection,
                    transaction);

                if (!orderUpdateResult.IsSuccess)
                {
                    transaction.Rollback();
                    return orderUpdateResult;
                }

                // 2. Get existing order items
                List<OrderItem> existingItems = await GetOrderItemsInTransactionAsync(
                    updateOrderDto.OrderId,
                    connection,
                    transaction);

                // 3. Merge order items (smart diff-based update)
                DatabaseResult mergeResult = await MergeOrderItemsAsync(
                    updateOrderDto.OrderId,
                    existingItems,
                    orderItems,
                    connection,
                    transaction);

                if (!mergeResult.IsSuccess)
                {
                    transaction.Rollback();
                    return DatabaseResult<OrderDto>.Failure(
                        mergeResult.ErrorMessage!,
                        mergeResult.ErrorCode);
                }

                // 4. Update order store
                _orderStore.Update(updateOrderDto);

                // Commit transaction
                transaction.Commit();

                _logger.LogInformation(
                    "Successfully updated order {OrderId} with {ItemCount} items",
                    updateOrderDto.OrderId,
                    orderItems.Count());

                return orderUpdateResult;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Failed to update order {OrderId} with items", updateOrderDto.OrderId);
                return DatabaseResult<OrderDto>.Failure(
                    $"Transaction failed: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        /// Smart merge strategy: Updates, Inserts, and Deletes only what changed
        /// Much faster than delete-all-and-recreate for large orders
        /// </summary>
        private async Task<DatabaseResult> MergeOrderItemsAsync(
            int orderId,
            List<OrderItem> existingItems,
            IEnumerable<OrderItemUpdateDto> newItems,
            IDbConnection connection,
            IDbTransaction transaction )
        {
            List<OrderItemUpdateDto> newItemsList = newItems.ToList();
            List<string> errors = new();

            try
            {
                // A. UPDATE existing items that changed
                var itemsToUpdate = newItemsList
                                    .Where(n => n.OrderItemId.HasValue)
                                    .Select(n => new
                                    {
                                        NewItem = n,
                                        ExistingItem = existingItems.FirstOrDefault(e => e.OrderItemId == n.OrderItemId)
                                    })
                                    .Where(x => x.ExistingItem != null && HasItemChanged(x.ExistingItem, x.NewItem))
                                    .ToList();

                if (itemsToUpdate.Any())
                {
                    foreach (var item in itemsToUpdate)
                    {
                        const string updateSql = @"
                            UPDATE OrderItem 
                            SET ProductId = @ProductId,
                                Quantity = @Quantity,
                                UnitPrice = @UnitPrice,
                                TotalPrice = @TotalPrice
                            WHERE OrderItemId = @OrderItemId";

                        await connection.ExecuteAsync(
                            updateSql,
                            new
                            {
                                item.NewItem.OrderItemId,
                                item.NewItem.ProductId,
                                item.NewItem.Quantity,
                                item.NewItem.UnitPrice,
                                item.NewItem.TotalPrice
                            },
                            transaction);
                    }

                    _logger.LogDebug("Updated {Count} order items", itemsToUpdate.Count);
                }

                // B. INSERT new items (items without OrderItemId)
                List<OrderItemUpdateDto> itemsToInsert = newItemsList
                                                         .Where(n => !n.OrderItemId.HasValue)
                                                         .ToList();

                if (itemsToInsert.Any())
                {
                    const string insertSql = @"
                        INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice, TotalPrice)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @TotalPrice)";

                    foreach (OrderItemUpdateDto item in itemsToInsert)
                    {
                        await connection.ExecuteAsync(
                            insertSql,
                            new
                            {
                                OrderId = orderId,
                                item.ProductId,
                                item.Quantity,
                                item.UnitPrice,
                                item.TotalPrice
                            },
                            transaction);
                    }

                    _logger.LogDebug("Inserted {Count} new order items", itemsToInsert.Count);
                }

                // C. DELETE items that were removed
                HashSet<int> existingIds = existingItems
                                           .Select(e => e.OrderItemId)
                                           .ToHashSet();
                HashSet<int> newIds = newItemsList
                                      .Where(n => n.OrderItemId.HasValue)
                                      .Select(n => n.OrderItemId!.Value)
                                      .ToHashSet();
                List<int> idsToDelete = existingIds
                                        .Except(newIds)
                                        .ToList();

                if (idsToDelete.Any())
                {
                    const string deleteSql = @"
                        DELETE FROM OrderItem 
                        WHERE OrderItemId IN @Ids";

                    await connection.ExecuteAsync(
                        deleteSql,
                        new
                        {
                            Ids = idsToDelete
                        },
                        transaction);

                    _logger.LogDebug("Deleted {Count} order items", idsToDelete.Count);
                }

                _logger.LogInformation(
                    "Merged order items: {Updated} updated, {Inserted} inserted, {Deleted} deleted",
                    itemsToUpdate.Count,
                    itemsToInsert.Count,
                    idsToDelete.Count);

                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to merge order items");
                return DatabaseResult.Failure(
                    $"Failed to merge order items: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        private bool HasItemChanged( OrderItem existing, OrderItemUpdateDto updated ) => existing.ProductId != updated.ProductId ||
                                                                                         existing.Quantity != updated.Quantity ||
                                                                                         existing.UnitPrice != updated.UnitPrice ||
                                                                                         existing.TotalPrice != updated.TotalPrice;

        private async Task<DatabaseResult<OrderDto>> UpdateOrderInTransactionAsync(
            UpdateOrderDto updateOrderDto,
            IDbConnection connection,
            IDbTransaction transaction )
        {
            try
            {
                // Get existing order
                const string getSql = "SELECT * FROM [Order] WHERE OrderId = @OrderId";
                Order? existingOrder = await connection.QuerySingleOrDefaultAsync<Order>(
                    getSql,
                    new
                    {
                        updateOrderDto.OrderId
                    },
                    transaction);

                if (existingOrder == null)
                {
                    return DatabaseResult<OrderDto>.Failure(
                        "Order not found",
                        DatabaseErrorCode.NotFound);
                }

                // Update order
                const string updateSql = @"
                    UPDATE [Order]
                    SET Status = @Status,
                        DeliveryDate = @DeliveryDate,
                        Notes = @Notes
                    WHERE OrderId = @OrderId";

                await connection.ExecuteAsync(
                    updateSql,
                    new
                    {
                        updateOrderDto.OrderId,
                        updateOrderDto.Status,
                        updateOrderDto.DeliveryDate,
                        updateOrderDto.Notes
                    },
                    transaction);

                // Return updated order DTO
                Order updatedOrder = existingOrder with
                {
                    Status = updateOrderDto.Status,
                    DeliveryDate = updateOrderDto.DeliveryDate,
                    Notes = updateOrderDto.Notes
                };

                return DatabaseResult<OrderDto>.Success(updatedOrder.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order in transaction");
                return DatabaseResult<OrderDto>.Failure(
                    $"Failed to update order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        private async Task<List<OrderItem>> GetOrderItemsInTransactionAsync(
            int orderId,
            IDbConnection connection,
            IDbTransaction transaction )
        {
            const string sql = "SELECT * FROM OrderItem WHERE OrderId = @OrderId";
            IEnumerable<OrderItem> items = await connection.QueryAsync<OrderItem>(
                sql,
                new
                {
                    OrderId = orderId
                },
                transaction);
            return items.ToList();
        }
    }
}
