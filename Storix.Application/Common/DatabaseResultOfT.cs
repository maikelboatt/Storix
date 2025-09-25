using Storix.Application.Enums;

namespace Storix.Application.Common
{
    /// <summary>
    ///     Represents the result of a database operation.
    /// </summary>
    /// <typeparam name="T" >The type of the result value.</typeparam>
    public class DatabaseResult<T>
    {
        public bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public string? ErrorMessage { get; init; }
        public DatabaseErrorCode ErrorCode { get; init; }

        public static DatabaseResult<T> Success( T value ) => new()
        {
            IsSuccess = true,
            Value = value,
            ErrorCode = DatabaseErrorCode.None
        };

        public static DatabaseResult<T> Failure( string errorMessage, DatabaseErrorCode errorCode ) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}
