namespace Storix.Domain.Models
{
    public record Customer(
        int CustomerId,
        string Name,
        string? Email,
        string? Phone,
        string? Address,
        bool IsActive );
}
