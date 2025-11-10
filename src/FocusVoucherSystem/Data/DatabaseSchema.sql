-- Focus Voucher System Database Schema
-- SQLite Database with WAL Mode for Performance
-- Version: 1.0.0

-- Enable WAL mode for better performance and concurrency
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA cache_size=10000;
PRAGMA temp_store=MEMORY;
PRAGMA mmap_size=268435456; -- 256MB memory mapped I/O
PRAGMA foreign_keys=ON;

-- =============================================================================
-- COMPANIES TABLE
-- Manages company information and voucher numbering sequence
-- =============================================================================
CREATE TABLE Companies (
    CompanyId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    FinancialYearStart DATE NOT NULL DEFAULT '2024-04-01',
    FinancialYearEnd DATE NOT NULL DEFAULT '2025-03-31',
    LastVoucherNumber INTEGER NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================================================
-- VEHICLES TABLE  
-- Master list of vehicles/accounts for voucher entries
-- =============================================================================
CREATE TABLE Vehicles (
    VehicleId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    VehicleNumber TEXT NOT NULL,
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId) ON DELETE CASCADE
);

-- =============================================================================
-- VOUCHERS TABLE
-- Core voucher entries with debit/credit transactions  
-- =============================================================================
CREATE TABLE Vouchers (
    VoucherId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    VoucherNumber INTEGER NOT NULL,
    Date DATE NOT NULL,
    VehicleId INTEGER NOT NULL,
    Amount DECIMAL(15,2) NOT NULL CHECK(Amount >= 0),
    DrCr TEXT NOT NULL CHECK(DrCr IN ('D', 'C')),
    Narration TEXT,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId) ON DELETE CASCADE,
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId) ON DELETE RESTRICT
);

-- =============================================================================
-- SETTINGS TABLE
-- Application configuration and preferences
-- =============================================================================
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    Description TEXT,
    ModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================================================
-- PERFORMANCE INDEXES
-- Optimized for voucher management system queries
-- =============================================================================

-- Voucher queries by company and date range (most frequent)
CREATE INDEX idx_vouchers_company_date 
ON Vouchers(CompanyId, Date DESC, VoucherId);

-- Vehicle ledger queries
CREATE INDEX idx_vouchers_vehicle_date 
ON Vouchers(VehicleId, Date DESC, VoucherId);

-- Voucher number lookups
CREATE INDEX idx_vouchers_company_number 
ON Vouchers(CompanyId, VoucherNumber);

-- Vehicle search and uniqueness
CREATE UNIQUE INDEX idx_vehicles_company_number 
ON Vehicles(CompanyId, VehicleNumber) WHERE IsActive = 1;

-- Active vehicle lookups
CREATE INDEX idx_vehicles_active 
ON Vehicles(CompanyId, IsActive, VehicleNumber) WHERE IsActive = 1;

-- Settings optimization
CREATE INDEX idx_settings_key ON Settings(Key);

-- =============================================================================
-- INITIAL DATA SETUP
-- Application settings only - no sample data
-- =============================================================================

-- Initial application settings
INSERT INTO Settings (Key, Value, Description) VALUES 
('DatabaseVersion', '1.0.0', 'Database schema version'),
('DateFormat', 'dd/MM/yyyy', 'Display date format'),
('DecimalPlaces', '2', 'Amount decimal places'),
('BackupRetentionDays', '30', 'Days to keep backup files'),
('ExportDirectory', 'Exports/', 'Default export location'),
('FirstRun', 'true', 'Indicates if this is the first application run');

-- =============================================================================
-- TRIGGERS FOR AUDIT TRAIL
-- Automatically update ModifiedDate on record changes
-- =============================================================================

-- Trigger for Companies table
CREATE TRIGGER trg_companies_modified_date 
AFTER UPDATE ON Companies
BEGIN
    UPDATE Companies SET ModifiedDate = CURRENT_TIMESTAMP WHERE CompanyId = NEW.CompanyId;
END;

-- Trigger for Vehicles table
CREATE TRIGGER trg_vehicles_modified_date 
AFTER UPDATE ON Vehicles
BEGIN
    UPDATE Vehicles SET ModifiedDate = CURRENT_TIMESTAMP WHERE VehicleId = NEW.VehicleId;
END;

-- Trigger for Vouchers table
CREATE TRIGGER trg_vouchers_modified_date 
AFTER UPDATE ON Vouchers
BEGIN
    UPDATE Vouchers SET ModifiedDate = CURRENT_TIMESTAMP WHERE VoucherId = NEW.VoucherId;
END;

-- Trigger for Settings table
CREATE TRIGGER trg_settings_modified_date 
AFTER UPDATE ON Settings
BEGIN
    UPDATE Settings SET ModifiedDate = CURRENT_TIMESTAMP WHERE Key = NEW.Key;
END;

-- =============================================================================
-- VALIDATION AND INTEGRITY VIEWS
-- Helpful views for data validation and reporting
-- =============================================================================

-- View for voucher balances per vehicle
CREATE VIEW VoucherBalances AS
SELECT 
    v.VehicleId,
    ve.VehicleNumber,
    ve.Description,
    SUM(CASE WHEN v.DrCr = 'D' THEN v.Amount ELSE -v.Amount END) as Balance,
    COUNT(*) as TransactionCount,
    MAX(v.Date) as LastTransactionDate
FROM Vouchers v
JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
WHERE ve.IsActive = 1
GROUP BY v.VehicleId, ve.VehicleNumber, ve.Description;

-- View for daily transaction summaries
CREATE VIEW DailySummary AS
SELECT 
    Date,
    CompanyId,
    COUNT(*) as VoucherCount,
    SUM(CASE WHEN DrCr = 'D' THEN Amount ELSE 0 END) as TotalDebits,
    SUM(CASE WHEN DrCr = 'C' THEN Amount ELSE 0 END) as TotalCredits,
    SUM(CASE WHEN DrCr = 'D' THEN Amount ELSE -Amount END) as NetAmount
FROM Vouchers
GROUP BY Date, CompanyId
ORDER BY Date DESC;

-- =============================================================================
-- DATABASE SCHEMA VERIFICATION
-- Query to verify schema version and integrity
-- =============================================================================

-- Schema verification query
SELECT 
    'Schema Version' as Check_Type, 
    Value as Result 
FROM Settings 
WHERE Key = 'DatabaseVersion'
UNION ALL
SELECT 
    'Table Count' as Check_Type,
    CAST(COUNT(*) as TEXT) as Result
FROM sqlite_master 
WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
UNION ALL
SELECT 
    'Index Count' as Check_Type,
    CAST(COUNT(*) as TEXT) as Result
FROM sqlite_master 
WHERE type = 'index' AND name NOT LIKE 'sqlite_%'
UNION ALL
SELECT 
    'Trigger Count' as Check_Type,
    CAST(COUNT(*) as TEXT) as Result
FROM sqlite_master 
WHERE type = 'trigger'
UNION ALL
SELECT 
    'View Count' as Check_Type,
    CAST(COUNT(*) as TEXT) as Result
FROM sqlite_master 
WHERE type = 'view';

-- End of schema file