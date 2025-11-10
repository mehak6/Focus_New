# Deployment and Configuration Guide - Focus Voucher System

## Overview
Comprehensive guide for building, deploying, and configuring the Focus Voucher System as a self-contained .NET 8 WPF application.

## Build Configuration

### Project File Settings
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Target Framework -->
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>false</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Application Properties -->
    <AssemblyTitle>Focus Voucher System</AssemblyTitle>
    <Product>Focus Voucher System</Product>
    <Company>Focus Technologies</Company>
    <Copyright>Copyright © 2024 Focus Technologies</Copyright>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    
    <!-- Deployment Configuration -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>false</PublishTrimmed>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    
    <!-- Application Icon and Manifest -->
    <ApplicationIcon>Resources\FocusVoucher.ico</ApplicationIcon>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>
</Project>
```

### Build Configurations

#### Debug Configuration
```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <Optimize>false</Optimize>
  <DefineConstants>DEBUG;TRACE</DefineConstants>
  <WarningsAsErrors>nullable</WarningsAsErrors>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

#### Release Configuration
```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <Optimize>true</Optimize>
  <DefineConstants>TRACE</DefineConstants>
  <WarningsAsErrors>nullable</WarningsAsErrors>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

## Deployment Methods

### Method 1: Single-File Executable (Primary)
**Use Case**: Portable deployment, no installation required

#### Build Command
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

#### Output Structure
```
publish/
├── FocusVoucherSystem.exe    (~50MB, includes .NET runtime)
├── FocusVoucherSystem.pdb    (Optional, for debugging)
└── [Temp extraction folder created at runtime]
```

#### Advantages
- No installation required
- No .NET runtime dependency
- Easy distribution and updates
- Portable between machines

#### Disadvantages
- Larger file size (~50MB)
- Slower initial startup (extraction overhead)
- No Start Menu integration

### Method 2: Framework-Dependent Deployment
**Use Case**: When .NET 8 runtime is already installed

#### Build Command
```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish-framework
```

#### Output Structure
```
publish-framework/
├── FocusVoucherSystem.exe    (~1MB)
├── FocusVoucherSystem.dll
├── [Various dependency DLLs]
└── FocusVoucherSystem.runtimeconfig.json
```

#### Prerequisites
- .NET 8 Desktop Runtime must be installed
- Windows 10 version 1607 or higher

### Method 3: MSI Installer (Optional)
**Use Case**: Enterprise deployment with Start Menu and Add/Remove Programs integration

#### Using WiX Toolset
```xml
<!-- Product.wxs -->
<Product Id="*" Name="Focus Voucher System" Language="1033" 
         Version="1.0.0.0" Manufacturer="Focus Technologies" 
         UpgradeCode="12345678-1234-1234-1234-123456789012">
  
  <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
  
  <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
  
  <MediaTemplate EmbedCab="yes" />
  
  <Feature Id="ProductFeature" Title="Focus Voucher System" Level="1">
    <ComponentGroupRef Id="ProductComponents" />
  </Feature>
</Product>
```

## Application Configuration

### App.config Settings
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <!-- Database Configuration -->
    <add key="DatabasePath" value="Data\FocusVouchers.db" />
    <add key="BackupPath" value="Backups\" />
    <add key="ExportPath" value="Exports\" />
    
    <!-- Application Settings -->
    <add key="CompanyName" value="XYZ Company" />
    <add key="FinancialYearStart" value="04-01" />
    <add key="DefaultDateFormat" value="dd/MM/yyyy" />
    
    <!-- Performance Settings -->
    <add key="MaxRecordsPerPage" value="1000" />
    <add key="DatabaseTimeout" value="30" />
    <add key="BackupRetentionDays" value="30" />
  </appSettings>
  
  <connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Data Source=Data\FocusVouchers.db;Cache=Shared;Journal Mode=WAL;" 
         providerName="Microsoft.Data.Sqlite" />
  </connectionStrings>
</configuration>
```

### Application Manifest (app.manifest)
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="FocusVoucherSystem.app"/>
  
  <!-- Windows Version Compatibility -->
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"/> <!-- Windows 10 -->
      <supportedOS Id="{1f676c76-80e1-4239-95bb-83d0f6d0da78}"/> <!-- Windows 8.1 -->
      <supportedOS Id="{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}"/> <!-- Windows 8 -->
      <supportedOS Id="{35138b9a-5d96-4fbd-8e2d-a2440225f93a}"/> <!-- Windows 7 -->
    </application>
  </compatibility>
  
  <!-- DPI Awareness -->
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    </windowsSettings>
  </application>
  
  <!-- No UAC Elevation Required -->
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

