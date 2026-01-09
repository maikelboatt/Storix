using Microsoft.Extensions.Logging;
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

namespace Storix.Core.ViewModels.Orders.Purchase
{
    /// <summary>
    /// ViewModel for Purchase Order details
    /// </summary>
    public class PurchaseOrderDetailsViewModel:OrderDetailsViewModelBase
    {
        public PurchaseOrderDetailsViewModel(
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
            ILogger<PurchaseOrderDetailsViewModel> logger )
            :base(
                orderService,
                orderItemService,
                orderCacheReadService,
                printManager,
                productStore,
                supplierStore,
                customerStore,
                userStore,
                locationStore,
                orderFulfillmentHelper,
                modalNavigationControl,
                logger)
        {
        }

        public override string OrderTypeDisplay => "Purchase";
        public override string OrderEntityName => "Supplier";
        public override bool IsPurchaseOrder => true;
    }
}
