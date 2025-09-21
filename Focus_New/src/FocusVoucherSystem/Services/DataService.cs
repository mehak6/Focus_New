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

    public DataService(string? databasePath = null)
    {
        _dbConnection = new DatabaseConnection(databasePath);
        
        // Initialize repositories
        Companies = new CompanyRepository(_dbConnection);
        Vehicles = new VehicleRepository(_dbConnection);
        Vouchers = new VoucherRepository(_dbConnection);
        Settings = new SettingRepository(_dbConnection);
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
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
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

    public void Dispose()
    {
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
