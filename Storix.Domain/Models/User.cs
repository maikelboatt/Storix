using Storix.Domain.Interfaces;

namespace Storix.Domain.Models
{
    public record User(
        int UserId,
        string Username,
        string PasswordHash,
        string Role,
        string? FullName,
        string? Email,
        bool IsActive,
        bool IsDeleted = false,
        DateTime? DeletedAt = null ):ISoftDeletable;
}
