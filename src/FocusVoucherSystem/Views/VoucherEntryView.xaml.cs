using System.Windows.Controls;
using System.Windows.Input;
using FocusVoucherSystem.ViewModels;

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
}