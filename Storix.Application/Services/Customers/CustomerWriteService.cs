using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Customers;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Stores.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers
{
    /// <summary>
    ///     Service responsible for customer write operations with ISoftDeletable support.
    /// </summary>
    public class CustomerWriteService(
        ICustomerRepository customerRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ICustomerValidationService customerValidationService,
        ICustomerStore customerStore,
        IValidator<CreateCustomerDto> createValidator,
        IValidator<UpdateCustomerDto> updateValidator,
        ILogger<CustomerWriteService> logger ):ICustomerWriteService
    {
        public async Task<DatabaseResult<CustomerDto>> CreateCustomerAsync( CreateCustomerDto createCustomerDto )
        {
            // Input validation
            DatabaseResult<CustomerDto> inputValidation = ValidateCreateInput(createCustomerDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<CustomerDto> businessValidation = await ValidateCreateBusiness(createCustomerDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create customer
            return await PerformCreate(createCustomerDto);
        }

        public async Task<DatabaseResult<CustomerDto>> UpdateCustomerAsync( UpdateCustomerDto updateCustomerDto )
        {
            // Input validation
            DatabaseResult<CustomerDto> inputValidation = ValidateUpdateInput(updateCustomerDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<CustomerDto> businessValidation = await ValidateUpdateBusiness(updateCustomerDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Perform update
            return await PerformUpdate(updateCustomerDto);
        }

        public async Task<DatabaseResult> SoftDeleteCustomerAsync( int customerId )
        {
            // Input validation
            if (customerId <= 0)
            {
                logger.LogWarning("Invalid customer ID {CustomerId} provided for soft deletion", customerId);
                return DatabaseResult.Failure("Customer ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await customerValidationService.ValidateForDeletion(customerId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform soft deletion
            return await PerformSoftDelete(customerId);
        }

        public async Task<DatabaseResult> RestoreCustomerAsync( int customerId )
        {
            // Input validation
            if (customerId <= 0)
            {
                logger.LogWarning("Invalid customer ID {CustomerId} provided for restoration", customerId);
                return DatabaseResult.Failure("Customer ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await customerValidationService.ValidateForRestore(customerId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform restoration
            return await PerformRestore(customerId);
        }

        public async Task<DatabaseResult> HardDeleteCustomerAsync( int customerId )
        {
            // Input validation
            if (customerId <= 0)
            {
                logger.LogWarning("Invalid customer ID {CustomerId} provided for hard deletion", customerId);
                return DatabaseResult.Failure("Customer ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await customerValidationService.ValidateForHardDeletion(customerId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform hard deletion
            return await PerformHardDelete(customerId);
        }

        #region Helper Methods

        private async Task<DatabaseResult<CustomerDto>> PerformCreate( CreateCustomerDto createCustomerDto )
        {
            // Convert DTO to domain model
            Customer customer = createCustomerDto.ToDomain();

            DatabaseResult<Customer> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.CreateAsync(customer),
                "Creating new customer"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                int customerId = result.Value.CustomerId;

                // Add to cache
                customerStore.Create(customerId, createCustomerDto);

                logger.LogInformation(
                    "Successfully created customer with ID {CustomerId} and name '{Name}'",
                    result.Value.CustomerId,
                    result.Value.Name);

                CustomerDto customerDto = result.Value.ToDto();

                return DatabaseResult<CustomerDto>.Success(customerDto);
            }

            logger.LogWarning("Failed to create customer: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<CustomerDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<CustomerDto>> PerformUpdate( UpdateCustomerDto updateCustomerDto )
        {
            // Get existing customer (only active ones - can't update deleted customers)
            DatabaseResult<Customer?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.GetByIdAsync(updateCustomerDto.CustomerId),
                $"Retrieving customer {updateCustomerDto.CustomerId} for update",
                enableRetry: false
            );

            if (!getResult.IsSuccess || getResult.Value == null)
            {
                logger.LogWarning(
                    "Cannot update customer {CustomerId}: {ErrorMessage}",
                    updateCustomerDto.CustomerId,
                    getResult.ErrorMessage ?? "Customer not found or is deleted");
                return DatabaseResult<CustomerDto>.Failure(
                    getResult.ErrorMessage ?? "Customer not found or is deleted. Restore the customer first if it was deleted.",
                    getResult.ErrorCode);
            }

            Customer existingCustomer = getResult.Value;

            // Update customer in cache
            customerStore.Update(updateCustomerDto);

            // Check if customer is soft-deleted
            if (existingCustomer.IsDeleted)
            {
                logger.LogWarning(
                    "Cannot update deleted customer {CustomerId}. Restore the customer first.",
                    updateCustomerDto.CustomerId);
                return DatabaseResult<CustomerDto>.Failure(
                    "Cannot update deleted customer. Restore the customer first.",
                    DatabaseErrorCode.InvalidInput);
            }

            // Update customer while preserving soft delete properties
            Customer updatedCustomer = updateCustomerDto.ToDomain(
                existingCustomer.IsDeleted,
                existingCustomer.DeletedAt);

            DatabaseResult<Customer> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => customerRepository.UpdateAsync(updatedCustomer),
                "Updating customer"
            );

            if (updateResult is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully updated customer with ID {CustomerId}", updateCustomerDto.CustomerId);
                return DatabaseResult<CustomerDto>.Success(updateResult.Value.ToDto());
            }

            logger.LogWarning(
                "Failed to update customer with ID {CustomerId}: {ErrorMessage}",
                updateCustomerDto.CustomerId,
                updateResult.ErrorMessage);
            return DatabaseResult<CustomerDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
        }

        private async Task<DatabaseResult> PerformSoftDelete( int customerId )
        {
            DatabaseResult result = await customerRepository.SoftDeleteAsync(customerId);

            if (result.IsSuccess)
            {
                // Updates in active cache
                bool removed = customerStore.Delete(customerId);

                if (!removed)
                    logger.LogWarning("Customer soft deleted in database (ID: {CustomerId} but wasn't found in active cache", customerId);
                else
                    logger.LogInformation("Successfully soft deleted customer with ID {CustomerId}", customerId);

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to soft delete customer with ID {CustomerId}: {ErrorMessage}", customerId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to soft delete customer",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int customerId )
        {
            DatabaseResult result = await customerRepository.RestoreAsync(customerId);

            if (result.IsSuccess)
            {
                // Fetch the restored customer from the database
                DatabaseResult<Customer?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => customerRepository.GetByIdAsync(customerId),
                    $"Retrieving restored customer {customerId}",
                    enableRetry: false);

                if (getResult is { IsSuccess: true, Value: not null })
                {
                    CreateCustomerDto createCustomerDto = new()
                    {

                        Address = getResult.Value.Address,
                        Email = getResult.Value.Email,
                        Name = getResult.Value.Name,
                        Phone = getResult.Value.Phone
                    };

                    CustomerDto? cached = customerStore.Create(getResult.Value.CustomerId, createCustomerDto);

                    if (cached != null)
                    {
                        logger.LogInformation("Successfully restored customer with ID {CustomerId} and added back to cache", customerId);
                    }
                    else
                    {
                        logger.LogWarning("Customer restored in database (ID: {CustomerId}) but failed to add to cache", customerId);
                    }

                }

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to restore customer with ID {CustomerId}: {ErrorMessage}", customerId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to restore customer",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int customerId )
        {
            DatabaseResult result = await customerRepository.HardDeleteAsync(customerId);

            if (result.IsSuccess)
            {
                // Updates in active cache
                bool removed = customerStore.Delete(customerId);

                if (!removed)
                    logger.LogWarning("Customer soft deleted in database (ID: {CustomerId} but wasn't found in active cache", customerId);
                else
                    logger.LogInformation("Successfully soft deleted customer with ID {CustomerId}", customerId);

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to hard delete customer with ID {CustomerId}: {ErrorMessage}", customerId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to hard delete customer",
                result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<CustomerDto> ValidateCreateInput( CreateCustomerDto? createCustomerDto )
        {
            if (createCustomerDto == null)
            {
                logger.LogWarning("Null CreateCustomerDto provided");
                return DatabaseResult<CustomerDto>.Failure(
                    "Customer data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = createValidator.Validate(createCustomerDto);

            if (validationResult.IsValid)
                return DatabaseResult<CustomerDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Customer creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<CustomerDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<CustomerDto>> ValidateCreateBusiness( CreateCustomerDto createCustomerDto )
        {
            // Check email availability if provided (excluding soft-deleted customers)
            if (!string.IsNullOrWhiteSpace(createCustomerDto.Email))
            {
                DatabaseResult<bool> emailExistsResult = await customerValidationService.EmailExistsAsync(
                    createCustomerDto.Email,
                    includeDeleted: false);

                if (!emailExistsResult.IsSuccess)
                    return DatabaseResult<CustomerDto>.Failure(
                        emailExistsResult.ErrorMessage!,
                        emailExistsResult.ErrorCode);

                if (emailExistsResult.Value)
                {
                    logger.LogWarning("Attempted to create customer with duplicate email: {Email}", createCustomerDto.Email);
                    return DatabaseResult<CustomerDto>.Failure(
                        $"A customer with the email '{createCustomerDto.Email}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            // Check phone availability if provided (excluding soft-deleted customers)
            if (!string.IsNullOrWhiteSpace(createCustomerDto.Phone))
            {
                DatabaseResult<bool> phoneExistsResult = await customerValidationService.PhoneExistsAsync(
                    createCustomerDto.Phone,
                    includeDeleted: false);

                if (!phoneExistsResult.IsSuccess)
                    return DatabaseResult<CustomerDto>.Failure(
                        phoneExistsResult.ErrorMessage!,
                        phoneExistsResult.ErrorCode);

                if (phoneExistsResult.Value)
                {
                    logger.LogWarning("Attempted to create customer with duplicate phone: {Phone}", createCustomerDto.Phone);
                    return DatabaseResult<CustomerDto>.Failure(
                        $"A customer with the phone number '{createCustomerDto.Phone}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            return DatabaseResult<CustomerDto>.Success(null!);
        }

        private DatabaseResult<CustomerDto> ValidateUpdateInput( UpdateCustomerDto? updateCustomerDto )
        {
            if (updateCustomerDto == null)
            {
                logger.LogWarning("Null UpdateCustomerDto provided");
                return DatabaseResult<CustomerDto>.Failure(
                    "Customer data cannot be null.",
                    DatabaseErrorCode.InvalidInput);
            }

            ValidationResult validationResult = updateValidator.Validate(updateCustomerDto);

            if (validationResult.IsValid)
                return DatabaseResult<CustomerDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(
                "Customer update validation failed for ID {CustomerId}: {ValidationErrors}",
                updateCustomerDto.CustomerId,
                errors);
            return DatabaseResult<CustomerDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<CustomerDto>> ValidateUpdateBusiness( UpdateCustomerDto updateCustomerDto )
        {
            // Check existence (only active customers can be updated)
            DatabaseResult<bool> existsResult = await customerValidationService.CustomerExistsAsync(
                updateCustomerDto.CustomerId,
                false);

            if (!existsResult.IsSuccess)
                return DatabaseResult<CustomerDto>.Failure(
                    existsResult.ErrorMessage!,
                    existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning(
                    "Attempted to update non-existent or deleted customer with ID {CustomerId}",
                    updateCustomerDto.CustomerId);
                return DatabaseResult<CustomerDto>.Failure(
                    $"Customer with ID {updateCustomerDto.CustomerId} not found or is deleted. " +
                    "Restore the customer first if it was deleted.",
                    DatabaseErrorCode.NotFound);
            }

            // Check email availability if provided (excluding this customer and soft-deleted customers)
            if (!string.IsNullOrWhiteSpace(updateCustomerDto.Email))
            {
                DatabaseResult<bool> emailExistsResult = await customerValidationService.EmailExistsAsync(
                    updateCustomerDto.Email,
                    updateCustomerDto.CustomerId,
                    false);

                if (!emailExistsResult.IsSuccess)
                    return DatabaseResult<CustomerDto>.Failure(
                        emailExistsResult.ErrorMessage!,
                        emailExistsResult.ErrorCode);

                if (emailExistsResult.Value)
                {
                    logger.LogWarning(
                        "Attempted to update customer {CustomerId} with duplicate email: {Email}",
                        updateCustomerDto.CustomerId,
                        updateCustomerDto.Email);
                    return DatabaseResult<CustomerDto>.Failure(
                        $"A customer with the email '{updateCustomerDto.Email}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            // Check phone availability if provided (excluding this customer and soft-deleted customers)
            if (!string.IsNullOrWhiteSpace(updateCustomerDto.Phone))
            {
                DatabaseResult<bool> phoneExistsResult = await customerValidationService.PhoneExistsAsync(
                    updateCustomerDto.Phone,
                    updateCustomerDto.CustomerId,
                    false);

                if (!phoneExistsResult.IsSuccess)
                    return DatabaseResult<CustomerDto>.Failure(
                        phoneExistsResult.ErrorMessage!,
                        phoneExistsResult.ErrorCode);

                if (phoneExistsResult.Value)
                {
                    logger.LogWarning(
                        "Attempted to update customer {CustomerId} with duplicate phone: {Phone}",
                        updateCustomerDto.CustomerId,
                        updateCustomerDto.Phone);
                    return DatabaseResult<CustomerDto>.Failure(
                        $"A customer with the phone number '{updateCustomerDto.Phone}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            return DatabaseResult<CustomerDto>.Success(null!);
        }

        #endregion
    }
}
