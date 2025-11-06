using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Storix.DataAccess.DBAccess
{
    /// <summary>
    ///     Provides methods for executing SQL queries against SQL Server database using Dapper.
    ///     Supports queries, commands, scalar operations, and transactions.
    /// </summary>
    public class SqlDataAccess( string connectionString, ILogger<SqlDataAccess> logger ):ISqlDataAccess
    {
        private const int DefaultCommandTimeout = 30;

        /// <summary>
        ///     Executes a SQL query that returns multiple result sets.
        /// </summary>
        /// <typeparam name="T">The type of object to map results into.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="map">A function to process the multiple result sets using Dapper's <see cref="SqlMapper.GridReader"/>.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>A collection of <typeparamref name="T"/> results.</returns>
        public async Task<IEnumerable<T>> QueryMultipleAsync<T>(
            string sql,
            object? parameters,
            Func<SqlMapper.GridReader, Task<IEnumerable<T>>> map,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("multiple query", sql, parameters);

                await using SqlConnection connection = new(connectionString);
                await using SqlMapper.GridReader multi = await connection.QueryMultipleAsync(
                    sql,
                    parameters,
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                IEnumerable<T> result = await map(multi);

                LogSuccess("multiple query", sql);
                return result;
            }
            catch (Exception e)
            {
                LogError(
                    e,
                    "multiple query",
                    sql,
                    parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a SQL command that performs an action (INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">Optional parameters for the command.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        public async Task CommandAsync(
            string sql,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("command", sql, parameters);

                await using SqlConnection connection = new(connectionString);
                await connection.ExecuteAsync(
                    sql,
                    parameters,
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("command", sql);
            }
            catch (Exception e)
            {
                LogError(
                    e,
                    "command",
                    sql,
                    parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a SQL command that performs an action and returns the number of affected rows.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">Optional parameters for the command.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> ExecuteAsync(
            string sql,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("execute", sql, parameters);

                await using SqlConnection connection = new(connectionString);
                int affectedRows = await connection.ExecuteAsync(
                    sql,
                    parameters,
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("execute", sql, affectedRows);

                return affectedRows;
            }
            catch (Exception e)
            {
                LogError(
                    e,
                    "execute",
                    sql,
                    parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a SQL query that returns a collection of results.
        /// </summary>
        /// <typeparam name="T">The type of objects returned.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>A collection of <typeparamref name="T"/> results.</returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("query", sql, parameters);

                await using SqlConnection connection = new(connectionString);
                IEnumerable<T> result = await connection.QueryAsync<T>(
                    sql,
                    parameters,
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                List<T> queryAsync = result.ToList();
                LogSuccess("query", sql, queryAsync.Count);

                return queryAsync;
            }
            catch (Exception e)
            {
                LogError(
                    e,
                    "query",
                    sql,
                    parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a SQL query that returns a single record or <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The type of object returned.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>A single <typeparamref name="T"/> object or <c>null</c>.</returns>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(
            string sql,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("single query", sql, parameters);

                await using SqlConnection connection = new(connectionString);
                T? result = await connection.QuerySingleOrDefaultAsync<T>(
                    sql,
                    parameters,
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("single query", sql, result != null);
                return result;
            }
            catch (Exception e)
            {
                LogError(
                    e,
                    "single query",
                    sql,
                    parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a SQL query that returns a single scalar value.
        /// </summary>
        /// <typeparam name="T">The type of scalar result.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="commandTimeout">Optional timeout in seconds.</param>
        /// <returns>The scalar value returned.</returns>
        public async Task<T> ExecuteScalarAsync<T>(
            string sql,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("scalar", sql, parameters);

                await using SqlConnection connection = new(connectionString);
                T result = await connection.ExecuteScalarAsync<T>(
                    sql,
                    parameters,
                    commandType: CommandType.Text,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("scalar", sql, result);
                return result;
            }
            catch (Exception e)
            {
                LogError(
                    e,
                    "scalar",
                    sql,
                    parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes an operation inside a database transaction and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="operation">The function to execute inside the transaction.</param>
        /// <returns>The result from the operation.</returns>
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<IDbConnection, IDbTransaction, Task<T>> operation )
        {
            await using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                logger.LogDebug("Starting database transaction");

                T result = await operation(connection, transaction);

                transaction.Commit();
                logger.LogDebug("Transaction committed successfully");
                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Transaction failed, rolling back. Error: {Message}", e.Message);
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        ///     Executes an operation inside a database transaction without returning a result.
        /// </summary>
        /// <param name="operation">The function to execute inside the transaction.</param>
        public async Task ExecuteInTransactionAsync(
            Func<IDbConnection, IDbTransaction, Task> operation )
        {
            await ExecuteInTransactionAsync<object>(async ( connection, transaction ) =>
            {
                await operation(connection, transaction);
                return null!;
            });
        }

        private void LogExecuting( string operation, string sql, object? parameters = null )
        {
            if (parameters != null)
            {
                logger.LogDebug(
                    "Executing {Operation} with parameters {@Parameters}. SQL: {Sql}",
                    operation,
                    parameters,
                    TruncateSql(sql));
            }
            else
            {
                logger.LogDebug("Executing {Operation}. SQL: {Sql}", operation, TruncateSql(sql));
            }
        }

        private void LogSuccess( string operation, string sql, object? additionalInfo = null )
        {
            if (additionalInfo != null)
            {
                logger.LogDebug(
                    "Successfully executed {Operation}. Info: {@AdditionalInfo}",
                    operation,
                    additionalInfo);
            }
            else
            {
                logger.LogDebug("Successfully executed {Operation}", operation);
            }
        }

        private void LogError( Exception e,
            string operation,
            string sql,
            object? parameters = null )
        {
            if (parameters != null)
            {
                logger.LogError(
                    e,
                    "Error executing {Operation} with parameters {@Parameters}. SQL: {Sql}. Error: {Message}",
                    operation,
                    parameters,
                    TruncateSql(sql),
                    e.Message);
            }
            else
            {
                logger.LogError(
                    e,
                    "Error executing {Operation}. SQL: {Sql}. Error: {Message}",
                    operation,
                    TruncateSql(sql),
                    e.Message);
            }
        }

        /// <summary>
        ///     Truncates SQL for logging to prevent excessive log size.
        /// </summary>
        private static string TruncateSql( string sql, int maxLength = 200 )
        {
            string cleaned = sql
                             .Trim()
                             .Replace("\r\n", " ")
                             .Replace("\n", " ");
            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            return cleaned.Length <= maxLength
                ? cleaned
                : cleaned.Substring(0, maxLength) + "...";
        }
    }
}
