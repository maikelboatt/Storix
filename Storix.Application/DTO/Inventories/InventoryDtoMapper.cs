using System;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Inventories
{
    public static class InventoryDtoMapper
    {
        public static InventoryDto ToDto( this Inventory inventory ) => new()
        {
            InventoryId = inventory.InventoryId,
            ProductId = inventory.ProductId,
            LocationId = inventory.LocationId,
            CurrentStock = inventory.CurrentStock,
            ReservedStock = inventory.ReservedStock,
            LastUpdated = DateTime.Now
        };

        public static CreateInventoryDto ToCreateDto( this InventoryDto dto ) => new()
        {
            LocationId = dto.LocationId,
            CurrentStock = dto.CurrentStock,
            ReservedStock = dto.ReservedStock,
            LastUpdated = DateTime.Now
        };

        public static UpdateInventoryDto ToUpdateDto( this InventoryDto dto ) => new()
        {
            InventoryId = dto.InventoryId,
            ProductId = dto.ProductId,
            LocationId = dto.LocationId,
            CurrentStock = dto.CurrentStock,
            ReservedStock = dto.ReservedStock,
            LastUpdated = DateTime.Now
        };

        public static Inventory ToDomain( this InventoryDto dto ) => new(dto.InventoryId, dto.ProductId, dto.LocationId, dto.CurrentStock, dto.ReservedStock, dto.LastUpdated);

        public static Inventory ToDomain( this CreateInventoryDto dto ) => new(0, dto.ProductId, dto.LocationId, dto.CurrentStock, dto.ReservedStock, dto.LastUpdated);

        public static Inventory ToDomain( this UpdateInventoryDto dto ) => new(dto.InventoryId, dto.ProductId, dto.LocationId, dto.CurrentStock, dto.ReservedStock, dto.LastUpdated);
    }
}
