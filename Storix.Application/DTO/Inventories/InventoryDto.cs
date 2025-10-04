using System;

namespace Storix.Application.DTO.Inventories
{
    public class InventoryDto
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
