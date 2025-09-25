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
        /// <returns></returns>
        public async Task<bool> ExistsAsync( int categoryId )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckCategoryExists",
                new { CategoryId = categoryId }
            );
            return count > 0;
        }

        /// <summary>
        ///     Checks if a category name already exists, optionally excluding a specific category ID.
        /// </summary>
        /// <param name="name" >Name to be checked.</param>
        /// <param name="excludeCategoryId" >ID of category.</param>
        /// <returns></returns>
        public async Task<bool> NameExistsAsync( string name, int? excludeCategoryId = null )
        {
            int count = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CheckCategoryNameExists",
                new { Name = name, ExcludeCategoryId = excludeCategoryId }
            );
            return count > 0;
        }

        #endregion

        #region Pagination

        /// <summary>
        ///     Gets the total counts of categories.
        /// </summary>
        /// <returns>Total count</returns>
        public async Task<int> GetTotalCountAsync() => await sqlDataAccess.ExecuteScalarAsync<int>("sp_GetProductCount");

        /// <summary>
        ///     Gets a paged list of categories.
        /// </summary>
        /// <param name="pageNumber" > Specified page number./</param>
        /// <param name="pageSize" > Specified page size.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize )
        {
            var parameters = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize
            };

            return await sqlDataAccess.QueryAsync<Category>("sp_GetCategoriesPaged", parameters);
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Get a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique Identifier.</param>
        /// <returns></returns>
        public async Task<Category?> GetByIdAsync( int categoryId ) => await sqlDataAccess.QuerySingleOrDefaultAsync<Category>(
            "sp_GetCategoryById",
            new { CategoryId = categoryId }
        );

        /// <summary>
        ///     Gets all categories.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetAllAsync() => await sqlDataAccess.QueryAsync<Category>("sp_GetAllCategories");

        /// <summary>
        ///     Gets root categories (categories without a parent).
        /// </summary>
        /// <returns>Enumerable of root categories.</returns>
        /// <exception cref="NotImplementedException" ></exception>
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync() => await sqlDataAccess.QueryAsync<Category>("sp_GetRootCategories");

        /// <summary>
        ///     Gets subcategories for a given parent category.
        /// </summary>
        /// <param name="parentCategoryId" >The ID of the parent category.</param>
        /// <returns> Enumerable of subcategories</returns>
        /// <exception cref="NotImplementedException" ></exception>
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId ) => await sqlDataAccess.QueryAsync<Category>(
            "sp_GetSubCategories",
            new { ParentCategoryId = parentCategoryId }
        );

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
                category.ParentCategoryId
            };

            int newCategoryId = await sqlDataAccess.ExecuteScalarAsync<int>(
                "sp_CreateCategory",
                parameters);

            return category with { CategoryId = newCategoryId };
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
                category.ParentCategoryId
            };

            await sqlDataAccess.CommandAsync("sp_UpdateCategory", parameters);
            return category;
        }


        /// <summary>
        ///     Permanently deletes a category by its ID.
        /// </summary>
        /// <param name="categoryId" >Unique identifier</param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync( int categoryId )
        {

            int affectedRow = await sqlDataAccess.ExecuteAsync(
                "sp_DeleteCategory",
                new { CategoryId = categoryId });

            return affectedRow > 0;
        }

        #endregion
    }
}
