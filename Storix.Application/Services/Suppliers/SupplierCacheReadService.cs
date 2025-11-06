using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Stores.Suppliers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Suppliers
{
    public class SupplierCacheReadService( ISupplierStore supplierStore, ISupplierReadService readService, ILogger<SupplierCacheReadService> logger )
        :ISupplierCacheReadService
    {
        public SupplierDto? GetSupplierByIdInCache( int supplierId )
        {
            logger.LogInformation("Retrieving supplier with ID: {SupplierId} from cache", supplierId);

            return supplierStore.GetById(supplierId);
        }

        public SupplierDto? GetSupplierByEmailInCache( string email )
        {
            logger.LogInformation("Retrieving supplier with Email: {SupplierEmail} from cache", email);

            return supplierStore.GetByEmail(email);
        }

        public SupplierDto? GetSupplierByPhoneInCache( string phone )
        {
            logger.LogInformation("Retrieving supplier with Phone: {SupplierPhone} from cache", phone);

            return supplierStore.GetByPhone(phone);
        }

        public IEnumerable<SupplierDto> GetAllActiveSuppliersInCache()
        {
            logger.LogInformation("Retrieving all active suppliers from cache");

            return supplierStore.GetActiveSuppliers();
        }

        public IEnumerable<SupplierDto> SearchSuppliersInCache( string searchTerm )
        {
            logger.LogInformation("Searching active categories in cache with term '{SearchTerm}'", searchTerm);

            return supplierStore.Search(searchTerm);
        }

        public bool SupplierExistsInCache( int supplierId ) => supplierStore.Exists(supplierId);

        public bool EmailExistsInCache( string email ) => supplierStore.EmailExists(email);

        public bool PhoneExistsInCache( string phone ) => supplierStore.PhoneExists(phone);

        public int GetSupplierCountInCache() => supplierStore.GetCount();

        public int GetActiveSupplierCountInCache() => supplierStore.GetActiveCount();

        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating supplier store cache refresh (active suppliers only)");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Gets only active suppliers from database
                    DatabaseResult<IEnumerable<Supplier>> result = await readService.GetsAllActiveSuppliersAsync();

                    if (result is { IsSuccess: true, Value: not null })
                    {
                        logger.LogInformation("Category store cache refreshed successfully with {Count} active suppliers", result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh supplier store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occured while refreshing suppliers store cache");
                }
            });
        }
    }
}
