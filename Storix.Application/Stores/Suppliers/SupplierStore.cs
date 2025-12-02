using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Storix.Application.DTO.Suppliers;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Suppliers
{
    /// <summary>
    /// In-memory cache for active (non-deleted) suppliers.
    /// Provides fast lookup for frequently accessed supplier data.
    /// </summary>
    public class SupplierStore:ISupplierStore
    {
        private readonly Dictionary<int, Supplier> _suppliers;
        private readonly Dictionary<string, int> _emailIndex;
        private readonly Dictionary<string, int> _phoneIndex;

        public SupplierStore( List<Supplier>? initialSuppliers = null )
        {
            _suppliers = new Dictionary<int, Supplier>();
            _emailIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _phoneIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (initialSuppliers == null) return;

            // Only cache active suppliers
            foreach (Supplier supplier in initialSuppliers.Where(s => !s.IsDeleted))
            {
                _suppliers[supplier.SupplierId] = supplier;

                if (!string.IsNullOrWhiteSpace(supplier.Email))
                    _emailIndex[supplier.Email] = supplier.SupplierId;

                if (!string.IsNullOrWhiteSpace(supplier.Phone))
                    _emailIndex[supplier.Phone] = supplier.SupplierId;
            }
        }

        public void Initialize( IEnumerable<Supplier> suppliers )
        {
            _suppliers.Clear();
            _emailIndex.Clear();
            _phoneIndex.Clear();

            foreach (Supplier supplier in suppliers)
            {
                _suppliers[supplier.SupplierId] = supplier;

                if (!string.IsNullOrWhiteSpace(supplier.Email))
                    _emailIndex[supplier.Email] = supplier.SupplierId;

                if (!string.IsNullOrWhiteSpace(supplier.Phone))
                    _emailIndex[supplier.Phone] = supplier.SupplierId;
            }
        }

        public void Clear()
        {
            _suppliers.Clear();
            _emailIndex.Clear();
            _phoneIndex.Clear();
        }

        public event Action<Supplier>? SupplierAdded;
        public event Action<Supplier>? SupplierUpdated;
        public event Action<int>? SupplierDeleted;

        public SupplierDto? Create( int supplierId, CreateSupplierDto createSupplierDto )
        {
            if (string.IsNullOrWhiteSpace(createSupplierDto.Name))
                return null;

            // Check if email already exists (if provided).
            if (!string.IsNullOrWhiteSpace(createSupplierDto.Email) && EmailExists(createSupplierDto.Email))
            {
                return null;
            }

            // Check if phone already exists (if provided).
            if (!string.IsNullOrWhiteSpace(createSupplierDto.Phone) && PhoneExists(createSupplierDto.Phone))
            {
                return null;
            }

            Supplier supplier = new(
                supplierId,
                createSupplierDto.Name,
                createSupplierDto.Email,
                createSupplierDto.Phone,
                createSupplierDto.Address,
                false,
                null);

            _suppliers[supplierId] = supplier;

            if (!string.IsNullOrWhiteSpace(supplier.Email))
                _emailIndex[supplier.Email] = supplierId;

            if (!string.IsNullOrWhiteSpace(supplier.Phone))
                _phoneIndex[supplier.Phone] = supplierId;

            SupplierAdded?.Invoke(supplier);
            return supplier.ToDto();
        }

        public SupplierDto? Update( UpdateSupplierDto updateSupplierDto )
        {
            // Only update active suppliers
            if (!_suppliers.TryGetValue(updateSupplierDto.SupplierId, out Supplier? existingSupplier))
                return null; // Supplier not found in active cache.

            if (string.IsNullOrWhiteSpace(updateSupplierDto.Name))
                return null;

            // Check email availability (excluding current supplier).
            if (!string.IsNullOrWhiteSpace(updateSupplierDto.Email) && EmailExists(updateSupplierDto.Email, updateSupplierDto.SupplierId))
                return null;

            // Check phone availability (excluding current supplier).
            if (!string.IsNullOrWhiteSpace(updateSupplierDto.Phone) && PhoneExists(updateSupplierDto.Phone, updateSupplierDto.SupplierId))
                return null;

            // Remove old email from index if it changed
            if (!string.IsNullOrWhiteSpace(existingSupplier.Email) &&
                !string.Equals(existingSupplier.Email, updateSupplierDto.Email, StringComparison.OrdinalIgnoreCase))
            {
                _emailIndex.Remove(existingSupplier.Email);
            }

            // Remove old phone from index if it changed
            if (!string.IsNullOrWhiteSpace(existingSupplier.Phone) &&
                !string.Equals(existingSupplier.Phone, updateSupplierDto.Phone, StringComparison.OrdinalIgnoreCase))
            {
                _phoneIndex.Remove(existingSupplier.Phone);
            }

            Supplier updatedSupplier = existingSupplier with
            {
                Name = updateSupplierDto.Name.Trim(),
                Email = updateSupplierDto.Email.Trim(),
                Phone = updateSupplierDto.Phone.Trim(),
                Address = updateSupplierDto.Address.Trim()
            };

            _suppliers[updateSupplierDto.SupplierId] = updatedSupplier;

            if (!string.IsNullOrWhiteSpace(updatedSupplier.Email))
                _emailIndex[updatedSupplier.Email] = updatedSupplier.SupplierId;

            if (!string.IsNullOrWhiteSpace(updatedSupplier.Phone))
                _phoneIndex[updatedSupplier.Phone] = updatedSupplier.SupplierId;

            SupplierUpdated?.Invoke(updatedSupplier);
            return updatedSupplier.ToDto();
        }

        public bool Delete( int supplierId )
        {
            // Remove from active cache
            if (!_suppliers.Remove(supplierId, out Supplier? supplier)) return false;

            if (!string.IsNullOrWhiteSpace(supplier.Email))
                _emailIndex.Remove(supplier.Email);

            if (!string.IsNullOrWhiteSpace(supplier.Phone))
                _phoneIndex.Remove(supplier.Phone);

            SupplierDeleted?.Invoke(supplierId);
            return true;
        }

        public SupplierDto? GetById( int supplierId ) => _suppliers.TryGetValue(supplierId, out Supplier? supplier)
            ? supplier.ToDto()
            : null;

        public string? GetSupplierName( int supplierId ) => GetById(supplierId)
            ?.Name;

        public SupplierDto? GetByEmail( string email )
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Fast email lookup using index
            if (_emailIndex.TryGetValue(email, out int supplierId))
                return _suppliers.TryGetValue(supplierId, out Supplier? supplier)
                    ? supplier.ToDto()
                    : null;

            return null;
        }

        public SupplierDto? GetByPhone( string phone )
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Fast phone lookup using index
            if (_phoneIndex.TryGetValue(phone, out int supplierId))
                return _suppliers.TryGetValue(supplierId, out Supplier? supplier)
                    ? supplier.ToDto()
                    : null;

            return null;
        }

        public List<SupplierDto> GetAll() => _suppliers
                                             .Values.OrderBy(s => s.Name)
                                             .Select(s => s.ToDto())
                                             .ToList();

        public List<SupplierDto> Search( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return GetAll();
            }

            string searchLower = searchTerm.ToLowerInvariant();

            return _suppliers
                   .Values
                   .Where(s =>
                              s
                                  .Name.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ||
                              s
                                  .Email.ToLowerInvariant()
                                  .Contains(searchLower) ||
                              s
                                  .Phone.ToLowerInvariant()
                                  .Contains(searchLower) ||
                              s
                                  .Address.ToLowerInvariant()
                                  .Contains(searchLower))
                   .OrderBy(c => c.Name)
                   .Select(c => c.ToDto())
                   .ToList();
        }

        public int GetCount() => _suppliers.Count;

        public int GetActiveCount() => _suppliers.Count;

        public List<SupplierDto> GetActiveSuppliers() => _suppliers
                                                         .Values.OrderBy(s => s.Name)
                                                         .Select(s => s.ToDto())
                                                         .ToList();

        public IEnumerable<Supplier> GetAllSuppliers() => _suppliers.Values.OrderBy(s => s.Name);

        #region Validation

        public bool Exists( int supplierId ) => _suppliers.ContainsKey(supplierId);

        public bool EmailExists( string email, int? excludeSupplierId = null )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            if (_emailIndex.TryGetValue(email, out int supplierId))
            {
                return excludeSupplierId == null || supplierId != excludeSupplierId.Value;
            }
            return false;
        }

        public bool PhoneExists( string phone, int? excludeSupplierId = null )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            if (_phoneIndex.TryGetValue(phone, out int supplierId))
            {
                return excludeSupplierId == null || supplierId != excludeSupplierId.Value;
            }
            return false;
        }

        #endregion
    }
}
