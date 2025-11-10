# UI/UX Design Guidelines - Focus Voucher System

## Overview
Modern WPF interface design that preserves the efficient workflow and hotkey system of the legacy DOS application while providing a contemporary user experience.

## Design Philosophy

### Core Principles
1. **Workflow Preservation**: Maintain the exact sequence and hotkey patterns from the legacy system
2. **Keyboard Efficiency**: Prioritize keyboard navigation over mouse interaction
3. **Information Density**: Display maximum relevant information without clutter
4. **Visual Hierarchy**: Clear distinction between primary actions and secondary information
5. **Responsive Design**: Adapt to different screen sizes while maintaining usability

### Legacy Compatibility
- Preserve all F-key shortcuts (F2, F5, F8, F9, Esc)
- Maintain tab navigation order identical to DOS version
- Keep similar information layout and grouping
- Preserve company/date header format

## Main Window Layout

### Window Structure
```
┌─────────────────────────────────────────────────────────────────┐
│ Focus Voucher System                                    [_][□][X]│
├─────────────────────────────────────────────────────────────────┤
│ XYZ Company | 01-01-2000                              Connected │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────┤
│Vehicle Number│Voucher Entry│   Reports   │Process Work │ Utilities│
│      F1      │     F2      │     F3      │     F4      │    F5   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│                    [Content Area]                               │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ Status: Ready | Records: 1,234 | F2=Add F5=Save F8=Delete Esc=Exit│
└─────────────────────────────────────────────────────────────────┘
```

### Header Bar
- **Company Info**: Left-aligned company name and date
- **Connection Status**: Right-aligned database status indicator
- **Modern Styling**: Subtle gradient background, clean typography

### Navigation Menu
- **Tab Style**: Horizontal tabs with F-key indicators
- **Visual State**: Active tab highlight, hover effects
- **Keyboard Access**: Alt+F1, Alt+F2, etc. for direct navigation
- **Icons**: Minimal icons to supplement text labels

### Status Bar
- **Left Section**: Current operation status
- **Center Section**: Record counts and statistics
- **Right Section**: Active hotkey reminders

## Voucher Entry Screen

### Layout Design
```
┌─────────────────────────────────────────────────────────────────┐
│ Voucher Entry                                                   │
├─────────────────────────────────────────────────────────────────┤
│ Company: XYZ Ltd          Date: [01/01/2000]    Voucher: [1234] │
├─────────────────────────────────────────────────────────────────┤
│ ┌─Vehicle/Account─┬─Amount──┬─Dr/Cr─┬─Narration────────────────┐ │
│ │ PAPPU /         │   0.74  │   D   │ Cash payment            │ │
│ │ STAPNI/ACCOUNT  │1,07,680 │   C   │ Account settlement      │ │
│ │ TARUN/ALUM      │ 5,734   │   D   │ Material supply         │ │
│ │ [New Entry]     │         │   D   │                         │ │
│ └─────────────────┴─────────┴───────┴─────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ Total Debits: 6,734.74    Total Credits: 1,07,680.82           │
│ Balance: -1,00,946.08                                           │
├─────────────────────────────────────────────────────────────────┤
│ F2=Add Row F5=Save F8=Delete F9=Print Esc=Cancel               │
└─────────────────────────────────────────────────────────────────┘
```

### Grid Design Specifications
- **Column Headers**: Bold, slightly larger font
- **Row Selection**: Full row highlight with subtle blue background
- **Cell Editing**: In-place editing with validation
- **Auto-complete**: Vehicle name dropdown with fuzzy search
- **Amount Formatting**: Right-aligned with thousand separators
- **Dr/Cr Toggle**: Toggle button or dropdown (D/C)

### Input Behavior
- **Tab Navigation**: Vehicle → Amount → DrCr → Narration → Next Row
- **Enter Key**: Same as Tab (move to next field)
- **F2**: Add new row at current position
- **F5**: Save all changes and validate
- **F8**: Delete current row with confirmation
- **Escape**: Cancel editing and revert changes

## Vehicle Management Screen

### Layout Design
```
┌─────────────────────────────────────────────────────────────────┐
│ Vehicle Management                                              │
├─────────────────────────────────────────────────────────────────┤
│ Search: [____________] [Clear] [Add New Vehicle]                │
├─────────────────────────────────────────────────────────────────┤
│ ┌─S.No─┬─Vehicle Number────────────┬─Description─────────┬─Action┐ │
│ │  1   │ PAPPU /                   │ Local Transport     │ [Edit]│ │
│ │  2   │ STAPNI / ACCOUNT          │ Main Account        │ [Edit]│ │
│ │  3   │ TARUN / ALUM              │ Aluminum Supplier   │ [Edit]│ │
│ │      │                           │                     │       │ │
│ └──────┴───────────────────────────┴─────────────────────┴───────┘ │
├─────────────────────────────────────────────────────────────────┤
│ Total Vehicles: 125 | Active: 120 | F2=Add F8=Delete            │
└─────────────────────────────────────────────────────────────────┘
```

### Features
- **Live Search**: Filter vehicles as user types
- **Quick Add**: F2 to add new vehicle inline
- **Bulk Operations**: Select multiple vehicles for operations
- **Sort Options**: Click column headers to sort

## Reports Screen

