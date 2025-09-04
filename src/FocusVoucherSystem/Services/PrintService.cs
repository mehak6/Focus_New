using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FocusVoucherSystem.Services;

public class PrintService
{
    public void PrintReport(string title, IEnumerable<ViewModels.ReportRow> rows)
    {
        var doc = BuildDocument(title, rows);

        var pd = new PrintDialog();
        if (pd.ShowDialog() == true)
        {
            doc.PageHeight = pd.PrintableAreaHeight;
            doc.PageWidth = pd.PrintableAreaWidth;
            doc.ColumnWidth = pd.PrintableAreaWidth;
            IDocumentPaginatorSource idp = doc;
            pd.PrintDocument(idp.DocumentPaginator, title);
        }
    }

    public void PreviewReport(string title, IEnumerable<ViewModels.ReportRow> rows)
    {
        var doc = BuildDocument(title, rows);

        var viewer = new FlowDocumentScrollViewer
        {
            Document = doc,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(10)
        };

        var printButton = new Button { Content = "Print", Margin = new Thickness(0,0,10,0), Padding = new Thickness(10,5,10,5) };
        printButton.Click += (s, e) =>
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                doc.PageHeight = pd.PrintableAreaHeight;
                doc.PageWidth = pd.PrintableAreaWidth;
                doc.ColumnWidth = pd.PrintableAreaWidth;
                IDocumentPaginatorSource idp = doc;
                pd.PrintDocument(idp.DocumentPaginator, title);
            }
        };

        var closeButton = new Button { Content = "Close", Padding = new Thickness(10,5,10,5) };
        var window = new Window
        {
            Title = $"Print Preview - {title}",
            Width = 900,
            Height = 700,
            Content = new DockPanel()
        };

        var topBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
        topBar.Children.Add(printButton);
        topBar.Children.Add(closeButton);

        DockPanel.SetDock(topBar, Dock.Top);
        ((DockPanel)window.Content).Children.Add(topBar);
        ((DockPanel)window.Content).Children.Add(viewer);

        closeButton.Click += (s, e) => window.Close();
        window.Show();
    }

    public void PrintReportDirectly(string title, IEnumerable<ViewModels.ReportRow> rows)
    {
        var doc = BuildDocument(title, rows);

        var pd = new PrintDialog();
        if (pd.ShowDialog() == true)
        {
            doc.PageHeight = pd.PrintableAreaHeight;
            doc.PageWidth = pd.PrintableAreaWidth;
            doc.ColumnWidth = pd.PrintableAreaWidth;
            IDocumentPaginatorSource idp = doc;
            pd.PrintDocument(idp.DocumentPaginator, title);
        }
    }

    public FlowDocument BuildDocument(string title, IEnumerable<ViewModels.ReportRow> rows)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new Thickness(50)
        };

        var header = new Paragraph(new Run(title))
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Blocks.Add(header);

        var table = new Table();
        doc.Blocks.Add(table);

        // Columns
        for (int i = 0; i < 7; i++)
            table.Columns.Add(new TableColumn());

        // Header row
        var headerGroup = new TableRowGroup();
        table.RowGroups.Add(headerGroup);
        var hrow = new TableRow();
        headerGroup.Rows.Add(hrow);

        string[] headers = ["Date", "V.No", "Vehicle", "Narration", "Amount", "Dr/Cr", "Balance"];        
        foreach (var h in headers)
        {
            var cell = new TableCell(new Paragraph(new Run(h)))
            {
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(4)
            };
            hrow.Cells.Add(cell);
        }

        // Data rows
        var body = new TableRowGroup();
        table.RowGroups.Add(body);
        foreach (var r in rows)
        {
            var row = new TableRow();
            body.Rows.Add(row);

            row.Cells.Add(new TableCell(new Paragraph(new Run(r.Date.ToString("dd/MM/yyyy")))) { Padding = new Thickness(4) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(r.VoucherNumber.ToString()))) { Padding = new Thickness(4) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(r.VehicleNumber))) { Padding = new Thickness(4) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(r.Narration))) { Padding = new Thickness(4) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(r.Amount.ToString("C2")))) { Padding = new Thickness(4), TextAlignment = TextAlignment.Right });
            row.Cells.Add(new TableCell(new Paragraph(new Run(r.DrCr))) { Padding = new Thickness(4) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(r.RunningBalance.ToString("C2")))) { Padding = new Thickness(4), TextAlignment = TextAlignment.Right });
        }

        return doc;
    }
}
