using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Service for exporting recovery statements to PDF format
/// </summary>
public class PdfExportService
{
    public PdfExportService()
    {
        // Set QuestPDF license for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a PDF file for the recovery statement
    /// </summary>
    public void GenerateRecoveryPdf(
        string filePath,
        string companyName,
        int days,
        IEnumerable<RecoveryItem> recoveryItems)
    {
        var items = recoveryItems.ToList();
        var totalVehicles = items.Count(x => !x.IsGroupHeader);
        var totalOutstanding = items.Where(x => !x.IsGroupHeader).Sum(x => x.RemainingBalance);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, items));
                page.Footer().Element(footer => ComposeFooter(footer, totalVehicles, totalOutstanding));
            });
        })
        .GeneratePdf(filePath);

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                // Company name
                column.Item().AlignCenter().Text(companyName)
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.Black);

                // Report title
                column.Item().AlignCenter().Text("RECOVERY STATEMENT")
                    .FontSize(16)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken3);

                // Report metadata
                column.Item().PaddingTop(5).AlignCenter().Text(text =>
                {
                    text.Span("Vehicles with positive balance and no transactions in last ");
                    text.Span($"{days} days").Bold();
                });

                column.Item().AlignCenter().Text($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                // Separator line
                column.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1)
                    .LineColor(Colors.Grey.Darken2);
            });
        }

        void ComposeContent(IContainer container, List<RecoveryItem> items)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                // Summary statistics
                column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(summaryColumn =>
                {
                    summaryColumn.Item().Text(text =>
                    {
                        text.Span("Total Vehicles: ").SemiBold();
                        text.Span(totalVehicles.ToString()).Bold().FontColor(Colors.Black);
                        text.Span("  |  ");
                        text.Span("Total Outstanding: ").SemiBold();
                        text.Span($"₹{totalOutstanding:N2}").Bold().FontColor(Colors.Red.Darken1);
                    });
                });

                // Data table
                column.Item().PaddingTop(10).Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);   // Vehicle Number
                        columns.RelativeColumn(2);   // Last Credit Amount
                        columns.RelativeColumn(1.5f); // Last Date
                        columns.RelativeColumn(2);   // Remaining Balance
                        columns.RelativeColumn(1.5f); // Status
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("Vehicle Number").SemiBold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Last Credit").SemiBold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Last Date").SemiBold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance").SemiBold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Days").SemiBold();

                        static IContainer HeaderCellStyle(IContainer container)
                        {
                            return container
                                .Background(Colors.Grey.Darken2)
                                .Padding(8)
                                .BorderBottom(2)
                                .BorderColor(Colors.Grey.Darken3);
                        }
                    });

                    // Data rows
                    int rowIndex = 0;
                    foreach (var item in items)
                    {
                        if (item.IsGroupHeader)
                        {
                            // Group header row - span all columns
                            table.Cell().ColumnSpan(5).Element(GroupHeaderCellStyle).Text(item.VehicleNumber)
                                .Bold()
                                .FontSize(11)
                                .FontColor(Colors.Black);
                        }
                        else
                        {
                            // Determine row background color (alternating)
                            var isEvenRow = (rowIndex % 2) == 0;
                            var backgroundColor = isEvenRow ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Element(c => DataCellStyle(c, backgroundColor)).Text(item.VehicleNumber)
                                .FontSize(9);

                            table.Cell().Element(c => DataCellStyle(c, backgroundColor)).AlignRight()
                                .Text(item.LastAmount > 0 ? $"₹{item.LastAmount:N2}" : "₹0.00")
                                .FontSize(9);

                            var dateText = item.LastDate?.ToString("dd/MM/yyyy") ?? "Never";
                            table.Cell().Element(c => DataCellStyle(c, backgroundColor)).AlignCenter()
                                .Text(dateText)
                                .FontSize(9);

                            table.Cell().Element(c => DataCellStyle(c, backgroundColor)).AlignRight()
                                .Text($"₹{item.RemainingBalance:N2}")
                                .FontSize(9)
                                .Bold();

                            table.Cell().Element(c => DataCellStyle(c, backgroundColor)).AlignCenter()
                                .Text(item.CreditStatus)
                                .FontSize(9)
                                .FontColor(item.HasCredits ? Colors.Orange.Darken1 : Colors.Red.Darken1);

                            rowIndex++;
                        }
                    }

                    static IContainer GroupHeaderCellStyle(IContainer container)
                    {
                        return container
                            .Background(Colors.Grey.Lighten2)
                            .Padding(6)
                            .BorderTop(1)
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    }

                    static IContainer DataCellStyle(IContainer container, string backgroundColor)
                    {
                        return container
                            .Background(backgroundColor)
                            .Padding(6)
                            .BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2);
                    }
                });
            });
        }

        void ComposeFooter(IContainer container, int totalVehicles, decimal totalOutstanding)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                // Summary totals
                column.Item().PaddingTop(10).Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Grand Total: ").Bold().FontSize(11);
                        text.Span($"{totalVehicles} vehicles").FontSize(11);
                    });

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Outstanding: ").Bold().FontSize(11);
                        text.Span($"₹{totalOutstanding:N2}").Bold().FontSize(11).FontColor(Colors.Red.Darken2);
                    });
                });

                // Page number
                column.Item().PaddingTop(10).AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                });

                // Footer note
                column.Item().AlignCenter().Text("Generated by Focus Voucher System")
                    .FontSize(8)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            });
        }
    }
}
