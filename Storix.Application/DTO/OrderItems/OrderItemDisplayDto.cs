namespace Storix.Application.DTO.OrderItems
{
    public class OrderItemDisplayDto
    {
        public int OrderItemId { get; set; }
        public int ItemNumber { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSKU { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool HasSKU => !string.IsNullOrWhiteSpace(ProductSKU);
    }
}
