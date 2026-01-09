using Microsoft.Extensions.Logging;
using Storix.Core.Control;
using Storix.Core.ViewModels.Orders.Fulfillment;
using Storix.Domain.Enums;

namespace Storix.Core.Helper
{
    public class OrderFulfillmentHelper:IOrderFulfillmentHelper
    {
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<OrderFulfillmentHelper> _logger;

        public OrderFulfillmentHelper(
            IModalNavigationControl modalNavigationControl,
            ILogger<OrderFulfillmentHelper> logger )
        {
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;
        }

        public async Task HandleFulfillmentFlowAsync(
            int orderId,
            string orderNumber,
            OrderType orderType,
            OrderStatus originalStatus,
            Func<Task> revertCallback )
        {
            try
            {
                OrderFulfillmentParameter parameter = new()
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    OrderType = orderType
                };

                OrderFulfillmentResult? result = await _modalNavigationControl
                    .PopUpWithResultAsync<OrderFulfillmentFormViewModel, OrderFulfillmentParameter, OrderFulfillmentResult>(parameter);

                if (result != null)
                {
                    if (result.Success)
                    {
                        _logger.LogInformation("Order {OrderNumber} fulfilled successfully", orderNumber);
                    }
                    else if (result.Cancelled)
                    {
                        _logger.LogInformation("User cancelled fulfillment");
                        await revertCallback();
                    }
                    else
                    {
                        _logger.LogError("Fulfillment failed: {Error}", result.ErrorMessage);
                        await revertCallback();
                    }
                }
                else
                {
                    _logger.LogInformation("Fulfillment modal closed without result");
                    await revertCallback();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing fulfillment modal");
                await revertCallback();
            }
        }
    }
}
