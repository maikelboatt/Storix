// namespace Storix.Application.DTO
// {
//     public class ProductDto
//     {
//         public int ProductId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string SKU { get; set; } = string.Empty;
//         public string Description { get; set; } = string.Empty;
//         public string? Barcode { get; set; }
//         public decimal Price { get; set; }
//         public decimal Cost { get; set; }
//         public int MinStockLevel { get; set; }
//         public int MaxStockLevel { get; set; }
//         public int SupplierId { get; set; }
//         public int CategoryId { get; set; }
//         public bool IsActive { get; set; }
//         public DateTime CreatedDate { get; set; }
//         public DateTime? UpdatedDate { get; set; }
//     }
//
//     public class CreateProductDto
//     {
//         public string Name { get; set; } = string.Empty;
//         public string SKU { get; set; } = string.Empty;
//         public string Description { get; set; } = string.Empty;
//         public string? Barcode { get; set; }
//         public decimal Price { get; set; }
//         public decimal Cost { get; set; }
//         public int MinStockLevel { get; set; }
//         public int MaxStockLevel { get; set; }
//         public int SupplierId { get; set; }
//         public int CategoryId { get; set; }
//         public bool IsActive { get; set; } = true;
//     }
//
//     public class UpdateProductDto
//     {
//         public int ProductId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string SKU { get; set; } = string.Empty;
//         public string Description { get; set; } = string.Empty;
//         public string? Barcode { get; set; }
//         public decimal Price { get; set; }
//         public decimal Cost { get; set; }
//         public int MinStockLevel { get; set; }
//         public int MaxStockLevel { get; set; }
//         public int SupplierId { get; set; }
//         public int CategoryId { get; set; }
//         public bool IsActive { get; set; }
//     }
//
//     public class CategoryDto
//     {
//         public int CategoryId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public int? ParentCategoryId { get; set; }
//     }
//
//     public class CreateCategoryDto
//     {
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public int? ParentCategoryId { get; set; }
//     }
//
//     public class UpdateCategoryDto
//     {
//         public int CategoryId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public int? ParentCategoryId { get; set; }
//     }
//
//     public class OrderDto
//     {
//         public int OrderId { get; set; }
//         public OrderType Type { get; set; }
//         public OrderStatus Status { get; set; }
//         public int? SupplierId { get; set; }
//         public int? CustomerId { get; set; }
//         public DateTime OrderDate { get; set; }
//         public DateTime? DeliveryDate { get; set; }
//         public string? Notes { get; set; }
//         public int CreatedBy { get; set; }
//     }
//
//     public class CreateOrderDto
//     {
//         public OrderType Type { get; set; }
//         public int? SupplierId { get; set; }
//         public int? CustomerId { get; set; }
//         public DateTime OrderDate { get; set; }
//         public DateTime? DeliveryDate { get; set; }
//         public string? Notes { get; set; }
//         public int CreatedBy { get; set; }
//     }
//
//     public class UpdateOrderDto
//     {
//         public int OrderId { get; set; }
//         public OrderStatus Status { get; set; }
//         public DateTime? DeliveryDate { get; set; }
//         public string? Notes { get; set; }
//     }
//
//     public class OrderItemDto
//     {
//         public int OrderItemId { get; set; }
//         public int OrderId { get; set; }
//         public int ProductId { get; set; }
//         public int Quantity { get; set; }
//         public decimal UnitPrice { get; set; }
//         public decimal TotalPrice { get; set; }
//     }
//
//     public class CreateOrderItemDto
//     {
//         public int OrderId { get; set; }
//         public int ProductId { get; set; }
//         public int Quantity { get; set; }
//         public decimal UnitPrice { get; set; }
//         public decimal TotalPrice { get; set; }
//     }
//
//     public class UpdateOrderItemDto
//     {
//         public int OrderItemId { get; set; }
//         public int Quantity { get; set; }
//         public decimal UnitPrice { get; set; }
//         public decimal TotalPrice { get; set; }
//     }
//
//     public class InventoryDto
//     {
//         public int InventoryId { get; set; }
//         public int ProductId { get; set; }
//         public int LocationId { get; set; }
//         public int CurrentStock { get; set; }
//         public int ReservedStock { get; set; }
//         public DateTime LastUpdated { get; set; }
//     }
//
//     public class CreateInventoryDto
//     {
//         public int ProductId { get; set; }
//         public int LocationId { get; set; }
//         public int CurrentStock { get; set; }
//         public int ReservedStock { get; set; } = 0;
//     }
//
//     public class UpdateInventoryDto
//     {
//         public int InventoryId { get; set; }
//         public int CurrentStock { get; set; }
//         public int ReservedStock { get; set; }
//     }
//
//     public class SupplierDto
//     {
//         public int SupplierId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public string Phone { get; set; } = string.Empty;
//         public string Address { get; set; } = string.Empty;
//     }
//
//     public class CreateSupplierDto
//     {
//         public string Name { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public string Phone { get; set; } = string.Empty;
//         public string Address { get; set; } = string.Empty;
//     }
//
//     public class UpdateSupplierDto
//     {
//         public int SupplierId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public string Phone { get; set; } = string.Empty;
//         public string Address { get; set; } = string.Empty;
//     }
//
//     public class LocationDto
//     {
//         public int LocationId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public LocationType Type { get; set; }
//         public string? Address { get; set; }
//     }
//
//     public class CreateLocationDto
//     {
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public LocationType Type { get; set; }
//         public string? Address { get; set; }
//     }
//
//     public class UpdateLocationDto
//     {
//         public int LocationId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public LocationType Type { get; set; }
//         public string? Address { get; set; }
//     }
//
//     public class CustomerDto
//     {
//         public int CustomerId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Email { get; set; }
//         public string? Phone { get; set; }
//         public string? Address { get; set; }
//         public bool IsActive { get; set; }
//     }
//
//     public class CreateCustomerDto
//     {
//         public string Name { get; set; } = string.Empty;
//         public string? Email { get; set; }
//         public string? Phone { get; set; }
//         public string? Address { get; set; }
//         public bool IsActive { get; set; } = true;
//     }
//
//     public class UpdateCustomerDto
//     {
//         public int CustomerId { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Email { get; set; }
//         public string? Phone { get; set; }
//         public string? Address { get; set; }
//         public bool IsActive { get; set; }
//     }
//
//     public class UserDto
//     {
//         public int UserId { get; set; }
//         public string Username { get; set; } = string.Empty;
//         public string Role { get; set; } = string.Empty;
//         public string? FullName { get; set; }
//         public string? Email { get; set; }
//         public bool IsActive { get; set; }
//     }
//
//     public class CreateUserDto
//     {
//         public string Username { get; set; } = string.Empty;
//         public string Password { get; set; } = string.Empty;
//         public string Role { get; set; } = string.Empty;
//         public string? FullName { get; set; }
//         public string? Email { get; set; }
//         public bool IsActive { get; set; } = true;
//     }
//
//     public class UpdateUserDto
//     {
//         public int UserId { get; set; }
//         public string Username { get; set; } = string.Empty;
//         public string Role { get; set; } = string.Empty;
//         public string? FullName { get; set; }
//         public string? Email { get; set; }
//         public bool IsActive { get; set; }
//     }
//
//     public class ChangePasswordDto
//     {
//         public int UserId { get; set; }
//         public string CurrentPassword { get; set; } = string.Empty;
//         public string NewPassword { get; set; } = string.Empty;
//     }
//
//     public class InventoryTransactionDto
//     {
//         public int TransactionId { get; set; }
//         public int ProductId { get; set; }
//         public int LocationId { get; set; }
//         public TransactionType Type { get; set; }
//         public int Quantity { get; set; }
//         public decimal? UnitCost { get; set; }
//         public string? Reference { get; set; }
//         public string? Notes { get; set; }
//         public int CreatedBy { get; set; }
//         public DateTime CreatedDate { get; set; }
//     }
//
//     public class CreateInventoryTransactionDto
//     {
//         public int ProductId { get; set; }
//         public int LocationId { get; set; }
//         public TransactionType Type { get; set; }
//         public int Quantity { get; set; }
//         public decimal? UnitCost { get; set; }
//         public string? Reference { get; set; }
//         public string? Notes { get; set; }
//         public int CreatedBy { get; set; }
//     }
//
//     public class InventoryMovementDto
//     {
//         public int MovementId { get; set; }
//         public int ProductId { get; set; }
//         public int FromLocationId { get; set; }
//         public int ToLocationId { get; set; }
//         public int Quantity { get; set; }
//         public string? Notes { get; set; }
//         public int CreatedBy { get; set; }
//         public DateTime CreatedDate { get; set; }
//     }
//
//     public class CreateInventoryMovementDto
//     {
//         public int ProductId { get; set; }
//         public int FromLocationId { get; set; }
//         public int ToLocationId { get; set; }
//         public int Quantity { get; set; }
//         public string? Notes { get; set; }
//         public int CreatedBy { get; set; }
//     }
//
//     Extended DTOs with additional data for views
//     public class ProductWithDetailsDto:ProductDto
//     {
//         public string SupplierName { get; set; } = string.Empty;
//         public string CategoryName { get; set; } = string.Empty;
//         public int TotalStock { get; set; }
//         public int AvailableStock { get; set; }
//     }
//
//     public class OrderWithDetailsDto:OrderDto
//     {
//         public string? SupplierName { get; set; }
//         public string? CustomerName { get; set; }
//         public string CreatedByName { get; set; } = string.Empty;
//         public decimal TotalPrice { get; set; }
//         public int TotalItems { get; set; }
//     }
//
//     public class InventoryWithDetailsDto:InventoryDto
//     {
//         public string ProductName { get; set; } = string.Empty;
//         public string ProductSKU { get; set; } = string.Empty;
//         public string LocationName { get; set; } = string.Empty;
//         public int AvailableStock { get; set; }
//         public int MinStockLevel { get; set; }
//         public int MaxStockLevel { get; set; }
//         public bool IsLowStock { get; set; }
//     }
// }


