using Dapper;
using FocusVoucherSystem.Models;
using System.Linq.Expressions;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository implementation for Vehicle operations using Dapper
/// </summary>
public class VehicleRepository : IVehicleRepository
{
    private readonly DatabaseConnection _dbConnection;

    public VehicleRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VehicleId, v.CompanyId, v.VehicleNumber, v.Narration, 
                   v.IsActive, v.CreatedDate, v.ModifiedDate,
                   c.CompanyId, c.Name, c.FinancialYearStart, c.FinancialYearEnd,
                   c.LastVoucherNumber, c.IsActive, c.CreatedDate, c.ModifiedDate
            FROM Vehicles v
            LEFT JOIN Companies c ON v.CompanyId = c.CompanyId
            WHERE v.VehicleId = @Id";
        
        var vehicles = await connection.QueryAsync<Vehicle, Company, Vehicle>(sql,
            (vehicle, company) => 
            {
                vehicle.Company = company;
                return vehicle;
            },
            new { Id = id },
            splitOn: "CompanyId");
            
        return vehicles.FirstOrDefault();
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VehicleId, v.CompanyId, v.VehicleNumber, v.Narration, 
                   v.IsActive, v.CreatedDate, v.ModifiedDate,
                   c.CompanyId, c.Name, c.FinancialYearStart, c.FinancialYearEnd,
                   c.LastVoucherNumber, c.IsActive, c.CreatedDate, c.ModifiedDate
            FROM Vehicles v
            LEFT JOIN Companies c ON v.CompanyId = c.CompanyId
            ORDER BY c.Name, v.VehicleNumber";
        
        var vehicles = await connection.QueryAsync<Vehicle, Company, Vehicle>(sql,
            (vehicle, company) => 
            {
                vehicle.Company = company;
                return vehicle;
            },
            splitOn: "CompanyId");
            
        return vehicles;
    }

    public async Task<IEnumerable<Vehicle>> FindAsync(Expression<Func<Vehicle, bool>> predicate)
    {
        var allVehicles = await GetAllAsync();
        return allVehicles.Where(predicate.Compile());
    }

    public async Task<Vehicle> AddAsync(Vehicle entity)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            INSERT INTO Vehicles (CompanyId, VehicleNumber, Narration, IsActive, CreatedDate, ModifiedDate)
            VALUES (@CompanyId, @VehicleNumber, @Narration, @IsActive, @CreatedDate, @ModifiedDate);
            SELECT last_insert_rowid();";

        entity.CreatedDate = DateTime.Now;
        entity.ModifiedDate = DateTime.Now;

        var id = await connection.QuerySingleAsync<int>(sql, entity);
        entity.VehicleId = id;
        
        return entity;
    }

    public async Task<Vehicle> UpdateAsync(Vehicle entity)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            UPDATE Vehicles 
            SET CompanyId = @CompanyId, VehicleNumber = @VehicleNumber, 
                Description = @Description, IsActive = @IsActive, ModifiedDate = @ModifiedDate
            WHERE VehicleId = @VehicleId";

        entity.ModifiedDate = DateTime.Now;
        await connection.ExecuteAsync(sql, entity);
        
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "DELETE FROM Vehicles WHERE VehicleId = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(1) FROM Vehicles WHERE VehicleId = @Id";
        
        var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    public async Task<int> CountAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM Vehicles";
        
        return await connection.QuerySingleAsync<int>(sql);
    }

    public async Task<IEnumerable<Vehicle>> GetByCompanyIdAsync(int companyId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT VehicleId, CompanyId, VehicleNumber, Narration, 
                   IsActive, CreatedDate, ModifiedDate
            FROM Vehicles 
            WHERE CompanyId = @CompanyId
            ORDER BY VehicleNumber";
        
        return await connection.QueryAsync<Vehicle>(sql, new { CompanyId = companyId });
    }

    public async Task<IEnumerable<Vehicle>> GetActiveByCompanyIdAsync(int companyId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT VehicleId, CompanyId, VehicleNumber, Narration, 
                   IsActive, CreatedDate, ModifiedDate
            FROM Vehicles 
            WHERE CompanyId = @CompanyId AND IsActive = 1
            ORDER BY VehicleNumber";
        
        return await connection.QueryAsync<Vehicle>(sql, new { CompanyId = companyId });
    }

    public async Task<IEnumerable<Vehicle>> SearchVehiclesAsync(int companyId, string searchTerm)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT VehicleId, CompanyId, VehicleNumber, Narration, 
                   IsActive, CreatedDate, ModifiedDate
            FROM Vehicles 
            WHERE CompanyId = @CompanyId 
              AND IsActive = 1
              AND (VehicleNumber LIKE @SearchTerm OR Narration LIKE @SearchTerm)
            ORDER BY VehicleNumber";
        
        var searchPattern = $"%{searchTerm}%";
        return await connection.QueryAsync<Vehicle>(sql, new { CompanyId = companyId, SearchTerm = searchPattern });
    }

    public async Task<Vehicle?> GetByVehicleNumberAsync(int companyId, string vehicleNumber)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT VehicleId, CompanyId, VehicleNumber, Narration, 
                   IsActive, CreatedDate, ModifiedDate
            FROM Vehicles 
            WHERE CompanyId = @CompanyId AND VehicleNumber = @VehicleNumber";
        
        return await connection.QuerySingleOrDefaultAsync<Vehicle>(sql, 
            new { CompanyId = companyId, VehicleNumber = vehicleNumber });
    }

    public async Task<decimal> GetVehicleBalanceAsync(int vehicleId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT COALESCE(SUM(CASE WHEN DrCr = 'D' THEN Amount ELSE -Amount END), 0)
            FROM Vouchers 
            WHERE VehicleId = @VehicleId";
        
        return await connection.QuerySingleAsync<decimal>(sql, new { VehicleId = vehicleId });
    }

    public async Task<DateTime?> GetLastTransactionDateAsync(int vehicleId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT MAX(Date)
            FROM Vouchers 
            WHERE VehicleId = @VehicleId";
        
        return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { VehicleId = vehicleId });
    }

    public async Task<bool> IsVehicleNumberUniqueAsync(int companyId, string vehicleNumber, int? excludeVehicleId = null)
    {
        var connection = await _dbConnection.GetConnectionAsync();

        string sql = "SELECT COUNT(1) FROM Vehicles WHERE CompanyId = @CompanyId AND VehicleNumber = @VehicleNumber";
        object parameters = new { CompanyId = companyId, VehicleNumber = vehicleNumber };

        if (excludeVehicleId.HasValue)
        {
            sql += " AND VehicleId != @ExcludeVehicleId";
            parameters = new { CompanyId = companyId, VehicleNumber = vehicleNumber, ExcludeVehicleId = excludeVehicleId.Value };
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count == 0;
    }

    public async Task<bool> MergeVehiclesAsync(int sourceVehicleId, int targetVehicleId)
    {
        var connection = await _dbConnection.GetConnectionAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            const string updateVouchersQuery = @"
                UPDATE Vouchers
                SET VehicleId = @TargetVehicleId, ModifiedDate = @ModifiedDate
                WHERE VehicleId = @SourceVehicleId";

            await connection.ExecuteAsync(updateVouchersQuery,
                new {
                    SourceVehicleId = sourceVehicleId,
                    TargetVehicleId = targetVehicleId,
                    ModifiedDate = DateTime.Now
                }, transaction);

            const string deleteVehicleQuery = "DELETE FROM Vehicles WHERE VehicleId = @SourceVehicleId";

            await connection.ExecuteAsync(deleteVehicleQuery,
                new { SourceVehicleId = sourceVehicleId }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }
}