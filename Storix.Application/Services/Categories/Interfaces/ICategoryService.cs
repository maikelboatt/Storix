using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Services.Categories.Interfaces
{
    /// <summary>
    ///     Interface for category service operations with enhanced error handling.
    /// </summary>
    public interface ICategoryService
    {
        #region Read Operations

        /// <summary>
        ///     Gets a category by its ID from the store (no database call needed).
        /// </summary>
        /// <param name="categoryId" >Unique Identifier</param>
        /// <returns>The category DTO or null if not found.</returns>
        CategoryDto? GetCategoryById( int categoryId );

        /// <summary>
        ///     Retrieves all categories from database and loads them into the store.
        /// </summary>
        /// <returns>A DatabaseResult containing all categories as DTOs.</returns>
        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();

        /// <summary>
        ///     Gets all root categories (categories without a parent).
        /// </summary>
        /// <returns>A DatabaseResult containing root categories.</returns>
        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetRootCategoriesAsync();

        /// <summary>
        ///     Gets all subcategories of a given parent category.
        /// </summary>
        /// <param name="parentCategoryId" >ID of the parent category</param>
        /// <returns>A DatabaseResult containing subcategories.</returns>
        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetSubCategoriesAsync( int parentCategoryId );

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new category with business validation.
        /// </summary>
        /// <param name="createCategoryDto" >The category creation record</param>
        /// <returns>A DatabaseResult containing the created category DTO</returns>
        Task<DatabaseResult<CategoryDto>> CreateCategoryAsync( CreateCategoryDto createCategoryDto );

        /// <summary>
        ///     Updates an existing category with business validation.
        /// </summary>
        /// <param name="updateCategoryDto" >The category update record.</param>
        /// <returns>A DatabaseResult containing the updated category DTO.</returns>
        Task<DatabaseResult<CategoryDto>> UpdateCategoryAsync( UpdateCategoryDto updateCategoryDto );

        /// <summary>
        ///     Permanently deletes a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique Identifier of the category.</param>
        /// <returns>A DatabaseResult indicating success or failure.</returns>
        Task<DatabaseResult> DeleteCategoryAsync( int categoryId );

        #endregion

        #region Validation

        /// <summary>
        ///     Checks if a category exists by ID (from store first, then database if needed).
        /// </summary>
        /// <param name="categoryId" >The category ID to check</param>
        /// <returns>A DatabaseResult containing true if exists, false otherwise.</returns>
        Task<DatabaseResult<bool>> CategoryExistsAsync( int categoryId );

        /// <summary>
        ///     Checks if a category name already exists (optionally excluding a specific category ID).
        /// </summary>
        /// <param name="name" >The category name to check</param>
        /// <param name="excludeCategoryId" >Optional category ID to exclude from the check</param>
        /// <returns>A DatabaseResult containing true if name exists, false otherwise.</returns>
        Task<DatabaseResult<bool>> CategoryNameExistsAsync( string name, int? excludeCategoryId = null );

        #endregion

        #region Pagination

        /// <summary>
        ///     Gets the total count of categories.
        /// </summary>
        /// <returns>A DatabaseResult containing the total count.</returns>
        Task<DatabaseResult<int>> GetTotalCategoryCountAsync();

        /// <summary>
        ///     Gets a paged list of categories.
        /// </summary>
        /// <param name="pageNumber" >The page number (1-based)</param>
        /// <param name="pageSize" >The number of items per page</param>
        /// <returns>A DatabaseResult containing the paged categories.</returns>
        Task<DatabaseResult<IEnumerable<CategoryDto>>> GetCategoryPagedAsync( int pageNumber, int pageSize );

        #endregion

        #region Store Operations

        /// <summary>
        ///     Searches categories by name or description from the store.
        /// </summary>
        /// <param name="searchTerm" >The search term to match against category name or description.</param>
        /// <param name="isActive" >Optional active status filter.</param>
        /// <returns>A collection of matching category DTOs.</returns>
        IEnumerable<CategoryDto> SearchCategories( string? searchTerm = null, bool? isActive = null );

        /// <summary>
        ///     Gets all active categories from the store.
        /// </summary>
        /// <returns>A collection of active category DTOs.</returns>
        IEnumerable<CategoryDto> GetActiveCategories();

        #endregion
    }
}