## Directory Structure and File Organization

### Runtime Directory Structure
```
FocusVoucherSystem.exe         # Main executable
├── Data/                      # Database files
│   ├── FocusVouchers.db      # Main SQLite database
│   ├── FocusVouchers.db-wal  # WAL file (created automatically)
│   └── FocusVouchers.db-shm  # Shared memory file
├── Backups/                   # Database backups
│   ├── Backup_2024-01-15.zip
│   └── Backup_2024-01-14.zip
├── Exports/                   # Report exports
│   ├── DayBook_2024-01-15.pdf
│   ├── Ledger_2024-01-15.csv
│   └── Reports/
├── Logs/                      # Application logs
│   ├── Application_2024-01-15.log
│   └── Error_2024-01-15.log
└── Temp/                      # Temporary files
    └── [Runtime temp files]
```

### First-Run Initialization
```csharp
public class ApplicationInitializer
{
    public static async Task InitializeAsync()
    {
        // Create directory structure
        CreateDirectoryStructure();
        
        // Initialize database
        await InitializeDatabaseAsync();
        
        // Create default settings
        CreateDefaultSettings();
        
        // Setup logging
        SetupLogging();
    }
    
    private static void CreateDirectoryStructure()
    {
        var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var directories = new[] { "Data", "Backups", "Exports", "Logs", "Temp" };
        
        foreach (var dir in directories)
        {
            var fullPath = Path.Combine(appPath, dir);
            Directory.CreateDirectory(fullPath);
        }
    }
}
```

## Database Deployment and Migration

### Initial Database Setup
```sql
-- Database creation script (embedded in application)
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA cache_size=10000;
PRAGMA foreign_keys=ON;

-- Create tables (as defined in DATABASE_DESIGN.md)
-- ... table creation statements ...

-- Insert default data
INSERT INTO Companies (Name, FinancialYearStart, FinancialYearEnd) 
VALUES ('Default Company', '2024-04-01', '2025-03-31');

INSERT INTO Settings (Key, Value, Description) VALUES 
('DatabaseVersion', '1.0.0', 'Database schema version'),
('FirstRun', 'true', 'Indicates if this is the first application run');
```

### Migration System
```csharp
public class DatabaseMigration
{
    private readonly string _connectionString;
    
    public async Task<bool> MigrateToLatestAsync()
    {
        var currentVersion = await GetDatabaseVersionAsync();
        var targetVersion = GetApplicationVersion();
        
        if (currentVersion == targetVersion) return true;
        
        var migrations = GetMigrationScripts(currentVersion, targetVersion);
        
        foreach (var migration in migrations)
        {
            await ExecuteMigrationAsync(migration);
        }
        
        return true;
    }
}
```

## Security and Code Signing

### Code Signing Certificate
```batch
# Sign the executable (production deployment)
signtool sign /f "certificate.pfx" /p "password" /t "http://timestamp.digicert.com" "FocusVoucherSystem.exe"

# Verify signature
signtool verify /pa "FocusVoucherSystem.exe"
```

### Security Considerations
- **No network access required** - Fully offline application
- **Local file system only** - No registry modifications
- **User-level permissions** - No administrator rights needed
- **Data encryption** - Database files can be encrypted (optional)

## Performance Optimization

### Build Optimizations
```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <!-- Ahead-of-Time Compilation -->
  <PublishAot>false</PublishAot> <!-- WPF not compatible with AOT -->
  
  <!-- Tree Trimming (Disabled for WPF compatibility) -->
  <PublishTrimmed>false</PublishTrimmed>
  
  <!-- Compression -->
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  
  <!-- Optimization -->
  <Optimize>true</Optimize>
  <TieredCompilation>true</TieredCompilation>
  <TieredPGO>true</TieredPGO>
</PropertyGroup>
```

### Runtime Optimizations
```csharp
// JIT optimization hints
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static class PerformanceHelpers
{
    // Hot path optimizations
}

// Assembly loading optimization
[ModuleInitializer]
public static void InitializeAssembly()
{
    // Pre-JIT critical methods
    RuntimeHelpers.PrepareMethod(typeof(VoucherService).GetMethod("Save").MethodHandle);
}
```

