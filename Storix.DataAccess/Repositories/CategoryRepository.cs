using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    public class CategoryRepository( ISqlDataAccess sqlDataAccess ):ICategoryRepository
    {
        #region Validation

        /// <summary>
        ///     Checks if a category exists by ID.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories in the check</param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync( int categoryId, bool includeDeleted = false )
        {
            var parameters = new
            {
                CategoryId = categoryId,
                IncludeDeleted = includeDeleted
            };
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckCategoryExists",
                parameters
            );
            return count > 0;
        }

        /// <summary>
        ///     Checks if a category name already exists, optionally excluding a specific category ID.
        /// </summary>
        /// <param name="name" >Name to be checked.</param>
        /// <param name="excludeCategoryId" >ID of category.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories in the check</param>
        /// <returns></returns>
        public async Task<bool> NameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false )
        {
            var parameters = new
            {
                Name = name,
                ExcludeCategoryId = excludeCategoryId,
                IncludeDeleted = includeDeleted
            };

            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckCategoryNameExists",
                parameters
            );
            return count > 0;
        }

        #endregion

        #region Pagination

        /// <summary>
        ///     Gets the total counts of categories.
        /// </summary>
        /// <returns>Total count</returns>
        public async Task<int> GetTotalCountAsync( bool includeDeleted = false )
        {
            string storedProcedure = includeDeleted ? "sp_GetCategoryCountIncludeDeleted" : "sp_GetCategoryCount";

            return await sqlDataAccess.ExecuteScalarAsync<int>(storedProcedure);
        }

        /// <summary>
        ///     Gets the count of active categories (non-deleted).
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetActiveCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetActiveCategoryCount");

        /// <summary>
        ///     Gets the count of soft-deleted categories.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetDeletedCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetDeletedCategoryCount");

        /// <summary>
        ///     Gets all active categories(IsActive = true and IsDeleted = false).
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllActiveAsync() => await sqlDataAccess.QueryAsync<Category>("sp_GetAllActiveCategories");

        /// <summary>
        ///     Gets all soft-deleted categories.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetAllDeletedAsync() => await sqlDataAccess.QueryAsync<Category>("sp_GetAllDeletedCategories");

        /// <summary>
        ///     Gets a paged list of categories.
        /// </summary>
        /// <param name="pageNumber" > Specified page number./</param>
        /// <param name="pageSize" > Specified page size.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories in the check</param>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false )
        {
            var parameters = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize,
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_GetCategoriesPagedIncludeDeleted" : "sp_GetCategoriesPaged";

            return await sqlDataAccess.QueryAsync<Category>(storedProcedure, parameters);
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Get a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique Identifier.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories.</param>
        /// <returns></returns>
        public async Task<Category?> GetByIdAsync( int categoryId, bool includeDeleted = false )
        {
            var parameters = new
            {
                CategoryId = categoryId,
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_GetCategoryByIdIncludeDeleted" : "sp_GetCategoryById";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Category>(
                storedProcedure,
                parameters);
        }

        /// <summary>
        ///     Gets all categories.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetAllAsync( bool includeDeleted = false )
        {
            var parameters = new
            {
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_GetAllCategoriesIncludeDeleted" : "sp_GetAllCategories";
            return await sqlDataAccess.QueryAsync<Category>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Gets root categories (categories without a parent).
        /// </summary>
        /// <returns>Enumerable of root categories.</returns>
        /// <exception cref="NotImplementedException" ></exception>
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync( bool includeDeleted = false )
        {
            var parameters = new
            {
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_GetRootCategoriesIncludeDeleted" : "sp_GetRootCategories";
            return await sqlDataAccess.QueryAsync<Category>(storedProcedure, parameters);
        }

        /// <summary>
        ///     Gets subcategories for a given parent category.
        /// </summary>
        /// <param name="parentCategoryId" >The ID of the parent category.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories.</param>
        /// <returns> Enumerable of subcategories</returns>
        /// <exception cref="NotImplementedException" ></exception>
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false )
        {
            var parameters = new
            {
                ParentCategoryId = parentCategoryId,
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_GetSubCategoriesIncludeDeleted" : "sp_GetSubCategories";
            return await sqlDataAccess.QueryAsync<Category>(
                storedProcedure,
                parameters
            );
        }

        /// <summary>
        ///     Searches categories by name or description.
        /// </summary>
        /// <param name="searchTerm" >The search term.</param>
        /// <param name="includeDeleted" >Whether to include soft-deleted categories.</param>
        public async Task<IEnumerable<Category>> SearchAsync( string searchTerm, bool includeDeleted = false )
        {
            var parameters = new
            {
                SearchTerm = $"%{searchTerm}%",
                IncludeDeleted = includeDeleted
            };

            string storedProcedure = includeDeleted ? "sp_SearchCategoriesIncludeDeleted" : "sp_SearchCategories";
            return await sqlDataAccess.QueryAsync<Category>(storedProcedure, parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new category and returns it with the assigned CategoryId.
        /// </summary>
        /// <param name="category" ></param>
        /// <returns></returns>
        public async Task<Category> CreateAsync( Category category )
        {
            var parameters = new
            {
                category.Name,
                category.Description,
                category.ParentCategoryId,
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            };

            int newCategoryId = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CreateCategory",
                parameters);

            return category with { CategoryId = newCategoryId, IsDeleted = false, DeletedAt = null };
        }

        /// <summary>
        ///     Updates an existing category.
        /// </summary>
        /// <param name="category" >New category record</param>
        /// <returns></returns>
        public async Task<Category> UpdateAsync( Category category )
        {
            var parameters = new
            {
                category.CategoryId,
                category.Name,
                category.Description,
                category.ParentCategoryId,
                category.IsDeleted,
                category.DeletedAt
            };

            await sqlDataAccess.CommandAsync("sp_UpdateCategory", parameters);
            return category;
        }


        /// <summary>
        ///     Permanently deletes a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <returns></returns>
        public async Task<bool> HardDeleteAsync( int categoryId )
        {
            int affectedRow = await sqlDataAccess.ExecuteAsync(
                "sp_HardDeleteCategory",
                new { CategoryId = categoryId });

            return affectedRow > 0;
        }

        /// <summary>
        ///     Soft deletes a category (sets IsDeleted = true and DeletedAt = current timestamp.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <returns></returns>
        public async Task<bool> SoftDeleteAsync( int categoryId )
        {
            var parameters = new
            {
                CategoryId = categoryId,
                DeletedAt = DateTime.UtcNow
            };

            int affectedRow = await sqlDataAccess.ExecuteAsync(
                "sp_SoftDeleteCategory",
                parameters);

            return affectedRow > 0;
        }

        /// <summary>
        ///     Restores a soft-deleted category (sets IsDeleted = false and DeletedAt = null).
        /// </summary>
        /// <param name="categoryId" ></param>
        /// <returns></returns>
        public async Task<bool> RestoreAsync( int categoryId )
        {
            var parameters = new
            {
                CategoryId = categoryId
            };

            int affectedRow = await sqlDataAccess.ExecuteAsync(
                "sp_RestoreCategory",
                parameters);

            return affectedRow > 0;
        }

        #endregion
    }
}
