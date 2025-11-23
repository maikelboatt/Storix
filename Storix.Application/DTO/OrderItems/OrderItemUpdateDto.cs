namespace Storix.Application.DTO.OrderItems
{
    public class OrderItemUpdateDto
    {
        public int? OrderItemId { get; set; } // Null for new items
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Computed property for total price
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
