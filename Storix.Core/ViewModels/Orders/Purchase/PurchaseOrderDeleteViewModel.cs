using Microsoft.Extensions.Logging;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Application.Stores.Suppliers;
using Storix.Application.Stores.Users;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Orders.Purchase
{
    public class PurchaseOrderDeleteViewModel:OrderDeleteViewModelBase
    {
        public PurchaseOrderDeleteViewModel( IOrderService orderService,
            IOrderCacheReadService orderCacheReadService,
            ISupplierStore supplierStore,
            ICustomerStore customerStore,
            IUserStore userStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<PurchaseOrderDeleteViewModel> logger ):base(
            orderService,
            orderCacheReadService,
            supplierStore,
            customerStore,
            userStore,
            modalNavigationControl,
            logger)
        {
        }

        public override string OrderTypeDisplay => "Purchase";
        public override string OrderEntityName => "Supplier";
    }
}
