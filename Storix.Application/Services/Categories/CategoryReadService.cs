using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Categories;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Services.Categories
{
    /// <summary>
    ///     Service responsible for category read operations with ISoftDeletable support.
    /// </summary>
    public class CategoryReadService(
        ICategoryRepository categoryRepository,
        ICategoryStore categoryStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<CategoryReadService> logger ):ICategoryReadService
    {
        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync( bool includeDeleted = false )
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetRootCategoriesAsync(includeDeleted),
                "Retrieving root categories"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {RootCategoryCount} root categories includeDeleted: {IncludeDeleted}",
                    result.Value.Count(),
                    includeDeleted);
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve root categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false )
        {
            if (parentCategoryId <= 0)
            {
                logger.LogWarning("Invalid parent category ID {ParentCategoryId} provided, includeDeleted: {IncludeDeleted}", parentCategoryId, includeDeleted);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(
                    "Parent category ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetSubCategoriesAsync(parentCategoryId, includeDeleted),
                $"Retrieving subcategories for parent category {parentCategoryId} with includeDeleted: {includeDeleted}"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {SubCategoryCount} subcategories for parent {ParentCategoryId}, includeDeleted: {IncludeDeleted}",
                    result.Value.Count(),
                    parentCategoryId,
                    includeDeleted);

                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning(
                "Failed to retrieve subcategories for parent {ParentCategoryId}: {ErrorMessage}",
                parentCategoryId,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetPagedAsync(pageNumber, pageSize, includeDeleted),
                $"Getting categories page {pageNumber} with size {pageSize} (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of categories ({CategoryCount} items, includeDeleted: {IncludeDeleted})",
                    pageNumber,
                    result.Value.Count(),
                    includeDeleted);

                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning(
                "Failed to retrieve categories page {PageNumber}: {ErrorMessage}",
                pageNumber,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalCategoryCountAsync( bool includeDeleted = false )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetTotalCountAsync(includeDeleted),
                $"Getting total category count (includeDeleted: {includeDeleted})",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total category count: {CategoryCount} (includeDeleted: {IncludeDeleted})", result.Value, includeDeleted);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public CategoryDto? GetCategoryById( int categoryId, bool includeDeleted = false )
        {
            if (categoryId <= 0)
            {
                logger.LogWarning("Invalid category ID {CategoryId} provided", categoryId);
                return null;
            }

            logger.LogDebug("Retrieving category with ID {CategoryId} from store", categoryId);
            CategoryDto? category = categoryStore.GetById(categoryId);
            return category;
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync( bool includeDeleted = false )
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetAllAsync(includeDeleted),
                "Retrieving all categories"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();

                if (!includeDeleted)
                    categoryStore.Initialize(result.Value.ToList());

                logger.LogInformation("Successfully loaded {CategoryCount} categories (includeDeleted: {IncludeDeleted}", result.Value.Count(), includeDeleted);

                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        // public async Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId, bool includeDeleted = false )
        // {
        //     if (categoryId <= 0)
        //     {
        //         logger.LogWarning("Invalid category ID {CategoryId} provided", categoryId);
        //         return DatabaseResult<bool>.Failure("Category ID must be a positive integer.", DatabaseErrorCode.InvalidInput);
        //     }
        //
        //     DatabaseResult<bool> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
        //         () => categoryRepository.ExistsAsync(categoryId, includeDeleted),
        //         $"Checking existence of category {categoryId} (includeDeleted: {includeDeleted})",
        //         false
        //     );
        //
        //     if (result.IsSuccess)
        //         logger.LogInformation("Category {CategoryId} exists: {Exists} (includeDeleted: {IncludeDeleted})", categoryId, result.Value, includeDeleted);
        //
        //     return result.IsSuccess
        //         ? DatabaseResult<bool>.Success(result.Value)
        //         : DatabaseResult<bool>.Failure(result.ErrorMessage!, result.ErrorCode);
        // }

        public async Task<DatabaseResult<int>> GetActiveCategoryCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                categoryRepository.GetActiveCountAsync,
                "Getting active category count",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Active category count: {CategoryCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetDeletedCategoryCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                categoryRepository.GetDeletedCountAsync,
                "Getting deleted category count",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Deleted category count: {CategoryCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllDeletedCategoriesAsync()
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                categoryRepository.GetAllDeletedAsync,
                "Retrieving all deleted categories"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {DeletedCategoryCount} deleted categories", result.Value.Count());
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve deleted categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync()
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                categoryRepository.GetAllActiveAsync,
                "Retrieving all active categories"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {ActiveCategoryCount} active categories", result.Value.Count());
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve active categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<CategoryDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.SearchAsync(searchTerm.Trim(), includeDeleted),
                $"Searching products with term '{searchTerm}' (includeDeleted: {includeDeleted})"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Search for '{SearchTerm}' returned {CategoryCount} categories (includeDeleted: {IncludeDeleted})",
                    searchTerm,
                    result.Value.Count(),
                    includeDeleted);
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to search categories with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
