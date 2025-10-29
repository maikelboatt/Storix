using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Storix.Application.Services.Users.Interfaces;

namespace Storix.Infrastructure
{
    /// <summary>
    /// Password Hasher implementation using Argon2id algorithm.
    /// </summary>
    public class Argon2PasswordHasher:IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 4;
        private const int MemorySize = 65536; // 64 MB
        private const int DegreeOfParallelism = 2;

        /// <summary>
        /// Hashes a plain text password using Argon2id.
        /// </summary>
        public string Hash( string password )
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            
            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password
            byte[] hash = HashPassword(password, salt);

            // Combine salt and hash, then encode to Base64
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(
                salt,
                0,
                hashBytes,
                0,
                SaltSize);
            Array.Copy(
                hash,
                0,
                hashBytes,
                SaltSize,
                HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        ///     Verifies that a plain text password matches a hashed password.
        /// </summary>
        public bool Verify( string password, string hashedPassword )
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            if (string.IsNullOrEmpty(hashedPassword))
                throw new ArgumentException("Hashed password cannot be null or empty.", nameof(hashedPassword));

            try
            {
                // Decode the Base64 string
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                if (hashBytes.Length != SaltSize + HashSize)
                    return false;

                // Extract the salt
                byte[] salt = new byte[SaltSize];
                Array.Copy(
                    hashBytes,
                    0,
                    salt,
                    0,
                    SaltSize);

                // Extract the hash
                byte[] storedHash = new byte[HashSize];
                Array.Copy(
                    hashBytes,
                    SaltSize,
                    storedHash,
                    0,
                    HashSize);

                // Hash the input password with the extracted salt
                byte[] computedHash = HashPassword(password, salt);

                // Compare the hashes using constant-time comparison
                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        private byte[] HashPassword( string password, byte[] salt )
        {
            using Argon2id argon2 = new(System.Text.Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations
            };

            return argon2.GetBytes(HashSize);
        }
    }
}
