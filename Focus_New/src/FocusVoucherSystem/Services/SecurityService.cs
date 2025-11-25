using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Provides security services including password hashing, encryption key derivation, and login attempt tracking
/// </summary>
public static class SecurityService
{
    // Salt for key derivation (fixed salt - in production, store this securely)
    private const string SALT = "FocusVoucherSystem2024SecureSalt";

    // Path for login state persistence
    private static readonly string LOGIN_STATE_PATH = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusVoucherSystem",
        "login_state.json"
    );

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

    #region Login Attempt Tracking

    /// <summary>
    /// Gets the current failed login attempt count
    /// </summary>
    public static int GetFailedAttempts()
    {
        var state = LoadLoginState();
        return state.FailedAttempts;
    }

    /// <summary>
    /// Increments the failed login attempt counter and returns new count
    /// </summary>
    public static int IncrementFailedAttempts()
    {
        var state = LoadLoginState();
        state.FailedAttempts++;
        state.LastAttemptTime = DateTime.UtcNow;
        SaveLoginState(state);
        return state.FailedAttempts;
    }

    /// <summary>
    /// Resets the failed login attempt counter to zero
    /// </summary>
    public static void ResetFailedAttempts()
    {
        var state = LoadLoginState();
        state.FailedAttempts = 0;
        state.LastAttemptTime = null;
        state.IsPermanentlyLocked = false;
        state.LockTimestamp = null;
        SaveLoginState(state);
    }

    /// <summary>
    /// Checks if the application is currently locked (3 or more failed attempts)
    /// </summary>
    public static bool IsLocked()
    {
        var state = LoadLoginState();
        return state.IsPermanentlyLocked || state.FailedAttempts >= 3;
    }

    /// <summary>
    /// Sets a permanent lock on the application
    /// </summary>
    public static void SetPermanentLock()
    {
        var state = LoadLoginState();
        state.IsPermanentlyLocked = true;
        state.LockTimestamp = DateTime.UtcNow;
        SaveLoginState(state);
    }

    /// <summary>
    /// Clears the lock and resets all attempt tracking (for recovery purposes)
    /// </summary>
    public static void ClearLock()
    {
        ResetFailedAttempts();
    }

    /// <summary>
    /// Gets the path to the login state file
    /// </summary>
    public static string GetLoginStateFilePath()
    {
        return LOGIN_STATE_PATH;
    }

    /// <summary>
    /// Loads login state from file, or returns default if file doesn't exist
    /// </summary>
    private static LoginState LoadLoginState()
    {
        try
        {
            if (!File.Exists(LOGIN_STATE_PATH))
            {
                return new LoginState();
            }

            var json = File.ReadAllText(LOGIN_STATE_PATH);
            return JsonSerializer.Deserialize<LoginState>(json) ?? new LoginState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load login state: {ex.Message}");
            return new LoginState();
        }
    }

    /// <summary>
    /// Saves login state to file
    /// </summary>
    private static void SaveLoginState(LoginState state)
    {
        try
        {
            var directory = Path.GetDirectoryName(LOGIN_STATE_PATH);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LOGIN_STATE_PATH, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save login state: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Represents the persistent login state for tracking failed attempts and lockout status
/// </summary>
public class LoginState
{
    public int FailedAttempts { get; set; }
    public DateTime? LastAttemptTime { get; set; }
    public bool IsPermanentlyLocked { get; set; }
    public DateTime? LockTimestamp { get; set; }
}
