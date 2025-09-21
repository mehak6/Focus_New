# Technical Architecture - Focus Voucher System

## Overview
Modern C# WPF application replacing legacy DOS voucher management system while preserving workflow and hotkey functionality.

## Technology Stack

### Core Framework
- **.NET 8 (LTS)** - Target x64 only for optimal performance
- **WPF** - Desktop UI framework with native Windows integration
- **C# 12** - Latest language features for clean, maintainable code

### Data Layer
- **SQLite** - Single-file database with WAL mode for performance
- **Dapper** - Lightweight ORM for fast data access
- **Microsoft.Data.Sqlite** - Official SQLite provider

### MVVM Framework
- **CommunityToolkit.Mvvm** - Lightweight MVVM toolkit
- **ObservableProperty** source generators for ViewModels
- **RelayCommand** for command binding

### UI Enhancement
- **ModernWpfUI** (optional) - Modern controls and dark mode support
- **WPF DataGrid** - Native virtualized grid for performance

### Export & Reporting
- **QuestPDF** - PDF generation library
- **ClosedXML** - Excel export functionality
- **System.IO.Compression** - Backup compression

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │  MainWindow     │  │  VoucherEntry   │  │   Reports    │ │
│  │  (Navigation)   │  │   (DataGrid)    │  │  (Export)    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     ViewModel Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ MainViewModel   │  │VoucherViewModel │  │ReportViewModel│ │
│  │ (Commands)      │  │ (CRUD Logic)    │  │ (Filtering)  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Business Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ VoucherService  │  │ ReportService   │  │ ExportService│ │
│  │ (Business Logic)│  │ (Calculations)  │  │ (PDF/CSV)    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Data Access Layer                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ VoucherRepository│  │ VehicleRepository│  │CompanyRepo   │ │
│  │ (Dapper + SQL)  │  │ (CRUD + Search) │  │ (Settings)   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Database Layer                         │
│              SQLite Database (WAL Mode)                    │
│     Companies | Vehicles | Vouchers | Settings             │
└─────────────────────────────────────────────────────────────┘
```

## Design Patterns

### MVVM Pattern Implementation
```csharp
// ViewModel with CommunityToolkit.Mvvm
[ObservableObject]
public partial class VoucherEntryViewModel
{
    [ObservableProperty]
    private ObservableCollection<Voucher> vouchers = [];
    
    [RelayCommand]
    private async Task AddVoucherAsync()
    {
        // Business logic
    }
}
```

### Repository Pattern
```csharp
public interface IVoucherRepository
{
    Task<IEnumerable<Voucher>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<int> InsertAsync(Voucher voucher);
    Task<bool> UpdateAsync(Voucher voucher);
    Task<bool> DeleteAsync(int voucherId);
}
```

### Command Pattern for Hotkeys
```csharp
public static class GlobalCommands
{
    public static RoutedCommand AddVoucher = new("AddVoucher", typeof(GlobalCommands));
    public static RoutedCommand Save = new("Save", typeof(GlobalCommands));
    public static RoutedCommand Delete = new("Delete", typeof(GlobalCommands));
    public static RoutedCommand Print = new("Print", typeof(GlobalCommands));
}
```

## Project Structure

```
FocusVoucherSystem/
├── src/
│   ├── FocusVoucherSystem/                 # Main WPF Application
│   │   ├── App.xaml                        # Application entry point
│   │   ├── MainWindow.xaml                 # Shell window
│   │   ├── Views/                          # XAML user controls
│   │   │   ├── VoucherEntryView.xaml
│   │   │   ├── ReportsView.xaml
│   │   │   └── VehicleManagementView.xaml
│   │   ├── ViewModels/                     # MVVM ViewModels
│   │   │   ├── MainViewModel.cs
│   │   │   ├── VoucherEntryViewModel.cs
│   │   │   └── ReportsViewModel.cs
│   │   ├── Models/                         # Data models
│   │   │   ├── Company.cs
│   │   │   ├── Vehicle.cs
│   │   │   └── Voucher.cs
│   │   ├── Services/                       # Business logic
│   │   │   ├── VoucherService.cs
│   │   │   ├── ReportService.cs
│   │   │   └── ExportService.cs
│   │   ├── Data/                           # Database access
│   │   │   ├── DatabaseContext.cs
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Commands/                       # Global commands
│   │   │   └── GlobalCommands.cs
│   │   ├── Converters/                     # Value converters
│   │   ├── Themes/                         # WPF styles/themes
│   │   └── Utilities/                      # Helper classes
│   └── FocusVoucherSystem.Core/            # Shared library (if needed)
├── Data/                                   # SQLite database files
├── Tools/                                  # Migration utilities
├── Tests/                                  # Unit tests
└── Documentation/                          # Additional docs
```

## Key Architectural Decisions

### 1. Single-File Deployment
- **Decision**: Self-contained single executable
- **Rationale**: Easy deployment, no dependencies, portable
- **Trade-off**: Larger file size (~50MB) vs installation complexity

### 2. SQLite with WAL Mode
- **Decision**: SQLite WAL mode instead of DELETE mode
- **Rationale**: Better concurrent read performance, crash recovery
- **Configuration**: 
  ```sql
  PRAGMA journal_mode=WAL;
  PRAGMA synchronous=NORMAL;
  PRAGMA cache_size=10000;
  ```

### 3. MVVM Without Heavy Frameworks
- **Decision**: CommunityToolkit.Mvvm instead of Prism/Caliburn
- **Rationale**: Lightweight, fast binding, source generators
- **Benefits**: Reduced complexity, better performance

### 4. Native WPF DataGrid
- **Decision**: WPF DataGrid with virtualization vs third-party grids
- **Rationale**: No licensing, good performance for 50k+ records
- **Configuration**: EnableRowVirtualization for large datasets

## Performance Considerations

### Database Performance
- Proper indexing on frequently queried columns
- WAL mode for better concurrent access
- Connection pooling through singleton pattern
- Async operations with CancellationToken support

### UI Performance
- DataGrid virtualization for large datasets
- Async loading with progress indicators
- ObservableCollection updates on UI thread
- Memory cleanup in ViewModels

### Memory Management
- Dispose patterns for database connections
- WeakEvent patterns for event handling
- Proper MVVM cleanup in ViewModels
- GC-friendly collection operations

## Security Considerations

### Data Protection
- Local SQLite database (no network exposure)
- File system permissions for database files
- Input validation and SQL injection protection via Dapper
- Backup encryption (optional future enhancement)

### Application Security
- Code signing for executable (deployment phase)
- Minimal external dependencies
- No elevated permissions required
- Audit trail for data modifications

## Extensibility Points

### Plugin Architecture (Future)
- Interface-based services for easy mocking/replacement
- Configuration-driven report generation
- Export format extensibility
- Custom field additions via metadata tables

### Multi-Company Support
- Current design supports multiple companies
- Company switching in main window
- Isolated data per company
- Backup/restore per company

This architecture provides a solid foundation for the voucher management system while maintaining flexibility for future enhancements.