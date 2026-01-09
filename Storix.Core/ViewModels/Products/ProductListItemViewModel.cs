using System;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;

namespace Storix.Core.ViewModels.Products
{
    /// <summary>
    /// ViewModel wrapper for ProductListDto with UI-specific functionality.
    /// CurrentStock can be updated independently to reflect real-time inventory changes.
    /// </summary>
    public class ProductListItemViewModel:MvxNotifyPropertyChanged
    {
        private readonly ProductListDto _product;
        private bool _isSelected;
        private int _currentStock; // ⭐ CHANGED: Now a backing field instead of pass-through

        public ProductListItemViewModel( ProductListDto product )
        {
            _product = product ?? throw new ArgumentNullException(nameof(product));
            _currentStock = product.CurrentStock; // ⭐ Initialize from DTO
        }

        #region Product Properties

        public int ProductId => _product.ProductId;
        public string Name => _product.Name;
        public string SKU => _product.SKU;
        public string? Barcode => _product.Barcode;
        public decimal Price => _product.Price;
        public decimal Cost => _product.Cost;
        public int MinStockLevel => _product.MinStockLevel;
        public int MaxStockLevel => _product.MaxStockLevel;
        public string? CategoryName => _product.CategoryName;
        public string? SupplierName => _product.SupplierName;

        /// <summary>
        /// ⭐ CHANGED: Current stock - now updatable to reflect real-time inventory changes.
        /// Updates trigger UI refresh for dependent properties (IsLowStock, StockStatus, etc.)
        /// </summary>
        public int CurrentStock
        {
            get => _currentStock;
            set
            {
                if (SetProperty(ref _currentStock, value))
                {
                    // Notify dependent properties
                    RaisePropertyChanged(nameof(IsLowStock));
                    RaisePropertyChanged(nameof(IsOutOfStock));
                    RaisePropertyChanged(nameof(StockStatus));
                    RaisePropertyChanged(nameof(StockStatusColor));
                }
            }
        }

        /// <summary>
        /// Whether product stock is below minimum level.
        /// Recalculates when CurrentStock changes.
        /// </summary>
        public bool IsLowStock => CurrentStock > 0 && CurrentStock < MinStockLevel;

        /// <summary>
        /// ⭐ NEW: Whether product is completely out of stock.
        /// </summary>
        public bool IsOutOfStock => CurrentStock == 0;

        /// <summary>
        /// ⭐ NEW: Stock status text for display.
        /// </summary>
        public string StockStatus => CurrentStock switch
        {
            0                            => "Out of Stock",
            var s when s < MinStockLevel => "Low Stock",
            var s when s > MaxStockLevel => "Overstocked",
            _                            => "In Stock"
        };

        /// <summary>
        /// ⭐ NEW: Color indicator for stock status (for UI binding).
        /// </summary>
        public string StockStatusColor => CurrentStock switch
        {
            0                            => "#EF4444", // Red - Out of stock
            var s when s < MinStockLevel => "#F59E0B", // Orange - Low stock
            var s when s > MaxStockLevel => "#3B82F6", // Blue - Overstocked
            _                            => "#10B981"  // Green - Good stock
        };

        #endregion

        #region UI-Specific Properties

        /// <summary>
        /// Indicates whether this product is currently selected in the UI.
        /// Used for checkbox binding in DataGrid.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Calculated profit margin for display purposes.
        /// </summary>
        public decimal ProfitMargin => Price - Cost;

        /// <summary>
        /// Profit margin as percentage.
        /// </summary>
        public decimal ProfitMarginPercent => Cost > 0
            ? (Price - Cost) / Cost * 100
            : 0;

        /// <summary>
        /// Formatted price for display.
        /// </summary>
        public string FormattedPrice => $"₵{Price:N2}";

        /// <summary>
        /// Formatted cost for display.
        /// </summary>
        public string FormattedCost => $"₵{Cost:N2}";

        /// <summary>
        /// ⭐ NEW: Formatted current stock for display.
        /// </summary>
        public string FormattedStock => $"{CurrentStock:N0} units";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the underlying Product DTO (useful for edit/delete operations).
        /// </summary>
        public ProductListDto GetProduct() => _product;

        /// <summary>
        /// Useful when you need the latest data including updated stock.
        /// </summary>
        public ProductListDto GetUpdatedProduct() => _product with
        {
            CurrentStock = _currentStock
        };

        /// <summary>
        /// Resets the selection state.
        /// </summary>
        public void ClearSelection()
        {
            IsSelected = false;
        }

        /// <summary>
        /// Updates the stock value (called by ViewModel when stock changes).
        /// </summary>
        public void UpdateStock( int newStock )
        {
            CurrentStock = newStock;
        }

        #endregion
    }
}
