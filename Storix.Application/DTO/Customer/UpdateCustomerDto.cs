namespace Storix.Application.DTO.Customer
{
    public abstract class UpdateCustomerDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}
