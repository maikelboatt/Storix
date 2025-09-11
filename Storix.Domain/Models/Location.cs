using Storix.Domain.Enums;

namespace Storix.Domain.Models
{
    public record Location(
        int LocationId,
        string Name,
        string? Description,
        LocationType Type,
        string? Address );
}
