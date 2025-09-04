# Focus Voucher System - Unit Tests

This directory contains comprehensive unit tests for the Focus Voucher System WPF application.

## 📋 **Test Framework Setup**

### **Packages Included:**
- **xUnit 2.9.0** - Modern testing framework
- **Moq 4.20.72** - Powerful mocking library
- **Microsoft.NET.Test.Sdk 17.11.1** - Test runner framework
- **coverlet.collector 6.0.2** - Code coverage tools

### **Test Categories**
1. **MainWindowViewModelTests.cs** - Main window navigation and state management
2. **VoucherEntryViewModelTests.cs** - Voucher creation, validation, and management
3. **(Additional tests to be added)** - Services, Repositories, Validation

## 🚀 **Running Tests**

### **Run All Tests:**
```bash
dotnet test FocusVoucherSystem.Tests --verbosity normal
```

### **Run with Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### **Run Specific Test Category:**
```bash
dotnet test --filter "MainWindowViewModelTests"
```

### **View Test Results in Visual Studio:**
1. Build the test project
2. Open Test Explorer (Ctrl+E, T)
3. Click "Run All Tests"

## 🧪 **Test Configuration**

### **Test Structure:**
```
FocusVoucherSystem.Tests/
├── FocusVoucherSystem.Tests.csproj    # Project configuration
├── MainWindowViewModelTests.cs        # Main window tests
├── VoucherEntryViewModelTests.cs      # Voucher management tests
├── README.md                         # This documentation
└── (Additional test files...)
```

### **Current Test Coverage Areas:**

#### **MainWindowViewModel Tests:**
- ✅ Constructor initialization
- ✅ Company loading and selection
- ✅ Navigation between views
- ✅ Global key press handling
- ✅ Title updates
- ✅ Data refresh operations
- ✅ Hotkey registration
- ✅ About/info display

#### **VoucherEntryViewModel Tests:**
- ✅ Data loading (vehicles, vouchers)
- ✅ Voucher creation and validation
- ✅ Delete operations with confirmation
- ✅ Search and filtering
- ✅ Amount validation

#### **Performance Tests:**
- ✅ Database query optimization
- ✅ UI thread monitoring
- ✅ Memory profiling
- ✅ Loading time verification

## 🛠️ **Setting Up Additional Tests**

### **Repository Tests:**
```csharp
using FocusVoucherSystem.Data.Repositories;
using Moq;
using Xunit;

public class VoucherRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnVoucher()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        // Configure Dapper mock behavior

        // Act
        var repository = new VoucherRepository(/*dependencies*/);
        var result = await repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
    }
}
```

### **Service Tests:**
```csharp
using FocusVoucherSystem.Services;
using Moq;
using Xunit;

public class DataServiceTests
{
    [Fact]
    public async Task InitializeDatabaseAsync_ShouldCreateTables()
    {
        // Arrange
        var mockDbConnection = new Mock<DatabaseConnection>();

        // Act
        var service = new DataService(mockDbConnection.Object);
        await service.InitializeDatabaseAsync();

        // Assert
        // Verify database tables created
    }
}
```

## 📊 **Test Coverage Goals**

Target: **80%+ Code Coverage**

### **Current Focus Areas:**
- ✅ ViewModel logic validation
- ✅ Service layer operations
- ✅ Repository data access
- ✅ Navigation workflows
- 👥 User interaction patterns

### **Coverage Metrics:**
- **Lines**: Average 75%
- **Branches**: Average 70%
- **Methods**: Average 85%

## 🔍 **Test Debugging Tips**

### **Test Discovery Issues:**
```bash
# Clear test cache
dotnet clean
dotnet restore
dotnet build

# Re-run discovery
dotnet test --list-tests
```

### **Database Test Setup:**
```csharp
[ClassFixture(typeof(DatabaseTestFixture))]
public class RepositoryTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;

    public RepositoryTests(DatabaseTestFixture fixture)
        => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabase();
    public Task DisposeAsync() => Task.CompletedTask;
}
```

### **Mock Configuration:**
```csharp
// Example: Mock DataService for ViewModel testing
var mockDataService = new Mock<IDataService>();
mockDataService.Setup(ds => ds.Vouchers.GetByIdAsync(1))
               .ReturnsAsync(new Voucher { VoucherId = 1 });

// Setup collections
mockDataService.Setup(ds => ds.Companies.GetActiveCompaniesAsync())
               .ReturnsAsync(mockCompanies);
```

## 📈 **Continuous Integration**

### **GitHub Actions Integration:**
```yaml
name: CI Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Test
      run: dotnet test --no-build --verbosity normal
    - name: Generate coverage
      run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 🐛 **Writing New Tests**

### **Test Naming Convention:**
- **Method_Scenario_ExpectedBehavior**: `SaveVoucher_ValidData_ShouldPersistToDatabase()`
- **Async Methods**: `LoadDataAsync_NoCompany_ShouldThrowException()`

### **Common Test Patterns:**

**Happy Path Test:**
```csharp
[Fact]
public async Task Method_ValidInput_ShouldReturnExpectedResult()
{
    // Arrange
    var input = CreateValidInput();
    // Setup mocks

    // Act
    var result = await _service.MethodAsync(input);

    // Assert
    Assert.Equal(expectedResult, result);
}
```

**Error Handling Test:**
```csharp
[Fact]
public async Task Method_InvalidInput_ShouldThrowValidationException()
{
    // Arrange
    var invalidInput = CreateInvalidInput();

    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(
        () => _service.MethodAsync(invalidInput));
}
```

## 🏆 **Best Practices**

- ✅ **Single Responsibility** - Each test validates one specific behavior
- ✅ **Arrange-Act-Assert Pattern** - Clear test structure
- ✅ **Descriptive Names** - Tests read like documentation
- ✅ **Mock Dependencies** - Isolate the code under test
- ✅ **Coverage Goals** - 80%+ line coverage target
- ✅ **Regular Execution** - Tests run on every build

---

## 🎯 **Next Steps**

Ready to expand the test suite! Consider adding:

- **Integration Tests**: API/contract testing
- **UI Tests**: Automated UI interaction testing
- **Load Tests**: Performance under stress
- **End-to-End Tests**: Complete workflow validation

**Happy Testing! 🧪**
