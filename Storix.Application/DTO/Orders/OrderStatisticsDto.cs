namespace Storix.Application.DTO.Orders
{
    /// <summary>
    ///     Statistics data transfer object for order reporting.
    /// </summary>
    public class OrderStatisticsDto
    {
        public int TotalOrders { get; set; }
        public int DraftOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int PurchaseOrderCount { get; set; }
        public int SaleOrderCount { get; set; }
    }
}
