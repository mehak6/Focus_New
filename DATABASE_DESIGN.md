# Database Design Specification - Focus Voucher System

## Overview
SQLite database design optimized for single-user voucher management with WAL mode for performance and crash recovery.

## Database Configuration

### SQLite Settings
```sql
-- Enable WAL mode for better performance and concurrency
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA cache_size=10000;
PRAGMA temp_store=MEMORY;
PRAGMA mmap_size=268435456; -- 256MB memory mapped I/O
```

### Connection String
```
Data Source=Data/FocusVouchers.db;Cache=Shared;Journal Mode=WAL;
```

## Entity Relationship Diagram

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│    Companies    │         │    Vehicles     │         │    Vouchers     │
├─────────────────┤         ├─────────────────┤         ├─────────────────┤
│ CompanyId (PK)  │────────>│ CompanyId (FK)  │<────────│ CompanyId (FK)  │
│ Name            │         │ VehicleId (PK)  │<────────│ VehicleId (FK)  │
│ FinancialYearStart       │ VehicleNumber   │         │ VoucherId (PK)  │
│ FinancialYearEnd         │ Description     │         │ VoucherNumber   │
│ LastVoucherNumber        │ IsActive        │         │ Date            │
│ IsActive        │         │                 │         │ Amount          │
│                 │         └─────────────────┘         │ DrCr            │
└─────────────────┘                                     │ Narration       │
                                                        │ CreatedDate     │
                                                        │ ModifiedDate    │
                                                        └─────────────────┘

                            ┌─────────────────┐
                            │    Settings     │
                            ├─────────────────┤
                            │ Key (PK)        │
                            │ Value           │
                            │ Description     │
                            └─────────────────┘
```

## Table Schemas

### 1. Companies Table
**Purpose**: Store company information and maintain voucher numbering sequence

```sql
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
```

**Key Features:**
- `LastVoucherNumber`: Tracks highest voucher number for continuous sequencing
- `FinancialYear`: For future reporting needs (not used for voucher reset)
- Soft delete via `IsActive` flag

### 2. Vehicles Table
**Purpose**: Master list of vehicles/accounts for voucher entries

```sql
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
```

**Key Features:**
- `VehicleNumber`: Primary identifier (matches legacy format)
- `Description`: Optional additional details
- Company-scoped uniqueness via constraint
- Soft delete support

### 3. Vouchers Table
**Purpose**: Core voucher entries with debit/credit transactions

```sql
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
```

**Key Features:**
- `VoucherNumber`: User-editable, continuous sequence per company
- `DrCr`: 'D' for Debit, 'C' for Credit (matches legacy format)
- `Amount`: Always positive, direction determined by DrCr
- Audit trail with Created/Modified dates
- RESTRICT on Vehicle delete to preserve data integrity

### 4. Settings Table
**Purpose**: Application configuration and preferences

```sql
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    Description TEXT,
    ModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**Initial Configuration:**
```sql
INSERT INTO Settings (Key, Value, Description) VALUES 
('DatabaseVersion', '1.0.0', 'Database schema version'),
('DefaultCompanyId', '1', 'Default company for new sessions'),
('DateFormat', 'dd/MM/yyyy', 'Display date format'),
('DecimalPlaces', '2', 'Amount decimal places'),
('BackupRetentionDays', '30', 'Days to keep backup files'),
('ExportDirectory', 'Exports/', 'Default export location');
```

## Indexes for Performance

### Primary Indexes (Automatic)
```sql
-- Created automatically with PRIMARY KEY constraints
-- Companies(CompanyId)
-- Vehicles(VehicleId) 
-- Vouchers(VoucherId)
```

### Custom Indexes
```sql
-- Voucher queries by company and date range
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
```

## Data Types and Constraints

### Data Type Decisions
- **INTEGER**: Auto-incrementing PKs, voucher numbers, boolean flags
- **TEXT**: All string data (SQLite handles UTF-8 natively)
- **DECIMAL(15,2)**: Amounts (sufficient for crores with 2 decimal places)
- **DATE**: Date-only fields (stored as TEXT in ISO format)
- **DATETIME**: Timestamp fields with timezone info

