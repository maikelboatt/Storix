using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Repositories;
using Storix.Application.Services.OrderItems.Interfaces;

namespace Storix.Application.Managers
{
    /// <summary>
    ///     Manager for complex order item operations and business logic.
    /// </summary>
    public class OrderItemManager(
        IOrderItemRepository orderItemRepository,
        IOrderItemService orderItemService,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderItemManager> logger ):IOrderItemManager
    {
        public async Task<DatabaseResult<IEnumerable<OrderItemDto>>> CreateBulkOrderItemsAsync(
            IEnumerable<CreateOrderItemDto> createOrderItemDtos )
        {
            List<OrderItemDto> createdItems = [];
            List<string> errors = [];

            foreach (CreateOrderItemDto dto in createOrderItemDtos)
            {
                DatabaseResult<OrderItemDto> result = await orderItemService.CreateOrderItemAsync(dto);
                if (result is { IsSuccess: true, Value: not null })
                {
                    createdItems.Add(result.Value);
                }
                else
                {
                    errors.Add($"Product {dto.ProductId}: {result.ErrorMessage}");
                }
            }

            if (errors.Count != 0)
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk creation completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<OrderItemDto>>.Failure(
                    $"Bulk creation completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Successfully created {ItemCount} order items", createdItems.Count);
            return DatabaseResult<IEnumerable<OrderItemDto>>.Success(createdItems);
        }

        public async Task<DatabaseResult> DeleteOrderItemsByOrderIdAsync( int orderId )
        {
            if (orderId <= 0)
            {
                logger.LogWarning("Invalid order ID {OrderId} for bulk deletion", orderId);
                return DatabaseResult.Failure("Order ID must be positive.", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<DatabaseResult> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.DeleteByOrderIdAsync(orderId),
                $"Deleting all items for order {orderId}",
                false);

            if (result.IsSuccess)
            {
                logger.LogInformation("Successfully deleted all items for order {OrderId}", orderId);
                return DatabaseResult.Success();
            }


            logger.LogWarning("Failed to delete items for order {OrderId}: {ErrorMessage}", orderId, result.ErrorMessage);
            return DatabaseResult.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetOrderItemCountAsync( int orderId )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetOrderItemsCountAsync(orderId),
                $"Getting item count for order {orderId}",
                false);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetOrderTotalQuantityAsync( int orderId )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetOrderTotalQuantityAsync(orderId),
                $"Getting total quantity for order {orderId}",
                false);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<decimal>> GetOrderTotalValueAsync( int orderId )
        {
            DatabaseResult<decimal> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetOrderTotalPriceAsync(orderId),
                $"Getting total value for order {orderId}",
                false);

            return result.IsSuccess
                ? DatabaseResult<decimal>.Success(result.Value)
                : DatabaseResult<decimal>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<OrderSummaryDto>> GetOrderSummaryAsync( int orderId )
        {
            Task<DatabaseResult<int>> countTask = GetOrderItemCountAsync(orderId);
            Task<DatabaseResult<int>> quantityTask = GetOrderTotalQuantityAsync(orderId);
            Task<DatabaseResult<decimal>> valueTask = GetOrderTotalValueAsync(orderId);

            await Task.WhenAll(countTask, quantityTask, valueTask);

            if (!countTask.Result.IsSuccess || !quantityTask.Result.IsSuccess || !valueTask.Result.IsSuccess)
            {
                return DatabaseResult<OrderSummaryDto>.Failure(
                    "Failed to retrieve order summary.",
                    DatabaseErrorCode.UnexpectedError);
            }

            OrderSummaryDto summary = new()
            {
                OrderId = orderId,
                ItemCount = countTask.Result.Value,
                TotalQuantity = quantityTask.Result.Value,
                TotalValue = valueTask.Result.Value
            };

            return DatabaseResult<OrderSummaryDto>.Success(summary);
        }
    }
}
