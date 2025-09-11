// namespace Storix.Domain.Models
// {
//     /// <summary>
//     ///     Represents a product in the inventory.
//     /// </summary>
//     public class Product(
//         int productId,
//         string name,
//         string sku,
//         string description,
//         string? barcode,
//         decimal price,
//         decimal cost,
//         int minStockLevel,
//         int maxStockLevel,
//         int supplierId,
//         int categoryId,
//         bool isActive,
//         DateTime createdDate,
//         DateTime? updatedDate )
//     {
//         public int ProductId { get; init; } = productId;
//         public string Name { get; init; } = name;
//         public string SKU { get; init; } = sku;
//         public string Description { get; init; } = description;
//         public string? Barcode { get; init; } = barcode;
//         public decimal Price { get; init; } = price;
//         public decimal Cost { get; init; } = cost;
//         public int MinStockLevel { get; init; } = minStockLevel;
//         public int MaxStockLevel { get; init; } = maxStockLevel;
//         public int SupplierId { get; init; } = supplierId;
//         public int CategoryId { get; init; } = categoryId;
//         public bool IsActive { get; init; } = isActive;
//         public DateTime CreatedDate { get; init; } = createdDate;
//         public DateTime? UpdatedDate { get; init; } = updatedDate;
//     }
//
//     public class Category(
//         int categoryId,
//         string name,
//         string? description,
//         int? parentCategoryId )
//     {
//         public int CategoryId { get; init; } = categoryId;
//         public string Name { get; init; } = name;
//         public string? Description { get; init; } = description;
//         public int? ParentCategoryId { get; init; } = parentCategoryId;
//     }
//
//     public class Order(
//         int orderId,
//         OrderType type,
//         OrderStatus status,
//         int? supplierId,
//         int? customerId,
//         DateTime orderDate,
//         DateTime? deliveryDate,
//         string? notes,
//         int createdBy )
//     {
//         public int OrderId { get; init; } = orderId;
//         public OrderType Type { get; init; } = type;
//         public OrderStatus Status { get; init; } = status;
//         public int? SupplierId { get; init; } = supplierId;
//         public int? CustomerId { get; init; } = customerId;
//         public DateTime OrderDate { get; init; } = orderDate;
//         public DateTime? DeliveryDate { get; init; } = deliveryDate;
//         public string? Notes { get; init; } = notes;
//         public int CreatedBy { get; init; } = createdBy;
//     }
//
//     public class OrderItem(
//         int orderItemId,
//         int orderId,
//         int productId,
//         int quantity,
//         decimal unitPrice,
//         decimal totalPrice )
//     {
//         public int OrderItemId { get; init; } = orderItemId;
//         public int OrderId { get; init; } = orderId;
//         public int ProductId { get; init; } = productId;
//         public int Quantity { get; init; } = quantity;
//         public decimal UnitPrice { get; init; } = unitPrice;
//         public decimal TotalPrice { get; init; } = totalPrice; // Fixed bug
//     }
//
//     public class Inventory(
//         int inventoryId,
//         int productId,
//         int locationId,
//         int currentStock,
//         int reservedStock,
//         DateTime lastUpdated )
//     {
//         public int InventoryId { get; init; } = inventoryId;
//         public int ProductId { get; init; } = productId;
//         public int LocationId { get; init; } = locationId;
//         public int CurrentStock { get; init; } = currentStock;
//         public int ReservedStock { get; init; } = reservedStock;
//         public DateTime LastUpdated { get; init; } = lastUpdated;
//     }
//
//     public class Supplier(
//         int supplierId,
//         string name,
//         string email,
//         string phone,
//         string address )
//     {
//         public int SupplierId { get; init; } = supplierId;
//         public string Name { get; init; } = name;
//         public string Email { get; init; } = email;
//         public string Phone { get; init; } = phone;
//         public string Address { get; init; } = address;
//     }
//
//     public class Location(
//         int locationId,
//         string name,
//         string? description,
//         LocationType type,
//         string? address )
//     {
//         public int LocationId { get; init; } = locationId;
//         public string Name { get; init; } = name;
//         public string? Description { get; init; } = description;
//         public LocationType Type { get; init; } = type;
//         public string? Address { get; init; } = address;
//     }
//
//     public class Customer(
//         int customerId,
//         string name,
//         string? email,
//         string? phone,
//         string? address,
//         bool isActive )
//     {
//         public int CustomerId { get; init; } = customerId;
//         public string Name { get; init; } = name;
//         public string? Email { get; init; } = email;
//         public string? Phone { get; init; } = phone;
//         public string? Address { get; init; } = address;
//         public bool IsActive { get; init; } = isActive;
//     }
//
//     public class User(
//         int userId,
//         string username,
//         string passwordHash,
//         string role,
//         string? fullName,
//         string? email,
//         bool isActive )
//     {
//         public int UserId { get; init; } = userId;
//         public string Username { get; init; } = username;
//         public string PasswordHash { get; init; } = passwordHash;
//         public string Role { get; init; } = role;
//         public string? FullName { get; init; } = fullName;
//         public string? Email { get; init; } = email;
//         public bool IsActive { get; init; } = isActive;
//     }
//
//     public class InventoryTransaction(
//         int transactionId,
//         int productId,
//         int locationId,
//         TransactionType type,
//         int quantity,
//         decimal? unitCost,
//         string? reference,
//         string? notes,
//         int createdBy,
//         DateTime createdDate )
//     {
//         public int TransactionId { get; init; } = transactionId;
//         public int ProductId { get; init; } = productId;
//         public int LocationId { get; init; } = locationId;
//         public TransactionType Type { get; init; } = type;
//         public int Quantity { get; init; } = quantity;
//         public decimal? UnitCost { get; init; } = unitCost;
//         public string? Reference { get; init; } = reference;
//         public string? Notes { get; init; } = notes;
//         public int CreatedBy { get; init; } = createdBy;
//         public DateTime CreatedDate { get; init; } = createdDate;
//     }
//
//     public class InventoryMovement(
//         int movementId,
//         int productId,
//         int fromLocationId,
//         int toLocationId,
//         int quantity,
//         string? notes,
//         int createdBy,
//         DateTime createdDate )
//     {
//         public int MovementId { get; init; } = movementId;
//         public int ProductId { get; init; } = productId;
//         public int FromLocationId { get; init; } = fromLocationId;
//         public int ToLocationId { get; init; } = toLocationId;
//         public int Quantity { get; init; } = quantity;
//         public string? Notes { get; init; } = notes;
//         public int CreatedBy { get; init; } = createdBy;
//         public DateTime CreatedDate { get; init; } = createdDate;
//     }
//
//     Enums
//     public enum OrderType
//     {
//         Purchase,
//         Sale,
//         Transfer,
//         Return
//     }
//
//     public enum OrderStatus
//     {
//         Draft,
//         Pending,
//         Confirmed,
//         Processing,
//         Shipped,
//         Delivered,
//         Cancelled,
//         Returned
//     }
//
//     public enum LocationType
//     {
//         Warehouse,
//         Store,
//         Transit,
//         Virtual
//     }
//
//     public enum TransactionType
//     {
//         StockIn,
//         StockOut,
//         Adjustment,
//         Transfer,
//         Return,
//         Damaged,
//         Lost
//     }
// }


