# Focus Voucher System v1.4.0

**Release Date**: November 19, 2025

## ✨ New Features

### Transaction Comparison
- **New comparison feature** in Search tab to identify unmatched debits and credits
- Select a vehicle and click "COMPARE" to automatically analyze transactions
- Finds debits without matching credits (and vice versa) of the same amount

### Visual Highlighting
- **Unmatched transactions** are highlighted with ⚠ warning icons
- **Yellow accent bar** on left side for unmatched debits
- **Red accent bar** on left side for unmatched credits
- Clear visual indication of discrepancies

### Filter Toggle
- **"Show Only Unmatched"** button to filter comparison results
- Quickly focus on problematic transactions
- Easy toggle on/off to see all data

## 🐛 Bug Fixes

### Reports Date Filtering
- Fixed reports not showing data on specific dates
- Improved SQLite date range queries with proper time handling
- Now uses inclusive date ranges (start date 00:00:00 to end date 23:59:59)

### Auto-Backup Status
- Fixed backup status not updating on main screen
- Resolved WPF threading issue with proper Dispatcher.Invoke
- Real-time backup time display now works correctly

### Merge Dialog Visibility
- Fixed text being cut off in merge dialogs
- Increased dialog width from 600px to 800px
- Added text wrapping with proper constraints
- Made dialogs resizable for better visibility

### DatePicker Input Issues
- Fixed dates changing unexpectedly when inputting in Reports tab
- Removed custom formatting code that was interfering with input
- DatePickers now use system culture format naturally

## 🎨 UI Improvements

### Date Format Consistency
- **DD/MM/YYYY format** consistently displayed in Voucher Entry tab
- **DD/MM/YYYY format** in Reports tab DatePickers
- Matches Indian date format standards (en-IN culture)

### Enhanced Dialogs
- Better text wrapping in merge dialogs
- Improved visibility of merge summary information
- Resizable dialog windows

### Comparison Visual Feedback
- Color-coded highlighting for unmatched transactions
- Intuitive yellow (debit) and red (credit) color scheme
- Warning icons for quick identification

## 🔧 Technical Changes

### Database Optimization
- Optimized SQLite date range queries
- Changed from DATE() function to direct date comparison
- Better performance with proper time component handling

### Threading Improvements
- Proper WPF threading with Dispatcher.Invoke for UI updates
- Fixed Timer.Elapsed background thread issues
- Ensures UI updates happen on correct thread

### DatePicker Handling
- Simplified DatePicker implementation
- Removed complex visual tree manipulation
- Uses natural WPF binding without interference

### Code Quality
- Enhanced error handling in date comparisons
- Better separation of concerns in view models
- Improved code maintainability

## 📦 Installation

1. Download `FocusVoucherSystem.exe`
2. Requires .NET 8.0 Runtime (if not already installed)
3. Run the executable
4. Your existing database will be automatically updated

## 🔄 Upgrade Notes

- This version is fully compatible with existing databases
- No manual migration required
- All existing data will be preserved

## 📝 Commits Included

- Transaction comparison feature with enhanced UI
- Auto-backup status update fix
- Merge dialog visibility improvements
- DD/MM/YYYY date format implementation
- Reports date filtering fixes
- DatePicker input issue resolution

---

**Previous Version**: v1.3.x
**GitHub Repository**: https://github.com/mehak6/Focus_New
**Tag**: v1.4.0
