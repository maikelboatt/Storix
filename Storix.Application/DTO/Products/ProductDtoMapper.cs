using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Products
{
    public static class ProductDtoMapper
    {
        public static ProductDto ToDto( this Product product ) => new()
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
            CreatedDate = product.CreatedDate,
            UpdatedDate = product.UpdatedDate,
            IsDeleted = product.IsDeleted,
            DeletedAt = product.DeletedAt
        };

        public static Product ToDomain( this ProductDto productDto ) => new(
            productDto.ProductId,
            productDto.Name,
            productDto.SKU,
            productDto.Description,
            productDto.Barcode,
            productDto.Price,
            productDto.Cost,
            productDto.MinStockLevel,
            productDto.MaxStockLevel,
            productDto.SupplierId,
            productDto.CategoryId,
            productDto.CreatedDate,
            productDto.UpdatedDate,
            productDto.IsDeleted,
            productDto.DeletedAt);

        public static CreateProductDto ToCreateDto( this ProductDto product ) => new()
        {
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
            IsDeleted = product.IsDeleted,
            DeletedAt = product.DeletedAt
        };

        public static UpdateProductDto ToUpdateDto( this ProductDto product ) => new()
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
            IsDeleted = product.IsDeleted,
            DeletedAt = product.DeletedAt
        };

        public static CreateProductDto ToCreateDto( this Product product ) => new()
        {
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
            IsDeleted = product.IsDeleted,
            DeletedAt = product.DeletedAt
        };

        public static Product ToDomain( this CreateProductDto dto ) => new(
            0,
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
            DateTime.MinValue,
            DateTime.UtcNow,
            dto.IsDeleted,
            dto.DeletedAt
        );

        public static Product ToDomain( this UpdateProductDto dto ) => new(
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
            DateTime.MinValue,
            DateTime.UtcNow,
            dto.IsDeleted,
            dto.DeletedAt
        );

        public static IEnumerable<ProductDto> ToDto( this IEnumerable<Product> products ) => products.Select(p => p.ToDto());
    }
}
