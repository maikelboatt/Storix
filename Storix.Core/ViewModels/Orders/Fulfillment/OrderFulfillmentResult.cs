namespace Storix.Core.ViewModels.Orders.Fulfillment
{
    /// <summary>
    /// Result returned when the fulfillment form closes
    /// </summary>
    public class OrderFulfillmentResult
    {
        public bool Success { get; set; }
        public bool Cancelled { get; set; }
        public int OrderId { get; set; }
        public int LocationId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
