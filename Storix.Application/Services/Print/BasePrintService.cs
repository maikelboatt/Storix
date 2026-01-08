using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace Storix.Application.Services.Print
{
    /// <summary>
    /// Base class for all print services with common printing functionality.
    /// </summary>
    public abstract class BasePrintService
    {
        protected readonly ILogger Logger;

        protected BasePrintService( ILogger logger )
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Common Print Methods

        protected void PrintFlowDocument( FlowDocument document, string documentName )
        {
            PrintDialog printDialog = new();

            if (printDialog.ShowDialog() == true)
            {
                document.PageHeight = printDialog.PrintableAreaHeight;
                document.PageWidth = printDialog.PrintableAreaWidth;

                IDocumentPaginatorSource idpSource = document;
                printDialog.PrintDocument(idpSource.DocumentPaginator, documentName);
            }
        }

        protected Paragraph CreateSectionHeader( string text )
        {
            Paragraph paragraph = new()
            {
                Margin = new Thickness(
                    0,
                    10,
                    0,
                    10),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };
            paragraph.Inlines.Add(new Run(text));

            return paragraph;
        }

        protected Paragraph CreateSeparator() => new()
        {
            Margin = new Thickness(
                0,
                10,
                0,
                10),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(
                0,
                0,
                0,
                1)
        };

        protected TableRow CreateInfoRow( string label,
            string value,
            bool bold = false,
            Brush? valueColor = null )
        {
            TableRow row = new();

            TableCell labelCell = new(
                new Paragraph(new Run(label))
                {
                    Margin = new Thickness(
                        0,
                        5,
                        0,
                        5),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Gray
                })
            {
                Padding = new Thickness(5)
            };

            TableCell valueCell = new(
                new Paragraph(new Run(value))
                {
                    Margin = new Thickness(
                        0,
                        5,
                        0,
                        5),
                    FontWeight = bold
                        ? FontWeights.Bold
                        : FontWeights.Normal,
                    Foreground = valueColor ?? Brushes.Black
                })
            {
                Padding = new Thickness(5)
            };

            row.Cells.Add(labelCell);
            row.Cells.Add(valueCell);

            return row;
        }

        protected TableCell CreateHeaderCell( string text ) => new(
            new Paragraph(new Run(text))
            {
                Margin = new Thickness(0),
                FontWeight = FontWeights.Bold,
                FontSize = 11
            })
        {
            Padding = new Thickness(
                8,
                5,
                8,
                5),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(
                0,
                0,
                1,
                1)
        };

        protected TableCell CreateDataCell( string text, Brush? foreground = null ) => new(
            new Paragraph(new Run(text))
            {
                Margin = new Thickness(0),
                FontSize = 11,
                Foreground = foreground ?? Brushes.Black
            })
        {
            Padding = new Thickness(
                8,
                5,
                8,
                5),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(
                0,
                0,
                1,
                1)
        };

        #endregion
    }
}
