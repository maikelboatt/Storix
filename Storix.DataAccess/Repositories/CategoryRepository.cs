using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Storix.Application.Common;
using Storix.Application.Enums;
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
        public async Task<bool> ExistsAsync( int categoryId, bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Categories WHERE CategoryId = @CategoryId"
                : "SELECT COUNT(1) FROM Categories WHERE CategoryId = @CategoryId AND IsDeleted = 0";

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
            string sql = includeDeleted
                ? @"SELECT COUNT(1) FROM Categories 
                    WHERE Name = @Name 
                    AND (@ExcludeCategoryId IS NULL OR CategoryId != @ExcludeCategoryId)"
                : @"SELECT COUNT(1) FROM Categories 
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

        #region Count Operations

        /// <summary>
        ///     Gets the total count of categories.
        /// </summary>
        public async Task<int> GetTotalCountAsync( bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? "SELECT COUNT(*) FROM Categories"
                : "SELECT COUNT(*) FROM Categories WHERE IsDeleted = 0";

            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of active categories (non-deleted).
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            const string sql = "SELECT COUNT(*) FROM Categories WHERE IsDeleted = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of soft-deleted categories.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            const string sql = "SELECT COUNT(*) FROM Categories WHERE IsDeleted = 1";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Get a category by its ID.
        /// </summary>
        public async Task<Category?> GetByIdAsync( int categoryId, bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? "SELECT * FROM Categories WHERE CategoryId = @CategoryId"
                : "SELECT * FROM Categories WHERE CategoryId = @CategoryId AND IsDeleted = 0";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Category>(
                sql,
                new
                {
                    CategoryId = categoryId
                });
        }

        /// <summary>
        ///     Gets all categories.
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllAsync( bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? "SELECT * FROM Categories ORDER BY Name"
                : "SELECT * FROM Categories WHERE IsDeleted = 0 ORDER BY Name";

            return await sqlDataAccess.QueryAsync<Category>(sql);
        }

        /// <summary>
        ///     Gets all active categories (non-deleted).
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllActiveAsync()
        {
            const string sql = "SELECT * FROM Categories WHERE IsDeleted = 0 ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Category>(sql);
        }

        /// <summary>
        ///     Gets all soft-deleted categories.
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllDeletedAsync()
        {
            const string sql = "SELECT * FROM Categories WHERE IsDeleted = 1 ORDER BY Name";
            return await sqlDataAccess.QueryAsync<Category>(sql);
        }

        /// <summary>
        ///     Gets root categories (categories without a parent).
        /// </summary>
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync( bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? "SELECT * FROM Categories WHERE ParentCategoryId IS NULL ORDER BY Name"
                : "SELECT * FROM Categories WHERE ParentCategoryId IS NULL AND IsDeleted = 0 ORDER BY Name";

            return await sqlDataAccess.QueryAsync<Category>(sql);
        }

        /// <summary>
        ///     Gets subcategories for a given parent category.
        /// </summary>
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync( int parentCategoryId, bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? "SELECT * FROM Categories WHERE ParentCategoryId = @ParentCategoryId ORDER BY Name"
                : "SELECT * FROM Categories WHERE ParentCategoryId = @ParentCategoryId AND IsDeleted = 0 ORDER BY Name";

            return await sqlDataAccess.QueryAsync<Category>(
                sql,
                new
                {
                    ParentCategoryId = parentCategoryId
                });
        }

        /// <summary>
        ///     Gets a paged list of categories.
        /// </summary>
        public async Task<IEnumerable<Category>> GetPagedAsync( int pageNumber, int pageSize, bool includeDeleted = false )
        {
            int offset = (pageNumber - 1) * pageSize;

            string sql = includeDeleted
                ? "SELECT * FROM Categories ORDER BY Name LIMIT @PageSize OFFSET @Offset"
                : "SELECT * FROM Categories WHERE IsDeleted = 0 ORDER BY Name LIMIT @PageSize OFFSET @Offset";

            return await sqlDataAccess.QueryAsync<Category>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        /// <summary>
        ///     Searches categories by name or description.
        /// </summary>
        public async Task<IEnumerable<Category>> SearchAsync( string searchTerm, bool includeDeleted = false )
        {
            string sql = includeDeleted
                ? @"SELECT * FROM Categories 
                    WHERE Name LIKE @SearchTerm 
                    OR Description LIKE @SearchTerm 
                    ORDER BY Name"
                : @"SELECT * FROM Categories 
                    WHERE IsDeleted = 0 
                    AND (Name LIKE @SearchTerm OR Description LIKE @SearchTerm) 
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
        /// </summary>
        public async Task<Category> CreateAsync( Category category )
        {
            const string sql = @"
                INSERT INTO Categories (Name, Description, ParentCategoryId, ImageUrl, IsDeleted, DeletedAt)
                VALUES (@Name, @Description, @ParentCategoryId, @ImageUrl, 0, NULL);
                SELECT last_insert_rowid();";

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
            const string sql = @"
                UPDATE Categories 
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
                const string sql = @"
                    UPDATE Categories 
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
                const string sql = @"
                    UPDATE Categories 
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
                const string sql = "DELETE FROM Categories WHERE CategoryId = @CategoryId";
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
