using Dapper;
using FocusVoucherSystem.Models;
using System.Linq.Expressions;

namespace FocusVoucherSystem.Data.Repositories;

/// <summary>
/// Repository implementation for Voucher operations using Dapper
/// </summary>
public class VoucherRepository : IVoucherRepository
{
    private readonly DatabaseConnection _dbConnection;

    public VoucherRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Voucher?> GetByIdAsync(int id)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   c.CompanyId, c.Name, c.FinancialYearStart, c.FinancialYearEnd,
                   c.LastVoucherNumber, c.IsActive, c.CreatedDate, c.ModifiedDate,
                   ve.VehicleId, ve.CompanyId, ve.VehicleNumber, ve.Description, 
                   ve.IsActive, ve.CreatedDate, ve.ModifiedDate
            FROM Vouchers v
            LEFT JOIN Companies c ON v.CompanyId = c.CompanyId
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.VoucherId = @Id";
        
        var vouchers = await connection.QueryAsync<Voucher, Company, Vehicle, Voucher>(sql,
            (voucher, company, vehicle) => 
            {
                voucher.Company = company;
                voucher.Vehicle = vehicle;
                return voucher;
            },
            new { Id = id },
            splitOn: "CompanyId,VehicleId");
            
        return vouchers.FirstOrDefault();
    }

    public async Task<IEnumerable<Voucher>> GetAllAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   c.CompanyId, c.Name, c.FinancialYearStart, c.FinancialYearEnd,
                   c.LastVoucherNumber, c.IsActive, c.CreatedDate, c.ModifiedDate,
                   ve.VehicleId, ve.CompanyId, ve.VehicleNumber, ve.Description, 
                   ve.IsActive, ve.CreatedDate, ve.ModifiedDate
            FROM Vouchers v
            LEFT JOIN Companies c ON v.CompanyId = c.CompanyId
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            ORDER BY v.Date DESC, v.VoucherNumber DESC";
        
        var vouchers = await connection.QueryAsync<Voucher, Company, Vehicle, Voucher>(sql,
            (voucher, company, vehicle) => 
            {
                voucher.Company = company;
                voucher.Vehicle = vehicle;
                return voucher;
            },
            splitOn: "CompanyId,VehicleId");
            
        return vouchers;
    }

    public async Task<IEnumerable<Voucher>> FindAsync(Expression<Func<Voucher, bool>> predicate)
    {
        var allVouchers = await GetAllAsync();
        return allVouchers.Where(predicate.Compile());
    }

    public async Task<Voucher> AddAsync(Voucher entity)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            INSERT INTO Vouchers (CompanyId, VoucherNumber, Date, VehicleId, Amount, DrCr, Narration, CreatedDate, ModifiedDate)
            VALUES (@CompanyId, @VoucherNumber, @Date, @VehicleId, @Amount, @DrCr, @Narration, @CreatedDate, @ModifiedDate);
            SELECT last_insert_rowid();";

        entity.CreatedDate = DateTime.Now;
        entity.ModifiedDate = DateTime.Now;

        var id = await connection.QuerySingleAsync<int>(sql, entity);
        entity.VoucherId = id;
        
        return entity;
    }

    public async Task<Voucher> UpdateAsync(Voucher entity)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            UPDATE Vouchers 
            SET CompanyId = @CompanyId, VoucherNumber = @VoucherNumber, Date = @Date, 
                VehicleId = @VehicleId, Amount = @Amount, DrCr = @DrCr, 
                Narration = @Narration, ModifiedDate = @ModifiedDate
            WHERE VoucherId = @VoucherId";

        entity.ModifiedDate = DateTime.Now;
        await connection.ExecuteAsync(sql, entity);
        
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "DELETE FROM Vouchers WHERE VoucherId = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(1) FROM Vouchers WHERE VoucherId = @Id";
        
        var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    public async Task<int> CountAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM Vouchers";
        
        return await connection.QuerySingleAsync<int>(sql);
    }

    public async Task<IEnumerable<Voucher>> GetByCompanyIdAsync(int companyId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT VoucherId, CompanyId, VoucherNumber, Date, VehicleId, 
                   Amount, DrCr, Narration, CreatedDate, ModifiedDate
            FROM Vouchers 
            WHERE CompanyId = @CompanyId
            ORDER BY Date DESC, VoucherNumber DESC";
        
        return await connection.QueryAsync<Voucher>(sql, new { CompanyId = companyId });
    }

    public async Task<IEnumerable<Voucher>> GetByDateRangeAsync(int companyId, DateTime startDate, DateTime endDate)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId, ve.CompanyId, ve.VehicleNumber, ve.Description, 
                   ve.IsActive, ve.CreatedDate, ve.ModifiedDate
            FROM Vouchers v
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.CompanyId = @CompanyId 
              AND v.Date >= @StartDate 
              AND v.Date <= @EndDate
            ORDER BY v.Date, v.VoucherNumber";
        
        var vouchers = await connection.QueryAsync<Voucher, Vehicle, Voucher>(sql,
            (voucher, vehicle) => 
            {
                voucher.Vehicle = vehicle;
                return voucher;
            },
            new { CompanyId = companyId, StartDate = startDate, EndDate = endDate },
            splitOn: "VehicleId");
            
        return vouchers;
    }

    public async Task<IEnumerable<Voucher>> GetByVehicleIdAsync(int vehicleId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT VoucherId, CompanyId, VoucherNumber, Date, VehicleId, 
                   Amount, DrCr, Narration, CreatedDate, ModifiedDate
            FROM Vouchers 
            WHERE VehicleId = @VehicleId
            ORDER BY Date DESC, VoucherNumber DESC";
        
        return await connection.QueryAsync<Voucher>(sql, new { VehicleId = vehicleId });
    }

    public async Task<IEnumerable<Voucher>> GetVehicleLedgerAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        
        string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId, ve.CompanyId, ve.VehicleNumber, ve.Description, 
                   ve.IsActive, ve.CreatedDate, ve.ModifiedDate
            FROM Vouchers v
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.VehicleId = @VehicleId";

        object parameters = new { VehicleId = vehicleId };

        if (startDate.HasValue && endDate.HasValue)
        {
            sql += " AND v.Date >= @StartDate AND v.Date <= @EndDate";
            parameters = new { VehicleId = vehicleId, StartDate = startDate, EndDate = endDate };
        }
        else if (startDate.HasValue)
        {
            sql += " AND v.Date >= @StartDate";
            parameters = new { VehicleId = vehicleId, StartDate = startDate };
        }
        else if (endDate.HasValue)
        {
            sql += " AND v.Date <= @EndDate";
            parameters = new { VehicleId = vehicleId, EndDate = endDate };
        }

        sql += " ORDER BY v.Date, v.VoucherNumber";

        var vouchers = await connection.QueryAsync<Voucher, Vehicle, Voucher>(sql,
            (voucher, vehicle) => 
            {
                voucher.Vehicle = vehicle;
                return voucher;
            },
            parameters,
            splitOn: "VehicleId");
            
        return vouchers;
    }

    public async Task<Voucher?> GetByVoucherNumberAsync(int companyId, int voucherNumber)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId, ve.CompanyId, ve.VehicleNumber, ve.Description, 
                   ve.IsActive, ve.CreatedDate, ve.ModifiedDate
            FROM Vouchers v
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.CompanyId = @CompanyId AND v.VoucherNumber = @VoucherNumber";
        
        var vouchers = await connection.QueryAsync<Voucher, Vehicle, Voucher>(sql,
            (voucher, vehicle) => 
            {
                voucher.Vehicle = vehicle;
                return voucher;
            },
            new { CompanyId = companyId, VoucherNumber = voucherNumber },
            splitOn: "VehicleId");
            
        return vouchers.FirstOrDefault();
    }

    public async Task<IEnumerable<Voucher>> GetDayBookAsync(int companyId, DateTime date)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId, ve.CompanyId, ve.VehicleNumber, ve.Description, 
                   ve.IsActive, ve.CreatedDate, ve.ModifiedDate
            FROM Vouchers v
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.CompanyId = @CompanyId 
              AND DATE(v.Date) = DATE(@Date)
            ORDER BY v.VoucherNumber";
        
        var vouchers = await connection.QueryAsync<Voucher, Vehicle, Voucher>(sql,
            (voucher, vehicle) => 
            {
                voucher.Vehicle = vehicle;
                return voucher;
            },
            new { CompanyId = companyId, Date = date },
            splitOn: "VehicleId");
            
        return vouchers;
    }

    public async Task<bool> IsVoucherNumberUniqueAsync(int companyId, int voucherNumber, int? excludeVoucherId = null)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        
        string sql = "SELECT COUNT(1) FROM Vouchers WHERE CompanyId = @CompanyId AND VoucherNumber = @VoucherNumber";
        object parameters = new { CompanyId = companyId, VoucherNumber = voucherNumber };

        if (excludeVoucherId.HasValue)
        {
            sql += " AND VoucherId != @ExcludeVoucherId";
            parameters = new { CompanyId = companyId, VoucherNumber = voucherNumber, ExcludeVoucherId = excludeVoucherId.Value };
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count == 0;
    }

    public async Task<decimal> GetVehicleBalanceAsync(int vehicleId, DateTime? upToDate = null)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        
        string sql = @"
            SELECT COALESCE(SUM(CASE WHEN DrCr = 'D' THEN Amount ELSE -Amount END), 0)
            FROM Vouchers 
            WHERE VehicleId = @VehicleId";

        object parameters = new { VehicleId = vehicleId };

        if (upToDate.HasValue)
        {
            sql += " AND Date <= @UpToDate";
            parameters = new { VehicleId = vehicleId, UpToDate = upToDate };
        }

        return await connection.QuerySingleAsync<decimal>(sql, parameters);
    }

    public async Task<(decimal TotalDebits, decimal TotalCredits)> GetDailySummaryAsync(int companyId, DateTime date)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT 
                COALESCE(SUM(CASE WHEN DrCr = 'D' THEN Amount ELSE 0 END), 0) as TotalDebits,
                COALESCE(SUM(CASE WHEN DrCr = 'C' THEN Amount ELSE 0 END), 0) as TotalCredits
            FROM Vouchers 
            WHERE CompanyId = @CompanyId 
              AND DATE(Date) = DATE(@Date)";
        
        var result = await connection.QuerySingleAsync<(decimal TotalDebits, decimal TotalCredits)>(sql, 
            new { CompanyId = companyId, Date = date });
            
        return result;
    }
}