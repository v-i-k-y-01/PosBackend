using PosBackend.Application.Common.Interfaces;

namespace PosBackend.Infrastructure.Authentication;

/// <summary>
/// BCrypt implementation of the <see cref="IPasswordHasher"/> interface.
/// Provides salted hashing and verification for passwords.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Hashes the plain text password using BCrypt.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The generated password hash string.</returns>
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>
    /// Verifies the plain text password against the hashed representation.
    /// Handles parse exceptions if the hash is malformed.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="passwordHash">The stored secure password hash.</param>
    /// <returns>True if verification succeeds; otherwise false.</returns>
    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Fail safely if salt/hash parsing fails due to invalid format.
            return false;
        }
    }
}
