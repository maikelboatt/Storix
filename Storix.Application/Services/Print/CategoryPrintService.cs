using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Print.Interfaces;

namespace Storix.Application.Services.Print
{
    public class CategoryPrintService:BasePrintService, ICategoryPrintService
    {
        public CategoryPrintService( ILogger<CategoryPrintService> logger ):base(logger)
        {
        }

        public void PrintCategoryDetails( CategoryDto category,
            string? parentCategoryName,
            List<SubcategoryInfo> subcategories,
            List<ProductSummary> products,
            int totalProducts,
            int totalSubcategories,
            decimal totalCategoryValue )
        {
            try
            {
                Logger.LogInformation(
                    "🖨️ Printing category details for: {CategoryName} (ID: {CategoryId})",
                    category.Name,
                    category.CategoryId);

                FlowDocument flowDocument = CreateCategoryDetailsDocument(
                    category,
                    parentCategoryName,
                    subcategories,
                    products,
                    totalProducts,
                    totalSubcategories,
                    totalCategoryValue);

                PrintFlowDocument(flowDocument, $"Category Details - {category.Name}");

                Logger.LogInformation("✅ Category details printed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Failed to print category details for category {CategoryId}", category.CategoryId);
                MessageBox.Show(
                    "Failed to print category details. Please try again.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateCategoryDetailsDocument(
            CategoryDto category,
            string? parentCategoryName,
            List<SubcategoryInfo> subcategories,
            List<ProductSummary> products,
            int totalProducts,
            int totalSubcategories,
            decimal totalCategoryValue )
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
                new Run("Category Details Report")
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

            // Category Information Section
            document.Blocks.Add(CreateSectionHeader("Category Information"));

            Table categoryTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            categoryTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            categoryTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup categoryGroup = new();
            categoryGroup.Rows.Add(CreateInfoRow("Category Name:", category.Name, true));
            categoryGroup.Rows.Add(CreateInfoRow("Category ID:", category.CategoryId.ToString()));

            if (!string.IsNullOrEmpty(parentCategoryName))
                categoryGroup.Rows.Add(
                    CreateInfoRow(
                        "Parent Category:",
                        parentCategoryName,
                        false,
                        new SolidColorBrush(Color.FromRgb(245, 158, 11))));
            else
                categoryGroup.Rows.Add(
                    CreateInfoRow(
                        "Category Type:",
                        "Parent Category",
                        false,
                        new SolidColorBrush(Color.FromRgb(59, 130, 246))));

            if (!string.IsNullOrEmpty(category.Description))
                categoryGroup.Rows.Add(CreateInfoRow("Description:", category.Description));

            categoryTable.RowGroups.Add(categoryGroup);
            document.Blocks.Add(categoryTable);

            // Statistics Section
            document.Blocks.Add(CreateSectionHeader("Category Statistics"));

            Table statsTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            statsTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            statsTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup statsGroup = new();
            statsGroup.Rows.Add(
                CreateInfoRow(
                    "Total Products:",
                    totalProducts.ToString(),
                    false,
                    new SolidColorBrush(Color.FromRgb(59, 130, 246))));
            statsGroup.Rows.Add(
                CreateInfoRow(
                    "Subcategories:",
                    totalSubcategories.ToString(),
                    false,
                    new SolidColorBrush(Color.FromRgb(16, 185, 129))));
            statsGroup.Rows.Add(
                CreateInfoRow(
                    "Total Inventory Value:",
                    $"${totalCategoryValue:N2}",
                    false,
                    new SolidColorBrush(Color.FromRgb(239, 68, 68))));

            statsTable.RowGroups.Add(statsGroup);
            document.Blocks.Add(statsTable);

            // Subcategories Section
            if (subcategories != null && subcategories.Any())
            {
                document.Blocks.Add(CreateSectionHeader("Subcategories"));

                Table subcategoryTable = new()
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

                subcategoryTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(3, GridUnitType.Star)
                    });
                subcategoryTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                subcategoryTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(4, GridUnitType.Star)
                    });

