using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
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
    /// Service responsible for supplier write operations with ISoftDeletable support
    /// </summary>
    public class SupplierWriteService(
        ISupplierRepository supplierRepository,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ISupplierValidationService supplierValidationService,
        ISupplierStore supplierStore,
        IValidator<CreateSupplierDto> createValidator,
        IValidator<UpdateSupplierDto> updateValidator,
        ILogger<SupplierWriteService> logger ):ISupplierWriteService
    {
        public async Task<DatabaseResult<SupplierDto>> CreateSupplierAsync( CreateSupplierDto createSupplierDto )
        {
            // Input validation 
            DatabaseResult<SupplierDto> inputValidation = ValidateCreateInput(createSupplierDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation
            DatabaseResult<SupplierDto> businessValidation = await ValidateCreateBusiness(createSupplierDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Create Supplier
            return await PerformCreate(createSupplierDto);
        }

        public async Task<DatabaseResult<SupplierDto>> UpdateSupplierAsync( UpdateSupplierDto updateSupplierDto )
        {
            // Input validation
            DatabaseResult<SupplierDto> inputValidation = ValidateUpdateInput(updateSupplierDto);
            if (!inputValidation.IsSuccess)
                return inputValidation;

            // Business validation 
            DatabaseResult<SupplierDto> businessValidation = await ValidateUpdateBusiness(updateSupplierDto);
            if (!businessValidation.IsSuccess)
                return businessValidation;

            // Update Supplier
            return await PerformUpdateAsync(updateSupplierDto);
        }

        public async Task<DatabaseResult> SoftDeleteSupplierAsync( int supplierId )
        {
            // Input validation
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid suppler ID {SupplierId} provided for soft deletion", supplierId);
                return DatabaseResult.Failure("Suppler ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await supplierValidationService.ValidateForDeletion(supplierId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform soft deletion
            return await PerformSoftDelete(supplierId);
        }

        public async Task<DatabaseResult> RestoreSupplierAsync( int supplierId )
        {
            // Input validation
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid suppler ID {SupplierId} provided for restoration", supplierId);
                return DatabaseResult.Failure("Suppler ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await supplierValidationService.ValidateForRestore(supplierId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform restoration
            return await PerformRestore(supplierId);
        }

        public async Task<DatabaseResult> HardDeleteSupplierAsync( int supplierId )
        {
            // Input validation
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid suppler ID {SupplierId} provided for deletion", supplierId);
                return DatabaseResult.Failure("Suppler ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
            }

            // Business validation
            DatabaseResult validationResult = await supplierValidationService.ValidateForHardDeletion(supplierId);
            if (!validationResult.IsSuccess)
                return validationResult;

            // Perform  deletion
            return await PerformHardDelete(supplierId);
        }


        #region Helper Methods

        private async Task<DatabaseResult<SupplierDto>> PerformCreate( CreateSupplierDto createSupplierDto )
        {
            // Convert Dto to domain model.
            Supplier supplier = createSupplierDto.ToDomain();

            DatabaseResult<Supplier> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.CreateAsync(supplier),
                "Create new supplier");

            if (result is { IsSuccess: true, Value: not null })
            {
                int supplierId = result.Value.SupplierId;

                // Add to cache 
                supplierStore.Create(supplierId, createSupplierDto);

                logger.LogInformation("Successfully created supplier with ID {SupplierId} and name '{Name}", supplierId, result.Value.Name);

                SupplierDto supplierDto = result.Value.ToDto();

                return DatabaseResult<SupplierDto>.Success(supplierDto);
            }

            logger.LogWarning("Failed to create supplier: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<SupplierDto>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        private async Task<DatabaseResult<SupplierDto>> PerformUpdateAsync( UpdateSupplierDto updateSupplierDto )
        {
            // Get existing supplier (only active ones - can't update deleted suppliers)
            DatabaseResult<Supplier?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.GetByIdAsync(updateSupplierDto.SupplierId, false),
                $"Retrieving supplier {updateSupplierDto.SupplierId} for update");

            if (!getResult.IsSuccess || getResult.Value is null)
            {
                logger.LogWarning(
                    "Cannot update suppler {SupplierId}: {ErrorMessage}",
                    updateSupplierDto.SupplierId,
                    getResult.ErrorMessage ?? "Supplier not found or is deleted");
                return DatabaseResult<SupplierDto>.Failure(
                    getResult.ErrorMessage ?? "Supplier not found or is deleted. Restore the suppler first if it was deleted.",
                    getResult.ErrorCode);
            }

            Supplier updatedSupplier = getResult.Value with
            {
                Name = updateSupplierDto.Name,
                Email = updateSupplierDto.Email,
                Phone = updateSupplierDto.Phone,
                Address = updateSupplierDto.Address
            };

            DatabaseResult<Supplier> updateResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => supplierRepository.UpdateAsync(updatedSupplier),
                "Updating supplier");

            if (!updateResult.IsSuccess || updateResult.Value == null)
            {
                logger.LogWarning("Failed to update supplier with ID {SupplierId}: {ErrorMessage}", updateSupplierDto, updateResult.ErrorMessage);
                return DatabaseResult<SupplierDto>.Failure(updateResult.ErrorMessage!, updateResult.ErrorCode);
            }

            SupplierDto supplierDto = updateResult.Value.ToDto();
            SupplierDto? storeResult = supplierStore.Update(updateSupplierDto);

            if (storeResult == null)
            {
                logger.LogWarning(
                    "Supplier with ID {SupplierId} updated in database but failed to update in cache",
                    updateSupplierDto.SupplierId);
            }

            logger.LogInformation("Successfully updated supplier with ID {SupplierId}", updateResult.Value.SupplierId);
            return DatabaseResult<SupplierDto>.Success(updateResult.Value.ToDto());

        }

        private async Task<DatabaseResult> PerformSoftDelete( int supplierId )
        {
            DatabaseResult result = await supplierRepository.SoftDeleteAsync(supplierId);

            if (result.IsSuccess)
            {
                // Updates in active cache
                bool removed = supplierStore.Delete(supplierId);

                if (!removed)
                    logger.LogWarning("Supplier soft deleted in database (ID: {SupplierId} but wasn't found on active cache", supplierId);
                else
                    logger.LogInformation("Successfully soft deleted supplier with ID {SupplierId}", supplierId);

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to soft delete supplier with ID {CustomerId}: {ErrorMessage}", supplierId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to soft delete supplier",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformRestore( int supplierId )
        {
            DatabaseResult result = await supplierRepository.RestoreAsync(supplierId);

            if (result.IsSuccess)
            {
                // Fetch the restored supplier from the database
                DatabaseResult<Supplier?> getResult = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    () => supplierRepository.GetByIdAsync(supplierId),
                    $"Retrieving restored supplier {supplierId}");

                if (getResult is { IsSuccess: true, Value: not null })
                {
                    CreateSupplierDto createSupplierDto = new()
                    {
                        Email = getResult.Value.Email,
                        Address = getResult.Value.Address,
                        Name = getResult.Value.Name,
                        Phone = getResult.Value.Phone
                    };

                    SupplierDto? cached = supplierStore.Create(getResult.Value.SupplierId, createSupplierDto);

                    if (cached != null)
                        logger.LogInformation("Successfully restored supplier with ID {SupplierId} and added to cache", supplierId);
                    else
                        logger.LogWarning("Suppler restored in database (ID: {SupplierId} but failed to add to cache", supplierId);
                }

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to restore supplier with ID {CustomerId}: {ErrorMessage}", supplierId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to restore supplier",
                result.ErrorCode);
        }

        private async Task<DatabaseResult> PerformHardDelete( int supplierId )
        {
            DatabaseResult result = await supplierRepository.HardDeleteAsync(supplierId);

            if (result.IsSuccess)
            {
                // Updates in active cache
                bool removed = supplierStore.Delete(supplierId);

                if (!removed)
                    logger.LogWarning("Supplier deleted in database (ID: {SupplierId} but wasn't found on active cache", supplierId);
                else
                    logger.LogInformation("Successfully deleted supplier with ID {SupplierId}", supplierId);

                return DatabaseResult.Success();
            }

            logger.LogWarning("Failed to delete supplier with ID {CustomerId}: {ErrorMessage}", supplierId, result.ErrorMessage);
            return DatabaseResult.Failure(
                result.ErrorMessage ?? "Failed to delete supplier",
                result.ErrorCode);
        }

        #endregion

        #region Validation Methods

        private DatabaseResult<SupplierDto> ValidateCreateInput( CreateSupplierDto? createSupplierDto )
        {
            if (createSupplierDto == null)
            {
                logger.LogWarning("Null CreateSupplierDto provided");
                return DatabaseResult<SupplierDto>.Failure("Supplier data cannot be null", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = createValidator.Validate(createSupplierDto);

            if (validationResult.IsValid)
                return DatabaseResult<SupplierDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Supplier creation validation failed: {ValidationErrors}", errors);
            return DatabaseResult<SupplierDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<SupplierDto>> ValidateCreateBusiness( CreateSupplierDto createSupplierDto )
        {
            // Check email availability if provided (excluding soft-deleted suppliers)
            if (!string.IsNullOrWhiteSpace(createSupplierDto.Email))
            {
                DatabaseResult<bool> emailExistsResult = await supplierValidationService.EmailExistsAsync(createSupplierDto.Email, includeDeleted: false);

                if (!emailExistsResult.IsSuccess)
                    return DatabaseResult<SupplierDto>.Failure(emailExistsResult.ErrorMessage!, emailExistsResult.ErrorCode);

                if (emailExistsResult.Value)
                {
                    logger.LogWarning("Attempted to create supplier with duplicate email: {Email}", createSupplierDto.Email);
                    return DatabaseResult<SupplierDto>.Failure(
                        $"A supplier with the email '{createSupplierDto.Email}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            // Check phone availability if provided (exclude soft-deleted suppliers)
            if (!string.IsNullOrWhiteSpace(createSupplierDto.Phone))
            {
                DatabaseResult<bool> phoneExistsResult = await supplierValidationService.PhoneExistsAsync(createSupplierDto.Phone, includeDeleted: false);

                if (!phoneExistsResult.IsSuccess)
                    return DatabaseResult<SupplierDto>.Failure(phoneExistsResult.ErrorMessage!, phoneExistsResult.ErrorCode);

                if (phoneExistsResult.Value)
                {
                    logger.LogWarning("Attempted to create supplier with duplicate phone: {Phone}", createSupplierDto.Phone);
                    return DatabaseResult<SupplierDto>.Failure(
                        $"A supplier with phone '{createSupplierDto.Phone}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            return DatabaseResult<SupplierDto>.Success(null!);
        }

        private DatabaseResult<SupplierDto> ValidateUpdateInput( UpdateSupplierDto? updateSupplierDto )
        {
            if (updateSupplierDto == null)
            {
                logger.LogWarning("Null UpdateSupplierDto provided");
                return DatabaseResult<SupplierDto>.Failure("Supplier data cannot be null", DatabaseErrorCode.InvalidInput);
            }

            ValidationResult? validationResult = updateValidator.Validate(updateSupplierDto);

            if (validationResult.IsValid)
                return DatabaseResult<SupplierDto>.Success(null!);

            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Supplier update validation failed: {ValidationErrors}", errors);
            return DatabaseResult<SupplierDto>.Failure(
                $"Validation failed: {errors}",
                DatabaseErrorCode.ValidationFailure);
        }

        private async Task<DatabaseResult<SupplierDto>> ValidateUpdateBusiness( UpdateSupplierDto updateSupplierDto )
        {
            // Check existence (only active suppliers can be updated)
            DatabaseResult<bool> existsResult = await supplierValidationService.SupplierExistsAsync(updateSupplierDto.SupplierId, false);

            if (!existsResult.IsSuccess)
                return DatabaseResult<SupplierDto>.Failure(existsResult.ErrorMessage!, existsResult.ErrorCode);

            if (!existsResult.Value)
            {
                logger.LogWarning("Attempted to update non-existent or deleted supplier with ID {SupplerId}", updateSupplierDto.SupplierId);
                return DatabaseResult<SupplierDto>.Failure(
                    $"Supplier with ID {updateSupplierDto.SupplierId} not found or is deleted. " + "Restore the supplier first if it was deleted.",
                    DatabaseErrorCode.NotFound);
            }

            // Check email availability if provided (excluding soft-deleted suppliers)
            if (!string.IsNullOrWhiteSpace(updateSupplierDto.Email))
            {
                DatabaseResult<bool> emailExistsResult = await supplierValidationService.EmailExistsAsync(updateSupplierDto.Email, includeDeleted: false);

                if (!emailExistsResult.IsSuccess)
                    return DatabaseResult<SupplierDto>.Failure(emailExistsResult.ErrorMessage!, emailExistsResult.ErrorCode);

                if (emailExistsResult.Value)
                {
                    logger.LogWarning("Attempted to update supplier with duplicate email: {Email}", updateSupplierDto.Email);
                    return DatabaseResult<SupplierDto>.Failure(
                        $"A supplier with the email '{updateSupplierDto.Email}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            // Check phone availability if provided (exclude soft-deleted suppliers)
            if (!string.IsNullOrWhiteSpace(updateSupplierDto.Phone))
            {
                DatabaseResult<bool> phoneExistsResult = await supplierValidationService.PhoneExistsAsync(updateSupplierDto.Phone, includeDeleted: false);

                if (!phoneExistsResult.IsSuccess)
                    return DatabaseResult<SupplierDto>.Failure(phoneExistsResult.ErrorMessage!, phoneExistsResult.ErrorCode);

                if (phoneExistsResult.Value)
                {
                    logger.LogWarning("Attempted to update supplier with duplicate phone: {Phone}", updateSupplierDto.Phone);
                    return DatabaseResult<SupplierDto>.Failure(
                        $"A supplier with phone '{updateSupplierDto.Phone}' already exists.",
                        DatabaseErrorCode.DuplicateKey);
                }
            }

            return DatabaseResult<SupplierDto>.Success(null!);
        }

        #endregion
    }
}
