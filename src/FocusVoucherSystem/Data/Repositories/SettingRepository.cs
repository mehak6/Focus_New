using Dapper;
using FocusVoucherSystem.Models;
using System.Linq.Expressions;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository implementation for Setting operations using Dapper
/// </summary>
public class SettingRepository : ISettingRepository
{
    private readonly DatabaseConnection _dbConnection;

    public SettingRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public Task<Setting?> GetByIdAsync(int id)
        => throw new NotSupportedException("Settings use string keys. Use GetByKeyAsync instead.");

    public async Task<IEnumerable<Setting>> GetAllAsync()
    {
        return await GetAllSettingsAsync();
    }

    public async Task<IEnumerable<Setting>> FindAsync(Expression<Func<Setting, bool>> predicate)
    {
        var allSettings = await GetAllSettingsAsync();
        return allSettings.Where(predicate.Compile());
    }

    public async Task<Setting> AddAsync(Setting entity)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            INSERT INTO Settings (Key, Value, Description, ModifiedDate)
            VALUES (@Key, @Value, @Description, @ModifiedDate)";

        entity.ModifiedDate = DateTime.Now;
        await connection.ExecuteAsync(sql, entity);
        
        return entity;
    }

    public async Task<Setting> UpdateAsync(Setting entity)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            UPDATE Settings 
            SET Value = @Value, Description = @Description, ModifiedDate = @ModifiedDate
            WHERE Key = @Key";

        entity.ModifiedDate = DateTime.Now;
        await connection.ExecuteAsync(sql, entity);
        
        return entity;
    }

    public Task<bool> DeleteAsync(int id)
        => throw new NotSupportedException("Settings use string keys. Use DeleteAsync(string key) instead.");

    public async Task<bool> DeleteAsync(string key)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "DELETE FROM Settings WHERE Key = @Key";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { Key = key });
        return rowsAffected > 0;
    }

    public Task<bool> ExistsAsync(int id)
        => throw new NotSupportedException("Settings use string keys. Use SettingExistsAsync instead.");

    public async Task<int> CountAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM Settings";
        
        return await connection.QuerySingleAsync<int>(sql);
    }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT Key, Value, Description, ModifiedDate
            FROM Settings 
            WHERE Key = @Key";
        
        return await connection.QuerySingleOrDefaultAsync<Setting>(sql, new { Key = key });
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT Value FROM Settings WHERE Key = @Key";
        
        return await connection.QuerySingleOrDefaultAsync<string>(sql, new { Key = key });
    }

    public async Task<T?> GetValueAsync<T>(string key, T? defaultValue = default)
    {
        var stringValue = await GetValueAsync(key);
        
        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;

        try
        {
            if (typeof(T) == typeof(bool))
            {
                var boolValue = stringValue.ToLowerInvariant() switch
                {
                    "true" => true,
                    "false" => false,
                    "yes" => true,
                    "no" => false,
                    "1" => true,
                    "0" => false,
                    "on" => true,
                    "off" => false,
                    _ => bool.TryParse(stringValue, out bool result) ? result : Convert.ToBoolean(defaultValue)
                };
                return (T)(object)boolValue;
            }
            
            if (typeof(T) == typeof(int))
            {
                return int.TryParse(stringValue, out int result) ? (T)(object)result : defaultValue;
            }
            
            if (typeof(T) == typeof(decimal))
            {
                return decimal.TryParse(stringValue, out decimal result) ? (T)(object)result : defaultValue;
            }
            
            if (typeof(T) == typeof(DateTime))
            {
                return DateTime.TryParse(stringValue, out DateTime result) ? (T)(object)result : defaultValue;
            }

            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var existing = await GetByKeyAsync(key);
        
        if (existing != null)
        {
            existing.Value = value;
            if (description != null)
                existing.Description = description;
            await UpdateAsync(existing);
        }
        else
        {
            var newSetting = new Setting
            {
                Key = key,
                Value = value,
                Description = description
            };
            await AddAsync(newSetting);
        }
    }

    public async Task SetValueAsync<T>(string key, T value, string? description = null)
    {
        string stringValue;
        
        if (typeof(T) == typeof(bool))
        {
            stringValue = value?.ToString()?.ToLowerInvariant() ?? "false";
        }
        else if (typeof(T) == typeof(DateTime))
        {
            stringValue = ((DateTime)(object)value!).ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            stringValue = value?.ToString() ?? string.Empty;
        }

        await SetValueAsync(key, stringValue, description);
    }

    public async Task<bool> SettingExistsAsync(string key)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(1) FROM Settings WHERE Key = @Key";
        
        var count = await connection.QuerySingleAsync<int>(sql, new { Key = key });
        return count > 0;
    }

    public async Task<IEnumerable<Setting>> GetAllSettingsAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT Key, Value, Description, ModifiedDate
            FROM Settings 
            ORDER BY Key";
        
        return await connection.QueryAsync<Setting>(sql);
    }
}
