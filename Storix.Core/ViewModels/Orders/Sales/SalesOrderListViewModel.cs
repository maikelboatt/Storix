using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Locations;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Users;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Users.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Core.Control;
using Storix.Core.ViewModels.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Orders.Sales
{
    public class SalesOrderListViewModel:MvxViewModel<OrderListViewModelParameter>
    {
        private readonly IOrderService _orderService;
        private readonly IOrderItemManager _orderItemManager;
        private readonly IOrderStore _orderStore;
        private readonly IOrderCacheReadService _orderCacheReadService;
        private readonly ICustomerCacheReadService _customerCacheReadService;
        private readonly IUserCacheReadService _userCacheReadService;
        private readonly ILocationCacheReadService _locationCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SalesOrderListViewModel> _logger;

        private MvxObservableCollection<SalesOrderListItemViewModel> _salesOrders = [];
        private List<SalesOrderListItemViewModel> _allSalesOrders = [];
        private string _searchText = string.Empty;
        private bool _isLoading;
        private OrderListViewModelParameter _filter;
        private string _filterTitle = string.Empty;
        private string _filterSubtitle = string.Empty;

        public SalesOrderListViewModel(
            IOrderService orderService,
            IOrderItemManager orderItemManager,
            IOrderStore orderStore,
            IOrderCacheReadService orderCacheReadService,
            ICustomerCacheReadService customerCacheReadService,
            IUserCacheReadService userCacheReadService,
            ILocationCacheReadService locationCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<SalesOrderListViewModel> logger )
        {
            _orderService = orderService;
            _orderItemManager = orderItemManager;
            _orderStore = orderStore;
            _orderCacheReadService = orderCacheReadService;
            _customerCacheReadService = customerCacheReadService;
            _userCacheReadService = userCacheReadService;
            _locationCacheReadService = locationCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to store events
            _orderStore.OrderAdded += OnOrderAdded;
            _orderStore.OrderUpdated += OnOrderUpdated;
            _orderStore.OrderDeleted += OnOrderDeleted;

            // Initialize commands
            OpenSalesOrderFormCommand = new MvxCommand<int>(ExecuteOpenSalesOrderForm);
            OpenSalesOrderDetailsCommand = new MvxCommand<int>(ExecuteOpenSalesOrderDetails);
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
                    string customerName = _orderCacheReadService.GetCustomerNameInCache(order.CustomerId ?? 0);
                    string locationName = _orderCacheReadService.GetLocationNameInCache(order.LocationId);
                    DatabaseResult<decimal> totalAmountResult =
                        await _orderItemManager.GetOrderTotalValueAsync(order.OrderId);

                    decimal totalAmount = totalAmountResult.IsSuccess
                        ? totalAmountResult.Value
                        : 0m;

                    SalesOrderListDto dto = order.ToSalesOrderListDto(customerName, locationName, totalAmount);
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

                    string customerName = _orderCacheReadService.GetCustomerNameInCache(order.CustomerId ?? 0);
                    string locationName = _orderCacheReadService.GetLocationNameInCache(order.LocationId);
                    DatabaseResult<decimal> totalAmountResult =
                        await _orderItemManager.GetOrderTotalValueAsync(order.OrderId);

                    decimal totalAmount = totalAmountResult.IsSuccess
                        ? totalAmountResult.Value
                        : 0m;

                    SalesOrderListDto dto = order.ToSalesOrderListDto(customerName, locationName, totalAmount);

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

        public override void Prepare( OrderListViewModelParameter parameter )
        {
            _filter = parameter ?? throw new ArgumentNullException(nameof(parameter));

            _logger.LogInformation(
                "📦 Preparing SalesOrderListViewModel with filter: {FilterType}, EntityId: {EntityId}",
                _filter.FilterType,
                _filter.EntityId);
        }

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

        #region Data Loading and Filtering

        private void LoadAndFilterSalesOrdersAsync()
        {
            switch (_filter.FilterType)
            {
                case OrderFilterType.Customer:
                    FilterByCustomer(_filter.EntityId);
                    SetCustomerFilterInfo(_filter.EntityId);
                    break;

                case OrderFilterType.CreatedBy:
                    FilterByUser(_filter.EntityId);
                    SetUserFilterInfo(_filter.EntityId);
                    break;

                case OrderFilterType.Location:
                    FilterByLocation(_filter.EntityId);
                    SetLocationFilterInfo(_filter.EntityId);
                    break;
                default:
                    _logger.LogInformation("Unspecified filter type: {FilterType}, will display all sales orders", _filter.FilterType);
                    LoadSalesOrdersAsync();
                    break;
            }
        }


        private void FilterByCustomer( int customerId )
        {
            List<SalesOrderListDto> result = _orderCacheReadService
                                             .GetSalesOrderListByCustomerInCache(customerId)
                                             .ToList();

            if (result.Count == 0)
            {
                _logger.LogInformation("No sales orders found for customer ID {CustomerId}", customerId);
                SalesOrders = [];
                _allSalesOrders.Clear();
                return;
            }

            _allSalesOrders = result
                              .Select(dto => new SalesOrderListItemViewModel(dto))
                              .ToList();

            ApplyFilter();
        }

        private void FilterByUser( int filterEntityId )
        {
            List<SalesOrderListDto> result = _orderCacheReadService
                                             .GetSalesOrderListByUserInCache(filterEntityId)
                                             .ToList();

            if (result.Count == 0)
            {
                _logger.LogInformation("No sales orders found for user ID {UserId}", filterEntityId);
                SalesOrders = [];
                _allSalesOrders.Clear();
                return;
            }

            _allSalesOrders = result
                              .Select(dto => new SalesOrderListItemViewModel(dto))
                              .ToList();

            ApplyFilter();
        }

        private void FilterByLocation( int filterEntityId )
        {
            List<SalesOrderListDto> result = _orderCacheReadService
                                             .GetSalesOrderListByLocationInCache(filterEntityId)
                                             .ToList();

            if (result.Count == 0)
            {
                _logger.LogInformation("No sales orders found for location ID {LocationId}", filterEntityId);
                SalesOrders = [];
                _allSalesOrders.Clear();
                return;
            }

            _allSalesOrders = result
                              .Select(dto => new SalesOrderListItemViewModel(dto))
                              .ToList();

            ApplyFilter();
        }

        private async Task LoadSalesOrdersAsync()
        {
            try
            {
                DatabaseResult<IEnumerable<SalesOrderListDto>> salesOrdersResult =
                    await _orderService.GetSalesOrderListAsync();

                if (!salesOrdersResult.IsSuccess || salesOrdersResult.Value == null)
                {
                    _logger.LogError("Failed to load sales orders: {ErrorMessage}", salesOrdersResult.ErrorMessage);
                    SalesOrders = [];
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

        private void SetCustomerFilterInfo( int customerId )
        {
            CustomerDto? customer = _customerCacheReadService.GetCustomerByIdInCache(customerId);
            if (customer != null)
            {
                FilterTitle = $"Orders for {customer.Name}";
                FilterSubtitle = "Showing all products for this customer";
            }
            else
            {
                FilterTitle = "Orders for Customer";
                FilterSubtitle = $"Customer ID: {customer}";
            }
        }

        private void SetUserFilterInfo( int userId )
        {
            UserDto? user = _userCacheReadService.GetByIdFromCache(userId);
            if (user != null)
            {
                FilterTitle = $"Orders created by {user.FullName}";
                FilterSubtitle = "Showing all orders created by this user";
            }
            else
            {
                FilterTitle = "Orders by User";
                FilterSubtitle = $"User ID: {userId}";
            }
        }

        private void SetLocationFilterInfo( int locationId )
        {
            LocationDto? location = _locationCacheReadService.GetLocationByIdInCache(locationId);
            if (location != null)
            {
                FilterTitle = $"Orders in {location.Name}";
                FilterSubtitle = "Showing all orders in this location";
            }
            else
            {
                FilterTitle = "Products in Location";
                FilterSubtitle = $"Location ID: {locationId}";
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

        /// <summary>
        /// Title describing the filter (e.g., "John Doe's Orders")
        /// </summary>
        public string FilterTitle
        {
            get => _filterTitle;
            private set => SetProperty(ref _filterTitle, value);
        }

        /// <summary>
        /// Subtitle with additional filter information
        /// </summary>
        public string FilterSubtitle
        {
            get => _filterSubtitle;
            private set => SetProperty(ref _filterSubtitle, value);
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
        public IMvxCommand<int> OpenSalesOrderDetailsCommand { get; }
        public IMvxCommand<int> OpenSalesOrderDeleteCommand { get; }
        public IMvxAsyncCommand RefreshCommand { get; }

        private void ExecuteOpenSalesOrderForm( int orderId )
        {
            _modalNavigationControl.PopUp<SalesOrderFormViewModel>(orderId);
        }

        private void ExecuteOpenSalesOrderDetails( int salesOrderId ) => _modalNavigationControl.PopUp<SalesOrderDetailsViewModel>(salesOrderId);


        private void ExecuteOpenSalesOrderDelete( int orderId )
        {
            _modalNavigationControl.PopUp<SalesOrderDeleteViewModel>(orderId);
        }

        #endregion
    }
}
