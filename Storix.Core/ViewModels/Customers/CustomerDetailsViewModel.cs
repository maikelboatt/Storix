using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.Print;
using Storix.Core.Control;
using Storix.Core.ViewModels.Orders.Sales;

namespace Storix.Core.ViewModels.Customers
{
    /// <summary>
    /// ViewModel for displaying comprehensive customer details.
    /// Shows customer information, statistics, and order history.
    /// </summary>
    public class CustomerDetailsViewModel:MvxViewModel<int>
    {
        private readonly ICustomerCacheReadService _customerCacheReadService;
        private readonly IPrintService _printService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CustomerDetailsViewModel> _logger;

        private CustomerDto? _customer;
        private bool _isLoading;
        private int _customerId;
        private ObservableCollection<OrderSummary> _recentOrders;

        #region Constructor

        public CustomerDetailsViewModel(
            ICustomerCacheReadService customerCacheReadService,
            IPrintService printService,
            IModalNavigationControl modalNavigationControl,
            ILogger<CustomerDetailsViewModel> logger )
        {
            _customerCacheReadService = customerCacheReadService ?? throw new ArgumentNullException(nameof(customerCacheReadService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _recentOrders = [];

            // Initialize commands
            CloseCommand = new MvxCommand(ExecuteCloseCommand);
            EditCustomerCommand = new MvxCommand(ExecuteEditCustomerCommand, () => CanEditCustomer);
            PrintDetailsCommand = new MvxCommand(ExecutePrintDetailsCommand, () => CanPrintDetails);
            ViewAllOrdersCommand = new MvxCommand(ExecuteViewAllOrdersCommand, () => CanViewAllOrders);
            SendEmailCommand = new MvxCommand(ExecuteSendEmailCommand, () => CanSendEmail);
            CallPhoneCommand = new MvxCommand(ExecuteCallPhoneCommand, () => CanCallPhone);
            CreateNewOrderCommand = new MvxCommand(ExecuteCreateNewOrderCommand, () => CanCreateNewOrder);
        }

        #endregion

        #region Commands

        public IMvxCommand CloseCommand { get; }
        public IMvxCommand EditCustomerCommand { get; }
        public IMvxCommand PrintDetailsCommand { get; }
        public IMvxCommand ViewAllOrdersCommand { get; }
        public IMvxCommand SendEmailCommand { get; }
        public IMvxCommand CallPhoneCommand { get; }
        public IMvxCommand CreateNewOrderCommand { get; }

        #endregion

        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _customerId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadCustomerDetailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load customer details for ID: {CustomerId}", _customerId);
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
        /// The customer being displayed
        /// </summary>
        public CustomerDto? Customer
        {
            get => _customer;
            private set => SetProperty(
                ref _customer,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanEditCustomer);
                    RaisePropertyChanged(() => CanPrintDetails);
                    RaisePropertyChanged(() => CanSendEmail);
                    RaisePropertyChanged(() => CanCallPhone);
                    RaisePropertyChanged(() => CanCreateNewOrder);
                    EditCustomerCommand.RaiseCanExecuteChanged();
                    PrintDetailsCommand.RaiseCanExecuteChanged();
                    SendEmailCommand.RaiseCanExecuteChanged();
                    CallPhoneCommand.RaiseCanExecuteChanged();
                    CreateNewOrderCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether data is being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Statistics Properties

        private int _totalOrders;
        private int _activeOrders;
        private decimal _totalRevenue;
        private decimal _averageOrderValue;
        private DateTime? _lastOrderDate;
        private DateTime? _firstOrderDate;

        /// <summary>
        /// Total number of orders from this customer
        /// </summary>
        public int TotalOrders
        {
            get => _totalOrders;
            private set => SetProperty(
                ref _totalOrders,
                value,
                () =>
                {
                    RaisePropertyChanged(() => HasOrders);
                    RaisePropertyChanged(() => CanViewAllOrders);
                    ViewAllOrdersCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Number of active/pending orders
        /// </summary>
        public int ActiveOrders
        {
            get => _activeOrders;
            private set => SetProperty(ref _activeOrders, value);
        }

        /// <summary>
        /// Total revenue from this customer
        /// </summary>
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            private set => SetProperty(ref _totalRevenue, value);
        }

        /// <summary>
        /// Average value per order
        /// </summary>
        public decimal AverageOrderValue
        {
            get => _averageOrderValue;
            private set => SetProperty(ref _averageOrderValue, value);
        }

        /// <summary>
        /// Date of last order
        /// </summary>
        public DateTime? LastOrderDate
        {
            get => _lastOrderDate;
            private set => SetProperty(ref _lastOrderDate, value, () => { RaisePropertyChanged(() => LastOrderDateDisplay); });
        }

        /// <summary>
        /// Date of first order
        /// </summary>
        public DateTime? FirstOrderDate
        {
            get => _firstOrderDate;
            private set => SetProperty(ref _firstOrderDate, value, () => { RaisePropertyChanged(() => CustomerSinceDisplay); });
        }

        /// <summary>
        /// Formatted display of last order date
        /// </summary>
        public string LastOrderDateDisplay => LastOrderDate?.ToString("MMM dd, yyyy") ?? "No orders yet";

        /// <summary>
        /// Formatted display of customer since date
        /// </summary>
        public string CustomerSinceDisplay => FirstOrderDate?.ToString("MMM yyyy") ?? "New Customer";

        /// <summary>
        /// Whether customer has orders
        /// </summary>
        public bool HasOrders => TotalOrders > 0;

        #endregion

        #region Collection Properties

        /// <summary>
        /// Recent orders from this customer (limited to 5 for preview)
        /// </summary>
        public ObservableCollection<OrderSummary> RecentOrders
        {
            get => _recentOrders;
            private set => SetProperty(ref _recentOrders, value);
        }

        #endregion

        #region Command Can Execute Properties

        public bool CanEditCustomer => Customer != null && !IsLoading;
        public bool CanPrintDetails => Customer != null && !IsLoading;
        public bool CanViewAllOrders => HasOrders && !IsLoading;
        public bool CanSendEmail => Customer != null && !string.IsNullOrWhiteSpace(Customer.Email) && !IsLoading;
        public bool CanCallPhone => Customer != null && !string.IsNullOrWhiteSpace(Customer.Phone) && !IsLoading;
        public bool CanCreateNewOrder => Customer != null && !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads complete customer details including statistics and related data
        /// </summary>
        private async Task LoadCustomerDetailsAsync()
        {
            _logger.LogInformation("👤 Loading customer details for ID: {CustomerId}", _customerId);

            // Load customer from cache
            CustomerDto? customer = _customerCacheReadService.GetCustomerByIdInCache(_customerId);

            if (customer == null)
            {
                _logger.LogWarning("⚠️ Customer with ID {CustomerId} not found in cache", _customerId);
                return;
            }

            Customer = customer;

            // Load orders and statistics
            LoadOrdersAndStatistics();

            _logger.LogInformation(
                "✅ Loaded customer details: {CustomerName} (ID: {CustomerId})",
                Customer.Name,
                Customer.CustomerId);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads orders and calculates statistics for this customer
        /// </summary>
        private void LoadOrdersAndStatistics()
        {
            if (Customer == null) return;

            try
            {
                // TODO: This would typically query from order service
                // For now, using placeholder data

                // Placeholder statistics - should come from actual order data
                TotalOrders = 0;
                ActiveOrders = 0;
                TotalRevenue = 0;
                AverageOrderValue = 0;
                LastOrderDate = null;
                FirstOrderDate = null;

                List<OrderSummary> orderSummaries = new();

                // TODO: Load actual orders from order service
                // Example:
                // var orders = _orderCacheReadService.GetOrdersByCustomerIdInCache(_customerId);
                // foreach (var order in orders.Take(5))
                // {
                //     orderSummaries.Add(new OrderSummary
                //     {
                //         OrderId = order.OrderId,
                //         OrderNumber = order.OrderNumber,
                //         OrderDate = order.OrderDate,
                //         TotalAmount = order.TotalAmount,
                //         Status = order.Status
                //     });
                // }

                RecentOrders = new ObservableCollection<OrderSummary>(orderSummaries);

                _logger.LogDebug(
                    "Loaded {Count} orders with total revenue ${Revenue:N2}",
                    TotalOrders,
                    TotalRevenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load orders for customer {CustomerId}", _customerId);
                TotalOrders = 0;
                TotalRevenue = 0;
                RecentOrders = new ObservableCollection<OrderSummary>();
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Closes the customer details dialogue
        /// </summary>
        private void ExecuteCloseCommand()
        {
            _logger.LogInformation("Closing customer details view");
            _modalNavigationControl.Close();
        }

        /// <summary>
        /// Opens the customer edit dialogue
        /// </summary>
        private void ExecuteEditCustomerCommand()
        {
            if (Customer == null) return;

            _logger.LogInformation("Opening edit dialog for customer {CustomerId}", Customer.CustomerId);

            _modalNavigationControl.PopUp<CustomerFormViewModel>(_customerId);
        }

        /// <summary>
        /// Prints customer details
        /// </summary>
        private void ExecutePrintDetailsCommand()
        {
            if (Customer == null) return;

            _logger.LogInformation("Printing details for customer {CustomerId}", Customer.CustomerId);

            // _printService.PrintCustomerDetails(
            //     Customer,
            //     RecentOrders.ToList(),
            //     TotalOrders,
            //     ActiveOrders,
            //     TotalRevenue,
            //     AverageOrderValue,
            //     LastOrderDate,
            //     FirstOrderDate
            // );
        }

        /// <summary>
        /// Opens the orders list filtered by this customer
        /// </summary>
        private void ExecuteViewAllOrdersCommand()
        {
            if (Customer == null) return;

            _logger.LogInformation("Viewing all orders for customer {CustomerId}", Customer.CustomerId);

            _modalNavigationControl.PopUp<SalesOrderListViewModel>(parameter: _customerId);
        }

        /// <summary>
        /// Opens default email client with customer email
        /// </summary>
        private void ExecuteSendEmailCommand()
        {
            if (Customer == null || string.IsNullOrWhiteSpace(Customer.Email)) return;

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"mailto:{Customer.Email}",
                        UseShellExecute = true
                    });
                _logger.LogInformation("Opening email client for {Email}", Customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open email client");
            }
        }

        /// <summary>
        /// Opens phone dialer with customer phone number
        /// </summary>
        private void ExecuteCallPhoneCommand()
        {
            if (Customer == null || string.IsNullOrWhiteSpace(Customer.Phone)) return;

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"tel:{Customer.Phone}",
                        UseShellExecute = true
                    });
                _logger.LogInformation("Opening phone dialer for {Phone}", Customer.Phone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open phone dialer");
            }
        }

        /// <summary>
        /// Creates a new order for this customer
        /// </summary>
        private void ExecuteCreateNewOrderCommand()
        {
            if (Customer == null) return;

            _logger.LogInformation("Creating new order for customer {CustomerId}", Customer.CustomerId);

            // TODO: Navigate to create order view with customer pre-selected
            // _modalNavigationControl.PopUp<CreateOrderViewModel>(_customerId);
        }

        #endregion
    }

    #region Helper Classes

    public class OrderSummary
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    #endregion
}
