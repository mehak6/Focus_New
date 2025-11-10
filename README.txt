================================================================================
FOCUS VOUCHER SYSTEM - BLACK AND WHITE ENHANCED VERSION
Date: November 8, 2025
================================================================================

TWO EXECUTABLES AVAILABLE:

1. FocusVoucherSystem.exe (148 MB)
   - Self-contained version
   - NO .NET Runtime required
   - Run anywhere on Windows 10+
   - Run with: run.bat

2. FocusVoucherSystem_Enhanced.exe (1.7 MB) - RECOMMENDED
   - Compact version (99% smaller!)
   - Requires .NET 8.0 Runtime installed
   - Same features as self-contained version
   - Run with: run_enhanced.bat

BOTH VERSIONS HAVE THE SAME FEATURES - Choose based on your needs!

================================================================================
WHAT'S NEW - BLACK AND WHITE PRINT OPTIMIZATION
================================================================================

RECOVERY STATEMENT IMPROVEMENTS:

✓ COMPLETE BLACK AND WHITE design for professional printing
✓ AUTO-SIZED columns that fit page width perfectly
✓ MINIMAL margins (15px/20px) for maximum content
✓ CSV Export REMOVED - only PDF export now
✓ Total Outstanding amounts REMOVED from summaries
✓ Vehicle Number is BOLD and BIGGER (14pt)
✓ Black borders on all cells for clear grid

PRINT LAYOUT FEATURES:
- Header: Black background with white text
- All borders: Solid black
- All text: Black (no colored status indicators)
- Group headers: Light gray background
- Summary boxes: White with black border
- Optimized font sizes (12pt base, 16pt title)

AUTO-SIZED COLUMNS (Star Sizing):
- Balance: 1* (proportional)
- Last Credit: 1* (proportional)
- Last Date: 0.9* (slightly smaller)
- Days: 0.6* (smallest)
- Vehicle Number: 1.5* (largest - for bold text)

Columns automatically adjust to fill page width - perfect fit guaranteed!

================================================================================
HOW TO RUN
================================================================================

OPTION 1: Self-Contained Version (No .NET Runtime Needed)
   Double-click: run.bat
   OR
   Double-click: FocusVoucherSystem.exe

OPTION 2: Compact Version (Requires .NET 8.0 Runtime)
   Double-click: run_enhanced.bat
   OR
   Double-click: FocusVoucherSystem_Enhanced.exe

   If .NET Runtime is not installed:
   Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   Choose: "Desktop Runtime x64" for Windows

================================================================================
RECOVERY STATEMENT USAGE
================================================================================

1. Navigate to "Recovery" tab
2. Enter number of days (e.g., 30)
3. Enter minimum credit amount (e.g., 5000) - optional
4. Click "GENERATE"
5. Results show vehicles with:
   - No transactions in last X days
   - Last credit >= minimum amount (if specified)
   - Current balance is positive

EXPORT OPTIONS:
- PRINT: Print directly to printer
- EXPORT PDF: Save as PDF file (opens folder after export)

Note: CSV export has been removed - use PDF export instead

================================================================================
DEPLOYMENT TO ANOTHER COMPUTER
================================================================================

SELF-CONTAINED VERSION (FocusVoucherSystem.exe):
Copy files:
1. FocusVoucherSystem.exe
2. FocusVoucher.db (your database)

No .NET Runtime needed! Just copy and run.

COMPACT VERSION (FocusVoucherSystem_Enhanced.exe):
Copy files:
1. FocusVoucherSystem_Enhanced.exe
2. e_sqlite3.dll
3. QuestPdfSkia.dll
4. LatoFont folder
5. FocusVoucher.db (your database)

Install .NET 8.0 Runtime on target computer:
https://dotnet.microsoft.com/download/dotnet/8.0

================================================================================
AUTOMATIC BACKUP SYSTEM
================================================================================

The system automatically backs up your database every 10 minutes to:
Desktop\FocusVoucherBackups\

BACKUP RETENTION POLICY:
✓ Keeps only the LATEST 5 BACKUPS automatically
✓ Older backups are deleted automatically
✓ Backups are compressed (.gz format) to save space
✓ Backup files named: FocusVoucher_Backup_YYYYMMDD_HHMMSS.db.gz

Example:
- FocusVoucher_Backup_20251108_180000.db.gz (most recent)
- FocusVoucher_Backup_20251108_175000.db.gz
- FocusVoucher_Backup_20251108_174000.db.gz
- FocusVoucher_Backup_20251108_173000.db.gz
- FocusVoucher_Backup_20251108_172000.db.gz (oldest - will be deleted on next backup)

The system automatically manages backups - no manual cleanup needed!

================================================================================
TECHNICAL DETAILS
================================================================================

Changes Made:
- PrintService.cs: Complete BLACK AND WHITE redesign
  * Star-sized columns for auto-fit
  * Black borders and headers
  * Removed colored status indicators
  * Minimal margins (15px/20px)
  * Optimized font sizes

- RecoveryViewModel.cs: Removed ExportCsv method
- RecoveryView.xaml: Removed EXPORT CSV button
- DatabaseBackupService.cs: Set to keep only 5 latest backups
- All versions built with same source code

Build Configurations:
- FocusVoucherSystem.exe:
  * Configuration: Release
  * Self-Contained: true
  * Size: 148 MB

- FocusVoucherSystem_Enhanced.exe:
  * Configuration: Release-Compact
  * Self-Contained: false
  * Size: 1.7 MB
  * Requires .NET 8.0 Runtime

================================================================================
SUPPORT
================================================================================

If you encounter any issues:
1. Try closing and restarting the application
2. For compact version: Verify .NET 8.0 Runtime is installed
3. Check that required DLL files are in the same folder
4. Ensure FocusVoucher.db is accessible

Both executables have identical features - choose based on:
- Self-contained: No runtime needed, larger file
- Compact: Requires runtime, much smaller file

================================================================================
