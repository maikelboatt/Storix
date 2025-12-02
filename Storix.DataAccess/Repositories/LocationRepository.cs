using System.Text;
using Dapper;
using Storix.Application.Common;
using Storix.Application.DataAccess;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.DataAccess.DBAccess;
using Storix.Domain.Models;
using Storix.Domain.Enums;

namespace Storix.DataAccess.Repositories
{
    public class LocationRepository( ISqlDataAccess sqlDataAccess ):ILocationRepository
    {
        #region Validation

        /// <summary>
        ///     Check if a location exists by ID.
        /// </summary>
        public async Task<bool> ExistsAsync( int locationId, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Location WHERE LocationId = @LocationId"
                // language=tsql
                : "SELECT COUNT(1) FROM Location WHERE LocationId = @LocationId AND IsDeleted = 0";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    LocationId = locationId
                });
        }

        /// <summary>
        ///     Check if a location exists by name.
        /// </summary>
        public async Task<bool> ExistsByNameAsync( string name, int? excludeLocationId = null, bool includeDeleted = false )
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(1) FROM Location WHERE Name = @Name AND (@ExcludeLocationId IS NULL OR LocationId != @ExcludeLocationId)"
                // language=tsql
                : "SELECT COUNT(1) FROM Location WHERE Name = @Name AND IsDeleted = 0 AND (@ExcludeLocationId IS NULL OR LocationId != @ExcludeLocationId)";

            return await sqlDataAccess.ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    Name = name,
                    ExcludeLocationId = excludeLocationId
                });
        }

        #endregion

        #region Count Operations

        /// <summary>
        ///     Gets the total count of locations (including deleted).
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Location";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of active locations.
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Location WHERE IsDeleted = 0";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of deleted locations.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            // language=tsql
            const string sql = "SELECT COUNT(*) FROM Location WHERE IsDeleted = 1";
            return await sqlDataAccess.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        ///     Gets the count of locations by type.
        /// </summary>
        public async Task<int> GetCountByTypeAsync( LocationType type, bool includeDeleted = false )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT COUNT(*) FROM Location WHERE Type = @Type"
                // language=tsql
                : "SELECT COUNT(*) FROM Location WHERE Type = @Type AND IsDeleted = 0";

            return await sqlDataAccess.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Type = type
                });
        }

        #endregion

        #region Read Operations

        /// <summary>
        ///     Gets a location by ID (includes deleted).
        /// </summary>
        public async Task<Location?> GetByIdAsync( int locationId, bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Location WHERE LocationId = @LocationId"
                // language=tsql
                : "SELECT * FROM Location WHERE LocationId = @LocationId AND IsDeleted = 0";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Location>(
                sql,
                new
                {
                    LocationId = locationId
                });
        }

        /// <summary>
        ///     Gets all locations (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Location>> GetAllAsync( bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Location ORDER BY Name"
                // language=tsql
                : "SELECT * FROM Location WHERE IsDeleted = 0 ORDER BY Name";

            return await sqlDataAccess.QueryAsync<Location>(sql);
        }

        /// <summary>
        ///     Gets a location by name (includes deleted).
        /// </summary>
        public async Task<Location?> GetByNameAsync( string name, bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Location WHERE Name = @Name"
                // language=tsql
                : "SELECT * FROM Location WHERE Name = @Name AND IsDeleted = 0";

            return await sqlDataAccess.QuerySingleOrDefaultAsync<Location>(
                sql,
                new
                {
                    Name = name
                });
        }

        /// <summary>
        ///     Gets locations by type (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Location>> GetByTypeAsync( LocationType type, bool includeDeleted = true )
        {
            // language=tsql
            string sql = includeDeleted
                ? "SELECT * FROM Location WHERE Type = @Type ORDER BY Name"
                // language=tsql
                : "SELECT * FROM Location WHERE Type = @Type AND IsDeleted = 0 ORDER BY Name";

            return await sqlDataAccess.QueryAsync<Location>(
                sql,
                new
                {
                    Type = type
                });
        }

        /// <summary>
        ///     Gets a paged list of locations (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Location>> GetPagedAsync( int pageNumber, int pageSize )
        {
            int offset = (pageNumber - 1) * pageSize;

            // language=tsql
            const string sql = @"
                SELECT * FROM Location 
                ORDER BY Name 
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            return await sqlDataAccess.QueryAsync<Location>(
                sql,
                new
                {
                    PageSize = pageSize,
                    Offset = offset
                });
        }

        #endregion

        #region Search & Filter

        /// <summary>
        ///     Searches locations with optional filters (includes deleted).
        /// </summary>
        public async Task<IEnumerable<Location>> SearchAsync(
            string? searchTerm = null,
            LocationType? type = null,
            bool? isDeleted = null )
        {
            // language=tsql
            StringBuilder sql = new("SELECT * FROM Location WHERE 1=1");
            DynamicParameters parameters = new();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql.Append(" AND (Name LIKE @SearchTerm OR Description LIKE @SearchTerm OR Address LIKE @SearchTerm)");
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            if (type.HasValue)
            {
                sql.Append(" AND Type = @Type");
                parameters.Add("Type", type.Value);
            }

            if (isDeleted.HasValue)
            {
                sql.Append(" AND IsDeleted = @IsDeleted");
                parameters.Add("IsDeleted", isDeleted.Value);
            }

            sql.Append(" ORDER BY Name");

            return await sqlDataAccess.QueryAsync<Location>(sql.ToString(), parameters);
        }

        #endregion

        #region Write Operations

        /// <summary>
        ///     Creates a new location and returns it with its generated ID.
        /// </summary>
        public async Task<Location> CreateAsync( Location location )
        {
            // language=tsql
            const string sql = @"
                INSERT INTO Location (
                    Name, Description, Type, Address, IsDeleted, DeletedAt
                )
                VALUES (
                    @Name, @Description, @Type, @Address, @IsDeleted, @DeletedAt
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int locationId = await sqlDataAccess.ExecuteScalarAsync<int>(sql, location);

            return location with
            {
                LocationId = locationId
            };
        }

        /// <summary>
        ///     Updates an existing location.
        /// </summary>
        public async Task<Location> UpdateAsync( Location location )
        {
            // language=tsql
            const string sql = @"
                UPDATE Location 
                SET Name = @Name,
                    Description = @Description,
                    Type = @Type,
                    Address = @Address
                WHERE LocationId = @LocationId";

            await sqlDataAccess.ExecuteAsync(sql, location);
            return location;
        }

        #endregion

        #region Delete Operations

        /// <summary>
        ///     Soft deletes a location by ID.
        /// </summary>
        public async Task<DatabaseResult> SoftDeleteAsync( int locationId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Location 
                    SET IsDeleted = 1,
                        DeletedAt = @DeletedAt
                    WHERE LocationId = @LocationId AND IsDeleted = 0";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        LocationId = locationId,
                        DeletedAt = DateTime.UtcNow
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Location with ID {locationId} not found or already deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error deleting location: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Restores a soft-deleted location.
        /// </summary>
        public async Task<DatabaseResult> RestoreAsync( int locationId )
        {
            try
            {
                // language=tsql
                const string sql = @"
                    UPDATE Location 
                    SET IsDeleted = 0,
                        DeletedAt = NULL
                    WHERE LocationId = @LocationId AND IsDeleted = 1";

                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        LocationId = locationId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Location with ID {locationId} not found or not deleted",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error restoring location: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        /// <summary>
        ///     Permanently deletes a location by ID.
        ///     WARNING: This permanently removes the location from the database.
        /// </summary>
        public async Task<DatabaseResult> HardDeleteAsync( int locationId )
        {
            try
            {
                // language=tsql
                const string sql = "DELETE FROM Location WHERE LocationId = @LocationId";
                int affectedRows = await sqlDataAccess.ExecuteAsync(
                    sql,
                    new
                    {
                        LocationId = locationId
                    });

                return affectedRows > 0
                    ? DatabaseResult.Success()
                    : DatabaseResult.Failure(
                        $"Location with ID {locationId} not found",
                        DatabaseErrorCode.NotFound);
            }
            catch (Exception ex)
            {
                return DatabaseResult.Failure(
                    $"Error permanently deleting location: {ex.Message}",
                    DatabaseErrorCode.UnexpectedError);
            }
        }

        #endregion
    }
}
