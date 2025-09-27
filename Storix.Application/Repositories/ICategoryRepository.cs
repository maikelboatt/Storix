using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface ICategoryRepository
    {
        /// <summary>
        ///     Checks if a category exists by ID.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories in the check</param>
        /// <returns></returns>
        Task<bool> ExistsAsync( int categoryId, bool includeDeleted = false );

        /// <summary>
        ///     Checks if a category name already exists, optionally excluding a specific category ID.
        /// </summary>
        /// <param name="name" >Name to be checked.</param>
        /// <param name="excludeCategoryId" >ID of category.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories in the check</param>
        /// <returns></returns>
        Task<bool> NameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false );

        /// <summary>
        ///     Gets the total counts of categories.
        /// </summary>
        /// <returns>Total count</returns>
        Task<int> GetTotalCountAsync( bool includeDeleted = false );

        /// <summary>
        ///     Gets the count of active categories (non-deleted).
        /// </summary>
        /// <returns></returns>
        Task<int> GetActiveCountAsync();

        /// <summary>
        ///     Gets the count of soft-deleted categories.
        /// </summary>
        /// <returns></returns>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        ///     Gets all active categories(IsActive = true and IsDeleted = false).
        /// </summary>
        Task<IEnumerable<Category>> GetAllActiveAsync();

        /// <summary>
        ///     Gets all soft-deleted categories.
        /// </summary>
        Task<IEnumerable<Category>> GetAllDeletedAsync();

        /// <summary>
        ///     Gets a paged list of categories.
        /// </summary>
        /// <param name="pageNumber" > Specified page number./</param>
        /// <param name="pageSize" > Specified page size.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories in the check</param>
        /// <returns></returns>
        Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false );

        /// <summary>
        ///     Get a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique Identifier.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories.</param>
        /// <returns></returns>
        Task<Category?> GetByIdAsync( int categoryId, bool includeDeleted = false );

        /// <summary>
        ///     Gets all categories.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Category>> GetAllAsync( bool includeDeleted = false );

        /// <summary>
        ///     Gets root categories (categories without a parent).
        /// </summary>
        /// <returns>Enumerable of root categories.</returns>
        /// <exception cref="NotImplementedException" ></exception>
        Task<IEnumerable<Category>> GetRootCategoriesAsync( bool includeDeleted = false );

        /// <summary>
        ///     Gets subcategories for a given parent category.
        /// </summary>
        /// <param name="parentCategoryId" >The ID of the parent category.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories.</param>
        /// <returns> Enumerable of subcategories</returns>
        /// <exception cref="NotImplementedException" ></exception>
        Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false );

        /// <summary>
        ///     Searches categories by name or description.
        /// </summary>
        /// <param name="searchTerm" >The search term.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories.</param>
        Task<IEnumerable<Category>> SearchAsync( string searchTerm, bool includeDeleted = false );

        /// <summary>
        ///     Creates a new category and returns it with the assigned CategoryId.
        /// </summary>
        /// <param name="category" ></param>
        /// <returns></returns>
        Task<Category> CreateAsync( Category category );

        /// <summary>
        ///     Updates an existing category.
        /// </summary>
        /// <param name="category" >New category record</param>
        /// <returns></returns>
        Task<Category> UpdateAsync( Category category );

        /// <summary>
        ///     Permanently deletes a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <returns></returns>
        Task<bool> HardDeleteAsync( int categoryId );

        /// <summary>
        ///     Soft deletes a category (sets IsDeleted = true and DeletedAt = current timestamp.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <returns></returns>
        Task<bool> SoftDeleteAsync( int categoryId );

        /// <summary>
        ///     Restores a soft-deleted category (sets IsDeleted = false and DeletedAt = null).
        /// </summary>
        /// <param name="categoryId" ></param>
        /// <returns></returns>
        Task<bool> RestoreAsync( int categoryId );
    }
}
