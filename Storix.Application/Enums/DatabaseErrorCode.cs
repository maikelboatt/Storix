namespace Storix.Application.Enums
{
    public enum DatabaseErrorCode
    {
        None,                // Success case
        PermissionDenied,    // User lacks permissions
        ConnectionFailure,   // Network/connectivity issues
        Timeout,             // Operation took too long
        DuplicateKey,        // Unique constraint violation
        ForeignKeyViolation, // Referential integrity violation
        UnexpectedError,     // Catch-all for unknown errors
        ValidationFailure,   // Business validation failed ← NEW
        InvalidInput,        // Invalid parameters/input ← NEW
        NotFound             // Record not found ← NEW
    }
}
