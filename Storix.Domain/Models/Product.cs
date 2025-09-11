namespace Storix.Domain.Models
{
    public record Product(
        int ProductId,
        string Name,
        string SKU,
        string Description,
        string? Barcode,
        decimal Price,
        decimal Cost,
        int MinStockLevel,
        int MaxStockLevel,
        int SupplierId,
        int CategoryId,
        bool IsActive,
        DateTime CreatedDate,
        DateTime? UpdatedDate = null )
    {
        public decimal ProfitMargin => Price - Cost;

        // You can still add business logic methods if needed
        public bool IsLowStock( int currentStock ) => currentStock <= MinStockLevel;
    }
}
