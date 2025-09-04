using System.Windows;
using System.Windows.Input;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Dialog for entering company name during initial setup
/// </summary>
public partial class CompanyInputDialog : Window
{
    public string? CompanyName { get; private set; }

    public CompanyInputDialog()
    {
        System.Diagnostics.Debug.WriteLine($"CompanyInputDialog constructor called");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine($"CompanyInputDialog constructor completed");
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        CompanyName = CompanyNameTextBox.Text.Trim();
        System.Diagnostics.Debug.WriteLine($"CompanyInputDialog.OKButton_Click: CompanyName='{CompanyName}'");

        if (!string.IsNullOrWhiteSpace(CompanyName))
        {
            System.Diagnostics.Debug.WriteLine($"CompanyInputDialog.OKButton_Click: Setting DialogResult to true");
            DialogResult = true;
            Close();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"CompanyInputDialog.OKButton_Click: CompanyName is empty/null");
            MessageBox.Show("Please enter a company name.", "Invalid Input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CompanyNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OKButton_Click(sender, e);
        }
    }
}
