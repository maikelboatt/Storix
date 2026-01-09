using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Fulfillment
{
    /// <summary>
    /// Parameter passed when navigating to OrderFulfillmentFormViewModel
    /// </summary>
    public class OrderFulfillmentParameter
    {
        public int OrderId { get; set; }
        public OrderType OrderType { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }
}
