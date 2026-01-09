using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Services.Users;

namespace Storix.Application.Services
{
    public class CacheInitializerService(
        IProductService productService,
        ISupplierService supplierService,
        ICategoryService categoryService,
        ICustomerService customerService,
        ILocationService locationService,
        IOrderService orderService,
        IInventoryManager inventoryManager,
        IOrderItemService orderItemService ):ICacheInitializerService
    {
        public async Task InitializeCacheAsync()
        {
            List<Task> tasks =
            [
                productService.GetAllActiveProductsAsync(),
                productService.GetAllActiveProductsForListAsync(),
                categoryService.GetAllActiveCategoriesAsync(),
                categoryService.GetAllActiveCategoriesForListAsync(),
                orderService.GetAllOrdersAsync(),
                orderService.GetSalesOrderListAsync(),
                orderService.GetPurchaseOrderListAsync(),
                supplierService.GetAllActiveSuppliersAsync(),
                customerService.GetAllActiveCustomersAsync(),
                locationService.GetAllActiveLocationsAsync(),
                inventoryManager.GetAllInventoryAsync()
                // userService.GetAllActiveSuppliersAsync()
            ];

            await Task.WhenAll(tasks);
        }
    }
}
