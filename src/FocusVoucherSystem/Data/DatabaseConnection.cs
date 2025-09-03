using Microsoft.Data.Sqlite;
using System.Data;
using System.IO;

namespace FocusVoucherSystem.Data;

/// <summary>
/// Manages SQLite database connections with WAL mode configuration
/// </summary>
public class DatabaseConnection : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseConnection(string databasePath = "Data/FocusVoucher.db")
    {
        _connectionString = $"Data Source={databasePath};Cache=Shared";
    }

    /// <summary>
    /// Gets an open database connection with WAL mode configured
    /// </summary>
    public async Task<IDbConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
            
            // Configure WAL mode for performance
            await ConfigureWalModeAsync();
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        return _connection;
    }

    /// <summary>
    /// Configures the database with WAL mode and performance optimizations
    /// </summary>
    private async Task ConfigureWalModeAsync()
    {
        if (_connection == null) return;

        var commands = new[]
        {
            "PRAGMA journal_mode=WAL;",
            "PRAGMA synchronous=NORMAL;", 
            "PRAGMA cache_size=10000;",
            "PRAGMA temp_store=MEMORY;",
            "PRAGMA mmap_size=268435456;", // 256MB
            "PRAGMA foreign_keys=ON;"
        };

        foreach (var command in commands)
        {
            using var cmd = new SqliteCommand(command, _connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Initializes the database schema if it doesn't exist
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        var connection = await GetConnectionAsync();
        
        // Read and execute the database schema
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DatabaseSchema.sql");
        if (File.Exists(schemaPath))
        {
            var schema = await File.ReadAllTextAsync(schemaPath);
            
            // Split by semicolon and execute each statement
            var statements = schema.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var statement in statements)
            {
                var trimmedStatement = statement.Trim();
                if (!string.IsNullOrEmpty(trimmedStatement))
                {
                    using var cmd = new SqliteCommand(trimmedStatement, (SqliteConnection)connection);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }

    /// <summary>
    /// Checks if the database exists and is properly initialized
    /// </summary>
    public async Task<bool> DatabaseExistsAsync()
    {
        try
        {
            var connection = await GetConnectionAsync();
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Companies';", (SqliteConnection)connection);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}