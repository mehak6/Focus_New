using System.IO;
using Timer = System.Timers.Timer;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Service for automatic database backup to desktop every 10 minutes
/// </summary>
public class DatabaseBackupService : IDisposable
{
    private readonly Timer _backupTimer;
    private readonly string _databasePath;
    private readonly string _backupDirectory;
    private bool _disposed = false;

    public DatabaseBackupService()
    {
        // Get the database path (same as application directory)
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _databasePath = Path.Combine(appDirectory, "FocusVoucher.db");

        // Set backup directory to Desktop
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _backupDirectory = Path.Combine(desktopPath, "FocusVoucherBackups");

        // Create backup directory if it doesn't exist
        Directory.CreateDirectory(_backupDirectory);

        // Setup timer for 10 minutes (600,000 milliseconds)
        _backupTimer = new Timer(600000); // 10 minutes
        _backupTimer.Elapsed += OnBackupTimer;
        _backupTimer.AutoReset = true;

        // Start the timer
        _backupTimer.Start();

        // Create initial backup
        CreateBackup();
    }

    /// <summary>
    /// Timer event handler for automatic backup
    /// </summary>
    private void OnBackupTimer(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            CreateBackup();
        }
        catch (Exception ex)
        {
            // Log the error but don't stop the service
            System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Backup failed - {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a backup of the database with timestamp
    /// </summary>
    private void CreateBackup()
    {
        try
        {
            // Check if database file exists
            if (!File.Exists(_databasePath))
            {
                System.Diagnostics.Debug.WriteLine("DatabaseBackupService: Database file not found for backup");
                return;
            }

            // Create backup filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"FocusVoucher_Backup_{timestamp}.db";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

            // Copy database file to backup location
            File.Copy(_databasePath, backupFilePath, true);

            // Also copy WAL and SHM files if they exist
            var walPath = _databasePath + "-wal";
            var shmPath = _databasePath + "-shm";

            if (File.Exists(walPath))
            {
                var walBackupPath = Path.Combine(_backupDirectory, $"FocusVoucher_Backup_{timestamp}.db-wal");
                File.Copy(walPath, walBackupPath, true);
            }

            if (File.Exists(shmPath))
            {
                var shmBackupPath = Path.Combine(_backupDirectory, $"FocusVoucher_Backup_{timestamp}.db-shm");
                File.Copy(shmPath, shmBackupPath, true);
            }

            System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Backup created - {backupFileName}");

            // Clean up old backups (keep only last 20 backups to save space)
            CleanupOldBackups();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Error creating backup - {ex.Message}");
        }
    }

    /// <summary>
    /// Removes old backup files, keeping only the most recent 20
    /// </summary>
    private void CleanupOldBackups()
    {
        try
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, "FocusVoucher_Backup_*.db")
                                     .OrderByDescending(f => File.GetCreationTime(f))
                                     .Skip(20) // Keep 20 most recent
                                     .ToArray();

            foreach (var oldBackup in backupFiles)
            {
                try
                {
                    File.Delete(oldBackup);

                    // Also delete associated WAL and SHM files
                    var walFile = oldBackup + "-wal";
                    var shmFile = oldBackup + "-shm";

                    if (File.Exists(walFile)) File.Delete(walFile);
                    if (File.Exists(shmFile)) File.Delete(shmFile);
                }
                catch
                {
                    // Ignore errors when deleting old backups
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Error cleaning up old backups - {ex.Message}");
        }
    }

    /// <summary>
    /// Forces an immediate backup
    /// </summary>
    public void ForceBackup()
    {
        CreateBackup();
    }

    /// <summary>
    /// Gets the backup directory path
    /// </summary>
    public string BackupDirectory => _backupDirectory;

    /// <summary>
    /// Stops the backup service
    /// </summary>
    public void Stop()
    {
        _backupTimer?.Stop();
    }

    /// <summary>
    /// Starts the backup service
    /// </summary>
    public void Start()
    {
        _backupTimer?.Start();
    }

    /// <summary>
    /// Gets whether the backup service is running
    /// </summary>
    public bool IsRunning => _backupTimer?.Enabled ?? false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _backupTimer?.Stop();
            _backupTimer?.Dispose();
            _disposed = true;
        }
    }
}