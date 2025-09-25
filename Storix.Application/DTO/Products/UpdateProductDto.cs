namespace Storix.Application.DTO.Products
{
    public class UpdateProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public int SupplierId { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; }
    }
}
