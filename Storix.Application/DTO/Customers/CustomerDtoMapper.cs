using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Customers
{
    public static class CustomerDtoMapper
    {
        public static CustomerDto ToDto( this Customer customer ) => new()
        {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address
        };

        public static CreateCustomerDto ToCreateDto( this CustomerDto dto ) => new()
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        public static UpdateCustomerDto ToUpdateDto( this CustomerDto dto ) => new()
        {
            CustomerId = dto.CustomerId,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        public static Customer ToDomain( this CustomerDto dto ) => new(
            dto.CustomerId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            false,
            null);

        public static Customer ToDomain( this CreateCustomerDto dto ) => new(
            0,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            false,
            null);

        public static Customer ToDomain( this UpdateCustomerDto dto, bool isDeleted = false, DateTime? deletedAt = null ) => new(
            dto.CustomerId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Address,
            isDeleted,
            deletedAt);

        public static IEnumerable<CustomerDto> ToDto( this IEnumerable<Customer> customers ) => customers.Select(ToDto);
    }
}
