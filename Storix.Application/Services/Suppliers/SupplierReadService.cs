using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Stores.Suppliers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Suppliers
{
    /// <summary>
    /// Service responsible for supplier read operations with ISoftDeletable support.
    /// Returns both active and deleted records.
    /// Use SupplierCacheReadService to check only active records.
    /// </summary>
    public class SupplierReadService(
        ISupplierRepository supplierRepository,
        ISupplierStore supplierStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<SupplierReadService> logger ):ISupplierReadService
    {
        /// <summary>
        /// Gets supplier by ID.
        /// </summary>
        public async Task<DatabaseResult<SupplierDto?>> GetSupplierByIdAsync( int supplierId )
        {
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid supplier ID {SupplierId} provided", supplierId);
                return DatabaseResult<SupplierDto?>.Failure("Supplier ID must be positive integer", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Supplier?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.GetByIdAsync(supplierId),
                $"Retrieving supplier {supplierId}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve supplier {SupplierId}: {ErrorMessage}", supplierId, result.ErrorMessage);
                return DatabaseResult<SupplierDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Supplier with ID {SupplierId} not found", supplierId);
                return DatabaseResult<SupplierDto?>.Failure("Supplier not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved supplier with ID {SupplierId}", supplierId);
            return DatabaseResult<SupplierDto?>.Success(result.Value.ToDto());
        }

        /// <summary>
        /// Gets supplier by Email.
        /// </summary>
        public async Task<DatabaseResult<SupplierDto?>> GetSupplierByEmailAsync( string email )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Null or empty email provided");
                return DatabaseResult<SupplierDto?>.Failure(
                    "Email cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Supplier?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.GetByEmailAsync(email),
                $"Retrieving supplier by email {email}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve supplier by Email {Email}: {ErrorMessage}", email, result.ErrorMessage);
                return DatabaseResult<SupplierDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Supplier with Email {Email} not found", email);
                return DatabaseResult<SupplierDto?>.Failure("Supplier not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved supplier with Email {Email}", email);
            return DatabaseResult<SupplierDto?>.Success(result.Value.ToDto());
        }

        /// <summary>
        /// Gets a supplier by Phone.
        /// </summary>
        public async Task<DatabaseResult<SupplierDto?>> GetSupplierByPhoneAsync( string phone )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                logger.LogWarning("Null or empty phone provided");
                return DatabaseResult<SupplierDto?>.Failure(
                    "Phone cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Supplier?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.GetByPhoneAsync(phone),
                $"Retrieving supplier by phone {phone}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve supplier by Phone {Phone}: {ErrorMessage}", phone, result.ErrorMessage);
                return DatabaseResult<SupplierDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Supplier with Phone {Phone} not found", phone);
                return DatabaseResult<SupplierDto?>.Failure("Supplier not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved supplier with Phone {Phone}", phone);
            return DatabaseResult<SupplierDto?>.Success(result.Value.ToDto());
        }

        /// <summary>
        /// Gets all suppliers in database.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> GetAllAsync()
        {
            DatabaseResult<IEnumerable<Supplier>> result =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(supplierRepository.GetAllAsync, "Retrieving all suppliers");

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve suppliers: {ErrorMessage}", result.ErrorMessage);
                return DatabaseResult<IEnumerable<SupplierDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No suppliers found");
                return DatabaseResult<IEnumerable<SupplierDto>>.Success([]);
            }

            logger.LogInformation("Successfully retrieve {SupplierCount} suppliers", result.Value.Count());
            return DatabaseResult<IEnumerable<SupplierDto>>.Success(result.Value.Select(s => s.ToDto()));
        }

        /// <summary>
        /// Retrieves all active (non-deleted) suppliers from persistence
        /// and initializes the in-memory store (cache) with them.
        /// </summary>
        /// <returns></returns>
        public async Task<DatabaseResult<IEnumerable<Supplier>>> GetsAllActiveSuppliersAsync()
        {
            return await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () =>
                {
                    // Fetch all suppliers from persistence.
                    IEnumerable<Supplier> allSuppliers = await supplierRepository.GetAllAsync();

                    // Filter only active (non-deleted) suppliers.
                    List<Supplier> activeSuppliers = allSuppliers
                                                     .Where(s => !s.IsDeleted)
                                                     .ToList();

                    // Initialize the in-memory store with active suppliers only.
                    supplierStore.Initialize(activeSuppliers);

                    logger.LogInformation("Successfully retrieved suppliers: {SupplierCount}", activeSuppliers.Count);
                    return (IEnumerable<Supplier>)activeSuppliers;
                },
                "Retrieving active suppliers"
            );
        }

        /// <summary>
        /// Retrieves all deleted suppliers from persistence and initializes the in-memory store (cache) with them.
        /// </summary>
        /// <returns></returns>
        public async Task<DatabaseResult<IEnumerable<Supplier>>> GetsAllDeletedSuppliersAsync()
        {
            return await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                async () =>
                {
                    // Fetch all suppliers from persistence.
                    IEnumerable<Supplier> allSuppliers = await supplierRepository.GetAllAsync();

                    // Filter only deleted (non-deleted) suppliers.
                    List<Supplier> deletedSuppliers = allSuppliers
                                                      .Where(s => s.IsDeleted)
                                                      .ToList();

                    logger.LogInformation("Successfully retrieved suppliers: {SupplierCount}", deletedSuppliers.Count);
                    return (IEnumerable<Supplier>)deletedSuppliers;
                },
                "Retrieving deleted suppliers"
            );
        }

        /// <summary>
        /// Gets the total number of suppliers in persistence.
        /// </summary>
        public async Task<DatabaseResult<int>> GetTotalCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                supplierRepository.GetTotalCountAsync,
                "Getting total supplier count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total supplier count: {Supplier}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Gets the total number of active suppliers.
        /// </summary>
        public async Task<DatabaseResult<int>> GetActiveCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                supplierRepository.GetActiveCountAsync,
                "Getting active supplier count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Active supplier count: {SupplierCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Gets the total number of deleted suppliers.
        /// </summary>
        public async Task<DatabaseResult<int>> GetDeletedCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                supplierRepository.GetDeletedCountAsync,
                "Getting deleted supplier count",
                enableRetry: false
            );

            if (result.IsSuccess)
                logger.LogInformation("Deleted supplier count: {SupplierCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        /// <summary>
        /// Searches suppliers with a search-term (email,phone,name,address etc...).
        /// </summary>
        /// <param name="searchTerm"></param>
        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> SearchAsync( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<SupplierDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Supplier>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.SearchAsync(searchTerm.Trim()),
                $"Searching suppliers with term '{searchTerm}'"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to search suppliers with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
                return DatabaseResult<IEnumerable<SupplierDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No suppliers found for search term '{SearchTerm}'", searchTerm);
                return DatabaseResult<IEnumerable<SupplierDto>>.Success([]);
            }

            logger.LogInformation(
                "Search for '{SearchTerm}' returned {UserCount} suppliers",
                searchTerm,
                result.Value.Count());

            return DatabaseResult<IEnumerable<SupplierDto>>.Success(result.Value.Select(s => s.ToDto()));
        }

        /// <summary>
        /// Gets a paged list of suppliers.
        /// </summary>
        public async Task<DatabaseResult<IEnumerable<SupplierDto>>> GetPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<SupplierDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Supplier>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting suppliers page {pageNumber} with size {pageSize}"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve suppliers page {PageNumber}: {ErrorMessage}", pageNumber, result.ErrorMessage);
                return DatabaseResult<IEnumerable<SupplierDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("No suppliers found for page {PageNumber}", pageNumber);
                return DatabaseResult<IEnumerable<SupplierDto>>.Success(Enumerable.Empty<SupplierDto>());
            }

            logger.LogInformation(
                "Successfully retrieved page {PageNumber} of suppliers ({UserCount} items)",
                pageNumber,
                result.Value.Count());

            return DatabaseResult<IEnumerable<SupplierDto>>.Success(result.Value.Select(u => u.ToDto()));
        }
    }
}
