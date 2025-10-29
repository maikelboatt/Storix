using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;

namespace Storix.DataAccess.Repositories
{
    /// <summary>
    ///Repository implementation for <see cref="Category"/> entity operations.
    /// 
    ///     Provides direct data access for category data
    ///     All Get methods return complete data (active + deleted records).
    ///     Service layer is responsible for filtering.
    ///     Use CategoryStore for fast active-only access.
    /// </summary>
    public class CategoryRepository( ISqlDataAccess sqlDataAccess ):ICategoryRepository
    {
        #region Validation (Only methods with includeDeleted parameter)

        /// <summary>
        ///     Checks if a category exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int categoryId, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Category WHERE CategoryId = @CategoryId"
                // language=tsql
                : "SELECT COUNT(1) FROM Category WHERE CategoryId = @CategoryId AND IsDeleted = 0";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    CategoryId = categoryId
                });
        }

        /// <summary>
        ///     Checks if a category name already exists, optionally excluding a specific category ID.
        /// </summary>
        public async Task<bool> NameExistsAsync( string name, int? excludeCategoryId = null, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? @"SELECT COUNT(1) FROM Category 
                    WHERE Name = @Name 
                    AND (@ExcludeCategoryId IS NULL OR CategoryId != @ExcludeCategoryId)"
                // language=tsql
                : @"SELECT COUNT(1) FROM Category 
                    WHERE Name = @Name 
                    AND IsDeleted = 0 
                    AND (@ExcludeCategoryId IS NULL OR CategoryId != @ExcludeCategoryId)";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    Name = name,
                    ExcludeCategoryId = excludeCategoryId
                });
        }

        #endregion

        #region Count Operations (Explicit methods for different counts)

        /// <summary>
        ///     Gets the total count of all categories (active + deleted).
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Category";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of active categories only.
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Category WHERE IsDeleted = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of soft-deleted categories only.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Category WHERE IsDeleted = 1";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        #endregion

        #region Read Operations (Always returns ALL records - active + deleted)

        /// <summary>
        ///     Gets a category by its ID (returns active or deleted).
        ///     Use CategoryStore.GetById() for fast active-only access.
        /// </summary>
        public async Task<Category?> GetByIdAsync( int categoryId )
        {
            // language=tsql
            const string sql = "SELECT * FROM Category WHERE CategoryId = @CategoryId";
            return await sqlDataAccess.QuerySingleOrDefaultAsync<Category>(
                sql,
                new
                {
                    CategoryId = categoryId
                });
        }

        /// <summary>
        ///     Gets all categories (active + deleted).
        ///     Service layer filters for active/deleted. Use CategoryStore.GetAll() for fast active-only access.
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Category ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Category>(sql);
        }

        /// <summary>
        ///     Gets root categories (active + deleted).
        ///     Service layer filters if needed. Use CategoryStore.GetRootCategories() for fast active-only access.
        /// </summary>
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
        {
            // language=tsql
            const string sql = "SELECT * FROM Category WHERE ParentCategoryId IS NULL ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Category>(sql);
        }

        /// <summary>
        ///     Gets subcategories for a given parent (active + deleted).
        ///     Service layer filters if needed.
        /// </summary>
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Category 
                WHERE ParentCategoryId = @ParentCategoryId 
                ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Category>(
                sql,
                new
                {
                    ParentCategoryId = parentCategoryId
                });
        }

        /// <summary>
        ///     Gets a paged list of categories (active + deleted).
        ///     Uses SQL Server OFFSET-FETCH syntax.
        /// </summary>
        public async Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Category 
                ORDER BY Name 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<Category>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        /// <summary>
        ///     Searches categories by name or description (active + deleted).
        ///     Service layer filters if needed.
        /// </summary>
        public async Task<IEnumerable<Category>> SearchAsync( string searchTerm )
        {
            // language=tsql
            const string sql = @"
                SELECT * FROM Category 
                WHERE Name LIKE @SearchTerm OR Description LIKE @SearchTerm 
                ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Category>(
                sql,
                new
                {
                    SearchTerm = $"%{searchTerm}%"
                });
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new category and returns it with the assigned CategoryId.
        ///     Uses SQL Server SCOPE_IDENTITY() to retrieve the newly inserted ID.
        /// </summary>
        public async Task<Category> CreateAsync( Category category )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Category (Name, Description, ParentCategoryId, ImageUrl, IsDeleted, DeletedAt)
                VALUES (@Name, @Description, @ParentCategoryId, @ImageUrl, 0, NULL);
                
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newCategoryId = await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    category.Name,
                    category.Description,
                    category.ParentCategoryId,
                    category.ImageUrl
                });

            return category with
            {
                CategoryId = newCategoryId,
                IsDeleted = false,
                DeletedAt = null
            };
        }

        /// <summary>
        ///     Updates an existing category.
        /// </summary>
        public async Task<Category> UpdateAsync( Category category )
        {
            // language=tsql
            const string sql = @"
                UPDATE Category 
                SET Name = @Name,
                    Description = @Description,
                    ParentCategoryId = @ParentCategoryId,
                    ImageUrl = @ImageUrl,
                    IsDeleted = @IsDeleted,
                    DeletedAt = @DeletedAt
                WHERE CategoryId = @CategoryId";

            await sqlDataAccess.ExecuteAsync(sql, category);
            return category;
        }

        /// <summary>
        ///     Soft deletes a category (sets IsDeleted = true and DeletedAt = current timestamp).
        /// </summary>
        public async Task<DatabaseResult> SoftDeleteAsync( int categoryId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Category 
                    SET IsDeleted = 1, DeletedAt = @DeletedAt 
                    WHERE CategoryId = @CategoryId AND IsDeleted = 0";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        CategoryId = categoryId,
                        DeletedAt = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Category with ID {categoryId} not found or already deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error soft deleting category with ID {categoryId}: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Restores a soft-deleted category (sets IsDeleted = false and DeletedAt = null).
        /// </summary>
        public async Task<DatabaseResult> RestoreAsync( int categoryId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Category 
                    SET IsDeleted = 0, DeletedAt = NULL 
                    WHERE CategoryId = @CategoryId AND IsDeleted = 1";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        CategoryId = categoryId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Category with ID {categoryId} cannot be restored because it doesn't exist or has not been soft-deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error restoring category with ID {categoryId}: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Permanently deletes a category by its ID.
        /// </summary>
        public async Task<DatabaseResult> HardDeleteAsync( int categoryId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Category WHERE CategoryId = @CategoryId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        CategoryId = categoryId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Category with ID {categoryId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error permanently deleting category with ID {categoryId}: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
