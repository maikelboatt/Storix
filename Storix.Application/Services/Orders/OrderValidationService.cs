using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    ///     Service responsible for validating operations.
    /// </summary>
    public class OrderValidationService(
        IOrderRepository orderRepository,
        IOrderStore orderStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderValidationService> logger ):IOrderValidationService
    {
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
                ? DatabaseResult<bool>.Success(true)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult> ValidateForActivation( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to activated non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canActivateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeActivated(orderId),
                $"Validating order {orderId} for activation");

            if (!canActivateResult.IsSuccess)
                return DatabaseResult.Failure(canActivateResult.ErrorMessage!, canActivateResult.ErrorCode);

            if (!canActivateResult.Value)
            {
                logger.LogWarning("Order {OrderId} cannot  be activated - not in Draft status", orderId);
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

            if (canFulfillResult.Value) return DatabaseResult.Success();

            logger.LogWarning("Order {OrderId} cannot  be fulfilled - not in Active status", orderId);
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
                $"Validation order {orderId} for completion");

            if (!canCompleteResult.IsSuccess)
                return DatabaseResult.Failure(canCompleteResult.ErrorMessage!, canCompleteResult.ErrorCode);

            if (!canCompleteResult.Value)
            {
                logger.LogWarning("Order {OrderId} cannot  be completed - not in Active status", orderId);
                return DatabaseResult.Failure("Only Active orders can be activated", DatabaseErrorCode.InvalidInput);
            }

            return DatabaseResult.Success();
        }

        public async Task<DatabaseResult> ValidForCancellation( int orderId )
        {
            DatabaseResult<bool> existsResult = await OrderExistsAsync(orderId);

            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                logger.LogWarning("Attempted to cancel non-existent order {OrderId}", orderId);
                return DatabaseResult.Failure($"Order {orderId} does not exist.", DatabaseErrorCode.NotFound);
            }

            DatabaseResult<bool> canCancelResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.CanBeCancelled(orderId),
                $"Validation order {orderId} for cancellation");

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

        public async Task<DatabaseResult<bool>> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false )
        {
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.SupplierHasOrdersAsync(supplierId, activeOnly),
                $"Checking if supplier {supplierId} has order (active: {activeOnly})",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<bool>> CustomerHasOrdersAsync( int customerId, bool activeOnly = false )
        {
            DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.SupplierHasOrdersAsync(customerId, activeOnly),
                $"Checking if customer {customerId} has order (active: {activeOnly})",
                false);

            return result.IsSuccess
                ? DatabaseResult<bool>.Success(result.Value)
                : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
