using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Services.Print.Interfaces;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Print
{
    public class OrderPrintService:BasePrintService, IOrderPrintService
    {
        public OrderPrintService( ILogger<OrderPrintService> logger ):base(logger)
        {
        }

        public void PrintOrderDetails( OrderDto order,
            string entityName,
            string locationName,
            string createdByName,
            List<OrderItemSummary> orderItems,
            decimal totalAmount )
        {
            try
            {
                Logger.LogInformation("🖨️ Printing order details for Order #{OrderId}", order.OrderId);

                FlowDocument flowDocument = CreateOrderDetailsDocument(
                    order,
                    entityName,
                    locationName,
                    createdByName,
                    orderItems,
                    totalAmount);

                PrintFlowDocument(flowDocument, $"Order Details - #{order.OrderId}");

                Logger.LogInformation("✅ Order details printed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Failed to print order details for order {OrderId}", order.OrderId);
                MessageBox.Show(
                    "Failed to print order details. Please try again.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void PrintOrderReceipt( OrderDto order,
            string entityName,
            string locationName,
            List<OrderItemSummary> orderItems,
            decimal subtotal,
            decimal tax,
            decimal total )
        {
            try
            {
                Logger.LogInformation("🖨️ Printing order receipt for Order #{OrderId}", order.OrderId);

                FlowDocument flowDocument = CreateOrderReceiptDocument(
                    order,
                    entityName,
                    locationName,
                    orderItems,
                    subtotal,
                    tax,
                    total);

                PrintFlowDocument(flowDocument, $"Order Receipt - #{order.OrderId}");

                Logger.LogInformation("✅ Order receipt printed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Failed to print order receipt");
                MessageBox.Show(
                    "Failed to print order receipt. Please try again.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region Document Creation

        private FlowDocument CreateOrderDetailsDocument( OrderDto order,
            string entityName,
            string locationName,
            string createdByName,
            List<OrderItemSummary> orderItems,
            decimal totalAmount )
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
                new Run($"{GetOrderTypeDisplay(order.Type)} Order Details")
                {
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold
                });
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(
                new Run($"Order #{order.OrderId}")
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold
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

            // Order Information Section
            document.Blocks.Add(CreateSectionHeader("Order Information"));

            Table orderTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            orderTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });
            orderTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            TableRowGroup orderGroup = new();
            orderGroup.Rows.Add(
                CreateInfoRow(
                    "Order Status:",
                    order.Status.ToString(),
                    true,
                    GetStatusColor(order.Status)));
            orderGroup.Rows.Add(CreateInfoRow(GetEntityLabel(order.Type), entityName));
            orderGroup.Rows.Add(CreateInfoRow("Location:", locationName));
            orderGroup.Rows.Add(CreateInfoRow("Order Date:", order.OrderDate.ToString("MMM dd, yyyy")));
            if (order.DeliveryDate.HasValue)
                orderGroup.Rows.Add(
                    CreateInfoRow(
                        "Delivery Date:",
                        order.DeliveryDate.Value.ToString("MMM dd, yyyy"),
                        false,
                        new SolidColorBrush(Color.FromRgb(16, 185, 129))));
            orderGroup.Rows.Add(CreateInfoRow("Created By:", createdByName));

            if (!string.IsNullOrEmpty(order.Notes))
                orderGroup.Rows.Add(CreateInfoRow("Notes:", order.Notes));

            orderTable.RowGroups.Add(orderGroup);
            document.Blocks.Add(orderTable);

            // Order Items Section
            if (orderItems != null && orderItems.Any())
            {
                document.Blocks.Add(CreateSectionHeader($"Order Items ({orderItems.Count} items)"));

                Table itemsTable = new()
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

                itemsTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(50)
                    }); // Line#
                itemsTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(3, GridUnitType.Star)
                    }); // Product
                itemsTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(2, GridUnitType.Star)
                    }); // SKU
                itemsTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    }); // Qty
                itemsTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    }); // Unit Price
                itemsTable.Columns.Add(
                    new TableColumn
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    }); // Total

                // Header row
                TableRowGroup itemsHeaderGroup = new();
                TableRow itemsHeaderRow = new()
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249))
                };
                itemsHeaderRow.Cells.Add(CreateHeaderCell("#"));
                itemsHeaderRow.Cells.Add(CreateHeaderCell("Product"));
                itemsHeaderRow.Cells.Add(CreateHeaderCell("SKU"));
                itemsHeaderRow.Cells.Add(CreateHeaderCell("Qty"));
                itemsHeaderRow.Cells.Add(CreateHeaderCell("Unit Price"));
                itemsHeaderRow.Cells.Add(CreateHeaderCell("Total"));
                itemsHeaderGroup.Rows.Add(itemsHeaderRow);
                itemsTable.RowGroups.Add(itemsHeaderGroup);

                // Data rows
                TableRowGroup itemsDataGroup = new();
                foreach (OrderItemSummary item in orderItems)
                {
                    TableRow row = new();
                    row.Cells.Add(CreateDataCell(item.LineNumber.ToString()));
                    row.Cells.Add(CreateDataCell(item.ProductName ?? "Unknown"));
                    row.Cells.Add(CreateDataCell(item.ProductSKU ?? "N/A"));
                    row.Cells.Add(CreateDataCell(item.Quantity.ToString(), new SolidColorBrush(Color.FromRgb(59, 130, 246))));
                    row.Cells.Add(CreateDataCell($"${item.UnitPrice:N2}"));
                    row.Cells.Add(CreateDataCell($"${item.LineTotal:N2}", new SolidColorBrush(Color.FromRgb(16, 185, 129))));

                    itemsDataGroup.Rows.Add(row);
                }
                itemsTable.RowGroups.Add(itemsDataGroup);
                document.Blocks.Add(itemsTable);
            }

            // Total Amount
            Table totalTable = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    20)
            };
            totalTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            totalTable.Columns.Add(
                new TableColumn
                {
                    Width = new GridLength(150)
                });

            TableRowGroup totalGroup = new();
            TableRow totalRow = new();
            totalRow.Cells.Add(
                new TableCell(
                    new Paragraph(new Run("TOTAL AMOUNT"))
                    {
                        Margin = new Thickness(0),
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        TextAlignment = TextAlignment.Right
                    })
                {
                    Padding = new Thickness(10)
                });
            totalRow.Cells.Add(
                new TableCell(
                    new Paragraph(new Run($"${totalAmount:N2}"))
                    {
                        Margin = new Thickness(0),
                        FontWeight = FontWeights.Bold,
                        FontSize = 18,
                        Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    })
                {
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush(Color.FromRgb(209, 250, 229))
                });
            totalGroup.Rows.Add(totalRow);
            totalTable.RowGroups.Add(totalGroup);
            document.Blocks.Add(totalTable);

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
            footer.Inlines.Add(new Run($"This order document was generated by STORIX Inventory Management System"));
            footer.Inlines.Add(new LineBreak());
            footer.Inlines.Add(new Run($"© {DateTime.Now.Year} STORIX. All rights reserved."));
            document.Blocks.Add(footer);

            return document;
        }

        private FlowDocument CreateOrderReceiptDocument( OrderDto order,
            string entityName,
            string locationName,
            List<OrderItemSummary> orderItems,
            decimal subtotal,
            decimal tax,
            decimal total )
        {
            // Similar structure to OrderDetails but formatted as a receipt
            // Compact format with emphasis on totals
            FlowDocument document = new()
            {
                PagePadding = new Thickness(30),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                PageWidth = 400 // Receipt size
            };

            // Receipt header
            Paragraph header = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    15),
                TextAlignment = TextAlignment.Center
            };
            header.Inlines.Add(
                new Run("STORIX")
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold
                });
            header.Inlines.Add(new LineBreak());
            header.Inlines.Add(
                new Run("Order Receipt")
                {
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold
                });
            header.Inlines.Add(new LineBreak());
            header.Inlines.Add(
                new Run($"#{order.OrderId}")
                {
                    FontSize = 12
                });
            header.Inlines.Add(new LineBreak());
            header.Inlines.Add(
                new Run(DateTime.Now.ToString("MMM dd, yyyy hh:mm tt"))
                {
                    FontSize = 9,
                    Foreground = Brushes.Gray
                });
            document.Blocks.Add(header);

            document.Blocks.Add(CreateSeparator());

            // Customer/Supplier and Location
            Paragraph infoPara = new()
            {
                Margin = new Thickness(
                    0,
                    0,
                    0,
                    10),
                FontSize = 10
            };
            infoPara.Inlines.Add(
                new Run($"{GetEntityLabel(order.Type)}: ")
                {
                    FontWeight = FontWeights.Bold
                });
            infoPara.Inlines.Add(new Run(entityName));
            infoPara.Inlines.Add(new LineBreak());
            infoPara.Inlines.Add(
                new Run("Location: ")
                {
                    FontWeight = FontWeights.Bold
                });
            infoPara.Inlines.Add(new Run(locationName));
            document.Blocks.Add(infoPara);

            document.Blocks.Add(CreateSeparator());

            // Items (compact)
            if (orderItems != null && orderItems.Any())
            {
                foreach (OrderItemSummary item in orderItems)
                {
                    Paragraph itemPara = new()
                    {
                        Margin = new Thickness(
                            0,
                            5,
                            0,
                            5),
                        FontSize = 10
                    };
                    itemPara.Inlines.Add(
                        new Run(item.ProductName ?? "Unknown")
                        {
                            FontWeight = FontWeights.SemiBold
                        });
                    itemPara.Inlines.Add(new LineBreak());
                    itemPara.Inlines.Add(new Run($"  {item.Quantity} x ${item.UnitPrice:N2}"));
                    itemPara.Inlines.Add(
                        new Run($" = ${item.LineTotal:N2}")
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                        });
                    document.Blocks.Add(itemPara);
                }
            }

            document.Blocks.Add(CreateSeparator());

            // Totals
            Paragraph totals = new()
            {
                Margin = new Thickness(0),
                FontSize = 11
            };
            totals.Inlines.Add(
                new Run("Subtotal: ")
                {
                    FontWeight = FontWeights.SemiBold
                });
            totals.Inlines.Add(new Run($"${subtotal:N2}"));
            totals.Inlines.Add(new LineBreak());
            totals.Inlines.Add(
                new Run("Tax: ")
                {
                    FontWeight = FontWeights.SemiBold
                });
            totals.Inlines.Add(new Run($"${tax:N2}"));
            totals.Inlines.Add(new LineBreak());
            totals.Inlines.Add(new LineBreak());
            totals.Inlines.Add(
                new Run("TOTAL: ")
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                });
            totals.Inlines.Add(
                new Run($"${total:N2}")
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                });
            document.Blocks.Add(totals);

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
                FontSize = 8,
                Foreground = Brushes.Gray
            };
            footer.Inlines.Add(new Run("Thank you for your business!"));
            footer.Inlines.Add(new LineBreak());
            footer.Inlines.Add(new Run($"STORIX © {DateTime.Now.Year}"));
            document.Blocks.Add(footer);

            return document;
        }

        #endregion

        #region Helper Methods

        private string GetOrderTypeDisplay( OrderType type ) => type == OrderType.Sale
            ? "Sales"
            : "Purchase";

        private string GetEntityLabel( OrderType type ) => type == OrderType.Sale
            ? "Customer"
            : "Supplier";

        private Brush GetStatusColor( OrderStatus status )
        {
            return status switch
            {
                OrderStatus.Draft     => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                OrderStatus.Active    => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                OrderStatus.Fulfilled => new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                OrderStatus.Completed => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                OrderStatus.Cancelled => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _                     => Brushes.Gray
            };
        }

        #endregion
    }
}
