namespace Storix.Application.Services.Users.Interfaces
{
    /// <summary>
    ///     Interface for password hashing operations.
    ///     Implement this interface using a secure hashing algorithm like BCrypt, Argon2, or PBKDF2.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        ///     Hashes a plain text password.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The hashed password.</returns>
        string Hash( string password );

        /// <summary>
        ///     Verifies that a plain text password matches a hashed password.
        /// </summary>
        /// <param name="password">The plain text password to verify.</param>
        /// <param name="hashedPassword">The hashed password to compare against.</param>
        /// <returns>True if the password matches; otherwise, false.</returns>
        bool Verify( string password, string hashedPassword );
    }
}
