using System;
using System.Collections.Generic;
using Storix.Application.DTO.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Customers
{
    public interface ICustomerStore
    {
        void Initialize( IEnumerable<Customer> customers );

        void Clear();

        event Action<Customer> CustomerAdded;

        event Action<Customer> CustomerUpdated;

        event Action<int> CustomerDeleted;

        CustomerDto? Create( int customerId, CreateCustomerDto createDto );

        CustomerDto? Update( UpdateCustomerDto updateDto );

        bool Delete( int customerId );

        CustomerDto? GetById( int customerId );

        string? GetCustomerName( int customerId );

        CustomerDto? GetByEmail( string email );

        CustomerDto? GetByPhone( string phone );

        List<CustomerDto> GetAll();

        List<CustomerDto> Search( string searchTerm );

        bool Exists( int customerId );

        bool EmailExists( string email, int? excludeCustomerId = null );

        bool PhoneExists( string phone, int? excludeCustomerId = null );

        int GetCount();

        int GetActiveCount();

        List<CustomerDto> GetActiveCustomers();

        IEnumerable<Customer> GetAllCustomers();
    }
}
