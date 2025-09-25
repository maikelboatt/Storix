using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Storix.DataAccess.DBAccess
{
    /// <summary>
    ///     Provides methods for executing stored procedures against SQL Server database using Dapper.
    ///     Supports queries, commands, scalar operations, and transactions.
    /// </summary>
    public class SqlDataAccess( string connectionString, ILogger<SqlDataAccess> logger ):ISqlDataAccess
    {
        private const int DefaultCommandTimeout = 30;

        /// <summary>
        ///     Executes a stored procedure that returns multiple result sets.
        /// </summary>
        /// <typeparam name="T" >The type of object to map results into.</typeparam>
        /// <param name="storedProcedure" >The stored procedure name.</param>
        /// <param name="parameters" >Optional parameters for the stored procedure.</param>
        /// <param name="map" >A function to process the multiple result sets using Dapper's <see cref="SqlMapper.GridReader" />.</param>
        /// <param name="commandTimeout" >Optional timeout in seconds.</param>
        /// <returns>A collection of <typeparamref name="T" /> results.</returns>
        public async Task<IEnumerable<T>> QueryMultipleAsync<T>(
            string storedProcedure,
            object? parameters,
            Func<SqlMapper.GridReader, Task<IEnumerable<T>>> map,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("multiple query", storedProcedure, parameters);

                await using SqlConnection connection = new(connectionString);
                await using SqlMapper.GridReader multi = await connection.QueryMultipleAsync(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                IEnumerable<T> result = await map(multi);

                LogSuccess("multiple query", storedProcedure);
                return result;
            }
            catch (Exception e)
            {
                LogError(e, "multiple query", storedProcedure, parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a stored procedure that performs an action (INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="storedProcedure" >The stored procedure name.</param>
        /// <param name="parameters" >Optional parameters for the stored procedure.</param>
        /// <param name="commandTimeout" >Optional timeout in seconds.</param>
        public async Task CommandAsync(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("command", storedProcedure, parameters);

                await using SqlConnection connection = new(connectionString);
                await connection.ExecuteAsync(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("command", storedProcedure);
            }
            catch (Exception e)
            {
                LogError(e, "command", storedProcedure, parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a stored procedure that performs an action and returns the number of affected rows.
        /// </summary>
        /// <returns></returns>
        public async Task<int> ExecuteAsync( string storedProcedure, object? parameters = null, int? commandTimeout = null )
        {
            try
            {
                LogExecuting("execute", storedProcedure, parameters);

                await using SqlConnection connection = new(connectionString);
                int affectedRows = await connection.ExecuteAsync(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("execute", storedProcedure, affectedRows);

                return affectedRows;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     Executes a stored procedure that returns a collection of results.
        /// </summary>
        /// <typeparam name="T" >The type of objects returned.</typeparam>
        /// <param name="storedProcedure" >The stored procedure name.</param>
        /// <param name="parameters" >Optional parameters for the stored procedure.</param>
        /// <param name="commandTimeout" >Optional timeout in seconds.</param>
        /// <returns>A collection of <typeparamref name="T" /> results.</returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("query", storedProcedure, parameters);

                await using SqlConnection connection = new(connectionString);
                IEnumerable<T> result = await connection.QueryAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                List<T> queryAsync = result.ToList();
                LogSuccess("query", storedProcedure, queryAsync.Count());

                return queryAsync;
            }
            catch (Exception e)
            {
                LogError(e, "query", storedProcedure, parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a stored procedure that returns a single record or <c>null</c>.
        /// </summary>
        /// <typeparam name="T" >The type of object returned.</typeparam>
        /// <param name="storedProcedure" >The stored procedure name.</param>
        /// <param name="parameters" >Optional parameters for the stored procedure.</param>
        /// <param name="commandTimeout" >Optional timeout in seconds.</param>
        /// <returns>A single <typeparamref name="T" /> object or <c>null</c>.</returns>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("single query", storedProcedure, parameters);

                await using SqlConnection connection = new(connectionString);
                T? result = await connection.QuerySingleOrDefaultAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("single query", storedProcedure, result != null);
                return result;
            }
            catch (Exception e)
            {
                LogError(e, "single query", storedProcedure, parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes a stored procedure that returns a single scalar value.
        /// </summary>
        /// <typeparam name="T" >The type of scalar result.</typeparam>
        /// <param name="storedProcedure" >The stored procedure name.</param>
        /// <param name="parameters" >Optional parameters for the stored procedure.</param>
        /// <param name="commandTimeout" >Optional timeout in seconds.</param>
        /// <returns>The scalar value returned.</returns>
        public async Task<T> ExecuteScalarAsync<T>(
            string storedProcedure,
            object? parameters = null,
            int? commandTimeout = null )
        {
            try
            {
                LogExecuting("scalar", storedProcedure, parameters);

                await using SqlConnection connection = new(connectionString);
                T result = await connection.ExecuteScalarAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: commandTimeout ?? DefaultCommandTimeout);

                LogSuccess("scalar", storedProcedure, result);
                return result;
            }
            catch (Exception e)
            {
                LogError(e, "scalar", storedProcedure, parameters);
                throw;
            }
        }

        /// <summary>
        ///     Executes an operation inside a database transaction and returns a result.
        /// </summary>
        /// <typeparam name="T" >The type of the result returned by the operation.</typeparam>
        /// <param name="operation" >The function to execute inside the transaction.</param>
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
        /// <param name="operation" >The function to execute inside the transaction.</param>
        public async Task ExecuteInTransactionAsync(
            Func<IDbConnection, IDbTransaction, Task> operation )
        {
            await ExecuteInTransactionAsync<object>(async ( connection, transaction ) =>
            {
                await operation(connection, transaction);
                return null!;
            });
        }

        private void LogExecuting( string operation, string storedProcedure, object? parameters = null )
        {
            if (parameters != null)
            {
                logger.LogDebug(
                    "Executing {Operation} {StoredProcedure} with parameters {@Parameters}",
                    operation,
                    storedProcedure,
                    parameters);
            }
            else
            {
                logger.LogDebug("Executing {Operation} {StoredProcedure}", operation, storedProcedure);
            }
        }

        private void LogSuccess( string operation, string storedProcedure, object? additionalInfo = null )
        {
            if (additionalInfo != null)
            {
                logger.LogDebug(
                    "Successfully executed {Operation} {StoredProcedure}. Info: {@AdditionalInfo}",
                    operation,
                    storedProcedure,
                    additionalInfo);
            }
            else
            {
                logger.LogDebug("Successfully executed {Operation} {StoredProcedure}", operation, storedProcedure);
            }
        }

        private void LogError( Exception e, string operation, string storedProcedure, object? parameters = null )
        {
            if (parameters != null)
            {
                logger.LogError(
                    e,
                    "Error executing {Operation} {StoredProcedure} with parameters {@Parameters}. Error: {Message}",
                    operation,
                    storedProcedure,
                    parameters,
                    e.Message);
            }
            else
            {
                logger.LogError(
                    e,
                    "Error executing {Operation} {StoredProcedure}. Error: {Message}",
                    operation,
                    storedProcedure,
                    e.Message);
            }
        }
    }
}
