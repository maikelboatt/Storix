using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Products
{
    /// <summary>
    /// Provides mapping logic between Product domain models and all Product DTO types.
    /// </summary>
    public static class ProductDtoMapper
    {
        #region === Core CRUD Mappings ===

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
            UpdatedDate = product.UpdatedDate
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
            false,
            null);

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
            CategoryId = product.CategoryId
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
            CategoryId = product.CategoryId
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
            CategoryId = product.CategoryId
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
            DateTime.UtcNow,
            DateTime.MinValue,
            false,
            null
        );

        public static Product ToDomain( this UpdateProductDto dto, bool isDeleted = false, DateTime? deletedAt = null ) => new(
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
            isDeleted,
            deletedAt
        );

        public static IEnumerable<ProductDto> ToDto( this IEnumerable<Product> products ) => products.Select(p => p.ToDto());

        #endregion


        #region === List & Detail DTO Mappings ===

        public static ProductListDto ToListDto( this Product product,
            string? categoryName = null,
            string? supplierName = null,
            int currentStock = 0 ) => new()
        {
            ProductId = product.ProductId,
            Name = product.Name,
            SKU = product.SKU,
            Barcode = product.Barcode,
            Price = product.Price,
            Cost = product.Cost,
            MinStockLevel = product.MinStockLevel,
            MaxStockLevel = product.MaxStockLevel,
            CategoryName = categoryName,
            SupplierName = supplierName,
            CurrentStock = currentStock,
            IsLowStock = product.IsLowStock(currentStock),
            IsDeleted = product.IsDeleted
        };

        public static ProductWithDetailsDto ToDetailsDto( this Product product,
            string categoryName,
            string supplierName,
            int totalStock,
            int availableStock ) => new()
        {
            ProductId = product.ProductId,
            Name = product.Name,
            SKU = product.SKU,
            Barcode = product.Barcode,
            Price = product.Price,
            Cost = product.Cost,
            MinStockLevel = product.MinStockLevel,
            MaxStockLevel = product.MaxStockLevel,
            Description = product.Description,
            SupplierId = product.SupplierId,
            CategoryId = product.CategoryId,
            SupplierName = supplierName,
            CategoryName = categoryName,
            TotalStock = totalStock,
            AvailableStock = availableStock,
            CreatedDate = product.CreatedDate,
            UpdatedDate = product.UpdatedDate
        };

        public static Product ToDomain( this ProductListDto dto, int supplierId = 0, int categoryId = 0 ) => new(
            dto.ProductId,
            dto.Name,
            dto.SKU,
            string.Empty,
            dto.Barcode,
            dto.Price,
            dto.Cost,
            dto.MinStockLevel,
            dto.MaxStockLevel,
            supplierId,
            categoryId,
            DateTime.UtcNow,
            null,
            dto.IsDeleted
        );

        public static Product ToDomain( this ProductWithDetailsDto dto ) => new(
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
            dto.CreatedDate,
            dto.UpdatedDate
        );

        #endregion
    }
}
