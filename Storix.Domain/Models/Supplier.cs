namespace Storix.Domain.Models
{
    public record Supplier(
        int SupplierId,
        string Name,
        string Email,
        string Phone,
        string Address );
}
