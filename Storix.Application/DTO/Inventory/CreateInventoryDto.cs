namespace Storix.Application.DTO.Inventory
{
    public class CreateInventoryDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; } = 0;
    }
}
