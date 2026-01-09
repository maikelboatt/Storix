using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    /// Service responsible for validating operations.
    /// </summary>
    public class OrderValidationService(
        IOrderRepository orderRepository,
        IOrderStore orderStore,
        ILocationRepository locationRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderValidationService> logger ):IOrderValidationService
    {
        #region Order Existence Validation

        public async Task<DatabaseResult<bool>> OrderExistsAsync( int orderId )
        {
            if (orderId <= 0)
                return DatabaseResult<bool>.Success(false);

            OrderDto? existsResult = orderStore.GetById(orderId);
            if (existsResult != null)
                return DatabaseResult<bool>.Success(true);

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.ExistsAsync(orderId),
                $"Checking if order {orderId} exists.");

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Order Status Transition Validation

        public async Task<DatabaseResult> ValidateForRevertToDraft( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to revert non-existent order to draft {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canRevertToDraft = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeActivated(orderId),
                $"Validating order {orderId} for reverting to draft");

            if (!canRevertToDraft.IsSuccess)
                return DatabaseResult.Failure(canRevertToDraft.ErrorMessage!, canRevertToDraft.ErrorCode);

            if (!canRevertToDraft.Value)
            {
                logger.LogWarning("Order {OrderId} cannot be reverted to draft - not in Active status", orderId);
                return DatabaseResult.Failure("Only active orders can be reverted to Draft", DatabaseErrorCode.InvalidInput);
            }

            return DatabaseResult.Success();
        }


        public async Task<DatabaseResult> ValidateForActivation( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to activate non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canActivateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeActivated(orderId),
                $"Validating order {orderId} for activation");

            if (!canActivateResult.IsSuccess)
                return DatabaseResult.Failure(canActivateResult.ErrorMessage!, canActivateResult.ErrorCode);

            if (!canActivateResult.Value)
            {
                logger.LogWarning("Order {OrderId} cannot be activated - not in Draft status", orderId);
                return DatabaseResult.Failure("Only Draft orders can be activated", DatabaseErrorCode.InvalidInput);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForFulfillment( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to fulfill non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canFulfillResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeFulfilled(orderId),
                $"Validating order {orderId} for fulfillment");

            if (!canFulfillResult.IsSuccess)
                return DatabaseResult.Failure(canFulfillResult.ErrorMessage!, canFulfillResult.ErrorCode);

            if (canFulfillResult.Value)
                return DatabaseResult.Success();

            logger.LogWarning("Order {OrderId} cannot be fulfilled - not in Active status", orderId);
            return DatabaseResult.Failure("Only Active orders can be fulfilled", DatabaseErrorCode.InvalidInput);
        }

        public async Task<DatabaseResult> ValidateForCompletion( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to complete non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canCompleteResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeCompleted(orderId),
                $"Validating order {orderId} for completion");

            if (!canCompleteResult.IsSuccess)
                return DatabaseResult.Failure(canCompleteResult.ErrorMessage!, canCompleteResult.ErrorCode);

            if (!canCompleteResult.Value)
            {
                logger.LogWarning("Order {OrderId} cannot be completed - not in Fulfilled status", orderId);
                return DatabaseResult.Failure("Only Fulfilled orders can be completed", DatabaseErrorCode.InvalidInput);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForCancellation( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to cancel non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canCancelResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeCancelled(orderId),
                $"Validating order {orderId} for cancellation");

            if (!canCancelResult.IsSuccess)
                return DatabaseResult.Failure(canCancelResult.ErrorMessage!, canCancelResult.ErrorCode);

            if (!canCancelResult.Value)
            {
                logger.LogWarning("Order {OrderId} cannot be cancelled - not in Active or Draft status", orderId);
                return DatabaseResult.Failure("Only Active and Draft orders can be cancelled", DatabaseErrorCode.InvalidInput);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidateForDeletion( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);
            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to delete non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order with ID {orderId} not found.", DatabaseErrorCode.NotFound);
            }

            logger.LogWarning("Order deletion requested for {OrderId} - Orders should rarely be deleted", orderId);
            return DatabaseResult.Success();
        }

        #endregion

        #region Supplier and Customer Order Validation

        public async Task<DatabaseResult<bool>> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false )
        {
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid supplier ID {SupplierId} provided", supplierId);
                return DatabaseResult<bool>.Failure(
                    "Supplier ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.SupplierHasOrdersAsync(supplierId, activeOnly),
                $"Checking if supplier {supplierId} has orders (active: {activeOnly})",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> CustomerHasOrdersAsync( int customerId, bool activeOnly = false )
        {
            if (customerId <= 0)
            {
                logger.LogWarning("Invalid customer ID {CustomerId} provided", customerId);
                return DatabaseResult<bool>.Failure(
                    "Customer ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CustomerHasOrdersAsync(customerId, activeOnly),
                $"Checking if customer {customerId} has orders (active: {activeOnly})",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region ✅ NEW: Location Validation Methods

        /// <summary>
        /// Checks if a location has any orders (active or historical).
        /// </summary>
        public async Task<DatabaseResult<bool>> LocationHasOrdersAsync( int locationId, bool activeOnly = false )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<bool>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.LocationHasOrdersAsync(locationId, activeOnly),
                $"Checking if location {locationId} has orders (active: {activeOnly})",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Checks if a location can be deleted (no active orders associated).
        /// </summary>
        public async Task<DatabaseResult<bool>> CanDeleteLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<bool>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanDeleteLocationAsync(locationId),
                $"Checking if location {locationId} can be deleted");

            if (!result.IsSuccess)
                return DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);

            if (!result.Value)
            {
                logger.LogWarning("Location {LocationId} cannot be deleted - has active orders", locationId);
            }

            return DatabaseResult<bool>.Success(result.Value);
        }

        /// <summary>
        /// Validates if a location exists and is valid for creating an order.
        /// </summary>
        public async Task<DatabaseResult> ValidateLocationForOrderAsync( int locationId, OrderType orderType )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Check if location exists
            DatabaseResult<bool> locationExistsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.ExistsAsync(locationId),
                $"Checking if location {locationId} exists");

            if (!locationExistsResult.IsSuccess)
                return DatabaseResult.Failure(locationExistsResult.ErrorMessage!, locationExistsResult.ErrorCode);

            if (!locationExistsResult.Value)
            {
                logger.LogWarning("Attempted to create {OrderType} order with non-existent location {LocationId}", orderType, locationId);
                return DatabaseResult.Failure($"Location {locationId} does not exist.", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Location {LocationId} validated successfully for {OrderType} order", locationId, orderType);
            return DatabaseResult.Success();
        }

        /// <summary>
        /// Validates if an order can be transferred to a new location.
        /// </summary>
        public async Task<DatabaseResult> ValidateOrderTransferAsync( int orderId, int newLocationId )
        {
            // Validate order exists
            DatabaseResult<bool> orderExistsResult = await OrderExistsAsync(orderId);
            if (!orderExistsResult.IsSuccess || !orderExistsResult.Value)
            {
                logger.LogWarning("Attempted to transfer non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            // Validate new location exists
            if (newLocationId <= 0)
            {
                logger.LogWarning("Invalid new location ID {LocationId} provided for transfer", newLocationId);
                return DatabaseResult.Failure(
                    "New location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<bool> locationExistsResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.ExistsAsync(newLocationId),
                $"Checking if new location {newLocationId} exists");

            if (!locationExistsResult.IsSuccess)
                return DatabaseResult.Failure(locationExistsResult.ErrorMessage!, locationExistsResult.ErrorCode);

            if (!locationExistsResult.Value)
            {
                logger.LogWarning("Attempted to transfer order {OrderId} to non-existent location {LocationId}", orderId, newLocationId);
                return DatabaseResult.Failure($"Location {newLocationId} does not exist.", DatabaseErrorCode.NotFound);
            }

            // Check order status - only Draft or Active orders can be transferred
            OrderDto? order = orderStore.GetById(orderId);
            if (order != null && order.Status != OrderStatus.Draft && order.Status != OrderStatus.Active)
            {
                logger.LogWarning("Order {OrderId} cannot be transferred - status is {Status}", orderId, order.Status);
                return DatabaseResult.Failure(
                    "Only Draft or Active orders can be transferred to a different location.",
                    DatabaseErrorCode.InvalidInput);
            }

            logger.LogInformation("Order {OrderId} validated successfully for transfer to location {LocationId}", orderId, newLocationId);
            return DatabaseResult.Success();
        }

        /// <summary>
        /// Checks if an order can be transferred to a different location.
        /// </summary>
        public async Task<DatabaseResult<bool>> CanTransferOrderToLocationAsync( int orderId, int newLocationId )
        {
            DatabaseResult validationResult = await ValidateOrderTransferAsync(orderId, newLocationId);

            return validationResult.IsSuccess
                ? DatabaseResult<bool>.Success(true)
                : DatabaseResult<bool>.Failure(validationResult.ErrorMessage!, validationResult.ErrorCode);
        }

        #endregion
    }
}
