namespace Storix.Domain.Models
{
    public record OrderItem(
        int OrderItemId,
        int OrderId,
        int ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice )
    {
        // Business logic for validation
        public bool IsValidTotal => TotalPrice == UnitPrice * Quantity;
    }
}
