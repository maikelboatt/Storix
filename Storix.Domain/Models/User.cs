namespace Storix.Domain.Models
{
    public record User(
        int UserId,
        string Username,
        string PasswordHash,
        string Role,
        string? FullName,
        string? Email,
        bool IsActive );
}
