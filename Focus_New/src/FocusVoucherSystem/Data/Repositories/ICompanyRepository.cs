using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository interface for Company-specific operations
/// </summary>
public interface ICompanyRepository : IRepository<Company>
{
    Task<Company?> GetByNameAsync(string name);
    Task<IEnumerable<Company>> GetActiveCompaniesAsync();
    Task<int> GetNextVoucherNumberAsync(int companyId);
    Task UpdateLastVoucherNumberAsync(int companyId, int voucherNumber);
    Task<bool> IsCompanyNameUniqueAsync(string name, int? excludeCompanyId = null);
}