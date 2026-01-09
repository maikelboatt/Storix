using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Orders
{
    public class OrderStore:IOrderStore
    {
        private readonly ICustomerCacheReadService _customerCacheReadService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly ILocationCacheReadService _locationCacheReadService;

        // Single dictionary for all orders
        private readonly Dictionary<int, Order> _orders;

        // Cached list DTOs for optimized list views
        private readonly Dictionary<int, SalesOrderListDto> _salesOrderListDtos;
        private readonly Dictionary<int, PurchaseOrderListDto> _purchaseOrderListDtos;

        public OrderStore(
            ICustomerCacheReadService customerCacheReadService,
            ISupplierCacheReadService supplierCacheReadService,
            ILocationCacheReadService locationCacheReadService,
            List<Order>? initialOrders = null )
        {
            _customerCacheReadService = customerCacheReadService;
            _supplierCacheReadService = supplierCacheReadService;
            _locationCacheReadService = locationCacheReadService;
            _orders = new Dictionary<int, Order>();
            _salesOrderListDtos = new Dictionary<int, SalesOrderListDto>();
            _purchaseOrderListDtos = new Dictionary<int, PurchaseOrderListDto>();

            if (initialOrders != null)
            {
                foreach (Order order in initialOrders)
                {
                    _orders[order.OrderId] = order;
                }
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

        public void InitializeSalesOrderList( List<SalesOrderListDto> salesOrderListDtos )
        {
            _salesOrderListDtos.Clear();
            foreach (SalesOrderListDto salesOrder in salesOrderListDtos)
            {
                _salesOrderListDtos[salesOrder.OrderId] = salesOrder;
            }
        }

        public void InitializePurchaseOrderList( List<PurchaseOrderListDto> purchaseOrderListDtos )
        {
            _purchaseOrderListDtos.Clear();
            foreach (PurchaseOrderListDto purchaseOrder in purchaseOrderListDtos)
            {
                _purchaseOrderListDtos[purchaseOrder.OrderId] = purchaseOrder;
            }
        }

        public string GetCustomerName( int customerId )
        {
            CustomerDto? customer = _customerCacheReadService.GetCustomerByIdInCache(customerId);
            return customer?.Name ?? "Unknown";
        }

        public string GetSupplierName( int supplierId )
        {
            SupplierDto? supplier = _supplierCacheReadService.GetSupplierByIdInCache(supplierId);
            return supplier?.Name ?? "Unknown";
        }

        public string GetLocationName( int supplierId ) => _locationCacheReadService.GetLocationByIdInCache(supplierId)
                                                                                    ?.Name ?? "Unknown";

        public void Clear()
        {
            _orders.Clear();
            _salesOrderListDtos.Clear();
            _purchaseOrderListDtos.Clear();
        }

        #region Events

        /// <summary>
        /// Event triggered when an order is added (for all order types)
        /// </summary>
        public event Action<Order>? OrderAdded;

        /// <summary>
        /// Event triggered when an order is updated (for all order types)
        /// </summary>
        public event Action<Order>? OrderUpdated;

        /// <summary>
        /// Event triggered when an order is deleted (for all order types)
        /// </summary>
        public event Action<int>? OrderDeleted;

        #endregion

        #region Write Operations

        private SalesOrderListDto GetOrCreateSalesOrderListDto( Order order )
        {
            if (_salesOrderListDtos.TryGetValue(order.OrderId, out SalesOrderListDto? cachedDto))
            {
                return cachedDto;
            }

            SalesOrderListDto newDto = new()
            {
                OrderId = order.OrderId,
                Status = order.Status,
                CreatedBy = order.CreatedBy,
                DeliveryDate = order.DeliveryDate,
                Notes = order.Notes,
                OrderDate = order.OrderDate,
                CustomerName = GetCustomerName(order.CustomerId ?? 0),
                LocationName = GetLocationName(order.LocationId),
                TotalAmount = 44m // Placeholder, should be calculated based on order items
            };

            _salesOrderListDtos[order.OrderId] = newDto;
            return newDto;
        }

        private PurchaseOrderListDto GetOrCreatePurchaseOrderListDto( Order order )
        {
            if (_purchaseOrderListDtos.TryGetValue(order.OrderId, out PurchaseOrderListDto? cachedDto))
            {
                return cachedDto;
            }

            PurchaseOrderListDto newDto = new()
            {
                OrderId = order.OrderId,
                Status = order.Status,
                CreatedBy = order.CreatedBy,
                DeliveryDate = order.DeliveryDate,
                Notes = order.Notes,
                OrderDate = order.OrderDate,
                SupplierName = GetSupplierName(order.SupplierId ?? 0),
                LocationName = GetLocationName(order.LocationId),
                TotalAmount = 44m // Placeholder, should be calculated based on order items
            };

            _purchaseOrderListDtos[order.OrderId] = newDto;
            return newDto;

        }

        public OrderDto? Create( int orderId, CreateOrderDto orderDto )
        {
            // Validation
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
                OrderStatus.Draft,
                orderDto.SupplierId,
                orderDto.CustomerId,
                orderDto.OrderDate,
                orderDto.DeliveryDate,
                orderDto.Notes,
                orderDto.CreatedBy,
                orderDto.LocationId
            );

            _orders[orderId] = order;


            switch (order.Type)
            {
                case OrderType.Sale:
                {
                    // Create and cache sales order list DTO
                    SalesOrderListDto salesOrderListDto = new()
                    {
                        OrderId = order.OrderId,
                        Status = order.Status,
                        CreatedBy = order.CreatedBy,
                        DeliveryDate = order.DeliveryDate,
                        Notes = order.Notes,
                        OrderDate = order.OrderDate,
                        CustomerName = GetCustomerName(order.CustomerId ?? 0),
                        LocationName = GetLocationName(order.LocationId),
                        TotalAmount = 44m // Placeholder, should be calculated based on order items
                    };
                    _salesOrderListDtos[order.OrderId] = salesOrderListDto;
                    break;
                }
                case OrderType.Purchase:
                {
                    // Create and cache purchase order list DTO
                    PurchaseOrderListDto purchaseOrderListDto = new()
                    {
                        OrderId = order.OrderId,
                        Status = order.Status,
                        CreatedBy = order.CreatedBy,
                        DeliveryDate = order.DeliveryDate,
                        Notes = order.Notes,
                        OrderDate = order.OrderDate,
                        SupplierName = GetSupplierName(order.SupplierId ?? 0),
                        LocationName = GetLocationName(order.LocationId),
                        TotalAmount = 44m // Placeholder, should be calculated based on order items
                    };
                    _purchaseOrderListDtos[order.OrderId] = purchaseOrderListDto;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Trigger event - listeners can handle async operations
            OrderAdded?.Invoke(order);

            return order.ToDto();
        }


        public OrderDto? Update( UpdateOrderDto orderDto )
        {
            if (!_orders.TryGetValue(orderDto.OrderId, out Order? existingOrder))
                return null;

            Order updatedOrder = existingOrder with
            {
                Status = orderDto.Status,
                LocationId = (int)orderDto.LocationId!,
                DeliveryDate = orderDto.DeliveryDate,
                Notes = orderDto.Notes
            };

            _orders[orderDto.OrderId] = updatedOrder;

            switch (updatedOrder.Type)
            {
                // Update cached list DTOs
                case OrderType.Sale when _salesOrderListDtos.ContainsKey(orderDto.OrderId):
                {
                    // Update the cached sales order list DTO
                    SalesOrderListDto existingDto = _salesOrderListDtos[orderDto.OrderId];
                    _salesOrderListDtos[orderDto.OrderId] = existingDto with
                    {
                        Status = updatedOrder.Status,
                        LocationName = GetLocationName(updatedOrder.LocationId),
                        DeliveryDate = updatedOrder.DeliveryDate,
                        Notes = updatedOrder.Notes
                    };
                    break;
                }
                case OrderType.Purchase when _purchaseOrderListDtos.ContainsKey(orderDto.OrderId):
                {
                    // Update the cached purchase order list DTO
                    PurchaseOrderListDto existingDto = _purchaseOrderListDtos[orderDto.OrderId];
                    _purchaseOrderListDtos[orderDto.OrderId] = existingDto with
                    {
                        Status = updatedOrder.Status,
                        LocationName = GetLocationName(updatedOrder.LocationId),
                        DeliveryDate = updatedOrder.DeliveryDate,
                        Notes = updatedOrder.Notes
                    };
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Trigger event
            OrderUpdated?.Invoke(updatedOrder);

            return updatedOrder.ToDto();
        }

        public bool Delete( int orderId )
        {
            if (!_orders.Remove(orderId, out Order? order))
                return false;

            _salesOrderListDtos.Remove(orderId);
            _purchaseOrderListDtos.Remove(orderId);

            // Trigger event
            OrderDeleted?.Invoke(orderId);

            return true;
        }

        public bool UpdateStatus( int orderId, OrderStatus newStatus )
        {
            if (!_orders.TryGetValue(orderId, out Order? existingOrder))
                return false;

            Order updatedOrder = existingOrder with
            {
                Status = newStatus
            };

            _orders[orderId] = updatedOrder;

            switch (updatedOrder.Type)
            {
                // Update cached list DTOs
                case OrderType.Sale when _salesOrderListDtos.ContainsKey(orderId):
                {
                    SalesOrderListDto existingDto = _salesOrderListDtos[orderId];
                    _salesOrderListDtos[orderId] = existingDto with
                    {
                        Status = newStatus
                    };
                    break;
                }
                case OrderType.Purchase when _purchaseOrderListDtos.ContainsKey(orderId):
                {
                    PurchaseOrderListDto existingDto = _purchaseOrderListDtos[orderId];
                    _purchaseOrderListDtos[orderId] = existingDto with
                    {
                        Status = newStatus
                    };
                    break;
                }
            }

            // Trigger event
            OrderUpdated?.Invoke(updatedOrder);

            return true;
        }

        public void UpdateLocation( int orderId, int newLocationId )
        {
            if (!_orders.TryGetValue(orderId, out Order? existingOrder))
                return;

            Order updatedOrder = existingOrder with
            {
                LocationId = newLocationId
            };

            _orders[orderId] = updatedOrder;

            string newLocationName = GetLocationName(newLocationId);

            switch (updatedOrder.Type)
            {
                // Update cached list DTOs
                case OrderType.Sale when _salesOrderListDtos.ContainsKey(orderId):
                {
                    SalesOrderListDto existingDto = _salesOrderListDtos[orderId];
                    _salesOrderListDtos[orderId] = existingDto with
                    {
                        LocationName = newLocationName
                    };
                    break;
                }
                case OrderType.Purchase when _purchaseOrderListDtos.ContainsKey(orderId):
                {
                    PurchaseOrderListDto existingDto = _purchaseOrderListDtos[orderId];
                    _purchaseOrderListDtos[orderId] = existingDto with
                    {
                        LocationName = newLocationName
                    };
                    break;
                }
            }

            // Trigger event
            OrderUpdated?.Invoke(updatedOrder);
        }

        #endregion

        #region Read Operations

        public OrderDto? GetById( int orderId )
        {

            bool result = _orders.TryGetValue(orderId, out Order? order);

            if (!result) return null;
            switch (order?.Type)
            {
                case OrderType.Sale when _salesOrderListDtos.ContainsKey(orderId):
                case OrderType.Purchase when _purchaseOrderListDtos.ContainsKey(orderId):
                    return order.ToDto();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
            // return  !_orders.TryGetValue(orderId, out Order? order)
            //      ? null
            //      : order.ToDto();
        }

        public List<SalesOrderListDto> GetSalesOrderList()
        {
            return _salesOrderListDtos
                   .Values
                   .OrderByDescending(o => o.OrderDate)
                   .ToList();
        }

        public List<PurchaseOrderListDto> GetPurchaseOrderList()
        {
            return _purchaseOrderListDtos
                   .Values
                   .OrderByDescending(o => o.OrderDate)
                   .ToList();
        }

        public List<OrderDto> GetAll(
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int locationId = 0,
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

            if (locationId > 0)
                orders = orders.Where(o => o.LocationId == locationId);

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

        public List<SalesOrderListDto> GetSalesOrderListByCustomer( int customerId ) => _orders
                                                                                        .Values.Where(o => o.Type == OrderType.Sale &&
                                                                                                          o.CustomerId == customerId)
                                                                                        .OrderByDescending(o => o.OrderDate)
                                                                                        .Select(GetOrCreateSalesOrderListDto)
                                                                                        .ToList();


        public List<SalesOrderListDto> GetSalesOrderListByLocation( int locationId ) => _orders
                                                                                        .Values.Where(o =>
                                                                                                          o.Type == OrderType.Sale &&
                                                                                                          o.LocationId == locationId)
                                                                                        .OrderByDescending(o => o.OrderDate)
                                                                                        .Select(GetOrCreateSalesOrderListDto)
                                                                                        .ToList();

        public List<SalesOrderListDto> GetSalesOrderListByUser( int userId ) => _orders
                                                                                .Values.Where(o => o.Type == OrderType.Sale &&
                                                                                                   o.CreatedBy == userId)
                                                                                .OrderByDescending(o => o.OrderDate)
                                                                                .Select(GetOrCreateSalesOrderListDto)
                                                                                .ToList();

        public List<PurchaseOrderListDto> GetPurchaseOrderListByCustomer( int customerId ) => _orders
                                                                                              .Values.Where(o =>
                                                                                                      o.Type == OrderType.Purchase &&
                                                                                                      o.CustomerId == customerId)
                                                                                              .OrderByDescending(o => o.OrderDate)
                                                                                              .Select(GetOrCreatePurchaseOrderListDto)
                                                                                              .ToList();

        public List<PurchaseOrderListDto> GetPurchaseOrderListByLocation( int locationId ) => _orders
                                                                                              .Values.Where(o =>
                                                                                                      o.Type == OrderType.Purchase &&
                                                                                                      o.LocationId == locationId)
                                                                                              .OrderByDescending(o => o.OrderDate)
                                                                                              .Select(GetOrCreatePurchaseOrderListDto)
                                                                                              .ToList();

        public List<PurchaseOrderListDto> GetPurchaseOrderListByUser( int userId ) => _orders
                                                                                      .Values.Where(o =>
                                                                                                        o.Type == OrderType.Purchase &&
                                                                                                        o.CreatedBy == userId)
                                                                                      .OrderByDescending(o => o.OrderDate)
                                                                                      .Select(GetOrCreatePurchaseOrderListDto)
                                                                                      .ToList();

        public List<OrderDto> GetByLocation( int locationId ) => _orders
                                                                 .Values
                                                                 .Where(o => o.LocationId == locationId)
                                                                 .OrderByDescending(o => o.OrderDate)
                                                                 .Select(o => o.ToDto())
                                                                 .ToList();

        public List<OrderDto> GetOrdersByLocation( int locationId ) => GetByLocation(locationId);

        public List<OrderDto> GetPurchaseOrdersByLocation( int locationId ) => _orders
                                                                               .Values.Where(o => o.Type == OrderType.Purchase)
                                                                               .OrderByDescending(o => o.OrderDate)
                                                                               .Select(o => o.ToDto())
                                                                               .ToList();

        public List<OrderDto> GetSalesOrdersByLocation( int locationId ) => _orders
                                                                            .Values.Where(o => o.Type == OrderType.Sale)
                                                                            .OrderByDescending(o => o.OrderDate)
                                                                            .Select(o => o.ToDto())
                                                                            .ToList();

        public List<OrderDto> GetByLocationAndStatus( int locationId, OrderStatus orderStatus ) => _orders
                                                                                                   .Values.Where(o => o.LocationId == locationId &&
                                                                                                           o.Status == orderStatus)
                                                                                                   .OrderByDescending(o => o.OrderDate)
                                                                                                   .Select(o => o.ToDto())
                                                                                                   .ToList();

        public List<OrderDto> GetByLocationAndType( int locationId, OrderType orderType ) => _orders
                                                                                             .Values.Where(o => o.LocationId == locationId &&
                                                                                                     o.Type == orderType)
                                                                                             .OrderByDescending(o => o.OrderDate)
                                                                                             .Select(o => o.ToDto())
                                                                                             .ToList();

        public List<OrderDto> GetOverdueOrders() => _orders
                                                    .Values
                                                    .Where(o => o.IsOverdue &&
                                                                o.Status != OrderStatus.Draft &&
                                                                o.Status != OrderStatus.Active)
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

        public List<OrderDto> GetActiveOrdersByLocation( int locationId ) => _orders
                                                                             .Values.Where(o => o.LocationId == locationId &&
                                                                                                o.Status == OrderStatus.Active)
                                                                             .OrderByDescending(o => o.OrderDate)
                                                                             .Select(o => o.ToDto())
                                                                             .ToList();

        public List<OrderDto> GetFulfilledOrders() => _orders
                                                      .Values
                                                      .Where(o => o.Status == OrderStatus.Fulfilled)
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

        public decimal GetTotalRevenueByLocation( int locationId ) => _orders
                                                                      .Values
                                                                      .Where(o => o.LocationId == locationId &&
                                                                                  o.Type == OrderType.Sale &&
                                                                                  (o.Status == OrderStatus.Fulfilled ||
                                                                                   o.Status == OrderStatus.Completed))
                                                                      .Sum(o => 100m); // Placeholder, should sum actual order totals

        #endregion

        #region Validation & Counts

        public bool Exists( int orderId ) => _orders.ContainsKey(orderId);

        public bool SupplierHasOrders( int supplierId, bool activeOnly = false ) => _orders.Values.Any(o =>
                                                                                                           o.SupplierId == supplierId &&
                                                                                                           (!activeOnly || o.Status is OrderStatus.Draft
                                                                                                               or OrderStatus.Active or OrderStatus.Fulfilled));

        public bool CustomerHasOrders( int customerId, bool activeOnly = false ) => _orders.Values.Any(o =>
                                                                                                           o.CustomerId == customerId &&
                                                                                                           (!activeOnly || o.Status is OrderStatus.Draft
                                                                                                               or OrderStatus.Active or OrderStatus.Fulfilled));

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

        #endregion
    }
}
