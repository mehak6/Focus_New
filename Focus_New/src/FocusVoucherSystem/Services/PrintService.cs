using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FocusVoucherSystem.Models;

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

    public void PrintRecoveryDirectly(string title, IEnumerable<RecoveryItem> recoveryItems)
    {
        var doc = BuildRecoveryDocument(title, recoveryItems);

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

    public FlowDocument BuildRecoveryDocument(string title, IEnumerable<RecoveryItem> recoveryItems)
    {
        var items = recoveryItems.ToList();
        var totalVehicles = items.Count(x => !x.IsGroupHeader);
        var totalOutstanding = items.Where(x => !x.IsGroupHeader).Sum(x => x.RemainingBalance);

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12, // Smaller font for better fit
            PagePadding = new Thickness(15, 20, 15, 20), // Minimal margins - Left, Top, Right, Bottom
            ColumnWidth = double.PositiveInfinity // Allow table to use full width
        };

        // Title header - BLACK AND WHITE
        var titleParagraph = new Paragraph(new Run(title))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5),
            Foreground = Brushes.Black
        };
        doc.Blocks.Add(titleParagraph);

        // Metadata - BLACK AND WHITE
        var metaParagraph = new Paragraph()
        {
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5),
            Foreground = Brushes.Black
        };
        metaParagraph.Inlines.Add(new Run($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}"));
        doc.Blocks.Add(metaParagraph);

        // Separator - BLACK AND WHITE
        var separator = new Paragraph()
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0, 0, 0, 2),
            Margin = new Thickness(0, 0, 0, 8)
        };
        doc.Blocks.Add(separator);

        // Summary section - BLACK AND WHITE (WITHOUT total outstanding)
        var summaryParagraph = new Paragraph()
        {
            Background = Brushes.White,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 10),
            TextAlignment = TextAlignment.Center,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };
        summaryParagraph.Inlines.Add(new Run("Total Vehicles: ") { FontWeight = FontWeights.SemiBold });
        summaryParagraph.Inlines.Add(new Run(totalVehicles.ToString())
        {
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black
        });
        doc.Blocks.Add(summaryParagraph);

        // Table - BLACK AND WHITE with Auto width
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };
        doc.Blocks.Add(table);

        // Columns with Auto width to fit page - BLACK AND WHITE
        // NEW ORDER: Balance (left) -> Last Credit -> Last Date -> Days -> Vehicle Number (right)
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });  // Balance - Auto
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });  // Last Credit - Auto
        table.Columns.Add(new TableColumn { Width = new GridLength(0.9, GridUnitType.Star) }); // Last Date - Slightly smaller
        table.Columns.Add(new TableColumn { Width = new GridLength(0.6, GridUnitType.Star) }); // Days - Smaller
        table.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) }); // Vehicle Number - Bigger

        // Header row - BLACK AND WHITE
        var headerGroup = new TableRowGroup();
        table.RowGroups.Add(headerGroup);
        var hrow = new TableRow
        {
            Background = Brushes.Black
        };
        headerGroup.Rows.Add(hrow);

        // NEW ORDER: Balance -> Last Credit -> Last Date -> Days -> Vehicle Number
        string[] headers = ["Balance", "Last Credit", "Last Date", "Days", "Vehicle Number"];
        TextAlignment[] alignments = [TextAlignment.Right, TextAlignment.Right, TextAlignment.Center, TextAlignment.Center, TextAlignment.Left];

        for (int i = 0; i < headers.Length; i++)
        {
            var cellPara = new Paragraph(new Run(headers[i]))
            {
                Margin = new Thickness(0),
                TextAlignment = alignments[i]
            };
            var cell = new TableCell(cellPara)
            {
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(4, 6, 4, 6),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            hrow.Cells.Add(cell);
        }

        // Data rows
        var body = new TableRowGroup();
        table.RowGroups.Add(body);
        int rowIndex = 0;

        foreach (var item in items)
        {
            var row = new TableRow();
            body.Rows.Add(row);

            if (item.IsGroupHeader)
            {
                // Group header styling - span all columns in one cell
                var groupPara = new Paragraph(new Run(item.VehicleNumber))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var groupHeaderCell = new TableCell(groupPara)
                {
                    Padding = new Thickness(5, 6, 5, 6),
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Background = Brushes.LightGray, // BLACK AND WHITE - Light gray for groups
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 1, 0, 1),
                    ColumnSpan = 5
                };
                row.Cells.Add(groupHeaderCell);
            }
            else
            {
                // Alternating row colors - BLACK AND WHITE
                var rowBackground = (rowIndex % 2 == 0)
                    ? Brushes.White
                    : Brushes.White; // All white for cleaner black and white print
                row.Background = rowBackground;

                // NEW ORDER: Balance -> Last Credit -> Last Date -> Days -> Vehicle Number

                // 1. Remaining Balance (LEFT - FIRST COLUMN) - BLACK AND WHITE
                var balancePara = new Paragraph(new Run($"₹{item.RemainingBalance:N2}"))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Right
                };
                row.Cells.Add(new TableCell(balancePara)
                {
                    Padding = new Thickness(4, 4, 4, 4),
                    FontWeight = FontWeights.Bold,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });

                // 2. Last Credit Amount - BLACK AND WHITE
                var amountText = item.LastAmount > 0 ? $"₹{item.LastAmount:N2}" : "₹0.00";
                var amountPara = new Paragraph(new Run(amountText))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Right
                };
                row.Cells.Add(new TableCell(amountPara)
                {
                    Padding = new Thickness(4, 4, 4, 4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });

                // 3. Last Date - BLACK AND WHITE
                var dateText = item.LastDate?.ToString("dd/MM/yyyy") ?? "Never";
                var datePara = new Paragraph(new Run(dateText))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                row.Cells.Add(new TableCell(datePara)
                {
                    Padding = new Thickness(4, 4, 4, 4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });

                // 4. Days (Status) - BLACK AND WHITE
                var statusRun = new Run(item.CreditStatus);
                statusRun.Foreground = Brushes.Black; // BLACK AND WHITE - all black text

                var statusPara = new Paragraph(statusRun)
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                row.Cells.Add(new TableCell(statusPara)
                {
                    Padding = new Thickness(4, 4, 4, 4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });

                // 5. Vehicle Number (RIGHT - LAST COLUMN - BOLD AND BIGGER) - BLACK AND WHITE
                var vehicleRun = new Run(item.VehicleNumber)
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 14 // Reduced from 16 for better fit
                };
                var vehiclePara = new Paragraph(vehicleRun) { Margin = new Thickness(0) };
                row.Cells.Add(new TableCell(vehiclePara)
                {
                    Padding = new Thickness(4, 4, 4, 4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });

                rowIndex++;
            }
        }

        // Footer summary - BLACK AND WHITE (WITHOUT total outstanding)
        var footerParagraph = new Paragraph()
        {
            Background = Brushes.White,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 10, 0, 0),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };
        footerParagraph.Inlines.Add(new Run("Grand Total: ") { FontWeight = FontWeights.Bold });
        footerParagraph.Inlines.Add(new Run($"{totalVehicles} vehicles"));
        doc.Blocks.Add(footerParagraph);

        return doc;
    }
}
