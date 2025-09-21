using System.Windows;
using FocusVoucherSystem.ViewModels;

namespace FocusVoucherSystem.Views;

public partial class MergeVehiclesDialog : Window
{
    public MergeVehiclesDialog()
    {
        InitializeComponent();
    }

    public MergeVehiclesDialogViewModel ViewModel => (MergeVehiclesDialogViewModel)DataContext;

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Merge_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SourceVehicle == null || ViewModel.TargetVehicle == null)
        {
            MessageBox.Show("Please select both source and target vehicles.", "Selection Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ViewModel.SourceVehicle.Vehicle.VehicleId == ViewModel.TargetVehicle.Vehicle.VehicleId)
        {
            MessageBox.Show("Source and target vehicles must be different.", "Invalid Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmResult = MessageBox.Show(
            $"Are you absolutely sure you want to merge these vehicles?\n\n" +
            $"Source: {ViewModel.SourceVehicle.Vehicle.VehicleNumber}\n" +
            $"Target: {ViewModel.TargetVehicle.Vehicle.VehicleNumber}\n\n" +
            $"This will transfer all {ViewModel.SourceVehicleVoucherCount} vouchers from source to target " +
            $"and permanently delete the source vehicle.\n\n" +
            $"THIS ACTION CANNOT BE UNDONE!",
            "Confirm Merge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes)
            return;

        try
        {
            var success = await ViewModel.MergeVehiclesAsync();

            if (success)
            {
                MessageBox.Show(
                    $"Vehicles merged successfully!\n\n" +
                    $"All vouchers from '{ViewModel.SourceVehicle.Vehicle.VehicleNumber}' have been transferred to " +
                    $"'{ViewModel.TargetVehicle.Vehicle.VehicleNumber}' and the source vehicle has been deleted.",
                    "Merge Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

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
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while merging vehicles:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}