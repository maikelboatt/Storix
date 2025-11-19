using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Core.Control;
using Storix.Core.ViewModels.Categories;

namespace Storix.Core.ViewModels.Orders.Purchase
{
    public class PurchaseOrderListViewModel:MvxViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IOrderStore _purchaseOrderStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<PurchaseOrderListViewModel> _logger;

        private MvxObservableCollection<PurchaseOrderListItemViewModel> _purchaseOrder = [];
        private List<PurchaseOrderListItemViewModel> _allPurchaseOrder = [];
        private string _searchText = string.Empty;
        private bool _isLoading;

        public PurchaseOrderListViewModel( IOrderService orderService,
            IOrderStore purchaseOrderStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<PurchaseOrderListViewModel> logger )
        {
            _orderService = orderService;
            _purchaseOrderStore = purchaseOrderStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            OpenPurchaseOrderFormCommand = new MvxCommand<int>(ExecutePurchaseOrderForm);
            OpenPurchaseOrderDeleteCommand = new MvxCommand<int>(ExecutePurchaseOrderDelete);

        }

        #region ViewModel LifeCycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadPurchaseOrderAsync();
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
            // _salesOrderStore.OrderAdded -= OnCategoryAdded;
            // _salesOrderStore.OrderUpdated -= OnCategoryUpdated;
            // _salesOrderStore.OrderDeleted -= OnCategoryDeleted;

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private async Task LoadPurchaseOrderAsync()
        {
            DatabaseResult<IEnumerable<PurchaseOrderListDto>> purchaseOrderResult = await _orderService.GetPurchaseOrderListAsync();

            if (!purchaseOrderResult.IsSuccess || purchaseOrderResult.Value == null)
            {
                _logger.LogError("Failed to load sales orders: {ErrorMessage}", purchaseOrderResult.ErrorMessage);
                PurchaseOrder = [];
                _allPurchaseOrder.Clear();
                return;
            }

            _allPurchaseOrder = purchaseOrderResult
                                .Value
                                .Select(dto => new PurchaseOrderListItemViewModel(dto))
                                .ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                PurchaseOrder = new MvxObservableCollection<PurchaseOrderListItemViewModel>(_allPurchaseOrder);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<PurchaseOrderListItemViewModel> filtered = _allPurchaseOrder
                                                                .Where(s => s
                                                                            .SupplierName.Contains(
                                                                                lowerSearchText,
                                                                                StringComparison.InvariantCultureIgnoreCase))
                                                                .ToList();
                PurchaseOrder = new MvxObservableCollection<PurchaseOrderListItemViewModel>(filtered);
            }
        }

        #region Properties

        public MvxObservableCollection<PurchaseOrderListItemViewModel> PurchaseOrder
        {
            get => _purchaseOrder;
            set => SetProperty(ref _purchaseOrder, value);
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

        private IEnumerable<PurchaseOrderListItemViewModel> SelectedPurchaseOrder => PurchaseOrder?.Where(c => c.IsSelected) ?? [];

        #endregion

        #region Commands

        public IMvxCommand<int> OpenPurchaseOrderFormCommand { get; }
        public IMvxCommand<int> OpenPurchaseOrderDeleteCommand { get; }

        private void ExecutePurchaseOrderDelete( int categoryId )
        {
            _modalNavigationControl.PopUp<CategoryDeleteViewModel>(categoryId);
        }

        private void ExecutePurchaseOrderForm( int categoryId )
        {
            _modalNavigationControl.PopUp<CategoryFormViewModel>(categoryId);
        }

        #endregion
    }
}
