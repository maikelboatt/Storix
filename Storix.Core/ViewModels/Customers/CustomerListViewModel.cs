using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Core.Control;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Customers
{
    public class CustomerListViewModel:MvxViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerStore _customerStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CustomerListViewModel> _logger;

        private MvxObservableCollection<CustomerListItemViewModel> _customers = [];
        private List<CustomerListItemViewModel> _allCustomers = [];
        private string _searchText = string.Empty;

        private bool _isLoading;

        public CustomerListViewModel( ICustomerService customerService,
            ICustomerStore customerStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<CustomerListViewModel> logger )
        {
            _customerService = customerService;
            _customerStore = customerStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to write operation events from the store
            _customerStore.CustomerAdded += OnCustomerAdded;
            _customerStore.CustomerUpdated += OnCustomerUpdated;
            _customerStore.CustomerDeleted += OnCustomerDeleted;

            // Initialize commands
            OpenCustomerFormCommand = new MvxCommand<int>(ExecuteCustomerForm);
            OpenCustomerDeleteCommand = new MvxCommand<int>(ExecuteCustomerDelete);

        }

        #region ViewModel LifeCycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadCustomers();
            }
            finally
            {
                IsLoading = false;
            }
            await base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            // Unsubscribe from store events to prevent memory leaks
            _customerStore.CustomerAdded -= OnCustomerAdded;
            _customerStore.CustomerUpdated -= OnCustomerUpdated;
            _customerStore.CustomerDeleted -= OnCustomerDeleted;

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private async Task LoadCustomers()
        {
            DatabaseResult<IEnumerable<CustomerDto>> result = await _customerService.GetAllActiveCustomersAsync();

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to load customers: {Error}", result.ErrorMessage);
                Customers = [];
                _allCustomers.Clear();
                return;
            }

            _allCustomers = result
                            .Value.Select(dto => new CustomerListItemViewModel(dto))
                            .ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                Customers = new MvxObservableCollection<CustomerListItemViewModel>(_allCustomers);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<CustomerListItemViewModel> filtered = _allCustomers
                                                           .Where(c => c
                                                                       .Name.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ||
                                                                       (c
                                                                        .Address?.ToLowerInvariant()
                                                                        .Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase)
                                                                        ?? false))
                                                           .ToList();
                Customers = new MvxObservableCollection<CustomerListItemViewModel>(filtered);
            }
        }

        #region Store Event Handlers

        private void OnCustomerAdded( Customer customer )
        {
            try
            {
                CustomerDto dto = customer.ToDto();

                CustomerListItemViewModel vm = new(dto);
                _allCustomers.Add(vm);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to CustomerAdded for {CustomerId}", customer.CustomerId);
            }
        }

        private void OnCustomerUpdated( Customer customer )
        {
            try
            {
                CustomerListItemViewModel? existing = _allCustomers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
                if (existing == null)
                    return;

                CustomerDto dto = customer.ToDto();
                CustomerListItemViewModel updatedVm = new(dto);

                int index = _allCustomers.IndexOf(existing);
                _allCustomers[index] = updatedVm;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to CustomerUpdated for {CustomerId}", customer.CustomerId);
            }
        }

        private void OnCustomerDeleted( int customerId )
        {
            try
            {
                _allCustomers.RemoveAll(c => c.CustomerId == customerId);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to CustomerDeleted for {CustomerId}", customerId);

            }
        }

        #endregion

        #region Properties

        public MvxObservableCollection<CustomerListItemViewModel> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private IEnumerable<CustomerListItemViewModel> SelectedCustomers => Customers?.Where(c => c.IsSelected) ?? [];

        #endregion

        #region Commands

        public IMvxCommand<int> OpenCustomerFormCommand { get; }
        public IMvxCommand<int> OpenCustomerDeleteCommand { get; }

        private void ExecuteCustomerDelete( int customerId )
        {
            _modalNavigationControl.PopUp<CustomerDeleteViewModel>(customerId);
        }

        private void ExecuteCustomerForm( int customerId )
        {
            _modalNavigationControl.PopUp<CustomerFormViewModel>(customerId);
        }

        #endregion
    }
}
