using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Print.Interfaces;

namespace Storix.Application.Services.Print
{
    public class ProductPrintService:BasePrintService, IProductPrintService
    {
        public ProductPrintService( ILogger<ProductPrintService> logger ):base(logger)
        {
        }

        public void PrintProductDetails( ProductDto product,
            List<StockLocationDto> stockLocations,
            string categoryName,
            string supplierName,
            int totalStock,
            int availableStock,
            int reservedStock )
        {
            try
            {
                Logger.LogInformation(
                    "🖨️ Printing product details for: {ProductName} (ID: {ProductId})",
                    product.Name,
                    product.ProductId);

                FlowDocument flowDocument = CreateProductDetailsDocument(
                    product,
                    stockLocations,
                    categoryName,
                    supplierName,
                    totalStock,
                    availableStock,
                    reservedStock);

                PrintFlowDocument(flowDocument, $"Product Details - {product.Name}");

                Logger.LogInformation("✅ Product details printed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Failed to print product details for product {ProductId}", product.ProductId);
                MessageBox.Show(
                    "Failed to print product details. Please try again.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region Stock Adjustment Receipt Print

        public void PrintStockAdjustmentReceipt( int productId,
            string productName,
            string sku,
            string locationName,
            int oldStock,
            int newStock,
            int adjustment,
            string reason )
        {
            try
            {
                Logger.LogInformation("🖨️ Printing stock adjustment receipt for product: {ProductName}", productName);

                FlowDocument flowDocument = CreateStockAdjustmentDocument(
                    productId,
                    productName,
                    sku,
                    locationName,
                    oldStock,
                    newStock,
                    adjustment,
                    reason);

                PrintFlowDocument(flowDocument, $"Stock Adjustment - {productName}");

                Logger.LogInformation("✅ Stock adjustment receipt printed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Failed to print stock adjustment receipt");
                MessageBox.Show(
                    "Failed to print adjustment receipt. Please try again.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateStockAdjustmentDocument(
            int productId,
            string productName,
            string sku,
            string locationName,
            int oldStock,
            int newStock,
            int adjustment,
            string reason )
        {
            FlowDocument document = new()
            {
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            // Header
            Paragraph headerParagraph = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    20),
                TextAlignment = TextAlignment.Center
            };
            headerParagraph.Inlines.Add(
                new Run("STORIX")
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                });
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(
                new Run("Stock Adjustment Receipt")
                {
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold
                });
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(
                new Run($"Date: {DateTime.Now:MMM dd, yyyy - hh:mm tt}")
                {
                    FontSize = 10,
                    Foreground = Brushes.Gray
                });
            document.Blocks.Add(headerParagraph);

            document.Blocks.Add(CreateSeparator());

            // Product Info
            Table infoTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            infoTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            infoTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup infoGroup = new();
            infoGroup.Rows.Add(CreateInfoRow("Product:", productName, true));
            infoGroup.Rows.Add(CreateInfoRow("SKU:", sku));
            infoGroup.Rows.Add(CreateInfoRow("Product ID:", productId.ToString()));
            infoGroup.Rows.Add(CreateInfoRow("Location:", locationName));

            infoTable.RowGroups.Add(infoGroup);
            document.Blocks.Add(infoTable);

            // Adjustment Details
            Paragraph adjustmentPara = new()
            {
                Margin = new Thickness(
                    0,
                    20,
                    0,
                    10),
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            adjustmentPara.Inlines.Add(new Run("Adjustment Details"));
            document.Blocks.Add(adjustmentPara);

            Table adjustmentTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            adjustmentTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            adjustmentTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup adjustmentGroup = new();
            adjustmentGroup.Rows.Add(CreateInfoRow("Previous Stock:", oldStock.ToString()));

            SolidColorBrush adjustmentColor = adjustment > 0
                ? Brushes.Green
                : Brushes.Red;
            string adjustmentText = adjustment > 0
                ? $"+{adjustment}"
                : adjustment.ToString();
            adjustmentGroup.Rows.Add(
                CreateInfoRow(
                    "Adjustment:",
                    adjustmentText,
                    false,
                    adjustmentColor));

            adjustmentGroup.Rows.Add(
                CreateInfoRow(
                    "New Stock:",
                    newStock.ToString(),
                    true,
                    Brushes.Blue));

            adjustmentTable.RowGroups.Add(adjustmentGroup);
            document.Blocks.Add(adjustmentTable);

            // Reason
            if (!string.IsNullOrEmpty(reason))
            {
                Paragraph reasonPara = new()
                {
                    Margin = new Thickness(
                        0,
                        10,
                        0,
                        10),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                reasonPara.Inlines.Add(new Run("Reason:"));
                document.Blocks.Add(reasonPara);

                Paragraph reasonTextPara = new()
                {
                    Margin = new Thickness(
                        20,
                        0,
                        0,
                        20),
                    FontSize = 11,
                    Foreground = Brushes.DarkSlateGray
                };
                reasonTextPara.Inlines.Add(new Run(reason));
                document.Blocks.Add(reasonTextPara);
            }

            document.Blocks.Add(CreateSeparator());

            // Footer
            Paragraph footer = new()
            {
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    0),
                TextAlignment = TextAlignment.Center,
                FontSize = 9,
                Foreground = Brushes.Gray
            };
            footer.Inlines.Add(new Run("This is an official stock adjustment record"));
            document.Blocks.Add(footer);

            return document;
        }

        #endregion

        private FlowDocument CreateProductDetailsDocument(
            ProductDto product,
            List<StockLocationDto> stockLocations,
            string categoryName,
            string supplierName,
            int totalStock,
            int availableStock,
            int reservedStock )
        {
            FlowDocument document = new()
            {
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            // Header
            Paragraph headerParagraph = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    20),
                TextAlignment = TextAlignment.Center
            };
            headerParagraph.Inlines.Add(
                new Run("STORIX")
                {
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                });
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(
                new Run("Product Details Report")
                {
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold
                });
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(
                new Run($"Generated: {DateTime.Now:MMM dd, yyyy - hh:mm tt}")
                {
                    FontSize = 10,
                    Foreground = Brushes.Gray
                });
            document.Blocks.Add(headerParagraph);

            // Separator
            document.Blocks.Add(CreateSeparator());

            // Product Information Section
            document.Blocks.Add(CreateSectionHeader("Product Information"));

            Table productTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            productTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            productTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup productGroup = new();
            productGroup.Rows.Add(CreateInfoRow("Product Name:", product.Name, true));
            productGroup.Rows.Add(CreateInfoRow("SKU:", product.SKU));
            productGroup.Rows.Add(CreateInfoRow("Product ID:", product.ProductId.ToString()));
            if (!string.IsNullOrEmpty(product.Barcode))
                productGroup.Rows.Add(CreateInfoRow("Barcode:", product.Barcode));
            if (!string.IsNullOrEmpty(product.Description))
                productGroup.Rows.Add(CreateInfoRow("Description:", product.Description));

            productTable.RowGroups.Add(productGroup);
            document.Blocks.Add(productTable);

            // Pricing Section
            document.Blocks.Add(CreateSectionHeader("Pricing Information"));

            Table pricingTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            pricingTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            pricingTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup pricingGroup = new();
            pricingGroup.Rows.Add(
                CreateInfoRow(
                    "Selling Price:",
                    $"${product.Price:N2}",
                    false,
                    Brushes.Green));
            pricingGroup.Rows.Add(
                CreateInfoRow(
                    "Cost Price:",
                    $"${product.Cost:N2}",
                    false,
                    Brushes.Orange));
            pricingGroup.Rows.Add(
                CreateInfoRow(
                    "Profit Margin:",
                    $"${product.Price - product.Cost:N2}",
                    false,
                    Brushes.Purple));
            pricingGroup.Rows.Add(
                CreateInfoRow(
                    "Total Stock Value:",
                    $"${totalStock * product.Cost:N2}",
                    false,
                    Brushes.Purple));

            pricingTable.RowGroups.Add(pricingGroup);
            document.Blocks.Add(pricingTable);

            // Stock Summary Section
            document.Blocks.Add(CreateSectionHeader("Stock Summary"));

            Table stockTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            stockTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            stockTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup stockGroup = new();
            stockGroup.Rows.Add(
                CreateInfoRow(
                    "Total Stock:",
                    totalStock.ToString(),
                    false,
                    Brushes.Blue));
            stockGroup.Rows.Add(
                CreateInfoRow(
                    "Available Stock:",
                    availableStock.ToString(),
                    false,
                    Brushes.Green));
            stockGroup.Rows.Add(
                CreateInfoRow(
                    "Reserved Stock:",
                    reservedStock.ToString(),
                    false,
                    Brushes.Orange));
            stockGroup.Rows.Add(
                CreateInfoRow(
                    "Min Stock Level:",
                    product.MinStockLevel.ToString(),
                    false,
                    Brushes.Red));
            stockGroup.Rows.Add(
                CreateInfoRow(
                    "Max Stock Level:",
                    product.MaxStockLevel.ToString(),
                    false,
                    Brushes.Green));

            stockTable.RowGroups.Add(stockGroup);
            document.Blocks.Add(stockTable);

            // Stock by Location Section
            if (stockLocations != null && stockLocations.Any())
            {
                document.Blocks.Add(CreateSectionHeader("Stock by Location"));

                Table locationTable = new()
                {
                    CellSpacing = 0,
                    Margin = new Thickness(
                        0,
                        10,
                        0,
                        20),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1)
                };

                locationTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(2, GridUnitType.Star)
                    });
                locationTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                locationTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                locationTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                locationTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });

