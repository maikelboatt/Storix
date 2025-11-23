using Microsoft.Extensions.Logging;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Core.Control;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Purchase
{
    /// <summary>
    /// ViewModel for creating and editing Purchase Orders using InputModel pattern
    /// </summary>
    public class PurchaseOrderFormViewModel:OrderFormViewModelBase
    {
        private readonly IOrderCoordinatorService _orderCoordinatorService;
        private readonly IOrderItemService _orderItemService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;

        protected override OrderType OrderType => OrderType.Purchase;

        public override string Title => IsEditMode
            ? "Edit Purchase Order"
            : "Create Purchase Order";

        public override string SaveButtonText => IsEditMode
            ? "Update"
            : "Create Order";

        public override string FormIcon => "📦";

        public PurchaseOrderFormViewModel(
            IOrderService orderService,
            IOrderCoordinatorService orderCoordinatorService,
            IOrderItemService orderItemService,
            IOrderItemManager orderItemManager,
            IProductCacheReadService productCacheReadService,
            ISupplierCacheReadService supplierCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<PurchaseOrderFormViewModel> logger )
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
            _supplierCacheReadService = supplierCacheReadService;
        }

        protected override async Task LoadEntitySpecificDataAsync()
        {
            try
            {
                IEnumerable<SupplierDto> suppliers = _supplierCacheReadService.GetAllActiveSuppliersInCache();
                Input.Suppliers.Clear();

                foreach (SupplierDto supplier in suppliers)
                {
                    Input.Suppliers.Add(supplier);
                }

                _logger.LogInformation("Loaded {Count} suppliers for purchase order", Input.Suppliers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers");
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

            // Ensure customer is null for purchase orders
            Input.CustomerId = null;
        }

        protected override void LoadEntitySpecificDataForEdit( OrderDto orderDto )
        {
            // Load supplier info
            if (orderDto.SupplierId.HasValue)
            {
                Input.SupplierId = orderDto.SupplierId;

                SupplierDto? supplier = Input.Suppliers.FirstOrDefault(s => s.SupplierId == orderDto.SupplierId);
                if (supplier != null)
                {
                    _logger.LogInformation("Loaded supplier {SupplierName} for order", supplier.Name);
                }
            }

            // Ensure this is treated as a purchase order
            Input.Type = OrderType.Purchase;
            Input.CustomerId = null;
        }

        protected override string GenerateOrderNumber()
        {
            // Generate format: PO-YYYYMMDD-XXXX
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int random = new Random().Next(1000, 9999);
            return $"PO-{datePart}-{random}";
        }

        protected override void ResetForm()
        {
            base.ResetForm();
            OrderNumber = GenerateOrderNumber();
        }
    }
}
