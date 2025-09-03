using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// Base ViewModel class that provides common functionality for all ViewModels
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    protected readonly DataService _dataService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    protected BaseViewModel(DataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// Sets the busy state with an optional message
    /// </summary>
    /// <param name="isBusy">Whether the ViewModel is busy</param>
    /// <param name="message">Optional busy message to display</param>
    protected void SetBusy(bool isBusy, string message = "")
    {
        IsBusy = isBusy;
        BusyMessage = message;
        
        if (!isBusy)
        {
            BusyMessage = string.Empty;
        }
    }

    /// <summary>
    /// Sets an error state with a message
    /// </summary>
    /// <param name="message">Error message to display</param>
    protected void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        SetBusy(false);
    }

    /// <summary>
    /// Clears any error state
    /// </summary>
    protected void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Executes an async operation with error handling and busy state management
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="busyMessage">Optional message to show while busy</param>
    /// <returns>True if operation succeeded, false if it failed</returns>
    protected async Task<bool> ExecuteAsync(Func<Task> operation, string busyMessage = "Processing...")
    {
        if (IsBusy) return false;

        try
        {
            ClearError();
            SetBusy(true, busyMessage);
            
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            SetError($"An error occurred: {ex.Message}");
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Executes an async operation with error handling and busy state management, returning a result
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="busyMessage">Optional message to show while busy</param>
    /// <returns>The result of the operation, or default(T) if failed</returns>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string busyMessage = "Processing...")
    {
        if (IsBusy) return default;

        try
        {
            ClearError();
            SetBusy(true, busyMessage);
            
            return await operation();
        }
        catch (Exception ex)
        {
            SetError($"An error occurred: {ex.Message}");
            return default;
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Command to clear any error state
    /// </summary>
    [RelayCommand]
    protected void ClearErrors()
    {
        ClearError();
    }

    /// <summary>
    /// Virtual method for ViewModels to perform cleanup
    /// </summary>
    public virtual void Cleanup()
    {
        // Override in derived classes for specific cleanup
    }
}