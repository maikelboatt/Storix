using Storix.Domain.Enums;
using Storix.Domain.Interfaces;

namespace Storix.Domain.Models
{
    public record Location(
        int LocationId,
        string Name,
        string? Description,
        LocationType Type,
        string? Address,
        bool IsDeleted = false,
        DateTime? DeletedAt = null ):ISoftDeletable;
}
