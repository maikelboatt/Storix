using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Services.Categories
{
    public class CategoryCacheReadService( ICategoryStore categoryStore, ICategoryReadService categoryReadService, ILogger<CategoryCacheReadService> logger )
        :ICategoryCacheReadService
    {
        /// <summary>
        /// Search active records in cache (fast).
        /// </summary>
        /// <returns> Only non-deleted records.</returns>
        public IEnumerable<CategoryDto> SearchCategoryInCache( string? searchTerm )
        {
            logger.LogInformation("Searching active categories in cache with term '{SearchTerm}'", searchTerm);

            return categoryStore
                   .SearchCategories(searchTerm)
                   .Select(c => c.ToDto());
        }

        /// <summary>
        /// Gets a category by ID from cache (fast).
        /// </summary>
        public CategoryDto? GetCategoryByIdInCache( int categoryId )
        {
            logger.LogInformation("Retrieving category with ID: {CategoryId} from cache", categoryId);

            return categoryStore.GetById(categoryId);
        }

        /// <summary>
        /// Get a child category by the parent ID from cache.
        /// </summary>
        public IEnumerable<CategoryDto> GetSubCategoriesInCache( int parentCategoryId )
        {
            logger.LogInformation("Get all subcategories of Parent Category with ID: {ParentCategoryId}", parentCategoryId);

            return categoryStore.GetChildren(parentCategoryId);
        }

        /// <summary>
        /// Get all parent categories from cache.
        /// </summary>
        public IEnumerable<CategoryDto> GetRootCategoriesInCache()
        {
            logger.LogInformation("Retrieve all root categories");

            return categoryStore.GetRootCategories();
        }

        /// <summary>
        /// Gets all active categories from cache (fast).
        /// </summary>
        public IEnumerable<CategoryDto> GetAllActiveCategoriesInCache()
        {
            logger.LogInformation("Retrieves all active categories from cache");

            return categoryStore
                .GetActiveCategories();
        }

        /// <summary>
        /// Checks if a category exists in cache (active only).
        /// </summary>
        public bool CategoryExistsInCache( int categoryId ) => categoryStore.Exists(categoryId);

        /// <summary>
        /// Gets the count of all active categories in cache.
        /// </summary>
        public int GetCategoryActiveCountInCache() => categoryStore.GetActiveCount();

        /// <summary>
        /// Checks if a category has child categories in cache (active only).
        /// </summary>
        public bool CategoryHasSubCategoriesInCache( int categoryId ) => categoryStore.HasChildren(categoryId);

        /// <summary>
        /// Refreshes the category cache from the database.
        /// Loads only active products into memory.
        /// </summary>
        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating category store cache refresh (active categories only)");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Gets only active categories from database
                    DatabaseResult<IEnumerable<CategoryDto>> result = await categoryReadService.GetAllActiveCategoriesAsync();

                    if (result is { IsSuccess: true, Value: not null })
                    {
                        logger.LogInformation("Category store cache refreshed successfully with {Count} active categories", result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh category store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occured while refreshing categories store cache");
                }
            });
        }
    }
}
