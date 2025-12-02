using System;
using System.Collections.Generic;
using Storix.Application.DTO.Suppliers;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Suppliers
{
    public interface ISupplierStore
    {
        void Initialize( IEnumerable<Supplier> suppliers );

        void Clear();

        event Action<Supplier> SupplierAdded;

        event Action<Supplier> SupplierUpdated;

        event Action<int> SupplierDeleted;

        SupplierDto? Create( int supplierId, CreateSupplierDto createSupplierDto );

        SupplierDto? Update( UpdateSupplierDto updateSupplierDto );

        bool Delete( int supplierId );

        SupplierDto? GetById( int supplierId );

        string? GetSupplierName( int supplierId );

        SupplierDto? GetByEmail( string email );

        SupplierDto? GetByPhone( string phone );

        List<SupplierDto> GetAll();

        List<SupplierDto> Search( string searchTerm );

        int GetCount();

        int GetActiveCount();

        List<SupplierDto> GetActiveSuppliers();

        IEnumerable<Supplier> GetAllSuppliers();

        bool Exists( int supplierId );

        bool EmailExists( string email, int? excludeSupplierId = null );

        bool PhoneExists( string phone, int? excludeSupplierId = null );
    }
}
