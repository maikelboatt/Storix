using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Category;
using Storix.Application.DTO.Customer;
using Storix.Application.DTO.Inventory;
using Storix.Application.DTO.InventoryMovement;
using Storix.Application.DTO.InventoryTransaction;
using Storix.Application.DTO.Location;
using Storix.Application.DTO.Order;
using Storix.Application.DTO.OrderItem;
using Storix.Application.DTO.Product;
using Storix.Application.DTO.Supplier;
using Storix.Application.DTO.User;
using Storix.Domain.Enums;

namespace Storix.Application.DTO.Mappers
{
    public static class DtoMapper
    {
        #region Product Mappings

        public static ProductDto ToDto( this Domain.Models.Product product ) => new()
        {
            ProductId = product.ProductId,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            Barcode = product.Barcode,
            Price = product.Price,
            Cost = product.Cost,
            MinStockLevel = product.MinStockLevel,
            MaxStockLevel = product.MaxStockLevel,
            SupplierId = product.SupplierId,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            CreatedDate = product.CreatedDate,
            UpdatedDate = product.UpdatedDate
        };


        public static Domain.Models.Product ToDomain( this CreateProductDto dto ) => new(
            0, // Will be set by database
            dto.Name,
            dto.SKU,
            dto.Description,
            dto.Barcode,
            dto.Price,
            dto.Cost,
            dto.MinStockLevel,
            dto.MaxStockLevel,
            dto.SupplierId,
            dto.CategoryId,
            dto.IsActive,
            DateTime.UtcNow,
            null
        );

        public static Domain.Models.Product ToDomain( this UpdateProductDto dto ) => new(
            dto.ProductId,
            dto.Name,
            dto.SKU,
            dto.Description,
            dto.Barcode,
            dto.Price,
            dto.Cost,
            dto.MinStockLevel,
            dto.MaxStockLevel,
            dto.SupplierId,
            dto.CategoryId,
            dto.IsActive,
            DateTime.MinValue, // Will be preserved from existing
            DateTime.UtcNow
        );

        #endregion

        #region Category Mappings

        public static CategoryDto ToDto( this Domain.Models.Category category ) => new()
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId
        };

        public static Domain.Models.Category ToDomain( this CreateCategoryDto dto ) => new(
            0,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId
        );

        public static Domain.Models.Category ToDomain( this UpdateCategoryDto dto ) => new(
            dto.CategoryId,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId
        );

        #endregion

        #region Order Mappings

        public static OrderDto ToDto( this Domain.Models.Order order ) => new()
        {
            OrderId = order.OrderId,
            Type = order.Type,
            Status = order.Status,
            SupplierId = order.SupplierId,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            Notes = order.Notes,
            CreatedBy = order.CreatedBy
        };

        public static Domain.Models.Order ToDomain( this CreateOrderDto dto ) => new(
            0,
            dto.Type,
            OrderStatus.Draft,
            dto.SupplierId,
            dto.CustomerId,
            dto.OrderDate,
            dto.DeliveryDate,
            dto.Notes,
            dto.CreatedBy
        );

        public static Domain.Models.Order ToDomain( this UpdateOrderDto dto, Domain.Models.Order existingOrder ) => new(
            dto.OrderId,
            existingOrder.Type,
            dto.Status,
            existingOrder.SupplierId,
            existingOrder.CustomerId,
            existingOrder.OrderDate,
            dto.DeliveryDate,
            dto.Notes,
            existingOrder.CreatedBy
        );

        #endregion

        #region OrderItem Mappings

        public static OrderItemDto ToDto( this Domain.Models.OrderItem orderItem ) => new()
        {
            OrderItemId = orderItem.OrderItemId,
            OrderId = orderItem.OrderId,
            ProductId = orderItem.ProductId,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            TotalPrice = orderItem.TotalPrice
        };

        public static Domain.Models.OrderItem ToDomain( this CreateOrderItemDto dto ) => new(
            0,
            dto.OrderId,
            dto.ProductId,
            dto.Quantity,
            dto.UnitPrice,
            dto.TotalPrice
        );

        public static Domain.Models.OrderItem ToDomain( this UpdateOrderItemDto dto, Domain.Models.OrderItem existingItem ) => new(
            dto.OrderItemId,
            existingItem.OrderId,
            existingItem.ProductId,
            dto.Quantity,
            dto.UnitPrice,
            dto.TotalPrice
        );

        #endregion

        #region Inventory Mappings

        public static InventoryDto ToDto( this Domain.Models.Inventory inventory ) => new()
        {
            InventoryId = inventory.InventoryId,
            ProductId = inventory.ProductId,
            LocationId = inventory.LocationId,
            CurrentStock = inventory.CurrentStock,
            ReservedStock = inventory.ReservedStock,
            LastUpdated = inventory.LastUpdated
        };

        public static Domain.Models.Inventory ToDomain( this CreateInventoryDto dto ) => new(
            0,
            dto.ProductId,
            dto.LocationId,
            dto.CurrentStock,
            dto.ReservedStock,
            DateTime.UtcNow
        );

        public static Domain.Models.Inventory ToDomain( this UpdateInventoryDto dto, Domain.Models.Inventory existingInventory ) => new(
            dto.InventoryId,
            existingInventory.ProductId,
            existingInventory.LocationId,
            dto.CurrentStock,
            dto.ReservedStock,
            DateTime.UtcNow
        );

        #endregion

        #region Supplier Mappings

        public static SupplierDto ToDto( this Domain.Models.Supplier supplier ) => new()
        {
            SupplierId = supplier.SupplierId,
            Name = supplier.Name,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address
        };

        public static Domain.Models.Supplier ToDomain( this CreateSupplierDto dto ) => new(
            0,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address
        );

