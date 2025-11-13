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
        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync()
        {
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                categoryRepository.GetRootCategoriesAsync,
                "Retrieving root categories"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {RootCategoryCount} root categories.",
                    result.Value.Count());
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
                logger.LogWarning("Invalid parent category ID {ParentCategoryId} provided.", parentCategoryId);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(
                    "Parent category ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetSubCategoriesAsync(parentCategoryId),
                $"Retrieving subcategories for parent category {parentCategoryId}."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {SubCategoryCount} subcategories for parent {ParentCategoryId}.",
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
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<CategoryDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting categories page {pageNumber} with size {pageSize}.)"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of categories ({CategoryCount} items.)",
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
                categoryRepository.GetTotalCountAsync,
                $"Getting total category count.",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total category count: {CategoryCount}.)", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

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
                () =>
                    categoryRepository.GetAllAsync(true),
                "Retrieving all categories"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();


                logger.LogInformation("Successfully loaded {CategoryCount} categories.", result.Value.Count());

                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }


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
            DatabaseResult<IEnumerable<CategoryDto>> result = await GetAllCategoriesAsync();

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<Category> enumerable = result.Value.Select(c => c.ToDomain());
                IEnumerable<Category> deleted = enumerable
                                                .Where(c => c.IsDeleted)
                                                .ToList();

                logger.LogInformation("Successfully retrieved {DeletedCategoryCount} deleted categories", deleted.Count());
                IEnumerable<CategoryDto> categoryDtos = deleted.ToDto();

                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve deleted categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllActiveCategoriesAsync()
        {
            // Now fetches only active categories from database
            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.GetAllAsync(false), // Filter in SQL
                "Retrieving active categories"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                List<Category> active = result.Value.ToList();
                IEnumerable<CategoryDto> categoryDtos = active.ToDto();


                logger.LogInformation("Successfully retrieved {ActiveCategoryCount} active categories", active.Count);
                categoryStore.Initialize(active);

                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to retrieve active categories: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryListDto>>> GetAllActiveCategoriesForListAsync()
        {
            DatabaseResult<IEnumerable<CategoryDto>> result = await GetAllActiveCategoriesAsync();

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<CategoryListDto> categoryListDtos = result
                                                                .Value
                                                                .Select(dto => new CategoryListDto
                                                                {
                                                                    CategoryId = dto.CategoryId,
                                                                    Name = dto.Name,
                                                                    Description = dto.Description,
                                                                    ParentCategory = categoryStore.GetCategoryName(dto.ParentCategoryId ?? 0),
                                                                    ImageUrl = dto.ImageUrl
                                                                });


                IEnumerable<CategoryListDto> dtos = categoryListDtos.ToList();
                categoryStore.InitializeCategoryList(dtos.ToList());

                logger.LogInformation("Successfully mapped {CategoryCount} active categories to CategoryListDTO", dtos.Count());

                return DatabaseResult<IEnumerable<CategoryListDto>>.Success(dtos);
            }

            logger.LogWarning("Failed to retrieve active categories for list: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryListDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<CategoryDto>>> SearchAsync( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<CategoryDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Category>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => categoryRepository.SearchAsync(searchTerm.Trim()),
                $"Searching products with term '{searchTerm}'."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Search for '{SearchTerm}' returned {CategoryCount} categories.",
                    searchTerm,
                    result.Value.Count());
                IEnumerable<CategoryDto> categoryDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<CategoryDto>>.Success(categoryDtos);
            }

            logger.LogWarning("Failed to search categories with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
            return DatabaseResult<IEnumerable<CategoryDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
