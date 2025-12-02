using Microsoft.Extensions.Logging;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Application.Stores.Suppliers;
using Storix.Application.Stores.Users;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Orders.Sales
{
    public class SalesOrderDeleteViewModel:OrderDeleteViewModelBase
    {
        public SalesOrderDeleteViewModel( IOrderService orderService,
            IOrderCacheReadService orderCacheReadService,
            ISupplierStore supplierStore,
            ICustomerStore customerStore,
            IUserStore userStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<SalesOrderDeleteViewModel> logger ):base(
            orderService,
            orderCacheReadService,
            supplierStore,
            customerStore,
            userStore,
            modalNavigationControl,
            logger)
        {
        }

        public override string OrderTypeDisplay => "Sales";
        public override string OrderEntityName => "Customer";
    }
}
