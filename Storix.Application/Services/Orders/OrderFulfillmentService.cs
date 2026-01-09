// OrderFulfillmentService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Repositories;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Orders
{
    public class OrderFulfillmentService(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IInventoryManager inventoryManager,
        ILogger<OrderFulfillmentService> logger ):IOrderFulfillmentService
    {
        /// <summary>
        /// Fulfills a purchase order by INCREASING inventory at receiving location
        /// </summary>
        public async Task<DatabaseResult> FulfillPurchaseOrderAsync(
            int orderId,
            int receivingLocationId,
            int userId )
        {
            logger.LogInformation(
                "🔵 Starting purchase order fulfillment - OrderId: {OrderId}, Location: {LocationId}",
                orderId,
                receivingLocationId);

            try
            {
                // 1. Validate order
                DatabaseResult<Order> orderValidation = await ValidateOrderForFulfillment(
                    orderId,
                    OrderType.Purchase);

                if (!orderValidation.IsSuccess || orderValidation.Value == null)
                    return DatabaseResult.Failure(orderValidation.ErrorMessage!, orderValidation.ErrorCode);

                Order order = orderValidation.Value;

                // 2. Get order items
                IEnumerable<OrderItem> orderItems = await orderItemRepository.GetByOrderIdAsync(orderId);
                List<OrderItem> items = orderItems.ToList();

                if (!items.Any())
                {
                    logger.LogError("❌ Order {OrderId} has no items", orderId);
                    return DatabaseResult.Failure("Order has no items", DatabaseErrorCode.InvalidInput);
                }

                logger.LogInformation("📦 Processing {Count} items for purchase order", items.Count);

                // 3. Process each item - INCREASE inventory
                foreach (OrderItem item in items)
                {
                    DatabaseResult increaseResult = await IncreaseInventoryForPurchaseAsync(
                        item,
                        receivingLocationId,
                        orderId,
                        userId);

                    if (!increaseResult.IsSuccess)
                    {
                        logger.LogError(
                            "❌ Failed to increase inventory for product {ProductId}: {Error}",
                            item.ProductId,
                            increaseResult.ErrorMessage);
                        return increaseResult;
                    }
                }

                logger.LogInformation("✅ Successfully fulfilled purchase order {OrderId}", orderId);
                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Exception fulfilling purchase order {OrderId}", orderId);
                return DatabaseResult.Failure(
                    $"Error fulfilling purchase order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        /// Fulfills a sales order by DECREASING inventory at shipping location
        /// </summary>
        public async Task<DatabaseResult> FulfillSalesOrderAsync(
            int orderId,
            int shippingLocationId,
            int userId )
        {
            logger.LogInformation(
                "🔵 Starting sales order fulfillment - OrderId: {OrderId}, Location: {LocationId}",
                orderId,
                shippingLocationId);

            try
            {
                // 1. Validate order
                DatabaseResult<Order> orderValidation = await ValidateOrderForFulfillment(
                    orderId,
                    OrderType.Sale);

                if (!orderValidation.IsSuccess || orderValidation.Value == null)
                    return DatabaseResult.Failure(orderValidation.ErrorMessage!, orderValidation.ErrorCode);

                Order order = orderValidation.Value;

                // 2. Get order items
                IEnumerable<OrderItem> orderItems = await orderItemRepository.GetByOrderIdAsync(orderId);
                List<OrderItem> items = orderItems.ToList();

                if (!items.Any())
                {
                    logger.LogError("❌ Order {OrderId} has no items", orderId);
                    return DatabaseResult.Failure("Order has no items", DatabaseErrorCode.InvalidInput);
                }

                logger.LogInformation("📦 Processing {Count} items for sales order", items.Count);

                // 3. Validate sufficient stock BEFORE making any changes
                DatabaseResult stockValidation = await ValidateStockAvailability(
                    items,
                    shippingLocationId);

                if (!stockValidation.IsSuccess)
                    return stockValidation;

                // 4. Process each item - DECREASE inventory
                foreach (OrderItem item in items)
                {
                    DatabaseResult decreaseResult = await DecreaseInventoryForSalesAsync(
                        item,
                        shippingLocationId,
                        orderId,
                        userId);

                    if (!decreaseResult.IsSuccess)
                    {
                        logger.LogError(
                            "❌ Failed to decrease inventory for product {ProductId}: {Error}",
                            item.ProductId,
                            decreaseResult.ErrorMessage);
                        return decreaseResult;
                    }
                }

                logger.LogInformation("✅ Successfully fulfilled sales order {OrderId}", orderId);
                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Exception fulfilling sales order {OrderId}", orderId);
                return DatabaseResult.Failure(
                    $"Error fulfilling sales order: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #region Private Methods

        /// <summary>
        /// Validates that an order can be fulfilled
        /// </summary>
        private async Task<DatabaseResult<Order>> ValidateOrderForFulfillment(
            int orderId,
            OrderType expectedType )
        {
            Order? order = await orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                logger.LogError("❌ Order {OrderId} not found", orderId);
                return DatabaseResult<Order>.Failure("Order not found", DatabaseErrorCode.NotFound);
            }

            if (order.Type != expectedType)
            {
                logger.LogError(
                    "❌ Order {OrderId} is type {ActualType}, expected {ExpectedType}",
                    orderId,
                    order.Type,
                    expectedType);
                return DatabaseResult<Order>.Failure(
                    $"Order is not a {expectedType.ToString().ToLower()} order",
                    DatabaseErrorCode.InvalidInput);
            }

            // ✅ FIXED: Accept both Active and Fulfilled status
            // Active = user is fulfilling right now
            // Fulfilled = status was changed, now processing inventory
            if (order.Status != OrderStatus.Active && order.Status != OrderStatus.Fulfilled)
            {
                logger.LogError(
                    "❌ Order {OrderId} has status {Status}, must be Active or Fulfilled",
                    orderId,
                    order.Status);
                return DatabaseResult<Order>.Failure(
                    "Order must be Active or Fulfilled to process fulfillment",
                    DatabaseErrorCode.InvalidInput);
            }

            logger.LogInformation(
                "✅ Order validation passed - Type: {Type}, Status: {Status}",
                order.Type,
                order.Status);

            return DatabaseResult<Order>.Success(order);
        }

        /// <summary>
        /// Validates that sufficient stock is available for all items
        /// </summary>
        private async Task<DatabaseResult> ValidateStockAvailability(
            List<OrderItem> items,
            int locationId )
        {
            logger.LogInformation("🔍 Validating stock availability at location {LocationId}", locationId);

            foreach (OrderItem item in items)
            {
                DatabaseResult<Inventory?> inventoryResult =
                    await inventoryManager.GetInventoryByProductAndLocationAsync(
                        item.ProductId,
                        locationId);

                if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                {
                    logger.LogError(
                        "❌ No inventory for product {ProductId} at location {LocationId}",
                        item.ProductId,
                        locationId);
                    return DatabaseResult.Failure(
                        $"Product {item.ProductId} not available at location {locationId}",
                        DatabaseErrorCode.NotFound);
                }

                Inventory inventory = inventoryResult.Value;

                // Check available stock (not reserved)
                if (inventory.AvailableStock < item.Quantity)
                {
                    logger.LogError(
                        "❌ Insufficient stock - Product: {ProductId}, Available: {Available}, Required: {Required}",
                        item.ProductId,
                        inventory.AvailableStock,
                        item.Quantity);
                    return DatabaseResult.Failure(
                        $"Insufficient stock for product {item.ProductId}. Available: {inventory.AvailableStock}, Required: {item.Quantity}",
                        DatabaseErrorCode.ConstraintViolation);
                }

                logger.LogInformation(
                    "✅ Stock OK - Product {ProductId}: Available={Available}, Required={Required}",
                    item.ProductId,
                    inventory.AvailableStock,
                    item.Quantity);
            }

            return DatabaseResult.Success();
        }

        /// <summary>
        /// Increases inventory for a purchase order item
        /// </summary>
        private async Task<DatabaseResult> IncreaseInventoryForPurchaseAsync(
            OrderItem item,
            int receivingLocationId,
            int orderId,
            int userId )
        {
            logger.LogInformation(
                "📈 INCREASING inventory - Product: {ProductId}, Quantity: +{Quantity}, Location: {LocationId}",
                item.ProductId,
                item.Quantity,
                receivingLocationId);

            try
            {
                // Check if inventory exists for this product at location
                DatabaseResult<Inventory?> inventoryResult =
                    await inventoryManager.GetInventoryByProductAndLocationAsync(
                        item.ProductId,
                        receivingLocationId);

                if (inventoryResult is { IsSuccess: true, Value: not null })
                {
                    // Inventory exists - adjust stock UP
                    Inventory inventory = inventoryResult.Value;

                    logger.LogInformation("📊 Current stock BEFORE: {Stock}", inventory.CurrentStock);

                    DatabaseResult adjustResult = await inventoryManager.AdjustStockAsync(
                        inventory.InventoryId,
                        item.Quantity, // 
                        $"Received from Purchase Order #PO-{orderId}",
                        userId);

                    if (!adjustResult.IsSuccess)
                    {
                        logger.LogError("❌ Failed to adjust stock: {Error}", adjustResult.ErrorMessage);
                        return adjustResult;
                    }

                    logger.LogInformation(
                        "📊 Stock AFTER should be: {NewStock}",
                        inventory.CurrentStock + item.Quantity);
                }
                else
                {
                    // No inventory exists - create new record
                    logger.LogInformation("📝 Creating new inventory record");

                    DatabaseResult<Inventory> createResult = await inventoryManager.CreateInventoryAsync(
                        item.ProductId,
                        receivingLocationId,
                        item.Quantity);

                    if (!createResult.IsSuccess)
                    {
                        logger.LogError("❌ Failed to create inventory: {Error}", createResult.ErrorMessage);
                        return DatabaseResult.Failure(createResult.ErrorMessage!, createResult.ErrorCode);
                    }

                    logger.LogInformation("✅ New inventory created with quantity: {Quantity}", item.Quantity);
                }

                // Create transaction record for audit trail
                DatabaseResult<InventoryTransaction> transactionResult = await inventoryManager.CreateTransactionAsync(
                    item.ProductId,
                    receivingLocationId,
                    TransactionType.StockIn,
                    item.Quantity,
                    item.UnitPrice,
                    $"PO-{orderId}",
                    $"Purchase Order #PO-{orderId} received",
                    userId);

                if (!transactionResult.IsSuccess)
                {
                    logger.LogWarning(
                        "⚠️ Transaction record creation failed (inventory still updated): {Error}",
                        transactionResult.ErrorMessage);
                }

                logger.LogInformation("✅ Inventory increased successfully for product {ProductId}", item.ProductId);
                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Exception increasing inventory for product {ProductId}", item.ProductId);
                return DatabaseResult.Failure(
                    $"Error increasing inventory: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        /// Decreases inventory for a sales order item
        /// </summary>
        private async Task<DatabaseResult> DecreaseInventoryForSalesAsync(
            OrderItem item,
            int shippingLocationId,
            int orderId,
            int userId )
        {
            logger.LogInformation(
                "📉 DECREASING inventory - Product: {ProductId}, Quantity: -{Quantity}, Location: {LocationId}",
                item.ProductId,
                item.Quantity,
                shippingLocationId);

            try
            {
                // Get inventory (we already validated it exists)
                DatabaseResult<Inventory?> inventoryResult =
                    await inventoryManager.GetInventoryByProductAndLocationAsync(
                        item.ProductId,
                        shippingLocationId);

                if (!inventoryResult.IsSuccess || inventoryResult.Value == null)
                {
                    logger.LogError("❌ Inventory not found");
                    return DatabaseResult.Failure(
                        $"No inventory found for product {item.ProductId} at location {shippingLocationId}",
                        DatabaseErrorCode.NotFound);
                }

                Inventory inventory = inventoryResult.Value;

                logger.LogInformation(
                    "📊 Current stock BEFORE: OnHand={OnHand}, Reserved={Reserved}, Available={Available}",
                    inventory.CurrentStock,
                    inventory.ReservedStock,
                    inventory.AvailableStock);

                // Step 1: Release reserved stock (if any)
                if (inventory.ReservedStock >= item.Quantity)
                {
                    logger.LogInformation("🔓 Releasing {Quantity} reserved stock", item.Quantity);

                    DatabaseResult releaseResult = await inventoryManager.ReleaseReservedStockAsync(
                        inventory.InventoryId,
                        item.Quantity);

                    if (!releaseResult.IsSuccess)
                    {
                        logger.LogError("❌ Failed to release reserved stock: {Error}", releaseResult.ErrorMessage);
                        // Continue anyway - reserved stock tracking is separate from actual stock
                    }
                    else
                    {
                        logger.LogInformation("✅ Reserved stock released");
                    }
                }
                else if (inventory.ReservedStock > 0)
                {
                    logger.LogWarning(
                        "⚠️ Partial reserved stock: {Reserved} < {Required}",
                        inventory.ReservedStock,
                        item.Quantity);

                    // Release whatever is reserved
                    await inventoryManager.ReleaseReservedStockAsync(
                        inventory.InventoryId,
                        inventory.ReservedStock);
                }

                // Step 2: Decrease actual stock
                logger.LogInformation("🔻 Decreasing actual stock by {Quantity}", item.Quantity);

                DatabaseResult adjustResult = await inventoryManager.AdjustStockAsync(
                    inventory.InventoryId,
                    -item.Quantity,  
                    $"Shipped for Sales Order #SO-{orderId}",
                    userId);

                if (!adjustResult.IsSuccess)
                {
                    logger.LogError("❌ CRITICAL: Failed to decrease stock: {Error}", adjustResult.ErrorMessage);
                    return adjustResult;
                }

                logger.LogInformation(
                    "📊 Stock AFTER should be: {NewStock}",
                    inventory.CurrentStock - item.Quantity);

                // Step 3: Verify the change actually happened
                DatabaseResult<Inventory?> verifyResult =
                    await inventoryManager.GetInventoryByProductAndLocationAsync(
                        item.ProductId,
                        shippingLocationId);

                if (verifyResult is { IsSuccess: true, Value: not null })
                {
                    logger.LogInformation(
                        "🔍 VERIFICATION: New stock = {Stock} (expected {Expected})",
                        verifyResult.Value.CurrentStock,
                        inventory.CurrentStock - item.Quantity);

                    if (verifyResult.Value.CurrentStock != inventory.CurrentStock - item.Quantity)
                    {
                        logger.LogError("❌ MISMATCH! Stock did not decrease correctly!");
                    }
                }

                // Step 4: Create transaction record for audit trail
                DatabaseResult<InventoryTransaction> transactionResult = await inventoryManager.CreateTransactionAsync(
                    item.ProductId,
                    shippingLocationId,
                    TransactionType.StockOut,
                    item.Quantity,
                    item.UnitPrice,
                    $"SO-{orderId}",
                    $"Sales Order #SO-{orderId} shipped",
                    userId);

                if (!transactionResult.IsSuccess)
                {
                    logger.LogWarning(
                        "⚠️ Transaction record creation failed (inventory still updated): {Error}",
                        transactionResult.ErrorMessage);
                }

                logger.LogInformation("✅ Inventory decreased successfully for product {ProductId}", item.ProductId);
                return DatabaseResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Exception decreasing inventory for product {ProductId}", item.ProductId);
                return DatabaseResult.Failure(
                    $"Error decreasing inventory: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
