using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Orders;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Application.Stores.Suppliers;
using Storix.Application.Stores.Users;
using Storix.Core.Control;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders
{
    /// <summary>
    /// Base ViewModel for order deletion confirmation dialogs.
    /// Provides common functionality for both Sales Order and Purchase Order deletion.
    /// </summary>
    public abstract class OrderDeleteViewModelBase:MvxViewModel<int>
    {
        protected readonly IOrderService _orderService;
        protected readonly IOrderCacheReadService _orderCacheReadService;
        protected readonly ISupplierStore _supplierStore;
        protected readonly ICustomerStore _customerStore;
        protected readonly IUserStore _userStore;
        protected readonly IModalNavigationControl _modalNavigationControl;
        protected readonly ILogger _logger;

        private OrderDto? _order;
        private bool _isLoading;
        protected int _orderId;

        protected OrderDeleteViewModelBase(
            IOrderService orderService,
            IOrderCacheReadService orderCacheReadService,
            ISupplierStore supplierStore,
            ICustomerStore customerStore,
            IUserStore userStore,
            IModalNavigationControl modalNavigationControl,
            ILogger logger )
        {
            _orderService = orderService;
            _orderCacheReadService = orderCacheReadService;
            _supplierStore = supplierStore;
            _customerStore = customerStore;
            _userStore = userStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            DeleteCommand = new MvxAsyncCommand(ExecuteDeleteCommandAsync, () => CanDelete);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        private async Task ExecuteDeleteCommandAsync()
        {
            if (Order == null)
            {
                _logger.LogWarning("⚠️ Delete command executed but Order is null. Aborting deletion.");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation(
                    "🗑️ Deleting {OrderType} order: {OrderId} - Status: {Status}",
                    OrderTypeDisplay,
                    _orderId,
                    Order.Status);

                await _orderService.DeleteOrderAsync(_orderId);

                _logger.LogInformation(
                    "✅ Successfully deleted {OrderType} order: {OrderId}",
                    OrderTypeDisplay,
                    Order.OrderId);

                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to delete {OrderType} order: {OrderId}",
                    OrderTypeDisplay,
                    Order?.OrderId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ {OrderType} order deletion cancelled by user", OrderTypeDisplay);
            _modalNavigationControl.Close();
        }

        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _orderId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadOrderAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error loading {OrderType} order details for deletion. OrderId: {OrderId}",
                    OrderTypeDisplay,
                    _orderId);
                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        #endregion

        #region Commands

        public IMvxCommand DeleteCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion

        #region Properties

        /// <summary>
        /// The order to be deleted with all its details
        /// </summary>
        public OrderDto? Order
        {
            get => _order;
            protected set => SetProperty(
                ref _order,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => SupplierName);
                    RaisePropertyChanged(() => CustomerName);
                    RaisePropertyChanged(() => CreatedByName);
                    RaisePropertyChanged(() => OrderTypeDisplay);
                    RaisePropertyChanged(() => StatusDisplay);
                    RaisePropertyChanged(() => StatusColor);
                    RaisePropertyChanged(() => HasNotes);
                    RaisePropertyChanged(() => HasDeliveryDate);
                    RaisePropertyChanged(() => OrderEntityName);
                    DeleteCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether a deletion operation is in progress
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(
                ref _isLoading,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => CanCancel);
                    DeleteCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Whether the delete command can be executed
        /// </summary>
        public bool CanDelete => Order != null && !IsLoading;

        /// <summary>
        /// Whether the cancel command can be executed
        /// </summary>
        public bool CanCancel => !IsLoading;

        public string SupplierName
        {
            get
            {
                if (Order == null || Order.SupplierId == null)
                    return "N/A";

                return _supplierStore.GetSupplierName(Order.SupplierId.Value) ?? "N/A";
            }
        }

        public string CustomerName
        {
            get
            {
                if (Order == null || Order.CustomerId == null)
                    return "N/A";

                return _customerStore.GetCustomerName(Order.CustomerId.Value) ?? "N/A";
            }
        }

        public string CreatedByName
        {
            get
            {
                if (Order == null)
                    return "N/A";

                return _userStore.GetUsername(Order.CreatedBy) ?? "Unknown User";
            }
        }

        public abstract string OrderTypeDisplay { get; }

        public abstract string OrderEntityName { get; }

        public string StatusDisplay => Order?.Status.ToString() ?? "Unknown";

        public string StatusColor
        {
            get
            {
                if (Order == null) return "#6B7280";

                return Order.Status switch
                {
                    OrderStatus.Draft     => "#F59E0B",
                    OrderStatus.Active    => "#3B82F6",
                    OrderStatus.Fulfilled => "#8B5CF6", // Purple
                    OrderStatus.Completed => "#10B981",
                    OrderStatus.Cancelled => "#EF4444",
                    _                     => "#6B7280"
                };
            }
        }

        public bool HasNotes => !string.IsNullOrWhiteSpace(Order?.Notes);
        public bool HasDeliveryDate => Order?.DeliveryDate != null;

        #endregion

        #region Methods

        private async Task LoadOrderAsync()
        {
            _logger.LogDebug(
                "🧩 Loading {OrderType} order details for deletion. OrderId: {OrderId}",
                OrderTypeDisplay,
                _orderId);

            OrderDto? order = _orderCacheReadService.GetOrderByIdInCache(_orderId);

            if (order == null)
            {
                _logger.LogWarning(
                    "⚠️ {OrderType} order with ID {OrderId} not found in cache.",
                    OrderTypeDisplay,
                    _orderId);
            }

            Order = order;

            if (Order != null)
            {
                _logger.LogDebug(
                    "✅ {OrderType} order details loaded successfully for OrderId: {OrderId}",
                    OrderTypeDisplay,
                    _orderId);
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}
