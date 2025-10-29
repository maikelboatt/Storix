using System.Collections.Generic;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    public interface ICategoryCacheReadService
    {
        /// <summary>
        /// Search active records in cache (fast).
        /// </summary>
        /// <returns> Only non-deleted records.</returns>
        IEnumerable<CategoryDto> SearchCategoryInCache( string? searchTerm );

        /// <summary>
        /// Gets a category by ID from cache (fast).
        /// </summary>
        CategoryDto? GetCategoryByIdInCache( int categoryId );

        /// <summary>
        /// Get a child category by the parent ID from cache.
        /// </summary>
        IEnumerable<CategoryDto> GetSubCategoriesInCache( int parentCategoryId );

        /// <summary>
        /// Get all parent categories from cache.
        /// </summary>
        IEnumerable<CategoryDto> GetRootCategoriesInCache();

        /// <summary>
        /// Gets all active categories from cache (fast).
        /// </summary>
        IEnumerable<CategoryDto> GetAllActiveCategoriesInCache();

        /// <summary>
        /// Checks if a category exists in cache (active only).
        /// </summary>
        bool CategoryExistsInCache( int categoryId );

        /// <summary>
        /// Gets the count of all active categories in cache.
        /// </summary>
        int GetCategoryActiveCountInCache();

        /// <summary>
        /// Checks if a category has child categories in cache (active only).
        /// </summary>
        bool CategoryHasSubCategoriesInCache( int categoryId );

        /// <summary>
        /// Refreshes the category cache from the database.
        /// Loads only active products into memory.
        /// </summary>
        void RefreshStoreCache();
    }
}
