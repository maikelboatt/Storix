using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Application.Stores.Locations;
using Storix.Application.Stores.Products;
using Storix.Application.Stores.Suppliers;
using Storix.Application.Stores.Users;
using Storix.Core.Control;
using Storix.Core.Helper;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders
{
    /// <summary>
    /// Base ViewModel for displaying order details.
    /// Works for both Sales Orders and Purchase Orders with status management.
    /// </summary>
    public abstract class OrderDetailsViewModelBase:MvxViewModel<int>
    {
        protected readonly IOrderService _orderService;
        private readonly IOrderItemService _orderItemService;
        protected readonly IOrderCacheReadService _orderCacheReadService;
        private readonly IPrintManager _printManager;
        private readonly IProductStore _productStore;
        protected readonly ISupplierStore _supplierStore;
        protected readonly ICustomerStore _customerStore;
        protected readonly IUserStore _userStore;
        protected readonly ILocationStore _locationStore;
        private readonly IOrderFulfillmentHelper _orderFulfillmentHelper;
        protected readonly IModalNavigationControl _modalNavigationControl;
        protected readonly ILogger _logger;

        private OrderDto? _order;
        private bool _isLoading;
        protected int _orderId;
        private OrderStatus _originalStatus;
        private string _orderNumber = string.Empty;

        private ObservableCollection<OrderItemDisplayDto> _orderItems = new();
        private decimal _orderSubtotal;
        private decimal _orderTotal;

        #region Constructor

        protected OrderDetailsViewModelBase(
            IOrderService orderService,
            IOrderItemService orderItemService,
            IOrderCacheReadService orderCacheReadService,
            IPrintManager printManager,
            IProductStore productStore,
            ISupplierStore supplierStore,
            ICustomerStore customerStore,
            IUserStore userStore,
            ILocationStore locationStore,
            IOrderFulfillmentHelper orderFulfillmentHelper,
            IModalNavigationControl modalNavigationControl,
            ILogger logger )
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _orderItemService = orderItemService;
            _orderCacheReadService = orderCacheReadService ?? throw new ArgumentNullException(nameof(orderCacheReadService));
            _printManager = printManager;
            _productStore = productStore;
            _supplierStore = supplierStore ?? throw new ArgumentNullException(nameof(supplierStore));
            _customerStore = customerStore ?? throw new ArgumentNullException(nameof(customerStore));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _locationStore = locationStore ?? throw new ArgumentNullException(nameof(locationStore));
            _orderFulfillmentHelper = orderFulfillmentHelper;
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            CloseCommand = new MvxCommand(ExecuteCloseCommand);
            EditOrderCommand = new MvxCommand(ExecuteEditOrderCommand, () => CanEditOrder);
            PrintDetailsCommand = new MvxCommand(ExecutePrintDetailsCommand, () => CanPrintDetails);
            ChangeStatusCommand = new MvxAsyncCommand<string>(ExecuteChangeStatusAsync, ( _ ) => CanChangeStatus);
        }

        #endregion

        #region Commands

        public IMvxCommand CloseCommand { get; }
        public IMvxCommand EditOrderCommand { get; }
        public IMvxCommand PrintDetailsCommand { get; }
        public IMvxAsyncCommand<string> ChangeStatusCommand { get; }

        #endregion

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
                await LoadOrderDetailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load order details for ID: {OrderId}", _orderId);
                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Collection of order items for display
        /// </summary>
        public ObservableCollection<OrderItemDisplayDto> OrderItems
        {
            get => _orderItems;
            set => SetProperty(
                ref _orderItems,
                value,
                () =>
                {
                    RaisePropertyChanged(() => TotalItems);
                    RaisePropertyChanged(() => HasOrderItems);
                });
        }

        /// <summary>
        /// Whether the order has any items
        /// </summary>
        public bool HasOrderItems => OrderItems?.Count > 0;

        /// <summary>
        /// Total number of items
        /// </summary>
        public int TotalItems => OrderItems?.Count ?? 0;

        /// <summary>
        /// Order subtotal (sum of all item totals)
        /// </summary>
        public decimal OrderSubtotal
        {
            get => _orderSubtotal;
            set => SetProperty(ref _orderSubtotal, value);
        }

        /// <summary>
        /// Order total amount
        /// </summary>
        public decimal OrderTotal
        {
            get => _orderTotal;
            set => SetProperty(ref _orderTotal, value);
        }

        /// <summary>
        /// The order being displayed
        /// </summary>
        public OrderDto? Order
        {
            get => _order;
            protected set => SetProperty(
                ref _order,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanEditOrder);
                    RaisePropertyChanged(() => CanPrintDetails);
                    RaisePropertyChanged(() => CanChangeStatus);
                    RaisePropertyChanged(() => StatusDisplay);
                    RaisePropertyChanged(() => StatusColor);
                    RaisePropertyChanged(() => CurrentStatus);
                    RaisePropertyChanged(() => HasNotes);
                    RaisePropertyChanged(() => HasDeliveryDate);
                    RaisePropertyChanged(() => EntityName);
                    RaisePropertyChanged(() => LocationName);
                    RaisePropertyChanged(() => CreatedByName);
                    EditOrderCommand.RaiseCanExecuteChanged();
                    PrintDetailsCommand.RaiseCanExecuteChanged();
                    ChangeStatusCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether data is being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(
                ref _isLoading,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanEditOrder);
                    RaisePropertyChanged(() => CanPrintDetails);
                    RaisePropertyChanged(() => CanChangeStatus);
                    EditOrderCommand.RaiseCanExecuteChanged();
                    PrintDetailsCommand.RaiseCanExecuteChanged();
                    ChangeStatusCommand.RaiseCanExecuteChanged();
                });
        }

        #endregion

        #region Display Properties

        /// <summary>
        /// Display name for order type (Sales/Purchase)
        /// </summary>
        public abstract string OrderTypeDisplay { get; }

        /// <summary>
        /// Display name for entity (Customer/Supplier)
        /// </summary>
        public abstract string OrderEntityName { get; }

        /// <summary>
        /// Whether this is a purchase order
        /// </summary>
        public abstract bool IsPurchaseOrder { get; }

        /// <summary>
        /// Current status display text
        /// </summary>
        public string StatusDisplay => Order?.Status.ToString() ?? "Unknown";

        /// <summary>
        /// Current status for binding
        /// </summary>
        public OrderStatus? CurrentStatus => Order?.Status;

        /// <summary>
        /// Color for status badge
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (Order == null) return "#6B7280";

                return Order.Status switch
                {
                    OrderStatus.Draft     => "#F59E0B",
                    OrderStatus.Active    => "#3B82F6",
                    OrderStatus.Fulfilled => "#8B5CF6",
                    OrderStatus.Completed => "#10B981",
                    OrderStatus.Cancelled => "#EF4444",
                    _                     => "#6B7280"
                };
            }
        }

        /// <summary>
        /// Whether order has notes
        /// </summary>
        public bool HasNotes => !string.IsNullOrWhiteSpace(Order?.Notes);

        /// <summary>
        /// Whether order has delivery date
        /// </summary>
        public bool HasDeliveryDate => Order?.DeliveryDate != null;

        /// <summary>
        /// Customer or Supplier name
        /// </summary>
        public string EntityName
        {
            get
            {
                if (Order == null) return "N/A";

                if (IsPurchaseOrder && Order.SupplierId.HasValue)
                    return _supplierStore.GetSupplierName(Order.SupplierId.Value) ?? "Unknown Supplier";

                if (!IsPurchaseOrder && Order.CustomerId.HasValue)
                    return _customerStore.GetCustomerName(Order.CustomerId.Value) ?? "Unknown Customer";

                return "N/A";
            }
        }

        /// <summary>
        /// Location name
        /// </summary>
        public string LocationName
        {
            get
            {
                if (Order?.LocationId == null) return "N/A";
                return _locationStore.GetLocationName(Order.LocationId) ?? "Unknown Location";
            }
        }

        /// <summary>
        /// Created by user name
        /// </summary>
        public string CreatedByName
        {
            get
            {
                if (Order == null) return "N/A";
                return _userStore.GetUsername(Order.CreatedBy) ?? "Unknown User";
            }
        }

        public bool CanChangeToActive => Order?.Status is OrderStatus.Draft;

        public bool CanChangeToFulfilled => Order?.Status is OrderStatus.Active;

        public bool CanChangeToCompleted => Order?.Status is OrderStatus.Fulfilled;

        public bool CanChangeToCancelled => Order?.Status is OrderStatus.Draft or OrderStatus.Active;

        public bool CanChangeToAny => Order?.Status is not (OrderStatus.Completed or OrderStatus.Cancelled);

        #endregion

        #region Command Can Execute Properties

        public bool CanEditOrder => Order != null && !IsLoading;
        public bool CanPrintDetails => Order != null && !IsLoading;
        public bool CanChangeStatus => Order != null && !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads order details and related data
        /// </summary>
        // Update the LoadOrderDetailsAsync method to include order items
        private async Task LoadOrderDetailsAsync()
        {
            _logger.LogInformation("📋 Loading order details for ID: {OrderId}", _orderId);

            // Load order from cache
            OrderDto? order = _orderCacheReadService.GetOrderByIdInCache(_orderId);

            if (order == null)
            {
                _logger.LogWarning("⚠️ Order with ID {OrderId} not found in cache", _orderId);
                return;
            }

            Order = order;

            // Load order items
            await LoadOrderItemsAsync();

            _logger.LogInformation(
                "✅ Loaded {OrderType} order: #{OrderId} - Status: {Status} - Items: {ItemCount}",
                OrderTypeDisplay,
                Order.OrderId,
                Order.Status,
                TotalItems);
        }

