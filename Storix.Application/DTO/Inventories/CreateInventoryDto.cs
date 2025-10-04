using System;

namespace Storix.Application.DTO.Inventories
{
    public class CreateInventoryDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; } = 0;
        public DateTime LastUpdated { get; set; }
    }
}
