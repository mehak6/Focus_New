using CommunityToolkit.Mvvm.ComponentModel;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.Services;
using System.Collections.ObjectModel;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the company selection window
/// </summary>
public partial class CompanySelectionViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _statusMessage = "Loading companies...";

    [ObservableProperty]
    private ObservableCollection<Company> _companies = new();

    public CompanySelectionViewModel(DataService dataService) : base(dataService)
    {
    }

    /// <summary>
    /// Initializes the view model by loading companies
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            StatusMessage = "Initializing database...";
            
            // Initialize database
            await _dataService.InitializeDatabaseAsync();
            
            StatusMessage = "Loading companies...";
            
            // Load companies
            await LoadCompaniesAsync();
            
            if (Companies.Any())
            {
                StatusMessage = $"Found {Companies.Count} company(ies). Select one to continue.";
            }
            else
            {
                StatusMessage = "No companies found. Create a new company to get started.";
            }
        }
        catch (Exception ex)
        {
            var detailsMessage = $"Error during initialization: {ex.Message}";
            if (ex.InnerException != null)
            {
                detailsMessage += $"\nInner: {ex.InnerException.Message}";
            }
            StatusMessage = detailsMessage;
            throw new InvalidOperationException(detailsMessage, ex); // Re-throw with details
        }
    }

    /// <summary>
    /// Loads all active companies from the database
    /// </summary>
    private async Task LoadCompaniesAsync()
    {
        try
        {
            var companies = await _dataService.Companies.GetActiveCompaniesAsync();
            Companies.Clear();
            foreach (var company in companies)
            {
                Companies.Add(company);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading companies: {ex.Message}";
            throw;
        }
    }

    /// <summary>
    /// Creates a new company
    /// </summary>
    public async Task<Company?> CreateCompanyAsync(string companyName)
    {
        try
        {
            var currentDate = DateTime.Now;
            var company = new Company
            {
                Name = companyName.Trim(),
                FinancialYearStart = new DateTime(currentDate.Year, 4, 1), // April 1st
                FinancialYearEnd = new DateTime(currentDate.Year + 1, 3, 31), // March 31st next year
                LastVoucherNumber = 0,
                IsActive = true
            };

            var createdCompany = await _dataService.Companies.AddAsync(company);
            
            // Add to the collection
            Companies.Add(createdCompany);
            
            // Save as default company setting
            await _dataService.Settings.SetValueAsync("DefaultCompanyId", createdCompany.CompanyId);
            
            return createdCompany;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating company: {ex.Message}";
            throw;
        }
    }

    /// <summary>
    /// Refreshes the companies list
    /// </summary>
    public async Task RefreshCompaniesAsync()
    {
        StatusMessage = "Refreshing companies...";
        await LoadCompaniesAsync();
        
        if (Companies.Any())
        {
            StatusMessage = $"Found {Companies.Count} company(ies).";
        }
        else
        {
            StatusMessage = "No companies found.";
        }
    }

    /// <summary>
    /// Deletes the specified company, including its related data
    /// </summary>
    public async Task<bool> DeleteCompanyAsync(Company company)
    {
        try
        {
            StatusMessage = $"Deleting company '{company.Name}'...";

            // Clear related data first (vouchers, vehicles, etc.)
            await _dataService.ClearCompanyDataAsync(company.CompanyId);

            // Delete the company record
            var deleted = await _dataService.Companies.DeleteAsync(company.CompanyId);

            if (deleted)
            {
                // Remove from observable collection
                var existing = Companies.FirstOrDefault(c => c.CompanyId == company.CompanyId);
                if (existing != null)
                {
                    Companies.Remove(existing);
                }

                // If the deleted company was set as default, clear the setting
                var defaultId = await _dataService.Settings.GetValueAsync<int>("DefaultCompanyId");
                if (defaultId == company.CompanyId)
                {
                    await _dataService.Settings.SetValueAsync("DefaultCompanyId", 0);
                }

                StatusMessage = $"Company '{company.Name}' deleted successfully.";
                return true;
            }
            else
            {
                StatusMessage = $"Failed to delete company '{company.Name}'.";
                return false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting company: {ex.Message}";
            return false;
        }
    }
}
