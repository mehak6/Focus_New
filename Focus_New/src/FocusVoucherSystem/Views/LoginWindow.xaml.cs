using System.Windows;
using System.Windows.Input;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Login window for password protection
/// </summary>
public partial class LoginWindow : Window
{
    private const string CORRECT_PASSWORD = "mehak";
    public bool IsAuthenticated { get; private set; }

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

        if (enteredPassword == CORRECT_PASSWORD)
        {
            IsAuthenticated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            IsAuthenticated = false;
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