### Constraint Strategy
- **CHECK Constraints**: Data validation (DrCr values, positive amounts)
- **FOREIGN KEY**: Referential integrity with CASCADE/RESTRICT policies
- **UNIQUE**: Business logic enforcement (vehicle numbers per company)
- **NOT NULL**: Required fields only (avoid over-constraining)

## Data Migration Strategy

### Legacy Data Import
```sql
-- Temporary import tables for legacy data processing
CREATE TEMP TABLE TempVouchers (
    VoucherNo INTEGER,
    Date TEXT,
    AccountName TEXT,
    Amount REAL,
    DrCr TEXT
);

CREATE TEMP TABLE TempVehicles (
    SerialNo INTEGER,
    VehicleNumber TEXT
);
```

### Migration Process
1. **Pre-import Validation**
   - Parse VCH.TXT and VEH.TXT files
   - Validate date formats and amounts
   - Check for duplicate vehicle numbers

2. **Vehicle Import**
   ```sql
   INSERT INTO Vehicles (CompanyId, VehicleNumber)
   SELECT 1, TRIM(VehicleNumber) 
   FROM TempVehicles 
   WHERE VehicleNumber IS NOT NULL AND VehicleNumber != '';
   ```

3. **Voucher Import**
   ```sql
   INSERT INTO Vouchers (CompanyId, VoucherNumber, Date, VehicleId, Amount, DrCr)
   SELECT 
       1,
       tv.VoucherNo,
       DATE(tv.Date),
       v.VehicleId,
       ABS(tv.Amount),
       tv.DrCr
   FROM TempVouchers tv
   JOIN Vehicles v ON v.VehicleNumber = tv.AccountName AND v.CompanyId = 1;
   ```

4. **Post-import Cleanup**
   - Update Companies.LastVoucherNumber
   - Validate data integrity
   - Generate import report

## Query Patterns and Performance

### Common Query Patterns
```sql
-- Daily voucher list (most frequent)
SELECT v.VoucherNumber, v.Date, vh.VehicleNumber, v.Amount, v.DrCr
FROM Vouchers v
JOIN Vehicles vh ON v.VehicleId = vh.VehicleId
WHERE v.CompanyId = ? AND v.Date BETWEEN ? AND ?
ORDER BY v.Date DESC, v.VoucherNumber DESC;

-- Vehicle ledger with running balance
SELECT 
    v.Date,
    v.VoucherNumber,
    v.Amount,
    v.DrCr,
    SUM(CASE WHEN v2.DrCr = 'D' THEN v2.Amount ELSE -v2.Amount END) 
        OVER (ORDER BY v2.Date, v2.VoucherNumber) as RunningBalance
FROM Vouchers v
JOIN Vouchers v2 ON v2.VehicleId = v.VehicleId AND v2.Date <= v.Date
WHERE v.VehicleId = ?
ORDER BY v.Date, v.VoucherNumber;

-- Next voucher number
SELECT COALESCE(MAX(VoucherNumber), 0) + 1
FROM Vouchers
WHERE CompanyId = ?;
```

### Performance Expectations
- **Small datasets (< 10K vouchers)**: Sub-millisecond queries
- **Medium datasets (10K-100K vouchers)**: < 50ms for most queries
- **Large datasets (100K+ vouchers)**: < 200ms with proper indexing

## Backup and Maintenance

### Automated Backup Strategy
```sql
-- WAL checkpoint for consistent backup
PRAGMA wal_checkpoint(FULL);

-- Vacuum for space optimization (monthly)
VACUUM;

-- Statistics update for query optimization
ANALYZE;
```

### Database Maintenance Tasks
1. **Daily**: WAL checkpoint during application shutdown
2. **Weekly**: Statistics update (ANALYZE)
3. **Monthly**: Full vacuum for space reclamation
4. **On-demand**: Export to CSV for external backup

## Version Management

### Schema Versioning
```sql
-- Version tracking for database migrations
CREATE TABLE SchemaVersions (
    Version TEXT PRIMARY KEY,
    AppliedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Description TEXT
);

INSERT INTO SchemaVersions VALUES ('1.0.0', CURRENT_TIMESTAMP, 'Initial schema');
```

### Future Migration Support
- Migration scripts named by version (e.g., `Migration_1.0.1.sql`)
- Incremental migrations with rollback support
- Data validation after each migration step

This database design provides a solid foundation for the voucher management system while maintaining performance and data integrity.