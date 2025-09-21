# Voucher Management System – Project Documentation

---

## 1. Project Overview
This project is a **Voucher Management System** designed to modernize the legacy DOS-based software currently in use. The system will manage vouchers (credit/debit against vehicles), preserve the old workflow and hotkeys, while introducing a modern interface and additional export capabilities.

---

## 2. Objectives
- Replace the legacy DOS voucher entry and listing software with a **modern C#-based solution**.
- Maintain familiarity by preserving old hotkeys (e.g., F2=Add, F5=Save, F8=Delete).
- Support **continuous voucher numbering** per company (no reset at year-end).
- Allow voucher numbers to be editable when necessary.
- Provide output in **on-screen view, print, CSV, and PDF formats**.
- Use a **modern grid UI** instead of plain tabular DOS style.

---

## 3. Functional Requirements

### Voucher Management
- **Voucher Numbering**:
  - Auto-increment, company-specific.
  - Continuous sequence (no reset per financial year).
  - Editable by user if required.
- **Voucher Entry Screen**:
  - Fields: Voucher No., Date, Vehicle (Account Name), Amount, Dr/Cr flag, Narration.
  - Grid-based interface for fast entry.
  - Support for quick vehicle lookup.

### Reporting
- **Voucher Listing** with filters (date range, vehicle, Dr/Cr).
- **Reports**:
  - Day Book
  - Vehicle Ledger (running balance)
  - Consolidated Ledger (all vehicles)
  - Recovery / Date-wise Recovery
- Export options: On-screen grid, Printable format, CSV, PDF.

### Hotkey Workflow
- Replicate **legacy F-key mapping**:
  - F2 = Add new voucher
  - F5 = Save
  - F8 = Delete
  - F9 = Print
  - Esc = Cancel/Exit

---

## 4. Non-Functional Requirements
- **Technology Stack**:
  - C# (.NET 8 or higher)
  - WPF for desktop UI
  - SQLite for database
  - QuestPDF or similar for PDF export
- **Performance**:
  - Load voucher lists of 50,000+ records without noticeable delay.
  - Export reports under 5 seconds for typical monthly data.
- **Compatibility**:
  - Windows 10 and above.
  - Printer integration (dot-matrix + modern printers).

---

## 5. System Architecture
```
+---------------------------+
|       User Interface      | (WPF)
+---------------------------+
             ↓
+---------------------------+
|   Business Logic Layer    | (Voucher operations, rules, hotkeys)
+---------------------------+
             ↓
+---------------------------+
|     Data Access Layer     | (Dapper / ADO.NET)
+---------------------------+
             ↓
+---------------------------+
|        Database           | (SQLite)
+---------------------------+
```

---

## 6. Database Design

### Tables
- **Company**
  - CompanyID (PK)
  - Name
  - FinancialYearStart
  - FinancialYearEnd

- **Vehicle**
  - VehicleID (PK)
  - CompanyID (FK)
  - VehicleNumber (Unique)
  - IsActive (default true)

- **Voucher**
  - VoucherID (PK)
  - CompanyID (FK)
  - VoucherNo (Auto-increment, editable)
  - Date
  - VehicleID (FK)
  - Amount
  - DrCr (enum: Debit/Credit)
  - Narration

- **Settings**
  - Key (PK)
  - Value

---

## 7. UI/UX Design

### Voucher Entry Screen (Modernized)
- **Top panel**: Company, Date, Voucher No.
- **Grid area**: Voucher entry (Vehicle, Amount, Dr/Cr, Narration).
- **Bottom panel**: Hotkey hints (F2=Add, F5=Save, F8=Delete, Esc=Cancel).

### Reports Screen
- Filters (Date range, Vehicle, Dr/Cr).
- Display in modern grid with:
  - Column sorting
  - Export buttons (CSV, PDF, Print)

---

## 8. Development Plan

### Phase 1 – Setup
- Create database schema.
- Build DAL with Dapper.
- Implement core voucher CRUD.

### Phase 2 – Voucher Screen
- Design voucher entry UI.
- Implement hotkey mapping.
- Add auto-increment logic.

### Phase 3 – Reporting
- Develop voucher listing.
- Add export functions.

### Phase 4 – Testing & Migration
- Test with sample voucher data (import legacy VCH.TXT into new DB).
- Validate report outputs against old system.

---

## 9. Future Enhancements
- Multi-user concurrency handling.
- Cloud sync / Web dashboard.
- Mobile-friendly report viewer.

---

## 10. Deliverables
- Complete source code (C# project).
- Database scripts (schema + seed data).
- Executable installer (.exe or .msi).
- User manual (with hotkeys, workflows, screenshots).
- Technical documentation (database ERD, schema details).

---

## 11. References
- Legacy voucher data sample (VCH.TXT)
- Screenshot of current voucher entry screen (for UI migration)

