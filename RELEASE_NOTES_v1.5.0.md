# Focus Voucher System v1.5.0

**Release Date**: November 20, 2025

## ✨ New Features

### Search Voucher Pagination
- **Page-based navigation** for vehicles with more than 500 vouchers
- Navigation buttons: First (⏮), Previous (◀), Next (▶), Last (⏭)
- Page counter showing "Page X of Y"
- Controls auto-hide when only 1 page exists

### Quick Vehicle Creation
- **Create vehicle directly** from Voucher Entry tab when vehicle not found
- Dialog prompt asks "Do you want to create this vehicle?"
- Vehicle is automatically created and selected for immediate use
- No need to switch to Vehicle Management tab

## 🐛 Bug Fixes

### WPF Threading Fix
- Fixed "calling thread cannot access this object" error when loading vouchers
- All UI updates now properly dispatched to UI thread
- Improved stability for large voucher datasets

## 🔧 Technical Changes

- Added `GreaterThanOneConverter` for pagination visibility
- Updated `LoadVouchersForVehicleAsync` with proper Dispatcher.InvokeAsync
- Modified `HandleVehicleSearchEnter` to support async vehicle creation

## 📦 Installation

1. Download `FocusVoucherSystem.exe`
2. Requires .NET 8.0 Runtime
3. Run the executable
4. Your existing database will be preserved

## 🔄 Upgrade Notes

- Fully compatible with v1.4.0 databases
- No manual migration required

---

**Previous Version**: v1.4.0
**GitHub Repository**: https://github.com/mehak6/Focus_New
**Tag**: v1.5.0