                // Header row
                TableRowGroup headerGroup = new();
                TableRow headerRow = new()
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249))
                };
                headerRow.Cells.Add(CreateHeaderCell("Location"));
                headerRow.Cells.Add(CreateHeaderCell("Current Stock"));
                headerRow.Cells.Add(CreateHeaderCell("Available"));
                headerRow.Cells.Add(CreateHeaderCell("Reserved"));
                headerRow.Cells.Add(CreateHeaderCell("Status"));
                headerGroup.Rows.Add(headerRow);
                locationTable.RowGroups.Add(headerGroup);

                // Data rows
                TableRowGroup dataGroup = new();
                foreach (StockLocationDto location in stockLocations.OrderByDescending(l => l.CurrentStock))
                {
                    TableRow row = new();
                    row.Cells.Add(CreateDataCell(location.LocationName ?? "Unknown"));
                    row.Cells.Add(CreateDataCell(location.CurrentStock.ToString()));
                    row.Cells.Add(CreateDataCell(location.AvailableStock.ToString()));
                    row.Cells.Add(CreateDataCell(location.ReservedStock.ToString()));

                    string status = location.CurrentStock == 0 ? "Out of Stock" :
                        location.IsLowStock ? "Low Stock" : "In Stock";
                    SolidColorBrush statusColor = location.CurrentStock == 0 ? Brushes.Red :
                        location.IsLowStock ? Brushes.Orange : Brushes.Green;
                    row.Cells.Add(CreateDataCell(status, statusColor));

                    dataGroup.Rows.Add(row);
                }
                locationTable.RowGroups.Add(dataGroup);
                document.Blocks.Add(locationTable);
            }

            // Related Information Section
            document.Blocks.Add(CreateSectionHeader("Related Information"));

            Table relatedTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            relatedTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            relatedTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup relatedGroup = new();
            relatedGroup.Rows.Add(CreateInfoRow("Category:", categoryName));
            relatedGroup.Rows.Add(CreateInfoRow("Supplier:", supplierName));
            relatedGroup.Rows.Add(CreateInfoRow("Created Date:", product.CreatedDate.ToString("MMM dd, yyyy - hh:mm tt")));
            if (product.UpdatedDate.HasValue)
                relatedGroup.Rows.Add(CreateInfoRow("Last Updated:", product.UpdatedDate.Value.ToString("MMM dd, yyyy - hh:mm tt")));

            relatedTable.RowGroups.Add(relatedGroup);
            document.Blocks.Add(relatedTable);

            // Footer
            document.Blocks.Add(CreateSeparator());
            Paragraph footer = new()
            {
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    0),
                TextAlignment = TextAlignment.Center,
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            footer.Inlines.Add(new Run($"This report was generated by STORIX Inventory Management System"));
            footer.Inlines.Add(new LineBreak());
            footer.Inlines.Add(new Run($"© {DateTime.Now.Year} STORIX. All rights reserved."));
            document.Blocks.Add(footer);

            return document;
        }

        #region Product Label Print

        /// <summary>
        /// Prints a product label (e.g., for barcode/shelf label)
        /// </summary>
        public void PrintProductLabel( ProductDto product )
        {
            try
            {
                Logger.LogInformation("🖨️ Printing product label for: {ProductName}", product.Name);

                FlowDocument flowDocument = CreateProductLabelDocument(product);
                PrintFlowDocument(flowDocument, $"Product Label - {product.SKU}");

                Logger.LogInformation("✅ Product label printed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Failed to print product label");
                MessageBox.Show(
                    "Failed to print product label. Please try again.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateProductLabelDocument( ProductDto product )
        {
            FlowDocument document = new()
            {
                PagePadding = new Thickness(20),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                PageHeight = 200,
                PageWidth = 400
            };

            // Product name
            Paragraph nameParagraph = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    10),
                TextAlignment = TextAlignment.Center
            };
            nameParagraph.Inlines.Add(
                new Run(product.Name)
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold
                });
            document.Blocks.Add(nameParagraph);

            // SKU
            Paragraph skuParagraph = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    10),
                TextAlignment = TextAlignment.Center
            };
            skuParagraph.Inlines.Add(
                new Run($"SKU: {product.SKU}")
                {
                    FontSize = 14,
                    FontFamily = new FontFamily("Consolas")
                });
            document.Blocks.Add(skuParagraph);

            // Barcode (if available)
            if (!string.IsNullOrEmpty(product.Barcode))
            {
                Paragraph barcodeParagraph = new()
                {
                    Margin = new Thickness(
                        0,
                        0,
                        0,
                        10),
                    TextAlignment = TextAlignment.Center
                };
                barcodeParagraph.Inlines.Add(
                    new Run(product.Barcode)
                    {
                        FontSize = 20,
                        FontFamily = new FontFamily("Libre Barcode 128"),
                        FontWeight = FontWeights.Bold
                    });
                document.Blocks.Add(barcodeParagraph);
            }

            // Price
            Paragraph priceParagraph = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    0),
                TextAlignment = TextAlignment.Center
            };
            priceParagraph.Inlines.Add(
                new Run($"${product.Price:N2}")
                {
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Green
                });
            document.Blocks.Add(priceParagraph);

            return document;
        }

        #endregion
    }
}
