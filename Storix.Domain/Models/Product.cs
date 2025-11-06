using Storix.Domain.Interfaces;

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
        DateTime CreatedDate,
        DateTime? UpdatedDate = null,
        bool IsDeleted = false,
        DateTime? DeletedAt = null
    ):ISoftDeletable
    {
        public decimal ProfitMargin => Price - Cost;


        public bool IsLowStock( int currentStock ) => currentStock <= MinStockLevel;
    }
}
