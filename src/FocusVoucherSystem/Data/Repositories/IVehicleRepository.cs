using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository interface for Vehicle-specific operations
/// </summary>
public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<IEnumerable<Vehicle>> GetByCompanyIdAsync(int companyId);
    Task<IEnumerable<Vehicle>> GetActiveByCompanyIdAsync(int companyId);
    Task<IEnumerable<Vehicle>> SearchVehiclesAsync(int companyId, string searchTerm);
    Task<Vehicle?> GetByVehicleNumberAsync(int companyId, string vehicleNumber);
    Task<decimal> GetVehicleBalanceAsync(int vehicleId);
    Task<DateTime?> GetLastTransactionDateAsync(int vehicleId);
    Task<bool> IsVehicleNumberUniqueAsync(int companyId, string vehicleNumber, int? excludeVehicleId = null);
}