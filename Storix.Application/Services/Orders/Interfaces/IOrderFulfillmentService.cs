using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderFulfillmentService
    {
        /// <summary>
        /// Fulfills a purchase order - increases inventory
        /// </summary>
        Task<DatabaseResult> FulfillPurchaseOrderAsync(
            int orderId,
            int receivingLocationId,
            int userId );

        /// <summary>
        /// Fulfills a sales order - decreases inventory
        /// </summary>
        Task<DatabaseResult> FulfillSalesOrderAsync(
            int orderId,
            int shippingLocationId,
            int userId );
    }
}
