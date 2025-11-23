using Microsoft.Extensions.Logging;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Orders;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;
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
            IOrderItemManager orderItemManager,
            IProductCacheReadService productCacheReadService,
            ICustomerCacheReadService customerCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<SalesOrderFormViewModel> logger )
            :base(
                orderService,
                orderCoordinatorService,
                orderItemService,
                orderItemManager,
                productCacheReadService,
                modalNavigationControl,
                logger)
        {
            _orderCoordinatorService = orderCoordinatorService;
            _orderItemService = orderItemService;
            _customerCacheReadService = customerCacheReadService;
        }

        private int _saleOrderId;

        public override void Prepare( int parameter )
        {
            _saleOrderId = parameter;
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

                _logger.LogInformation("Loaded {Count} customers for sales order", Input.Customers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
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
