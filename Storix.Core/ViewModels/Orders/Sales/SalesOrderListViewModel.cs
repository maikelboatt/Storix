using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Core.Control;
using Storix.Core.ViewModels.Categories;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Sales
{
    public class SalesOrderListViewModel:MvxViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IOrderStore _salesOrderStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SalesOrderListViewModel> _logger;

        private MvxObservableCollection<SalesOrderListItemViewModel> _salesOrder = [];
        private List<SalesOrderListItemViewModel> _allSalesOrder = [];
        private string _searchText = string.Empty;
        private bool _isLoading;

        public SalesOrderListViewModel( IOrderService orderService,
            IOrderStore salesOrderStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<SalesOrderListViewModel> logger )
        {
            _orderService = orderService;
            _salesOrderStore = salesOrderStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            OpenSalesOrderFormCommand = new MvxCommand<int>(ExecuteSalesOrderForm);
            OpenSalesOrderDeleteCommand = new MvxCommand<int>(ExecuteSalesOrderDelete);

        }

        #region ViewModel LifeCycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadSalesOrderAsync();
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

        private async Task LoadSalesOrderAsync()
        {
            DatabaseResult<IEnumerable<SalesOrderListDto>> salesOrdersResult = await _orderService.GetSalesOrderListAsync();

            if (!salesOrdersResult.IsSuccess || salesOrdersResult.Value == null)
            {
                _logger.LogError("Failed to load sales orders: {ErrorMessage}", salesOrdersResult.ErrorMessage);
                SalesOrder = [];
                _allSalesOrder.Clear();
                return;
            }

            _allSalesOrder = salesOrdersResult
                             .Value
                             .Select(dto => new SalesOrderListItemViewModel(dto))
                             .ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                SalesOrder = new MvxObservableCollection<SalesOrderListItemViewModel>(_allSalesOrder);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<SalesOrderListItemViewModel> filtered = _allSalesOrder
                                                             .Where(s => s
                                                                         .CustomerName.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase))
                                                             .ToList();
                SalesOrder = new MvxObservableCollection<SalesOrderListItemViewModel>(filtered);
            }
        }

        #region Properties

        public MvxObservableCollection<SalesOrderListItemViewModel> SalesOrder
        {
            get => _salesOrder;
            set => SetProperty(ref _salesOrder, value);
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

        private IEnumerable<SalesOrderListItemViewModel> SelectedSalesOrder => SalesOrder?.Where(c => c.IsSelected) ?? [];

        #endregion

        #region Commands

        public IMvxCommand<int> OpenSalesOrderFormCommand { get; }
        public IMvxCommand<int> OpenSalesOrderDeleteCommand { get; }

        private void ExecuteSalesOrderDelete( int categoryId )
        {
            _modalNavigationControl.PopUp<CategoryDeleteViewModel>(categoryId);
        }

        private void ExecuteSalesOrderForm( int categoryId )
        {
            _modalNavigationControl.PopUp<CategoryFormViewModel>(categoryId);
        }

        #endregion
    }
}
