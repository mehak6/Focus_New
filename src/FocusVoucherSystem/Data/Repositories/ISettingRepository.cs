using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository interface for Setting-specific operations
/// </summary>
public interface ISettingRepository : IRepository<Setting>
{
    Task<Setting?> GetByKeyAsync(string key);
    Task<string?> GetValueAsync(string key);
    Task<T?> GetValueAsync<T>(string key, T defaultValue = default);
    Task SetValueAsync(string key, string value, string? description = null);
    Task SetValueAsync<T>(string key, T value, string? description = null);
    Task<bool> SettingExistsAsync(string key);
    Task<IEnumerable<Setting>> GetAllSettingsAsync();
}