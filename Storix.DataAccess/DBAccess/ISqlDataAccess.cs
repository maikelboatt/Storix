using System.Data;
using Dapper;

namespace Storix.DataAccess.DBAccess
{
    /// <summary>
    ///     Interface for SQL data access operations using Dapper.
    /// </summary>
    public interface ISqlDataAccess
    {
        /// <summary>
        ///     Executes a stored procedure that performs an action without returning data.
        /// </summary>
        Task CommandAsync(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a stored procedure and returns a collection of results.
        /// </summary>
        Task<IEnumerable<T>> QueryAsync<T>(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a stored procedure and returns a single result or null.
        /// </summary>
        Task<T?> QuerySingleOrDefaultAsync<T>(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a stored procedure and returns a single value.
        /// </summary>
        Task<T> ExecuteScalarAsync<T>(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes an operation within a transaction that returns a value.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(
            Func<IDbConnection, IDbTransaction, Task<T>> operation );

        /// <summary>
        ///     Executes an operation within a transaction that does not return a value.
        /// </summary>
        Task ExecuteInTransactionAsync(
            Func<IDbConnection, IDbTransaction, Task> operation );

        /// <summary>
        ///     Executes a stored procedure that returns multiple result sets and maps them.
        /// </summary>
        Task<IEnumerable<T>> QueryMultipleAsync<T>(
            string storedProcedure,
            object? parameters,
            Func<SqlMapper.GridReader, Task<IEnumerable<T>>> map,
            int? commandTimeout = null );
    }
}
