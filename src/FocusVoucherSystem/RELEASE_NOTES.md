Release Notes

Version: v0.2.0
Date: 2025-09-04

Highlights
- Added startup Company Selection window with create/select and safe delete.
- File menu simplified to a single “Select Company…” action that reopens the selector.
- Reports screen: removed Excel/PDF export buttons; CSV and Print retained.
- Voucher Entry: Delete button enabled and wired; deletes selected voucher with confirmation.
- Menu dropdown styling adjusted for better visibility on light submenu background.
- Startup stability: ensure app does not close when the selection dialog closes (sets ShutdownMode appropriately).

Technical
- New: `Views/CompanySelectionWindow.*`, `ViewModels/CompanySelectionViewModel.cs`, `Views/CompanyInputDialog.*`.
- View changes: `MainWindow.xaml`, `Views/ReportsView.xaml`.
- ViewModel changes: `MainWindowViewModel.cs`, `VoucherEntryViewModel.cs`.
- Services: minor `App.xaml.cs` startup logic adjustment.

Build
- `dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true`
- Artifacts in `bin/Release/net8.0-windows/win-x64/publish/`.

