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

namespace Storix.Core.ViewModels.Orders.Sales
{
    /// <summary>
    /// ViewModel for Sales Order details
    /// </summary>
    public class SalesOrderDetailsViewModel:OrderDetailsViewModelBase
    {
        public SalesOrderDetailsViewModel(
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
            ILogger<SalesOrderDetailsViewModel> logger )
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

        public override string OrderTypeDisplay => "Sales";
        public override string OrderEntityName => "Customer";
        public override bool IsPurchaseOrder => false;
    }
}
