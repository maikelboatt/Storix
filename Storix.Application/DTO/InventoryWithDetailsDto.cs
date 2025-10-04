using Storix.Application.DTO.Inventories;

namespace Storix.Application.DTO
{
    public class InventoryWithDetailsDto:InventoryDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public bool IsLowStock { get; set; }
    }
}
