using FocusVoucherSystem.Data;
using FocusVoucherSystem.Data.Repositories;
using Dapper;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Central data service that provides access to all repositories
/// </summary>
public class DataService : IDisposable
{
    private readonly DatabaseConnection _dbConnection;
    private readonly DatabaseBackupService _backupService;

    public DataService(string? databasePath = null, string? encryptionKey = null)
    {
        _dbConnection = new DatabaseConnection(databasePath, encryptionKey);

        // Initialize repositories
        Companies = new CompanyRepository(_dbConnection);
        Vehicles = new VehicleRepository(_dbConnection);
        Vouchers = new VoucherRepository(_dbConnection);
        Settings = new SettingRepository(_dbConnection);

        // Initialize automatic backup service
        _backupService = new DatabaseBackupService();
    }

    /// <summary>
    /// Company repository for managing companies
    /// </summary>
    public ICompanyRepository Companies { get; }

    /// <summary>
    /// Vehicle repository for managing vehicles/accounts
    /// </summary>
    public IVehicleRepository Vehicles { get; }

    /// <summary>
    /// Voucher repository for managing voucher entries
    /// </summary>
    public IVoucherRepository Vouchers { get; }

    /// <summary>
    /// Setting repository for managing application settings
    /// </summary>
    public ISettingRepository Settings { get; }

