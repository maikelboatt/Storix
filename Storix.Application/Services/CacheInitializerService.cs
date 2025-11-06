using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Customers.Interfaces;
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
        IOrderService orderService,
        IOrderItemService orderItemService ):ICacheInitializerService
    {
        public async Task InitializeCacheAsync()
        {
            List<Task> tasks =
            [
                productService.GetAllActiveProductsAsync(),
                productService.GetAllActiveProductsForListAsync(),
                supplierService.GetAllActiveAsync(),
                categoryService.GetAllActiveCategoriesAsync(),

                customerService.GetAllActiveAsync(),
                orderService.GetAllOrdersAsync()
                // userService.GetAllActiveAsync()
            ];

            await Task.WhenAll(tasks);
        }
    }
}
