using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FocusVoucherSystem.Models;

namespace FocusVoucherSystem.Services;

public class ImportOptions
{
    public required string FolderPath { get; init; }
    public required int CompanyId { get; init; }
    public bool DryRun { get; init; } = true;
    public bool CreateMissingVehicles { get; init; } = true;
}

public class ImportResult
{
    public int VehiclesParsed { get; set; }
    public int VehiclesInserted { get; set; }
    public int VouchersParsed { get; set; }
    public int VouchersInserted { get; set; }
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public override string ToString()
    {
        return $"Vehicles: parsed {VehiclesParsed}, inserted {VehiclesInserted}\n" +
               $"Vouchers: parsed {VouchersParsed}, inserted {VouchersInserted}\n" +
               (Warnings.Count > 0 ? $"Warnings ({Warnings.Count}):\n - " + string.Join("\n - ", Warnings) + "\n" : string.Empty) +
               (Errors.Count > 0 ? $"Errors ({Errors.Count}):\n - " + string.Join("\n - ", Errors) : string.Empty);
    }
}

public class ImportService
{
    private readonly DataService _dataService;

    public ImportService(DataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<ImportResult> ImportAsync(ImportOptions options)
    {
        var result = new ImportResult();
        var vehPath = Path.Combine(options.FolderPath, "VEH.TXT");
        var vchPath = Path.Combine(options.FolderPath, "VCH.TXT");

        if (!File.Exists(vehPath)) result.Warnings.Add($"VEH.TXT not found at {vehPath}");
        if (!File.Exists(vchPath)) result.Warnings.Add($"VCH.TXT not found at {vchPath}");

        var vehicles = new List<string>();
        // Track highest voucher number seen/inserted to advance company's sequence
        var company = await _dataService.Companies.GetByIdAsync(options.CompanyId);
        var currentLast = company?.LastVoucherNumber ?? 0;
        var maxVoucherNo = currentLast;
        if (File.Exists(vehPath))
        {
            vehicles = await ParseVehiclesAsync(vehPath);
            result.VehiclesParsed = vehicles.Count;

            if (!options.DryRun)
            {
                foreach (var vn in vehicles)
                {
                    // Check existing by company + vehicle number
                    var existing = await _dataService.Vehicles.GetByVehicleNumberAsync(options.CompanyId, vn);
                    if (existing == null && options.CreateMissingVehicles)
                    {
                        await _dataService.Vehicles.AddAsync(new Vehicle
                        {
                            CompanyId = options.CompanyId,
                            VehicleNumber = vn,
                            Narration = string.Empty,
                            IsActive = true
                        });
                        result.VehiclesInserted++;
                    }
                }
            }
        }

        var voucherLines = new List<(int VNo, DateTime Date, string Vehicle, decimal Amount, string DrCr)>();
        if (File.Exists(vchPath))
        {
            voucherLines = await ParseVouchersAsync(vchPath);
            result.VouchersParsed = voucherLines.Count;

            if (!options.DryRun)
            {
                foreach (var v in voucherLines)
                {
                    // Resolve vehicle
                    var veh = await _dataService.Vehicles.GetByVehicleNumberAsync(options.CompanyId, v.Vehicle);
                    if (veh == null)
                    {
                        if (options.CreateMissingVehicles)
                        {
                            veh = await _dataService.Vehicles.AddAsync(new Vehicle
                            {
                                CompanyId = options.CompanyId,
                                VehicleNumber = v.Vehicle,
                                Narration = string.Empty,
                                IsActive = true
                            });
                            result.VehiclesInserted++;
                        }
                        else
                        {
                            result.Warnings.Add($"Missing vehicle '{v.Vehicle}' for voucher {v.VNo} on {v.Date:yyyy-MM-dd}");
                            continue;
                        }
                    }

                    // Insert voucher if unique
                    var exists = await _dataService.Vouchers.GetByVoucherNumberAsync(options.CompanyId, v.VNo);
                    if (exists != null)
                    {
                        result.Warnings.Add($"Duplicate voucher number {v.VNo} (skipped)");
                        continue;
                    }

                    await _dataService.Vouchers.AddAsync(new Voucher
                    {
                        CompanyId = options.CompanyId,
                        VoucherNumber = v.VNo,
                        Date = v.Date,
                        VehicleId = veh!.VehicleId,
                        Amount = v.Amount,
                        DrCr = v.DrCr,
                        Narration = string.Empty
                    });
                    result.VouchersInserted++;
                    if (v.VNo > maxVoucherNo) maxVoucherNo = v.VNo;
                }
            }
        }

        // Advance company's last voucher number to the highest voucher number seen
        if (!options.DryRun && maxVoucherNo > currentLast)
        {
            await _dataService.Companies.UpdateLastVoucherNumberAsync(options.CompanyId, maxVoucherNo);
        }

        return result;
    }

    public async Task<List<string>> ParseVehiclesAsync(string vehFilePath)
    {
        var list = new List<string>();
        foreach (var raw in await File.ReadAllLinesAsync(vehFilePath))
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("====") || line.Contains("PAGE NO") || line.Contains("VEHICLE NUMBER LIST")) continue;
            if (line.StartsWith("S.No")) continue;

            // Typical data line: "1    GJ05AB1234" or similar
            var m = Regex.Match(line, "^\\s*([0-9]+)\\s+(.+?)\\s*$");
            if (m.Success)
            {
                var vehNo = m.Groups[2].Value.Trim();
                if (vehNo.Length >= 3) list.Add(vehNo);
            }
        }
        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<List<(int VNo, DateTime Date, string Vehicle, decimal Amount, string DrCr)>> ParseVouchersAsync(string vchFilePath)
    {
        var list = new List<(int, DateTime, string, decimal, string)>();
        var dateFormats = new[] { "dd-MM-yyyy", "dd/MM/yyyy", "d-M-yyyy", "d/M/yyyy" };

        foreach (var raw in await File.ReadAllLinesAsync(vchFilePath))
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("====") || line.Contains("PAGE NO") || line.Contains("CASH VOUCHER LIST")) continue;
            if (line.StartsWith("V. No.")) continue;

            // Heuristic columns: VNo, Date, A/C Name (Vehicle), Amount, D/C
            // Regex with flexible spacing and amount with commas
            var m = Regex.Match(line, "^\\s*(?<vno>\\d+)\\s+(?<date>\\d{1,2}[-/]\\d{1,2}[-/]\\d{2,4})\\s+(?<name>.+?)\\s+(?<amount>[0-9,]+(?:\\.\\d{1,2})?)\\s+(?<dc>[DC])\\s*$");
            if (!m.Success) continue;

            if (!int.TryParse(m.Groups["vno"].Value, out var vno)) continue;
            var dateStr = m.Groups["date"].Value;
            if (!DateTime.TryParseExact(dateStr, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date)) continue;
            var veh = m.Groups["name"].Value.Trim();
            var amountStr = m.Groups["amount"].Value.Replace(",", "");
            if (!decimal.TryParse(amountStr, out var amount)) continue;
            var dc = m.Groups["dc"].Value.ToUpperInvariant();
            if (dc != "D" && dc != "C") dc = "D";

            list.Add((vno, date, veh, amount, dc));
        }

        return list;
    }
}
