using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Repositories;
using Storix.Application.Services.OrderItems.Interfaces;
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
        IOrderItemService orderItemService,
        IInventoryManager inventoryManager,
        IOrderStore orderStore,
        IValidator<CreateOrderDto> createValidation,
        IValidator<UpdateOrderDto> updateValidation,
        ILogger<OrderWriteService> logger ):IOrderWriteService
    {
        #region Create and Update Operations

        public async Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto )
        {
            // Input validation
            DatabaseResult<OrderDto> inputValidation = ValidateCreateInput(createOrderDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Validate location
            DatabaseResult locationValidation = await orderValidationService.ValidateLocationForOrderAsync(
                createOrderDto.LocationId,
                createOrderDto.Type);
            if (!locationValidation.IsSuccess)
                return DatabaseResult<OrderDto>.Failure(locationValidation.ErrorMessage!, locationValidation.ErrorCode);

            // Validate stock for sales orders
            DatabaseResult stockValidation = await ValidateStockForSalesOrder(createOrderDto);
            if (!stockValidation.IsSuccess)
                return DatabaseResult<OrderDto>.Failure(stockValidation.ErrorMessage!, stockValidation.ErrorCode);

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

            // Validate location change if present
            if (updateOrderDto.LocationId.HasValue)
            {
                DatabaseResult<Order?> orderResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => orderRepository.GetByIdAsync(updateOrderDto.OrderId),
                    $"Retrieving order {updateOrderDto.OrderId} for location validation",
                    false);

                if (orderResult is { IsSuccess: true, Value: not null })
                {
                    // Only validate location change if it's actually changing
                    if (orderResult.Value.LocationId != updateOrderDto.LocationId.Value)
                    {
                        DatabaseResult locationValidation = await ValidateLocationChange(
                            updateOrderDto.OrderId,
                            orderResult.Value.LocationId,
                            updateOrderDto.LocationId.Value,
                            orderResult.Value.Type);

                        if (!locationValidation.IsSuccess)
                            return DatabaseResult<OrderDto>.Failure(locationValidation.ErrorMessage!, locationValidation.ErrorCode);
                    }
                }
            }

            // Perform Update
            return await PerformUpdateAsync(updateOrderDto);
        }

        #endregion

        #region Status Change Operations

        public async Task<DatabaseResult> RevertToDraftOrderAsync( int orderId, OrderStatus originalStatus )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(RevertToDraftOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForRevertToDraft(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.RevertToDraftOrderAsync(orderId);

            if (result.IsSuccess)
            {
                orderStore.UpdateStatus(orderId, OrderStatus.Draft);
                logger.LogInformation("Order {OrderId} reverted to draft successfully", orderId);
            }

            // OrderDto? order = orderStore.GetById(orderId);
            // if (order is not null)
            //     await HandleStatusChangeAsync(order.ToDomain(), originalStatus, OrderStatus.Draft);

            return result;
        }

        public async Task<DatabaseResult> ActivateOrderAsync( int orderId, OrderStatus originalStatus )
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

            OrderDto? order = orderStore.GetById(orderId);
            if (order is not null)
                await HandleStatusChangeAsync(order.ToDomain(), originalStatus, OrderStatus.Active);


            return result;
        }

        public async Task<DatabaseResult> FulfillOrderAsync( int orderId, OrderStatus originalStatus )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(FulfillOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForFulfillment(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.FulfillOrderAsync(orderId);

            if (result.IsSuccess)
            {
                orderStore.UpdateStatus(orderId, OrderStatus.Fulfilled);
                logger.LogInformation("Order {OrderId} fulfilled successfully", orderId);
            }

            OrderDto? order = orderStore.GetById(orderId);
            if (order is not null)
                await HandleStatusChangeAsync(order.ToDomain(), originalStatus, OrderStatus.Fulfilled);

            return result;
        }

        public async Task<DatabaseResult> CompleteOrderAsync( int orderId, OrderStatus originalStatus )
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

            OrderDto? order = orderStore.GetById(orderId);
            if (order is not null)
                await HandleStatusChangeAsync(order.ToDomain(), originalStatus, OrderStatus.Completed);

            return result;
        }

        public async Task<DatabaseResult> CancelOrderAsync( int orderId, OrderStatus originalStatus, string? reason = null )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(CancelOrderAsync));
            if (!checkForNull.IsSuccess) return checkForNull;

            DatabaseResult validationResult = await orderValidationService.ValidateForCancellation(orderId);
            if (!validationResult.IsSuccess)
                return validationResult;

            DatabaseResult result = await orderRepository.CancelOrderAsync(orderId, reason);

            if (result.IsSuccess)
            {
                orderStore.UpdateStatus(orderId, OrderStatus.Cancelled);
                logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
            }

            OrderDto? order = orderStore.GetById(orderId);
            if (order is not null)
                await HandleStatusChangeAsync(order.ToDomain(), originalStatus, OrderStatus.Cancelled);
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

        #endregion

        #region Location Transfer Operation

        /// <summary>
        /// Transfers an order to a different location.
        /// Only allowed for Draft and Active orders.
        /// Validates stock availability at the new location for Sales orders.
        /// </summary>
        public async Task<DatabaseResult> TransferOrderToLocationAsync( int orderId, int newLocationId, string? reason = null )
        {
            DatabaseResult checkForNull = CheckForNull(orderId, nameof(TransferOrderToLocationAsync));
            if (!checkForNull.IsSuccess)
                return checkForNull;

            if (newLocationId <= 0)
            {
                logger.LogWarning("Invalid new location ID {LocationId} provided for order transfer", newLocationId);
                return DatabaseResult.Failure(
                    "New location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Validate transfer is allowed
            DatabaseResult validationResult = await orderValidationService.ValidateOrderTransferAsync(orderId, newLocationId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Get order details
            DatabaseResult<Order?> orderResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByIdAsync(orderId),
                $"Retrieving order {orderId} for transfer");

            if (!orderResult.IsSuccess || orderResult.Value == null)
            {
                logger.LogWarning("Cannot transfer order {OrderId}: Order not found", orderId);
                return DatabaseResult.Failure("Order not found", DatabaseErrorCode.NotFound);
            }

            Order order = orderResult.Value;

            // Validate stock at new location for sales orders
            if (order.Type == OrderType.Sale)
            {
                DatabaseResult stockValidation = await ValidateStockAtLocation(orderId, newLocationId);
                if (!stockValidation.IsSuccess)
                    return stockValidation;
            }

            // Perform transfer
            DatabaseResult transferResult = await orderRepository.TransferOrderToLocationAsync(orderId, newLocationId);

            if (transferResult.IsSuccess)
            {
                orderStore.UpdateLocation(orderId, newLocationId);
                logger.LogInformation(
                    "Order {OrderId} transferred from location {OldLocationId} to location {NewLocationId}. Reason: {Reason}",
                    orderId,
                    order.LocationId,
                    newLocationId,
                    reason ?? "Not specified");
            }

            return transferResult;
        }

        #endregion

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
                logger.LogInformation(
                    "Successfully created order with ID {OrderId} in Draft status at location {LocationId}",
                    result.Value.OrderId,
                    result.Value.LocationId);
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
                logger.LogWarning(
                    "Cannot update order {OrderId}: {ErrorMessage}",
                    updateOrderDto.OrderId,
                    getResult.ErrorMessage ?? "Order not found");
                return DatabaseResult<OrderDto>.Failure(
                    getResult.ErrorMessage ?? "Order not found",
                    getResult.ErrorCode);
            }

            Order existingOrder = getResult.Value;
            OrderStatus oldStatus = existingOrder.Status;
            OrderStatus newStatus = updateOrderDto.Status;
            int oldLocationId = existingOrder.LocationId;
            int newLocationId = updateOrderDto.LocationId ?? existingOrder.LocationId;

            Order updatedOrder = existingOrder with
            {
                Status = updateOrderDto.Status,
                LocationId = newLocationId,
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

            await HandleStatusChangeAsync(existingOrder, oldStatus, newStatus);

            if (oldLocationId != newLocationId)
            {
                logger.LogInformation(
                    "Order {OrderId} location changed from {OldLocationId} to {NewLocationId}",
                    updateOrderDto.OrderId,
                    oldLocationId,
                    newLocationId);
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

        private async Task HandleStatusChangeAsync( Order order, OrderStatus oldStatus, OrderStatus newStatus )
        {
            switch (newStatus)
            {
                // Status changed to Active - Reserve stock for sales orders
                case OrderStatus.Active when oldStatus != OrderStatus.Active:
                {
                    if (order.Type == OrderType.Sale)
                    {

                        DatabaseResult<IEnumerable<OrderItemDto>> itemResult = await orderItemService.GetOrderItemsByOrderIdAsync(order.OrderId);
                        if (!itemResult.IsSuccess || itemResult.Value == null)
                        {
                            logger.LogWarning(
                                "Cannot reserve stock for order {OrderId}: Unable to retrieve order items",
                                order.OrderId);
                            return;
                        }

                        IEnumerable<OrderItemDto>? orderItems = itemResult.Value;

                        foreach (OrderItemDto item in orderItems)
                        {
                            DatabaseResult<Inventory?> inventoryResult =
                                await inventoryManager.GetInventoryByProductAndLocationAsync(item.ProductId, order.LocationId);

                            if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                            {
                                logger.LogWarning(
                                    "Cannot reserve stock for order {OrderId}: Product {ProductId} not found in inventory at location {LocationId}",
                                    order.OrderId,
                                    item.ProductId,
                                    order.LocationId);
                                return;
                            }

                            Inventory? inventory = inventoryResult.Value;

                            if (inventory.AvailableStock < item.Quantity)
                            {
                                logger.LogWarning(
                                    "Insufficient stock to reserve for order {OrderId}: Product {ProductId} at location {LocationId}. Available: {Available}, Required: {Required}",
                                    order.OrderId,
                                    item.ProductId,
                                    order.LocationId,
                                    inventory.AvailableStock,
                                    item.Quantity);

                                return;
                            }

                            await inventoryManager.ReserveStockAsync(inventoryResult.Value.InventoryId, item.Quantity);
                            logger.LogInformation(
                                "Sales order {OrderId} activated at location {LocationId} - stock should be reserved",
                                order.OrderId,
                                order.LocationId);

                        }
                    }
                    break;
                }
                // Status changed to Cancelled - Release reserved stock for sales orders
                case OrderStatus.Cancelled when oldStatus == OrderStatus.Active:
                {
                    if (order.Type == OrderType.Sale)
                    {
                        DatabaseResult<IEnumerable<OrderItemDto>> itemResult = await orderItemService.GetOrderItemsByOrderIdAsync(order.OrderId);
                        if (!itemResult.IsSuccess || itemResult.Value == null)
                        {
                            logger.LogWarning("Cannot release reserved stock for order {OrderId}: Unable to retrieve order items", order.OrderId);
                            return;
                        }

                        foreach (OrderItemDto item in itemResult.Value)
                        {
                            DatabaseResult<Inventory?> inventoryResult =
                                await inventoryManager.GetInventoryByProductAndLocationAsync(item.ProductId, order.LocationId);

                            if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                            {
                                logger.LogWarning(
                                    "Cannot release reserved stock for order {OrderId}: Product {ProductId} not found in inventory at location {LocationId}",
                                    order.OrderId,
                                    item.ProductId,
                                    order.LocationId);
                                return;
                            }

                            await inventoryManager.ReleaseReservedStockAsync(inventoryResult.Value.InventoryId, item.Quantity);
                            logger.LogInformation(
                                "Released {Quantity} reserved stock for product {ProductId} at location {LocationId} for cancelled order {OrderId}",
                                item.Quantity,
                                item.ProductId,
                                order.LocationId,
                                order.OrderId);

                        }
                    }
                    break;
                }
                case OrderStatus.Fulfilled:
                    logger.LogInformation(
                        "Order {OrderId} marked as Fulfilled at location {LocationId}. User must complete fulfillment process to update inventory.",
                        order.OrderId,
                        order.LocationId);
                    break;
            }
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
                    logger.LogWarning("Purchase order created without supplier ID");
                    return DatabaseResult<OrderDto>.Failure("Purchase order must have a supplier", DatabaseErrorCode.InvalidInput);

                case { Type: OrderType.Sale, CustomerId: null }:
                    logger.LogWarning("Sales order created without customer ID");
                    return DatabaseResult<OrderDto>.Failure("Sales order must have a customer", DatabaseErrorCode.InvalidInput);

                default:
                    return DatabaseResult<OrderDto>.Success(null!);
            }
        }

        private async Task<DatabaseResult> ValidateStockForSalesOrder( CreateOrderDto createOrderDto )
        {
            if (createOrderDto.Type != OrderType.Sale)
                return DatabaseResult.Success();

            foreach (CreateOrderItemDto item in createOrderDto.OrderItems)
            {
                DatabaseResult<Inventory?> inventoryResult =
                    await inventoryManager.GetInventoryByProductAndLocationAsync(item.ProductId, createOrderDto.LocationId);

                if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                {
                    logger.LogWarning(
                        "Product {ProductId} not available at location {LocationId}",
                        item.ProductId,
                        createOrderDto.LocationId);
                    return DatabaseResult.Failure(
                        $"Product {item.ProductId} not available at location {createOrderDto.LocationId}",
                        DatabaseErrorCode.NotFound);
                }

                if (inventoryResult.Value.AvailableStock < item.Quantity)
                {
                    logger.LogWarning(
                        "Insufficient stock for product {ProductId} at location {LocationId}. Available: {Available}, Required: {Required}",
                        item.ProductId,
                        createOrderDto.LocationId,
                        inventoryResult.Value.AvailableStock,
                        item.Quantity);
                    return DatabaseResult.Failure(
                        $"Insufficient stock for product {item.ProductId}. Available: {inventoryResult.Value.AvailableStock}, Required: {item.Quantity}",
                        DatabaseErrorCode.ConstraintViolation);
                }
            }

            return DatabaseResult.Success();
        }

        /// <summary>
        /// Validates stock availability at a specific location for an existing sales order.
        /// </summary>
        private async Task<DatabaseResult> ValidateStockAtLocation( int orderId, int locationId )
        {
            // Get order items
            DatabaseResult<IEnumerable<OrderItemDto>> itemsResult =
                await orderItemService.GetOrderItemsByOrderIdAsync(orderId);

            if (!itemsResult.IsSuccess || itemsResult.Value == null)
            {
                logger.LogWarning("Cannot validate stock for order {OrderId}: Unable to retrieve order items", orderId);
                return DatabaseResult.Failure(
                    "Unable to retrieve order items for stock validation",
                    DatabaseErrorCode.NotFound);
            }

            foreach (OrderItemDto item in itemsResult.Value)
            {
                DatabaseResult<Inventory?> inventoryResult =
                    await inventoryManager.GetInventoryByProductAndLocationAsync(item.ProductId, locationId);

                if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                {
                    logger.LogWarning(
                        "Product {ProductId} not available at location {LocationId} for order transfer",
                        item.ProductId,
                        locationId);
                    return DatabaseResult.Failure(
                        $"Product {item.ProductId} not available at location {locationId}",
                        DatabaseErrorCode.NotFound);
                }

                if (inventoryResult.Value.AvailableStock >= item.Quantity) continue;

                logger.LogWarning(
                    "Insufficient stock for product {ProductId} at location {LocationId} for order transfer. Available: {Available}, Required: {Required}",
                    item.ProductId,
                    locationId,
                    inventoryResult.Value.AvailableStock,
                    item.Quantity);
                return DatabaseResult.Failure(
                    $"Insufficient stock for product {item.ProductId} at location {locationId}. Available: {inventoryResult.Value.AvailableStock}, Required: {item.Quantity}",
                    DatabaseErrorCode.ConstraintViolation);
            }

            logger.LogInformation("Stock validation passed for order {OrderId} at location {LocationId}", orderId, locationId);
            return DatabaseResult.Success();
        }

        /// <summary>
        /// Validates that a location change is allowed and has sufficient stock.
        /// </summary>
        private async Task<DatabaseResult> ValidateLocationChange(
            int orderId,
            int oldLocationId,
            int newLocationId,
            OrderType orderType )
        {
            // Validate new location exists
            DatabaseResult locationValidation = await orderValidationService.ValidateLocationForOrderAsync(
                newLocationId,
                orderType);

            if (!locationValidation.IsSuccess)
                return locationValidation;

            // For sales orders, validate stock at new location
            if (orderType == OrderType.Sale)
            {
                DatabaseResult stockValidation = await ValidateStockAtLocation(orderId, newLocationId);
                if (!stockValidation.IsSuccess)
                {
                    logger.LogWarning(
                        "Cannot change order {OrderId} location from {OldLocationId} to {NewLocationId}: {Reason}",
                        orderId,
                        oldLocationId,
                        newLocationId,
                        stockValidation.ErrorMessage);
                    return stockValidation;
                }
            }

            logger.LogInformation(
                "Location change validated for order {OrderId}: {OldLocationId} → {NewLocationId}",
                orderId,
                oldLocationId,
                newLocationId);
            return DatabaseResult.Success();
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
            logger.LogWarning("UpdateOrderDto validation failed: {Errors}", errors);
            return DatabaseResult<OrderDto>.Failure($"Validation failed {errors}", DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<OrderDto>> ValidateUpdateBusiness( UpdateOrderDto updateOrderDto )
        {
            // Check existence of order
            DatabaseResult<bool> existsResult = await orderValidationService.OrderExistsAsync(updateOrderDto.OrderId);
            if (!existsResult.IsSuccess)
                return DatabaseResult<OrderDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (existsResult.Value) return DatabaseResult<OrderDto>.Success(null!);

            logger.LogWarning("Attempted to update non-existent order with ID {OrderId}", updateOrderDto.OrderId);
            return DatabaseResult<OrderDto>.Failure(
                $"Order with ID {updateOrderDto.OrderId} not found.",
                DatabaseErrorCode.NotFound);
        }

        #endregion
    }
}
