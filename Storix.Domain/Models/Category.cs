using Storix.Domain.Interfaces;

namespace Storix.Domain.Models
{
    public record Category(
        int CategoryId,
        string Name,
        string? Description,
        int? ParentCategoryId,
        string? ImageUrl,
        bool IsDeleted = false,
        DateTime? DeletedAt = null ):ISoftDeletable;
}
