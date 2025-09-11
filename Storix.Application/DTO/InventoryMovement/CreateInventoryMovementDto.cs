namespace Storix.Application.DTO.InventoryMovement
{
    public class CreateInventoryMovementDto
    {
        public int ProductId { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
