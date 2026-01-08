using System.Collections.Generic;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Print.Interfaces
{
    public interface IProductPrintService
    {
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
    }
}
