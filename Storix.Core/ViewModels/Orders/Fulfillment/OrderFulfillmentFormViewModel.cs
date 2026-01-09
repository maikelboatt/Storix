using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Dialog;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Fulfillment
{
    /// <summary>
    /// ViewModel for the Order Fulfillment form
    /// Handles inventory updates when orders are fulfilled
    /// </summary>
    public class OrderFulfillmentFormViewModel:MvxViewModel<OrderFulfillmentParameter>
    {
        private readonly IOrderService _orderService;
        private readonly IOrderItemService _orderItemService;
        private readonly IDialogService _dialogService;
        private readonly IOrderFulfillmentService _orderFulfillmentService;
        private readonly ILocationCacheReadService _locationCacheReadService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<OrderFulfillmentFormViewModel> _logger;

        private int _orderId;
        private OrderType _orderType;
        private string _orderNumber = string.Empty;
        private int _selectedLocationId;
        private DateTime _fulfillmentDate = DateTime.Now;
        private string? _notes;
        private bool _isLoading;
        private bool _isProcessing;

        public OrderFulfillmentFormViewModel(
            IOrderService orderService,
            IOrderItemService orderItemService,
            IDialogService dialogService,
            IOrderFulfillmentService orderFulfillmentService,
            ILocationCacheReadService locationCacheReadService,
            IProductCacheReadService productCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<OrderFulfillmentFormViewModel> logger )
        {
            _orderService = orderService;
            _orderItemService = orderItemService;
            _dialogService = dialogService;
            _orderFulfillmentService = orderFulfillmentService;
            _locationCacheReadService = locationCacheReadService;
            _productCacheReadService = productCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            ConfirmFulfillmentCommand = new MvxAsyncCommand(ExecuteConfirmFulfillmentAsync, () => CanConfirm);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => !IsProcessing);
        }

        #region Lifecycle

        public override void Prepare( OrderFulfillmentParameter parameter )
        {
            _orderId = parameter.OrderId;
            _orderType = parameter.OrderType;
            _orderNumber = parameter.OrderNumber;

            _logger.LogInformation(
                "Preparing OrderFulfillmentFormViewModel for Order {OrderNumber} (ID: {OrderId}, Type: {OrderType})",
                _orderNumber,
                _orderId,
                _orderType);
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadLocationsAsync();
                await LoadOrderItemsAsync();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        #endregion

        #region Properties

        public string Title => _orderType == OrderType.Purchase
            ? "Receive Purchase Order"
            : "Ship Sales Order";

        public string LocationLabel => _orderType == OrderType.Purchase
            ? "Receiving Location:"
            : "Shipping Location:";

        public string ConfirmButtonText => _orderType == OrderType.Purchase
            ? "Confirm Receipt"
            : "Confirm Shipment";

        public string FormIcon => _orderType == OrderType.Purchase
            ? "📦"
            : "🚚";

        public string OrderNumber
        {
            get => _orderNumber;
            set => SetProperty(ref _orderNumber, value);
        }

        public int SelectedLocationId
        {
            get => _selectedLocationId;
            set
            {
                if (SetProperty(ref _selectedLocationId, value))
                {
                    RaisePropertyChanged(() => CanConfirm);
                    ConfirmFulfillmentCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime FulfillmentDate
        {
            get => _fulfillmentDate;
            set => SetProperty(ref _fulfillmentDate, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                SetProperty(ref _isProcessing, value);
                ConfirmFulfillmentCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<LocationDto> _availableLocations = [];
        public ObservableCollection<LocationDto> AvailableLocations
        {
            get => _availableLocations;
            set => SetProperty(ref _availableLocations, value);
        }

        private ObservableCollection<FulfillmentItemViewModel> _fulfillmentItems = [];
        public ObservableCollection<FulfillmentItemViewModel> FulfillmentItems
        {
            get => _fulfillmentItems;
            set => SetProperty(ref _fulfillmentItems, value);
        }

        public bool CanConfirm => SelectedLocationId > 0 &&
                                  FulfillmentItems.Any() &&
                                  FulfillmentItems.All(x => x.QuantityToFulfill > 0) &&
                                  !IsProcessing;

        #endregion

        #region Commands

        public IMvxAsyncCommand ConfirmFulfillmentCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion

        #region Data Loading

        private async Task LoadLocationsAsync()
        {
            try
            {
                List<LocationDto> locations = _locationCacheReadService
                                              .GetAllActiveLocationsInCache()
                                              .ToList();

                AvailableLocations = new ObservableCollection<LocationDto>(locations);

                // Auto-select first location
                if (AvailableLocations.Any())
                {
                    SelectedLocationId = AvailableLocations.First()
                                                           .LocationId;
                }

                _logger.LogInformation("Loaded {Count} locations", locations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading locations");
            }

        }

        private async Task LoadOrderItemsAsync()
        {
            try
            {
                DatabaseResult<IEnumerable<OrderItemDto>> result =
                    await _orderItemService.GetOrderItemsByOrderIdAsync(_orderId);

                if (result is { IsSuccess: true, Value: not null })
                {
                    List<FulfillmentItemViewModel> items = result
                                                           .Value.Select(item =>
                                                           {
                                                               ProductDto? product = _productCacheReadService
                                                                   .GetProductByIdFromCache(item.ProductId);

                                                               return new FulfillmentItemViewModel
                                                               {
                                                                   OrderItemId = item.OrderItemId,
                                                                   ProductId = item.ProductId,
                                                                   ProductName = product?.Name ?? "Unknown Product",
                                                                   SKU = product?.SKU ?? "",
                                                                   OrderedQuantity = item.Quantity,
                                                                   QuantityToFulfill = item.Quantity, // Default to full quantity
                                                                   UnitPrice = item.UnitPrice
                                                               };
                                                           })
                                                           .ToList();

                    FulfillmentItems = new ObservableCollection<FulfillmentItemViewModel>(items);

                    // Subscribe to quantity changes
                    foreach (FulfillmentItemViewModel item in FulfillmentItems)
                    {
                        item.PropertyChanged += ( s, e ) =>
                        {
                            if (e.PropertyName == nameof(FulfillmentItemViewModel.QuantityToFulfill))
                            {
                                RaisePropertyChanged(() => CanConfirm);
                                ConfirmFulfillmentCommand.RaiseCanExecuteChanged();
                            }
                        };
                    }

                    _logger.LogInformation("Loaded {Count} order items", items.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order items");
            }
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteConfirmFulfillmentAsync()
        {
            if (SelectedLocationId <= 0 || !FulfillmentItems.Any())
            {
                _logger.LogWarning("Cannot confirm fulfillment: Invalid location or no items");
                return;
            }

            IsProcessing = true;

            try
            {
                DatabaseResult result;

                if (_orderType == OrderType.Purchase)
                {
                    result = await _orderFulfillmentService.FulfillPurchaseOrderAsync(
                        _orderId,
                        SelectedLocationId,
                        1); // TODO: Get actual user ID from auth service
                }
                else
                {
                    result = await _orderFulfillmentService.FulfillSalesOrderAsync(
                        _orderId,
                        SelectedLocationId,
                        1); // TODO: Get actual user ID from auth service
                }

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Successfully fulfilled order {OrderNumber} at location {LocationId}",
                        _orderNumber,
                        SelectedLocationId);

                    // Close the modal and signal success
                    _modalNavigationControl.Close(
                        new OrderFulfillmentResult
                        {
                            Success = true,
                            OrderId = _orderId,
                            LocationId = SelectedLocationId
                        });
                }
                else
                {
                    _logger.LogError(
                        "Failed to fulfill order {OrderNumber}: {Error}",
                        _orderNumber,
                        result.ErrorMessage);

                    _dialogService.ShowError($"Failed to fulfill order {OrderNumber}. Please try again.");

                    _modalNavigationControl.Close(
                        new OrderFulfillmentResult
                        {
                            Success = false,
                            ErrorMessage = result.ErrorMessage
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming fulfillment for order {OrderNumber}", _orderNumber);

                _modalNavigationControl.Close(
                    new OrderFulfillmentResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    }
                );
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("User cancelled fulfillment for order {OrderNumber}", _orderNumber);

            _modalNavigationControl.Close(
                new OrderFulfillmentResult
                {
                    Success = false,
                    Cancelled = true
                }
            );
        }

        #endregion
    }
}
