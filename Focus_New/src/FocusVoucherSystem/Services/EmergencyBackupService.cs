using System.IO;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Handles emergency backup operations when login attempts are exceeded
/// Moves database and backups to secure hidden location
/// </summary>
public class EmergencyBackupService
{
    private const string SECURE_BACKUP_ROOT = "FocusVoucherSystem\\SecureBackups";
    private readonly string _appDirectory;
    private readonly string _desktopBackupDirectory;

    public EmergencyBackupService()
    {
        _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _desktopBackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "FocusVoucherBackups"
        );
    }

    /// <summary>
    /// Creates an emergency backup and hides all data in secure location
    /// Returns the path to the backup folder
    /// </summary>
    public async Task<string> CreateEmergencyBackup()
    {
        try
        {
            // 1. Create timestamped secure backup folder
            var backupPath = GetHiddenBackupPath();
            Directory.CreateDirectory(backupPath);

            // 2. Copy database files to secure location
            await CopyDatabaseFiles(backupPath);

            // 3. Move Desktop backups to secure location
            var desktopBackupsPath = await MoveDesktopBackups(backupPath);

            // 4. Create recovery instructions
            CreateRecoveryInstructions(backupPath, desktopBackupsPath);

            // 5. Delete original database files
            DeleteOriginalDatabase();

            System.Diagnostics.Debug.WriteLine($"Emergency backup created at: {backupPath}");
            return backupPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Emergency backup failed: {ex.Message}");
            throw new InvalidOperationException("Failed to create emergency backup", ex);
        }
    }

    /// <summary>
    /// Returns the secure hidden backup path with timestamp
    /// </summary>
    private string GetHiddenBackupPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var secureBackupRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            SECURE_BACKUP_ROOT
        );

        return Path.Combine(secureBackupRoot, $"{timestamp}_EMERGENCY");
    }

    /// <summary>
    /// Copies database files (.db, .db-wal, .db-shm) to secure backup location
    /// </summary>
    private async Task CopyDatabaseFiles(string backupPath)
    {
        var databaseFiles = new[]
        {
            "FocusVoucher.db",
            "FocusVoucher.db-wal",
            "FocusVoucher.db-shm"
        };

        foreach (var fileName in databaseFiles)
        {
            var sourcePath = Path.Combine(_appDirectory, fileName);
            var destPath = Path.Combine(backupPath, fileName);

            if (File.Exists(sourcePath))
            {
                await Task.Run(() => File.Copy(sourcePath, destPath, true));
                System.Diagnostics.Debug.WriteLine($"Copied {fileName} to secure backup");
            }
        }
    }

    /// <summary>
    /// Moves all Desktop backup files to secure location
    /// Returns the path where backups were moved
    /// </summary>
    private async Task<string> MoveDesktopBackups(string targetFolder)
    {
        var desktopBackupTarget = Path.Combine(targetFolder, "moved_desktop_backups");

        try
        {
            if (!Directory.Exists(_desktopBackupDirectory))
            {
                System.Diagnostics.Debug.WriteLine("No Desktop backups found to move");
                return string.Empty;
            }

            Directory.CreateDirectory(desktopBackupTarget);

            var backupFiles = Directory.GetFiles(_desktopBackupDirectory, "*.*", SearchOption.AllDirectories);

            if (backupFiles.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("Desktop backup folder is empty");
                return string.Empty;
            }

            foreach (var sourceFile in backupFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                var destFile = Path.Combine(desktopBackupTarget, fileName);

                await Task.Run(() => File.Move(sourceFile, destFile, true));
                System.Diagnostics.Debug.WriteLine($"Moved backup: {fileName}");
            }

            // Remove empty Desktop backup directory
            if (Directory.GetFiles(_desktopBackupDirectory).Length == 0)
            {
                Directory.Delete(_desktopBackupDirectory, true);
                System.Diagnostics.Debug.WriteLine("Removed empty Desktop backup folder");
            }

            return desktopBackupTarget;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to move Desktop backups: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Creates recovery instructions file in the backup folder
    /// </summary>
    private void CreateRecoveryInstructions(string backupPath, string desktopBackupsPath)
    {
        var instructionsPath = Path.Combine(backupPath, "recovery_instructions.txt");
        var loginStatePath = SecurityService.GetLoginStateFilePath();

        var instructions = $@"FOCUS VOUCHER SYSTEM - EMERGENCY BACKUP
========================================

Your application was locked after 3 failed login attempts.

BACKUP LOCATION: {backupPath}

TO RECOVER YOUR DATA:
1. Close the application
2. Navigate to: {_appDirectory}
3. Copy these files from this backup folder to the app directory:
   - FocusVoucher.db
   - FocusVoucher.db-wal (if exists)
   - FocusVoucher.db-shm (if exists)

4. Delete this file to reset the lockout:
   {loginStatePath}

5. Restart the application and enter the correct password

DESKTOP BACKUPS:
{(string.IsNullOrEmpty(desktopBackupsPath)
    ? "No Desktop backups were found to move."
    : $"Your automatic backups from Desktop were moved here for security:\n{desktopBackupsPath}")}

IMPORTANT NOTES:
- Keep this backup folder safe - it contains all your voucher data
- Only restore after you are certain you know the correct password
- If you continue to have issues, contact support

Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";

        File.WriteAllText(instructionsPath, instructions);
        System.Diagnostics.Debug.WriteLine("Recovery instructions created");
    }

    /// <summary>
    /// Deletes the original database files from application directory
    /// </summary>
    private void DeleteOriginalDatabase()
    {
        var databaseFiles = new[]
        {
            "FocusVoucher.db",
            "FocusVoucher.db-wal",
            "FocusVoucher.db-shm"
        };

        foreach (var fileName in databaseFiles)
        {
            var filePath = Path.Combine(_appDirectory, fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"Deleted original file: {fileName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete {fileName}: {ex.Message}");
                // Continue even if deletion fails - backup is safe
            }
        }
    }
}
