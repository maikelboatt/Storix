namespace Storix.Application.DTO.Orders
{
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public int ItemCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }
}
