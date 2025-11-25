using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FocusVoucherSystem.Services;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Login window for password protection with secure hashing, encryption key derivation, and attempt tracking
/// </summary>
public partial class LoginWindow : Window
{
    public bool IsAuthenticated { get; private set; }
    public string? EncryptionKey { get; private set; }
    private int _failedAttempts = 0;

    public LoginWindow()
    {
        InitializeComponent();

        // Check if application is already locked
        if (SecurityService.IsLocked())
        {
            ShowLockoutMessage();
            return;
        }

        // Load current attempt count
        _failedAttempts = SecurityService.GetFailedAttempts();

        // Update UI if there are previous attempts
        if (_failedAttempts > 0)
        {
            UpdateAttemptDisplay();
        }

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

    private async void ValidatePassword()
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
            // SUCCESS - Reset attempt counter and proceed
            SecurityService.ResetFailedAttempts();
            IsAuthenticated = true;
            EncryptionKey = SecurityService.DeriveEncryptionKey(enteredPassword);
            DialogResult = true;
            Close();
        }
        else
        {
            // FAILED - Increment attempt counter
            _failedAttempts = SecurityService.IncrementFailedAttempts();

            if (_failedAttempts >= 3)
            {
                // LOCKOUT - Trigger emergency backup
                await HandleLockout();
            }
            else
            {
                // Show warning with remaining attempts
                UpdateAttemptDisplay();
                ShowAttemptError();
                PasswordInput.Clear();
                PasswordInput.Focus();
            }
        }
    }

    /// <summary>
    /// Handles lockout after 3 failed attempts - triggers backup and locks application
    /// </summary>
    private async Task HandleLockout()
    {
        try
        {
            // Disable controls immediately
            PasswordInput.IsEnabled = false;
            LoginButton.IsEnabled = false;

            // Show processing message
            ErrorMessage.Text = "⚠️ Too many failed attempts. Securing data...";
            ErrorMessage.Foreground = Brushes.Red;
            ErrorMessage.Visibility = Visibility.Visible;

            // Create emergency backup
            var backupService = new EmergencyBackupService();
            var backupPath = await backupService.CreateEmergencyBackup();

            // Set permanent lock
            SecurityService.SetPermanentLock();

            // Show final lockout message
            var message = $@"⚠️ SECURITY LOCKOUT ⚠️

Application locked after 3 failed password attempts.

Your data has been backed up to a secure location for recovery.

Backup Location:
{backupPath}

To recover your data:
1. Read recovery_instructions.txt in the backup folder
2. Manually restore database files
3. Delete login state file to reset lock

The application will now close.";

            MessageBox.Show(message, "Security Lockout", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Close application
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lockout handling failed: {ex.Message}");
            MessageBox.Show($"Failed to secure data: {ex.Message}\n\nApplication will close for security.",
                "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Shows lockout message when application is already locked
    /// </summary>
    private void ShowLockoutMessage()
    {
        // Disable all controls
        PasswordInput.IsEnabled = false;
        LoginButton.IsEnabled = false;

        var loginStatePath = SecurityService.GetLoginStateFilePath();

        var message = $@"⚠️ APPLICATION LOCKED ⚠️

This application is locked due to too many failed login attempts.

To recover:
1. Locate your backup in:
   {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusVoucherSystem", "SecureBackups")}

2. Follow instructions in recovery_instructions.txt

3. Delete the lock file:
   {loginStatePath}

4. Restart the application

The application will now close.";

        MessageBox.Show(message, "Application Locked", MessageBoxButton.OK, MessageBoxImage.Stop);
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Updates the attempt counter display
    /// </summary>
    private void UpdateAttemptDisplay()
    {
        if (AttemptCounter != null)
        {
            AttemptCounter.Text = $"Attempt {_failedAttempts} of 3";
            AttemptCounter.Visibility = Visibility.Visible;

            // Change color based on severity
            if (_failedAttempts == 1)
            {
                AttemptCounter.Foreground = Brushes.Orange;
            }
            else if (_failedAttempts >= 2)
            {
                AttemptCounter.Foreground = Brushes.Red;
            }
        }
    }

    /// <summary>
    /// Shows error message with attempt information
    /// </summary>
    private void ShowAttemptError()
    {
        if (_failedAttempts == 2)
        {
            ShowError($"⚠️ Incorrect password. Attempt {_failedAttempts} of 3.\n\nWARNING: One more failed attempt will lock the application and secure all data!");
            ErrorMessage.Foreground = Brushes.DarkOrange;
        }
        else
        {
            ShowError($"Incorrect password. Attempt {_failedAttempts} of 3.");
            ErrorMessage.Foreground = Brushes.OrangeRed;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage.Text = message;
        ErrorMessage.Visibility = Visibility.Visible;
    }
}
