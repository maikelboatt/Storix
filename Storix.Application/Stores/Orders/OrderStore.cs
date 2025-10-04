using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Orders
{
    public class OrderStore:IOrderStore
    {
        private readonly Dictionary<int, Order> _orders;

        public OrderStore( List<Order>? initialOrders = null )
        {
            _orders = new Dictionary<int, Order>();

            if (initialOrders == null) return;

            foreach (Order order in initialOrders)
            {
                _orders[order.OrderId] = order;
            }
        }

        public void Initialize( IEnumerable<Order> orders )
        {
            _orders.Clear();
            foreach (Order order in orders)
            {
                _orders[order.OrderId] = order;
            }
        }

        public void Clear() => _orders.Clear();

        public OrderDto? Create( int orderId, OrderDto orderDto )
        {
            switch (orderDto)
            {
                case { Type: OrderType.Purchase, SupplierId: null }:
                case { Type: OrderType.Sale, CustomerId: null }:
                    return null;
            }

            if (orderDto.CreatedBy <= 0)
                return null;

            Order order = new(
                orderId,
                orderDto.Type,
                orderDto.Status,
                orderDto.SupplierId,
                orderDto.CustomerId,
                orderDto.OrderDate,
                orderDto.DeliveryDate,
                orderDto.Notes,
                orderDto.CreatedBy
            );

            _orders[orderId] = order;
            return order.ToDto();
        }

        public OrderDto? GetById( int orderId ) => !_orders.TryGetValue(orderId, out Order? order)
            ? null
            : order.ToDto();

        public OrderDto? Update( OrderDto orderDto )
        {
            if (!_orders.TryGetValue(orderDto.OrderId, out Order? existingOrder)) return null;
            Order updatedOrder = existingOrder with
            {
                Status = orderDto.Status,
                DeliveryDate = orderDto.DeliveryDate,
                Notes = orderDto.Notes
            };

            _orders[orderDto.OrderId] = updatedOrder;
            return updatedOrder.ToDto();
        }

        public bool UpdateStatus( int orderId, OrderStatus newStatus )
        {
            if (!_orders.TryGetValue(orderId, out Order? existingOrder)) return false;

            Order updatedOrder = existingOrder with
            {
                Status = newStatus
            };

            _orders[orderId] = updatedOrder;
            return true;
        }

        public bool Delete( int orderId ) => _orders.Remove(orderId);

        public List<OrderDto> GetAll( OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int skip = 0,
            int take = 100 )
        {
            IEnumerable<Order> orders = _orders.Values.AsEnumerable();

            if (type.HasValue)
                orders = orders.Where(o => o.Type == type.Value);

            if (status.HasValue)
                orders = orders.Where(o => o.Status == status.Value);

            if (supplierId.HasValue)
                orders = orders.Where(o => o.SupplierId == supplierId.Value);

            if (customerId.HasValue)
                orders = orders.Where(o => o.CustomerId == customerId.Value);

            return orders
                   .OrderByDescending(o => o.OrderDate)
                   .Skip(skip)
                   .Take(take)
                   .Select(o => o.ToDto())
                   .ToList();
        }

        public List<OrderDto> GetByType( OrderType type ) => _orders
                                                             .Values
                                                             .Where(o => o.Type == type)
                                                             .OrderByDescending(o => o.OrderDate)
                                                             .Select(o => o.ToDto())
                                                             .ToList();

        public List<OrderDto> GetByStatus( OrderStatus status ) => _orders
                                                                   .Values
                                                                   .Where(o => o.Status == status)
                                                                   .OrderByDescending(o => o.OrderDate)
                                                                   .Select(o => o.ToDto())
                                                                   .ToList();

        public List<OrderDto> GetBySupplier( int supplierId ) => _orders
                                                                 .Values
                                                                 .Where(o => o.SupplierId == supplierId)
                                                                 .OrderByDescending(o => o.OrderDate)
                                                                 .Select(o => o.ToDto())
                                                                 .ToList();

        public List<OrderDto> GetByCustomer( int customerId ) => _orders
                                                                 .Values
                                                                 .Where(o => o.CustomerId == customerId)
                                                                 .OrderByDescending(o => o.OrderDate)
                                                                 .Select(o => o.ToDto())
                                                                 .ToList();

        public List<OrderDto> GetOverdueOrders() => _orders
                                                    .Values
                                                    .Where(o => o.IsOverdue && o.Status != OrderStatus.Draft && o.Status != OrderStatus.Active)
                                                    .OrderBy(o => o.DeliveryDate)
                                                    .Select(o => o.ToDto())
                                                    .ToList();

        public List<OrderDto> GetByCreatedBy( int createdBy ) => _orders
                                                                 .Values
                                                                 .Where(o => o.CreatedBy == createdBy)
                                                                 .OrderByDescending(o => o.OrderDate)
                                                                 .Select(o => o.ToDto())
                                                                 .ToList();

        public List<OrderDto> GetDraftOrders() => _orders
                                                  .Values
                                                  .Where(o => o.Status == OrderStatus.Draft)
                                                  .OrderByDescending(o => o.OrderDate)
                                                  .Select(o => o.ToDto())
                                                  .ToList();

        public List<OrderDto> GetActiveOrders() => _orders
                                                   .Values
                                                   .Where(o => o.Status == OrderStatus.Active)
                                                   .OrderByDescending(o => o.OrderDate)
                                                   .Select(o => o.ToDto())
                                                   .ToList();

        public List<OrderDto> GetCompletedOrders() => _orders
                                                      .Values
                                                      .Where(o => o.Status == OrderStatus.Completed)
                                                      .OrderByDescending(o => o.OrderDate)
                                                      .Select(o => o.ToDto())
                                                      .ToList();

        public List<OrderDto> GetCancelledOrders() => _orders
                                                      .Values
                                                      .Where(o => o.Status == OrderStatus.Cancelled)
                                                      .OrderByDescending(o => o.OrderDate)
                                                      .Select(o => o.ToDto())
                                                      .ToList();

        public bool Exists( int orderId ) => _orders.ContainsKey(orderId);

        public bool SupplierHasOrders( int supplierId, bool activeOnly = false ) => _orders
                                                                                    .Values
                                                                                    .Any(o => o.SupplierId == supplierId &&
                                                                                              (!activeOnly || o.Status is OrderStatus.Draft
                                                                                                  or OrderStatus.Active));

        public bool CustomerHasOrders( int customerId, bool activeOnly = false ) => _orders
                                                                                    .Values
                                                                                    .Any(o => o.CustomerId == customerId &&
                                                                                              (!activeOnly || o.Status is OrderStatus.Draft
                                                                                                  or OrderStatus.Active));

        public int GetCount( OrderType? type = null, OrderStatus? status = null )
        {
            IEnumerable<Order> orders = _orders.Values.AsEnumerable();

            if (type.HasValue)
                orders = orders.Where(o => o.Type == type.Value);

            if (status.HasValue)
                orders = orders.Where(o => o.Status == status.Value);

            return orders.Count();
        }

        public int GetTotalCount() => _orders.Count;

        public int GetCountByType( OrderType type ) => _orders.Values.Count(o => o.Type == type);

        public int GetCountByStatus( OrderStatus status ) => _orders.Values.Count(o => o.Status == status);

        public IEnumerable<Order> SearchOrders( OrderType? type = null, OrderStatus? status = null )
        {
            IEnumerable<Order> query = _orders.Values.AsEnumerable();

            if (type.HasValue)
                query = query.Where(o => o.Type == type.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            return query
                   .OrderByDescending(o => o.OrderDate)
                   .ToList();
        }
    }
}
