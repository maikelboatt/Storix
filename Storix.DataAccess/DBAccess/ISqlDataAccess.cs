using System.Data;
using Dapper;

namespace Storix.DataAccess.DBAccess
{
    public interface ISqlDataAccess
    {
        /// <summary>
        ///     Executes a SQL query that returns multiple result sets.
        /// </summary>
        /// <typeparam name="T">The type of object to map results into.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="map">A function to process the multiple result sets using Dapper's <see cref="SqlMapper.GridReader"/>.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>A collection of <typeparamref name="T"/> results.</returns>
        Task<IEnumerable<T>> QueryMultipleAsync<T>(
            string sql,
            object? parameters,
            Func<SqlMapper.GridReader, Task<IEnumerable<T>>> map,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a SQL command that performs an action (INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">Optional parameters for the command.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        Task CommandAsync(
            string sql,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a SQL command that performs an action and returns the number of affected rows.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">Optional parameters for the command.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> ExecuteAsync(
            string sql,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a SQL query that returns a collection of results.
        /// </summary>
        /// <typeparam name="T">The type of objects returned.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>A collection of <typeparamref name="T"/> results.</returns>
        Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a SQL query that returns a single record or <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The type of object returned.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>A single <typeparamref name="T"/> object or <c>null</c>.</returns>
        Task<T?> QuerySingleOrDefaultAsync<T>(
            string sql,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes a SQL query that returns a single scalar value.
        /// </summary>
        /// <typeparam name="T">The type of scalar result.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>The scalar value returned.</returns>
        Task<T> ExecuteScalarAsync<T>(
            string sql,
            object? parameters = null,
            int? commandTimeout = null );

        /// <summary>
        ///     Executes an operation inside a database transaction and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="operation">The function to execute inside the transaction.</param>
        /// <returns>The result from the operation.</returns>
        Task<T> ExecuteInTransactionAsync<T>(
            Func<IDbConnection, IDbTransaction, Task<T>> operation );

        /// <summary>
        ///     Executes an operation inside a database transaction without returning a result.
        /// </summary>
        /// <param name="operation">The function to execute inside the transaction.</param>
        Task ExecuteInTransactionAsync(
            Func<IDbConnection, IDbTransaction, Task> operation );
    }
}
