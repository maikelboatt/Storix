using System.Collections.Generic;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Products;

namespace Storix.Application.Managers.Interfaces
{
    /// <summary>
    /// Main print manager that coordinates all printing operations.
    /// </summary>
    public interface IPrintManager
    {
        // Product Printing
        void PrintProductDetails( ProductDto product,
            List<StockLocationDto> stockLocations,
            string categoryName,
            string supplierName,
            int totalStock,
            int availableStock,
            int reservedStock );

        void PrintProductLabel( ProductDto product );

        void PrintStockAdjustmentReceipt( int productId,
            string productName,
            string sku,
            string locationName,
            int oldStock,
            int newStock,
            int adjustment,
            string reason );

        // Category Printing
        void PrintCategoryDetails( CategoryDto category,
            string? parentCategoryName,
            List<SubcategoryInfo> subcategories,
            List<ProductSummary> products,
            int totalProducts,
            int totalSubcategories,
            decimal totalCategoryValue );

        // Order Printing
        void PrintOrderDetails( OrderDto order,
            string entityName,
            string locationName,
            string createdByName,
            List<OrderItemSummary> orderItems,
            decimal totalAmount );

        void PrintOrderReceipt( OrderDto order,
            string entityName,
            string locationName,
            List<OrderItemSummary> orderItems,
            decimal subtotal,
            decimal tax,
            decimal total );
    }
}
