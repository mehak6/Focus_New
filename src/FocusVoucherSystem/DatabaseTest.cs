using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FocusVoucherSystem.Data;

namespace FocusVoucherSystem;

/// <summary>
/// Temporary test program to verify database data for Default Company
/// </summary>
public class DatabaseTest
{
    public static async Task Main(string[] args)
    {
        try
        {
            var dbConnection = new DatabaseConnection();
            
            // Initialize database if needed
            await dbConnection.InitializeDatabaseAsync();
            
            Console.WriteLine("=== Database Test Results ===\n");
            
            // 1. Check if database exists and is initialized
            var dbExists = await dbConnection.DatabaseExistsAsync();
            Console.WriteLine($"Database exists: {dbExists}\n");
            
            using var connection = await dbConnection.GetConnectionAsync();
            var sqliteConnection = (SqliteConnection)connection;
            
            // 2. Query companies table
            Console.WriteLine("--- Companies in Database ---");
            using (var cmd = new SqliteCommand("SELECT CompanyId, Name FROM Companies;", sqliteConnection))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"CompanyId: {reader.GetInt32(0)}, Name: {reader.GetString(1)}");
                }
            }
            Console.WriteLine();
            
            // 3. Count vouchers for company ID 1
            Console.WriteLine("--- Voucher Count for Company ID 1 ---");
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM Vouchers WHERE CompanyId = 1;", sqliteConnection))
            {
                var count = await cmd.ExecuteScalarAsync();
                Console.WriteLine($"Total vouchers for Company ID 1: {count}");
            }
            Console.WriteLine();
            
            // 4. Sample vouchers with vehicle information
            Console.WriteLine("--- Sample Vouchers for Company ID 1 (Recent 5) ---");
            using (var cmd = new SqliteCommand(@"
                SELECT v.VoucherNumber, v.Date, v.Amount, v.DrCr, ve.VehicleNumber, v.Narration
                FROM Vouchers v 
                LEFT JOIN Vehicles ve ON v.VehicleId = ve.VehicleId 
                WHERE v.CompanyId = 1 
                ORDER BY v.Date DESC, v.VoucherNumber DESC 
                LIMIT 5;", sqliteConnection))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    Console.WriteLine("No vouchers found for Company ID 1!");
                }
                else
                {
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"Voucher {reader.GetInt32(0)}: " +
                                        $"Date: {reader.GetString(1)}, " +
                                        $"Amount: {reader.GetDecimal(2)}, " +
                                        $"DrCr: {reader.GetString(3)}, " +
                                        $"Vehicle: {reader.GetString(4)}, " +
                                        $"Narration: {reader.GetString(5)}");
                    }
                }
            }
            Console.WriteLine();
            
            // 5. All vouchers for debugging
            Console.WriteLine("--- All Vouchers in Database ---");
            using (var cmd = new SqliteCommand("SELECT VoucherId, CompanyId, VoucherNumber, Date FROM Vouchers ORDER BY VoucherId;", sqliteConnection))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    Console.WriteLine("No vouchers found in database at all!");
                }
                else
                {
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"VoucherId: {reader.GetInt32(0)}, " +
                                        $"CompanyId: {reader.GetInt32(1)}, " +
                                        $"VoucherNumber: {reader.GetInt32(2)}, " +
                                        $"Date: {reader.GetString(3)}");
                    }
                }
            }
            Console.WriteLine();
            
            // 6. All vehicles for debugging
            Console.WriteLine("--- All Vehicles in Database ---");
            using (var cmd = new SqliteCommand("SELECT VehicleId, CompanyId, VehicleNumber, Description FROM Vehicles ORDER BY VehicleId;", sqliteConnection))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    Console.WriteLine("No vehicles found in database!");
                }
                else
                {
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"VehicleId: {reader.GetInt32(0)}, " +
                                        $"CompanyId: {reader.GetInt32(1)}, " +
                                        $"VehicleNumber: {reader.GetString(2)}, " +
                                        $"Description: {reader.GetString(3)}");
                    }
                }
            }
            Console.WriteLine();
            
            // 7. Test the GetRecentVouchersAsync query logic (similar to the one causing issues)
            Console.WriteLine("--- Testing GetRecentVouchersAsync Query Logic ---");
            using (var cmd = new SqliteCommand(@"
                SELECT 
                    v.VoucherId,
                    v.VoucherNumber,
                    v.Date,
                    v.Amount,
                    v.DrCr,
                    v.Narration,
                    ve.VehicleNumber
                FROM Vouchers v
                INNER JOIN Vehicles ve ON v.VehicleId = ve.VehicleId  
                WHERE v.CompanyId = @companyId
                ORDER BY v.Date DESC, v.VoucherNumber DESC
                LIMIT @limit", sqliteConnection))
            {
                cmd.Parameters.AddWithValue("@companyId", 1);
                cmd.Parameters.AddWithValue("@limit", 10);
                
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    Console.WriteLine("GetRecentVouchersAsync query returned no results for Company ID 1!");
                }
                else
                {
                    Console.WriteLine("GetRecentVouchersAsync query results:");
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"  VoucherId: {reader.GetInt32(0)}, " +
                                        $"VoucherNumber: {reader.GetInt32(1)}, " +
                                        $"Date: {reader.GetString(2)}, " +
                                        $"Amount: {reader.GetDecimal(3)}, " +
                                        $"DrCr: {reader.GetString(4)}, " +
                                        $"Vehicle: {reader.GetString(6)}");
                    }
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}