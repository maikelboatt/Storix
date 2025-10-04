using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.OrderItems
{
    /// <summary>
    ///     Service responsible for order item validation operations.
    /// </summary>
    public class OrderItemValidationService(
        IOrderItemRepository orderItemRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderItemValidationService> logger ):IOrderItemValidationService
    {
        public async Task<DatabaseResult<bool>> OrderItemExistsAsync( int orderItemId )
        {
            if (orderItemId <= 0)
                return DatabaseResult<bool>.Success(false);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.ExistsAsync(orderItemId),
                $"Checking if order item {orderItemId} exists",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForCreation( int orderId, int productId )
        {
            // Check if order exists
            DatabaseResult<Order?> orderResult = await CheckOrderExists(orderId);

            if (!orderResult.IsSuccess || orderResult.Value == null)
            {
                logger.LogWarning("Attempted to add item to non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order with ID {orderId} not found.", DatabaseErrorCode.NotFound);
            }

            // Check if order is in Draft status
            if (orderResult.Value.Status != OrderStatus.Draft)
            {
                logger.LogWarning("Attempted to add item to order {OrderId} with status {Status}", orderId, orderResult.Value.Status);
                return DatabaseResult.Failure("Items can only be added to Draft orders.", DatabaseErrorCode.InvalidInput);
            }

            // Check if product exists
            DatabaseResult<bool> productExistsResult = await CheckProductExists(productId);

            if (!productExistsResult.IsSuccess || !productExistsResult.Value)
            {
                logger.LogWarning("Attempted to add non-existent product {ProductId} to order", productId);
                return DatabaseResult.Failure($"Product with ID {productId} not found.", DatabaseErrorCode.NotFound);
            }

            // Check if product already exists in this order
            DatabaseResult<bool> duplicateResult = await ProductExistsInOrder(orderId, productId);

            if (duplicateResult.IsSuccess && duplicateResult.Value)
            {
                logger.LogWarning("Attempted to add duplicate product {ProductId} to order {OrderId}", productId, orderId);
                return DatabaseResult.Failure("This product is already in the order. Update the existing item instead.", DatabaseErrorCode.DuplicateKey);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForUpdate( int orderItemId )
        {
            // Check if order item exists
            DatabaseResult<OrderItem?> itemResult = await OrderItemExists(orderItemId);

            if (!itemResult.IsSuccess || itemResult.Value == null)
            {
                logger.LogWarning("Attempted to update non-existent order item {OrderItemId}", orderItemId);
                return DatabaseResult.Failure($"Order item with ID {orderItemId} not found.", DatabaseErrorCode.NotFound);
            }

            // Check if parent order is still in Draft status
            DatabaseResult<Order?> orderResult = await OrderInDraftStatus(itemResult);

            if (!orderResult.IsSuccess || orderResult.Value == null)
            {
                return DatabaseResult.Failure("Parent order not found.", DatabaseErrorCode.NotFound);
            }

            if (orderResult.Value.Status != OrderStatus.Draft)
            {
                logger.LogWarning(
                    "Attempted to update item in order {OrderId} with status {Status}",
                    orderResult.Value.OrderId,
                    orderResult.Value.Status);
                return DatabaseResult.Failure("Items can only be updated in Draft orders.", DatabaseErrorCode.InvalidInput);
            }

            return DatabaseResult.Success();
        }


        public async Task<DatabaseResult> ValidateForDeletion( int orderItemId ) => await ValidateForUpdate(orderItemId); // Same rules as update

        public async Task<DatabaseResult<bool>> ProductExistsInOrdersAsync( int productId )
        {
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.ProductExistsInOrdersAsync(productId),
                $"Checking if product {productId} exists in orders",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }


        #region Private Helpers

        private async Task<DatabaseResult<bool>> ProductExistsInOrder( int orderId, int productId )
        {

            DatabaseResult<bool> duplicateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.ProductExistsInOrderAsync(orderId, productId),
                $"Checking for duplicate product in order",
                false);
            return duplicateResult;
        }

        private async Task<DatabaseResult<bool>> CheckProductExists( int productId )
        {

            DatabaseResult<bool> productExistsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => productRepository.ExistsAsync(productId, false),
                $"Checking product {productId} exists",
                false);
            return productExistsResult;
        }

        private async Task<DatabaseResult<Order?>> CheckOrderExists( int orderId )
        {

            DatabaseResult<Order?> orderResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByIdAsync(orderId),
                $"Checking order {orderId} exists",
                false);
            return orderResult;
        }


        private async Task<DatabaseResult<Order?>> OrderInDraftStatus( DatabaseResult<OrderItem?> itemResult )
        {

            DatabaseResult<Order?> orderResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByIdAsync(itemResult.Value!.OrderId),
                $"Checking order status",
                false);
            return orderResult;
        }

        private async Task<DatabaseResult<OrderItem?>> OrderItemExists( int orderItemId )
        {

            DatabaseResult<OrderItem?> itemResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetByIdAsync(orderItemId),
                $"Retrieving order item {orderItemId}",
                false);
            return itemResult;
        }

        #endregion
    }
}
