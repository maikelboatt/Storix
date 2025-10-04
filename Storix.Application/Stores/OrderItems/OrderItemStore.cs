using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.OrderItems;
using Storix.Domain.Models;

namespace Storix.Application.Stores.OrderItems
{
    public class OrderItemStore:IOrderItemStore
    {
        private readonly Dictionary<int, OrderItem> _orderItems;
        private readonly Dictionary<int, List<int>> _orderItemsByOrderId;   // OrderId -> List of OrderItemIds
        private readonly Dictionary<int, List<int>> _orderItemsByProductId; // ProductId -> List of OrderItemIds

        public OrderItemStore( List<OrderItem>? initialOrderItems = null )
        {
            _orderItems = new Dictionary<int, OrderItem>();
            _orderItemsByOrderId = new Dictionary<int, List<int>>();
            _orderItemsByProductId = new Dictionary<int, List<int>>();

            if (initialOrderItems == null) return;

            foreach (OrderItem item in initialOrderItems)
            {
                AddToIndexes(item);
            }
        }

        public void Initialize( IEnumerable<OrderItem> orderItems )
        {
            _orderItems.Clear();
            _orderItemsByOrderId.Clear();
            _orderItemsByProductId.Clear();

            foreach (OrderItem item in orderItems)
            {
                AddToIndexes(item);
            }
        }

        public void Clear()
        {
            _orderItems.Clear();
            _orderItemsByOrderId.Clear();
            _orderItemsByProductId.Clear();
        }

        public OrderItemDto? Create( int orderItemId, OrderItemDto orderItemDto )
        {
            // Validation
            if (orderItemDto.OrderId <= 0 || orderItemDto.ProductId <= 0)
                return null;

            if (orderItemDto.Quantity <= 0)
                return null;

            if (orderItemDto.UnitPrice <= 0)
                return null;

            // Check for duplicate product in same order
            if (ProductExistsInOrder(orderItemDto.OrderId, orderItemDto.ProductId))
                return null;

            OrderItem orderItem = new(
                orderItemId,
                orderItemDto.OrderId,
                orderItemDto.ProductId,
                orderItemDto.Quantity,
                orderItemDto.UnitPrice,
                orderItemDto.TotalPrice
            );

            AddToIndexes(orderItem);
            return orderItem.ToDto();
        }

        public OrderItemDto? GetById( int orderItemId )
        {
            _orderItems.TryGetValue(orderItemId, out OrderItem? orderItem);
            return orderItem?.ToDto();
        }

        public OrderItemDto? Update( OrderItemDto orderItemDto )
        {
            if (!_orderItems.TryGetValue(orderItemDto.OrderItemId, out OrderItem? existingItem))
                return null;

            if (orderItemDto.Quantity <= 0 || orderItemDto.UnitPrice <= 0)
                return null;

            OrderItem updatedItem = existingItem with
            {
                Quantity = orderItemDto.Quantity,
                UnitPrice = orderItemDto.UnitPrice,
                TotalPrice = orderItemDto.TotalPrice
            };

            _orderItems[orderItemDto.OrderItemId] = updatedItem;
            return updatedItem.ToDto();
        }

        public bool Delete( int orderItemId )
        {
            if (!_orderItems.TryGetValue(orderItemId, out OrderItem? item))
                return false;

            // Remove from main dictionary
            _orderItems.Remove(orderItemId);

            // Remove from order index
            if (_orderItemsByOrderId.TryGetValue(item.OrderId, out List<int>? orderItemIds))
            {
                orderItemIds.Remove(orderItemId);
                if (orderItemIds.Count == 0)
                    _orderItemsByOrderId.Remove(item.OrderId);
            }

            // Remove from product index
            if (_orderItemsByProductId.TryGetValue(item.ProductId, out List<int>? productItemIds))
            {
                productItemIds.Remove(orderItemId);
                if (productItemIds.Count == 0)
                    _orderItemsByProductId.Remove(item.ProductId);
            }

            return true;
        }

        public List<OrderItemDto> GetByOrderId( int orderId )
        {
            if (!_orderItemsByOrderId.TryGetValue(orderId, out List<int>? itemIds))
                return [];

            return itemIds
                   .Select(id => _orderItems[id])
                   .Select(item => item.ToDto())
                   .ToList();
        }

        public List<OrderItemDto> GetByProductId( int productId )
        {
            if (!_orderItemsByProductId.TryGetValue(productId, out List<int>? itemIds))
                return new List<OrderItemDto>();

            return itemIds
                   .Select(id => _orderItems[id])
                   .OrderByDescending(item => item.OrderId) // Most recent orders first
                   .Select(item => item.ToDto())
                   .ToList();
        }

        public bool DeleteByOrderId( int orderId )
        {
            if (!_orderItemsByOrderId.TryGetValue(orderId, out List<int>? itemIds))
                return false;

            // Create a copy since we're modifying the collection
            List<int> itemIdsCopy = itemIds.ToList();

            foreach (int itemId in itemIdsCopy)
            {
                Delete(itemId);
            }

            return true;
        }

        public bool Exists( int orderItemId ) => _orderItems.ContainsKey(orderItemId);

        public bool OrderHasItems( int orderId ) => _orderItemsByOrderId.ContainsKey(orderId) &&
                                                    _orderItemsByOrderId[orderId].Count > 0;

        public bool ProductExistsInOrders( int productId ) => _orderItemsByProductId.ContainsKey(productId) &&
                                                              _orderItemsByProductId[productId].Count > 0;

        public bool ProductExistsInOrder( int orderId, int productId )
        {
            return _orderItemsByOrderId.TryGetValue(orderId, out List<int>? itemIds) && itemIds.Any(itemId => _orderItems[itemId].ProductId == productId);

        }

        public int GetOrderItemCount( int orderId ) => !_orderItemsByOrderId.TryGetValue(orderId, out List<int>? itemIds)
            ? 0
            : itemIds.Count;

        public int GetOrderTotalQuantity( int orderId )
        {
            return !_orderItemsByOrderId.TryGetValue(orderId, out List<int>? itemIds)
                ? 0
                : itemIds.Sum(itemId => _orderItems[itemId].Quantity);

        }

        public decimal GetOrderTotalValue( int orderId )
        {
            return !_orderItemsByOrderId.TryGetValue(orderId, out List<int>? itemIds)
                ? 0m
                : itemIds.Sum(itemId => _orderItems[itemId].TotalPrice);

        }

        public int GetTotalCount() => _orderItems.Count;

        private void AddToIndexes( OrderItem item )
        {
            // Add to main dictionary
            _orderItems[item.OrderItemId] = item;

            // Add to order index
            if (!_orderItemsByOrderId.ContainsKey(item.OrderId))
                _orderItemsByOrderId[item.OrderId] = [];

            _orderItemsByOrderId[item.OrderId]
                .Add(item.OrderItemId);

            // Add to product index
            if (!_orderItemsByProductId.ContainsKey(item.ProductId))
                _orderItemsByProductId[item.ProductId] = [];

            _orderItemsByProductId[item.ProductId]
                .Add(item.OrderItemId);
        }
    }
}
