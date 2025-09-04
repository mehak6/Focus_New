Focus Voucher System (WPF, .NET 8)

Overview
- Modern WPF replacement for legacy voucher management.
- SQLite persistence with Dapper and MVVM (CommunityToolkit.Mvvm).
- Startup company selection, voucher entry, vehicles, search, and reports.

Download
- Latest release: https://github.com/kta136/Focus_New/releases/latest
- Windows: download the ZIP, extract, then run `FocusVoucherSystem.exe`.

What's New
- Company selection at startup: choose or create a company before entering the app.
- Delete company at startup: safely deletes company, vouchers, and vehicles.
- File → Select Company…: reopen the company selector at any time.
- Reports: Excel/PDF export buttons removed; CSV and Print remain.
- Voucher Entry: Delete button enabled; deletes selected voucher with confirmation.

Getting Started (Development)
- Prerequisites: .NET 8 SDK (Windows) and Visual Studio/VS Code.
- Open `FocusVoucherSystem.sln` and set configuration to `Debug`.
- Press F5 to run.

Build/Publish (Release)
- Command line:
  - `dotnet restore`
  - `dotnet build -c Release`
  - `dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true`
- Output: `bin/Release/net8.0-windows/win-x64/publish/`

Key Features
- Voucher entry with Dr/Cr, vehicle linking, live totals, and search.
- Vehicle management.
- Reports (Day Book, Consolidated, Vehicle Ledger, Trial Balance) with CSV export and printing.
- Hotkeys (F1..F9, Esc) via HotkeyService.

Important Shortcuts
- F2: New voucher
- F5: Save voucher
- F8: Delete selected voucher
- File → Select Company…: change or create companies

Data Location
- SQLite database file: `FocusVoucher.db` created in the app output directory.

Tech Stack
- WPF, .NET 8, CommunityToolkit.Mvvm, Dapper, Microsoft.Data.Sqlite.

Contributing
- Fork and open a PR. Please keep changes focused and include a brief rationale.

License
- Proprietary (internal project). Do not distribute without permission.

