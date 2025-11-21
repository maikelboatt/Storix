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

namespace Storix.Core.ViewModels.Orders.Purchase
{
    public class PurchaseOrderListViewModel:MvxViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IOrderItemManager _orderItemManager;
        private readonly IOrderStore _orderStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<PurchaseOrderListViewModel> _logger;

        private MvxObservableCollection<PurchaseOrderListItemViewModel> _purchaseOrders = new();
        private List<PurchaseOrderListItemViewModel> _allPurchaseOrders = new();
        private string _searchText = string.Empty;
        private bool _isLoading;

        public PurchaseOrderListViewModel(
            IOrderService orderService,
            IOrderItemManager orderItemManager,
            IOrderStore orderStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<PurchaseOrderListViewModel> logger )
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
            OpenPurchaseOrderFormCommand = new MvxCommand(ExecuteOpenPurchaseOrderForm);
            OpenPurchaseOrderEditCommand = new MvxCommand<int>(ExecuteOpenPurchaseOrderEdit);
            OpenPurchaseOrderDeleteCommand = new MvxCommand<int>(ExecuteOpenPurchaseOrderDelete);
            RefreshCommand = new MvxAsyncCommand(LoadPurchaseOrdersAsync);
        }

        #region Event Handlers

        private void OnOrderAdded( Order order )
        {
            // Only handle purchase orders
            if (order.Type != OrderType.Purchase)
                return;

            // Fire and forget - let the async operation complete without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    string supplierName = _orderStore.GetSupplierName(order.SupplierId ?? 0);
                    DatabaseResult<decimal> totalAmountResult =
                        await _orderItemManager.GetOrderTotalValueAsync(order.OrderId);

                    decimal totalAmount = totalAmountResult.IsSuccess
                        ? totalAmountResult.Value
                        : 0m;

                    PurchaseOrderListDto dto = order.ToPurchaseOrderListDto(supplierName, totalAmount);
                    PurchaseOrderListItemViewModel vm = new(dto);

                    // Update on UI thread
                    await InvokeOnMainThreadAsync(() =>
                    {
                        _allPurchaseOrders.Add(vm);
                        ApplyFilter();
                    });

                    _logger.LogInformation("Added purchase order {OrderId} to list", order.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling OrderAdded for {OrderId}", order.OrderId);
                }
            });
        }

        private void OnOrderUpdated( Order order )
        {
            // Only handle purchase orders
            if (order.Type != OrderType.Purchase)
                return;

            // Fire and forget
            _ = Task.Run(async () =>
            {
                try
                {
                    PurchaseOrderListItemViewModel? existingVm = _allPurchaseOrders.FirstOrDefault(p => p.OrderId == order.OrderId);
                    if (existingVm == null)
                    {
                        _logger.LogWarning("Updated order {OrderId} not found in list", order.OrderId);
                        return;
                    }

                    string supplierName = _orderStore.GetSupplierName(order.SupplierId ?? 0);
                    DatabaseResult<decimal> totalAmountResult =
                        await _orderItemManager.GetOrderTotalValueAsync(order.OrderId);

                    decimal totalAmount = totalAmountResult.IsSuccess
                        ? totalAmountResult.Value
                        : 0m;

                    PurchaseOrderListDto dto = order.ToPurchaseOrderListDto(supplierName, totalAmount);

                    // Update on UI thread
                    await InvokeOnMainThreadAsync(() =>
                    {
                        existingVm.UpdateFromDto(dto);
                        ApplyFilter();
                    });

                    _logger.LogInformation("Updated purchase order {OrderId} in list", order.OrderId);
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
                int removedCount = _allPurchaseOrders.RemoveAll(p => p.OrderId == orderId);
                if (removedCount > 0)
                {
                    ApplyFilter();
                    _logger.LogInformation("Removed purchase order {OrderId} from list", orderId);
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
                await LoadPurchaseOrdersAsync();
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

        private async Task LoadPurchaseOrdersAsync()
        {
            try
            {
                DatabaseResult<IEnumerable<PurchaseOrderListDto>> purchaseOrdersResult =
                    await _orderService.GetPurchaseOrderListAsync();

                if (!purchaseOrdersResult.IsSuccess || purchaseOrdersResult.Value == null)
                {
                    _logger.LogError("Failed to load purchase orders: {ErrorMessage}", purchaseOrdersResult.ErrorMessage);
                    PurchaseOrders = new MvxObservableCollection<PurchaseOrderListItemViewModel>();
                    _allPurchaseOrders.Clear();
                    return;
                }

                _allPurchaseOrders = purchaseOrdersResult
                                     .Value
                                     .Select(dto => new PurchaseOrderListItemViewModel(dto))
                                     .ToList();

                ApplyFilter();
                _logger.LogInformation("Loaded {Count} purchase orders", _allPurchaseOrders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase orders");
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                PurchaseOrders = new MvxObservableCollection<PurchaseOrderListItemViewModel>(_allPurchaseOrders);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<PurchaseOrderListItemViewModel> filtered = _allPurchaseOrders
                                                                .Where(p => p.SupplierName.Contains(
                                                                           lowerSearchText,
                                                                           StringComparison.InvariantCultureIgnoreCase))
                                                                .ToList();
                PurchaseOrders = new MvxObservableCollection<PurchaseOrderListItemViewModel>(filtered);
            }

            _logger.LogDebug("Filtered purchase orders: {Count} of {Total}", PurchaseOrders.Count, _allPurchaseOrders.Count);
        }

        #endregion

        #region Properties

        public MvxObservableCollection<PurchaseOrderListItemViewModel> PurchaseOrders
        {
            get => _purchaseOrders;
            set => SetProperty(ref _purchaseOrders, value);
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

        public IEnumerable<PurchaseOrderListItemViewModel> SelectedPurchaseOrders =>
            PurchaseOrders?.Where(p => p.IsSelected) ?? Enumerable.Empty<PurchaseOrderListItemViewModel>();

        #endregion

        #region Commands

        public IMvxCommand OpenPurchaseOrderFormCommand { get; }
        public IMvxCommand<int> OpenPurchaseOrderEditCommand { get; }
        public IMvxCommand<int> OpenPurchaseOrderDeleteCommand { get; }
        public IMvxAsyncCommand RefreshCommand { get; }

        private void ExecuteOpenPurchaseOrderForm()
        {
            // Pass 0 for create mode
            _modalNavigationControl.PopUp<PurchaseOrderFormViewModel>(0);
        }

        private void ExecuteOpenPurchaseOrderEdit( int orderId )
        {
            // Pass orderId for edit mode
            _modalNavigationControl.PopUp<PurchaseOrderFormViewModel>(orderId);
        }

        private void ExecuteOpenPurchaseOrderDelete( int orderId )
        {
            // TODO: Create PurchaseOrderDeleteViewModel
            // _modalNavigationControl.PopUp<PurchaseOrderDeleteViewModel>(orderId);
            _logger.LogWarning("Delete not implemented for order {OrderId}", orderId);
        }

        #endregion
    }
}
