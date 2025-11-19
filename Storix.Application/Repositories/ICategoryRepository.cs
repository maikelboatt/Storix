using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface ICategoryRepository
    {
        /// <summary>
        ///     Checks if a category exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int categoryId, bool includeDeleted = false );

        /// <summary>
        ///     Checks if a category name already exists, optionally excluding a specific category ID.
        /// </summary>
        Task<bool> NameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false );

        /// <summary>
        ///     Gets the total count of all categories (active + deleted).
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of active categories only.
        /// </summary>
        Task<int> GetActiveCountAsync();

        /// <summary>
        ///     Gets the count of soft-deleted categories only.
        /// </summary>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        ///     Gets a category by its ID.
        ///     Use CategoryStore.GetById() for fast active-only access.
        /// </summary>
        Task<Category?> GetByIdAsync( int categoryId, bool includeDeleted = true );

        /// <summary>
        ///     Gets all categories (active + deleted).
        ///     Service layer filters for active/deleted. Use CategoryStore.GetAll() for fast active-only access.
        /// </summary>
        Task<IEnumerable<Category>> GetAllAsync( bool includeDeleted = false );

        /// <summary>
        ///     Gets root categories (active + deleted).
        ///     Service layer filters if needed. Use CategoryStore.GetRootCategories() for fast active-only access.
        /// </summary>
        Task<IEnumerable<Category>> GetRootCategoriesAsync();

        /// <summary>
        ///     Gets subcategories for a given parent (active + deleted).
        ///     Service layer filters if needed.
        /// </summary>
        Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId );

        /// <summary>
        ///     Gets a paged list of categories (active + deleted).
        ///     Service layer filters if needed.
        /// </summary>
        Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches categories by name or description (active + deleted).
        ///     Service layer filters if needed.
        /// </summary>
        Task<IEnumerable<Category>> SearchAsync( string searchTerm );

        /// <summary>
        ///     Creates a new category and returns it with the assigned CategoryId.
        /// </summary>
        Task<Category> CreateAsync( Category category );

        /// <summary>
        ///     Updates an existing category.
        /// </summary>
        Task<Category> UpdateAsync( Category category );

        /// <summary>
        ///     Soft deletes a category (sets IsDeleted = true and DeletedAt = current timestamp).
        /// </summary>
        Task<DatabaseResult> SoftDeleteAsync( int categoryId );

        /// <summary>
        ///     Restores a soft-deleted category (sets IsDeleted = false and DeletedAt = null).
        /// </summary>
        Task<DatabaseResult> RestoreAsync( int categoryId );

        /// <summary>
        ///     Permanently deletes a category by its ID.
        /// </summary>
        Task<DatabaseResult> HardDeleteAsync( int categoryId );
    }
}
