using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;

namespace Storix.Core.ViewModels.Products
{
    public class ProductListItemViewModel( ProductListDto product ):MvxNotifyPropertyChanged
    {
        private readonly ProductListDto _product = product ?? throw new ArgumentNullException(nameof(product));
        private bool _isSelected;

        #region Product Properities

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
        public int CurrentStock => _product.CurrentStock;
        public bool IsLowStock => _product.IsLowStock;

        #endregion

        #region UI_Specific Properties

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
        /// Formatted price for display.
        /// </summary>
        public string FormattedPrice => $"₵{Price:N2}";

        /// <summary>
        /// Formatted cost for display.
        /// </summary>
        public string FormattedCost => $"₵{Cost:N2}";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the underlying Product entity (useful for edit/delete operations).
        /// </summary>
        public ProductListDto GetProduct() => _product;

        /// <summary>
        /// Resets the selection state.
        /// </summary>
        public void ClearSelection()
        {
            IsSelected = false;
        }

        #endregion
    }
}
