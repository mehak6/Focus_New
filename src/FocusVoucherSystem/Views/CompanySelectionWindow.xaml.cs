using System.Windows;
using System.Windows.Input;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.ViewModels;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Company selection window for startup
/// </summary>
public partial class CompanySelectionWindow : Window
{
    public CompanySelectionViewModel ViewModel { get; }
    public Company? SelectedCompany { get; private set; }
    public bool ShouldContinue { get; private set; }

    public CompanySelectionWindow(CompanySelectionViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        
        // Bind the companies list
        CompaniesListBox.ItemsSource = viewModel.Companies;
        
        // Update status messages
        viewModel.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(CompanySelectionViewModel.StatusMessage))
            {
                StatusTextBlock.Text = viewModel.StatusMessage;
            }
        };
        
        // Handle double-click on list item
        CompaniesListBox.MouseDoubleClick += (s, e) => 
        {
            if (CompaniesListBox.SelectedItem is Company company)
            {
                SelectCompany(company);
            }
        };
        
        // Handle key presses
        KeyDown += CompanySelectionWindow_KeyDown;
        
        // Set focus to list if companies exist, otherwise to create button
        Loaded += (s, e) => 
        {
            if (viewModel.Companies.Any())
            {
                CompaniesListBox.Focus();
                CompaniesListBox.SelectedIndex = 0;
            }
            else
            {
                CreateCompanyButton.Focus();
            }
        };
    }

    private void CompanySelectionWindow_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (CompaniesListBox.SelectedItem is Company)
                {
                    SelectCompanyButton_Click(sender, e);
                }
                else
                {
                    CreateCompanyButton_Click(sender, e);
                }
                e.Handled = true;
                break;
                
            case Key.Escape:
                ExitButton_Click(sender, e);
                e.Handled = true;
                break;
                
            case Key.N when Keyboard.Modifiers == ModifierKeys.Control:
                CreateCompanyButton_Click(sender, e);
                e.Handled = true;
                break;
        }
    }

    private async void SelectCompanyButton_Click(object sender, RoutedEventArgs e)
    {
        if (CompaniesListBox.SelectedItem is Company selectedCompany)
        {
            SelectCompany(selectedCompany);
        }
        else
        {
            ViewModel.StatusMessage = "Please select a company first.";
        }
    }

    private async void CreateCompanyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.StatusMessage = "Opening company creation dialog...";

            var inputDialog = new CompanyInputDialog();
            var result = inputDialog.ShowDialog();

            // Add debug logging
            System.Diagnostics.Debug.WriteLine($"CompanyInputDialog.ShowDialog() returned: {result}");
            System.Diagnostics.Debug.WriteLine($"CompanyInputDialog.CompanyName: '{inputDialog.CompanyName}'");

            if (result == true && !string.IsNullOrWhiteSpace(inputDialog.CompanyName))
            {
                ViewModel.StatusMessage = $"Creating company: {inputDialog.CompanyName}";

                var newCompany = await ViewModel.CreateCompanyAsync(inputDialog.CompanyName);
                if (newCompany != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Company created successfully: {newCompany.Name} (ID: {newCompany.CompanyId})");
                    ViewModel.StatusMessage = $"Company '{newCompany.Name}' created successfully!";
                    SelectCompany(newCompany);
                }
                else
                {
                    ViewModel.StatusMessage = "Company creation failed - no company returned.";
                    MessageBox.Show("Company creation failed - no company object was returned.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ViewModel.StatusMessage = $"Company creation cancelled or failed. Result: {result}, CompanyName: '{inputDialog.CompanyName}'";
                System.Diagnostics.Debug.WriteLine($"Company creation cancelled or failed. Result: {result}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in CreateCompanyButton_Click: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            ViewModel.StatusMessage = $"Error creating company: {ex.Message}";
            MessageBox.Show($"Failed to create company: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StatusMessage = "Application closing...";
        ShouldContinue = false;
        DialogResult = false;
        Close();
    }

    private async void DeleteCompanyButton_Click(object sender, RoutedEventArgs e)
    {
        if (CompaniesListBox.SelectedItem is not Company selectedCompany)
        {
            ViewModel.StatusMessage = "Please select a company to delete.";
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete '{selectedCompany.Name}'?\n\nThis will remove all vouchers and vehicles for this company.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var success = await ViewModel.DeleteCompanyAsync(selectedCompany);
            if (success)
            {
                // Update selection/focus
                if (ViewModel.Companies.Any())
                {
                    CompaniesListBox.Focus();
                    CompaniesListBox.SelectedIndex = 0;
                }
                else
                {
                    CreateCompanyButton.Focus();
                }
            }
            else
            {
                MessageBox.Show("Failed to delete the company.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting company: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectCompany(Company company)
    {
        System.Diagnostics.Debug.WriteLine($"SelectCompany called with company: {company?.Name} (ID: {company?.CompanyId})");

        SelectedCompany = company;
        ShouldContinue = true;

        if (SelectedCompany != null && ShouldContinue)
        {
            System.Diagnostics.Debug.WriteLine($"SelectCompany: Setting DialogResult to true");
            DialogResult = true;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"SelectCompany: ERROR - SelectedCompany or ShouldContinue is false. SelectedCompany={SelectedCompany}, ShouldContinue={ShouldContinue}");
            DialogResult = false;
        }

        ViewModel.StatusMessage = $"Selected company: {company?.Name ?? "NULL"}";
        System.Diagnostics.Debug.WriteLine($"SelectCompany: About to close window. DialogResult={DialogResult}");

        // Close the window - WPF will handle visibility automatically
        Close();

        System.Diagnostics.Debug.WriteLine($"SelectCompany: Window closed");
    }
}
