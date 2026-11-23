using System.Text.Json;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

#region Company Service
public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CompanyService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDto?> GetPrimaryCompanyAsync(CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return company == null ? null : MapToDto(company);
    }

    public async Task<CompanyDto> UpdateCompanyAsync(UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
        {
            throw new NotFoundException("Company", "Primary");
        }

        var taxExists = await _context.Companies
            .AnyAsync(c => c.TaxNumber == request.TaxNumber && c.Id != company.Id, cancellationToken);
        if (taxExists)
        {
            throw new ConflictException("Company", "TaxNumber", request.TaxNumber);
        }

        var oldValues = new
        {
            company.LegalName,
            company.TradingName,
            company.TaxNumber,
            company.Email,
            company.Phone,
            company.Website,
            company.LogoUrl
        };

        company.LegalName = request.LegalName;
        company.TradingName = request.TradingName;
        company.TaxNumber = request.TaxNumber;
        company.Email = request.Email;
        company.Phone = request.Phone;
        company.Website = request.Website;
        company.LogoUrl = request.LogoUrl;
        company.UpdatedAt = DateTime.UtcNow;
        company.UpdatedBy = _currentUserService.Username;

        await _auditService.RecordAsync(
            "Update", "organization", "Companies", company.Id.ToString(),
            oldValues, request, null, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(company);
    }

    public async Task<OrganizationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var isGlobal = _currentUserService.IsInRole(AppRoles.SuperAdmin)
                    || _currentUserService.IsInRole("SuperAdmin")
                    || _currentUserService.IsInRole("Admin");

        var allowedBranches = isGlobal ? new List<long>() : GetAllowedBranchIds();
        var scopeType = isGlobal ? "Global" : "BranchScoped";

        var branchQuery = _context.Branches.AsNoTracking();
        var deptQuery = _context.Departments.AsNoTracking();
        var empQuery = _context.Employees.AsNoTracking();
        var whQuery = _context.Warehouses.AsNoTracking();

        if (!isGlobal)
        {
            branchQuery = branchQuery.Where(b => allowedBranches.Contains(b.Id));
            deptQuery = deptQuery.Where(d => allowedBranches.Contains(d.BranchId));
            empQuery = empQuery.Where(e => allowedBranches.Contains(e.BranchId));
            whQuery = whQuery.Where(w => allowedBranches.Contains(w.BranchId));
        }

        var totalBranches = await branchQuery.CountAsync(cancellationToken);
        var activeBranches = await branchQuery.CountAsync(b => b.IsActive, cancellationToken);
        var inactiveBranches = totalBranches - activeBranches;

        var totalDepartments = await deptQuery.CountAsync(cancellationToken);
        var activeDepartments = await deptQuery.CountAsync(d => d.IsActive, cancellationToken);

        var totalEmployees = await empQuery.CountAsync(cancellationToken);
        var activeEmployees = await empQuery.CountAsync(e => e.Status == "Active", cancellationToken);
        var inactiveEmployees = await empQuery.CountAsync(e => e.Status == "Inactive", cancellationToken);
        var onLeaveEmployees = await empQuery.CountAsync(e => e.Status == "OnLeave", cancellationToken);
        var resignedEmployees = await empQuery.CountAsync(e => e.Status == "Resigned", cancellationToken);
        var terminatedEmployees = await empQuery.CountAsync(e => e.Status == "Terminated", cancellationToken);

        var totalWarehouses = await whQuery.CountAsync(cancellationToken);
        var activeWarehouses = await whQuery.CountAsync(w => w.IsActive, cancellationToken);
        var inactiveWarehouses = totalWarehouses - activeWarehouses;

        return new OrganizationOverviewDto(
            totalBranches,
            activeBranches,
            inactiveBranches,
            totalDepartments,
            activeDepartments,
            totalEmployees,
            activeEmployees,
            inactiveEmployees,
            onLeaveEmployees,
            resignedEmployees,
            terminatedEmployees,
            totalWarehouses,
            activeWarehouses,
            inactiveWarehouses,
            scopeType,
            isGlobal ? totalBranches : allowedBranches.Count
        );
    }

    public async Task<PagedResult<CompanyDto>> GetCompaniesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Companies.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.LegalName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CompanyDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return company == null ? null : MapToDto(company);
    }

    public async Task<CompanyDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);

        return company == null ? null : MapToDto(company);
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var codeExists = await _context.Companies.AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException("Company", "Code", request.Code);
        }

        var taxExists = await _context.Companies.AnyAsync(c => c.TaxNumber == request.TaxNumber, cancellationToken);
        if (taxExists)
        {
            throw new ConflictException("Company", "TaxNumber", request.TaxNumber);
        }

        var entity = new Company
        {
            Code = request.Code,
            LegalName = request.LegalName,
            TradingName = request.TradingName,
            TaxNumber = request.TaxNumber,
            Email = request.Email,
            Phone = request.Phone,
            Website = request.Website,
            LogoUrl = request.LogoUrl,
            IsActive = true,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Companies.AddAsync(entity, cancellationToken);
        await _auditService.RecordAsync("Create", "organization", "Companies", entity.Code, null, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    private List<long> GetAllowedBranchIds()
    {
        var allowed = _currentUserService.AccessibleBranchIds.ToList();
        if (_currentUserService.DefaultBranchId.HasValue && !allowed.Contains(_currentUserService.DefaultBranchId.Value))
        {
            allowed.Add(_currentUserService.DefaultBranchId.Value);
        }
        return allowed;
    }

    private static CompanyDto MapToDto(Company c) => new(
        c.Id,
        c.PublicId,
        c.Code,
        c.LegalName,
        c.TradingName,
        c.TaxNumber,
        c.Email,
        c.Phone,
        c.Website,
        c.LogoUrl,
        c.IsActive,
        c.CreatedAt
    );
}
#endregion

#region Branch Service
public class BranchService : IBranchService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public BranchService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<BranchDto>> GetBranchesAsync(
        string? search,
        bool? isActive,
        string? branchType,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Branches
            .AsNoTracking()
            .Include(b => b.Address)
            .AsQueryable();

        var isSuperAdmin = IsSuperAdmin();
        if (!isSuperAdmin)
        {
            var allowed = GetAllowedBranchIds();
            query = query.Where(b => allowed.Contains(b.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => b.Name.Contains(term) || b.Code.Contains(term) || b.Phone.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(b => b.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(branchType))
        {
            query = query.Where(b => b.BranchType == branchType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => MapToDto(b))
            .ToListAsync(cancellationToken);

        return new PagedResult<BranchDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<BranchDetailDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, true, cancellationToken);
        if (branch == null) return null;

        VerifyBranchAccess(branch.Id);

        var employeeCount = await _context.Employees.CountAsync(e => e.BranchId == branch.Id, cancellationToken);
        var departmentCount = await _context.Departments.CountAsync(d => d.BranchId == branch.Id, cancellationToken);
        var warehouseCount = await _context.Warehouses.CountAsync(w => w.BranchId == branch.Id, cancellationToken);

        var departments = await _context.Departments
            .AsNoTracking()
            .Include(d => d.ManagerEmployee)
            .Where(d => d.BranchId == branch.Id)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.PublicId,
                d.CompanyId,
                d.BranchId,
                branch.Code,
                branch.Name,
                d.Code,
                d.Name,
                d.Description,
                d.ManagerEmployeeId,
                d.ManagerEmployee != null ? d.ManagerEmployee.FullName : null,
                d.IsActive,
                d.CreatedAt,
                0))
            .ToListAsync(cancellationToken);

        var warehouses = await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.ManagerEmployee)
            .Include(w => w.Zones)
            .Where(w => w.BranchId == branch.Id)
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(
                w.Id,
                w.PublicId,
                w.BranchId,
                branch.Code,
                branch.Name,
                w.Code,
                w.Name,
                w.Type,
                w.TotalAreaSqM,
                w.AddressId,
                w.ManagerEmployeeId,
                w.ManagerEmployee != null ? w.ManagerEmployee.FullName : null,
                w.IsActive,
                w.CreatedAt,
                w.Zones.Count))
            .ToListAsync(cancellationToken);

        return new BranchDetailDto(
            MapToDto(branch),
            employeeCount,
            departmentCount,
            warehouseCount,
            departments,
            warehouses
        );
    }

    public async Task<BranchSummaryDto?> GetSummaryByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, false, cancellationToken);
        if (branch == null) return null;

        VerifyBranchAccess(branch.Id);

        var employeeCount = await _context.Employees.CountAsync(e => e.BranchId == branch.Id, cancellationToken);
        var departmentCount = await _context.Departments.CountAsync(d => d.BranchId == branch.Id, cancellationToken);
        var warehouseCount = await _context.Warehouses.CountAsync(w => w.BranchId == branch.Id, cancellationToken);
        var activeWarehouseCount = await _context.Warehouses.CountAsync(w => w.BranchId == branch.Id && w.IsActive, cancellationToken);

        return new BranchSummaryDto(
            branch.Id,
            branch.PublicId,
            branch.Code,
            branch.Name,
            branch.BranchType,
            branch.IsActive,
            employeeCount,
            departmentCount,
            warehouseCount,
            activeWarehouseCount
        );
    }

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = request.CompanyId.HasValue && request.CompanyId.Value > 0
            ? request.CompanyId.Value
            : (await _context.Companies.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(cancellationToken));

        if (companyId == 0)
        {
            throw new ValidationException("Company", "A valid company must exist before creating branches.");
        }

        var codeExists = await _context.Branches.AnyAsync(b => b.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException("Branch", "Code", request.Code);
        }

        var entity = new Branch
        {
            CompanyId = companyId,
            Code = request.Code,
            Name = request.Name,
            BranchType = request.BranchType,
            Phone = request.Phone,
            Email = request.Email,
            AddressId = request.AddressId,
            IsActive = true,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Branches.AddAsync(entity, cancellationToken);
        await _auditService.RecordAsync("Create", "organization", "Branches", entity.Code, null, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<BranchDto> UpdateAsync(string identifier, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, false, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", identifier);
        }

        VerifyBranchAccess(branch.Id);

        var oldValues = new
        {
            branch.Name,
            branch.BranchType,
            branch.Phone,
            branch.Email,
            branch.AddressId
        };

        branch.Name = request.Name;
        branch.BranchType = request.BranchType;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.AddressId = request.AddressId;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = _currentUserService.Username;

        await _auditService.RecordAsync("Update", "organization", "Branches", branch.Id.ToString(), oldValues, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(branch);
    }

    public async Task UpdateStatusAsync(string identifier, UpdateBranchStatusRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, false, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", identifier);
        }

        VerifyBranchAccess(branch.Id);

        if (!request.IsActive)
        {
            var hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.BranchId == branch.Id && e.Status == "Active", cancellationToken);
            if (hasActiveEmployees)
            {
                throw new ConflictException("Cannot deactivate branch with active employees assigned.");
            }

            var hasActiveWarehouses = await _context.Warehouses
                .AnyAsync(w => w.BranchId == branch.Id && w.IsActive, cancellationToken);
            if (hasActiveWarehouses)
            {
                throw new ConflictException("Cannot deactivate branch with active warehouses associated.");
            }
        }

        var oldStatus = branch.IsActive;
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = _currentUserService.Username;

        await _auditService.RecordAsync(
            "StatusChange", "organization", "Branches", branch.Id.ToString(),
            new { IsActive = oldStatus },
            new { IsActive = request.IsActive, request.Reason },
            request.Reason,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<EmployeeDto>> GetBranchEmployeesAsync(
        string identifier,
        string? search,
        string? status,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, false, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", identifier);
        }

        VerifyBranchAccess(branch.Id);

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.UserAccount)
            .Where(e => e.BranchId == branch.Id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Code.Contains(term) || e.FirstName.Contains(term) || e.LastName.Contains(term) || e.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeDto(
                e.Id,
                e.PublicId,
                e.BranchId,
                branch.Code,
                branch.Name,
                e.DepartmentId,
                e.Department != null ? e.Department.Code : null,
                e.Department != null ? e.Department.Name : null,
                e.Code,
                e.FirstName,
                e.LastName,
                e.FullName,
                e.Email,
                e.PhoneNumber,
                e.DateOfBirth,
                e.Gender,
                e.JobTitle,
                e.HireDate,
                e.Status,
                e.AddressId,
                e.CreatedAt,
                e.UserAccount != null,
                e.UserAccount != null ? e.UserAccount.PublicId : null,
                e.UserAccount != null ? e.UserAccount.Username : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetBranchDepartmentsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, false, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", identifier);
        }

        VerifyBranchAccess(branch.Id);

        return await _context.Departments
            .AsNoTracking()
            .Include(d => d.ManagerEmployee)
            .Where(d => d.BranchId == branch.Id)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.PublicId,
                d.CompanyId,
                d.BranchId,
                branch.Code,
                branch.Name,
                d.Code,
                d.Name,
                d.Description,
                d.ManagerEmployeeId,
                d.ManagerEmployee != null ? d.ManagerEmployee.FullName : null,
                d.IsActive,
                d.CreatedAt,
                d.Employees.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetBranchWarehousesAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var branch = await FindBranchByIdentifierAsync(identifier, false, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", identifier);
        }

        VerifyBranchAccess(branch.Id);

        return await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.ManagerEmployee)
            .Include(w => w.Zones)
            .Where(w => w.BranchId == branch.Id)
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(
                w.Id,
                w.PublicId,
                w.BranchId,
                branch.Code,
                branch.Name,
                w.Code,
                w.Name,
                w.Type,
                w.TotalAreaSqM,
                w.AddressId,
                w.ManagerEmployeeId,
                w.ManagerEmployee != null ? w.ManagerEmployee.FullName : null,
                w.IsActive,
                w.CreatedAt,
                w.Zones.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<BranchDto>> GetBranchesAsync(long? companyId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Branches.AsNoTracking();

        if (companyId.HasValue)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        var isSuperAdmin = IsSuperAdmin();
        if (!isSuperAdmin)
        {
            var allowed = GetAllowedBranchIds();
            query = query.Where(b => allowed.Contains(b.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => MapToDto(b))
            .ToListAsync(cancellationToken);

        return new PagedResult<BranchDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<BranchDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (branch != null) VerifyBranchAccess(branch.Id);
        return branch == null ? null : MapToDto(branch);
    }

    public async Task<BranchDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.PublicId == publicId, cancellationToken);

        if (branch != null) VerifyBranchAccess(branch.Id);
        return branch == null ? null : MapToDto(branch);
    }

    private async Task<Branch?> FindBranchByIdentifierAsync(string identifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Branches.AsNoTracking() : _context.Branches.AsQueryable();

        if (long.TryParse(identifier, out var id))
        {
            return await query.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        if (Guid.TryParse(identifier, out var publicId))
        {
            return await query.FirstOrDefaultAsync(b => b.PublicId == publicId, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(b => b.Code == identifier, cancellationToken);
    }

    private bool IsSuperAdmin() =>
        _currentUserService.IsInRole(AppRoles.SuperAdmin)
        || _currentUserService.IsInRole("SuperAdmin")
        || _currentUserService.IsInRole("Admin");

    private List<long> GetAllowedBranchIds()
    {
        var allowed = _currentUserService.AccessibleBranchIds.ToList();
        if (_currentUserService.DefaultBranchId.HasValue && !allowed.Contains(_currentUserService.DefaultBranchId.Value))
        {
            allowed.Add(_currentUserService.DefaultBranchId.Value);
        }
        return allowed;
    }

    private void VerifyBranchAccess(long branchId)
    {
        if (IsSuperAdmin()) return;
        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException("You do not have authorization to access this operating branch.");
        }
    }

    private static BranchDto MapToDto(Branch b) => new(
        b.Id,
        b.PublicId,
        b.CompanyId,
        b.Code,
        b.Name,
        b.BranchType,
        b.Phone,
        b.Email,
        b.AddressId,
        b.IsActive,
        b.CreatedAt,
        b.Address?.FormattedAddress
    );
}
#endregion

#region Department Service
public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public DepartmentService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(
        string? search,
        long? branchId,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Departments
            .AsNoTracking()
            .Include(d => d.Branch)
            .Include(d => d.ManagerEmployee)
            .Include(d => d.Employees)
            .AsQueryable();

        if (!IsSuperAdmin())
        {
            var allowed = GetAllowedBranchIds();
            query = query.Where(d => allowed.Contains(d.BranchId));
        }

        if (branchId.HasValue)
        {
            VerifyBranchAccess(branchId.Value);
            query = query.Where(d => d.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Name.Contains(term) || d.Code.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => MapToDto(d))
            .ToListAsync(cancellationToken);

        return new PagedResult<DepartmentDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<DepartmentDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var department = await FindDepartmentByIdentifierAsync(identifier, true, cancellationToken);
        if (department == null) return null;

        VerifyBranchAccess(department.BranchId);
        return MapToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        VerifyBranchAccess(request.BranchId);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            throw new ValidationException("Branch", "Branch not found or is currently inactive.");
        }

        var codeExists = await _context.Departments
            .AnyAsync(d => d.BranchId == request.BranchId && d.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException("Department", "Code", request.Code);
        }

        if (request.ManagerEmployeeId.HasValue)
        {
            var manager = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.ManagerEmployeeId.Value, cancellationToken);
            if (manager == null || manager.BranchId != request.BranchId)
            {
                throw new ValidationException("Department", "Department manager must belong to the same branch.");
            }
        }

        var entity = new Department
        {
            CompanyId = branch.CompanyId,
            BranchId = request.BranchId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            ManagerEmployeeId = request.ManagerEmployeeId,
            IsActive = true,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Departments.AddAsync(entity, cancellationToken);
        await _auditService.RecordAsync("Create", "organization", "Departments", entity.Code, null, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        entity.Branch = branch;
        return MapToDto(entity);
    }

    public async Task<DepartmentDto> UpdateAsync(string identifier, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = await FindDepartmentByIdentifierAsync(identifier, false, cancellationToken);
        if (department == null)
        {
            throw new NotFoundException("Department", identifier);
        }

        VerifyBranchAccess(department.BranchId);

        if (request.ManagerEmployeeId.HasValue)
        {
            var manager = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.ManagerEmployeeId.Value, cancellationToken);
            if (manager == null || manager.BranchId != department.BranchId)
            {
                throw new ValidationException("Department", "Department manager must belong to the same branch.");
            }
        }

        var oldValues = new
        {
            department.Name,
            department.Description,
            department.ManagerEmployeeId
        };

        department.Name = request.Name;
        department.Description = request.Description;
        department.ManagerEmployeeId = request.ManagerEmployeeId;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = _currentUserService.Username;

        await _auditService.RecordAsync("Update", "organization", "Departments", department.Id.ToString(), oldValues, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(department);
    }

    public async Task UpdateStatusAsync(string identifier, UpdateDepartmentStatusRequest request, CancellationToken cancellationToken = default)
    {
        var department = await FindDepartmentByIdentifierAsync(identifier, false, cancellationToken);
        if (department == null)
        {
            throw new NotFoundException("Department", identifier);
        }

        VerifyBranchAccess(department.BranchId);

        if (!request.IsActive)
        {
            var hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.DepartmentId == department.Id && e.Status == "Active", cancellationToken);
            if (hasActiveEmployees)
            {
                throw new ConflictException("Cannot deactivate department with active employees assigned.");
            }
        }

        var oldStatus = department.IsActive;
        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = _currentUserService.Username;

        await _auditService.RecordAsync(
            "StatusChange", "organization", "Departments", department.Id.ToString(),
            new { IsActive = oldStatus },
            new { IsActive = request.IsActive, request.Reason },
            request.Reason,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<EmployeeDto>> GetDepartmentEmployeesAsync(string identifier, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var department = await FindDepartmentByIdentifierAsync(identifier, true, cancellationToken);
        if (department == null)
        {
            throw new NotFoundException("Department", identifier);
        }

        VerifyBranchAccess(department.BranchId);

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Branch)
            .Include(e => e.UserAccount)
            .Where(e => e.DepartmentId == department.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeDto(
                e.Id,
                e.PublicId,
                e.BranchId,
                e.Branch.Code,
                e.Branch.Name,
                department.Id,
                department.Code,
                department.Name,
                e.Code,
                e.FirstName,
                e.LastName,
                e.FullName,
                e.Email,
                e.PhoneNumber,
                e.DateOfBirth,
                e.Gender,
                e.JobTitle,
                e.HireDate,
                e.Status,
                e.AddressId,
                e.CreatedAt,
                e.UserAccount != null,
                e.UserAccount != null ? e.UserAccount.PublicId : null,
                e.UserAccount != null ? e.UserAccount.Username : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeDto>(items, totalCount, request.Page, request.PageSize);
    }

    private async Task<Department?> FindDepartmentByIdentifierAsync(string identifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Departments.AsNoTracking() : _context.Departments.AsQueryable();
        query = query.Include(d => d.Branch).Include(d => d.ManagerEmployee).Include(d => d.Employees);

        if (long.TryParse(identifier, out var id))
        {
            return await query.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        if (Guid.TryParse(identifier, out var publicId))
        {
            return await query.FirstOrDefaultAsync(d => d.PublicId == publicId, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(d => d.Code == identifier, cancellationToken);
    }

    private bool IsSuperAdmin() =>
        _currentUserService.IsInRole(AppRoles.SuperAdmin)
        || _currentUserService.IsInRole("SuperAdmin")
        || _currentUserService.IsInRole("Admin");

    private List<long> GetAllowedBranchIds()
    {
        var allowed = _currentUserService.AccessibleBranchIds.ToList();
        if (_currentUserService.DefaultBranchId.HasValue && !allowed.Contains(_currentUserService.DefaultBranchId.Value))
        {
            allowed.Add(_currentUserService.DefaultBranchId.Value);
        }
        return allowed;
    }

    private void VerifyBranchAccess(long branchId)
    {
        if (IsSuperAdmin()) return;
        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException("You do not have authorization to access this operating branch.");
        }
    }

    private static DepartmentDto MapToDto(Department d) => new(
        d.Id,
        d.PublicId,
        d.CompanyId,
        d.BranchId,
        d.Branch?.Code ?? string.Empty,
        d.Branch?.Name ?? string.Empty,
        d.Code,
        d.Name,
        d.Description,
        d.ManagerEmployeeId,
        d.ManagerEmployee?.FullName,
        d.IsActive,
        d.CreatedAt,
        d.Employees?.Count ?? 0
    );
}
#endregion

#region Employee Service
public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    public EmployeeService(
        ApplicationDbContext context,
        IAuditService auditService,
        IPasswordHasherService passwordHasher,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<EmployeeDto>> GetEmployeesAsync(
        string? search,
        long? branchId,
        long? departmentId,
        string? status,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Branch)
            .Include(e => e.Department)
            .Include(e => e.UserAccount)
            .AsQueryable();

        if (!IsSuperAdmin())
        {
            var allowed = GetAllowedBranchIds();
            query = query.Where(e => allowed.Contains(e.BranchId));
        }

        if (branchId.HasValue)
        {
            VerifyBranchAccess(branchId.Value);
            query = query.Where(e => e.BranchId == branchId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Code.Contains(term)
                                  || e.FirstName.Contains(term)
                                  || e.LastName.Contains(term)
                                  || e.FullName.Contains(term)
                                  || e.Email.Contains(term)
                                  || e.PhoneNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => MapToDto(e))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<EmployeeDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployeeByIdentifierAsync(identifier, true, cancellationToken);
        if (employee == null) return null;

        VerifyBranchAccess(employee.BranchId);
        return MapToDto(employee);
    }

    public async Task<EmployeeProfileDto?> GetProfileByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Branch)
                .ThenInclude(b => b.Address)
            .Include(e => e.Department)
            .Include(e => e.Address)
            .Include(e => e.UserAccount)
                .ThenInclude(u => u!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(e => e.UserAccount)
                .ThenInclude(u => u!.BranchAccesses)
                    .ThenInclude(ba => ba.Branch)
            .AsQueryable();

        Employee? employee;
        if (long.TryParse(identifier, out var id))
        {
            employee = await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(identifier, out var publicId))
        {
            employee = await query.FirstOrDefaultAsync(e => e.PublicId == publicId, cancellationToken);
        }
        else
        {
            employee = await query.FirstOrDefaultAsync(e => e.Code == identifier, cancellationToken);
        }

        if (employee == null) return null;
        VerifyBranchAccess(employee.BranchId);

        var branchDto = new BranchDto(
            employee.Branch.Id,
            employee.Branch.PublicId,
            employee.Branch.CompanyId,
            employee.Branch.Code,
            employee.Branch.Name,
            employee.Branch.BranchType,
            employee.Branch.Phone,
            employee.Branch.Email,
            employee.Branch.AddressId,
            employee.Branch.IsActive,
            employee.Branch.CreatedAt,
            employee.Branch.Address?.FormattedAddress
        );

        DepartmentDto? deptDto = null;
        if (employee.Department != null)
        {
            deptDto = new DepartmentDto(
                employee.Department.Id,
                employee.Department.PublicId,
                employee.Department.CompanyId,
                employee.Department.BranchId,
                employee.Branch.Code,
                employee.Branch.Name,
                employee.Department.Code,
                employee.Department.Name,
                employee.Department.Description,
                employee.Department.ManagerEmployeeId,
                null,
                employee.Department.IsActive,
                employee.Department.CreatedAt,
                0
            );
        }

        EmployeeAccountDto accountDto;
        if (employee.UserAccount != null)
        {
            var u = employee.UserAccount;
            accountDto = new EmployeeAccountDto(
                true,
                u.Id,
                u.PublicId,
                u.Username,
                u.Email,
                u.IsActive,
                u.IsLockedOut,
                u.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList(),
                u.BranchAccesses.Where(ba => ba.Branch.IsActive).Select(ba => ba.Branch.Name).ToList(),
                u.LastLoginAt
            );
        }
        else
        {
            accountDto = new EmployeeAccountDto(false, null, null, null, null, false, false, [], [], null);
        }

        return new EmployeeProfileDto(
            employee.Id,
            employee.PublicId,
            employee.Code,
            employee.FirstName,
            employee.LastName,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.Gender,
            employee.JobTitle,
            employee.HireDate,
            employee.Status,
            employee.CreatedAt,
            branchDto,
            deptDto,
            employee.Address?.FormattedAddress,
            accountDto
        );
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        VerifyBranchAccess(request.BranchId);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            throw new ValidationException("Branch", "Branch not found or is currently inactive.");
        }

        if (request.DepartmentId.HasValue)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value, cancellationToken);
            if (dept == null || dept.BranchId != request.BranchId || !dept.IsActive)
            {
                throw new ValidationException("Department", "Selected department does not exist in this branch or is inactive.");
            }
        }

        var codeExists = await _context.Employees.AnyAsync(e => e.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException("Employee", "Code", request.Code);
        }

        var emailExists = await _context.Employees.AnyAsync(e => e.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            throw new ConflictException("Employee", "Email", request.Email);
        }

        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var entity = new Employee
        {
            BranchId = request.BranchId,
            DepartmentId = request.DepartmentId,
            Code = request.Code,
            FirstName = request.FirstName,
            LastName = request.LastName,
            FullName = fullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            JobTitle = request.JobTitle,
            HireDate = request.HireDate,
            Status = "Active",
            AddressId = request.AddressId,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Employees.AddAsync(entity, cancellationToken);
        await _auditService.RecordAsync("Create", "organization", "Employees", entity.Code, null, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        entity.Branch = branch;
        return MapToDto(entity);
    }

    public async Task<EmployeeDto> UpdateAsync(string identifier, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployeeByIdentifierAsync(identifier, false, cancellationToken);
        if (employee == null)
        {
            throw new NotFoundException("Employee", identifier);
        }

        VerifyBranchAccess(employee.BranchId);
        if (request.BranchId != employee.BranchId)
        {
            VerifyBranchAccess(request.BranchId);
            var newBranch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (newBranch == null || !newBranch.IsActive)
            {
                throw new ValidationException("Branch", "Target branch does not exist or is inactive.");
            }
        }

        if (request.DepartmentId.HasValue)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value, cancellationToken);
            if (dept == null || dept.BranchId != request.BranchId || !dept.IsActive)
            {
                throw new ValidationException("Department", "Selected department does not exist in the target branch or is inactive.");
            }
        }

        if (request.Email != employee.Email)
        {
            var emailExists = await _context.Employees.AnyAsync(e => e.Email == request.Email && e.Id != employee.Id, cancellationToken);
            if (emailExists)
            {
                throw new ConflictException("Employee", "Email", request.Email);
            }
        }

        var oldValues = new
        {
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.PhoneNumber,
            employee.JobTitle,
            employee.BranchId,
            employee.DepartmentId
        };

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.FullName = $"{request.FirstName} {request.LastName}".Trim();
        employee.Email = request.Email;
        employee.PhoneNumber = request.PhoneNumber;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.JobTitle = request.JobTitle;
        employee.HireDate = request.HireDate;
        employee.BranchId = request.BranchId;
        employee.DepartmentId = request.DepartmentId;
        employee.AddressId = request.AddressId;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUserService.Username;

        await _auditService.RecordAsync("Update", "organization", "Employees", employee.Id.ToString(), oldValues, request, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(employee);
    }

    public async Task UpdateStatusAsync(string identifier, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id.ToString() == identifier || e.PublicId.ToString() == identifier || e.Code == identifier, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", identifier);
        }

        VerifyBranchAccess(employee.BranchId);

        if (employee.Status == "Terminated" && request.Status != "Terminated")
        {
            throw new ConflictException("Employee", "Status", "Cannot reactivate or change status of a terminated employee.");
        }

        var oldStatus = employee.Status;
        employee.Status = request.Status;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUserService.Username;

        // Account security integration: deactivating/terminating employee disables linked account and revokes active sessions
        if (request.Status is "Inactive" or "Terminated" or "Resigned" && employee.UserAccount != null)
        {
            var user = employee.UserAccount;
            user.IsActive = false;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = _currentUserService.Username;

            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && !s.IsTerminated)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsTerminated = true;
            }

            await _auditService.RecordAsync(
                "AccountStatusChange", "identity", "UserAccounts", user.Id.ToString(),
                new { IsActive = true },
                new { IsActive = false, Reason = $"Linked employee transitioned to {request.Status}" },
                request.Reason,
                cancellationToken);
        }

        await _auditService.RecordAsync(
            "StatusChange", "organization", "Employees", employee.Id.ToString(),
            new { Status = oldStatus },
            new { Status = request.Status, request.Reason },
            request.Reason,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeAccountDto> GetAccountAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.UserAccount)
                .ThenInclude(u => u!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(e => e.UserAccount)
                .ThenInclude(u => u!.BranchAccesses)
                    .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(e => e.Id.ToString() == identifier || e.PublicId.ToString() == identifier || e.Code == identifier, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", identifier);
        }

        VerifyBranchAccess(employee.BranchId);

        if (employee.UserAccount == null)
        {
            return new EmployeeAccountDto(false, null, null, null, null, false, false, [], [], null);
        }

        var u = employee.UserAccount;
        return new EmployeeAccountDto(
            true,
            u.Id,
            u.PublicId,
            u.Username,
            u.Email,
            u.IsActive,
            u.IsLockedOut,
            u.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList(),
            u.BranchAccesses.Where(ba => ba.Branch.IsActive).Select(ba => ba.Branch.Name).ToList(),
            u.LastLoginAt
        );
    }

    public async Task<EmployeeAccountDto> CreateAccountAsync(string identifier, CreateEmployeeAccountRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id.ToString() == identifier || e.PublicId.ToString() == identifier || e.Code == identifier, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", identifier);
        }

        VerifyBranchAccess(employee.BranchId);

        if (employee.Status != "Active")
        {
            throw new ValidationException("Employee", "Cannot create a user account for a non-active employee.");
        }

        if (employee.UserAccount != null)
        {
            throw new ConflictException("UserAccount", "EmployeeId", employee.Id.ToString());
        }

        var usernameExists = await _context.UserAccounts.AnyAsync(u => u.Username == request.Username, cancellationToken);
        if (usernameExists)
        {
            throw new ConflictException("UserAccount", "Username", request.Username);
        }

        var emailExists = await _context.UserAccounts.AnyAsync(u => u.Email == employee.Email, cancellationToken);
        if (emailExists)
        {
            throw new ConflictException("UserAccount", "Email", employee.Email);
        }

        var roles = await _context.Roles
            .Where(r => request.RoleCodes.Contains(r.Code) && r.IsActive)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
        {
            throw new ValidationException("Role", "At least one valid system role must be assigned.");
        }

        var branchIds = request.BranchIds?.ToList() ?? new List<long>();
        if (!branchIds.Contains(employee.BranchId))
        {
            branchIds.Add(employee.BranchId);
        }

        var validBranches = await _context.Branches
            .Where(b => branchIds.Contains(b.Id) && b.IsActive)
            .ToListAsync(cancellationToken);

        var user = new UserAccount
        {
            EmployeeId = employee.Id,
            Username = request.Username,
            Email = employee.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = true,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = _currentUserService.Username
            });
        }

        var defaultBranchId = request.DefaultBranchId ?? employee.BranchId;
        foreach (var branch in validBranches)
        {
            user.BranchAccesses.Add(new UserBranchAccess
            {
                BranchId = branch.Id,
                IsDefault = branch.Id == defaultBranchId,
                GrantedBy = _currentUserService.Username,
                GrantedAt = DateTime.UtcNow
            });
        }

        await _context.UserAccounts.AddAsync(user, cancellationToken);
        await _auditService.RecordAsync(
            "AccountCreate", "identity", "UserAccounts", user.Username,
            null,
            new { user.Username, user.Email, user.EmployeeId, Roles = request.RoleCodes },
            null,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeAccountDto(
            true,
            user.Id,
            user.PublicId,
            user.Username,
            user.Email,
            user.IsActive,
            false,
            roles.Select(r => r.Code).ToList(),
            validBranches.Select(b => b.Name).ToList(),
            null
        );
    }

    public async Task UpdateAccountStatusAsync(string identifier, UpdateEmployeeAccountStatusRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id.ToString() == identifier || e.PublicId.ToString() == identifier || e.Code == identifier, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", identifier);
        }

        VerifyBranchAccess(employee.BranchId);

        if (employee.UserAccount == null)
        {
            throw new NotFoundException("UserAccount", "Employee has no linked account.");
        }

        var user = employee.UserAccount;
        var oldStatus = user.IsActive;
        user.IsActive = request.IsActive;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUserService.Username;

        if (!request.IsActive)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && !s.IsTerminated)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsTerminated = true;
            }
        }

        await _auditService.RecordAsync(
            "AccountStatusChange", "identity", "UserAccounts", user.Id.ToString(),
            new { IsActive = oldStatus },
            new { IsActive = request.IsActive, request.Reason },
            request.Reason,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeBranchAccessDto> GetBranchAccessAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Branch)
            .Include(e => e.UserAccount)
                .ThenInclude(u => u!.BranchAccesses)
                    .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(e => e.Id.ToString() == identifier || e.PublicId.ToString() == identifier || e.Code == identifier, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", identifier);
        }

        VerifyBranchAccess(employee.BranchId);

        var list = new List<EmployeeAssignedBranchDto>();

        if (employee.UserAccount?.BranchAccesses != null && employee.UserAccount.BranchAccesses.Count > 0)
        {
            foreach (var ba in employee.UserAccount.BranchAccesses.Where(b => b.Branch.IsActive))
            {
                list.Add(new EmployeeAssignedBranchDto(
                    ba.BranchId,
                    ba.Branch.PublicId,
                    ba.Branch.Code,
                    ba.Branch.Name,
                    ba.IsDefault
                ));
            }
        }
        else
        {
            list.Add(new EmployeeAssignedBranchDto(
                employee.Branch.Id,
                employee.Branch.PublicId,
                employee.Branch.Code,
                employee.Branch.Name,
                true
            ));
        }

        return new EmployeeBranchAccessDto(
            employee.Id,
            employee.PublicId,
            employee.FullName,
            employee.Branch.Id,
            employee.Branch.Name,
            list
        );
    }

    private async Task<Employee?> FindEmployeeByIdentifierAsync(string identifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Employees.AsNoTracking() : _context.Employees.AsQueryable();
        query = query.Include(e => e.Branch).Include(e => e.Department).Include(e => e.UserAccount);

        if (long.TryParse(identifier, out var id))
        {
            return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        if (Guid.TryParse(identifier, out var publicId))
        {
            return await query.FirstOrDefaultAsync(e => e.PublicId == publicId, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(e => e.Code == identifier, cancellationToken);
    }

    private bool IsSuperAdmin() =>
        _currentUserService.IsInRole(AppRoles.SuperAdmin)
        || _currentUserService.IsInRole("SuperAdmin")
        || _currentUserService.IsInRole("Admin");

    private List<long> GetAllowedBranchIds()
    {
        var allowed = _currentUserService.AccessibleBranchIds.ToList();
        if (_currentUserService.DefaultBranchId.HasValue && !allowed.Contains(_currentUserService.DefaultBranchId.Value))
        {
            allowed.Add(_currentUserService.DefaultBranchId.Value);
        }
        return allowed;
    }

    private void VerifyBranchAccess(long branchId)
    {
        if (IsSuperAdmin()) return;
        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException("You do not have authorization to access this operating branch.");
        }
    }

    private static EmployeeDto MapToDto(Employee e) => new(
        e.Id,
        e.PublicId,
        e.BranchId,
        e.Branch?.Code ?? string.Empty,
        e.Branch?.Name ?? string.Empty,
        e.DepartmentId,
        e.Department?.Code,
        e.Department?.Name,
        e.Code,
        e.FirstName,
        e.LastName,
        e.FullName,
        e.Email,
        e.PhoneNumber,
        e.DateOfBirth,
        e.Gender,
        e.JobTitle,
        e.HireDate,
        e.Status,
        e.AddressId,
        e.CreatedAt,
        e.UserAccount != null,
        e.UserAccount?.PublicId,
        e.UserAccount?.Username
    );
}
#endregion

#region Warehouse Service
public class WarehouseService : IWarehouseService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public WarehouseService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(
        string? search,
        long? branchId,
        string? type,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Warehouses
            .AsNoTracking()
            .Include(w => w.Branch)
            .Include(w => w.ManagerEmployee)
            .Include(w => w.Zones)
            .AsQueryable();

        if (!IsSuperAdmin())
        {
            var allowed = GetAllowedBranchIds();
            query = query.Where(w => allowed.Contains(w.BranchId));
        }

        if (branchId.HasValue)
        {
            VerifyBranchAccess(branchId.Value);
            query = query.Where(w => w.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(w => w.Type == type);
        }

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w => w.Name.Contains(term) || w.Code.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(w => w.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => MapToDto(w))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<WarehouseDetailDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var query = _context.Warehouses
            .AsNoTracking()
            .Include(w => w.Branch)
            .Include(w => w.ManagerEmployee)
            .Include(w => w.Zones)
            .AsQueryable();

        Warehouse? warehouse;
        if (long.TryParse(identifier, out var id))
        {
            warehouse = await query.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(identifier, out var publicId))
        {
            warehouse = await query.FirstOrDefaultAsync(w => w.PublicId == publicId, cancellationToken);
        }
        else
        {
            warehouse = await query.FirstOrDefaultAsync(w => w.Code == identifier, cancellationToken);
        }

        if (warehouse == null) return null;
        VerifyBranchAccess(warehouse.BranchId);

        var zones = warehouse.Zones
            .OrderBy(z => z.Code)
            .Select(z => new WarehouseZoneDto(
                z.Id,
                z.PublicId,
                z.Code,
                z.Name,
                z.ZoneType,
                z.TemperatureType,
                z.IsActive
            ))
            .ToList();

        return new WarehouseDetailDto(MapToDto(warehouse), zones);
    }

    private bool IsSuperAdmin() =>
        _currentUserService.IsInRole(AppRoles.SuperAdmin)
        || _currentUserService.IsInRole("SuperAdmin")
        || _currentUserService.IsInRole("Admin");

    private List<long> GetAllowedBranchIds()
    {
        var allowed = _currentUserService.AccessibleBranchIds.ToList();
        if (_currentUserService.DefaultBranchId.HasValue && !allowed.Contains(_currentUserService.DefaultBranchId.Value))
        {
            allowed.Add(_currentUserService.DefaultBranchId.Value);
        }
        return allowed;
    }

    private void VerifyBranchAccess(long branchId)
    {
        if (IsSuperAdmin()) return;
        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException("You do not have authorization to access this operating branch.");
        }
    }

    private static WarehouseDto MapToDto(Warehouse w) => new(
        w.Id,
        w.PublicId,
        w.BranchId,
        w.Branch?.Code ?? string.Empty,
        w.Branch?.Name ?? string.Empty,
        w.Code,
        w.Name,
        w.Type,
        w.TotalAreaSqM,
        w.AddressId,
        w.ManagerEmployeeId,
        w.ManagerEmployee?.FullName,
        w.IsActive,
        w.CreatedAt,
        w.Zones?.Count ?? 0
    );
}
#endregion
