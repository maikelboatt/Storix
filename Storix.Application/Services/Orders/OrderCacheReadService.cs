using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    /// Provides high-speed cache reads for order data stored in memory.
    /// This service focuses solely on non-persistent reads from the <see cref="OrderStore"/>.
    /// </summary>
    public class OrderCacheReadService(
        IOrderStore orderStore,
        IOrderReadService orderReadService,
        ILogger<OrderCacheReadService> logger ):IOrderCacheReadService
    {
        /// <summary>
        /// Retrieves an order by its ID from cache.
        /// </summary>
        public OrderDto? GetOrderByIdInCache( int orderId )
        {
            logger.LogInformation("Retrieving order with ID {OrderId} from cache", orderId);
            return orderStore.GetById(orderId);
        }

        /// <summary>
        /// Searches cached orders by optional filters.
        /// </summary>
        public IEnumerable<OrderDto> SearchOrdersInCache( OrderType? type = null, OrderStatus? status = null )
        {
            logger.LogInformation("Searching cached orders by Type={Type}, Status={Status}", type, status);
            return orderStore
                   .SearchOrders(type, status)
                   .Select(o => o.ToDto());
        }

        /// <summary>
        /// Gets all cached orders with optional filtering.
        /// </summary>
        public IEnumerable<OrderDto> GetAllOrdersInCache(
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int skip = 0,
            int take = 100 )
        {
            logger.LogInformation(
                "Getting cached orders (Type={Type}, Status={Status}, Supplier={SupplierId}, Customer={CustomerId})",
                type,
                status,
                supplierId,
                customerId);
            return orderStore.GetAll(
                type,
                status,
                supplierId,
                customerId,
                skip,
                take);
        }

        public IEnumerable<OrderDto> GetOrdersByTypeInCache( OrderType type )
        {
            logger.LogInformation("Retrieving cached orders of Type={Type}", type);
            return orderStore.GetByType(type);
        }

        public IEnumerable<OrderDto> GetOrdersByStatusInCache( OrderStatus status )
        {
            logger.LogInformation("Retrieving cached orders with Status={Status}", status);
            return orderStore.GetByStatus(status);
        }

        public IEnumerable<OrderDto> GetOrdersBySupplierInCache( int supplierId )
        {
            logger.LogInformation("Retrieving cached orders for Supplier={SupplierId}", supplierId);
            return orderStore.GetBySupplier(supplierId);
        }

        public IEnumerable<OrderDto> GetOrdersByCustomerInCache( int customerId )
        {
            logger.LogInformation("Retrieving cached orders for Customer={CustomerId}", customerId);
            return orderStore.GetByCustomer(customerId);
        }

        public IEnumerable<OrderDto> GetOverdueOrdersInCache()
        {
            logger.LogInformation("Retrieving cached overdue orders");
            return orderStore.GetOverdueOrders();
        }

        public IEnumerable<OrderDto> GetOrderByCreatedByInCache( int createdBy )
        {
            logger.LogInformation("Retrieving cached orders mad by User{UserId}", createdBy);
            return orderStore.GetByCreatedBy(createdBy);
        }

        public IEnumerable<OrderDto> GetDraftOrdersInCache() => orderStore.GetDraftOrders();

        public IEnumerable<OrderDto> GetActiveOrdersInCache() => orderStore.GetActiveOrders();

        public IEnumerable<OrderDto> GetCompletedOrdersInCache() => orderStore.GetCompletedOrders();

        public IEnumerable<OrderDto> GetCancelledOrdersInCache() => orderStore.GetCancelledOrders();

        public bool OrderExistsInCache( int orderId ) => orderStore.Exists(orderId);

        public int GetTotalOrderCountInCache() => orderStore.GetTotalCount();

        public int GetOrderCountByTypeInCache( OrderType type ) => orderStore.GetCountByType(type);

        public int GetOrderCountByStatusInCache( OrderStatus status ) => orderStore.GetCountByStatus(status);

        public bool SupplierHasOrdersInCache( int supplierId, bool activeOnly = false ) => orderStore.SupplierHasOrders(supplierId, activeOnly);

        public bool CustomerHasOrdersInCache( int customerId, bool activeOnly = false ) => orderStore.CustomerHasOrders(customerId, activeOnly);

        /// <summary>
        /// Refreshes the order cache from database asynchronously.
        /// </summary>
        public void RefreshStoreCache()
        {
            logger.LogInformation("Refreshing order store cache");
            _ = Task.Run(async () =>
            {
                try
                {
                    DatabaseResult<IEnumerable<OrderDto>> result = await orderReadService.GetAllOrdersAsync();
                    if (result is { IsSuccess: true, Value: not null })
                    {
                        logger.LogInformation("Order store cache refreshed with {Count} orders", result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh order store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while refreshing order store cache");
                }
            });
        }
    }
}
