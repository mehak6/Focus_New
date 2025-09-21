using System.Windows.Input;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Service for managing global hotkey commands throughout the application
/// </summary>
public class HotkeyService
{
    private readonly Dictionary<Key, ICommand> _globalHotkeys;
    private readonly Dictionary<Key, string> _hotkeyDescriptions;

    public HotkeyService()
    {
        _globalHotkeys = new Dictionary<Key, ICommand>();
        _hotkeyDescriptions = new Dictionary<Key, string>();
        
        InitializeDefaultDescriptions();
    }

    /// <summary>
    /// Event raised when a hotkey is pressed
    /// </summary>
    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    /// <summary>
    /// Registers a global hotkey command
    /// </summary>
    /// <param name="key">The key to register</param>
    /// <param name="command">The command to execute</param>
    /// <param name="description">Description of what this hotkey does</param>
    public void RegisterHotkey(Key key, ICommand command, string description = "")
    {
        _globalHotkeys[key] = command;
        if (!string.IsNullOrEmpty(description))
        {
            _hotkeyDescriptions[key] = description;
        }
    }

    /// <summary>
    /// Unregisters a hotkey
    /// </summary>
    /// <param name="key">The key to unregister</param>
    public void UnregisterHotkey(Key key)
    {
        _globalHotkeys.Remove(key);
        _hotkeyDescriptions.Remove(key);
    }

    /// <summary>
    /// Handles a key press and executes the associated command if registered
    /// </summary>
    /// <param name="key">The key that was pressed</param>
    /// <returns>True if a command was executed</returns>
    public bool HandleKeyPress(Key key)
    {
        if (_globalHotkeys.TryGetValue(key, out var command))
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
                HotkeyPressed?.Invoke(this, new HotkeyEventArgs(key, _hotkeyDescriptions.GetValueOrDefault(key, "")));
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Gets all registered hotkeys with their descriptions
    /// </summary>
    /// <returns>Dictionary of key to description mappings</returns>
    public Dictionary<Key, string> GetRegisteredHotkeys()
    {
        return new Dictionary<Key, string>(_hotkeyDescriptions);
    }

    /// <summary>
    /// Checks if a key is registered as a hotkey
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key is registered</returns>
    public bool IsHotkeyRegistered(Key key)
    {
        return _globalHotkeys.ContainsKey(key);
    }

    /// <summary>
    /// Gets the description for a registered hotkey
    /// </summary>
    /// <param name="key">The key to get description for</param>
    /// <returns>Description string or empty if not found</returns>
    public string GetHotkeyDescription(Key key)
    {
        return _hotkeyDescriptions.GetValueOrDefault(key, "");
    }

    /// <summary>
    /// Initializes the default hotkey descriptions based on legacy DOS system
    /// </summary>
    private void InitializeDefaultDescriptions()
    {
        _hotkeyDescriptions[Key.F1] = "Vehicle Number management";
        _hotkeyDescriptions[Key.F2] = "Add new voucher/row";
        _hotkeyDescriptions[Key.F3] = "Reports menu";
        _hotkeyDescriptions[Key.F4] = "Process Work";
        _hotkeyDescriptions[Key.F5] = "Save current operation";
        _hotkeyDescriptions[Key.F6] = "Reserved for future use";
        _hotkeyDescriptions[Key.F7] = "Reserved for future use";
        _hotkeyDescriptions[Key.F8] = "Delete selected item";
        _hotkeyDescriptions[Key.F9] = "Print current view";
        _hotkeyDescriptions[Key.Escape] = "Cancel/Exit current operation";
    }

    /// <summary>
    /// Clears all registered hotkeys
    /// </summary>
    public void ClearAllHotkeys()
    {
        _globalHotkeys.Clear();
        _hotkeyDescriptions.Clear();
        InitializeDefaultDescriptions();
    }
}

/// <summary>
/// Event arguments for hotkey events
/// </summary>
public class HotkeyEventArgs : EventArgs
{
    public Key Key { get; }
    public string Description { get; }
    public DateTime Timestamp { get; }

    public HotkeyEventArgs(Key key, string description = "")
    {
        Key = key;
        Description = description;
        Timestamp = DateTime.Now;
    }
}