                // Header row
                TableRowGroup subcatHeaderGroup = new();
                TableRow subcatHeaderRow = new()
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249))
                };
                subcatHeaderRow.Cells.Add(CreateHeaderCell("Subcategory Name"));
                subcatHeaderRow.Cells.Add(CreateHeaderCell("Products"));
                subcatHeaderRow.Cells.Add(CreateHeaderCell("Description"));
                subcatHeaderGroup.Rows.Add(subcatHeaderRow);
                subcategoryTable.RowGroups.Add(subcatHeaderGroup);

                // Data rows
                TableRowGroup subcatDataGroup = new();
                foreach (SubcategoryInfo subcategory in subcategories.OrderBy(s => s.Name))
                {
                    TableRow row = new();
                    row.Cells.Add(CreateDataCell(subcategory.Name ?? "Unknown"));
                    row.Cells.Add(CreateDataCell(subcategory.ProductCount.ToString(), new SolidColorBrush(Color.FromRgb(59, 130, 246))));
                    row.Cells.Add(CreateDataCell(subcategory.Description ?? "-"));

                    subcatDataGroup.Rows.Add(row);
                }
                subcategoryTable.RowGroups.Add(subcatDataGroup);
                document.Blocks.Add(subcategoryTable);
            }

            // Products Section
            if (products != null && products.Any())
            {
                document.Blocks.Add(CreateSectionHeader($"Products in Category (Showing {products.Count} of {totalProducts})"));

                Table productTable = new()
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

                productTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(3, GridUnitType.Star)
                    });
                productTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(2, GridUnitType.Star)
                    });
                productTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                productTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });

                // Header row
                TableRowGroup productHeaderGroup = new();
                TableRow productHeaderRow = new()
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249))
                };
                productHeaderRow.Cells.Add(CreateHeaderCell("Product Name"));
                productHeaderRow.Cells.Add(CreateHeaderCell("SKU"));
                productHeaderRow.Cells.Add(CreateHeaderCell("Stock"));
                productHeaderRow.Cells.Add(CreateHeaderCell("Price"));
                productHeaderGroup.Rows.Add(productHeaderRow);
                productTable.RowGroups.Add(productHeaderGroup);

                // Data rows
                TableRowGroup productDataGroup = new();
                foreach (ProductSummary product in products.OrderBy(p => p.Name))
                {
                    TableRow row = new();
                    row.Cells.Add(CreateDataCell(product.Name ?? "Unknown"));
                    row.Cells.Add(CreateDataCell(product.SKU ?? "N/A"));
                    row.Cells.Add(
                        CreateDataCell(
                            product.Stock.ToString(),
                            product.Stock > 0
                                ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                                : new SolidColorBrush(Color.FromRgb(239, 68, 68))));
                    row.Cells.Add(CreateDataCell($"${product.Price:N2}", new SolidColorBrush(Color.FromRgb(16, 185, 129))));

                    productDataGroup.Rows.Add(row);
                }
                productTable.RowGroups.Add(productDataGroup);
                document.Blocks.Add(productTable);

                // Note if there are more products
                if (totalProducts > products.Count)
                {
                    Paragraph noteParagraph = new()
                    {
                        Margin = new Thickness(
                            0,
                            0,
                            0,
                            20),
                        FontSize = 11,
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Gray
                    };
                    noteParagraph.Inlines.Add(new Run($"Note: Only showing first {products.Count} products. Total products in category: {totalProducts}"));
                    document.Blocks.Add(noteParagraph);
                }
            }

            // Summary Section
            document.Blocks.Add(CreateSeparator());

            Paragraph summaryParagraph = new()
            {
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20),
                FontSize = 12
            };
            summaryParagraph.Inlines.Add(
                new Run("Category Summary")
                {
                    FontWeight = FontWeights.Bold
                });
            summaryParagraph.Inlines.Add(new LineBreak());
            summaryParagraph.Inlines.Add(new LineBreak());
            summaryParagraph.Inlines.Add(
                new Run(
                    $"This category contains {totalProducts} product(s) across {totalSubcategories} subcategory(ies) with a total inventory value of ${totalCategoryValue:N2}."));

            if (!string.IsNullOrEmpty(parentCategoryName))
            {
                summaryParagraph.Inlines.Add(new LineBreak());
                summaryParagraph.Inlines.Add(new Run($"This is a subcategory of '{parentCategoryName}'."));
            }
            else if (totalSubcategories > 0)
            {
                summaryParagraph.Inlines.Add(new LineBreak());
                summaryParagraph.Inlines.Add(new Run($"As a parent category, it organizes products into {totalSubcategories} subcategory(ies)."));
            }

            document.Blocks.Add(summaryParagraph);

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
    }
}
