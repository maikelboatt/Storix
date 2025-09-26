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
    ///     Service responsible for category read operations.
    /// </summary>
    public class CategoryReadService(
        ICategoryRepository categoryRepository,
        ICategoryStore categoryStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<CategoryReadService> logger ):ICategoryReadService
    {
        public CategoryDto? GetCategoryById( int categoryId )
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

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync()
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetAllAsync(),
                "Retrieving all categories"
            );

            if (result.IsSuccess && result.Value != null)
            {
                categoryStore.Initialize(result.Value);
                logger.LogInformation("Successfully loaded {CategoryCount} categories", result.Value.Count());

                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync()
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetRootCategoriesAsync(),
                "Retrieving root categories"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {RootCategoryCount} root categories", result.Value.Count());
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve root categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId )
        {
            if (parentCategoryId <= 0)
            {
                logger.LogWarning("Invalid parent category ID {ParentCategoryId} provided", parentCategoryId);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(
                    "Parent category ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetSubCategoriesAsync(parentCategoryId),
                $"Retrieving subcategories for parent category {parentCategoryId}"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved {SubCategoryCount} subcategories for parent {ParentCategoryId}",
                    result.Value.Count(),
                    parentCategoryId);
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning(
                "Failed to retrieve subcategories for parent {ParentCategoryId}: {ErrorMessage}",
                parentCategoryId,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0 ? "Page number must be positive" : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting categories page {pageNumber} with size {pageSize}"
            );

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of categories ({CategoryCount} items)",
                    pageNumber,
                    result.Value.Count());
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning(
                "Failed to retrieve categories page {PageNumber}: {ErrorMessage}",
                pageNumber,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalCategoryCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetTotalCountAsync(),
                "Getting total category count",
                false
            );

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
