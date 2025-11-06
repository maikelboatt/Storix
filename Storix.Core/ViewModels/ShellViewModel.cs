using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Stores;
using Storix.Core.Factory;

namespace Storix.Core.ViewModels
{
    public class ShellViewModel:MvxViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ModalNavigationStore _modalNavigationStore;
        private MvxViewModel? _currentMainContent;

        public ShellViewModel( IViewModelFactory viewModelFactory, ModalNavigationStore modalNavigationStore )
        {
            _viewModelFactory = viewModelFactory;
            _modalNavigationStore = modalNavigationStore;

            // Initialize the main content to AnalyticsPaneViewModel
            AnalyticsPaneViewModel? analyticsPaneViewModel = viewModelFactory.CreateViewModel<AnalyticsPaneViewModel>();
            CurrentMainContent = analyticsPaneViewModel;

            // Initialize commands
            NavigateToAnalyticCommand = new MvxCommand(ExecuteNavigateToAnalytics);
            NavigateToProductsCommand = new MvxCommand(ExecuteNavigateToProducts);
            NavigateToCategoriesCommand = new MvxCommand(ExecuteNavigateToCategories);
            NavigateToSalesOrdersCommand = new MvxCommand(ExecuteNavigateToSalesOrders);
            NavigateToPurchaseOrdersCommand = new MvxCommand(ExecuteNavigateToPurchaseOrders);
            NavigateToSuppliersCommand = new MvxCommand(ExecuteNavigateToSuppliers);
            NavigateToCustomersCommand = new MvxCommand(ExecuteNavigateToCustomers);
            NavigateToLocationsCommand = new MvxCommand(ExecuteNavigateToLocations);
            NavigateToReportsCommand = new MvxCommand(ExecuteNavigateToReports);
            NavigateToSettingsCommand = new MvxCommand(ExecuteNavigateToSettings);

            modalNavigationStore.CurrentModalViewModelChanged += ModalNavigationStoreOnCurrentModalViewModelChanged;

        }

        private void ModalNavigationStoreOnCurrentModalViewModelChanged()
        {
            System.Diagnostics.Debug.WriteLine($"Modal changed. IsOpen: {IsModalOpen}, Content: {CurrentModalContent?.GetType().Name}");
            RaisePropertyChanged(nameof(CurrentModalContent));
            RaisePropertyChanged(nameof(IsModalOpen));
        }

        #region Properties

        public MvxViewModel? CurrentMainContent
        {
            get => _currentMainContent;
            private set => SetProperty(ref _currentMainContent, value);
        }

        public bool IsModalOpen => _modalNavigationStore.CurrentModalViewModel != null;
        public MvxViewModel? CurrentModalContent => _modalNavigationStore.CurrentModalViewModel;

        #endregion

        #region Commands

        public IMvxCommand NavigateToAnalyticCommand { get; }
        public IMvxCommand NavigateToProductsCommand { get; }
        public IMvxCommand NavigateToCategoriesCommand { get; }
        public IMvxCommand NavigateToSalesOrdersCommand { get; }
        public IMvxCommand NavigateToPurchaseOrdersCommand { get; }
        public IMvxCommand NavigateToSuppliersCommand { get; }
        public IMvxCommand NavigateToCustomersCommand { get; }
        public IMvxCommand NavigateToLocationsCommand { get; }
        public IMvxCommand NavigateToReportsCommand { get; }
        public IMvxCommand NavigateToSettingsCommand { get; }

        #endregion

        #region Command Execution

        private void ExecuteNavigateToAnalytics()
        {
            AnalyticsPaneViewModel? analyticsPaneViewModel = _viewModelFactory.CreateViewModel<AnalyticsPaneViewModel>();
            CurrentMainContent = analyticsPaneViewModel;
            analyticsPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToProducts()
        {
            ProductPaneViewModel? productPaneViewModel = _viewModelFactory.CreateViewModel<ProductPaneViewModel>();
            CurrentMainContent = productPaneViewModel;
            productPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToCategories()
        {
            CategoryPaneViewModel? categoryPaneViewModel = _viewModelFactory.CreateViewModel<CategoryPaneViewModel>();
            CurrentMainContent = categoryPaneViewModel;
            categoryPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToSalesOrders()
        {
            SalesOrderPaneViewModel? salesOrderPaneViewModel = _viewModelFactory.CreateViewModel<SalesOrderPaneViewModel>();
            CurrentMainContent = salesOrderPaneViewModel;
            salesOrderPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToPurchaseOrders()
        {
            PurchaseOrderPaneViewModel? purchaseOrderPaneViewModel = _viewModelFactory.CreateViewModel<PurchaseOrderPaneViewModel>();
            CurrentMainContent = purchaseOrderPaneViewModel;
            purchaseOrderPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToSuppliers()
        {
            SupplierPaneViewModel? supplierPaneViewModel = _viewModelFactory.CreateViewModel<SupplierPaneViewModel>();
            CurrentMainContent = supplierPaneViewModel;
            supplierPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToCustomers()
        {
            CustomerPaneViewModel? customerPaneViewModel = _viewModelFactory.CreateViewModel<CustomerPaneViewModel>();
            CurrentMainContent = customerPaneViewModel;
            customerPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToLocations()
        {
            LocationPaneViewModel? locationPaneViewModel = _viewModelFactory.CreateViewModel<LocationPaneViewModel>();
            CurrentMainContent = locationPaneViewModel;
            locationPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToReports()
        {
            ReportsPaneViewModel? reportPaneViewModel = _viewModelFactory.CreateViewModel<ReportsPaneViewModel>();
            CurrentMainContent = reportPaneViewModel;
            reportPaneViewModel?.Initialize();
        }

        private void ExecuteNavigateToSettings()
        {
            SettingsPaneViewModel? settingsPaneViewModel = _viewModelFactory.CreateViewModel<SettingsPaneViewModel>();
            CurrentMainContent = settingsPaneViewModel;
            settingsPaneViewModel?.Initialize();
        }

        #endregion
    }
}
