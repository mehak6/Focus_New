using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Windows.Threading;
using FocusVoucherSystem.ViewModels;
using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Interaction logic for SearchView.xaml
/// </summary>
public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles keyboard navigation in the vehicle search box
    /// </summary>
    private void VehicleSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SearchViewModel viewModel) return;

        // Simple keyboard navigation
        switch (e.Key)
        {
            case Key.Down:
                if (VehicleResultsPopup.IsOpen && viewModel.VehicleSearchResults.Count > 0)
                {
                    var currentIndex = VehicleSearchListBox.SelectedIndex;
                    var newIndex = (currentIndex + 1) % viewModel.VehicleSearchResults.Count;
                    VehicleSearchListBox.SelectedIndex = newIndex;
                }
                else if (viewModel.VehicleSearchResults.Count > 0)
                {
                    viewModel.IsVehicleSearchOpen = true;
                    VehicleSearchListBox.SelectedIndex = 0;
                }
                e.Handled = true;
                break;

            case Key.Up:
                if (VehicleResultsPopup.IsOpen && viewModel.VehicleSearchResults.Count > 0)
                {
                    var currentIndex = VehicleSearchListBox.SelectedIndex;
                    var newIndex = currentIndex <= 0 ? viewModel.VehicleSearchResults.Count - 1 : currentIndex - 1;
                    VehicleSearchListBox.SelectedIndex = newIndex;
                }
                e.Handled = true;
                break;

            case Key.Enter:
                if (VehicleResultsPopup.IsOpen && VehicleSearchListBox.SelectedItem is VehicleDisplayItem selectedVehicle)
                {
                    // Directly select the vehicle
                    SelectVehicle(selectedVehicle);
                }
                e.Handled = true;
                break;

            case Key.Escape:
                viewModel.IsVehicleSearchOpen = false;
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Helper method to select a vehicle and load vouchers
    /// </summary>
    private void SelectVehicle(VehicleDisplayItem vehicle)
    {
        if (DataContext is SearchViewModel viewModel)
        {
            viewModel.VehicleSearchTerm = vehicle.DisplayName;
            viewModel.IsVehicleSearchOpen = false;
            viewModel.SelectedVehicle = vehicle;
        }
    }

    /// <summary>
    /// Handles selection changes in the vehicle search ListBox
    /// </summary>
    private void VehicleSearchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle mouse selection - select vehicle when clicked
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is VehicleDisplayItem selectedVehicle)
        {
            SelectVehicle(selectedVehicle);
        }
    }

    /// <summary>
    /// Helper method to find child controls
    /// </summary>
    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tChild)
            {
                return tChild;
            }

            var foundChild = FindChild<T>(child);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }
}