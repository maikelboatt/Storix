using Storix.Domain.Enums;

namespace Storix.Domain.Models
{
    public record Order(
        int OrderId,
        OrderType Type,
        OrderStatus Status,
        int? SupplierId,
        int? CustomerId,
        DateTime OrderDate,
        DateTime? DeliveryDate,
        string? Notes,
        int CreatedBy )
    {
        // You can add computed properties
        public bool IsOverdue => DeliveryDate.HasValue && DeliveryDate < DateTime.UtcNow;
    }
}
