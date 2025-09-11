namespace Storix.Domain.Models
{
    public record Inventory(
        int InventoryId,
        int ProductId,
        int LocationId,
        int CurrentStock,
        int ReservedStock,
        DateTime LastUpdated )
    {
        public int AvailableStock => CurrentStock - ReservedStock;
        public bool IsInStock => AvailableStock > 0;
    }
}
