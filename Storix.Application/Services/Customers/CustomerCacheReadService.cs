using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Customers;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers
{
    public class CustomerCacheReadService( ICustomerStore customerStore, ICustomerReadService readService, ILogger<CustomerCacheReadService> logger )
        :ICustomerCacheReadService
    {
        public CustomerDto? GetCustomerByIdInCache( int customerId )
        {
            logger.LogInformation("Retrieving customer with ID: {CustomerId} from cache", customerId);

            return customerStore.GetById(customerId);
        }

        public CustomerDto? GetCustomerByEmailInCache( string email )
        {
            logger.LogInformation("Retrieving customer with Email: {CustomerEmail} from cache", email);

            return customerStore.GetByEmail(email);
        }

        public List<CustomerDto> GetAllCustomersInCache()
        {
            logger.LogInformation("Retrieving all customers from cache");

            return customerStore.GetAll();
        }

        public List<CustomerDto> GetAllActiveCustomersInCache()
        {
            logger.LogInformation("Retrieving all active customers from cache");

            return customerStore.GetActiveCustomers();
        }

        public CustomerDto? GetCustomerByPhoneInCache( string phone )
        {
            logger.LogInformation("Retrieving customer with Phone: {CustomerPhone} from cache", phone);

            return customerStore.GetByPhone(phone);
        }

        public IEnumerable<CustomerDto> SearchCustomersInCache( string searchTerm )
        {
            logger.LogInformation("Searching active categories in cache with term '{SearchTerm}'", searchTerm);

            return customerStore.Search(searchTerm);
        }

        public bool CustomerExistsInCache( int customerId ) => customerStore.Exists(customerId);

        public bool EmailExistsInCache( string email ) => customerStore.EmailExists(email);

        public bool PhoneExistsInCache( string phone ) => customerStore.PhoneExists(phone);

        public int GetCustomerCountInCache() => customerStore.GetCount();

        public int GetActiveCustomerCountInCache() => customerStore.GetActiveCount();

        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating customer store cache refresh (active customers only)");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Gets only active customers from database
                    DatabaseResult<IEnumerable<CustomerDto>> result = await readService.GetsAllActiveCustomersAsync();

                    if (result is { IsSuccess: true, Value: not null })
                    {
                        logger.LogInformation("Category store cache refreshed successfully with {Count} active customers", result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh customer store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occured while refreshing customers store cache");
                }
            });
        }
    }
}
