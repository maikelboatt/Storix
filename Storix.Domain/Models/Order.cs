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
        public bool IsPending => Status == OrderStatus.Pending;

        // If Order needs complex behavior, consider making it a class instead
        // and having OrderService handle the business logic
    }
}
