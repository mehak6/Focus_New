using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository interface for Voucher-specific operations
/// </summary>
public interface IVoucherRepository : IRepository<Voucher>
{
    Task<IEnumerable<Voucher>> GetByCompanyIdAsync(int companyId);
    Task<IEnumerable<Voucher>> GetRecentVouchersAsync(int companyId, int limit = 100);
    Task<IEnumerable<Voucher>> SearchVouchersAsync(int companyId, string searchTerm, int limit = 100);
    Task<IEnumerable<Voucher>> GetByDateRangeAsync(int companyId, DateTime startDate, DateTime endDate);
    Task<(IEnumerable<Voucher> Vouchers, int TotalCount, bool HasMore)> GetByDateRangePagedAsync(int companyId, DateTime startDate, DateTime endDate, int pageSize = 1000, int offset = 0);
    Task<IEnumerable<Voucher>> GetByVehicleIdAsync(int vehicleId);
    Task<(IEnumerable<Voucher> Vouchers, int TotalCount)> GetByVehicleIdPagedAsync(int vehicleId, int pageSize = 500, int offset = 0);
    Task<IEnumerable<Voucher>> GetVehicleLedgerAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);
    Task<Voucher?> GetByVoucherNumberAsync(int companyId, int voucherNumber);
    Task<IEnumerable<Voucher>> GetDayBookAsync(int companyId, DateTime date);
    Task<bool> IsVoucherNumberUniqueAsync(int companyId, int voucherNumber, int? excludeVoucherId = null);
    Task<int> CountByCompanyAsync(int companyId);
    Task<decimal> GetVehicleBalanceAsync(int vehicleId, DateTime? upToDate = null);
    Task<(decimal TotalDebits, decimal TotalCredits)> GetDailySummaryAsync(int companyId, DateTime date);
}