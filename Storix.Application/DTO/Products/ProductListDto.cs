namespace Storix.Application.DTO.Products
{
    /// <summary>
    /// Lightweight DTO for displaying products in lists/grids.
    /// Includes only essential display information.
    /// </summary>
    public record ProductListDto
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string SKU { get; init; } = string.Empty;
        public string? Barcode { get; init; }
        public decimal Price { get; init; }
        public decimal Cost { get; init; }
        public int MinStockLevel { get; init; }
        public int MaxStockLevel { get; init; }
        public string? CategoryName { get; init; } // Joined data
        public string? SupplierName { get; init; } // Joined data
        public int CurrentStock { get; init; }     // Aggregated data
        public bool IsLowStock { get; init; }      // Computed
        public bool IsDeleted { get; init; }
    }
}
