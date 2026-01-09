using Storix.Domain.Enums;

namespace Storix.Core.Helper
{
    public interface IOrderFulfillmentHelper
    {
        Task HandleFulfillmentFlowAsync(
            int orderId,
            string orderNumber,
            OrderType orderType,
            OrderStatus originalStatus,
            Func<Task> revertCallback );
    }
}
