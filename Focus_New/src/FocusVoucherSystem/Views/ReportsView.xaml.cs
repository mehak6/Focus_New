using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FocusVoucherSystem.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private void DatePicker_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker datePicker)
        {
            // Find the internal TextBox within the DatePicker
            var textBox = FindVisualChild<DatePickerTextBox>(datePicker);
            if (textBox != null)
            {
                // Subscribe to the date change to update the format
                datePicker.SelectedDateChanged += (s, args) =>
                {
                    if (datePicker.SelectedDate.HasValue)
                    {
                        textBox.Text = datePicker.SelectedDate.Value.ToString("dd/MM/yyyy");
                    }
                };

                // Set initial format if there's already a selected date
                if (datePicker.SelectedDate.HasValue)
                {
                    textBox.Text = datePicker.SelectedDate.Value.ToString("dd/MM/yyyy");
                }
            }
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

