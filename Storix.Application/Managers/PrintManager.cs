using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Products;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Print.Interfaces;

namespace Storix.Application.Managers
{
    /// <summary>
    /// Main print manager that delegates to specialized print services.
    /// </summary>
    public class PrintManager:IPrintManager
    {
        private readonly IProductPrintService _productPrintService;
        private readonly ICategoryPrintService _categoryPrintService;
        private readonly IOrderPrintService _orderPrintService;
        private readonly ILogger<PrintManager> _logger;

        public PrintManager(
            IProductPrintService productPrintService,
            ICategoryPrintService categoryPrintService,
            IOrderPrintService orderPrintService,
            ILogger<PrintManager> logger )
        {
            _productPrintService = productPrintService ?? throw new ArgumentNullException(nameof(productPrintService));
            _categoryPrintService = categoryPrintService ?? throw new ArgumentNullException(nameof(categoryPrintService));
            _orderPrintService = orderPrintService ?? throw new ArgumentNullException(nameof(orderPrintService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Product Printing

        public void PrintProductDetails( ProductDto product,
            List<StockLocationDto> stockLocations,
            string categoryName,
            string supplierName,
            int totalStock,
            int availableStock,
            int reservedStock )
        {
            _logger.LogInformation("🖨️ PrintManager: Delegating product details print to ProductPrintService");

            _productPrintService.PrintProductDetails(
                product,
                stockLocations,
                categoryName,
                supplierName,
                totalStock,
                availableStock,
                reservedStock);
        }

        public void PrintProductLabel( ProductDto product )
        {
            _logger.LogInformation("🖨️ PrintManager: Delegating product label print to ProductPrintService");
            _productPrintService.PrintProductLabel(product);
        }

        public void PrintStockAdjustmentReceipt( int productId,
            string productName,
            string sku,
            string locationName,
            int oldStock,
            int newStock,
            int adjustment,
            string reason )
        {
            _logger.LogInformation("🖨️ PrintManager: Delegating stock adjustment receipt to ProductPrintService");
            _productPrintService.PrintStockAdjustmentReceipt(
                productId,
                productName,
                sku,
                locationName,
                oldStock,
                newStock,
                adjustment,
                reason);
        }

        #endregion

        #region Category Printing

        public void PrintCategoryDetails( CategoryDto category,
            string? parentCategoryName,
            List<SubcategoryInfo> subcategories,
            List<ProductSummary> products,
            int totalProducts,
            int totalSubcategories,
            decimal totalCategoryValue )
        {
            _logger.LogInformation("🖨️ PrintManager: Delegating category details print to CategoryPrintService");
            _categoryPrintService.PrintCategoryDetails(
                category,
                parentCategoryName,
                subcategories,
                products,
                totalProducts,
                totalSubcategories,
                totalCategoryValue);
        }

        #endregion

        #region Order Printing

        public void PrintOrderDetails( OrderDto order,
            string entityName,
            string locationName,
            string createdByName,
            List<OrderItemSummary> orderItems,
            decimal totalAmount )
        {
            _logger.LogInformation("🖨️ PrintManager: Delegating order details print to OrderPrintService");
            _orderPrintService.PrintOrderDetails(
                order,
                entityName,
                locationName,
                createdByName,
                orderItems,
                totalAmount);
        }

        public void PrintOrderReceipt( OrderDto order,
            string entityName,
            string locationName,
            List<OrderItemSummary> orderItems,
            decimal subtotal,
            decimal tax,
            decimal total )
        {
            _logger.LogInformation("🖨️ PrintManager: Delegating order receipt print to OrderPrintService");
            _orderPrintService.PrintOrderReceipt(
                order,
                entityName,
                locationName,
                orderItems,
                subtotal,
                tax,
                total);
        }

        #endregion
    }
}
