namespace Storix.Application.DTO.Inventory
{
    public abstract class UpdateInventoryDto
    {
        public int InventoryId { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; }
    }
}
