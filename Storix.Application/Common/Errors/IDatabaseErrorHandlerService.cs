using System;
using System.Threading.Tasks;

namespace Storix.Application.Common.Errors
{
    /// <summary>
    ///     Interface for the enhanced database error handler service.
    /// </summary>
    public interface IDatabaseErrorHandlerService
    {
        public Task<DatabaseResult<TResult>> HandleDatabaseOperationAsync<TResult>(
            Func<Task<TResult>> operation,
            string userActionDescription,
            bool showUserMessage = true,
            bool enableRetry = true );

        public Task<DatabaseResult> HandleDatabaseOperationAsync(
            Func<Task> operation,
            string userActionDescription,
            bool showUserMessage = true,
            bool enableRetry = true );
    }
}
