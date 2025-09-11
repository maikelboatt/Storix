namespace Storix.Domain.Models
{
    public record Category(
        int CategoryId,
        string Name,
        string? Description,
        int? ParentCategoryId );
}
