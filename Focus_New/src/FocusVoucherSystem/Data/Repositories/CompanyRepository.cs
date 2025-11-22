using Dapper;
using FocusVoucherSystem.Models;
using System.Linq.Expressions;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository implementation for Company operations using Dapper
/// </summary>
public class CompanyRepository : ICompanyRepository
{
    private readonly DatabaseConnection _dbConnection;

    public CompanyRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Company?> GetByIdAsync(int id)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT CompanyId, Name, FinancialYearStart, FinancialYearEnd, 
                   LastVoucherNumber, IsActive, CreatedDate, ModifiedDate
            FROM Companies 
            WHERE CompanyId = @Id";
        
        return await connection.QuerySingleOrDefaultAsync<Company>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Company>> GetAllAsync()
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT CompanyId, Name, FinancialYearStart, FinancialYearEnd, 
                   LastVoucherNumber, IsActive, CreatedDate, ModifiedDate
            FROM Companies 
            ORDER BY Name";
        
        return await connection.QueryAsync<Company>(sql);
    }

    public async Task<IEnumerable<Company>> FindAsync(Expression<Func<Company, bool>> predicate)
    {
        // For simplicity, we'll implement common scenarios
        // In a full implementation, you might use a library like DynamicLinq
        var allCompanies = await GetAllAsync();
        return allCompanies.Where(predicate.Compile());
    }

    public async Task<Company> AddAsync(Company entity)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            INSERT INTO Companies (Name, FinancialYearStart, FinancialYearEnd, 
                                 LastVoucherNumber, IsActive, CreatedDate, ModifiedDate)
            VALUES (@Name, @FinancialYearStart, @FinancialYearEnd, 
                   @LastVoucherNumber, @IsActive, @CreatedDate, @ModifiedDate);
            SELECT last_insert_rowid();";

        entity.CreatedDate = DateTime.Now;
        entity.ModifiedDate = DateTime.Now;

        var id = await connection.QuerySingleAsync<int>(sql, entity);
        entity.CompanyId = id;
        
        return entity;
    }

    public async Task<Company> UpdateAsync(Company entity)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            UPDATE Companies 
            SET Name = @Name, FinancialYearStart = @FinancialYearStart, 
                FinancialYearEnd = @FinancialYearEnd, LastVoucherNumber = @LastVoucherNumber,
                IsActive = @IsActive, ModifiedDate = @ModifiedDate
            WHERE CompanyId = @CompanyId";

        entity.ModifiedDate = DateTime.Now;
        await connection.ExecuteAsync(sql, entity);
        
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "DELETE FROM Companies WHERE CompanyId = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(1) FROM Companies WHERE CompanyId = @Id";
        
        var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    public async Task<int> CountAsync()
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM Companies";
        
        return await connection.QuerySingleAsync<int>(sql);
    }

    public async Task<Company?> GetByNameAsync(string name)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT CompanyId, Name, FinancialYearStart, FinancialYearEnd, 
                   LastVoucherNumber, IsActive, CreatedDate, ModifiedDate
            FROM Companies 
            WHERE Name = @Name";
        
        return await connection.QuerySingleOrDefaultAsync<Company>(sql, new { Name = name });
    }

    public async Task<IEnumerable<Company>> GetActiveCompaniesAsync()
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT CompanyId, Name, FinancialYearStart, FinancialYearEnd, 
                   LastVoucherNumber, IsActive, CreatedDate, ModifiedDate
            FROM Companies 
            WHERE IsActive = 1
            ORDER BY Name";
        
        return await connection.QueryAsync<Company>(sql);
    }

    public async Task<int> GetNextVoucherNumberAsync(int companyId)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT LastVoucherNumber + 1 FROM Companies WHERE CompanyId = @CompanyId";
        
        return await connection.QuerySingleAsync<int>(sql, new { CompanyId = companyId });
    }

    public async Task UpdateLastVoucherNumberAsync(int companyId, int voucherNumber)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            UPDATE Companies 
            SET LastVoucherNumber = CASE 
                WHEN @VoucherNumber > LastVoucherNumber THEN @VoucherNumber 
                ELSE LastVoucherNumber 
            END, ModifiedDate = @ModifiedDate
            WHERE CompanyId = @CompanyId";

        await connection.ExecuteAsync(sql, new 
        { 
            CompanyId = companyId, 
            VoucherNumber = voucherNumber,
            ModifiedDate = DateTime.Now 
        });
    }

    public async Task<bool> IsCompanyNameUniqueAsync(string name, int? excludeCompanyId = null)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
        
        string sql = "SELECT COUNT(1) FROM Companies WHERE Name = @Name";
        object parameters = new { Name = name };

        if (excludeCompanyId.HasValue)
        {
            sql += " AND CompanyId != @ExcludeCompanyId";
            parameters = new { Name = name, ExcludeCompanyId = excludeCompanyId.Value };
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count == 0;
    }
}