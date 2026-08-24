namespace PosBackend.Application.Common.Interfaces;

/// <summary>
/// Defines hashing and verification services for user passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the provided plain text password securely.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The secure, salted password hash.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies the plain text password against a stored secure password hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="passwordHash">The stored secure password hash to compare against.</param>
    /// <returns>True if the password matches the hash; otherwise false.</returns>
    bool Verify(string password, string passwordHash);
}
