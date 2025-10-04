namespace Storix.Application.DTO.OrderItems
{
    public class UpdateOrderItemDto
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
