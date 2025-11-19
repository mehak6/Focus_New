using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FocusVoucherSystem.Views;

public partial class ReportsView : UserControl
{
    private DatePickerTextBox? _startDateTextBox;
    private DatePickerTextBox? _endDateTextBox;

    public ReportsView()
    {
        InitializeComponent();
    }

    private void StartDatePicker_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker datePicker && _startDateTextBox == null)
        {
            // Find the internal TextBox within the DatePicker
            _startDateTextBox = FindVisualChild<DatePickerTextBox>(datePicker);
            if (_startDateTextBox != null)
            {
                // Subscribe to the date change to update the format
                datePicker.SelectedDateChanged += StartDatePicker_SelectedDateChanged;

                // Set initial format if there's already a selected date
                UpdateDatePickerFormat(datePicker, _startDateTextBox);
            }
        }
    }

    private void EndDatePicker_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker datePicker && _endDateTextBox == null)
        {
            // Find the internal TextBox within the DatePicker
            _endDateTextBox = FindVisualChild<DatePickerTextBox>(datePicker);
            if (_endDateTextBox != null)
            {
                // Subscribe to the date change to update the format
                datePicker.SelectedDateChanged += EndDatePicker_SelectedDateChanged;

                // Set initial format if there's already a selected date
                UpdateDatePickerFormat(datePicker, _endDateTextBox);
            }
        }
    }

    private void StartDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DatePicker datePicker && _startDateTextBox != null)
        {
            UpdateDatePickerFormat(datePicker, _startDateTextBox);
        }
    }

    private void EndDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DatePicker datePicker && _endDateTextBox != null)
        {
            UpdateDatePickerFormat(datePicker, _endDateTextBox);
        }
    }

    private static void UpdateDatePickerFormat(DatePicker datePicker, DatePickerTextBox textBox)
    {
        if (datePicker.SelectedDate.HasValue)
        {
            // Use Dispatcher to avoid interfering with the binding update
            datePicker.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.Text = datePicker.SelectedDate.Value.ToString("dd/MM/yyyy");
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}

