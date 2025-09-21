using System.Windows.Controls;
using System.Windows.Input;
using FocusVoucherSystem.ViewModels;
using System.Linq;
using System.Windows;

namespace FocusVoucherSystem.Views;

/// <summary>
/// Interaction logic for VoucherEntryView.xaml
/// </summary>
public partial class VoucherEntryView : UserControl
{
    public VoucherEntryView()
    {
        InitializeComponent();
        
        // Make the UserControl focusable so it can receive key events
        Focusable = true;
        
        // Handle key events for hotkeys
        KeyDown += VoucherEntryView_KeyDown;
        
        // Set focus when loaded
        Loaded += (s, e) => Focus();

        DataContextChanged += VoucherEntryView_DataContextChanged;
    }
    
    private void VoucherEntryView_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is VoucherEntryViewModel viewModel)
        {
            switch (e.Key)
            {
                case Key.F5:
                    if (viewModel.SaveVoucherCommand.CanExecute(null))
                    {
                        viewModel.SaveVoucherCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
                    
                case Key.F2:
                    if (viewModel.NewVoucherCommand.CanExecute(null))
                    {
                        viewModel.NewVoucherCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
                    
                case Key.F8:
                    if (viewModel.DeleteVoucherCommand.CanExecute(null))
                    {
                        viewModel.DeleteVoucherCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }

    private void SearchVoucherTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is VoucherEntryViewModel viewModel)
        {
            var textBox = sender as TextBox;
            if (textBox != null && viewModel.SearchVoucherCommand.CanExecute(textBox.Text))
            {
                viewModel.SearchVoucherCommand.Execute(textBox.Text);
                e.Handled = true;
            }
        }
    }

    private void VoucherEntryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is VoucherEntryViewModel oldVm)
        {
            oldVm.FocusVehicleSearchRequested -= FocusVehicleSearch;
            oldVm.FocusAmountFieldRequested -= FocusAmountField;
        }
        if (e.NewValue is VoucherEntryViewModel newVm)
        {
            newVm.FocusVehicleSearchRequested += FocusVehicleSearch;
            newVm.FocusAmountFieldRequested += FocusAmountField;
        }
    }

    private void FocusVehicleSearch()
    {
        // Return focus to the vehicle search box after save
        Dispatcher.InvokeAsync(() =>
        {
            if (FindName("VehicleSearchTextBox") is TextBox tb)
            {
                tb.Focus();
            }
        });
    }

    private void FocusAmountField()
    {
        // Focus the amount field after vehicle selection
        Dispatcher.InvokeAsync(() =>
        {
            if (FindName("AmountTextBox") is TextBox amountTb)
            {
                amountTb.Focus();
                amountTb.SelectAll(); // Select all text in amount field for easy replacement
            }
        });
    }

    private void VehicleSearchTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not VoucherEntryViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            // Use the new HandleVehicleSearchEnter command that can create new vehicles
            if (vm.HandleVehicleSearchEnterCommand.CanExecute(null))
            {
                vm.HandleVehicleSearchEnterCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down || e.Key == Key.Up)
        {
            if (VehicleSuggestionsList is ListBox lb)
            {
                Action focusAndMove = () =>
                {
                    var count = lb.Items.Count;
                    if (count == 0) return;

                    var index = lb.SelectedIndex;
                    if (e.Key == Key.Down)
                    {
                        index = index < 0 ? 0 : Math.Min(index + 1, count - 1);
                    }
                    else // Up
                    {
                        index = index < 0 ? count - 1 : Math.Max(index - 1, 0);
                    }

                    lb.SelectedIndex = index;
                    lb.UpdateLayout();
                    lb.ScrollIntoView(lb.Items[index]);
                    lb.Focus();
                };

                if (!vm.ShowVehicleSuggestions && vm.FilteredVehicles.Any())
                {
                    vm.ShowVehicleSuggestions = true;
                    // Defer focusing until popup opens
                    Dispatcher.BeginInvoke(focusAndMove);
                }
                else
                {
                    focusAndMove();
                }

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            vm.ShowVehicleSuggestions = false;
            e.Handled = true;
        }
    }

    // Capture arrow/enter before TextBox default handling
    private void VehicleSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        VehicleSearchTextBox_OnKeyDown(sender, e);
    }

    private void VehicleSuggestionsList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not VoucherEntryViewModel vm) return;
        if (sender is ListBox lb && lb.SelectedItem is Models.Vehicle v)
        {
            vm.SelectVehicleCommand.Execute(v);
        }
    }

    private void VehicleSuggestionsList_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not VoucherEntryViewModel vm) return;
        if (sender is not ListBox lb) return;

        if (e.Key == Key.Enter && lb.SelectedItem is Models.Vehicle v)
        {
            vm.SelectVehicleCommand.Execute(v);
            // Return focus to the search box for continued typing
            FocusVehicleSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.ShowVehicleSuggestions = false;
            FocusVehicleSearch();
            e.Handled = true;
        }
    }

    private void VehicleSuggestionsPopup_OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is VoucherEntryViewModel vm)
        {
            vm.ShowVehicleSuggestions = false;
        }
    }

    private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        // Select all text when amount field gets focus for easy replacement
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }
}
