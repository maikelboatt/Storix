using System;
using System.Collections.Generic;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Orders
{
    public interface IOrderStore
    {
        void Initialize( IEnumerable<Order> orders );

        void InitializeSalesOrderList( List<SalesOrderListDto> salesOrderListDtos );

        void InitializePurchaseOrderList( List<PurchaseOrderListDto> purchaseOrderListDtos );

        string GetCustomerName( int customerId );

        string GetSupplierName( int supplierId );

        void Clear();

        /// <summary>
        ///     Event triggered when an order is added.
        /// </summary>
        event Action<Order> OrderAdded;

        /// <summary>
        ///     Event triggered when an order is updated.
        /// </summary>
        event Action<Order> OrderUpdated;

        /// <summary>
        ///     Event triggered when an order is deleted.
        /// </summary>
        event Action<int> OrderDeleted;

        OrderDto? Create( int orderId, CreateOrderDto orderDto );

        OrderDto? Update( UpdateOrderDto orderDto );

        bool Delete( int orderId );

        bool UpdateStatus( int orderId, OrderStatus newStatus );

        OrderDto? GetById( int orderId );

        List<OrderDto> GetAll( OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int skip = 0,
            int take = 100 );

        List<OrderDto> GetByType( OrderType type );

        List<OrderDto> GetByStatus( OrderStatus status );

        List<OrderDto> GetBySupplier( int supplierId );

        List<OrderDto> GetByCustomer( int customerId );

        List<OrderDto> GetOverdueOrders();

        List<OrderDto> GetByCreatedBy( int createdBy );

        List<OrderDto> GetDraftOrders();

        List<OrderDto> GetActiveOrders();

        List<OrderDto> GetFulfilledOrders();

        List<OrderDto> GetCompletedOrders();

        List<OrderDto> GetCancelledOrders();

        bool Exists( int orderId );

        bool SupplierHasOrders( int supplierId, bool activeOnly = false );

        bool CustomerHasOrders( int customerId, bool activeOnly = false );

        int GetCount( OrderType? type = null, OrderStatus? status = null );

        int GetTotalCount();

        int GetCountByType( OrderType type );

        int GetCountByStatus( OrderStatus status );

        IEnumerable<Order> SearchOrders( OrderType? type = null, OrderStatus? status = null );
    }
}
