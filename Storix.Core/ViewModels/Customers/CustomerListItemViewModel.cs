using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;

namespace Storix.Core.ViewModels.Customers
{
    public class CustomerListItemViewModel( CustomerDto customer ):MvxNotifyPropertyChanged
    {
        private readonly CustomerDto _customer = customer ?? throw new ArgumentNullException(nameof(customer));

        private bool _isSelected;

        #region Supplier Properties

        public int CustomerId => _customer.CustomerId;
        public string Name => _customer.Name;
        public string Email => _customer.Email!;
        public string Phone => _customer.Phone!;
        public string Address => _customer.Address!;

        #endregion

        #region UI_Specific Properties

        /// <summary>
        /// Indicates whether this category is currently selected in the UI.
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
        /// Gets the underlying Customer entity (useful for edit/delete operations).
        /// </summary>
        public CustomerDto GetCustomer() => _customer;

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
