using System.Windows;
using System.Windows.Input;
using FocusVoucherSystem.Services;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Login window for password protection with secure hashing and encryption key derivation
/// </summary>
public partial class LoginWindow : Window
{
    public bool IsAuthenticated { get; private set; }
    public string? EncryptionKey { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        PasswordInput.Focus();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ValidatePassword();
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ValidatePassword();
        }
    }

    private void ValidatePassword()
    {
        var enteredPassword = PasswordInput.Password;

        if (string.IsNullOrEmpty(enteredPassword))
        {
            ShowError("Please enter a password");
            return;
        }

        // Hash the entered password and compare with stored hash
        var enteredPasswordHash = SecurityService.ComputeSha256Hash(enteredPassword);
        var correctPasswordHash = SecurityService.GetPasswordHash();

        if (enteredPasswordHash.Equals(correctPasswordHash, StringComparison.OrdinalIgnoreCase))
        {
            IsAuthenticated = true;
            // Derive encryption key from password for database encryption
            EncryptionKey = SecurityService.DeriveEncryptionKey(enteredPassword);
            DialogResult = true;
            Close();
        }
        else
        {
            IsAuthenticated = false;
            EncryptionKey = null;
            ShowError("Incorrect password. Access denied.");
            PasswordInput.Clear();
            PasswordInput.Focus();
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage.Text = message;
        ErrorMessage.Visibility = Visibility.Visible;
    }
}