    /// <summary>
    /// Initializes the database schema if needed
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        var exists = await _dbConnection.DatabaseExistsAsync();
        if (!exists)
        {
            await _dbConnection.InitializeDatabaseAsync();
        }
        else
        {
            await ApplyPerformanceOptimizationsAsync();
            await ApplyDatabaseMigrationsAsync();
        }
    }

    /// <summary>
    /// Applies performance optimizations to existing databases
    /// </summary>
    private async Task ApplyPerformanceOptimizationsAsync()
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();

            // Check if the optimized index already exists
            const string checkIndexSql = @"
                SELECT COUNT(*) FROM sqlite_master
                WHERE type='index' AND name='idx_vouchers_daybook_optimized'";

            var indexExists = await connection.QuerySingleAsync<int>(checkIndexSql);

            if (indexExists == 0)
            {
                // Create the optimized index for day book reports
                const string createIndexSql = @"
                    CREATE INDEX idx_vouchers_daybook_optimized
                    ON Vouchers(CompanyId, Date, VehicleId, VoucherId)";

                await connection.ExecuteAsync(createIndexSql);
                System.Diagnostics.Debug.WriteLine("Added optimized index for day book reports");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply performance optimizations: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies database migrations for schema changes
    /// </summary>
    private async Task ApplyDatabaseMigrationsAsync()
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();

            // Check if Description column exists in Vehicles table
            const string checkColumnSql = @"
                SELECT COUNT(*)
                FROM pragma_table_info('Vehicles')
                WHERE name='Description'";

            var descriptionColumnExists = await connection.QuerySingleAsync<int>(checkColumnSql);

            if (descriptionColumnExists > 0)
            {
                // Rename Description column to Narration
                const string renameColumnSql = @"
                    ALTER TABLE Vehicles RENAME COLUMN Description TO Narration";

                await connection.ExecuteAsync(renameColumnSql);
                System.Diagnostics.Debug.WriteLine("Renamed Description column to Narration in Vehicles table");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply database migrations: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all vouchers and vehicles for a company and resets voucher numbering
    /// </summary>
    public async Task ClearCompanyDataAsync(int companyId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        using var tx = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync("DELETE FROM Vouchers WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, tx);
            await connection.ExecuteAsync("DELETE FROM Vehicles WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, tx);
            await connection.ExecuteAsync("UPDATE Companies SET LastVoucherNumber = 0 WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            try
            {
                tx.Rollback();
            }
            catch (Exception rollbackEx)
            {
                // Log rollback failure
                System.Diagnostics.Debug.WriteLine($"Transaction rollback failed: {rollbackEx.Message}");
            }
            throw; // Re-throw original exception
        }
    }

    /// <summary>
    /// Gets the database connection for advanced operations
    /// </summary>
    public async Task<System.Data.IDbConnection> GetConnectionAsync()
    {
        return await _dbConnection.GetConnectionAsync();
    }

    /// <summary>
    /// Performs a database health check
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();
            var companyCount = await Companies.CountAsync();
            return companyCount >= 0; // Any count (including 0) means database is accessible
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets database statistics for monitoring
    /// </summary>
    public async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        return new DatabaseStats
        {
            CompanyCount = await Companies.CountAsync(),
            VehicleCount = await Vehicles.CountAsync(),
            VoucherCount = await Vouchers.CountAsync(),
            SettingCount = await Settings.CountAsync(),
            LastChecked = DateTime.Now
        };
    }

    /// <summary>
    /// Forces an immediate database backup
    /// </summary>
    public void ForceBackup()
    {
        _backupService?.ForceBackup();
    }

    /// <summary>
    /// Gets the backup service for advanced backup operations
    /// </summary>
    public DatabaseBackupService BackupService => _backupService;

    /// <summary>
    /// Performs database maintenance operations to reclaim space and optimize performance
    /// </summary>
    public async Task PerformDatabaseMaintenanceAsync()
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();

            // Execute VACUUM to reclaim unused space
            await connection.ExecuteAsync("VACUUM");

            // Analyze tables to update query planner statistics
            await connection.ExecuteAsync("ANALYZE");

            System.Diagnostics.Debug.WriteLine("Database maintenance completed: VACUUM and ANALYZE executed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database maintenance failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets database file size information
    /// </summary>
    public async Task<DatabaseSizeInfo> GetDatabaseSizeInfoAsync()
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();

            // Get page count and page size
            var pageCount = await connection.QuerySingleAsync<long>("PRAGMA page_count");
            var pageSize = await connection.QuerySingleAsync<long>("PRAGMA page_size");
            var freeListCount = await connection.QuerySingleAsync<long>("PRAGMA freelist_count");

            var totalSize = pageCount * pageSize;
            var usedSize = (pageCount - freeListCount) * pageSize;
            var freeSpace = freeListCount * pageSize;

            return new DatabaseSizeInfo
            {
                TotalSizeBytes = totalSize,
                UsedSizeBytes = usedSize,
                FreeSpaceBytes = freeSpace,
                PageCount = pageCount,
                PageSize = pageSize,
                FreeListCount = freeListCount
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get database size info: {ex.Message}");
            return new DatabaseSizeInfo();
        }
    }

    public void Dispose()
    {
        _backupService?.Dispose();
        _dbConnection?.Dispose();
    }
}

/// <summary>
/// Database statistics for monitoring and debugging
/// </summary>
public class DatabaseStats
{
    public int CompanyCount { get; set; }
    public int VehicleCount { get; set; }
    public int VoucherCount { get; set; }
    public int SettingCount { get; set; }
    public DateTime LastChecked { get; set; }

    public override string ToString()
    {
        return $"Companies: {CompanyCount}, Vehicles: {VehicleCount}, " +
               $"Vouchers: {VoucherCount}, Settings: {SettingCount} " +
               $"(as of {LastChecked:yyyy-MM-dd HH:mm:ss})";
    }
}

/// <summary>
/// Database size and space utilization information
/// </summary>
public class DatabaseSizeInfo
{
    public long TotalSizeBytes { get; set; }
    public long UsedSizeBytes { get; set; }
    public long FreeSpaceBytes { get; set; }
    public long PageCount { get; set; }
    public long PageSize { get; set; }
    public long FreeListCount { get; set; }

    public double TotalSizeMB => TotalSizeBytes / (1024.0 * 1024.0);
    public double UsedSizeMB => UsedSizeBytes / (1024.0 * 1024.0);
    public double FreeSpaceMB => FreeSpaceBytes / (1024.0 * 1024.0);
    public double FreeSpacePercentage => TotalSizeBytes > 0 ? (FreeSpaceBytes * 100.0) / TotalSizeBytes : 0;

    public override string ToString()
    {
        return $"Total: {TotalSizeMB:F2} MB, Used: {UsedSizeMB:F2} MB, " +
               $"Free: {FreeSpaceMB:F2} MB ({FreeSpacePercentage:F1}% free)";
    }
}
