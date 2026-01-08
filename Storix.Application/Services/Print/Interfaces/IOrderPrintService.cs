using System.Collections.Generic;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;

namespace Storix.Application.Services.Print.Interfaces
{
    public interface IOrderPrintService
    {
        void PrintOrderDetails( OrderDto order,
            string entityName,
            string locationName,
            string createdByName,
            List<OrderItemSummary> orderItems,
            decimal totalAmount );

        void PrintOrderReceipt( OrderDto order,
            string entityName,
            string locationName,
            List<OrderItemSummary> orderItems,
            decimal subtotal,
            decimal tax,
            decimal total );
    }
}
