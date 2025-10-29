using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers
{
    /// <summary>
    /// Service responsible for customer read operations with ISoftDeletable support.
    /// Returns both active and deleted records.
    /// Use CustomerCacheReadService to check only active records.
    /// </summary>
    public class CustomerReadService(
        ICustomerRepository customerRepository,
        ICustomerStore customerStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<CustomerReadService> logger ):ICustomerReadService
    {
        /// <summary>
        /// Gets a customer by ID.
        /// </summary>
        public async Task<DatabaseResult<CustomerDto>> GetCustomerByIdAsync( int customerId )
        {
            if (customerId <= 0)
            {
                logger.LogWarning("Invalid customer ID {CustomerId} provided", customerId);
                return DatabaseResult<CustomerDto>.Failure("Customer ID must be positive integer", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Customer?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.GetByIdAsync(customerId),
                $"Retrieving customer {customerId}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve customer {CustomerId}: {ErrorMessage}", customerId, result.ErrorMessage);
                return DatabaseResult<CustomerDto>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Customer with ID {CustomerId} not found", customerId);
                return DatabaseResult<CustomerDto>.Failure("Customer not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved customer with ID {CustomerId}", customerId);
            return DatabaseResult<CustomerDto>.Success(result.Value.ToDto());
        }

        /// <summary>
        /// Gets a customer by Email.
        /// </summary>
        public async Task<DatabaseResult<CustomerDto?>> GetCustomerByEmailAsync( string email )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Null or empty email provided");
                return DatabaseResult<CustomerDto?>.Failure(
                    "Email cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Customer?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.GetByEmailAsync(email),
                $"Retrieving customer by email {email}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve customer by Email {Email}: {ErrorMessage}", email, result.ErrorMessage);
                return DatabaseResult<CustomerDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Customer with Email {Email} not found", email);
                return DatabaseResult<CustomerDto?>.Failure("Customer not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved customer with Email {Email}", email);
            return DatabaseResult<CustomerDto?>.Success(result.Value.ToDto());
        }

        /// <summary>
        /// Gets a customer by Phone.
        /// </summary>
        public async Task<DatabaseResult<CustomerDto?>> GetCustomerByPhoneAsync( string phone )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                logger.LogWarning("Null or empty phone provided");
                return DatabaseResult<CustomerDto?>.Failure(
                    "Phone cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Customer?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.GetByPhoneAsync(phone),
                $"Retrieving customer by phone {phone}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve customer by Phone {Phone}: {ErrorMessage}", phone, result.ErrorMessage);
                return DatabaseResult<CustomerDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Customer with Phone {Phone} not found", phone);
                return DatabaseResult<CustomerDto?>.Failure("Customer not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved customer with Phone {Phone}", phone);
            return DatabaseResult<CustomerDto?>.Success(result.Value.ToDto());
        }

        /// <summary>
        /// Gets all customers in database.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> GetAllAsync()
        {
            DatabaseResult<IEnumerable<Customer>> result =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(customerRepository.GetAllAsync, "Retrieving all customers");

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve customers: {ErrorMessage}", result.ErrorMessage);
                return DatabaseResult<IEnumerable<CustomerDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No customers found");
                return DatabaseResult<IEnumerable<CustomerDto>>.Success([]);
            }

            logger.LogInformation("Successfully retrieve {SupplierCount} customers", result.Value.Count());
            return DatabaseResult<IEnumerable<CustomerDto>>.Success(result.Value.Select(s => s.ToDto()));
        }

        /// <summary>
        /// Retrieves all active (non-deleted) customers from persistence and initializes the in-memory store (cache) with them.
        /// </summary>
        /// <returns></returns>
        public async Task<DatabaseResult<IEnumerable<Customer>>> GetsAllActiveCustomersAsync()
        {
            return await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () =>
                {
                    // Fetch all customers from persistence.
                    IEnumerable<Customer> allCustomers = await customerRepository.GetAllAsync();

                    // Filter only active (non-deleted) customers.
                    List<Customer> activeCustomers = allCustomers
                                                     .Where(s => !s.IsDeleted)
                                                     .ToList();

                    // Initialize the in-memory store with active customers only.
                    customerStore.Initialize(activeCustomers);

                    logger.LogInformation("Successfully retrieved customers: {CustomerCount}", activeCustomers.Count);
                    return (IEnumerable<Customer>)activeCustomers;
                },
                "Retrieving active customers"
            );
        }

        /// <summary>
        /// Retrieves all deleted customers from persistence.
        /// </summary>
        /// <returns></returns>
        public async Task<DatabaseResult<IEnumerable<Customer>>> GetsAllDeletedCustomersAsync()
        {
            return await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () =>
                {
                    // Fetch all customers from persistence.
                    IEnumerable<Customer> allCustomers = await customerRepository.GetAllAsync();

                    // Filter only deleted customers.
                    List<Customer> deletedCustomers = allCustomers
                                                      .Where(s => s.IsDeleted)
                                                      .ToList();

                    logger.LogInformation("Successfully retrieved customers: {CustomerCount}", deletedCustomers.Count);
                    return (IEnumerable<Customer>)deletedCustomers;
                },
                "Retrieving deleted customers"
            );
        }

        /// <summary>
        /// Gets the total counts of customers in persistence.
        /// </summary>
        public async Task<DatabaseResult<int>> GetTotalCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                customerRepository.GetTotalCountAsync,
                "Getting total customer count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total customer count: {UserCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Gets the total number of active customers.
        /// </summary>
        public async Task<DatabaseResult<int>> GetActiveCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                customerRepository.GetActiveCountAsync,
                "Getting active customer count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Active customer count: {CustomerCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Gets the total number of deleted customers.
        /// </summary>
        public async Task<DatabaseResult<int>> GetDeletedCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                customerRepository.GetDeletedCountAsync,
                "Getting deleted customer count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Deleted customer count: {CustomerCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Searches customers with a search-term (email,phone,name,address etc...).
        /// </summary>
        /// <param name="searchTerm"></param>
        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> SearchAsync( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<CustomerDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Customer>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.SearchAsync(searchTerm.Trim()),
                $"Searching customers with term '{searchTerm}'"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to search customers with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
                return DatabaseResult<IEnumerable<CustomerDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No customers found for search term '{SearchTerm}'", searchTerm);
                return DatabaseResult<IEnumerable<CustomerDto>>.Success([]);
            }

            logger.LogInformation(
                "Search for '{SearchTerm}' returned {UserCount} customers",
                searchTerm,
                result.Value.Count());

            return DatabaseResult<IEnumerable<CustomerDto>>.Success(result.Value.Select(c => c.ToDto()));
        }

        /// <summary>
        /// Gets a paged list of customers.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<CustomerDto>>> GetPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<CustomerDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Customer>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting customers page {pageNumber} with size {pageSize}"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve customers page {PageNumber}: {ErrorMessage}", pageNumber, result.ErrorMessage);
                return DatabaseResult<IEnumerable<CustomerDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No customers found for page {PageNumber}", pageNumber);
                return DatabaseResult<IEnumerable<CustomerDto>>.Success(Enumerable.Empty<CustomerDto>());
            }

            logger.LogInformation(
                "Successfully retrieved page {PageNumber} of customers ({UserCount} items)",
                pageNumber,
                result.Value.Count());

            return DatabaseResult<IEnumerable<CustomerDto>>.Success(result.Value.Select(u => u.ToDto()));
        }
    }
}
