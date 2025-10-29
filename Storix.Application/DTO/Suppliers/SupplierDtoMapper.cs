using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Suppliers
{
    public static class SupplierDtoMapper
    {
        public static SupplierDto ToDto( this Supplier supplier ) => new()
        {
            SupplierId = supplier.SupplierId,
            Name = supplier.Name,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address
        };

        public static CreateSupplierDto ToCreateDto( this SupplierDto dto ) => new()
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        public static UpdateSupplierDto ToUpdateDto( this SupplierDto dto ) => new()
        {
            SupplierId = dto.SupplierId,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        public static Supplier ToDomain( this SupplierDto dto ) => new(
            dto.SupplierId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            false,
            null);

        public static Supplier ToDomain( this CreateSupplierDto dto ) => new(
            0,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            false,
            null);

        public static Supplier ToDomain( this UpdateSupplierDto dto, bool isDeleted = false, DateTime? deletedAt = null ) => new(
            dto.SupplierId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            isDeleted,
            deletedAt);

        public static IEnumerable<SupplierDto> ToDto( this IEnumerable<Supplier> customers ) => customers.Select(ToDto);
    }
}
