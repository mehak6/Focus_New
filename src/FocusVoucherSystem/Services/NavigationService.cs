using System.Windows.Controls;
using FocusVoucherSystem.ViewModels;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Service for managing navigation between different views in the application
/// </summary>
public class NavigationService
{
    private readonly Dictionary<string, Type> _viewTypes;
    private readonly Dictionary<string, Type> _viewModelTypes;
    private ContentControl? _navigationHost;
    private readonly Stack<string> _navigationHistory;

    public NavigationService()
    {
        _viewTypes = new Dictionary<string, Type>();
        _viewModelTypes = new Dictionary<string, Type>();
        _navigationHistory = new Stack<string>();
    }

    /// <summary>
    /// Gets the current view key
    /// </summary>
    public string? CurrentView { get; private set; }

    /// <summary>
    /// Event raised when navigation occurs
    /// </summary>
    public event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>
    /// Sets the navigation host control
    /// </summary>
    /// <param name="host">The ContentControl that will host the views</param>
    public void SetNavigationHost(ContentControl host)
    {
        _navigationHost = host;
    }

    /// <summary>
    /// Registers a view and its corresponding ViewModel
    /// </summary>
    /// <param name="key">Unique key for the view</param>
    /// <param name="viewType">Type of the view (UserControl)</param>
    /// <param name="viewModelType">Type of the ViewModel</param>
    public void RegisterView(string key, Type viewType, Type? viewModelType = null)
    {
        _viewTypes[key] = viewType;
        if (viewModelType != null)
        {
            _viewModelTypes[key] = viewModelType;
        }
    }

    /// <summary>
    /// Navigates to a specific view
    /// </summary>
    /// <param name="viewKey">Key of the view to navigate to</param>
    /// <param name="parameters">Optional parameters to pass to the ViewModel</param>
    /// <returns>True if navigation succeeded</returns>
    public async Task<bool> NavigateToAsync(string viewKey, object? parameters = null)
    {
        System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Starting navigation to {viewKey}");

        if (_navigationHost == null)
        {
            throw new InvalidOperationException("Navigation host not set. Call SetNavigationHost first.");
        }

        if (!_viewTypes.ContainsKey(viewKey))
        {
            throw new ArgumentException($"View with key '{viewKey}' is not registered.");
        }

        try
        {
            // Create view instance
            var viewType = _viewTypes[viewKey];
            System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Creating view of type {viewType.Name}");

            var view = Activator.CreateInstance(viewType) as UserControl;

            if (view == null)
            {
                throw new InvalidOperationException($"Failed to create view of type {viewType.Name}");
            }

            System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: View created successfully");

            // Create and set ViewModel if registered
            if (_viewModelTypes.ContainsKey(viewKey))
            {
                var viewModelType = _viewModelTypes[viewKey];
                System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Creating ViewModel of type {viewModelType.Name}");

                var viewModel = CreateViewModel(viewModelType);

                if (viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Setting ViewModel as DataContext");
                    view.DataContext = viewModel;

                    // Initialize ViewModel with parameters
                    if (viewModel is INavigationAware navAware)
                    {
                        System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Calling OnNavigatedToAsync");
                        await navAware.OnNavigatedToAsync(parameters);
                        System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: OnNavigatedToAsync completed");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: WARNING - ViewModel creation failed");
                }
            }

            // Add to navigation history if it's a new view
            if (CurrentView != null && CurrentView != viewKey)
            {
                _navigationHistory.Push(CurrentView);
            }

            // Set the view
            System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Setting navigation host content");
            _navigationHost.Content = view;
            CurrentView = viewKey;

            // Raise navigation event
            Navigated?.Invoke(this, new NavigationEventArgs(viewKey, parameters));

            System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: Navigation to {viewKey} completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            // Log error (in a real app, you'd use a logging framework)
            System.Diagnostics.Debug.WriteLine($"NavigationService.NavigateToAsync: ERROR - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Navigates back to the previous view
    /// </summary>
    /// <returns>True if navigation succeeded</returns>
    public async Task<bool> GoBackAsync()
    {
        if (_navigationHistory.Count == 0)
            return false;

        var previousView = _navigationHistory.Pop();
        return await NavigateToAsync(previousView);
    }

    /// <summary>
    /// Checks if navigation back is possible
    /// </summary>
    public bool CanGoBack => _navigationHistory.Count > 0;

    /// <summary>
    /// Clears the navigation history
    /// </summary>
    public void ClearHistory()
    {
        _navigationHistory.Clear();
    }

    private DataService? _dataService;

    /// <summary>
    /// Sets the DataService instance for dependency injection
    /// </summary>
    public void SetDataService(DataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// Creates a ViewModel instance with dependency injection
    /// </summary>
    private object? CreateViewModel(Type viewModelType)
    {
        try
        {
            // For now, we'll use simple constructor injection
            // In a more complex app, you'd use a proper DI container
            var constructors = viewModelType.GetConstructors();
            var constructor = constructors.FirstOrDefault();
            
            if (constructor == null)
                return null;

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                
                if (paramType == typeof(DataService))
                {
                    // Use the shared DataService instance
                    args[i] = _dataService ?? throw new InvalidOperationException("DataService not set in NavigationService");
                }
                else if (paramType == typeof(NavigationService))
                {
                    args[i] = this;
                }
                else
                {
                    // For other types, try to create with parameterless constructor
                    args[i] = Activator.CreateInstance(paramType) ?? new object();
                }
            }

            return Activator.CreateInstance(viewModelType, args);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Interface for ViewModels that need to be aware of navigation
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called when navigating to this ViewModel
    /// </summary>
    /// <param name="parameters">Navigation parameters</param>
    Task OnNavigatedToAsync(object? parameters);

    /// <summary>
    /// Called when navigating away from this ViewModel
    /// </summary>
    Task OnNavigatedFromAsync();
}

/// <summary>
/// Event arguments for navigation events
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public string ViewKey { get; }
    public object? Parameters { get; }

    public NavigationEventArgs(string viewKey, object? parameters = null)
    {
        ViewKey = viewKey;
        Parameters = parameters;
    }
}
