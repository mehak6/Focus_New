# Focus Voucher System v1.6.0

**Release Date**: December 30, 2025

## ✨ New Features

### Automatic Database Backup
- **Automated protection**: Database is automatically backed up every 10 minutes.
- **Location**: Backups are saved to a `FocusVoucherBackups` folder on the User's Desktop.
- **Retention**: Keeps the 5 most recent backups to save space while ensuring data safety.
- **Compression**: Backups are compressed (if enabled) to minimize disk usage.
- **Silent Operation**: Runs quietly in the background without interrupting workflow.

## 🔧 Technical Changes

- Added `DatabaseBackupService` for handling timer-based backups.
- Integrated backup service into application startup and shutdown lifecycle in `App.xaml.cs`.
- Fixed missing `e_sqlite3.dll` dependency issue in deployment.

## 📦 Installation

1. Download `FocusVoucherSystem.exe`
2. Requires .NET 8.0 Runtime
3. Run the executable
4. Your existing database will be preserved
5. **Note**: First run will create the backup folder on your Desktop.

## 🔄 Upgrade Notes

- Fully compatible with previous databases.
- No manual action required; backups start automatically.

---

**Previous Version**: v1.5.1
**Tag**: v1.6.0
