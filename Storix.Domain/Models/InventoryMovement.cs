namespace Storix.Domain.Models
{
    public record InventoryMovement(
        int MovementId,
        int ProductId,
        int FromLocationId,
        int ToLocationId,
        int Quantity,
        string? Notes,
        int CreatedBy,
        DateTime CreatedDate );
}
