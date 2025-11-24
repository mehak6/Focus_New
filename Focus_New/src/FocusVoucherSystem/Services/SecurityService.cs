using System.Security.Cryptography;
using System.Text;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Provides security services including password hashing and encryption key derivation
/// </summary>
public static class SecurityService
{
    // Salt for key derivation (fixed salt - in production, store this securely)
    private const string SALT = "FocusVoucherSystem2024SecureSalt";

    /// <summary>
    /// Derives an encryption key from a password using PBKDF2
    /// </summary>
    public static string DeriveEncryptionKey(string password)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes(SALT),
            100000, // 100k iterations
            HashAlgorithmName.SHA256))
        {
            var keyBytes = pbkdf2.GetBytes(32); // 256-bit key
            return Convert.ToHexString(keyBytes);
        }
    }

    /// <summary>
    /// Compute SHA256 hash of input string
    /// </summary>
    public static string ComputeSha256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }

    /// <summary>
    /// Gets the stored password hash for validation
    /// SHA256 hash of "mehak"
    /// </summary>
    public static string GetPasswordHash()
    {
        // Pre-computed SHA256 hash of "mehak"
        // Computed dynamically to ensure accuracy
        return ComputeSha256Hash("mehak");
    }
}
