using MvvmCross.ViewModels;
using Storix.Application.DTO.Suppliers;

namespace Storix.Core.ViewModels.Suppliers
{
    public class SupplierListItemViewModel( SupplierDto supplier ):MvxViewModel
    {
        private readonly SupplierDto _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
        private bool _isSelected;

        #region Supplier Properties

        public int SupplierId => _supplier.SupplierId;
        public string Name => _supplier.Name;
        public string Email => _supplier.Email;
        public string Phone => _supplier.Phone;
        public string Address => _supplier.Address;

        #endregion

        #region UI_Specific Properties

        /// <summary>
        /// Indicates whether this Supplier is currently selected in the UI.
        /// Used for checkbox binding in DataGrid.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the underlying Category entity (useful for edit/delete operations).
        /// </summary>
        public SupplierDto GetSupplier() => _supplier;

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