// Add this new method for loading order items
        private async Task LoadOrderItemsAsync()
        {
            try
            {
                _logger.LogInformation("📦 Loading order items for order {OrderId}", _orderId);

                // Get order items from service
                DatabaseResult<IEnumerable<OrderItemDto>> items = await _orderItemService.GetOrderItemsByOrderIdAsync(_orderId);

                if (items is { IsSuccess: true, Value: not null })
                {
                    // Convert to display DTOs with product information
                    List<OrderItemDisplayDto> displayItems = items
                                                             .Value
                                                             .Select(( item, index ) => new OrderItemDisplayDto
                                                             {
                                                                 OrderItemId = item.OrderItemId,
                                                                 ItemNumber = index + 1,
                                                                 ProductId = item.ProductId,
                                                                 ProductName = _productStore.GetProductName(item.ProductId)
                                                                               ?? "Unknown Product",
                                                                 ProductSKU = _productStore.GetProductSku(item.ProductId),
                                                                 Quantity = item.Quantity,
                                                                 UnitPrice = item.UnitPrice,
                                                                 TotalPrice = item.Quantity * item.UnitPrice
                                                             })
                                                             .ToList();

                    OrderItems = new ObservableCollection<OrderItemDisplayDto>(displayItems);

                    // Calculate totals
                    CalculateOrderTotals();

                    _logger.LogInformation(
                        "✅ Loaded {ItemCount} order items - Total: ${Total:N2}",
                        OrderItems.Count,
                        OrderTotal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load order items for order {OrderId}", _orderId);
                OrderItems = [];
            }
        }

        private void CalculateOrderTotals()
        {
            OrderSubtotal = OrderItems?.Sum(item => item.TotalPrice) ?? 0;
            OrderTotal = OrderSubtotal; // Can add tax, shipping, discounts here in the future
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Closes the order details dialog
        /// </summary>
        private void ExecuteCloseCommand()
        {
            _logger.LogInformation("Closing order details view");
            _modalNavigationControl.Close();
        }

        /// <summary>
        /// Opens the order edit dialog
        /// </summary>
        private void ExecuteEditOrderCommand()
        {
            if (Order == null) return;

            _logger.LogInformation("Opening edit dialog for order {OrderId}", Order.OrderId);

            _modalNavigationControl.PopUp<OrderFormViewModelBase>(parameter: _orderId);

        }

        /// <summary>
        /// Prints order details
        /// </summary>
        private void ExecutePrintDetailsCommand()
        {
            if (Order == null) return;

            _logger.LogInformation("Printing details for order {OrderId}", Order.OrderId);

            // TODO: Implement print functionality
            // _printManager.PrintOrderDetails(
            //     Order,
            //     OrderEntityName,
            //     LocationName,
            //     CreatedByName,
            //     OrderItems,
            //     33m);
            // Example: _printService.PrintOrderDetails(Order, OrderItems);
        }

        /// <summary>
        /// Changes the order status 
        /// </summary>
        private async Task ExecuteChangeStatusAsync( string statusString )
        {
            if (Order == null || string.IsNullOrEmpty(statusString))
            {
                _logger.LogWarning("⚠️ Cannot change status: Order or status is null");
                return;
            }

            if (!Enum.TryParse(statusString, out OrderStatus newStatus))
            {
                _logger.LogWarning("⚠️ Invalid status value: {Status}", statusString);
                return;
            }

            // Don't change if already at this status
            if (Order.Status == newStatus)
            {
                _logger.LogDebug("Status unchanged: {Status}", newStatus);
                return;
            }

            // Validate status transition
            if (!IsValidStatusTransition(Order.Status, newStatus))
            {
                _logger.LogWarning(
                    "❌ Invalid status transition: {From} → {To}",
                    Order.Status,
                    newStatus);

                MessageBox.Show(
                    $"Cannot change status from {Order.Status} to {newStatus}.\n\n" +
                    GetStatusTransitionError(Order.Status, newStatus),
                    "Invalid Status Change",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Confirm status change with user
            MessageBoxResult result = MessageBox.Show(
                $"Change order status from {Order.Status} to {newStatus}?\n\n" +
                GetStatusChangeWarning(Order.Status, newStatus),
                "Confirm Status Change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                _logger.LogInformation("Status change cancelled by user");
                return;
            }

            IsLoading = true;

            // Store original status before change
            OrderStatus originalStatus = Order.Status;
            string orderNumber = $"{(Order.Type == OrderType.Sale ? "SO" : "PO")}-{Order.OrderId:D6}";

            try
            {
                _logger.LogInformation(
                    "🔄 Changing order {OrderId} status from {OldStatus} to {NewStatus}",
                    Order.OrderId,
                    Order.Status,
                    newStatus);


                switch (newStatus)
                {
                    case OrderStatus.Active:
                        await _orderService.ActivateOrderAsync(_orderId, originalStatus);
                        break;

                    case OrderStatus.Fulfilled:
                        await _orderService.FulfillOrderAsync(_orderId, originalStatus);

                        // Show fulfillment modal
                        await _orderFulfillmentHelper.HandleFulfillmentFlowAsync(
                            _orderId,
                            orderNumber,
                            Order.Type,
                            originalStatus,
                            async () => await RevertOrderStatusAsync(Order.Status));
                        // Reload to show updated stock
                        await LoadOrderDetailsAsync();
                        await LoadOrderItemsAsync();
                        return;

                    case OrderStatus.Completed:
                        await _orderService.CompleteOrderAsync(_orderId, originalStatus);
                        break;

                    case OrderStatus.Cancelled:
                        await _orderService.CancelOrderAsync(_orderId, originalStatus);
                        break;

                    case OrderStatus.Draft:
                        await _orderService.RevertToDraftOrderAsync(_orderId, originalStatus);
                        break;
                }

                // Reload order to reflect changes
                await LoadOrderDetailsAsync();
                await LoadOrderItemsAsync();

                _logger.LogInformation(
                    "✅ Successfully changed order {OrderId} status to {Status}",
                    Order.OrderId,
                    newStatus);

                MessageBox.Show(
                    $"Order status successfully changed to {newStatus}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to change order {OrderId} status to {Status}",
                    Order.OrderId,
                    newStatus);

                MessageBox.Show(
                    "Failed to change order status. Please try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RevertOrderStatusAsync( OrderStatus originalStatus )
        {
            try
            {
                UpdateOrderDto revertDto = new()
                {
                    OrderId = _orderId,
                    Status = originalStatus,
                    LocationId = Order.LocationId,
                    DeliveryDate = Order.DeliveryDate,
                    Notes = Order.Notes
                };

                await _orderService.UpdateOrderAsync(revertDto);
                await LoadOrderDetailsAsync(); // Reload to show reverted status

                _logger.LogInformation("Reverted order {OrderId} status to {Status}", _orderId, originalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverting order status");
            }
        }

        /// <summary>
        /// Validates if a status transition is allowed
        /// </summary>
        private bool IsValidStatusTransition( OrderStatus currentStatus, OrderStatus newStatus )
        {
            // Define allowed transitions
            return currentStatus switch
            {
                OrderStatus.Draft => newStatus is OrderStatus.Active or OrderStatus.Cancelled,

                OrderStatus.Active => newStatus is OrderStatus.Fulfilled or OrderStatus.Cancelled,

                OrderStatus.Fulfilled => newStatus is OrderStatus.Completed,
                // ❌ Cannot go back to Active or Draft after Fulfilled!

                OrderStatus.Completed => false,
                // ❌ Completed is final - no transitions allowed

                OrderStatus.Cancelled => false,
                // ❌ Cancelled is final - no transitions allowed

                _ => false
            };
        }

        /// <summary>
        /// Gets error message for invalid transitions
        /// </summary>
        private string GetStatusTransitionError( OrderStatus from, OrderStatus to )
        {
            return from switch
            {
                OrderStatus.Draft when to == OrderStatus.Fulfilled =>
                    "Draft orders must be activated before fulfillment.\n" +
                    "Please change to Active first.",

                OrderStatus.Draft when to == OrderStatus.Completed =>
                    "Draft orders cannot be completed directly.\n" +
                    "Please activate and fulfill the order first.",

                OrderStatus.Active when to == OrderStatus.Draft =>
                    "Active orders cannot be reverted to Draft.\n" +
                    "To make changes, cancel the order and create a new one.",

                OrderStatus.Active when to == OrderStatus.Completed =>
                    "Active orders must be fulfilled before completion.\n" +
                    "Please change to Fulfilled first.",

                OrderStatus.Fulfilled when to == OrderStatus.Draft =>
                    "Fulfilled orders cannot be reverted to Draft.\n" +
                    "Inventory has already been updated.",

                OrderStatus.Fulfilled when to == OrderStatus.Active =>
                    "⚠️ CRITICAL: Fulfilled orders cannot be reverted to Active.\n\n" +
                    "Reason: Inventory has already been updated:\n" +
                    (Order.Type == OrderType.Sale
                        ? "• Stock was decreased when order was fulfilled\n" +
                          "• Reserved stock was released\n"
                        : "• Stock was increased when order was fulfilled\n") +
                    "• Transaction records were created\n\n" +
                    "If you need to make changes:\n" +
                    "1. Complete this order\n" +
                    "2. Create a new order with corrections\n" +
                    "3. Or manually adjust inventory if needed",

                OrderStatus.Fulfilled when to == OrderStatus.Cancelled =>
                    "Fulfilled orders cannot be cancelled.\n" +
                    "Inventory has already been updated. Please complete the order instead.",

                OrderStatus.Completed when to != OrderStatus.Completed =>
                    "Completed orders are final and cannot be changed.\n" +
                    "Create a new order if corrections are needed.",

                OrderStatus.Cancelled when to != OrderStatus.Cancelled =>
                    "Cancelled orders are final and cannot be changed.\n" +
                    "Create a new order if needed.",

                _ => $"Cannot change status from {from} to {to}."
            };
        }

        /// <summary>
        /// Gets warning message for status change
        /// </summary>
        private string GetStatusChangeWarning( OrderStatus from, OrderStatus to )
        {
            if (to == OrderStatus.Active)
            {
                return IsPurchaseOrder
                    ? "This will mark the order as active and in progress."
                    : "This will RESERVE stock for this order.";
            }

            if (to == OrderStatus.Fulfilled)
            {
                return IsPurchaseOrder
                    ? "This will mark goods as received and prompt inventory INCREASE."
                    : "This will mark goods as shipped and prompt inventory DECREASE.";
            }

            if (to == OrderStatus.Completed)
            {
                return "This will close the order. No further changes can be made.";
            }

            if (to == OrderStatus.Cancelled)
            {
                return from == OrderStatus.Active && !IsPurchaseOrder
                    ? "This will cancel the order and RELEASE reserved stock."
                    : "This will cancel the order.";
            }

            return "This will change the order status.";
        }

        #endregion
    }
}