        public static Domain.Models.Supplier ToDomain( this UpdateSupplierDto dto ) => new(
            dto.SupplierId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address
        );

        #endregion

        #region Location Mappings

        public static LocationDto ToDto( this Domain.Models.Location location ) => new()
        {
            LocationId = location.LocationId,
            Name = location.Name,
            Description = location.Description,
            Type = location.Type,
            Address = location.Address
        };

        public static Domain.Models.Location ToDomain( this CreateLocationDto dto ) => new(
            0,
            dto.Name,
            dto.Description,
            dto.Type,
            dto.Address
        );

        public static Domain.Models.Location ToDomain( this UpdateLocationDto dto ) => new(
            dto.LocationId,
            dto.Name,
            dto.Description,
            dto.Type,
            dto.Address
        );

        #endregion

        #region Customer Mappings

        public static CustomerDto ToDto( this Domain.Models.Customer customer ) => new()
        {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            IsActive = customer.IsActive
        };

        public static Domain.Models.Customer ToDomain( this CreateCustomerDto dto ) => new(
            0,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            dto.IsActive
        );

        public static Domain.Models.Customer ToDomain( this UpdateCustomerDto dto ) => new(
            dto.CustomerId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            dto.IsActive
        );

        #endregion

        #region User Mappings

        public static UserDto ToDto( this Domain.Models.User user ) => new()
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive
        };

        public static Domain.Models.User ToDomain( this CreateUserDto dto, string passwordHash ) => new(
            0,
            dto.Username,
            passwordHash,
            dto.Role,
            dto.FullName,
            dto.Email,
            dto.IsActive
        );

        public static Domain.Models.User ToDomain( this UpdateUserDto dto, Domain.Models.User existingUser ) => new(
            dto.UserId,
            dto.Username,
            existingUser.PasswordHash, // Keep existing password
            dto.Role,
            dto.FullName,
            dto.Email,
            dto.IsActive
        );

        #endregion

        #region InventoryTransaction Mappings

        public static InventoryTransactionDto ToDto( this Domain.Models.InventoryTransaction transaction ) => new()
        {
            TransactionId = transaction.TransactionId,
            ProductId = transaction.ProductId,
            LocationId = transaction.LocationId,
            Type = transaction.Type,
            Quantity = transaction.Quantity,
            UnitCost = transaction.UnitCost,
            Reference = transaction.Reference,
            Notes = transaction.Notes,
            CreatedBy = transaction.CreatedBy,
            CreatedDate = transaction.CreatedDate
        };

        public static Domain.Models.InventoryTransaction ToDomain( this CreateInventoryTransactionDto dto ) => new(
            0,
            dto.ProductId,
            dto.LocationId,
            dto.Type,
            dto.Quantity,
            dto.UnitCost,
            dto.Reference,
            dto.Notes,
            dto.CreatedBy,
            DateTime.UtcNow
        );

        #endregion

        #region InventoryMovement Mappings

        public static InventoryMovementDto ToDto( this Domain.Models.InventoryMovement movement ) => new()
        {
            MovementId = movement.MovementId,
            ProductId = movement.ProductId,
            FromLocationId = movement.FromLocationId,
            ToLocationId = movement.ToLocationId,
            Quantity = movement.Quantity,
            Notes = movement.Notes,
            CreatedBy = movement.CreatedBy,
            CreatedDate = movement.CreatedDate
        };

        public static Domain.Models.InventoryMovement ToDomain( this CreateInventoryMovementDto dto ) => new(
            0,
            dto.ProductId,
            dto.FromLocationId,
            dto.ToLocationId,
            dto.Quantity,
            dto.Notes,
            dto.CreatedBy,
            DateTime.UtcNow
        );

        #endregion

        #region Collection Mappings

        public static IEnumerable<ProductDto> ToDto( this IEnumerable<Domain.Models.Product> products )
        {
            return products.Select(p => p.ToDto());
        }

        public static IEnumerable<CategoryDto> ToDto( this IEnumerable<Domain.Models.Category> categories )
        {
            return categories.Select(c => c.ToDto());
        }

        public static IEnumerable<OrderDto> ToDto( this IEnumerable<Domain.Models.Order> orders )
        {
            return orders.Select(o => o.ToDto());
        }

        public static IEnumerable<OrderItemDto> ToDto( this IEnumerable<Domain.Models.OrderItem> orderItems )
        {
            return orderItems.Select(oi => oi.ToDto());
        }

        public static IEnumerable<InventoryDto> ToDto( this IEnumerable<Domain.Models.Inventory> inventories )
        {
            return inventories.Select(i => i.ToDto());
        }

        public static IEnumerable<SupplierDto> ToDto( this IEnumerable<Domain.Models.Supplier> suppliers )
        {
            return suppliers.Select(s => s.ToDto());
        }

        public static IEnumerable<LocationDto> ToDto( this IEnumerable<Domain.Models.Location> locations )
        {
            return locations.Select(l => l.ToDto());
        }

        public static IEnumerable<CustomerDto> ToDto( this IEnumerable<Domain.Models.Customer> customers )
        {
            return customers.Select(c => c.ToDto());
        }

        public static IEnumerable<UserDto> ToDto( this IEnumerable<Domain.Models.User> users )
        {
            return users.Select(u => u.ToDto());
        }

        public static IEnumerable<InventoryTransactionDto> ToDto( this IEnumerable<Domain.Models.InventoryTransaction> transactions )
        {
            return transactions.Select(t => t.ToDto());
        }

        public static IEnumerable<InventoryMovementDto> ToDto( this IEnumerable<Domain.Models.InventoryMovement> movements )
        {
            return movements.Select(m => m.ToDto());
        }

        #endregion
    }
}
