// using Storix.Domain.Enums;
// using Storix.Domain.Interfaces;
//
// namespace Storix.Domain.Models
// {
//     public record Product(
//         int ProductId,
//         string Name,
//         string SKU,
//         string Description,
//         string? Barcode,
//         decimal Price,
//         decimal Cost,
//         int MinStockLevel,
//         int MaxStockLevel,
//         int SupplierId,
//         int CategoryId,
//         DateTime CreatedDate,
//         DateTime? UpdatedDate = null,
//         bool IsDeleted = false,
//         DateTime? DeletedAt = null
//     ):ISoftDeletable
//     {
//         public decimal ProfitMargin => Price - Cost;
//
//
//         // You can still add business logic methods if needed
//         public bool IsLowStock( int currentStock ) => currentStock <= MinStockLevel;
//     }
//
//     public record Category(
//         int CategoryId,
//         string Name,
//         string? Description,
//         int? ParentCategoryId,
//         string? ImageUrl,
//         bool IsDeleted = false,
//         DateTime? DeletedAt = null ):ISoftDeletable;
//
//     public record Order(
//         int OrderId,
//         OrderType Type,
//         OrderStatus Status,
//         int? SupplierId,
//         int? CustomerId,
//         DateTime OrderDate,
//         DateTime? DeliveryDate,
//         string? Notes,
//         int CreatedBy )
//     {
//         // You can add computed properties
//         public bool IsOverdue => DeliveryDate.HasValue && DeliveryDate < DateTime.UtcNow;
//     }
//
//     public record OrderItem(
//         int OrderItemId,
//         int OrderId,
//         int ProductId,
//         int Quantity,
//         decimal UnitPrice,
//         decimal TotalPrice )
//     {
//         // Business logic for validation
//         public bool IsValidTotal => TotalPrice == UnitPrice * Quantity;
//     }
//
//     public record Inventory(
//         int InventoryId,
//         int ProductId,
//         int LocationId,
//         int CurrentStock,
//         int ReservedStock,
//         DateTime LastUpdated )
//     {
//         public int AvailableStock => CurrentStock - ReservedStock;
//         public bool IsInStock => AvailableStock > 0;
//     }
//
//     public record InventoryMovement(
//         int MovementId,
//         int ProductId,
//         int FromLocationId,
//         int ToLocationId,
//         int Quantity,
//         string? Notes,
//         int CreatedBy,
//         DateTime CreatedDate );
//
//     public record InventoryTransaction(
//         int TransactionId,
//         int ProductId,
//         int LocationId,
//         TransactionType Type,
//         int Quantity,
//         decimal? UnitCost,
//         string? Reference,
//         string? Notes,
//         int CreatedBy,
//         DateTime CreatedDate );
//
//     public record Location(
//         int LocationId,
//         string Name,
//         string? Description,
//         LocationType Type,
//         string? Address,
//         bool IsDeleted = false,
//         DateTime? DeletedAt = null ):ISoftDeletable;
//
//     public record Supplier(
//         int SupplierId,
//         string Name,
//         string Email,
//         string Phone,
//         string Address,
//         bool IsDeleted = false,
//         DateTime? DeletedAt = null ):ISoftDeletable;
//
//     public record Customer(
//         int CustomerId,
//         string Name,
//         string? Email,
//         string? Phone,
//         string? Address,
//         bool IsDeleted = false,
//         DateTime? DeletedAt = null ):ISoftDeletable;
//     
//     public record User(
//         int UserId,
//         string Username,
//         string Password,
//         string Role,
//         string? FullName,
//         string? Email,
//         bool IsActive,
//         bool IsDeleted = false,
//         DateTime? DeletedAt = null ):ISoftDeletable;
// }
//
//
//
//