### Report Selection Interface
```
┌─────────────────────────────────────────────────────────────────┐
│ Reports                                                         │
├─────────────────────────────────────────────────────────────────┤
│ Report Type: [Day Book (Full Entries) ▼]                       │
│                                                                 │
│ Date Range:  From [01/01/2000] To [31/12/2000]                 │
│ Vehicle:     [All Vehicles ▼]                                  │
│ Dr/Cr:       [All ▼] [Debit] [Credit]                         │
│                                                                 │
│ [Generate Report] [Export CSV] [Export PDF] [Print]            │
├─────────────────────────────────────────────────────────────────┤
│ ┌─Date──────┬─V.No─┬─Vehicle─────────┬─Amount────┬─Dr/Cr─┬─Balance┐ │
│ │01/01/2000 │ 424  │ PAPPU /         │     0.74  │   D   │   0.74 │ │
│ │01/01/2000 │ 632  │ STAPNI/ACCOUNT  │1,07,680.82│   C   │-1,07,680│ │
│ │           │      │                 │           │       │        │ │
│ └───────────┴──────┴─────────────────┴───────────┴───────┴────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ Showing 1-100 of 15,423 | Total Dr: 2,34,567 Cr: 1,89,432      │
└─────────────────────────────────────────────────────────────────┘
```

### Report Types Available
1. **Day Book (Full Entries)** - All vouchers in date range
2. **Day Book (Consolidated)** - Summary by date
3. **Ledger of a Vehicle** - Single vehicle statement
4. **Ledger (Full Entries)** - All vehicles detailed
5. **Ledger (Consolidated)** - All vehicles summarized
6. **Interest Calculation** - Interest on balances
7. **Recovery Statement** - Payment tracking
8. **Date Wise Recovery List** - Recovery by date

## Color Scheme and Typography

### Primary Colors
- **Background**: `#F8F9FA` (Light gray)
- **Primary**: `#0D47A1` (Dark blue)
- **Secondary**: `#424242` (Dark gray)
- **Success**: `#2E7D32` (Green)
- **Warning**: `#F57C00` (Orange)
- **Error**: `#C62828` (Red)

### Legacy Mode Colors (Optional)
- **Background**: `#000000` (Black)
- **Text**: `#00FF00` (Green)
- **Headers**: `#FFFF00` (Yellow)
- **Selection**: `#0000FF` (Blue)

### Typography
- **Primary Font**: `Segoe UI, 10pt` (Windows standard)
- **Monospace**: `Consolas, 9pt` (For amounts and numbers)
- **Headers**: `Segoe UI Semibold, 11pt`
- **Status Bar**: `Segoe UI, 8pt`

## Responsive Design

### Screen Size Adaptations
- **Minimum**: 1024x768 (functional but cramped)
- **Recommended**: 1366x768 (optimal layout)
- **Large Screens**: Scale up fonts and spacing proportionally

### Grid Responsiveness
- **Column Auto-sizing**: Adjust column widths based on content
- **Horizontal Scrolling**: When content exceeds screen width
- **Row Height**: Consistent 24px for keyboard navigation

## Accessibility Features

### Keyboard Navigation
- **Tab Order**: Logical left-to-right, top-to-bottom
- **Access Keys**: Alt+Letter shortcuts for all major functions
- **Arrow Keys**: Grid navigation and menu navigation
- **Context Menus**: Right-click or Shift+F10

### Visual Accessibility
- **High Contrast**: Support Windows high contrast themes
- **Font Scaling**: Respect system font size settings
- **Focus Indicators**: Clear visual focus indicators
- **Color Independence**: Don't rely solely on color for information

## Animation and Transitions

### Subtle Animations
- **Page Transitions**: 200ms slide/fade between screens
- **Validation**: Red border flash for invalid input
- **Loading**: Progress bar for long operations
- **Hover Effects**: Subtle button/row highlighting

### Performance Guidelines
- **60 FPS Target**: Smooth animations on typical hardware
- **Reduced Motion**: Respect system accessibility settings
- **Hardware Acceleration**: Use GPU acceleration where beneficial

## Data Validation and Feedback

### Input Validation
- **Real-time**: Validate as user types (non-intrusive)
- **On Save**: Comprehensive validation with clear error messages
- **Visual Cues**: Red borders, warning icons, tooltip messages

### Error Handling
```
┌─────────────────────────────────────────┐
│ ⚠️  Validation Error                    │
├─────────────────────────────────────────┤
│ • Amount cannot be zero or negative     │
│ • Vehicle name is required              │
│ • Date must be within current year      │
├─────────────────────────────────────────┤
│              [Fix Issues]               │
└─────────────────────────────────────────┘
```

### Success Feedback
- **Status Bar Updates**: "Voucher saved successfully"
- **Visual Confirmation**: Brief green highlight on saved rows
- **Sound Cues**: Optional system sounds for save/error

## Print and Export Design

### Print Layout
- **Header**: Company name, report title, date range
- **Footer**: Page numbers, generation timestamp
- **Margins**: Standard 1-inch margins for compatibility
- **Fonts**: Black text, clear readable fonts for dot-matrix printers

### Export Formats
- **CSV**: Clean data export for Excel compatibility
- **PDF**: Professional formatted reports with company branding
- **Excel**: Formatted spreadsheets with formulas and styling

## Performance and Usability

### Data Loading
- **Lazy Loading**: Load data as user scrolls
- **Progress Indicators**: Show progress for operations > 1 second
- **Background Operations**: Non-blocking save/export operations

### Memory Management
- **Virtual Scrolling**: Handle large datasets efficiently
- **Data Caching**: Cache frequently accessed data
- **Dispose Pattern**: Proper cleanup of resources

### User Experience Metrics
- **Startup Time**: < 3 seconds on typical hardware
- **Data Entry Speed**: Match or exceed DOS version efficiency
- **Response Time**: < 100ms for common operations
- **Error Recovery**: Graceful handling of all error conditions

This UI/UX design maintains the efficiency of the legacy system while providing a modern, professional interface that users will find familiar and productive.