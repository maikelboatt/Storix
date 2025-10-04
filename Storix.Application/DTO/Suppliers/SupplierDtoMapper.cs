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
            Address = supplier.Address,
            IsDeleted = supplier.IsDeleted,
            DeletedAt = supplier.DeletedAt
        };

        public static CreateSupplierDto ToCreateDto( this SupplierDto dto ) => new()
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt
        };

        public static UpdateSupplierDto ToUpdateDto( this SupplierDto dto ) => new()
        {
            SupplierId = dto.SupplierId,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt
        };

        public static Supplier ToDomain( this SupplierDto dto ) => new(
            dto.SupplierId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            dto.IsDeleted,
            dto.DeletedAt);

        public static Supplier ToDomain( this CreateSupplierDto dto ) => new(
            0,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            dto.IsDeleted,
            dto.DeletedAt);

        public static Supplier ToDomain( this UpdateSupplierDto dto ) => new(
            dto.SupplierId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            dto.IsDeleted,
            dto.DeletedAt);

        public static IEnumerable<SupplierDto> ToDto( this IEnumerable<Supplier> customers ) => customers.Select(ToDto);
    }
}
