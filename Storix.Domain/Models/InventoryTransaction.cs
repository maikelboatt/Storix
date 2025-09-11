using Storix.Domain.Enums;

namespace Storix.Domain.Models
{
    public record InventoryTransaction(
        int TransactionId,
        int ProductId,
        int LocationId,
        TransactionType Type,
        int Quantity,
        decimal? UnitCost,
        string? Reference,
        string? Notes,
        int CreatedBy,
        DateTime CreatedDate );
}
