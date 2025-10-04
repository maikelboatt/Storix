using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
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
    public class OrderItemWriteService(
        IOrderItemRepository orderItemRepository,
        IOrderItemValidationService orderItemValidationService,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        IOrderItemStore orderItemStore,
        IValidator<CreateOrderItemDto> createValidator,
        IValidator<UpdateOrderItemDto> updateValidator,
        ILogger<OrderItemWriteService> logger ):IOrderItemWriteService
    {
        public async Task<DatabaseResult<OrderItemDto>> CreateOrderItemAsync( CreateOrderItemDto createOrderItemDto )
        {
            // Input validation
            DatabaseResult<OrderItemDto> inputValidation = ValidateCreateInput(createOrderItemDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<OrderItemDto> businessValidation = await ValidateCreateBusiness(createOrderItemDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create order item
            return await PerformCreateAsync(createOrderItemDto);
        }

        public async Task<DatabaseResult<OrderItemDto>> UpdateOrderItemAsync( UpdateOrderItemDto updateOrderItemDto )
        {
            // Input validation
            DatabaseResult<OrderItemDto> inputValidation = ValidateUpdateInput(updateOrderItemDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<OrderItemDto> businessValidation = await ValidateUpdateBusiness(updateOrderItemDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Update order item
            return await PerformUpdateAsync(updateOrderItemDto);
        }

        public async Task<DatabaseResult> DeleteOrderItemAsync( int orderItemId )
        {
            if (orderItemId <= 0)
            {
                logger.LogWarning("Invalid order item ID {OrderItemId} for deletion", orderItemId);
                return DatabaseResult.Failure("Order item ID must be positive.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await orderItemValidationService.ValidateForDeletion(orderItemId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform deletion
            return await PerformDeleteAsync(orderItemId);
        }

        #region Private Helpers

        private async Task<DatabaseResult<OrderItemDto>> PerformCreateAsync( CreateOrderItemDto createOrderItemDto )
        {

            OrderItem orderItem = createOrderItemDto.ToDomain();

            DatabaseResult<OrderItem> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.CreateAsync(orderItem),
                "Creating new order item");

            if (result is { IsSuccess: true, Value: not null })
            {
                OrderItemDto dto = result.Value.ToDto();
                orderItemStore.Create(result.Value.OrderItemId, dto);
                logger.LogInformation("Successfully added item {OrderItemId} to order {OrderId}", dto.OrderItemId, dto.OrderId);
                return DatabaseResult<OrderItemDto>.Success(dto);
            }

            logger.LogWarning("Failed to create order item: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<OrderItemDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<OrderItemDto>> PerformUpdateAsync( UpdateOrderItemDto updateOrderItemDto )
        {
            // Fetch existing order item
            DatabaseResult<OrderItem?> fetchResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.GetByIdAsync(updateOrderItemDto.OrderItemId),
                $"Fetching order item {updateOrderItemDto.OrderItemId} for update");

            if (!fetchResult.IsSuccess || fetchResult.Value == null)
            {
                logger.LogWarning("Order item {OrderItemId} not found for update", updateOrderItemDto.OrderItemId);
                return DatabaseResult<OrderItemDto>.Failure($"Order item with ID {updateOrderItemDto.OrderItemId} not found.", DatabaseErrorCode.NotFound);
            }

            OrderItem dto = updateOrderItemDto.ToDomain(fetchResult.Value);

            DatabaseResult<OrderItem> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderItemRepository.UpdateAsync(dto),
                $"Updating order item {dto.OrderItemId}");

            if (updateResult is { IsSuccess: true, Value: not null })
            {
                OrderItemDto itemDto = updateResult.Value.ToDto();
                orderItemStore.Update(itemDto); // Update cache
                logger.LogInformation("Successfully updated item {OrderItemId} in order {OrderId}", itemDto.OrderItemId, itemDto.OrderId);
                return DatabaseResult<OrderItemDto>.Success(itemDto);
            }

            logger.LogWarning("Failed to update order item: {ErrorMessage}", updateResult.ErrorMessage);
            return DatabaseResult<OrderItemDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformDeleteAsync( int orderItemId )
        {
            DatabaseResult deleteResult = await orderItemRepository.DeleteAsync(orderItemId);

            if (deleteResult.IsSuccess)
            {
                orderItemStore.Delete(orderItemId); // Remove from cache
                logger.LogInformation("Successfully deleted order item {OrderItemId}", orderItemId);
                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to delete order item {OrderItemId}: {ErrorMessage}", orderItemId, deleteResult.ErrorMessage);
            return DatabaseResult.Failure(deleteResult.ErrorMessage!, deleteResult.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<OrderItemDto> ValidateCreateInput( CreateOrderItemDto createOrderItemDto )
        {
            if (createOrderItemDto == null!)
            {
                logger.LogWarning("CreateOrderItemDto is null.");
                return DatabaseResult<OrderItemDto>.Failure("CreateOrderItemDto cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidator.Validate(createOrderItemDto);

            if (validationResult.IsValid)
                return DatabaseResult<OrderItemDto>.Success(null!); // No error, return success with null value

            string errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("CreateOrderItemDto validation failed: {Errors}", errorMessages);
            return DatabaseResult<OrderItemDto>.Failure(errorMessages, DatabaseErrorCode.InvalidInput);
        }

        private async Task<DatabaseResult<OrderItemDto>> ValidateCreateBusiness( CreateOrderItemDto createOrderItemDto )
        {
            DatabaseResult validationResult = await orderItemValidationService.ValidateForCreation(createOrderItemDto.OrderId, createOrderItemDto.ProductId);

            return validationResult.IsSuccess
                ? DatabaseResult<OrderItemDto>.Success(null!) // No error, return success with null value
                : DatabaseResult<OrderItemDto>.Failure(validationResult.ErrorMessage!, validationResult.ErrorCode);
        }

        private DatabaseResult<OrderItemDto> ValidateUpdateInput( UpdateOrderItemDto updateOrderItemDto )
        {
            if (updateOrderItemDto == null!)
            {
                logger.LogWarning("UpdateOrderItemDto is null.");
                return DatabaseResult<OrderItemDto>.Failure("UpdateOrderItemDto cannot be null.", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = updateValidator.Validate(updateOrderItemDto);

            if (validationResult.IsValid)
                return DatabaseResult<OrderItemDto>.Success(null!); // No error, return success with null value

            string errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("UpdateOrderItemDto validation failed: {Errors}", errorMessages);
            return DatabaseResult<OrderItemDto>.Failure(errorMessages, DatabaseErrorCode.InvalidInput);
        }

        private async Task<DatabaseResult<OrderItemDto>> ValidateUpdateBusiness( UpdateOrderItemDto updateOrderItemDto )
        {
            DatabaseResult validationResult = await orderItemValidationService.ValidateForUpdate(updateOrderItemDto.OrderItemId);

            return validationResult.IsSuccess
                ? DatabaseResult<OrderItemDto>.Success(null!) // No error, return success with null value
                : DatabaseResult<OrderItemDto>.Failure(validationResult.ErrorMessage!, validationResult.ErrorCode);
        }

        #endregion
    }
}
