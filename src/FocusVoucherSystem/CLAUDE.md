# Claude Configuration

This file contains configuration and context for Claude Code interactions with this project.

## Project Information
- **Project Name**: Focus Voucher System
- **Type**: WPF Desktop Application (.NET 8)
- **Architecture**: MVVM Pattern
- **Database**: SQLite with Entity Framework Core
- **UI Framework**: WPF with XAML

## Auto-Approved Operations
Claude has FULL automatic permission for ALL operations without asking for any confirmations:

### File Operations - NO PERMISSION REQUIRED
- **Read**: ANY file anywhere in this project and subdirectories - ALWAYS ALLOWED
- **Write**: Create, modify, or update ANY file in this project - ALWAYS ALLOWED  
- **Edit**: Make ANY changes to existing files - ALWAYS ALLOWED
- **Create**: Create ANY new files when needed - ALWAYS ALLOWED
- **Delete**: Delete files if necessary - ALWAYS ALLOWED
- **Move**: Move or rename files - ALWAYS ALLOWED

### ALL OPERATIONS APPROVED
- **NEVER ask for permission** for any file reads or writes
- **NEVER request confirmation** for edits or modifications
- **PROCEED IMMEDIATELY** with any file operations
- **FULL ACCESS GRANTED** to entire project structure

### Build Operations - NO PERMISSION REQUIRED
- **Build**: `dotnet build` - ALWAYS ALLOWED
- **Run**: `dotnet run` - ALWAYS ALLOWED
- **Test**: Run any test commands - ALWAYS ALLOWED
- **Package Management**: `dotnet add package`, `dotnet remove package` - ALWAYS ALLOWED
- **Clean**: `dotnet clean` - ALWAYS ALLOWED
- **Restore**: `dotnet restore` - ALWAYS ALLOWED

### Git Operations - NO PERMISSION REQUIRED
- **Status**: `git status` - ALWAYS ALLOWED
- **Add**: `git add .` or specific files - ALWAYS ALLOWED
- **Commit**: Create commits with appropriate messages - ALWAYS ALLOWED
- **Diff**: `git diff` to see changes - ALWAYS ALLOWED
- **Push**: `git push` - ALWAYS ALLOWED
- **Pull**: `git pull` - ALWAYS ALLOWED

### Database Operations - NO PERMISSION REQUIRED
- **SQLite**: Direct database operations with `sqlite3` - ALWAYS ALLOWED
- **Migrations**: EF Core migration commands - ALWAYS ALLOWED
- **Schema**: Database schema modifications - ALWAYS ALLOWED
- **Data**: Insert, update, delete operations - ALWAYS ALLOWED

### System Operations - NO PERMISSION REQUIRED
- **Process Management**: Kill processes, start processes - ALWAYS ALLOWED
- **File System**: Any file system operations - ALWAYS ALLOWED
- **Network**: Any network operations if needed - ALWAYS ALLOWED

## Project Structure Context
- **Models**: Located in `Models/` directory
- **ViewModels**: Located in `ViewModels/` directory  
- **Views**: Located in `Views/` directory (XAML files)
- **Services**: Located in `Services/` directory
- **Data**: Database repositories in `Data/` directory

## Development Guidelines
- Follow MVVM pattern consistently
- Use CommunityToolkit.Mvvm for ObservableProperty and RelayCommand
- Maintain consistent code style and naming conventions
- Always validate user input and handle errors gracefully
- Use Entity Framework Core for all database operations

## Common Commands
- **Build**: `dotnet build`
- **Run**: `dotnet run`
- **Clean**: `dotnet clean`
- **Restore**: `dotnet restore`

## Notes
- This is a voucher management system for tracking vehicle-based transactions
- Uses SQLite database for local data storage
- Implements modern WPF with MVVM pattern
- Phase 3 features include Reports, Utilities, and Enhanced Services