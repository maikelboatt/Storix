using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Stores.Suppliers;
using Storix.Core.Control;
using Storix.Core.ViewModels.Categories;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Suppliers
{
    public class SupplierListViewModel:MvxViewModel
    {
        private readonly ISupplierService _supplierService;
        private readonly ISupplierStore _supplierStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SupplierListViewModel> _logger;

        private MvxObservableCollection<SupplierListItemViewModel> _suppliers = [];
        private List<SupplierListItemViewModel> _allSuppliers = [];
        private string _searchText = string.Empty;

        private bool _isLoading;

        public SupplierListViewModel( ISupplierService supplierService,
            ISupplierStore supplierStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<SupplierListViewModel> logger )
        {
            _supplierService = supplierService;
            _supplierStore = supplierStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to write operation events from the store
            _supplierStore.SupplierAdded += OnSupplierAdded;
            _supplierStore.SupplierUpdated += OnSupplierUpdated;
            _supplierStore.SupplierDeleted += OnSupplierDeleted;

            // Initialize commands
            OpenSupplierFormCommand = new MvxCommand<int>(ExecuteSupplierForm);
            OpenSupplierDeleteCommand = new MvxCommand<int>(ExecuteSupplierDelete);
        }


        #region ViewModel LifeCycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadSuppliers();
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
            _supplierStore.SupplierAdded -= OnSupplierAdded;
            _supplierStore.SupplierUpdated -= OnSupplierUpdated;
            _supplierStore.SupplierDeleted -= OnSupplierDeleted;

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private async Task LoadSuppliers()
        {
            DatabaseResult<IEnumerable<SupplierDto>> result = await _supplierService.GetAllActiveSuppliersAsync();

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to load suppliers: {Error}", result.ErrorMessage);
                Suppliers = [];
                _allSuppliers.Clear();
                return;
            }

            _allSuppliers = result
                            .Value
                            .Select(dto => new SupplierListItemViewModel(dto))
                            .ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                Suppliers = new MvxObservableCollection<SupplierListItemViewModel>(_allSuppliers);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<SupplierListItemViewModel> filtered = _allSuppliers
                                                           .Where(c => c
                                                                       .Name.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ||
                                                                       (c
                                                                        .Address?.ToLowerInvariant()
                                                                        .Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase)
                                                                        ?? false))
                                                           .ToList();
                Suppliers = new MvxObservableCollection<SupplierListItemViewModel>(filtered);
            }
        }

        #region Store Event Handlers

        private void OnSupplierAdded( Supplier supplier )
        {
            try
            {
                SupplierDto dto = supplier.ToDto();

                SupplierListItemViewModel vm = new(dto);
                _allSuppliers.Add(vm);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to SupplierAdded for {CustomerId}", supplier.SupplierId);
            }
        }

        private void OnSupplierUpdated( Supplier supplier )
        {
            try
            {
                SupplierListItemViewModel? existing = _allSuppliers.FirstOrDefault(s => s.SupplierId == supplier.SupplierId);
                if (existing == null)
                    return;

                SupplierDto dto = supplier.ToDto();

                SupplierListItemViewModel updatedVm = new(dto);

                int index = _allSuppliers.IndexOf(existing);
                _allSuppliers[index] = updatedVm;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to SupplierUpdated for {CustomerId}", supplier.SupplierId);
            }
        }

        private void OnSupplierDeleted( int supplierId )
        {
            try
            {
                _allSuppliers.RemoveAll(s => s.SupplierId == supplierId);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to SupplierAdded for {CustomerId}", supplierId);
            }
        }

        #endregion

        #region Properties

        public MvxObservableCollection<SupplierListItemViewModel> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
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

        private IEnumerable<SupplierListItemViewModel> SelectedSuppliers => Suppliers?.Where(s => s.IsSelected) ?? [];

        #endregion

        #region Commands

        public IMvxCommand<int> OpenSupplierFormCommand { get; }
        public IMvxCommand<int> OpenSupplierDeleteCommand { get; }

        private void ExecuteSupplierDelete( int categoryId )
        {
            // _modalNavigationControl.PopUp<SupplierDeleteViewModel>(categoryId);
        }

        private void ExecuteSupplierForm( int categoryId )
        {
            // _modalNavigationControl.PopUp<SupplierFormViewModel>(categoryId);
        }

        #endregion
    }
}
