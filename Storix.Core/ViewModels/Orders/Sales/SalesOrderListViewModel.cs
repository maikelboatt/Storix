using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Core.Control;
using Storix.Core.ViewModels.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Orders.Sales
{
    public class SalesOrderListViewModel:MvxViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IOrderItemManager _orderItemManager;
        private readonly IOrderStore _orderStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SalesOrderListViewModel> _logger;

        private MvxObservableCollection<SalesOrderListItemViewModel> _salesOrders = new();
        private List<SalesOrderListItemViewModel> _allSalesOrders = new();
        private string _searchText = string.Empty;
        private bool _isLoading;

        public SalesOrderListViewModel(
            IOrderService orderService,
            IOrderItemManager orderItemManager,
            IOrderStore orderStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<SalesOrderListViewModel> logger )
        {
            _orderService = orderService;
            _orderItemManager = orderItemManager;
            _orderStore = orderStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to store events
            _orderStore.OrderAdded += OnOrderAdded;
            _orderStore.OrderUpdated += OnOrderUpdated;
            _orderStore.OrderDeleted += OnOrderDeleted;

            // Initialize commands
            OpenSalesOrderFormCommand = new MvxCommand<int>(ExecuteOpenSalesOrderForm);
            OpenSalesOrderDeleteCommand = new MvxCommand<int>(ExecuteOpenSalesOrderDelete);
            RefreshCommand = new MvxAsyncCommand(LoadSalesOrdersAsync);
        }

        #region Event Handlers

        private void OnOrderAdded( Order order )
        {
            // Only handle sales orders
            if (order.Type != OrderType.Sale)
                return;

            // Fire and forget - let the async operation complete without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    string customerName = _orderStore.GetCustomerName(order.CustomerId ?? 0);
                    DatabaseResult<decimal> totalAmountResult =
                        await _orderItemManager.GetOrderTotalValueAsync(order.OrderId);

                    decimal totalAmount = totalAmountResult.IsSuccess
                        ? totalAmountResult.Value
                        : 0m;

                    SalesOrderListDto dto = order.ToSalesOrderListDto(customerName, totalAmount);
                    SalesOrderListItemViewModel vm = new(dto);

                    // Update on UI thread
                    await InvokeOnMainThreadAsync(() =>
                    {
                        _allSalesOrders.Add(vm);
                        ApplyFilter();
                    });

                    _logger.LogInformation("Added sales order {OrderId} to list", order.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling OrderAdded for {OrderId}", order.OrderId);
                }
            });
        }

        private void OnOrderUpdated( Order order )
        {
            // Only handle sales orders
            if (order.Type != OrderType.Sale)
                return;

            // Fire and forget
            _ = Task.Run(async () =>
            {
                try
                {
                    SalesOrderListItemViewModel? existingVm = _allSalesOrders.FirstOrDefault(s => s.OrderId == order.OrderId);
                    if (existingVm == null)
                    {
                        _logger.LogWarning("Updated order {OrderId} not found in list", order.OrderId);
                        return;
                    }

                    string customerName = _orderStore.GetCustomerName(order.CustomerId ?? 0);
                    DatabaseResult<decimal> totalAmountResult =
                        await _orderItemManager.GetOrderTotalValueAsync(order.OrderId);

                    decimal totalAmount = totalAmountResult.IsSuccess
                        ? totalAmountResult.Value
                        : 0m;

                    SalesOrderListDto dto = order.ToSalesOrderListDto(customerName, totalAmount);

                    // Update on UI thread
                    await InvokeOnMainThreadAsync(() =>
                    {
                        existingVm.UpdateFromDto(dto);
                        ApplyFilter();
                    });

                    _logger.LogInformation("Updated sales order {OrderId} in list", order.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling OrderUpdated for {OrderId}", order.OrderId);
                }
            });
        }

        private void OnOrderDeleted( int orderId )
        {
            try
            {
                // Already on appropriate thread from OrderStore
                int removedCount = _allSalesOrders.RemoveAll(s => s.OrderId == orderId);
                if (removedCount > 0)
                {
                    ApplyFilter();
                    _logger.LogInformation("Removed sales order {OrderId} from list", orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling OrderDeleted for {OrderId}", orderId);
            }
        }

        #endregion

        #region ViewModel Lifecycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadSalesOrdersAsync();
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
            _orderStore.OrderAdded -= OnOrderAdded;
            _orderStore.OrderUpdated -= OnOrderUpdated;
            _orderStore.OrderDeleted -= OnOrderDeleted;

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        #region Data Loading

        private async Task LoadSalesOrdersAsync()
        {
            try
            {
                DatabaseResult<IEnumerable<SalesOrderListDto>> salesOrdersResult =
                    await _orderService.GetSalesOrderListAsync();

                if (!salesOrdersResult.IsSuccess || salesOrdersResult.Value == null)
                {
                    _logger.LogError("Failed to load sales orders: {ErrorMessage}", salesOrdersResult.ErrorMessage);
                    SalesOrders = new MvxObservableCollection<SalesOrderListItemViewModel>();
                    _allSalesOrders.Clear();
                    return;
                }

                _allSalesOrders = salesOrdersResult
                                  .Value
                                  .Select(dto => new SalesOrderListItemViewModel(dto))
                                  .ToList();

                ApplyFilter();
                _logger.LogInformation("Loaded {Count} sales orders", _allSalesOrders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales orders");
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                SalesOrders = new MvxObservableCollection<SalesOrderListItemViewModel>(_allSalesOrders);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<SalesOrderListItemViewModel> filtered = _allSalesOrders
                                                             .Where(s => s.CustomerName.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase))
                                                             .ToList();
                SalesOrders = new MvxObservableCollection<SalesOrderListItemViewModel>(filtered);
            }

            _logger.LogDebug("Filtered sales orders: {Count} of {Total}", SalesOrders.Count, _allSalesOrders.Count);
        }

        #endregion

        #region Properties

        public MvxObservableCollection<SalesOrderListItemViewModel> SalesOrders
        {
            get => _salesOrders;
            set => SetProperty(ref _salesOrders, value);
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

        public IEnumerable<SalesOrderListItemViewModel> SelectedSalesOrders =>
            SalesOrders?.Where(c => c.IsSelected) ?? Enumerable.Empty<SalesOrderListItemViewModel>();

        #endregion

        #region Commands

        public IMvxCommand<int> OpenSalesOrderFormCommand { get; }
        public IMvxCommand<int> OpenSalesOrderDeleteCommand { get; }
        public IMvxAsyncCommand RefreshCommand { get; }

        private void ExecuteOpenSalesOrderForm( int orderId )
        {
            _modalNavigationControl.PopUp<SalesOrderFormViewModel>(orderId);
        }

        private void ExecuteOpenSalesOrderDelete( int orderId )
        {
            // TODO: Create SalesOrderDeleteViewModel
            // _modalNavigationControl.PopUp<SalesOrderDeleteViewModel>(orderId);
            _logger.LogWarning("Delete not implemented for order {OrderId}", orderId);
        }

        #endregion
    }
}
