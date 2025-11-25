using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Customers
{
    /// <summary>
    ///     In-memory cache for active (non-deleted) customers.
    ///     Provides fast lookup for frequently accessed customer data.
    /// </summary>
    public class CustomerStore:ICustomerStore
    {
        private readonly Dictionary<int, Customer> _customers;
        private readonly Dictionary<string, int> _emailIndex; // Fast email lookup
        private readonly Dictionary<string, int> _phoneIndex; // Fast phone lookup

        public CustomerStore( List<Customer>? initialCustomers = null )
        {
            _customers = new Dictionary<int, Customer>();
            _emailIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _phoneIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (initialCustomers == null) return;

            // Only cache active customers
            foreach (Customer customer in initialCustomers.Where(c => !c.IsDeleted))
            {
                _customers[customer.CustomerId] = customer;

                if (!string.IsNullOrWhiteSpace(customer.Email))
                    _emailIndex[customer.Email] = customer.CustomerId;

                if (!string.IsNullOrWhiteSpace(customer.Phone))
                    _phoneIndex[customer.Phone] = customer.CustomerId;
            }
        }

        public void Initialize( IEnumerable<Customer> customers )
        {
            _customers.Clear();
            _emailIndex.Clear();
            _phoneIndex.Clear();

            // Only cache active customers
            foreach (Customer customer in customers.Where(c => !c.IsDeleted))
            {
                _customers[customer.CustomerId] = customer;

                if (!string.IsNullOrWhiteSpace(customer.Email))
                    _emailIndex[customer.Email] = customer.CustomerId;

                if (!string.IsNullOrWhiteSpace(customer.Phone))
                    _phoneIndex[customer.Phone] = customer.CustomerId;
            }
        }

        public void Clear()
        {
            _customers.Clear();
            _emailIndex.Clear();
            _phoneIndex.Clear();
        }

        public event Action<Customer>? CustomerAdded;
        public event Action<Customer>? CustomerUpdated;
        public event Action<int>? CustomerDeleted;

        public CustomerDto? Create( int customerId, CreateCustomerDto createDto )
        {
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return null;
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrWhiteSpace(createDto.Email) && EmailExists(createDto.Email))
            {
                return null;
            }

            // Check if phone already exists (if provided)
            if (!string.IsNullOrWhiteSpace(createDto.Phone) && PhoneExists(createDto.Phone))
            {
                return null;
            }

            Customer customer = new(
                customerId,
                createDto.Name.Trim(),
                createDto.Email?.Trim(),
                createDto.Phone?.Trim(),
                createDto.Address?.Trim(),
                false,
                null
            );

            _customers[customerId] = customer;

            if (!string.IsNullOrWhiteSpace(customer.Email))
                _emailIndex[customer.Email] = customerId;

            if (!string.IsNullOrWhiteSpace(customer.Phone))
                _phoneIndex[customer.Phone] = customerId;

            CustomerAdded?.Invoke(customer);
            return customer.ToDto();
        }

        public CustomerDto? Update( UpdateCustomerDto updateDto )
        {
            // Only update active customers
            if (!_customers.TryGetValue(updateDto.CustomerId, out Customer? existingCustomer))
            {
                return null; // Customer not found in active cache
            }

            if (string.IsNullOrWhiteSpace(updateDto.Name))
            {
                return null;
            }

            // Check email availability (excluding current customer)
            if (!string.IsNullOrWhiteSpace(updateDto.Email) && EmailExists(updateDto.Email, updateDto.CustomerId))
            {
                return null;
            }

            // Check phone availability (excluding current customer)
            if (!string.IsNullOrWhiteSpace(updateDto.Phone) && PhoneExists(updateDto.Phone, updateDto.CustomerId))
            {
                return null;
            }

            // Remove old email from index if it changed
            if (!string.IsNullOrWhiteSpace(existingCustomer.Email) &&
                !string.Equals(existingCustomer.Email, updateDto.Email, StringComparison.OrdinalIgnoreCase))
            {
                _emailIndex.Remove(existingCustomer.Email);
            }

            // Remove old phone from index if it changed
            if (!string.IsNullOrWhiteSpace(existingCustomer.Phone) &&
                !string.Equals(existingCustomer.Phone, updateDto.Phone, StringComparison.OrdinalIgnoreCase))
            {
                _phoneIndex.Remove(existingCustomer.Phone);
            }

            Customer updatedCustomer = existingCustomer with
            {
                Name = updateDto.Name.Trim(),
                Email = updateDto.Email?.Trim(),
                Phone = updateDto.Phone?.Trim(),
                Address = updateDto.Address?.Trim()
                // IsDeleted, DeletedAt remain unchanged
            };

            _customers[updateDto.CustomerId] = updatedCustomer;

            if (!string.IsNullOrWhiteSpace(updatedCustomer.Email))
                _emailIndex[updatedCustomer.Email] = updatedCustomer.CustomerId;

            if (!string.IsNullOrWhiteSpace(updatedCustomer.Phone))
                _phoneIndex[updatedCustomer.Phone] = updatedCustomer.CustomerId;

            CustomerUpdated?.Invoke(updatedCustomer);
            return updatedCustomer.ToDto();
        }

        public bool Delete( int customerId )
        {
            // Remove from active cache
            if (!_customers.Remove(customerId, out Customer? customer)) return false;

            if (!string.IsNullOrWhiteSpace(customer.Email))
                _emailIndex.Remove(customer.Email);

            if (!string.IsNullOrWhiteSpace(customer.Phone))
                _phoneIndex.Remove(customer.Phone);

            CustomerDeleted?.Invoke(customerId);
            return true;
        }

        public CustomerDto? GetById( int customerId ) =>
            // Only searches active customers
            _customers.TryGetValue(customerId, out Customer? customer)
                ? customer.ToDto()
                : null;

        public CustomerDto? GetByEmail( string email )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            // Fast email lookup using index
            if (_emailIndex.TryGetValue(email, out int customerId))
            {
                return _customers.TryGetValue(customerId, out Customer? customer)
                    ? customer.ToDto()
                    : null;
            }

            return null;
        }

        public CustomerDto? GetByPhone( string phone )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            // Fast phone lookup using index
            if (_phoneIndex.TryGetValue(phone, out int customerId))
            {
                return _customers.TryGetValue(customerId, out Customer? customer)
                    ? customer.ToDto()
                    : null;
            }

            return null;
        }

        public List<CustomerDto> GetAll()
        {
            return _customers
                   .Values
                   .OrderBy(c => c.Name)
                   .Select(c => c.ToDto())
                   .ToList();
        }

        public List<CustomerDto> Search( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return GetAll();
            }

            string searchLower = searchTerm.ToLowerInvariant();

            return _customers
                   .Values
                   .Where(c =>
                              c
                                  .Name.ToLowerInvariant()
                                  .Contains(searchLower) ||
                              c.Email != null && c
                                                 .Email.ToLowerInvariant()
                                                 .Contains(searchLower) ||
                              c.Phone != null && c
                                                 .Phone.ToLowerInvariant()
                                                 .Contains(searchLower) ||
                              c.Address != null && c
                                                   .Address.ToLowerInvariant()
                                                   .Contains(searchLower))
                   .OrderBy(c => c.Name)
                   .Select(c => c.ToDto())
                   .ToList();
        }

        public bool Exists( int customerId ) =>
            // Only checks active customers
            _customers.ContainsKey(customerId);

        public bool EmailExists( string email, int? excludeCustomerId = null )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            if (_emailIndex.TryGetValue(email, out int customerId))
            {
                return excludeCustomerId == null || customerId != excludeCustomerId.Value;
            }

            return false;
        }

        public bool PhoneExists( string phone, int? excludeCustomerId = null )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            if (_phoneIndex.TryGetValue(phone, out int customerId))
            {
                return excludeCustomerId == null || customerId != excludeCustomerId.Value;
            }

            return false;
        }

        public int GetCount() => _customers.Count;

        public int GetActiveCount() => _customers.Count;

        public List<CustomerDto> GetActiveCustomers() => GetAll();

        public IEnumerable<Customer> GetAllCustomers()
        {
            return _customers.Values.OrderBy(c => c.Name);
        }
    }
}
