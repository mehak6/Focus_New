using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.Services;
using Dapper;

namespace FocusVoucherSystem.Views;

public partial class SimpleMergeVehiclesDialog : Window
{
    private readonly DataService _dataService;
    private readonly Company _currentCompany;
    private readonly List<VehicleDisplayItem> _vehicles;
    private VehicleDisplayItem? _sourceVehicle;
    private VehicleDisplayItem? _targetVehicle;

    // Search state
    private string _sourceSearchText = string.Empty;
    private string _targetSearchText = string.Empty;
    private List<VehicleDisplayItem> _filteredSourceVehicles = new();
    private List<VehicleDisplayItem> _filteredTargetVehicles = new();
    private bool _showSourceSuggestions;
    private bool _showTargetSuggestions;

    public bool MergeSuccessful { get; private set; }

    public SimpleMergeVehiclesDialog(DataService dataService, Company currentCompany, IEnumerable<VehicleDisplayItem> vehicles)
    {
        InitializeComponent();

        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _currentCompany = currentCompany ?? throw new ArgumentNullException(nameof(currentCompany));
        _vehicles = vehicles?.Where(v => v.IsActive).ToList() ?? new List<VehicleDisplayItem>();

        LoadVehicles();
    }

    private void LoadVehicles()
    {
        try
        {
            // Initialize search lists
            UpdateSourceVehicleFilter();
            UpdateTargetVehicleFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading vehicles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Vehicle Search Functionality

    private void UpdateSourceVehicleFilter()
    {
        var term = _sourceSearchText.Trim();
        _filteredSourceVehicles.Clear();

        if (string.IsNullOrWhiteSpace(term))
        {
            // Show all available vehicles initially
            _filteredSourceVehicles.AddRange(_vehicles.Take(10));
        }
        else
        {
            var matches = _vehicles
                .Where(v => MatchesSearchTerm(v, term))
                .OrderBy(v => GetSearchRelevanceScore(v, term))
                .Take(20);

            _filteredSourceVehicles.AddRange(matches);
        }

        UpdateSourceSuggestionsList();
    }

    private void UpdateTargetVehicleFilter()
    {
        var term = _targetSearchText.Trim();
        _filteredTargetVehicles.Clear();

        // Exclude source vehicle from target options
        var availableVehicles = _vehicles.Where(v => v.VehicleId != _sourceVehicle?.VehicleId);

        if (string.IsNullOrWhiteSpace(term))
        {
            // Show available vehicles initially
            _filteredTargetVehicles.AddRange(availableVehicles.Take(10));
        }
        else
        {
            var matches = availableVehicles
                .Where(v => MatchesSearchTerm(v, term))
                .OrderBy(v => GetSearchRelevanceScore(v, term))
                .Take(20);

            _filteredTargetVehicles.AddRange(matches);
        }

        UpdateTargetSuggestionsList();
    }

    private bool MatchesSearchTerm(VehicleDisplayItem vehicle, string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;

        var searchTerm = term.ToLowerInvariant();
        var vehicleNumber = vehicle.VehicleNumber?.ToLowerInvariant() ?? "";
        var description = vehicle.Description?.ToLowerInvariant() ?? "";

        return vehicleNumber.Contains(searchTerm) || description.Contains(searchTerm);
    }

    private int GetSearchRelevanceScore(VehicleDisplayItem vehicle, string term)
    {
        var searchTerm = term.ToLowerInvariant();
        var vehicleNumber = vehicle.VehicleNumber?.ToLowerInvariant() ?? "";
        var description = vehicle.Description?.ToLowerInvariant() ?? "";

        // Exact match at start gets highest priority
        if (vehicleNumber.StartsWith(searchTerm)) return 1;
        if (description.StartsWith(searchTerm)) return 2;

        // Contains match gets lower priority
        if (vehicleNumber.Contains(searchTerm)) return 3;
        if (description.Contains(searchTerm)) return 4;

        return 5;
    }

    private void UpdateSourceSuggestionsList()
    {
        SourceSuggestionsList.Items.Clear();
        foreach (var vehicle in _filteredSourceVehicles)
        {
            SourceSuggestionsList.Items.Add(vehicle);
        }
        SourceSuggestionsList.DisplayMemberPath = "DisplayName";
    }

    private void UpdateTargetSuggestionsList()
    {
        TargetSuggestionsList.Items.Clear();
        foreach (var vehicle in _filteredTargetVehicles)
        {
            TargetSuggestionsList.Items.Add(vehicle);
        }
        TargetSuggestionsList.DisplayMemberPath = "DisplayName";
    }

    private void SelectSourceVehicle(VehicleDisplayItem vehicle)
    {
        _sourceVehicle = vehicle;
        SourceSearchTextBox.Text = vehicle.DisplayName;
        _sourceSearchText = vehicle.DisplayName;
        _showSourceSuggestions = false;
        SourceSuggestionsPopup.IsOpen = false;

        // Update target vehicle filter to exclude selected source
        UpdateTargetVehicleFilter();
        UpdateSummary();
    }

    private void SelectTargetVehicle(VehicleDisplayItem vehicle)
    {
        _targetVehicle = vehicle;
        TargetSearchTextBox.Text = vehicle.DisplayName;
        _targetSearchText = vehicle.DisplayName;
        _showTargetSuggestions = false;
        TargetSuggestionsPopup.IsOpen = false;

        UpdateSummary();
    }

    #endregion

    private async void UpdateSummary()
    {
        try
        {
            if (_sourceVehicle != null && _targetVehicle != null && _sourceVehicle.VehicleId != _targetVehicle.VehicleId)
            {
                var sourceCount = await GetVoucherCountAsync(_sourceVehicle.VehicleId);
                var targetCount = await GetVoucherCountAsync(_targetVehicle.VehicleId);

                SummaryText.Text = $"Source: {_sourceVehicle.VehicleNumber} ({sourceCount} vouchers, Balance: {_sourceVehicle.FormattedBalance})\n" +
                                  $"Target: {_targetVehicle.VehicleNumber} ({targetCount} vouchers, Balance: {_targetVehicle.FormattedBalance})";

                SummaryBorder.Visibility = Visibility.Visible;
                MergeButton.IsEnabled = true;
            }
            else
            {
                SummaryBorder.Visibility = Visibility.Collapsed;
                MergeButton.IsEnabled = false;
            }
        }
        catch (Exception)
        {
            SummaryBorder.Visibility = Visibility.Collapsed;
            MergeButton.IsEnabled = false;
            // Don't show error for summary updates - just disable the button
        }
    }

    private async Task<int> GetVoucherCountAsync(int vehicleId)
    {
        try
        {
            var connection = await _dataService.GetConnectionAsync();
            const string sql = "SELECT COUNT(*) FROM Vouchers WHERE VehicleId = @VehicleId";
            return await connection.QuerySingleAsync<int>(sql, new { VehicleId = vehicleId });
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Merge_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceVehicle == null || _targetVehicle == null)
        {
            MessageBox.Show("Please select both source and target vehicles.", "Selection Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_sourceVehicle.VehicleId == _targetVehicle.VehicleId)
        {
            MessageBox.Show("Source and target vehicles must be different.", "Invalid Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sourceCount = await GetVoucherCountAsync(_sourceVehicle.VehicleId);

        var confirmResult = MessageBox.Show(
            $"Are you absolutely sure you want to merge these vehicles?\n\n" +
            $"Source: {_sourceVehicle.VehicleNumber}\n" +
            $"Target: {_targetVehicle.VehicleNumber}\n\n" +
            $"This will transfer all {sourceCount} vouchers from source to target " +
            $"and permanently delete the source vehicle.\n\n" +
            $"THIS ACTION CANNOT BE UNDONE!",
            "Confirm Merge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes)
            return;

        try
        {
            MergeButton.IsEnabled = false;
            MergeButton.Content = "Merging...";

            var success = await _dataService.Vehicles.MergeVehiclesAsync(_sourceVehicle.VehicleId, _targetVehicle.VehicleId);

            if (success)
            {
                MessageBox.Show(
                    $"Vehicles merged successfully!\n\n" +
                    $"All vouchers from '{_sourceVehicle.VehicleNumber}' have been transferred to " +
                    $"'{_targetVehicle.VehicleNumber}' and the source vehicle has been deleted.",
                    "Merge Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                MergeSuccessful = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Failed to merge vehicles. Please try again.",
                    "Merge Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                MergeButton.Content = "🔄 Merge Vehicles";
                MergeButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while merging vehicles:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            MergeButton.Content = "🔄 Merge Vehicles";
            MergeButton.IsEnabled = true;
        }
    }

    #region Event Handlers

    // Source TextBox Events
    private void SourceSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _sourceSearchText = SourceSearchTextBox.Text ?? string.Empty;
        UpdateSourceVehicleFilter();
        _showSourceSuggestions = !string.IsNullOrWhiteSpace(_sourceSearchText);
        SourceSuggestionsPopup.IsOpen = _showSourceSuggestions && _filteredSourceVehicles.Any();
    }

    private void SourceSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        SourceSearchTextBox_KeyDown(sender, e);
    }

    private void SourceSearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Select first vehicle if available
            if (_filteredSourceVehicles.Any())
            {
                SelectSourceVehicle(_filteredSourceVehicles.First());
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down || e.Key == Key.Up)
        {
            if (SourceSuggestionsList.Items.Count > 0)
            {
                SourceSuggestionsPopup.IsOpen = true;
                _showSourceSuggestions = true;

                var index = SourceSuggestionsList.SelectedIndex;
                if (e.Key == Key.Down)
                {
                    index = index < 0 ? 0 : Math.Min(index + 1, SourceSuggestionsList.Items.Count - 1);
                }
                else // Up
                {
                    index = index < 0 ? SourceSuggestionsList.Items.Count - 1 : Math.Max(index - 1, 0);
                }

                SourceSuggestionsList.SelectedIndex = index;
                SourceSuggestionsList.Focus();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            SourceSuggestionsPopup.IsOpen = false;
            _showSourceSuggestions = false;
            e.Handled = true;
        }
    }

    // Target TextBox Events
    private void TargetSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _targetSearchText = TargetSearchTextBox.Text ?? string.Empty;
        UpdateTargetVehicleFilter();
        _showTargetSuggestions = !string.IsNullOrWhiteSpace(_targetSearchText);
        TargetSuggestionsPopup.IsOpen = _showTargetSuggestions && _filteredTargetVehicles.Any();
    }

    private void TargetSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        TargetSearchTextBox_KeyDown(sender, e);
    }

    private void TargetSearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Select first vehicle if available
            if (_filteredTargetVehicles.Any())
            {
                SelectTargetVehicle(_filteredTargetVehicles.First());
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down || e.Key == Key.Up)
        {
            if (TargetSuggestionsList.Items.Count > 0)
            {
                TargetSuggestionsPopup.IsOpen = true;
                _showTargetSuggestions = true;

                var index = TargetSuggestionsList.SelectedIndex;
                if (e.Key == Key.Down)
                {
                    index = index < 0 ? 0 : Math.Min(index + 1, TargetSuggestionsList.Items.Count - 1);
                }
                else // Up
                {
                    index = index < 0 ? TargetSuggestionsList.Items.Count - 1 : Math.Max(index - 1, 0);
                }

                TargetSuggestionsList.SelectedIndex = index;
                TargetSuggestionsList.Focus();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            TargetSuggestionsPopup.IsOpen = false;
            _showTargetSuggestions = false;
            e.Handled = true;
        }
    }

    // Source Suggestions List Events
    private void SourceSuggestionsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        SourceSuggestionsList_KeyDown(sender, e);
    }

    private void SourceSuggestionsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SourceSuggestionsList.SelectedItem is VehicleDisplayItem vehicle)
        {
            SelectSourceVehicle(vehicle);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SourceSuggestionsPopup.IsOpen = false;
            _showSourceSuggestions = false;
            SourceSearchTextBox.Focus();
            e.Handled = true;
        }
    }

    private void SourceSuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SourceSuggestionsList.SelectedItem is VehicleDisplayItem vehicle)
        {
            SelectSourceVehicle(vehicle);
        }
    }

    // Target Suggestions List Events
    private void TargetSuggestionsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        TargetSuggestionsList_KeyDown(sender, e);
    }

    private void TargetSuggestionsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && TargetSuggestionsList.SelectedItem is VehicleDisplayItem vehicle)
        {
            SelectTargetVehicle(vehicle);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            TargetSuggestionsPopup.IsOpen = false;
            _showTargetSuggestions = false;
            TargetSearchTextBox.Focus();
            e.Handled = true;
        }
    }

    private void TargetSuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TargetSuggestionsList.SelectedItem is VehicleDisplayItem vehicle)
        {
            SelectTargetVehicle(vehicle);
        }
    }

    // Popup Events
    private void SourceSuggestionsPopup_Closed(object? sender, EventArgs e)
    {
        _showSourceSuggestions = false;
    }

    private void TargetSuggestionsPopup_Closed(object? sender, EventArgs e)
    {
        _showTargetSuggestions = false;
    }

    #endregion
}