## Deployment Automation

### Build Script (PowerShell)
```powershell
# BuildAndDeploy.ps1
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$CreateInstaller
)

Write-Host "Building Focus Voucher System v$Version" -ForegroundColor Green

# Clean previous builds
Remove-Item -Path ".\publish" -Recurse -Force -ErrorAction SilentlyContinue

# Build single-file executable
dotnet publish -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version `
    -o .\publish

if ($CreateInstaller) {
    Write-Host "Creating MSI installer..." -ForegroundColor Yellow
    # Run WiX toolset to create MSI
    candle.exe -dVersion=$Version Product.wxs
    light.exe -out "FocusVoucherSystem-$Version.msi" Product.wixobj
}

Write-Host "Build completed successfully!" -ForegroundColor Green
```

### Batch Deployment Script
```batch
@echo off
REM QuickDeploy.bat - Simple deployment script

echo Deploying Focus Voucher System...

REM Create deployment directory
if not exist "C:\FocusVoucher\" mkdir "C:\FocusVoucher\"

REM Copy executable
copy "FocusVoucherSystem.exe" "C:\FocusVoucher\"

REM Create desktop shortcut
powershell "$s=(New-Object -COM WScript.Shell).CreateShortcut('%userprofile%\Desktop\Focus Voucher.lnk');$s.TargetPath='C:\FocusVoucher\FocusVoucherSystem.exe';$s.Save()"

echo Deployment completed!
echo Application installed to: C:\FocusVoucher\
pause
```

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 version 1607 (Anniversary Update) or later
- **Processor**: x64 compatible processor
- **Memory**: 2 GB RAM
- **Storage**: 200 MB available space
- **Display**: 1024x768 resolution minimum

### Recommended Requirements
- **Operating System**: Windows 10 version 21H2 or Windows 11
- **Processor**: Intel Core i3 or AMD Ryzen 3 (or equivalent)
- **Memory**: 4 GB RAM
- **Storage**: 1 GB available space (including data growth)
- **Display**: 1366x768 resolution or higher

### Software Dependencies
- **None** - Self-contained deployment includes all required components
- **Optional**: PDF viewer for exported reports
- **Optional**: Excel for CSV/XLSX file handling

## Troubleshooting and Diagnostics

### Common Issues and Solutions

#### Issue: Application won't start
```
Solutions:
1. Check Windows version compatibility (Windows 10 1607+)
2. Verify x64 processor architecture
3. Check antivirus software blocking execution
4. Run as administrator if file permissions issue
5. Check Windows Event Log for detailed error information
```

#### Issue: Database corruption
```
Solutions:
1. Restore from automatic backup in Backups/ folder
2. Use SQLite recovery tools
3. Import data from exported CSV files
4. Contact support with .db-wal and .db-shm files
```

#### Issue: Performance problems
```
Solutions:
1. Check available disk space (>1GB recommended)
2. Close other memory-intensive applications
3. Consider data archiving if >100K voucher entries
4. Update to latest application version
5. Check antivirus real-time scanning impact
```

### Diagnostic Information Collection
```csharp
public class DiagnosticCollector
{
    public static DiagnosticReport GenerateReport()
    {
        return new DiagnosticReport
        {
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
            OperatingSystem = Environment.OSVersion.ToString(),
            DotNetVersion = Environment.Version.ToString(),
            WorkingDirectory = Environment.CurrentDirectory,
            DatabaseSize = new FileInfo("Data/FocusVouchers.db").Length,
            AvailableMemory = GC.GetTotalMemory(false),
            RecordCounts = GetRecordCounts()
        };
    }
}
```

## Update and Maintenance Strategy

### Update Deployment
1. **Backup Current Database**: Automatic backup before update
2. **Replace Executable**: Simple file replacement
3. **Run Migration**: Automatic database schema updates
4. **Verify Operation**: Post-update validation

### Maintenance Schedule
- **Daily**: Automatic backup (if data changed)
- **Weekly**: WAL checkpoint and optimization
- **Monthly**: Full database vacuum and statistics update
- **Quarterly**: Archive old data (optional)

This deployment configuration ensures reliable, efficient distribution and operation of the Focus Voucher System across various Windows environments.