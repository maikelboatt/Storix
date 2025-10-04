using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.OrderItems;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Stores.OrderItems;
using Storix.Domain.Models;

namespace Storix.Application.Services.OrderItems
{
    /// <summary>
    /// Service for reading order item data.
    /// </summary>
    public class OrderItemReadService(
        IOrderItemRepository orderItemRepository,
        IOrderItemStore orderItemStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderItemReadService> logger ):IOrderItemReadService
    {
        public OrderItemDto? GetOrderItemById( int orderItemId )
        {
            if (orderItemId <= 0)
            {
                logger.LogWarning("Invalid order item ID {OrderItemId} provided", orderItemId);
                return null;
            }

            logger.LogDebug("Retrieving order item with ID {OrderItemId} from store", orderItemId);
            return orderItemStore.GetById(orderItemId);
        }

        public async Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderIdAsync( int orderId )
        {
            if (orderId <= 0)
            {
                logger.LogWarning("Invalid order ID {OrderId} provided for retrieving order items", orderId);
                return DatabaseResult<IEnumerable<OrderItemDto>>.Failure("Order Id must be positive", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<OrderItem>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetByOrderIdAsync(orderId),
                $"Retrieving order items for order {orderId}",
                false);

            if (result is { IsSuccess: true, Value: not null })
            {
                orderItemStore.Initialize(result.Value);

                logger.LogInformation("Successfully retrieved {Count} items for order {OrderId}", result.Value.Count(), orderId);
                IEnumerable<OrderItemDto> dtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<OrderItemDto>>.Success(dtos);
            }

            logger.LogWarning("Failed to retrieve order items for order {OrderId}: {ErrorMessage}", orderId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderItemDto>>.Failure(result.ErrorMessage ?? "Failed to retrieve order items", result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByProductIdAsync( int productId )
        {
            if (productId <= 0)
            {
                logger.LogWarning("Invalid product ID {ProductId} provided for retrieving order items", productId);
                return DatabaseResult<IEnumerable<OrderItemDto>>.Failure("Product Id must be positive", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<OrderItem>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetByProductIdAsync(productId),
                $"Retrieving order history for product {productId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {Count} items for product {ProductId}", result.Value.Count(), productId);
                IEnumerable<OrderItemDto> dtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<OrderItemDto>>.Success(dtos);
            }

            logger.LogWarning("Failed to retrieve order items for product {ProductId}: {ErrorMessage}", productId, result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderItemDto>>.Failure(result.ErrorMessage ?? "Failed to retrieve order items", result.ErrorCode);
        }
    }
}
