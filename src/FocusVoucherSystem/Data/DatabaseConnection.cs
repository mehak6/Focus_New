using Microsoft.Data.Sqlite;
using System.Data;
using System.IO;
using System.Text;

namespace FocusVoucherSystem.Data;

/// <summary>
/// Manages SQLite database connections with WAL mode configuration
/// </summary>
public class DatabaseConnection : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseConnection(string databasePath = null)
    {
        // Default to same folder as exe
        if (string.IsNullOrEmpty(databasePath))
        {
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            databasePath = Path.Combine(exeDirectory, "FocusVoucher.db");
        }

        _connectionString = $"Data Source={databasePath};Cache=Shared";
    }

    /// <summary>
    /// Gets a new database connection with WAL mode configured
    /// </summary>
    public async Task<IDbConnection> GetConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        // Configure WAL mode and performance settings for each connection
        await ConfigureConnectionAsync(connection);
        
        return connection;
    }

    /// <summary>
    /// Gets the shared connection (for special cases requiring transaction management)
    /// </summary>
    public async Task<IDbConnection> GetSharedConnectionAsync()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
            await ConfigureConnectionAsync(_connection);
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        return _connection;
    }

    /// <summary>
    /// Configures a database connection with WAL mode and performance optimizations
    /// </summary>
    private async Task ConfigureConnectionAsync(SqliteConnection connection)
    {
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
            using var cmd = new SqliteCommand(command, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Configures the database with WAL mode and performance optimizations (legacy method)
    /// </summary>
    private async Task ConfigureWalModeAsync()
    {
        if (_connection == null) return;
        await ConfigureConnectionAsync(_connection);
    }

    /// <summary>
    /// Initializes the database schema if it doesn't exist
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Check if database is already initialized
            if (await DatabaseExistsAsync())
            {
                return; // Database already exists and is initialized
            }

            // Create Data directory if it doesn't exist
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir);

            var connection = await GetConnectionAsync();
            
            // Read and execute the database schema
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DatabaseSchema.sql");
            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException($"Database schema file not found at: {schemaPath}");
            }

            var schema = await File.ReadAllTextAsync(schemaPath);
            
            // Split by semicolon and execute each statement - improved parsing
            var statements = SplitSqlStatements(schema);
            
            foreach (var statement in statements)
            {
                var trimmedStatement = statement.Trim();
                if (!string.IsNullOrEmpty(trimmedStatement) && 
                    !trimmedStatement.StartsWith("--") && 
                    !trimmedStatement.StartsWith("/*"))
                {
                    try
                    {
                        using var cmd = new SqliteCommand(trimmedStatement, (SqliteConnection)connection);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log the problematic statement for debugging
                        var preview = trimmedStatement.Length > 100 ? 
                            trimmedStatement.Substring(0, 100) + "..." : 
                            trimmedStatement;
                        throw new InvalidOperationException($"Failed to execute SQL statement: {preview}", ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize database", ex);
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

    /// <summary>
    /// Executes multiple operations within a transaction
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await ConfigureConnectionAsync(connection);
        
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await operation(connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes multiple operations within a transaction (void return)
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> operation)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await ConfigureConnectionAsync(connection);
        
        using var transaction = connection.BeginTransaction();
        try
        {
            await operation(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Splits SQL script into individual statements, handling multi-line statements properly
    /// </summary>
    private static List<string> SplitSqlStatements(string sql)
    {
        var statements = new List<string>();
        var currentStatement = new StringBuilder();
        var lines = sql.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmedLine) || 
                trimmedLine.StartsWith("--") || 
                trimmedLine.StartsWith("/*"))
            {
                continue;
            }
            
            currentStatement.AppendLine(line);
            
            // If line ends with semicolon, it's the end of a statement
            if (trimmedLine.EndsWith(";"))
            {
                var statement = currentStatement.ToString().Trim();
                if (!string.IsNullOrEmpty(statement))
                {
                    // Remove the trailing semicolon
                    if (statement.EndsWith(";"))
                    {
                        statement = statement.Substring(0, statement.Length - 1).Trim();
                    }
                    
                    if (!string.IsNullOrEmpty(statement))
                    {
                        statements.Add(statement);
                    }
                }
                currentStatement.Clear();
            }
        }
        
        // Add any remaining statement
        var finalStatement = currentStatement.ToString().Trim();
        if (!string.IsNullOrEmpty(finalStatement))
        {
            statements.Add(finalStatement);
        }
        
        return statements;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
