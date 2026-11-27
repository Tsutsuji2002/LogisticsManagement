using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Development-only idempotent data seeder.
/// Creates a full set of reference data (countries/provinces, currencies, UoMs,
/// number sequences, company, branches, warehouses/zones/locations, departments,
/// employees, user accounts across all roles, carriers, shipping rate cards,
/// notification templates) and representative sample data (customers, catalog)
/// so the system can be tested end-to-end immediately after a fresh migration.
/// </summary>
public class DevDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasherService _hasher;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(
        ApplicationDbContext db,
        IPasswordHasherService hasher,
        ILogger<DevDataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Run steps in dependency order; each step is idempotent.
        await SeedGeographyAsync(ct);
        await SeedCurrenciesAsync(ct);
        await SeedUnitsOfMeasureAsync(ct);
        await SeedNumberSequencesAsync(ct);
        await SeedCompanyBranchesAndWarehousesAsync(ct);
        await SeedWarehouseZonesAndLocationsAsync(ct);
        await SeedDepartmentsAsync(ct);
        await SeedEmployeesAndUsersAsync(ct);
        await EnsureBootstrapAdminBranchAccessAsync(ct);
        await SeedCustomersAsync(ct);
        await SeedProductCatalogAsync(ct);
        await SeedCarriersAsync(ct);
        await SeedShippingRateCardAsync(ct);
        await SeedNotificationTemplatesAsync(ct);
        _logger.LogInformation("DevDataSeeder: completed successfully.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Geography (Country + Provinces)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedGeographyAsync(CancellationToken ct)
    {
        if (await _db.Countries.AnyAsync(ct)) return;

        var vn = new Country
        {
            Code = "VN",
            Alpha3Code = "VNM",
            Name = "Vietnam",
            NativeName = "Việt Nam",
            NumericCode = 704,
            PhonePrefix = "+84",
            IsActive = true
        };
        vn.Provinces.Add(new Province { Code = "79", Name = "TP. Hồ Chí Minh", Type = "Thành phố", IsActive = true });
        vn.Provinces.Add(new Province { Code = "01", Name = "Hà Nội", Type = "Thành phố", IsActive = true });
        vn.Provinces.Add(new Province { Code = "48", Name = "Đà Nẵng", Type = "Thành phố", IsActive = true });

        _db.Countries.Add(vn);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded country and provinces.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Currencies
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCurrenciesAsync(CancellationToken ct)
    {
        if (await _db.Currencies.AnyAsync(ct)) return;

        _db.Currencies.AddRange(
            new Currency { Code = "VND", Name = "Việt Nam Đồng", Symbol = "₫", DecimalPlaces = 0, ExchangeRateToDefault = 1m, IsDefault = true, IsActive = true },
            new Currency { Code = "USD", Name = "US Dollar", Symbol = "$", DecimalPlaces = 2, ExchangeRateToDefault = 25000m, IsDefault = false, IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded currencies.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Units of Measure
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedUnitsOfMeasureAsync(CancellationToken ct)
    {
        if (await _db.UnitsOfMeasure.AnyAsync(ct)) return;

        _db.UnitsOfMeasure.AddRange(
            new UnitOfMeasure { Code = "PCS", Name = "Piece", Type = "Count", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true },
            new UnitOfMeasure { Code = "KG", Name = "Kilogram", Type = "Weight", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true },
            new UnitOfMeasure { Code = "G", Name = "Gram", Type = "Weight", ConversionFactor = 0.001m, IsBaseUnit = false, IsActive = true },
            new UnitOfMeasure { Code = "CBM", Name = "Cubic Metre", Type = "Volume", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true },
            new UnitOfMeasure { Code = "CM", Name = "Centimetre", Type = "Dimension", ConversionFactor = 0.01m, IsBaseUnit = false, IsActive = true },
            new UnitOfMeasure { Code = "M", Name = "Metre", Type = "Dimension", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded units of measure.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Number Sequences (matching exact backend Scope keys)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedNumberSequencesAsync(CancellationToken ct)
    {
        var existingScopes = await _db.NumberSequences
            .Select(n => n.Scope)
            .ToHashSetAsync(ct);

        var required = new[]
        {
            new NumberSequence { Scope = "ORDER",      Prefix = "ORD",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "WAYBILL",    Prefix = "WB",   NextNumber = 1, Increment = 1, NumberLength = 8, CurrentFormat = "{PREFIX}{YYYYMMDD}{NUMBER}",  ResetFrequency = "None"    },
            new NumberSequence { Scope = "INV",        Prefix = "INV",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "PAY",        Prefix = "PAY",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "FLE",        Prefix = "FLE",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "RECEIPT",    Prefix = "RCP",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "DISPATCH",   Prefix = "DSP",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "ADJUST",     Prefix = "ADJ",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "TRANSFER",   Prefix = "TRF",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "INV-TXN",    Prefix = "TXN",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "PICKUP",     Prefix = "PKP",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "RETURN",     Prefix = "RTN",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" },
            new NumberSequence { Scope = "REMITTANCE", Prefix = "REM",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly" }
        };

        var toAdd = required.Where(s => !existingScopes.Contains(s.Scope)).ToList();
        if (toAdd.Count > 0)
        {
            _db.NumberSequences.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DevDataSeeder: seeded {Count} number sequences.", toAdd.Count);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Company + Branches + Warehouses
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCompanyBranchesAndWarehousesAsync(CancellationToken ct)
    {
        if (await _db.Companies.AnyAsync(ct)) return;

        var company = new Company
        {
            Code = "LMS",
            LegalName = "Công ty TNHH Logistics Management",
            TradingName = "LMS Logistics",
            TaxNumber = "0123456789",
            Email = "contact@lms-logistics.vn",
            Phone = "028-3838-0000",
            Website = "https://lms-logistics.vn",
            IsActive = true
        };

        var hcmBranch = new Branch
        {
            Code = "HCM",
            Name = "Chi nhánh Hồ Chí Minh",
            BranchType = "Hub",
            Phone = "028-3838-0001",
            Email = "hcm@lms-logistics.vn",
            IsActive = true
        };

        var haBranch = new Branch
        {
            Code = "HAN",
            Name = "Chi nhánh Hà Nội",
            BranchType = "Hub",
            Phone = "024-3838-0001",
            Email = "hanoi@lms-logistics.vn",
            IsActive = true
        };

        company.Branches.Add(hcmBranch);
        company.Branches.Add(haBranch);

        hcmBranch.Warehouses.Add(new Warehouse
        {
            Code = "WH-HCM-01",
            Name = "Kho Hồ Chí Minh Trung Tâm",
            Type = "Hybrid",
            TotalAreaSqM = 5000m,
            IsActive = true
        });

        haBranch.Warehouses.Add(new Warehouse
        {
            Code = "WH-HAN-01",
            Name = "Kho Hà Nội Trung Tâm",
            Type = "Hybrid",
            TotalAreaSqM = 4000m,
            IsActive = true
        });

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded company, branches, and warehouses.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Warehouse Zones + Locations
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedWarehouseZonesAndLocationsAsync(CancellationToken ct)
    {
        if (await _db.WarehouseZones.AnyAsync(ct)) return;

        var warehouses = await _db.Warehouses.ToListAsync(ct);
        if (warehouses.Count == 0) return;

        foreach (var wh in warehouses)
        {
            var receivingZone = new WarehouseZone
            {
                WarehouseId = wh.Id,
                Code = $"{wh.Code}-RCV",
                Name = $"Khu Tiếp Nhận ({wh.Code})",
                ZoneType = "Receiving",
                TemperatureType = "Ambient",
                IsActive = true
            };
            receivingZone.Locations.Add(new WarehouseLocation
            {
                Code = $"{wh.Code}-RCV-01",
                Aisle = "A",
                Rack = "01",
                Shelf = "01",
                Bin = "01",
                Barcode = $"{wh.Code}-RCV-01"
            });

            var storageZone = new WarehouseZone
            {
                WarehouseId = wh.Id,
                Code = $"{wh.Code}-STR",
                Name = $"Khu Lưu Trữ ({wh.Code})",
                ZoneType = "BulkStorage",
                TemperatureType = "Ambient",
                IsActive = true
            };
            storageZone.Locations.Add(new WarehouseLocation
            {
                Code = $"{wh.Code}-STR-A01",
                Aisle = "A",
                Rack = "01",
                Shelf = "01",
                Bin = "01",
                Barcode = $"{wh.Code}-STR-A01"
            });
            storageZone.Locations.Add(new WarehouseLocation
            {
                Code = $"{wh.Code}-STR-A02",
                Aisle = "A",
                Rack = "01",
                Shelf = "01",
                Bin = "02",
                Barcode = $"{wh.Code}-STR-A02"
            });

            var hubZone = new WarehouseZone
            {
                WarehouseId = wh.Id,
                Code = $"{wh.Code}-HUB",
                Name = $"Khu Phân Loại & Xuất Hub ({wh.Code})",
                ZoneType = "ParcelSorting",
                TemperatureType = "Ambient",
                IsActive = true
            };
            hubZone.Locations.Add(new WarehouseLocation
            {
                Code = $"{wh.Code}-HUB-STAGE01",
                Aisle = "S",
                Rack = "01",
                Shelf = "01",
                Bin = "01",
                Barcode = $"{wh.Code}-HUB-STAGE01"
            });

            _db.WarehouseZones.AddRange(receivingZone, storageZone, hubZone);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded warehouse zones and locations.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Departments
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedDepartmentsAsync(CancellationToken ct)
    {
        if (await _db.Departments.AnyAsync(ct)) return;

        var company = await _db.Companies.FirstAsync(ct);
        var hcmBranch = await _db.Branches.FirstAsync(b => b.Code == "HCM", ct);
        var haBranch = await _db.Branches.FirstAsync(b => b.Code == "HAN", ct);

        _db.Departments.AddRange(
            new Department { CompanyId = company.Id, BranchId = hcmBranch.Id, Code = "OPS-HCM", Name = "Vận hành HCM", IsActive = true },
            new Department { CompanyId = company.Id, BranchId = hcmBranch.Id, Code = "FIN-HCM", Name = "Tài chính HCM", IsActive = true },
            new Department { CompanyId = company.Id, BranchId = hcmBranch.Id, Code = "CS-HCM", Name = "Dịch vụ khách hàng HCM", IsActive = true },
            new Department { CompanyId = company.Id, BranchId = hcmBranch.Id, Code = "WH-HCM", Name = "Kho vận HCM", IsActive = true },
            new Department { CompanyId = company.Id, BranchId = haBranch.Id, Code = "OPS-HAN", Name = "Vận hành Hà Nội", IsActive = true },
            new Department { CompanyId = company.Id, BranchId = haBranch.Id, Code = "FIN-HAN", Name = "Tài chính Hà Nội", IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded departments.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. Employees + User Accounts (every role covered)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedEmployeesAndUsersAsync(CancellationToken ct)
    {
        if (await _db.Employees.AnyAsync(ct)) return;

        var hcmBranch = await _db.Branches.FirstAsync(b => b.Code == "HCM", ct);
        var haBranch = await _db.Branches.FirstAsync(b => b.Code == "HAN", ct);
        var opsDept = await _db.Departments.FirstAsync(d => d.Code == "OPS-HCM", ct);
        var finDept = await _db.Departments.FirstAsync(d => d.Code == "FIN-HCM", ct);
        var csDept = await _db.Departments.FirstAsync(d => d.Code == "CS-HCM", ct);
        var whDept = await _db.Departments.FirstAsync(d => d.Code == "WH-HCM", ct);
        var opsHaDept = await _db.Departments.FirstAsync(d => d.Code == "OPS-HAN", ct);

        var roles = await _db.Roles.ToDictionaryAsync(r => r.Code, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        Employee MakeEmployee(string code, string first, string last, string job,
                              string phone,
                              long branchId, long? deptId,
                              string username, string password,
                              string roleCode, bool isDefaultBranch = true)
        {
            var emp = new Employee
            {
                BranchId = branchId,
                DepartmentId = deptId,
                Code = code,
                FirstName = first,
                LastName = last,
                FullName = $"{last} {first}",
                Email = $"{username}@lms-logistics.vn",
                PhoneNumber = phone,
                JobTitle = job,
                HireDate = today.AddYears(-2),
                Status = "Active"
            };

            var account = new UserAccount
            {
                Username = username,
                Email = emp.Email,
                PasswordHash = _hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true,
                CreatedBy = "DevSeed"
            };

            if (roles.TryGetValue(roleCode, out var role))
            {
                account.UserRoles.Add(new UserRole
                {
                    RoleId = role.Id,
                    AssignedBy = "DevSeed",
                    AssignedAt = DateTime.UtcNow
                });
            }

            account.BranchAccesses.Add(new UserBranchAccess
            {
                BranchId = branchId,
                IsDefault = isDefaultBranch,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = "DevSeed"
            });

            emp.UserAccount = account;
            return emp;
        }

        const string defaultPw = "Test@123456";

        var employees = new List<Employee>
        {
            // Branch Manager (access to both HCM & HAN)
            MakeEmployee("EMP-HCM-MGR", "Minh", "Nguyễn", "Trưởng Chi Nhánh",
                         "0901000001", hcmBranch.Id, opsDept.Id,
                         "branch.manager", defaultPw, AppRoles.BranchManager),

            // Operations Manager / Dispatcher
            MakeEmployee("EMP-HCM-OPS", "Hoa", "Trần", "Quản lý Vận hành",
                         "0901000002", hcmBranch.Id, opsDept.Id,
                         "ops.manager", defaultPw, AppRoles.OperationsManager),

            // Finance Staff
            MakeEmployee("EMP-HCM-FIN", "Long", "Lê", "Nhân viên Tài chính",
                         "0901000003", hcmBranch.Id, finDept.Id,
                         "finance.staff", defaultPw, AppRoles.FinanceStaff),

            // Customer Service
            MakeEmployee("EMP-HCM-CS", "Lan", "Phạm", "Nhân viên CSKH",
                         "0901000004", hcmBranch.Id, csDept.Id,
                         "cs.staff", defaultPw, AppRoles.CustomerService),

            // Warehouse / Delivery Staff (for Parcel Hub, Inventory & COD collection tests)
            MakeEmployee("EMP-HCM-WH", "Tuấn", "Vũ", "Nhân viên Kho & Giao nhận",
                         "0901000005", hcmBranch.Id, whDept.Id,
                         "wh.staff", defaultPw, AppRoles.WarehouseStaff),

            // Read-only viewer
            MakeEmployee("EMP-HCM-RO", "An", "Đỗ", "Giám sát",
                         "0901000006", hcmBranch.Id, opsDept.Id,
                         "viewer", defaultPw, AppRoles.ReadOnly),

            // Hanoi branch manager
            MakeEmployee("EMP-HAN-MGR", "Bình", "Hoàng", "Trưởng Chi Nhánh Hà Nội",
                         "0902000001", haBranch.Id, opsHaDept.Id,
                         "hanoi.manager", defaultPw, AppRoles.BranchManager),
        };

        _db.Employees.AddRange(employees);
        await _db.SaveChangesAsync(ct);

        // Grant HCM branch manager access to HAN too for multi-branch testing
        var mgrAccount = employees[0].UserAccount!;
        mgrAccount.BranchAccesses.Add(new UserBranchAccess
        {
            BranchId = haBranch.Id,
            IsDefault = false,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = "DevSeed"
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("DevDataSeeder: seeded {Count} employees and user accounts.", employees.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. Grant Admin Bootstrap account branch access so dropdown shows HCM/HAN
    // ─────────────────────────────────────────────────────────────────────────
    private async Task EnsureBootstrapAdminBranchAccessAsync(CancellationToken ct)
    {
        var admin = await _db.UserAccounts
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.Username == "admin", ct);
        if (admin == null || admin.BranchAccesses.Count > 0) return;

        var hcmBranch = await _db.Branches.FirstOrDefaultAsync(b => b.Code == "HCM", ct);
        var haBranch = await _db.Branches.FirstOrDefaultAsync(b => b.Code == "HAN", ct);

        if (hcmBranch != null)
        {
            admin.BranchAccesses.Add(new UserBranchAccess
            {
                BranchId = hcmBranch.Id,
                IsDefault = true,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = "DevSeed"
            });
        }
        if (haBranch != null)
        {
            admin.BranchAccesses.Add(new UserBranchAccess
            {
                BranchId = haBranch.Id,
                IsDefault = false,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = "DevSeed"
            });
        }

        if (admin.BranchAccesses.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DevDataSeeder: granted branch access (HCM, HAN) to 'admin'.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 10. Sample Customers
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCustomersAsync(CancellationToken ct)
    {
        if (await _db.Customers.AnyAsync(ct)) return;

        var hcmBranch = await _db.Branches.FirstAsync(b => b.Code == "HCM", ct);
        var haBranch = await _db.Branches.FirstAsync(b => b.Code == "HAN", ct);
        var csEmp = await _db.Employees.FirstOrDefaultAsync(e => e.Code == "EMP-HCM-CS", ct);

        _db.Customers.AddRange(
            new Customer
            {
                BranchId = hcmBranch.Id,
                CustomerCode = "CUS-HCM-001",
                CustomerType = "Corporate",
                Name = "Công ty CP Thương mại ABC",
                TradeName = "ABC Trading",
                TaxCode = "0300000001",
                Email = "info@abc-trading.vn",
                PhoneNumber = "028-3838-1001",
                CreditLimit = 50_000_000m,
                CreditTermDays = 30,
                PaymentType = "PostpaidMonthly",
                AccountManagerEmployeeId = csEmp?.Id,
                IsActive = true
            },
            new Customer
            {
                BranchId = hcmBranch.Id,
                CustomerCode = "CUS-HCM-002",
                CustomerType = "Corporate",
                Name = "Công ty TNHH Xuất nhập khẩu XYZ",
                TradeName = "XYZ Import Export",
                TaxCode = "0300000002",
                Email = "contact@xyz-ie.vn",
                PhoneNumber = "028-3838-1002",
                CreditLimit = 100_000_000m,
                CreditTermDays = 45,
                PaymentType = "PostpaidMonthly",
                IsActive = true
            },
            new Customer
            {
                BranchId = hcmBranch.Id,
                CustomerCode = "CUS-HCM-003",
                CustomerType = "Individual",
                Name = "Nguyễn Văn Test",
                Email = "test.nguyen@gmail.com",
                PhoneNumber = "0901234567",
                CreditLimit = 0m,
                CreditTermDays = 0,
                PaymentType = "Prepaid",
                IsActive = true
            },
            new Customer
            {
                BranchId = haBranch.Id,
                CustomerCode = "CUS-HAN-001",
                CustomerType = "Corporate",
                Name = "Tập đoàn Phân phối Hà Nội",
                TaxCode = "0100000001",
                Email = "info@hanoi-dist.vn",
                PhoneNumber = "024-3838-2001",
                CreditLimit = 200_000_000m,
                CreditTermDays = 60,
                PaymentType = "PostpaidMonthly",
                IsActive = true
            },
            new Customer
            {
                BranchId = hcmBranch.Id,
                CustomerCode = "CUS-HCM-COD",
                CustomerType = "Corporate",
                Name = "Shop Online COD Demo",
                TaxCode = "0300000099",
                Email = "cod@shop-demo.vn",
                PhoneNumber = "028-9999-9999",
                CreditLimit = 10_000_000m,
                CreditTermDays = 7,
                PaymentType = "CodOffset",
                IsActive = true
            }
        );
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded customers.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 11. Product Catalog (categories + products)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedProductCatalogAsync(CancellationToken ct)
    {
        if (await _db.ProductCategories.AnyAsync(ct)) return;

        var pcsUom = await _db.UnitsOfMeasure.FirstAsync(u => u.Code == "PCS", ct);
        var kgUom = await _db.UnitsOfMeasure.FirstAsync(u => u.Code == "KG", ct);
        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);

        var catElec = new ProductCategory { Code = "CAT-ELEC", Name = "Điện tử - Điện máy", IsActive = true };
        var catFashion = new ProductCategory { Code = "CAT-FASHION", Name = "Thời trang", IsActive = true };
        var catFood = new ProductCategory { Code = "CAT-FOOD", Name = "Thực phẩm - Đồ uống", IsActive = true };
        var catDoc = new ProductCategory { Code = "CAT-DOC", Name = "Tài liệu - Văn phòng phẩm", IsActive = true };
        var catBulk = new ProductCategory { Code = "CAT-BULK", Name = "Hàng thùng lớn", IsActive = true };

        _db.ProductCategories.AddRange(catElec, catFashion, catFood, catDoc, catBulk);
        await _db.SaveChangesAsync(ct);

        _db.Products.AddRange(
            new Product { CategoryId = catElec.Id, Sku = "ELEC-001", Name = "Điện thoại thông minh 6\"", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.2m, LengthCm = 16m, WidthCm = 8m, HeightCm = 1m, DeclaredValue = 5_000_000m, IsActive = true },
            new Product { CategoryId = catElec.Id, Sku = "ELEC-002", Name = "Máy tính bảng 10\"", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.5m, LengthCm = 24m, WidthCm = 17m, HeightCm = 1m, DeclaredValue = 8_000_000m, IsActive = true },
            new Product { CategoryId = catElec.Id, Sku = "ELEC-003", Name = "Laptop 15\"", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 2.1m, LengthCm = 36m, WidthCm = 25m, HeightCm = 3m, DeclaredValue = 20_000_000m, IsActive = true },
            new Product { CategoryId = catElec.Id, Sku = "ELEC-004", Name = "Tai nghe không dây", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.3m, LengthCm = 20m, WidthCm = 18m, HeightCm = 8m, DeclaredValue = 2_500_000m, IsActive = true },
            new Product { CategoryId = catFashion.Id, Sku = "FASH-001", Name = "Áo thun unisex", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.3m, LengthCm = 30m, WidthCm = 20m, HeightCm = 5m, DeclaredValue = 350_000m, IsActive = true },
            new Product { CategoryId = catFashion.Id, Sku = "FASH-002", Name = "Quần jean nam", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.6m, LengthCm = 35m, WidthCm = 25m, HeightCm = 5m, DeclaredValue = 600_000m, IsActive = true },
            new Product { CategoryId = catFashion.Id, Sku = "FASH-003", Name = "Giày sneaker", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 1.0m, LengthCm = 32m, WidthCm = 20m, HeightCm = 15m, DeclaredValue = 1_200_000m, IsActive = true },
            new Product { CategoryId = catFood.Id, Sku = "FOOD-001", Name = "Cà phê rang xay 500g", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.55m, LengthCm = 20m, WidthCm = 10m, HeightCm = 5m, DeclaredValue = 180_000m, IsPerishable = true, IsActive = true },
            new Product { CategoryId = catFood.Id, Sku = "FOOD-002", Name = "Trái cây sấy khô 1kg", UnitOfMeasureId = kgUom.Id, CurrencyId = vnd.Id, WeightKg = 1.1m, LengthCm = 25m, WidthCm = 20m, HeightCm = 10m, DeclaredValue = 250_000m, IsPerishable = true, IsActive = true },
            new Product { CategoryId = catDoc.Id, Sku = "DOC-001", Name = "Hồ sơ tài liệu A4", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.5m, LengthCm = 31m, WidthCm = 22m, HeightCm = 3m, DeclaredValue = 50_000m, IsActive = true },
            new Product { CategoryId = catDoc.Id, Sku = "DOC-002", Name = "Thư từ, bưu phẩm nhỏ", UnitOfMeasureId = pcsUom.Id, CurrencyId = vnd.Id, WeightKg = 0.1m, LengthCm = 24m, WidthCm = 17m, HeightCm = 1m, DeclaredValue = 20_000m, IsActive = true },
            new Product { CategoryId = catBulk.Id, Sku = "BULK-001", Name = "Thùng hàng thương mại 20kg", UnitOfMeasureId = kgUom.Id, CurrencyId = vnd.Id, WeightKg = 20m, LengthCm = 60m, WidthCm = 40m, HeightCm = 40m, DeclaredValue = 1_000_000m, IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded product categories and products.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 12. Carriers & Services
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCarriersAsync(CancellationToken ct)
    {
        if (await _db.Carriers.AnyAsync(ct)) return;

        var ghn = new Carrier
        {
            Code = "GHN",
            Name = "Giao Hàng Nhanh",
            ContactPhone = "1900-636677",
            ContactEmail = "cskh@ghn.vn",
            ApiBaseUrl = "https://sandbox.ghn.vn/api",
            TrackingUrlTemplate = "https://donhang.ghn.vn/?order_code={TRACKING}",
            IsActive = true
        };
        ghn.Services.Add(new CarrierService { ServiceCode = "GHN-STD", ServiceName = "GHN Chuẩn", EstimatedTransitHours = 48, MaxWeightKg = 50m, IsCodSupported = true, IsInsuranceSupported = true, IsActive = true });
        ghn.Services.Add(new CarrierService { ServiceCode = "GHN-EXP", ServiceName = "GHN Hỏa Tốc", EstimatedTransitHours = 12, MaxWeightKg = 20m, IsCodSupported = true, IsInsuranceSupported = true, IsActive = true });

        var ghtk = new Carrier
        {
            Code = "GHTK",
            Name = "Giao Hàng Tiết Kiệm",
            ContactPhone = "1900-6092",
            ContactEmail = "cskh@ghtk.vn",
            ApiBaseUrl = "https://sandbox.ghtk.vn/services",
            TrackingUrlTemplate = "https://i.ghtk.vn/{TRACKING}",
            IsActive = true
        };
        ghtk.Services.Add(new CarrierService { ServiceCode = "GHTK-ECO", ServiceName = "GHTK Tiết Kiệm", EstimatedTransitHours = 72, MaxWeightKg = 30m, IsCodSupported = true, IsInsuranceSupported = true, IsActive = true });

        var vtpost = new Carrier
        {
            Code = "VTPOST",
            Name = "Viettel Post",
            ContactPhone = "1900-8095",
            ContactEmail = "support@viettelpost.vn",
            ApiBaseUrl = "https://partner.viettelpost.vn/v2",
            TrackingUrlTemplate = "https://viettelpost.com.vn/tra-cuu-hanh-trinh-don/{TRACKING}",
            IsActive = true
        };
        vtpost.Services.Add(new CarrierService { ServiceCode = "VTP-VCN", ServiceName = "Chuyển phát nhanh VCN", EstimatedTransitHours = 24, MaxWeightKg = 100m, IsCodSupported = true, IsInsuranceSupported = true, IsActive = true });

        _db.Carriers.AddRange(ghn, ghtk, vtpost);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded carriers and carrier services.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 13. Default Shipping Rate Card & Rules
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedShippingRateCardAsync(CancellationToken ct)
    {
        if (await _db.ShippingRateCards.AnyAsync(ct)) return;

        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);
        var card = new ShippingRateCard
        {
            Code = "RATE-STD-2026",
            Name = "Bảng giá tiêu chuẩn 2026",
            Description = "Bảng giá vận chuyển mặc định áp dụng toàn hệ thống",
            BaseCurrencyId = vnd.Id,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDefault = true,
            IsActive = true
        };

        card.Rules.Add(new ShippingRateRule
        {
            ServiceType = "StandardGround",
            FromWeightKg = 0m,
            ToWeightKg = 2m,
            BaseRate = 25_000m,
            AdditionalPerKgRate = 5_000m,
            SurchargePercent = 0m,
            MinimumFee = 20_000m,
            EstimatedDays = 3
        });
        card.Rules.Add(new ShippingRateRule
        {
            ServiceType = "StandardGround",
            FromWeightKg = 2.01m,
            ToWeightKg = 50m,
            BaseRate = 35_000m,
            AdditionalPerKgRate = 8_000m,
            SurchargePercent = 0m,
            MinimumFee = 35_000m,
            EstimatedDays = 4
        });
        card.Rules.Add(new ShippingRateRule
        {
            ServiceType = "Express",
            FromWeightKg = 0m,
            ToWeightKg = 30m,
            BaseRate = 45_000m,
            AdditionalPerKgRate = 12_000m,
            SurchargePercent = 5m,
            MinimumFee = 45_000m,
            EstimatedDays = 1
        });
        card.Rules.Add(new ShippingRateRule
        {
            ServiceType = "SameDay",
            FromWeightKg = 0m,
            ToWeightKg = 15m,
            BaseRate = 65_000m,
            AdditionalPerKgRate = 15_000m,
            SurchargePercent = 10m,
            MinimumFee = 65_000m,
            EstimatedDays = 0
        });

        _db.ShippingRateCards.Add(card);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded default shipping rate card and rules.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 14. Notification Templates
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedNotificationTemplatesAsync(CancellationToken ct)
    {
        if (await _db.NotificationTemplates.AnyAsync(ct)) return;

        _db.NotificationTemplates.AddRange(
            new NotificationTemplate
            {
                Code = "ORDER_CONFIRMED",
                Name = "Xác nhận đơn hàng",
                Channel = "InApp",
                SubjectTemplate = "Đơn hàng {{OrderNumber}} đã được xác nhận",
                BodyTemplate = "Đơn hàng {{OrderNumber}} của khách hàng {{CustomerName}} đã được xác nhận.",
                IsActive = true
            },
            new NotificationTemplate
            {
                Code = "SHIPMENT_DISPATCHED",
                Name = "Vận đơn xuất kho",
                Channel = "InApp",
                SubjectTemplate = "Vận đơn {{TrackingNumber}} đã xuất kho",
                BodyTemplate = "Vận đơn {{TrackingNumber}} đang được giao tới người nhận.",
                IsActive = true
            },
            new NotificationTemplate
            {
                Code = "INVOICE_ISSUED",
                Name = "Phát hành hóa đơn",
                Channel = "InApp",
                SubjectTemplate = "Hóa đơn {{InvoiceNumber}} đã phát hành",
                BodyTemplate = "Hóa đơn {{InvoiceNumber}} với tổng tiền {{TotalAmount}} đã được phát hành.",
                IsActive = true
            }
        );
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DevDataSeeder: seeded notification templates.");
    }
}
