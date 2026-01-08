using System.Collections.Generic;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Print
{
    /// <summary>
    /// Service for printing product details and reports
    /// </summary>
    public interface IPrintService
    {
        void PrintProductDetails( ProductDto product,
            List<StockLocationDto> stockLocations,
            string categoryName,
            string supplierName,
            int totalStock,
            int availableStock,
            int reservedStock );

        void PrintCategoryDetails(
            CategoryDto category,
            string? parentCategoryName,
            List<SubcategoryInfo> subcategories,
            List<ProductSummary> products,
            int totalProducts,
            int totalSubcategories,
            decimal totalCategoryValue
        );

        void PrintProductLabel( ProductDto product );

        void PrintStockAdjustmentReceipt( int productId,
            string productName,
            string sku,
            string locationName,
            int oldStock,
            int newStock,
            int adjustment,
            string reason );
    }
}
