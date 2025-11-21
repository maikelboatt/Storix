using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Products;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;
using Storix.Core.InputModel;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders
{
    /// <summary>
    /// Base ViewModel for order forms (Sales and Purchase)
    /// Contains shared logic for order creation and editing using InputModel pattern
    /// </summary>
    public abstract class OrderFormViewModelBase:MvxViewModel<int>
    {
        protected readonly IOrderService _orderService;
        protected readonly IOrderItemManager _orderItemManager;
        protected readonly IOrderItemService _orderItemService;
        protected readonly IProductCacheReadService _productCacheReadService;
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

        // TotalAmount is calculated from OrderItems
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
        private List<ProductDto> _availableProducts = new();
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

        protected OrderFormViewModelBase(
            IOrderService orderService,
            IOrderItemManager orderItemManager,
            IProductCacheReadService productCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger logger )
        {
            _orderService = orderService;
            _orderItemManager = orderItemManager;
            _productCacheReadService = productCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            _orderInputModel = new OrderInputModel();

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

        #region Abstract Methods

        protected abstract Task LoadEntitySpecificDataAsync();

        protected abstract void InitializeForCreate();

        protected abstract void LoadEntitySpecificDataForEdit( OrderDto orderDto );

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
                    // Create UpdateOrderDto from OrderDto
                    UpdateOrderDto updateDto = new()
                    {
                        OrderId = orderDto.OrderId,
                        Status = orderDto.Status,
                        DeliveryDate = orderDto.DeliveryDate,
                        Notes = orderDto.Notes
                    };

                    Input = new OrderInputModel(updateDto);
                    Input.OrderId = _orderId; // Ensure OrderId is set for edit mode

                    // OrderNumber is not in OrderDto, so we generate it or retrieve it separately
                    // For now, generate a placeholder - you may need to add OrderNumber to your Order domain model
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
                }
                else
                {
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

        protected virtual async Task PerformCreateAsync()
        {
            CreateOrderDto createDto = Input.ToCreateDto();
            DatabaseResult<OrderDto> result = await _orderService.CreateOrderAsync(createDto);

            if (result.IsSuccess && result.Value != null)
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
            DatabaseResult<OrderDto> result = await _orderService.UpdateOrderAsync(updateDto);

            if (result.IsSuccess)
            {
                // Update order items (delete and recreate for simplicity)
                await _orderItemManager.DeleteOrderItemsByOrderIdAsync(_orderId);

                IEnumerable<CreateOrderItemDto> orderItems = OrderItems.Select(item => new CreateOrderItemDto
                {
                    OrderId = _orderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });

                await _orderItemManager.CreateBulkOrderItemsAsync(orderItems);
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

    /// <summary>
    /// Represents a single row in the order items grid
    /// </summary>
    public class OrderItemRowViewModel:MvxNotifyPropertyChanged
    {
        private readonly List<ProductDto> _availableProducts;

        private int? _orderItemId;
        private int _productId;
        private string? _productName;
        private int _quantity;
        private decimal _unitPrice;

        public OrderItemRowViewModel( List<ProductDto> availableProducts )
        {
            _availableProducts = availableProducts;
        }

        // Event fired when product is selected
        public event EventHandler<int>? ProductSelected;

        public int? OrderItemId
        {
            get => _orderItemId;
            set => SetProperty(ref _orderItemId, value);
        }

        public int ProductId
        {
            get => _productId;
            set
            {
                if (SetProperty(ref _productId, value))
                {
                    // Update product name
                    ProductDto? product = _availableProducts.FirstOrDefault(p => p.ProductId == value);
                    if (product != null)
                    {
                        ProductName = product.Name;
                        // Notify that product was selected
                        ProductSelected?.Invoke(this, value);
                    }
                }
            }
        }

        public string? ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    RaisePropertyChanged(() => LineTotal);
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    RaisePropertyChanged(() => LineTotal);
                }
            }
        }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}
