using System.Windows;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.Enums;
using Storix.Infrastructure.Services.Messages;

namespace Storix.Infrastructure.Services.Errors
{
    /// <summary>
    ///     Handles database-related errors with enhanced error handling, retry logic, and result patterns.
    /// </summary>
    /// <param name="messageService" >Service for displaying messages to the user.</param>
    /// <param name="logger" >Logger for error logging.</param>
    public class DatabaseErrorHandlerService(
        IMessageService messageService,
        ILogger<DatabaseErrorHandlerService> logger,
        RetryConfig? retryConfig = null ):IDatabaseErrorHandlerService
    {
        private readonly RetryConfig _retryConfig = retryConfig ?? new RetryConfig();

        /// <summary>
        ///     Executes a database operation with enhanced error handling and retry logic.
        /// </summary>
        /// <typeparam name="TResult" >The result type of the operation.</typeparam>
        /// <param name="operation" >The asynchronous database operation to execute.</param>
        /// <param name="userActionDescription" >A description of the user action being performed.</param>
        /// <param name="showUserMessage" >Whether to show error messages to the user (default: true).</param>
        /// <param name="enableRetry" >Whether to enable retry logic for this operation (default: true).</param>
        /// <returns>A DatabaseResult containing the operation result or error information.</returns>
        public async Task<DatabaseResult<TResult>> HandleDatabaseOperationAsync<TResult>(
            Func<Task<TResult>> operation,
            string userActionDescription,
            bool showUserMessage = true,
            bool enableRetry = true )
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= _retryConfig.MaxRetries)
            {
                try
                {
                    if (attempt > 0)
                    {
                        TimeSpan delay = TimeSpan.FromMilliseconds(
                            _retryConfig.InitialDelay.TotalMilliseconds * Math.Pow(_retryConfig.BackoffMultiplier, attempt - 1));

                        logger.LogInformation(
                            "Retrying operation '{UserActionDescription}' (attempt {Attempt}/{MaxRetries}) after {Delay}ms",
                            userActionDescription,
                            attempt,
                            _retryConfig.MaxRetries,
                            delay.TotalMilliseconds);

                        await Task.Delay(delay);
                    }

                    TResult result = await operation();

                    if (attempt > 0)
                    {
                        logger.LogInformation(
                            "Operation '{UserActionDescription}' succeeded on retry attempt {Attempt}",
                            userActionDescription,
                            attempt);
                    }

                    return DatabaseResult<TResult>.Success(result);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    (DatabaseErrorCode errorCode, bool shouldRetry) = ClassifyException(ex);

                    logger.LogWarning(
                        "Database operation failed (attempt {Attempt}/{MaxAttempts}) during: {UserActionDescription}. Error: {ErrorMessage}",
                        attempt,
                        _retryConfig.MaxRetries + 1,
                        userActionDescription,
                        ex.Message);

                    // Don't retry if retries are disabled, max attempts reached, or error type shouldn't be retried
                    if (!enableRetry || attempt > _retryConfig.MaxRetries || !shouldRetry)
                    {
                        break;
                    }
                }
            }

            // Handle the final failure
            (DatabaseErrorCode finalErrorCode, _) = ClassifyException(lastException!);
            string errorMessage = GetUserFriendlyErrorMessage(finalErrorCode);

            logger.LogError(
                "Database operation '{UserActionDescription}' failed after {Attempts} attempts. Final error: {ErrorMessage}",
                userActionDescription,
                attempt,
                lastException!.Message);

            if (showUserMessage)
            {
                ShowErrorMessage(finalErrorCode, errorMessage);
            }

