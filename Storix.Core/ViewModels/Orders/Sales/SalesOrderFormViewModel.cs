using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Locations;
using Storix.Application.DTO.Orders;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.Dialog;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;
using Storix.Core.Helper;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Sales
{
    /// <summary>
    /// ViewModel for creating and editing Sales Orders using InputModel pattern
    /// </summary>
    public class SalesOrderFormViewModel:OrderFormViewModelBase
    {
        private readonly IOrderCoordinatorService _orderCoordinatorService;
        private readonly IOrderItemService _orderItemService;
        private readonly ILocationCacheReadService _locationCacheReadService;
        private readonly ICustomerCacheReadService _customerCacheReadService;

        protected override OrderType OrderType => OrderType.Sale;

        public override string Title => IsEditMode
            ? "Edit Sales Order"
            : "Create Sales Order";

        public override string SaveButtonText => IsEditMode
            ? "Update"
            : "Create Order";

        public override string FormIcon => "🛒";

        public SalesOrderFormViewModel(
            IOrderService orderService,
            IOrderCoordinatorService orderCoordinatorService,
            IOrderItemService orderItemService,
            IOrderFulfillmentService orderFulfillmentService,
            IDialogService dialogService,
            IOrderItemManager orderItemManager,
            IInventoryCacheReadService inventoryCacheReadService,
            IProductCacheReadService productCacheReadService,
            ILocationCacheReadService locationCacheReadService,
            ICustomerCacheReadService customerCacheReadService,
            IOrderFulfillmentHelper orderFulfillmentHelper,
            IModalNavigationControl modalNavigationControl,
            ILogger<SalesOrderFormViewModel> logger )
            :base(
                orderService,
                orderCoordinatorService,
                orderItemService,
                orderFulfillmentService,
                dialogService,
                orderItemManager,
                inventoryCacheReadService,
                productCacheReadService,
                locationCacheReadService,
                orderFulfillmentHelper,
                modalNavigationControl,
                logger)
        {
            _orderCoordinatorService = orderCoordinatorService;
            _orderItemService = orderItemService;
            _locationCacheReadService = locationCacheReadService;
            _customerCacheReadService = customerCacheReadService;
        }

        protected override async Task LoadEntitySpecificDataAsync()
        {
            try
            {
                List<CustomerDto> customers = _customerCacheReadService.GetAllActiveCustomersInCache();
                Input.Customers.Clear();

                foreach (CustomerDto customer in customers)
                {
                    Input.Customers.Add(customer);
                }

                List<LocationDto> locations = _locationCacheReadService.GetAllActiveLocationsInCache();
                Input.Locations.Clear();

                foreach (LocationDto location in locations)
                {
                    Input.Locations.Add(location);
                }

                _logger.LogInformation("Loaded {Count} and {LocationCount} customers for sales order  ", Input.Customers.Count, Input.Locations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers and Locations");
            }
        }

        protected override void InitializeForCreate()
        {
            // Set order type
            Input.Type = OrderType;

            // Generate order number
            OrderNumber = GenerateOrderNumber();

            // Set created by (should come from auth service)
            Input.CreatedBy = CurrentUserId;

            // Set order date to today
            Input.OrderDate = DateTime.Now;

            // Ensure supplier is null for sales orders
            Input.SupplierId = null;
        }

        protected override void LoadEntitySpecificDataForEdit( OrderDto orderDto )
        {
            // Load customer info
            if (orderDto.CustomerId.HasValue)
            {
                Input.CustomerId = orderDto.CustomerId;

                CustomerDto? customer = Input.Customers.FirstOrDefault(c => c.CustomerId == orderDto.CustomerId);
                if (customer != null)
                {
                    _logger.LogInformation("Loaded customer {CustomerName} for order", customer.Name);
                }
            }

            // Ensure this is treated as a sales order
            Input.Type = OrderType.Sale;
            Input.SupplierId = null;
        }

        protected override async Task PopulateInputCollectionsAsync()
        {
            // Populate locations
            List<LocationDto> locations = _locationCacheReadService.GetAllActiveLocationsInCache();
            Input.Locations = new ObservableCollection<LocationDto>(locations);

            // Populate customers
            List<CustomerDto> customers = _customerCacheReadService.GetAllActiveCustomersInCache();
            Input.Customers = new ObservableCollection<CustomerDto>(customers);

            _logger.LogInformation(
                "Populated Input collections: {LocationCount} locations, {CustomerCount} customers",
                Input.Locations.Count,
                Input.Customers.Count);

            await Task.CompletedTask;
        }

        protected override string GenerateOrderNumber()
        {
            // Generate format: SO-YYYYMMDD-XXXX
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int random = new Random().Next(1000, 9999);
            return $"SO-{datePart}-{random}";
        }

        protected override void ResetForm()
        {
            base.ResetForm();
            OrderNumber = GenerateOrderNumber();
        }
    }
}
