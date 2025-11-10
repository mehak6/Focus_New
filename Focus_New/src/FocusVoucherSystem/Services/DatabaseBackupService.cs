using System.IO;
using System.IO.Compression;
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

    // Backup settings
    public bool EnableCompression { get; set; } = true;
    public int MaxBackupCount { get; set; } = 5;  // Keep only latest 5 backups

    // Smart retention policy settings - DISABLED (using simple retention instead)
    public bool EnableSmartRetention { get; set; } = false;  // Use simple retention (keep latest 5)
    public int HourlyRetentionHours { get; set; } = 24;      // Keep hourly backups for 24 hours
    public int DailyRetentionDays { get; set; } = 30;        // Keep daily backups for 30 days
    public int WeeklyRetentionWeeks { get; set; } = 12;      // Keep weekly backups for 12 weeks
    public int BackupIntervalMinutes { get; set; } = 10;     // Backup every 10 minutes

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

        // Setup timer based on configured interval
        _backupTimer = new Timer(BackupIntervalMinutes * 60 * 1000); // Convert minutes to milliseconds
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
    /// Creates a backup of the database with timestamp and optional compression
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

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (EnableCompression)
            {
                CreateCompressedBackup(timestamp);
            }
            else
            {
                CreateUncompressedBackup(timestamp);
            }

            // Clean up old backups
            CleanupOldBackups();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Error creating backup - {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a compressed backup using GZip compression
    /// </summary>
    private void CreateCompressedBackup(string timestamp)
    {
        var backupFileName = $"FocusVoucher_Backup_{timestamp}.db.gz";
        var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

        // Compress main database file
        using (var originalFile = new FileStream(_databasePath, FileMode.Open, FileAccess.Read))
        using (var compressedFile = new FileStream(backupFilePath, FileMode.Create))
        using (var compressionStream = new GZipStream(compressedFile, CompressionMode.Compress))
        {
            originalFile.CopyTo(compressionStream);
        }

        // Compress WAL file if it exists
        var walPath = _databasePath + "-wal";
        if (File.Exists(walPath))
        {
            var walBackupPath = Path.Combine(_backupDirectory, $"FocusVoucher_Backup_{timestamp}.db-wal.gz");
            using (var originalWal = new FileStream(walPath, FileMode.Open, FileAccess.Read))
            using (var compressedWal = new FileStream(walBackupPath, FileMode.Create))
            using (var compressionStream = new GZipStream(compressedWal, CompressionMode.Compress))
            {
                originalWal.CopyTo(compressionStream);
            }
        }

        // Compress SHM file if it exists
        var shmPath = _databasePath + "-shm";
        if (File.Exists(shmPath))
        {
            var shmBackupPath = Path.Combine(_backupDirectory, $"FocusVoucher_Backup_{timestamp}.db-shm.gz");
            using (var originalShm = new FileStream(shmPath, FileMode.Open, FileAccess.Read))
            using (var compressedShm = new FileStream(shmBackupPath, FileMode.Create))
            using (var compressionStream = new GZipStream(compressedShm, CompressionMode.Compress))
            {
                originalShm.CopyTo(compressionStream);
            }
        }

        System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Compressed backup created - {backupFileName}");
    }

    /// <summary>
    /// Creates an uncompressed backup (legacy method)
    /// </summary>
    private void CreateUncompressedBackup(string timestamp)
    {
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

        System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Uncompressed backup created - {backupFileName}");
    }

    /// <summary>
    /// Implements smart backup retention policy with tiered retention
    /// </summary>
    private void CleanupOldBackups()
    {
        try
        {
            if (EnableSmartRetention)
            {
                ApplySmartRetentionPolicy();
            }
            else
            {
                ApplySimpleRetentionPolicy();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Error cleaning up old backups - {ex.Message}");
        }
    }

    /// <summary>
    /// Applies smart tiered retention policy: hourly for 24h, daily for 30 days, weekly for 12 weeks
    /// </summary>
    private void ApplySmartRetentionPolicy()
    {
        var allBackupFiles = GetAllBackupFiles();
        var now = DateTime.Now;
        var filesToKeep = new HashSet<string>();

        // Group files by time periods
        var filesByTime = allBackupFiles
            .Select(f => new { File = f, Time = File.GetCreationTime(f) })
            .OrderByDescending(x => x.Time)
            .ToList();

        // Keep all backups from the last hour (most recent)
        var lastHourFiles = filesByTime.Where(x => (now - x.Time).TotalHours < 1).Select(x => x.File);
        foreach (var file in lastHourFiles) filesToKeep.Add(file);

        // Keep hourly backups for the specified retention period
        var hourlyFiles = filesByTime
            .Where(x => (now - x.Time).TotalHours < HourlyRetentionHours)
            .GroupBy(x => new { x.Time.Year, x.Time.Month, x.Time.Day, x.Time.Hour })
            .Select(g => g.OrderByDescending(x => x.Time).First().File);
        foreach (var file in hourlyFiles) filesToKeep.Add(file);

        // Keep daily backups for the specified retention period
        var dailyFiles = filesByTime
            .Where(x => (now - x.Time).TotalDays < DailyRetentionDays)
            .GroupBy(x => new { x.Time.Year, x.Time.Month, x.Time.Day })
            .Select(g => g.OrderByDescending(x => x.Time).First().File);
        foreach (var file in dailyFiles) filesToKeep.Add(file);

        // Keep weekly backups for the specified retention period
        var weeklyFiles = filesByTime
            .Where(x => (now - x.Time).TotalDays < WeeklyRetentionWeeks * 7)
            .GroupBy(x => GetWeekOfYear(x.Time))
            .Select(g => g.OrderByDescending(x => x.Time).First().File);
        foreach (var file in weeklyFiles) filesToKeep.Add(file);

        // Delete files not in the keep list
        var filesToDelete = allBackupFiles.Except(filesToKeep);
        DeleteBackupFiles(filesToDelete);

        System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Smart cleanup completed, keeping {filesToKeep.Count} backups");
    }

    /// <summary>
    /// Applies simple retention policy based on MaxBackupCount
    /// </summary>
    private void ApplySimpleRetentionPolicy()
    {
        var allBackupFiles = GetAllBackupFiles();
        var filesToDelete = allBackupFiles
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(MaxBackupCount);

        DeleteBackupFiles(filesToDelete);
        System.Diagnostics.Debug.WriteLine($"DatabaseBackupService: Simple cleanup completed, keeping {MaxBackupCount} most recent");
    }

    /// <summary>
    /// Gets all backup files (both compressed and uncompressed)
    /// </summary>
    private List<string> GetAllBackupFiles()
    {
        var allBackupFiles = new List<string>();
        allBackupFiles.AddRange(Directory.GetFiles(_backupDirectory, "FocusVoucher_Backup_*.db"));
        allBackupFiles.AddRange(Directory.GetFiles(_backupDirectory, "FocusVoucher_Backup_*.db.gz"));
        return allBackupFiles;
    }

    /// <summary>
    /// Deletes backup files and their associated WAL/SHM files
    /// </summary>
    private void DeleteBackupFiles(IEnumerable<string> filesToDelete)
    {
        foreach (var oldBackup in filesToDelete)
        {
            try
            {
                File.Delete(oldBackup);

                // Also delete associated WAL and SHM files (both compressed and uncompressed)
                var baseFileName = oldBackup.Replace(".gz", ""); // Remove .gz if present
                var walFile = baseFileName + "-wal";
                var shmFile = baseFileName + "-shm";
                var walFileGz = walFile + ".gz";
                var shmFileGz = shmFile + ".gz";

                if (File.Exists(walFile)) File.Delete(walFile);
                if (File.Exists(shmFile)) File.Delete(shmFile);
                if (File.Exists(walFileGz)) File.Delete(walFileGz);
                if (File.Exists(shmFileGz)) File.Delete(shmFileGz);
            }
            catch
            {
                // Ignore errors when deleting old backups
            }
        }
    }

    /// <summary>
    /// Gets the week of year for grouping weekly backups
    /// </summary>
    private string GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var weekOfYear = culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        return $"{date.Year}-W{weekOfYear:00}";
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