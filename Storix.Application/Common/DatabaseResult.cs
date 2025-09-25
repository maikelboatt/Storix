using Storix.Application.Enums;

namespace Storix.Application.Common
{
    /// <summary>
    ///     Represents the result of a database operation that doesn't return a value.
    /// </summary>
    public class DatabaseResult
    {
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public DatabaseErrorCode ErrorCode { get; init; }

        public static DatabaseResult Success() => new()
        {
            IsSuccess = true,
            ErrorCode = DatabaseErrorCode.None
        };

        public static DatabaseResult Failure( string errorMessage, DatabaseErrorCode errorCode ) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}
