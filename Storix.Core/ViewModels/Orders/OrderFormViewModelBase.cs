using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Products;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Dialog;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;
using Storix.Core.Helper;
using Storix.Core.InputModel;
using Storix.Core.ViewModels.Orders.Fulfillment;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Orders
{
    /// <summary>
    /// Base ViewModel for order forms (Sales and Purchase)
    /// Contains shared logic for order creation and editing using InputModel pattern
    /// </summary>
    public abstract class OrderFormViewModelBase:MvxViewModel<int>
    {
        protected readonly IOrderService _orderService;
        private readonly IOrderCoordinatorService _orderCoordinatorService;
        protected readonly IOrderItemManager _orderItemManager;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        protected readonly IOrderItemService _orderItemService;
        private readonly IOrderFulfillmentService _orderFulfillmentService;
        private readonly IDialogService _dialogService;
        protected readonly IProductCacheReadService _productCacheReadService;
        private readonly ILocationCacheReadService _locationCacheReadService;
        private readonly IOrderFulfillmentHelper _orderFulfillmentHelper;
        protected readonly IModalNavigationControl _modalNavigationControl;
        protected readonly ILogger _logger;

        // Current user ID (should come from authentication service)
        protected int CurrentUserId { get; set; } = 1; // TODO: Get from auth service

        private OrderInputModel _orderInputModel;
        private int _orderId;
        private bool _isEditMode;
        private bool _isLoading;
        private string _orderNumber = string.Empty;
        private decimal _totalAmount;

        private OrderStatus _originalStatus;

        protected OrderFormViewModelBase(
            IOrderService orderService,
            IOrderCoordinatorService orderCoordinatorService,
            IOrderItemService orderItemService,
            IOrderFulfillmentService orderFulfillmentService,
            IDialogService dialogService,
            IOrderItemManager orderItemManager,
            IInventoryCacheReadService inventoryCacheReadService,
            IProductCacheReadService productCacheReadService,
            ILocationCacheReadService locationCacheReadService,
            IOrderFulfillmentHelper orderFulfillmentHelper,
            IModalNavigationControl modalNavigationControl,
            ILogger logger )
        {
            _orderService = orderService;
            _orderCoordinatorService = orderCoordinatorService;
            _orderItemService = orderItemService;
            _orderFulfillmentService = orderFulfillmentService;
            _dialogService = dialogService;
            _orderItemManager = orderItemManager;
            _inventoryCacheReadService = inventoryCacheReadService;
            _productCacheReadService = productCacheReadService;
            _locationCacheReadService = locationCacheReadService;
            _orderFulfillmentHelper = orderFulfillmentHelper;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            _orderInputModel = new OrderInputModel();

            // Initialise commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
            AddOrderItemCommand = new MvxCommand(AddOrderItem);
            RemoveOrderItemCommand = new MvxCommand<OrderItemRowViewModel>(RemoveOrderItem);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);

            // Subscribe to order items collection changes
            _orderItems.CollectionChanged += ( s, e ) =>
            {
                CalculateTotalAmount();
                RaisePropertyChanged(() => CanSave);
                SaveCommand.RaiseCanExecuteChanged();
            };
        }


        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _orderId = parameter;
            _logger.LogInformation("🔧 Preparing OrderFormViewModel with OrderId: {OrderId}", _orderId);
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                _logger.LogInformation(
                    "🔄 Initializing {FormType}. OrderId: {OrderId}, Mode: {Mode}",
                    GetType()
                        .Name,
                    _orderId,
                    _orderId > 0
                        ? "EDIT"
                        : "CREATE");

                await LoadProductsAsync();
                await LoadEntitySpecificDataAsync(); // Load customers or suppliers

                if (_orderId > 0)
                {
                    // Editing Mode
                    await LoadOrderForEditAsync();
                }
                else
                {
                    // Create Mode
                    InitializeForCreate();
                }

                SubscribeToInputModelEvents();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            UnsubscribeFromInputModelEvents();

            // Unsubscribe from all order item events
            foreach (OrderItemRowViewModel item in OrderItems)
            {
                item.PropertyChanged -= OnOrderItemPropertyChanged;
                item.ProductSelected -= OnOrderItemProductSelected;
            }

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        #region Properties

        public OrderInputModel Input
        {
            get => _orderInputModel;
            protected set
            {
                if (_orderInputModel != value)
                {
                    UnsubscribeFromInputModelEvents();
                    SetProperty(ref _orderInputModel, value);
                    SubscribeToInputModelEvents();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                SaveCommand?.RaiseCanExecuteChanged();
                ResetCommand?.RaiseCanExecuteChanged();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                SetProperty(ref _isEditMode, value);
                RaisePropertyChanged(() => Title);
                RaisePropertyChanged(() => SaveButtonText);
            }
        }

        // OrderNumber is stored in ViewModel, not in InputModel
        public string OrderNumber
        {
            get => _orderNumber;
            set => SetProperty(ref _orderNumber, value);
        }

        // TotalPrice is calculated from OrderItems
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        // Order Items
        private ObservableCollection<OrderItemRowViewModel> _orderItems = new();
        public ObservableCollection<OrderItemRowViewModel> OrderItems
        {
            get => _orderItems;
            set => SetProperty(ref _orderItems, value);
        }

        // Available products for selection
        private List<ProductDto> _availableProducts = [];
        public List<ProductDto> AvailableProducts
        {
            get => _availableProducts;
            set => SetProperty(ref _availableProducts, value);
        }

        // Order statuses for dropdown
        public List<OrderStatus> OrderStatuses { get; } = Enum
                                                          .GetValues(typeof(OrderStatus))
                                                          .Cast<OrderStatus>()
                                                          .ToList();

        #endregion

        #region Abstract Properties

        protected abstract OrderType OrderType { get; }
        public abstract string Title { get; }
        public abstract string SaveButtonText { get; }
        public abstract string FormIcon { get; }

        #endregion

        #region Validation Properties

        public bool IsValid => _orderInputModel?.IsValid ?? false;
        public bool HasErrors => _orderInputModel?.HasErrors ?? false;
        public bool CanSave => IsValid && OrderItems.Any() && !string.IsNullOrWhiteSpace(OrderNumber) && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Commands

        public IMvxAsyncCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }
        public IMvxCommand AddOrderItemCommand { get; }
        public IMvxCommand<OrderItemRowViewModel> RemoveOrderItemCommand { get; }
        public IMvxCommand ResetCommand { get; }

        #endregion

        #region Abstract Methods

        protected abstract Task LoadEntitySpecificDataAsync();

        protected abstract void InitializeForCreate();

        protected abstract void LoadEntitySpecificDataForEdit( OrderDto orderDto );

        protected abstract Task PopulateInputCollectionsAsync();

        protected abstract string GenerateOrderNumber();

        #endregion

        #region Data Loading

        protected virtual async Task LoadProductsAsync()
        {
            try
            {
                AvailableProducts = _productCacheReadService
                                    .GetActiveProductsFromCache()
                                    .ToList();
                _logger.LogInformation("Loaded {Count} products for order form", AvailableProducts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
            }
        }

        protected virtual async Task LoadOrderForEditAsync()
        {
            try
            {
                OrderDto? orderDto = _orderService.GetOrderById(_orderId);
                if (orderDto != null)
                {
                    _originalStatus = orderDto.Status;

                    // Create UpdateOrderDto from OrderDto
                    UpdateOrderDto updateDto = new()
                    {
                        OrderId = orderDto.OrderId,
                        Status = orderDto.Status,
                        LocationId = orderDto.LocationId,
                        DeliveryDate = orderDto.DeliveryDate,
                        Notes = orderDto.Notes
                    };

                    Input = new OrderInputModel(updateDto)
                    {
                        OrderId = _orderId, // Ensure OrderId is set for edit mode
                        OrderDate = orderDto.OrderDate
                    };

                    await PopulateInputCollectionsAsync();

                    Input.LocationId = orderDto.LocationId;

                    // OrderNumber is not in OrderDto, so we generate it or retrieve it separately
                    OrderNumber = $"{(orderDto.Type == OrderType.Sale ? "SO" : "PO")}-{orderDto.OrderId:D6}";

                    // Load entity-specific data (customer/supplier)
                    LoadEntitySpecificDataForEdit(orderDto);

                    // Load order items
                    DatabaseResult<IEnumerable<OrderItemDto>> itemsResult = await _orderItemService.GetOrderItemsByOrderIdAsync(_orderId);
                    if (itemsResult is { IsSuccess: true, Value: not null })
                    {
                        OrderItems.Clear();
                        foreach (OrderItemDto item in itemsResult.Value)
                        {
                            ProductDto? product = AvailableProducts.FirstOrDefault(p => p.ProductId == item.ProductId);
                            if (product != null)
                            {
                                OrderItemRowViewModel rowVM = new(AvailableProducts)
                                {
                                    OrderItemId = item.OrderItemId,
                                    ProductId = item.ProductId,
                                    ProductName = product.Name,
                                    Quantity = item.Quantity,
                                    UnitPrice = item.UnitPrice
                                };

                                // Subscribe to property changes for recalculation
                                rowVM.PropertyChanged += OnOrderItemPropertyChanged;
                                rowVM.ProductSelected += OnOrderItemProductSelected;
                                OrderItems.Add(rowVM);
                            }
                        }
                    }

                    IsEditMode = true;
                    CalculateTotalAmount();
                    RaiseAllPropertiesChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order for edit");
            }
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteSaveCommandAsync()
        {
            if (!Input.Validate() || !OrderItems.Any() || string.IsNullOrWhiteSpace(OrderNumber))
            {
                _logger.LogWarning("Validation failed or missing required data");
                return;
            }

            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    await PerformUpdateAsync();

                    // Check if status changed to Fulfilled
                    if (Input.Status == OrderStatus.Fulfilled &&
                        _originalStatus != OrderStatus.Fulfilled)
                    {

                        _logger.LogInformation(
                            "Order {OrderNumber} status changed to Fulfilled - opening fulfillment dialog",
                            OrderNumber);

                        _modalNavigationControl.Close();

                        // Open fulfillment modal
                        await ShowFulfillmentModalAsync();
                        return;
                    }
                }
                else
                {
                    // Check stock before creating new sales order
                    if (Input.Type == OrderType.Sale)
                    {
                        if (!ValidateStockAvailability())
                            return;
                    }
                    await PerformCreateAsync();
                }

                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateStockAvailability()
        {
            // Check if location is selected
            if (Input.LocationId <= 0)
            {
                _dialogService.ShowWarning("Please select a location before creating the order.");
                return false;
            }

            // Get location names for better error messages
            string locationName = Input.Locations?.FirstOrDefault(l => l.LocationId == Input.LocationId)
                                       ?.Name ?? $"Location ID {Input.LocationId}";

            List<string> stockErrors = new();

            foreach (OrderItemRowViewModel orderItem in OrderItems)
            {
                // Skip items without product selected
                if (orderItem.ProductId <= 0)
                    continue;

                // Quick check against available products
                Inventory? inventory = _inventoryCacheReadService.GetInventoryByProductAndLocationInCache(
                    orderItem.ProductId,
                    Input.LocationId);

                if (inventory == null)
                {
                    _logger.LogWarning(
                        "No inventory found for product {ProductId} ({ProductName}) at location {LocationId}",
                        orderItem.ProductId,
                        orderItem.ProductName,
                        Input.LocationId);

                    stockErrors.Add($"• {orderItem.ProductName}: Not available at this location");
                    continue; // Check other items instead of returning
                }

                int availableQuantity = inventory.AvailableStock;

                if (availableQuantity < orderItem.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient stock for product {ProductId} {ProductName}. Available: {Available}, Requested: {Requested}",
                        orderItem.ProductId,
                        orderItem.ProductName,
                        availableQuantity,
                        orderItem.Quantity);

                    stockErrors.Add($". {orderItem.ProductName}: Available {availableQuantity}, Requested {orderItem.Quantity}");
                }
            }

            // Show all errors at once if any were found
            if (stockErrors.Count != 0)
            {
                string message = $"Inventory at location '{locationName}' has insufficient stock:\n\n" +
                                 string.Join("\n", stockErrors) +
                                 "\n\nPlease adjust quantities or select a different location.";

                _dialogService.ShowWarning(message, "Insufficient Stock");
                return false;
            }

            _logger.LogInformation("Stock validation passed for location {LocationId}", Input.LocationId);
            return true;
        }

        private async Task ShowFulfillmentModalAsync()
        {
            await _orderFulfillmentHelper.HandleFulfillmentFlowAsync(
                _orderId,
                _orderNumber,
                OrderType,
                _originalStatus,
                RevertOrderStatusAsync);
        }

        private async Task RevertOrderStatusAsync()
        {
            try
            {
                UpdateOrderDto revertDto = new()
                {
                    OrderId = _orderId,
                    Status = _originalStatus,
                    LocationId = Input.LocationId,
                    DeliveryDate = Input.DeliveryDate,
                    Notes = Input.Notes
                };

                await _orderService.UpdateOrderAsync(revertDto);
                _logger.LogInformation("Reverted order {OrderId} status to {Status}", _orderId, _originalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverting order status");
            }
        }

        protected virtual async Task PerformCreateAsync()
        {
            CreateOrderDto createDto = Input.ToCreateDto();
            DatabaseResult<OrderDto> result = await _orderService.CreateOrderAsync(createDto);

            if (result is { IsSuccess: true, Value: not null })
            {
                // Create order items
                IEnumerable<CreateOrderItemDto> orderItems = OrderItems.Select(item => new CreateOrderItemDto
                {
                    OrderId = result.Value.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });

                await _orderItemManager.CreateBulkOrderItemsAsync(orderItems);
                _logger.LogInformation(
                    "Successfully created order {OrderId} with number {OrderNumber}",
                    result.Value.OrderId,
                    OrderNumber);
            }
        }

        protected virtual async Task PerformUpdateAsync()
        {
            if (_orderId <= 0) return;

            UpdateOrderDto updateDto = Input.ToUpdateDto();

            // Convert ViewModels to DTOs
            IEnumerable<OrderItemUpdateDto> orderItemDtos = OrderItems.Select(item => new OrderItemUpdateDto
            {
                OrderItemId = item.OrderItemId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });

            // Use coordinator service
            DatabaseResult<OrderDto> result = await _orderCoordinatorService.UpdateOrderWithItemsAsync(
                updateDto,
                orderItemDtos);

            // DatabaseResult<OrderDto> result = await _orderService.UpdateOrderAsync(updateDto);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to update order: {ErrorMessage}", result.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("Successfully updated order {OrderId}", _orderId);
            }
        }

        private void ExecuteCancelCommand()
        {
            _modalNavigationControl.Close();
        }

        private void ExecuteResetCommand()
        {
            if (IsEditMode && _orderId > 0)
            {
                // Reload original data
                _ = LoadOrderForEditAsync();
            }
            else
            {
                // Clear form
                ResetForm();
            }
        }

        protected virtual void AddOrderItem()
        {
            OrderItemRowViewModel newItem = new(AvailableProducts)
            {
                Quantity = 1,
                UnitPrice = 0
            };

            newItem.PropertyChanged += OnOrderItemPropertyChanged;
            newItem.ProductSelected += OnOrderItemProductSelected;
            OrderItems.Add(newItem);
        }

        protected virtual void RemoveOrderItem( OrderItemRowViewModel item )
        {
            if (item != null)
            {
                item.PropertyChanged -= OnOrderItemPropertyChanged;
                item.ProductSelected -= OnOrderItemProductSelected;
                OrderItems.Remove(item);
            }
        }

        // private async Task ExecuteCompleteFulfillmentAsync()
        // {
        //     if (SelectedLocationId <= 0)
        //     {
        //         _logger.LogWarning("No location selected for fulfillment");
        //         return;
        //     }
        //
        //     IsLoading = true;
        //
        //     try
        //     {
        //         DatabaseResult result;
        //
        //         if (OrderType == OrderType.Purchase)
        //         {
        //             result = await _orderFulfillmentService.FulfillPurchaseOrderAsync(
        //                 _orderId,
        //                 SelectedLocationId,
        //                 CurrentUserId);
        //         }
        //         else // Sales Order
        //         {
        //             result = await _orderFulfillmentService.FulfillSalesOrderAsync(
        //                 _orderId,
        //                 SelectedLocationId,
        //                 CurrentUserId);
        //         }
        //
        //         if (result.IsSuccess)
        //         {
        //             _logger.LogInformation("Successfully fulfilled order {OrderId}", _orderId);
        //
        //             // Update order status to Completed
        //             await UpdateOrderStatusToCompletedAsync();
        //
        //             _modalNavigationControl.Close();
        //         }
        //         else
        //         {
        //             _logger.LogError("Failed to fulfill order: {ErrorMessage}", result.ErrorMessage);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error completing fulfillment");
        //     }
        //     finally
        //     {
        //         IsLoading = false;
        //     }
        // }

        // private async Task UpdateOrderStatusToCompletedAsync()
        // {
        //     UpdateOrderDto completedDto = new()
        //     {
        //         OrderId = _orderId,
        //         Status = OrderStatus.Completed,
        //         DeliveryDate = DateTime.Now,
        //         Notes = Input.Notes
        //     };
        //
        //     await _orderService.UpdateOrderAsync(completedDto);
        // }

        protected void CalculateTotalAmount()
        {
            TotalAmount = OrderItems.Sum(item => item.LineTotal);
        }

        private void OnOrderItemPropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            if (e.PropertyName == nameof(OrderItemRowViewModel.Quantity) ||
                e.PropertyName == nameof(OrderItemRowViewModel.UnitPrice) ||
                e.PropertyName == nameof(OrderItemRowViewModel.LineTotal))
            {
                CalculateTotalAmount();
            }
        }

        private void OnOrderItemProductSelected( object? sender, int productId )
        {
            if (sender is OrderItemRowViewModel rowVM)
            {
                ProductDto? product = AvailableProducts.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    rowVM.UnitPrice = product.Price; // Automatically set unit price from product
                    _logger.LogDebug("Set unit price {Price} for product {ProductName}", product.Price, product.Name);
                }
            }
        }

        protected virtual void ResetForm()
        {
            Input = new OrderInputModel();
            OrderItems.Clear();
            TotalAmount = 0;
            OrderNumber = string.Empty;
            IsEditMode = false;
            InitializeForCreate();
            RaiseAllPropertiesChanged();
        }

        #endregion

        #region Event Handling

        protected void SubscribeToInputModelEvents()
        {
            if (_orderInputModel == null) return;
            _orderInputModel.PropertyChanged += OnInputModelPropertyChanged;
            _orderInputModel.ErrorsChanged += OnInputModelErrorsChanged;
        }

        protected void UnsubscribeFromInputModelEvents()
        {
            if (_orderInputModel == null) return;
            _orderInputModel.PropertyChanged -= OnInputModelPropertyChanged;
            _orderInputModel.ErrorsChanged -= OnInputModelErrorsChanged;
        }

        private void OnInputModelPropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnInputModelErrorsChanged( object? sender, DataErrorsChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            SaveCommand.RaiseCanExecuteChanged();
        }

        protected void RaiseAllPropertiesChanged()
        {
            RaisePropertyChanged(() => Title);
            RaisePropertyChanged(() => SaveButtonText);
            RaisePropertyChanged(() => IsEditMode);
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            RaisePropertyChanged(() => CanCancel);
            RaisePropertyChanged(() => OrderNumber);
            RaisePropertyChanged(() => TotalAmount);
        }

        #endregion
    }
}
