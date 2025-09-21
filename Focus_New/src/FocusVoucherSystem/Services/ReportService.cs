using FocusVoucherSystem.Models;
using System.Linq;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Provides report data assembly from repositories
/// </summary>
public class ReportService
{
    private readonly DataService _dataService;

    public ReportService(DataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// Day Book (Full Entries) for company and date range - retrieves ALL vouchers within the specified period
    /// </summary>
    public async Task<IEnumerable<Voucher>> GetDayBookAsync(int companyId, DateTime startDate, DateTime endDate)
    {
        // Use GetByDateRangeAsync to get all vouchers within the date range
        var vouchers = await _dataService.Vouchers.GetByDateRangeAsync(companyId, startDate, endDate);

        // Return vouchers ordered by date and voucher number for proper chronological display
        return vouchers.OrderBy(v => v.Date).ThenBy(v => v.VoucherNumber);
    }

    /// <summary>
    /// Day Book (Full Entries) with streaming/paginated loading for large datasets
    /// </summary>
    public async IAsyncEnumerable<(IEnumerable<Voucher> Vouchers, int ProcessedCount, int TotalCount, bool IsComplete)> GetDayBookStreamAsync(
        int companyId, DateTime startDate, DateTime endDate, int pageSize = 1000)
    {
        int offset = 0;
        int totalCount = 0;
        int processedCount = 0;
        bool hasMore = true;

        while (hasMore)
        {
            var (vouchers, total, more) = await _dataService.Vouchers.GetByDateRangePagedAsync(
                companyId, startDate, endDate, pageSize, offset);

            totalCount = total;
            var voucherList = vouchers.ToList();
            processedCount += voucherList.Count;
            hasMore = more;

            yield return (voucherList, processedCount, totalCount, !hasMore);

            offset += pageSize;
        }
    }

    /// <summary>
    /// Day Book (Consolidated): totals per date for a range
    /// </summary>
    public async Task<IEnumerable<ConsolidatedDayBookEntry>> GetDayBookConsolidatedAsync(int companyId, DateTime startDate, DateTime endDate)
    {
        var vouchers = await GetDayBookAsync(companyId, startDate, endDate);

        var grouped = vouchers
            .GroupBy(v => v.Date.Date)
            .Select(g => new ConsolidatedDayBookEntry
            {
                Date = g.Key,
                TotalDebits = g.Where(x => x.DrCr == "D").Sum(x => x.Amount),
                TotalCredits = g.Where(x => x.DrCr == "C").Sum(x => x.Amount)
            })
            .OrderBy(e => e.Date)
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Vehicle Ledger: opening balance up to startDate-1 and entries in range
    /// </summary>
    public async Task<VehicleLedgerData> GetVehicleLedgerAsync(int vehicleId, DateTime startDate, DateTime endDate)
    {
        // Opening balance is sum of Dr-Cr up to the day before startDate
        var opening = await _dataService.Vouchers.GetVehicleBalanceAsync(vehicleId, startDate.AddDays(-1));
        var entries = await _dataService.Vouchers.GetVehicleLedgerAsync(vehicleId, startDate, endDate);
        return new VehicleLedgerData
        {
            VehicleId = vehicleId,
            OpeningBalance = opening,
            Entries = entries.OrderBy(v => v.Date).ThenBy(v => v.VoucherNumber).ToList()
        };
    }

    /// <summary>
    /// Trial Balance: per-vehicle net balance as of endDate
    /// </summary>
    public async Task<IEnumerable<TrialBalanceEntry>> GetTrialBalanceAsync(int companyId, DateTime endDate)
    {
        var vehicles = await _dataService.Vehicles.GetActiveByCompanyIdAsync(companyId);
        var list = new List<TrialBalanceEntry>();
        foreach (var v in vehicles)
        {
            var bal = await _dataService.Vouchers.GetVehicleBalanceAsync(v.VehicleId, endDate);
            var drcr = bal >= 0 ? "D" : "C";
            list.Add(new TrialBalanceEntry { Vehicle = v, Amount = Math.Abs(bal), DrCr = drcr });
        }
        // Sort by vehicle number
        return list.OrderBy(t => t.Vehicle.VehicleNumber);
    }
}

public class ConsolidatedDayBookEntry
{
    public DateTime Date { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}

public class VehicleLedgerData
{
    public int VehicleId { get; set; }
    public decimal OpeningBalance { get; set; }
    public IEnumerable<Voucher> Entries { get; set; } = Array.Empty<Voucher>();
}

public class TrialBalanceEntry
{
    public Models.Vehicle Vehicle { get; set; } = new();
    public decimal Amount { get; set; }
    public string DrCr { get; set; } = "D";
}
