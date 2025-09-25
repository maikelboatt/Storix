using Storix.Application.DTO.Products;

namespace Storix.Application.DTO
{
    public class ProductWithDetailsDto:ProductDto
    {
        public string SupplierName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalStock { get; set; }
        public int AvailableStock { get; set; }
    }
}