            return DatabaseResult<TResult>.Failure(errorMessage, finalErrorCode);
        }

        /// <summary>
        ///     Executes a database operation that does not return a value.
        /// </summary>
        /// <param name="operation" >The asynchronous database operation to execute.</param>
        /// <param name="userActionDescription" >A description of the user action being performed.</param>
        /// <param name="showUserMessage" >Whether to show error messages to the user (default: true).</param>
        /// <param name="enableRetry" >Whether to enable retry logic for this operation (default: true).</param>
        /// <returns>A DatabaseResult indicating success or failure.</returns>
        public async Task<DatabaseResult> HandleDatabaseOperationAsync(
            Func<Task> operation,
            string userActionDescription,
            bool showUserMessage = true,
            bool enableRetry = true )
        {
            DatabaseResult<bool> result = await HandleDatabaseOperationAsync(
                async () =>
                {
                    await operation();
                    return true; // Return dummy value for void operations
                },
                userActionDescription,
                showUserMessage,
                enableRetry);

            return result.IsSuccess
                ? DatabaseResult.Success()
                : DatabaseResult.Failure(result.ErrorMessage!, result.ErrorCode);
        }


        /// <summary>
        ///     Classifies an exception and determines if it should be retried.
        /// </summary>
        /// <param name="exception" >The exception to classify.</param>
        /// <returns>A tuple containing the error code and whether the operation should be retried.</returns>
        private (DatabaseErrorCode errorCode, bool shouldRetry) ClassifyException( Exception exception )
        {
            return exception switch
            {
                SqlException sqlEx => sqlEx.Number switch
                {
                    18456 or 4060 => (DatabaseErrorCode.PermissionDenied, false),                                          // Don't retry permission errors
                    2 or 53       => (DatabaseErrorCode.ConnectionFailure, _retryConfig.EnableRetryForConnectionFailures), // Network-related errors
                    2547          => (DatabaseErrorCode.DuplicateKey, false),                                              // Primary key violation
                    547           => (DatabaseErrorCode.ForeignKeyViolation, false),                                       // Foreign key constraint
                    -2            => (DatabaseErrorCode.Timeout, _retryConfig.EnableRetryForTimeouts),                     // Command timeout
                    _             => (DatabaseErrorCode.ConnectionFailure, _retryConfig.EnableRetryForConnectionFailures)
                },
                TimeoutException      => (DatabaseErrorCode.Timeout, _retryConfig.EnableRetryForTimeouts),
                TaskCanceledException => (DatabaseErrorCode.Timeout, _retryConfig.EnableRetryForTimeouts),
                _                     => (DatabaseErrorCode.UnexpectedError, false)
            };
        }

        /// <summary>
        ///     Gets a user-friendly error message for the given error code.
        /// </summary>
        /// <param name="errorCode" >The error code.</param>
        /// <returns>A user-friendly error message.</returns>
        private static string GetUserFriendlyErrorMessage( DatabaseErrorCode errorCode )
        {
            return errorCode switch
            {
                DatabaseErrorCode.PermissionDenied => "You do not have permission to perform this action. Please contact your administrator.",
                DatabaseErrorCode.ConnectionFailure => "Unable to connect to the database. Please check your network connection or contact support.",
                DatabaseErrorCode.Timeout => "The operation took too long to complete. Please try again or contact support if the problem persists.",
                DatabaseErrorCode.DuplicateKey => "This record already exists. Please check your data and try again.",
                DatabaseErrorCode.ForeignKeyViolation => "Cannot complete this operation because it would violate data integrity rules.",
                DatabaseErrorCode.UnexpectedError => "An unexpected error occurred. Please try again or contact support.",
                DatabaseErrorCode.ValidationFailure => "The data provided is invalid. Please review and correct any errors.",
                DatabaseErrorCode.InvalidInput => "The input provided is not valid. Please check and try again.",
                DatabaseErrorCode.PartialFailure => "The operation was only partially successful. Please review the results and try again if necessary.",
                DatabaseErrorCode.ConstraintViolation =>
                    "The operation could not be completed due to a constraint violation. Please check your data and try again.",
                DatabaseErrorCode.NotFound => "The requested record was not found. Please verify the information and try again.",
                _                          => "An unexpected error occurred. Please try again or contact support."
            };
        }

        /// <summary>
        ///     Shows an error message to the user based on the error code.
        /// </summary>
        /// <param name="errorCode" >The error code.</param>
        /// <param name="message" >The error message.</param>
        private void ShowErrorMessage( DatabaseErrorCode errorCode, string message )
        {
            (string title, MessageBoxImage icon) = errorCode switch
            {
                DatabaseErrorCode.PermissionDenied    => ("Permission Error", MessageBoxImage.Warning),
                DatabaseErrorCode.DuplicateKey        => ("Duplicate Data", MessageBoxImage.Warning),
                DatabaseErrorCode.ForeignKeyViolation => ("Data Integrity Error", MessageBoxImage.Warning),
                _                                     => ("Database Error", MessageBoxImage.Error)
            };

            messageService.Show(
                message,
                title,
                MessageBoxButton.OK,
                icon);
        }
    }
}
