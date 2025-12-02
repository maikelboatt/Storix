using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    /// Service responsible for write operations on orders.
    /// </summary>
    public class OrderWriteService(
        IOrderRepository orderRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        IOrderValidationService orderValidationService,
        IOrderStore orderStore,
        IValidator<CreateOrderDto> createValidation,
        IValidator<UpdateOrderDto> updateValidation,
        ILogger<OrderWriteService> logger ):IOrderWriteService
    {
        public async Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto )
        {
            // Input validation
            DatabaseResult<OrderDto> inputValidation = ValidateCreateInput(createOrderDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<OrderDto> businessValidation = ValidateCreateBusiness(createOrderDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform Create
            return await PerformCreateAsync(createOrderDto);
        }

        public async Task<DatabaseResult<OrderDto>> UpdateOrderAsync( UpdateOrderDto updateOrderDto )
        {
            // Input validation
            DatabaseResult<OrderDto> inputValidation = ValidateUpdateInput(updateOrderDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<OrderDto> businessValidation = await ValidateUpdateBusiness(updateOrderDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform Update
            return await PerformUpdateAsync(updateOrderDto);
        }


        public async Task<DatabaseResult> ActivateOrderAsync( int orderId )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(ActivateOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForActivation(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.ActivateOrderAsync(orderId);

            if (result.IsSuccess)
            {
                orderStore.UpdateStatus(orderId, OrderStatus.Active);
                logger.LogInformation("Order {OrderId} activated successfully", orderId);
            }

            return result;
        }

        public async Task<DatabaseResult> FulfillOrderAsync( int orderId )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(FulfillOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForFulfillment(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.FulfillOrderAsync(orderId);

            if (!result.IsSuccess) return result;

            orderStore.UpdateStatus(orderId, OrderStatus.Fulfilled);
            logger.LogInformation("Order {OrderId} fulfilled successfully", orderId);

            return result;
        }

        public async Task<DatabaseResult> CompleteOrderAsync( int orderId )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(CompleteOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForCompletion(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.CompleteOrderAsync(orderId);

            if (result.IsSuccess)
            {
                orderStore.UpdateStatus(orderId, OrderStatus.Completed);
                logger.LogInformation("Order {OrderId} completed successfully", orderId);
            }

            return result;
        }

        public async Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(CancelOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidForCancellation(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.CancelOrderAsync(orderId, reason);

            if (result.IsSuccess)
            {
                orderStore.UpdateStatus(orderId, OrderStatus.Cancelled);
                logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
            }

            return result;
        }

        public async Task<DatabaseResult> DeleteOrderAsync( int orderId )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(DeleteOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForDeletion(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.DeleteAsync(orderId);

            if (!result.IsSuccess) return result;

            orderStore.Delete(orderId);
            logger.LogInformation("Order {OrderId} permanently deleted - THIS SHOULD BE RARE", orderId);
            return result;
        }

        #region Private Methods

        private async Task<DatabaseResult<OrderDto>> PerformCreateAsync( CreateOrderDto createOrderDto )
        {
            // Create order with Draft status
            Order order = createOrderDto.ToDomain();

            DatabaseResult<Order> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CreateAsync(order),
                "Creating new order");

            if (result is { IsSuccess: true, Value: not null })
            {
                OrderDto orderDto = result.Value.ToDto();
                orderStore.Create(result.Value.OrderId, createOrderDto);
                logger.LogInformation("Successfully created order with ID {OrderId} in Draft status", result.Value.OrderId);
                return DatabaseResult<OrderDto>.Success(orderDto);
            }

            logger.LogWarning("Failed to create order: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<OrderDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<OrderDto>> PerformUpdateAsync( UpdateOrderDto updateOrderDto )
        {
            // Get existing order
            DatabaseResult<Order?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByIdAsync(updateOrderDto.OrderId),
                $"Retrieving order {updateOrderDto.OrderId} for update",
                false);

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning("Cannot update order {OrderId}: {ErrorMessage}", updateOrderDto.OrderId, getResult.ErrorMessage ?? "Order not found");
                return DatabaseResult<OrderDto>.Failure(
                    getResult.ErrorMessage ?? "Order not found",
                    getResult.ErrorCode);
            }

            Order updatedOrder = getResult.Value with
            {
                Status = updateOrderDto.Status,
                DeliveryDate = updateOrderDto.DeliveryDate,
                Notes = updateOrderDto.Notes
            };

            DatabaseResult<Order> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.UpdateAsync(updatedOrder),
                "Updating order");

            if (!updateResult.IsSuccess || updateResult.Value == null)
            {
                logger.LogWarning(
                    "Failed to update order {OrderId}: {ErrorMessage}",
                    updateOrderDto.OrderId,
                    updateResult.ErrorMessage);
                return DatabaseResult<OrderDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
            }

            OrderDto orderDto = updateResult.Value.ToDto();
            OrderDto? storeResult = orderStore.Update(updateOrderDto);

            if (storeResult == null)
            {
                logger.LogWarning("Order {OrderId} updated in database but failed to update in store", updateOrderDto.OrderId);
            }

            logger.LogInformation("Successfully updated order with ID {OrderId}", updateOrderDto.OrderId);
            return DatabaseResult<OrderDto>.Success(orderDto);
        }

        private DatabaseResult CheckForNull( int orderId, string queryDescription )
        {
            if (orderId > 0) return DatabaseResult.Success();

            logger.LogWarning("{QueryDescription} called with invalid orderId: {OrderId}", queryDescription, orderId);
            return DatabaseResult.Failure($"Invalid order ID {orderId}", DatabaseErrorCode.InvalidInput);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<OrderDto> ValidateCreateInput( CreateOrderDto createOrderDto )
        {
            if (createOrderDto == null!)
            {
                logger.LogWarning("CreateOrderAsync called with null CreateOrderDto");
                return DatabaseResult<OrderDto>.Failure("Order cannot be null", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidation.Validate(createOrderDto);

            if (validationResult.IsValid) return DatabaseResult<OrderDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("CreateOrderDto validation failed: {Errors}", errors);
            return DatabaseResult<OrderDto>.Failure($"Validation failed {errors}", DatabaseErrorCode.ValidationFailure);
        }

        private DatabaseResult<OrderDto> ValidateCreateBusiness( CreateOrderDto createOrderDto )
        {
            switch (createOrderDto)
            {
                case { Type: OrderType.Purchase, SupplierId: null }:
                    logger.LogWarning("Purchase order created without suppler ID");
                    return DatabaseResult<OrderDto>.Failure("Purchase order must have a supplier", DatabaseErrorCode.InvalidInput);

                case { Type: OrderType.Sale, CustomerId: null }:
                    logger.LogWarning("Sales order created without customer ID");
                    return DatabaseResult<OrderDto>.Failure("Sales order must have a customer", DatabaseErrorCode.InvalidInput);

                default:
                    return DatabaseResult<OrderDto>.Success(null!);
            }
        }

        private DatabaseResult<OrderDto> ValidateUpdateInput( UpdateOrderDto updateOrderDto )
        {
            if (updateOrderDto == null!)
            {
                logger.LogWarning("UpdateOrderAsync called with null UpdateOrderDto");
                return DatabaseResult<OrderDto>.Failure("Order cannot be null", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = updateValidation.Validate(updateOrderDto);

            if (validationResult.IsValid) return DatabaseResult<OrderDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("CreateOrderDto validation failed: {Errors}", errors);
            return DatabaseResult<OrderDto>.Failure($"Validation failed {errors}", DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<OrderDto>> ValidateUpdateBusiness( UpdateOrderDto updateOrderDto )
        {
            // Check existence of order
            DatabaseResult<bool> existsResult = await orderValidationService.OrderExistsAsync(updateOrderDto.OrderId);
            if (!existsResult.IsSuccess)
                return DatabaseResult<OrderDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (existsResult.Value) return DatabaseResult<OrderDto>.Success(null!);

            logger.LogWarning("Attempted to update non-existent category with ID {CategoryId}", updateOrderDto.OrderId);
            return DatabaseResult<OrderDto>.Failure(
                $"Category with ID {updateOrderDto.OrderId} not found.",
                DatabaseErrorCode.NotFound);
        }

        #endregion
    }
}
