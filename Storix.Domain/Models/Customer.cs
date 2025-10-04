using Storix.Domain.Interfaces;

namespace Storix.Domain.Models
{
    public record Customer(
        int CustomerId,
        string Name,
        string? Email,
        string? Phone,
        string? Address,
        bool IsDeleted = false,
        DateTime? DeletedAt = null ):ISoftDeletable;
}
