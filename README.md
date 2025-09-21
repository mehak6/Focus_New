# Focus Voucher System v1.0.3

A Windows desktop application for managing financial vouchers and vehicle accounting with SQLite database backend.

## Overview

Focus Voucher System is a comprehensive voucher management application designed for businesses to track debit/credit transactions across multiple vehicles/accounts within different companies. The system provides efficient voucher processing with automated numbering, financial year management, and robust data integrity.

## Features

### Core Functionality
- **Multi-Company Support**: Manage vouchers across multiple companies
- **Vehicle/Account Management**: Track transactions for individual vehicles or accounts
- **Voucher Processing**: Create, edit, and manage debit/credit vouchers
- **Automated Numbering**: Sequential voucher numbering per company
- **Financial Year Support**: Built-in financial year management (April-March cycle)

### Database Features
- **SQLite Backend**: High-performance SQLite database with WAL mode
- **Data Integrity**: Foreign key constraints and validation triggers
- **Audit Trail**: Automatic timestamp tracking for all modifications
- **Performance Optimization**: Strategic indexes for fast queries
- **Backup Support**: 30-day backup retention policy

### Reporting & Analytics
- **Voucher Balances**: Real-time balance calculations per vehicle
- **Daily Summaries**: Transaction summaries with debit/credit totals
- **Export Capabilities**: Data export functionality
- **Transaction History**: Complete audit trail of all voucher activities

## System Requirements

- **Operating System**: Windows 10/11 (64-bit)
- **Memory**: Minimum 4GB RAM
- **Storage**: 100MB available disk space
- **Database**: SQLite (included via e_sqlite3.dll)

## Installation

1. Download the FocusVoucherSystem-v1.0.3-win-x64 package
2. Extract all files to your desired installation directory
3. Ensure all files are in the same folder:
   - `FocusVoucherSystem.exe` (Main application)
   - `e_sqlite3.dll` (SQLite library)
   - `FocusVoucher.db` (Database file)
   - `Data/` folder containing database schema

## Getting Started

### First Run
1. Launch `FocusVoucherSystem.exe`
2. The system will detect if this is the first run and initialize settings
3. Create your first company to begin managing vouchers

### Basic Workflow
1. **Setup Company**: Create or select a company
2. **Add Vehicles**: Register vehicles/accounts for the company
3. **Create Vouchers**: Process debit/credit transactions
4. **Generate Reports**: View balances and transaction summaries

## Database Schema

The application uses a normalized SQLite database with the following core tables:

### Main Tables
- **Companies**: Company information and voucher numbering
- **Vehicles**: Vehicle/account master data
- **Vouchers**: Core transaction records
- **Settings**: Application configuration

### Key Features
- **Financial Year Management**: Automatic April-March financial year handling
- **Voucher Numbering**: Sequential numbering per company with last number tracking
- **Transaction Types**: Support for both Debit (D) and Credit (C) entries
- **Amount Validation**: Positive amount constraints with 15,2 decimal precision

### Performance Optimizations
- Strategic indexes on frequently queried columns
- WAL mode for improved concurrency
- Memory-mapped I/O for better performance
- Optimized cache settings

## File Structure

```
FocusVoucherSystem-v1.0.3-win-x64/
├── FocusVoucherSystem.exe     # Main application executable
├── FocusVoucherSystem.pdb     # Debug symbols
├── e_sqlite3.dll              # SQLite database engine
├── FocusVoucher.db            # Application database
├── Data/
│   └── DatabaseSchema.sql     # Database schema definition
└── README.md                  # This file
```

## Configuration

### Application Settings
The system stores configuration in the Settings table:
- **DatabaseVersion**: Schema version tracking
- **DateFormat**: Display format (dd/MM/yyyy)
- **DecimalPlaces**: Amount precision (2 decimal places)
- **BackupRetentionDays**: Backup file retention (30 days)
- **ExportDirectory**: Default export location

### Database Configuration
- **WAL Mode**: Enabled for performance and concurrency
- **Foreign Keys**: Enforced for data integrity
- **Cache Size**: 10,000 pages for optimal performance
- **Memory Mapped**: 256MB for faster I/O operations

## Data Management

### Backup Strategy
- Automatic backup retention for 30 days
- WAL mode provides crash recovery
- Regular database integrity checks recommended

### Export Options
- Default export directory: `Exports/`
- Multiple export formats supported
- Transaction data and reports can be exported

## Troubleshooting

### Common Issues
1. **Database Lock**: Ensure only one instance is running
2. **Permission Errors**: Run with appropriate file system permissions
3. **Missing DLL**: Verify `e_sqlite3.dll` is in the same directory

### Database Verification
Use the built-in schema verification query to check database integrity:
- Table count verification
- Index optimization status
- Trigger functionality

## Version Information

- **Version**: 1.0.3
- **Database Schema**: 1.0.0
- **Platform**: Windows x64
- **Build Date**: September 4, 2023
- **SQLite Version**: Latest (via e_sqlite3.dll)

## Support

For technical support or feature requests, please contact the development team or refer to the application documentation within the system.

## License

This software is provided as-is. Please refer to your license agreement for terms of use and distribution rights.