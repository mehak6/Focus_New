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
        using var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   c.CompanyId AS CompId, c.Name, c.FinancialYearStart, c.FinancialYearEnd,
                   c.LastVoucherNumber, c.IsActive AS CompIsActive, c.CreatedDate AS CompCreatedDate, c.ModifiedDate AS CompModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
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
            splitOn: "CompId,VehId");
            
        return vouchers.FirstOrDefault();
    }

    public async Task<IEnumerable<Voucher>> GetAllAsync()
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   c.CompanyId AS CompId, c.Name, c.FinancialYearStart, c.FinancialYearEnd,
                   c.LastVoucherNumber, c.IsActive AS CompIsActive, c.CreatedDate AS CompCreatedDate, c.ModifiedDate AS CompModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
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
            splitOn: "CompId,VehId");
            
        return vouchers;
    }

    public async Task<IEnumerable<Voucher>> FindAsync(Expression<Func<Voucher, bool>> predicate)
    {
        var allVouchers = await GetAllAsync();
        return allVouchers.Where(predicate.Compile());
    }

    public async Task<Voucher> AddAsync(Voucher entity)
    {
        using var connection = await _dbConnection.GetConnectionAsync();
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
        using var connection = await _dbConnection.GetConnectionAsync();
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
        
        try
        {
            // First, get vouchers for the company (limit to recent ones for performance)
            const string voucherSql = @"
                SELECT VoucherId, CompanyId, VoucherNumber, Date, VehicleId, 
                       Amount, DrCr, Narration, CreatedDate, ModifiedDate
                FROM Vouchers 
                WHERE CompanyId = @CompanyId
                ORDER BY Date DESC, VoucherNumber DESC
                LIMIT 50";
            
            // Use dynamic to avoid type mapping issues, then manually create Voucher objects
            var dynamicVouchers = await connection.QueryAsync(voucherSql, new { CompanyId = companyId });
            
            var voucherList = new List<Voucher>();
            foreach (var row in dynamicVouchers)
            {
                try
                {
                    var voucher = new Voucher
                    {
                        VoucherId = Convert.ToInt32(row.VoucherId),          // Convert long to int
                        CompanyId = Convert.ToInt32(row.CompanyId),          // Convert long to int  
                        VoucherNumber = Convert.ToInt32(row.VoucherNumber),  // Convert long to int
                        Date = DateTime.Parse(row.Date.ToString()),
                        VehicleId = Convert.ToInt32(row.VehicleId),          // Convert long to int
                        Amount = Convert.ToDecimal(row.Amount),              // Convert double to decimal
                        DrCr = row.DrCr.ToString(),
                        Narration = row.Narration?.ToString(),
                        CreatedDate = DateTime.Parse(row.CreatedDate.ToString()),
                        ModifiedDate = DateTime.Parse(row.ModifiedDate.ToString())
                    };
                    voucherList.Add(voucher);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error mapping voucher row: {ex.Message}");
                }
            }
            
            if (voucherList.Any())
            {
                // Get all vehicles for these vouchers
                var vehicleIds = voucherList.Select(v => v.VehicleId).Distinct().ToList();
                if (vehicleIds.Any())
                {
                    var vehicleSql = $@"
                        SELECT VehicleId, CompanyId, VehicleNumber, Narration,
                               IsActive, CreatedDate, ModifiedDate
                        FROM Vehicles
                        WHERE VehicleId IN ({string.Join(",", vehicleIds)})";
                    
                    var vehicles = await connection.QueryAsync<Vehicle>(vehicleSql);
                    var vehicleDict = vehicles.ToDictionary(v => v.VehicleId);
                    
                    // Assign vehicles to vouchers
                    foreach (var voucher in voucherList)
                    {
                        if (vehicleDict.TryGetValue(voucher.VehicleId, out var vehicle))
                        {
                            voucher.Vehicle = vehicle;
                        }
                    }
                }
            }
            
            return voucherList;
        }
        catch (Exception ex)
        {
            // Log error but rethrow to let calling code handle it
            System.Diagnostics.Debug.WriteLine($"Error loading vouchers: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<Voucher>> GetRecentVouchersAsync(int companyId, int limit = 100)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
            FROM Vouchers v
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.CompanyId = @CompanyId
            ORDER BY v.Date DESC, v.VoucherNumber DESC
            LIMIT @Limit";
        
        var vouchers = await connection.QueryAsync<Voucher, Vehicle, Voucher>(sql,
            (voucher, vehicle) => 
            {
                voucher.Vehicle = vehicle;
                return voucher;
            },
            new { CompanyId = companyId, Limit = limit },
            splitOn: "VehId");
            
        return vouchers;
    }

    public async Task<IEnumerable<Voucher>> SearchVouchersAsync(int companyId, string searchTerm, int limit = 100)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
            FROM Vouchers v
            LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
            WHERE v.CompanyId = @CompanyId 
            AND (ve.VehicleNumber LIKE @SearchTerm COLLATE NOCASE 
                 OR v.Narration LIKE @SearchTerm COLLATE NOCASE)
            ORDER BY v.Date DESC, v.VoucherNumber DESC
            LIMIT @Limit";
        
        var searchPattern = $"%{searchTerm}%";
        var vouchers = await connection.QueryAsync<Voucher, Vehicle, Voucher>(sql,
            (voucher, vehicle) => 
            {
                voucher.Vehicle = vehicle;
                return voucher;
            },
            new { CompanyId = companyId, SearchTerm = searchPattern, Limit = limit },
            splitOn: "VehId");
            
        return vouchers;
    }

    public async Task<IEnumerable<Voucher>> GetByDateRangeAsync(int companyId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();

            // Use simplified approach with manual row parsing
            const string sql = @"
                SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId,
                       v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                       COALESCE(ve.VehicleNumber, '') as VehicleNumber,
                       COALESCE(ve.Narration, '') as VehicleDescription
                FROM Vouchers v
                LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
                WHERE v.CompanyId = @CompanyId
                  AND v.Date >= @StartDate
                  AND v.Date <= @EndDate
                ORDER BY v.Date, v.VoucherNumber";

            var parameters = new {
                CompanyId = companyId,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            };

            var results = await connection.QueryAsync(sql, parameters);
            var voucherList = new List<Voucher>();

            foreach (var row in results)
            {
                try
                {
                    var voucher = new Voucher
                    {
                        VoucherId = Convert.ToInt32(row.VoucherId ?? 0),
                        CompanyId = Convert.ToInt32(row.CompanyId ?? 0),
                        VoucherNumber = Convert.ToInt32(row.VoucherNumber ?? 0),
                        VehicleId = Convert.ToInt32(row.VehicleId ?? 0),
                        Amount = Convert.ToDecimal(row.Amount ?? 0),
                        DrCr = (row.DrCr ?? "D").ToString(),
                        Narration = (row.Narration ?? "").ToString(),
                    };

                    // Parse dates manually
                    if (DateTime.TryParse((row.Date ?? "").ToString(), out DateTime date))
                        voucher.Date = date;
                    else
                        voucher.Date = DateTime.Today;

                    if (DateTime.TryParse((row.CreatedDate ?? "").ToString(), out DateTime created))
                        voucher.CreatedDate = created;
                    else
                        voucher.CreatedDate = DateTime.Now;

                    if (DateTime.TryParse((row.ModifiedDate ?? "").ToString(), out DateTime modified))
                        voucher.ModifiedDate = modified;
                    else
                        voucher.ModifiedDate = DateTime.Now;

                    // Create vehicle object
                    voucher.Vehicle = new Vehicle
                    {
                        VehicleId = voucher.VehicleId,
                        VehicleNumber = (row.VehicleNumber ?? "").ToString(),
                        Narration = (row.VehicleDescription ?? "").ToString()
                    };

                    voucherList.Add(voucher);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing voucher row: {ex.Message}");
                    continue;
                }
            }

            return voucherList;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetByDateRangeAsync: {ex.Message}");
            return Enumerable.Empty<Voucher>();
        }
    }

    /// <summary>
    /// Gets paginated vouchers for a date range with optimized performance - Simplified approach
    /// </summary>
    public async Task<(IEnumerable<Voucher> Vouchers, int TotalCount, bool HasMore)> GetByDateRangePagedAsync(
        int companyId, DateTime startDate, DateTime endDate, int pageSize = 1000, int offset = 0)
    {
        try
        {
            var connection = await _dbConnection.GetConnectionAsync();

            // Get total count first
            const string countSql = @"
                SELECT COUNT(*)
                FROM Vouchers
                WHERE CompanyId = @CompanyId
                  AND Date >= @StartDate
                  AND Date <= @EndDate";

            var totalCount = await connection.QuerySingleAsync<int>(countSql,
                new { CompanyId = companyId, StartDate = startDate, EndDate = endDate });

            if (totalCount == 0)
                return (Enumerable.Empty<Voucher>(), 0, false);

            // Use raw SQL with manual mapping to avoid type conversion issues
            const string sql = @"
                SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId,
                       v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                       COALESCE(ve.VehicleNumber, '') as VehicleNumber,
                       COALESCE(ve.Narration, '') as VehicleDescription
                FROM Vouchers v
                LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
                WHERE v.CompanyId = @CompanyId
                  AND v.Date >= @StartDate
                  AND v.Date <= @EndDate
                ORDER BY v.Date, v.VoucherNumber
                LIMIT @PageSize OFFSET @Offset";

            var parameters = new {
                CompanyId = companyId,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                PageSize = pageSize,
                Offset = offset
            };

            var results = await connection.QueryAsync(sql, parameters);
            var voucherList = new List<Voucher>();

            foreach (var row in results)
            {
                try
                {
                    var voucher = new Voucher
                    {
                        VoucherId = Convert.ToInt32(row.VoucherId ?? 0),
                        CompanyId = Convert.ToInt32(row.CompanyId ?? 0),
                        VoucherNumber = Convert.ToInt32(row.VoucherNumber ?? 0),
                        VehicleId = Convert.ToInt32(row.VehicleId ?? 0),
                        Amount = Convert.ToDecimal(row.Amount ?? 0),
                        DrCr = (row.DrCr ?? "D").ToString(),
                        Narration = (row.Narration ?? "").ToString(),
                    };

                    // Parse dates manually
                    if (DateTime.TryParse((row.Date ?? "").ToString(), out DateTime date))
                        voucher.Date = date;
                    else
                        voucher.Date = DateTime.Today;

                    if (DateTime.TryParse((row.CreatedDate ?? "").ToString(), out DateTime created))
                        voucher.CreatedDate = created;
                    else
                        voucher.CreatedDate = DateTime.Now;

                    if (DateTime.TryParse((row.ModifiedDate ?? "").ToString(), out DateTime modified))
                        voucher.ModifiedDate = modified;
                    else
                        voucher.ModifiedDate = DateTime.Now;

                    // Create vehicle object
                    voucher.Vehicle = new Vehicle
                    {
                        VehicleId = voucher.VehicleId,
                        VehicleNumber = (row.VehicleNumber ?? "").ToString(),
                        Narration = (row.VehicleDescription ?? "").ToString()
                    };

                    voucherList.Add(voucher);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing voucher row: {ex.Message}");
                    // Skip this row and continue
                    continue;
                }
            }

            var hasMore = (offset + pageSize) < totalCount;
            return (voucherList, totalCount, hasMore);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetByDateRangePagedAsync: {ex.Message}");
            return (Enumerable.Empty<Voucher>(), 0, false);
        }
    }

    public async Task<IEnumerable<Voucher>> GetByVehicleIdAsync(int vehicleId)
    {
        try
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            
            // Optimized query with pre-calculated running balance
            const string sql = @"
                WITH VoucherData AS (
                    SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                           v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                           ve.VehicleNumber, ve.Narration,
                           SUM(CASE WHEN v2.DrCr = 'D' THEN v2.Amount ELSE -v2.Amount END) AS RunningBalance
                    FROM Vouchers v
                    LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
                    LEFT JOIN Vouchers v2 ON v2.VehicleId = v.VehicleId 
                        AND (v2.Date < v.Date OR (v2.Date = v.Date AND v2.VoucherId <= v.VoucherId))
                    WHERE v.VehicleId = @VehicleId
                    GROUP BY v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                             v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                             ve.VehicleNumber, ve.Narration
                )
                SELECT VoucherId, CompanyId, VoucherNumber, Date, VehicleId, 
                       Amount, DrCr, Narration, CreatedDate, ModifiedDate,
                       VehicleNumber, Narration, RunningBalance
                FROM VoucherData
                ORDER BY Date DESC, VoucherId DESC
                LIMIT 1000";
            
            var vouchers = await connection.QueryAsync<dynamic>(sql, new { VehicleId = vehicleId });
            
            return vouchers.Select(v => new Voucher
            {
                VoucherId = SafeToInt32(v.VoucherId),
                CompanyId = SafeToInt32(v.CompanyId),
                VoucherNumber = SafeToInt32(v.VoucherNumber),
                Date = SafeToDateTime(v.Date),
                VehicleId = SafeToInt32(v.VehicleId),
                Amount = SafeToDecimal(v.Amount),
                DrCr = v.DrCr?.ToString() ?? string.Empty,
                Narration = v.Narration?.ToString() ?? string.Empty,
                CreatedDate = SafeToDateTime(v.CreatedDate),
                ModifiedDate = SafeToDateTime(v.ModifiedDate),
                RunningBalance = SafeToDecimal(v.RunningBalance ?? 0),
                Vehicle = new Vehicle
                {
                    VehicleId = SafeToInt32(v.VehicleId),
                    VehicleNumber = v.VehicleNumber?.ToString() ?? string.Empty,
                    Narration = v.Narration?.ToString() ?? string.Empty
                }
            }).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetByVehicleIdAsync for vehicleId {vehicleId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets paginated vouchers for a vehicle with pre-calculated running balances
    /// </summary>
    public async Task<(IEnumerable<Voucher> Vouchers, int TotalCount)> GetByVehicleIdPagedAsync(int vehicleId, int pageSize = 500, int offset = 0)
    {
        try
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            
            // First get total count
            const string countSql = "SELECT COUNT(*) FROM Vouchers WHERE VehicleId = @VehicleId";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, new { VehicleId = vehicleId });
            
            // Then get paginated data with running balance
            const string sql = @"
                WITH VoucherData AS (
                    SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                           v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                           ve.VehicleNumber, ve.Narration,
                           SUM(CASE WHEN v2.DrCr = 'D' THEN v2.Amount ELSE -v2.Amount END) AS RunningBalance,
                           ROW_NUMBER() OVER (ORDER BY v.Date DESC, v.VoucherId DESC) AS RowNum
                    FROM Vouchers v
                    LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId
                    LEFT JOIN Vouchers v2 ON v2.VehicleId = v.VehicleId 
                        AND (v2.Date < v.Date OR (v2.Date = v.Date AND v2.VoucherId <= v.VoucherId))
                    WHERE v.VehicleId = @VehicleId
                    GROUP BY v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                             v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                             ve.VehicleNumber, ve.Narration
                )
                SELECT VoucherId, CompanyId, VoucherNumber, Date, VehicleId, 
                       Amount, DrCr, Narration, CreatedDate, ModifiedDate,
                       VehicleNumber, Narration, RunningBalance
                FROM VoucherData
                WHERE RowNum > @Offset AND RowNum <= @Offset + @PageSize
                ORDER BY Date DESC, VoucherId DESC";
            
            var vouchers = await connection.QueryAsync<dynamic>(sql, new { 
                VehicleId = vehicleId, 
                Offset = offset, 
                PageSize = pageSize 
            });
            
            var voucherList = vouchers.Select(v => new Voucher
            {
                VoucherId = SafeToInt32(v.VoucherId),
                CompanyId = SafeToInt32(v.CompanyId),
                VoucherNumber = SafeToInt32(v.VoucherNumber),
                Date = SafeToDateTime(v.Date),
                VehicleId = SafeToInt32(v.VehicleId),
                Amount = SafeToDecimal(v.Amount),
                DrCr = v.DrCr?.ToString() ?? string.Empty,
                Narration = v.Narration?.ToString() ?? string.Empty,
                CreatedDate = SafeToDateTime(v.CreatedDate),
                ModifiedDate = SafeToDateTime(v.ModifiedDate),
                RunningBalance = SafeToDecimal(v.RunningBalance ?? 0),
                Vehicle = new Vehicle
                {
                    VehicleId = SafeToInt32(v.VehicleId),
                    VehicleNumber = v.VehicleNumber?.ToString() ?? string.Empty,
                    Narration = v.Narration?.ToString() ?? string.Empty
                }
            }).ToList();
            
            return (voucherList, totalCount);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetByVehicleIdPagedAsync for vehicleId {vehicleId}: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<Voucher>> GetVehicleLedgerAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        
        string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
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
            splitOn: "VehId");
            
        return vouchers;
    }

    public async Task<Voucher?> GetByVoucherNumberAsync(int companyId, int voucherNumber)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
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
            splitOn: "VehId");
            
        return vouchers.FirstOrDefault();
    }

    public async Task<IEnumerable<Voucher>> GetDayBookAsync(int companyId, DateTime date)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = @"
            SELECT v.VoucherId, v.CompanyId, v.VoucherNumber, v.Date, v.VehicleId, 
                   v.Amount, v.DrCr, v.Narration, v.CreatedDate, v.ModifiedDate,
                   ve.VehicleId AS VehId, ve.CompanyId AS VehCompanyId, ve.VehicleNumber, ve.Narration, 
                   ve.IsActive AS VehIsActive, ve.CreatedDate AS VehCreatedDate, ve.ModifiedDate AS VehModifiedDate
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
            splitOn: "VehId");
            
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

    public async Task<int> CountByCompanyAsync(int companyId)
    {
        var connection = await _dbConnection.GetConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM Vouchers WHERE CompanyId = @CompanyId";
        
        return await connection.QuerySingleAsync<int>(sql, new { CompanyId = companyId });
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

    /// <summary>
    /// Safe type conversion helpers for SQLite dynamic results
    /// </summary>
    private static int SafeToInt32(object value)
    {
        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            string strValue when int.TryParse(strValue, out var parsed) => parsed,
            _ => 0
        };
    }

    private static decimal SafeToDecimal(object value)
    {
        return value switch
        {
            decimal decValue => decValue,
            double dblValue => (decimal)dblValue,
            float floatValue => (decimal)floatValue,
            long longValue => longValue,
            int intValue => intValue,
            string strValue when decimal.TryParse(strValue, out var parsed) => parsed,
            _ => 0m
        };
    }

    private static DateTime SafeToDateTime(object value)
    {
        return value switch
        {
            DateTime dateTime => dateTime,
            string strValue when DateTime.TryParse(strValue, out var parsed) => parsed,
            _ => DateTime.MinValue
        };
    }
}