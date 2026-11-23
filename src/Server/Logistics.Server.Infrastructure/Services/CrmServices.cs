using System.Text.Json;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

#region Customer Service
public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CustomerService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private IQueryable<Customer> ApplyBranchFilter(IQueryable<Customer> query)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return query;
        }

        var accessibleBranchIds = _currentUserService.AccessibleBranchIds;
        var defaultBranchId = _currentUserService.DefaultBranchId;

        return query.Where(c => accessibleBranchIds.Contains(c.BranchId) || (defaultBranchId.HasValue && c.BranchId == defaultBranchId.Value));
    }

    private async Task<Customer> ResolveCustomerEntityAsync(string identifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Customers.AsNoTracking() : _context.Customers.AsQueryable();
        query = query
            .Include(c => c.Branch)
            .Include(c => c.AccountManagerEmployee);

        Customer? customer;
        if (long.TryParse(identifier, out var id))
        {
            customer = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(identifier, out var publicId))
        {
            customer = await query.FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);
        }
        else
        {
            customer = await query.FirstOrDefaultAsync(c => c.CustomerCode == identifier, cancellationToken);
        }

        if (customer == null)
        {
            throw new NotFoundException("Customer", identifier);
        }

        EnsureBranchAccess(customer.BranchId);
        return customer;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(
        string? search,
        string? customerType,
        bool? isActive,
        long? branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers
            .AsNoTracking()
            .Include(c => c.Branch)
            .Include(c => c.AccountManagerEmployee)
            .AsQueryable();

        query = ApplyBranchFilter(query);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerCode.ToLower().Contains(term) ||
                c.Name.ToLower().Contains(term) ||
                (c.TradeName != null && c.TradeName.ToLower().Contains(term)) ||
                c.Email.ToLower().Contains(term) ||
                c.PhoneNumber.ToLower().Contains(term) ||
                (c.TaxCode != null && c.TaxCode.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(customerType))
        {
            query = query.Where(c => c.CustomerType == customerType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (branchId.HasValue && branchId.Value > 0)
        {
            EnsureBranchAccess(branchId.Value);
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= dateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "code" or "customercode" => request.SortDescending ? query.OrderByDescending(c => c.CustomerCode) : query.OrderBy(c => c.CustomerCode),
            "name" => request.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "type" or "customertype" => request.SortDescending ? query.OrderByDescending(c => c.CustomerType) : query.OrderBy(c => c.CustomerType),
            "status" or "isactive" => request.SortDescending ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
            _ => request.SortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CustomerDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerEntityAsync(identifier, true, cancellationToken);
        return MapToDto(customer);
    }

    public async Task<CustomerDetailDto?> GetDetailByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerEntityAsync(identifier, true, cancellationToken);

        var contacts = await _context.CustomerContacts
            .AsNoTracking()
            .Where(cc => cc.CustomerId == customer.Id)
            .OrderByDescending(cc => cc.IsPrimary)
            .ThenBy(cc => cc.ContactName)
            .Select(cc => CustomerContactService.MapToDto(cc))
            .ToListAsync(cancellationToken);

        var addresses = await _context.CustomerAddresses
            .AsNoTracking()
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Country)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Province)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.District)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Ward)
            .Where(ca => ca.CustomerId == customer.Id)
            .OrderByDescending(ca => ca.IsDefault)
            .ThenBy(ca => ca.AddressType)
            .Select(ca => CustomerAddressService.MapToDto(ca))
            .ToListAsync(cancellationToken);

        var contracts = await _context.CustomerContracts
            .AsNoTracking()
            .Where(cc => cc.CustomerId == customer.Id)
            .OrderByDescending(cc => cc.StartDate)
            .Select(cc => CustomerContractService.MapToDto(cc, customer.CustomerCode))
            .ToListAsync(cancellationToken);

        var pricingRules = await _context.CustomerPricingRules
            .AsNoTracking()
            .Include(pr => pr.CustomerContract)
            .Include(pr => pr.ShippingRateCard)
            .Where(pr => pr.CustomerId == customer.Id)
            .OrderBy(pr => pr.Priority)
            .ThenByDescending(pr => pr.EffectiveFrom)
            .Select(pr => CustomerPricingRuleService.MapToDto(pr, customer.CustomerCode))
            .ToListAsync(cancellationToken);

        return new CustomerDetailDto(
            MapToDto(customer),
            contacts,
            addresses,
            contracts,
            pricingRules
        );
    }

    public async Task<CustomerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var query = ApplyBranchFilter(_context.Customers.AsNoTracking());

        var total = await query.CountAsync(cancellationToken);
        var active = await query.CountAsync(c => c.IsActive, cancellationToken);
        var inactive = total - active;
        var individual = await query.CountAsync(c => c.CustomerType == "Individual", cancellationToken);
        var corporate = await query.CountAsync(c => c.CustomerType == "Corporate", cancellationToken);

        var withActiveContracts = await query
            .Where(c => c.Contracts.Any(ct => ct.Status == "Active"))
            .CountAsync(cancellationToken);

        var isGlobal = _currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin");

        return new CustomerOverviewDto(
            total,
            active,
            inactive,
            individual,
            corporate,
            withActiveContracts,
            isGlobal ? "Global" : "BranchScoped"
        );
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        long targetBranchId;
        if (request.BranchId.HasValue && request.BranchId.Value > 0)
        {
            EnsureBranchAccess(request.BranchId.Value);
            targetBranchId = request.BranchId.Value;
        }
        else if (_currentUserService.DefaultBranchId.HasValue)
        {
            targetBranchId = _currentUserService.DefaultBranchId.Value;
        }
        else if (_currentUserService.AccessibleBranchIds.Count > 0)
        {
            targetBranchId = _currentUserService.AccessibleBranchIds[0];
        }
        else
        {
            var firstBranch = await _context.Branches.OrderBy(b => b.Id).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (firstBranch == 0)
            {
                throw new ConflictException("No operating branch found in system to register customer.");
            }
            targetBranchId = firstBranch;
        }

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == targetBranchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            throw new ConflictException($"Branch id '{targetBranchId}' is either inactive or does not exist.");
        }

        var codeExists = await _context.Customers.AnyAsync(c => c.CustomerCode == request.CustomerCode, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException("Customer", "CustomerCode", request.CustomerCode);
        }

        if (request.AccountManagerEmployeeId.HasValue)
        {
            var empExists = await _context.Employees.AnyAsync(e => e.Id == request.AccountManagerEmployeeId.Value, cancellationToken);
            if (!empExists)
            {
                throw new NotFoundException("Employee", request.AccountManagerEmployeeId.Value);
            }
        }

        var customer = new Customer
        {
            BranchId = targetBranchId,
            CustomerCode = request.CustomerCode,
            CustomerType = request.CustomerType,
            Name = request.Name,
            TradeName = request.TradeName,
            TaxCode = request.TaxCode,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreditLimit = request.CreditLimit,
            CreditTermDays = request.CreditTermDays,
            PaymentType = request.PaymentType,
            AccountManagerEmployeeId = request.AccountManagerEmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username
        };

        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Create",
            schemaName: "crm",
            tableName: "Customers",
            primaryKey: customer.Id.ToString(),
            oldValues: null,
            newValues: MapToDto(customer),
            notes: $"Created customer {customer.CustomerCode} under branch {branch.Code}",
            cancellationToken: cancellationToken);

        return await GetByIdOrPublicIdAsync(customer.Id.ToString(), cancellationToken) ?? MapToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(string identifier, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerEntityAsync(identifier, false, cancellationToken);

        if (request.AccountManagerEmployeeId.HasValue)
        {
            var empExists = await _context.Employees.AnyAsync(e => e.Id == request.AccountManagerEmployeeId.Value, cancellationToken);
            if (!empExists)
            {
                throw new NotFoundException("Employee", request.AccountManagerEmployeeId.Value);
            }
        }

        var oldValues = new
        {
            customer.CustomerType,
            customer.Name,
            customer.TradeName,
            customer.TaxCode,
            customer.Email,
            customer.PhoneNumber,
            customer.CreditLimit,
            customer.CreditTermDays,
            customer.PaymentType,
            customer.AccountManagerEmployeeId
        };

        customer.CustomerType = request.CustomerType;
        customer.Name = request.Name;
        customer.TradeName = request.TradeName;
        customer.TaxCode = request.TaxCode;
        customer.Email = request.Email;
        customer.PhoneNumber = request.PhoneNumber;
        customer.CreditLimit = request.CreditLimit;
        customer.CreditTermDays = request.CreditTermDays;
        customer.PaymentType = request.PaymentType;
        customer.AccountManagerEmployeeId = request.AccountManagerEmployeeId;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Update",
            schemaName: "crm",
            tableName: "Customers",
            primaryKey: customer.Id.ToString(),
            oldValues: oldValues,
            newValues: MapToDto(customer),
            notes: $"Updated customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return await GetByIdOrPublicIdAsync(customer.Id.ToString(), cancellationToken) ?? MapToDto(customer);
    }

    public async Task UpdateStatusAsync(string identifier, UpdateCustomerStatusRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerEntityAsync(identifier, false, cancellationToken);

        if (customer.IsActive == request.IsActive)
        {
            return;
        }

        var oldStatus = customer.IsActive;
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: request.IsActive ? "Activate" : "Deactivate",
            schemaName: "crm",
            tableName: "Customers",
            primaryKey: customer.Id.ToString(),
            oldValues: new { IsActive = oldStatus },
            newValues: new { IsActive = customer.IsActive, request.Reason },
            notes: $"Status changed to {(request.IsActive ? "Active" : "Inactive")}: {request.Reason}",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerEntityAsync(identifier, false, cancellationToken);

        // Check if customer has orders or shipments
        var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == customer.Id, cancellationToken);
        var hasShipments = await _context.Shipments.AnyAsync(s => s.CustomerId == customer.Id, cancellationToken);
        var hasInvoices = await _context.Invoices.AnyAsync(i => i.CustomerId == customer.Id, cancellationToken);

        if (hasOrders || hasShipments || hasInvoices)
        {
            throw new ConflictException("Cannot delete customer with existing business history. Deactivate the customer instead.");
        }

        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Deactivate",
            schemaName: "crm",
            tableName: "Customers",
            primaryKey: customer.Id.ToString(),
            oldValues: new { IsActive = true },
            newValues: new { IsActive = false },
            notes: "Deactivated customer via delete request",
            cancellationToken: cancellationToken);
    }

    public static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto(
            c.Id,
            c.PublicId,
            c.BranchId,
            c.Branch?.Code,
            c.Branch?.Name,
            c.CustomerCode,
            c.CustomerType,
            c.Name,
            c.TradeName,
            c.TaxCode,
            c.Email,
            c.PhoneNumber,
            c.CreditLimit,
            c.CreditTermDays,
            c.PaymentType,
            c.AccountManagerEmployeeId,
            c.AccountManagerEmployee?.FullName,
            c.IsActive,
            c.CreatedAt,
            c.UpdatedAt
        );
    }
}
#endregion

#region Customer Contact Service
public class CustomerContactService : ICustomerContactService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CustomerContactService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Customer> ResolveCustomerAsync(string customerIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = _context.Customers;
        Customer? customer;
        if (long.TryParse(customerIdentifier, out var id))
        {
            customer = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(customerIdentifier, out var publicId))
        {
            customer = await query.FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);
        }
        else
        {
            customer = await query.FirstOrDefaultAsync(c => c.CustomerCode == customerIdentifier, cancellationToken);
        }

        if (customer == null)
        {
            throw new NotFoundException("Customer", customerIdentifier);
        }

        EnsureBranchAccess(customer.BranchId);
        return customer;
    }

    private async Task<CustomerContact> ResolveContactAsync(long customerId, string contactIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<CustomerContact> query = _context.CustomerContacts.Where(cc => cc.CustomerId == customerId);
        CustomerContact? contact;
        if (long.TryParse(contactIdentifier, out var id))
        {
            contact = await query.FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(contactIdentifier, out var publicId))
        {
            contact = await query.FirstOrDefaultAsync(cc => cc.PublicId == publicId, cancellationToken);
        }
        else
        {
            contact = await query.FirstOrDefaultAsync(cc => cc.ContactName == contactIdentifier, cancellationToken);
        }

        if (contact == null)
        {
            throw new NotFoundException("CustomerContact", contactIdentifier);
        }

        return contact;
    }

    public async Task<IReadOnlyList<CustomerContactDto>> GetContactsAsync(
        string customerIdentifier,
        string? search,
        bool? isPrimary,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        var query = _context.CustomerContacts
            .AsNoTracking()
            .Where(cc => cc.CustomerId == customer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(cc =>
                cc.ContactName.ToLower().Contains(term) ||
                (cc.JobTitle != null && cc.JobTitle.ToLower().Contains(term)) ||
                cc.PhoneNumber.ToLower().Contains(term) ||
                (cc.Email != null && cc.Email.ToLower().Contains(term)));
        }

        if (isPrimary.HasValue)
        {
            query = query.Where(cc => cc.IsPrimary == isPrimary.Value);
        }

        return await query
            .OrderByDescending(cc => cc.IsPrimary)
            .ThenBy(cc => cc.ContactName)
            .Select(cc => MapToDto(cc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerContactDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string contactIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contact = await ResolveContactAsync(customer.Id, contactIdentifier, cancellationToken);
        return MapToDto(contact);
    }

    public async Task<CustomerContactDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        if (request.IsPrimary)
        {
            var existingPrimaries = await _context.CustomerContacts
                .Where(cc => cc.CustomerId == customer.Id && cc.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var ep in existingPrimaries)
            {
                ep.IsPrimary = false;
                ep.UpdatedAt = DateTime.UtcNow;
                ep.UpdatedBy = _currentUserService.Username;
            }
        }

        var contact = new CustomerContact
        {
            CustomerId = customer.Id,
            ContactName = request.ContactName,
            JobTitle = request.JobTitle,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            IsPrimary = request.IsPrimary,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username
        };

        await _context.CustomerContacts.AddAsync(contact, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Create",
            schemaName: "crm",
            tableName: "CustomerContacts",
            primaryKey: contact.Id.ToString(),
            oldValues: null,
            newValues: MapToDto(contact),
            notes: $"Added contact {contact.ContactName} to customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return MapToDto(contact);
    }

    public async Task<CustomerContactDto> UpdateAsync(
        string customerIdentifier,
        string contactIdentifier,
        UpdateCustomerContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contact = await ResolveContactAsync(customer.Id, contactIdentifier, cancellationToken);

        if (request.IsPrimary && !contact.IsPrimary)
        {
            var otherPrimaries = await _context.CustomerContacts
                .Where(cc => cc.CustomerId == customer.Id && cc.IsPrimary && cc.Id != contact.Id)
                .ToListAsync(cancellationToken);

            foreach (var op in otherPrimaries)
            {
                op.IsPrimary = false;
                op.UpdatedAt = DateTime.UtcNow;
                op.UpdatedBy = _currentUserService.Username;
            }
        }

        var oldValues = new
        {
            contact.ContactName,
            contact.JobTitle,
            contact.PhoneNumber,
            contact.Email,
            contact.IsPrimary,
            contact.Notes
        };

        contact.ContactName = request.ContactName;
        contact.JobTitle = request.JobTitle;
        contact.PhoneNumber = request.PhoneNumber;
        contact.Email = request.Email;
        contact.IsPrimary = request.IsPrimary;
        contact.Notes = request.Notes;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Update",
            schemaName: "crm",
            tableName: "CustomerContacts",
            primaryKey: contact.Id.ToString(),
            oldValues: oldValues,
            newValues: MapToDto(contact),
            notes: $"Updated contact {contact.ContactName} for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return MapToDto(contact);
    }

    public async Task SetPrimaryAsync(
        string customerIdentifier,
        string contactIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contact = await ResolveContactAsync(customer.Id, contactIdentifier, cancellationToken);

        if (contact.IsPrimary)
        {
            return;
        }

        var otherPrimaries = await _context.CustomerContacts
            .Where(cc => cc.CustomerId == customer.Id && cc.IsPrimary && cc.Id != contact.Id)
            .ToListAsync(cancellationToken);

        foreach (var op in otherPrimaries)
        {
            op.IsPrimary = false;
            op.UpdatedAt = DateTime.UtcNow;
            op.UpdatedBy = _currentUserService.Username;
        }

        contact.IsPrimary = true;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "SetPrimary",
            schemaName: "crm",
            tableName: "CustomerContacts",
            primaryKey: contact.Id.ToString(),
            oldValues: new { IsPrimary = false },
            newValues: new { IsPrimary = true },
            notes: $"Set contact {contact.ContactName} as primary for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);
    }

    public async Task UpdateStatusAsync(
        string customerIdentifier,
        string contactIdentifier,
        UpdateContactStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contact = await ResolveContactAsync(customer.Id, contactIdentifier, cancellationToken);

        var currentNotes = contact.Notes ?? string.Empty;
        if (!request.IsActive)
        {
            if (!currentNotes.StartsWith("[Inactive]"))
            {
                contact.Notes = string.IsNullOrWhiteSpace(request.Reason)
                    ? $"[Inactive] {currentNotes}".Trim()
                    : $"[Inactive: {request.Reason}] {currentNotes}".Trim();
            }
        }
        else
        {
            if (currentNotes.StartsWith("[Inactive"))
            {
                var idx = currentNotes.IndexOf(']');
                contact.Notes = idx >= 0 ? currentNotes[(idx + 1)..].Trim() : currentNotes;
            }
        }

        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: request.IsActive ? "Activate" : "Deactivate",
            schemaName: "crm",
            tableName: "CustomerContacts",
            primaryKey: contact.Id.ToString(),
            oldValues: new { Notes = currentNotes },
            newValues: new { Notes = contact.Notes },
            notes: $"Contact status updated to {(request.IsActive ? "Active" : "Inactive")}",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        string customerIdentifier,
        string contactIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contact = await ResolveContactAsync(customer.Id, contactIdentifier, cancellationToken);

        _context.CustomerContacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Delete",
            schemaName: "crm",
            tableName: "CustomerContacts",
            primaryKey: contact.Id.ToString(),
            oldValues: MapToDto(contact),
            newValues: null,
            notes: $"Removed contact {contact.ContactName} from customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);
    }

    public static CustomerContactDto MapToDto(CustomerContact cc)
    {
        return new CustomerContactDto(
            cc.Id,
            cc.PublicId,
            cc.CustomerId,
            cc.ContactName,
            cc.JobTitle,
            cc.PhoneNumber,
            cc.Email,
            cc.IsPrimary,
            cc.Notes,
            cc.CreatedAt,
            cc.UpdatedAt
        );
    }
}
#endregion

#region Customer Address Service
public class CustomerAddressService : ICustomerAddressService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CustomerAddressService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Customer> ResolveCustomerAsync(string customerIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = _context.Customers;
        Customer? customer;
        if (long.TryParse(customerIdentifier, out var id))
        {
            customer = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(customerIdentifier, out var publicId))
        {
            customer = await query.FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);
        }
        else
        {
            customer = await query.FirstOrDefaultAsync(c => c.CustomerCode == customerIdentifier, cancellationToken);
        }

        if (customer == null)
        {
            throw new NotFoundException("Customer", customerIdentifier);
        }

        EnsureBranchAccess(customer.BranchId);
        return customer;
    }

    private async Task<CustomerAddress> ResolveCustomerAddressAsync(long customerId, string addressIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<CustomerAddress> query = _context.CustomerAddresses
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Country)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Province)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.District)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Ward)
            .Where(ca => ca.CustomerId == customerId);

        CustomerAddress? customerAddress;
        if (long.TryParse(addressIdentifier, out var id))
        {
            customerAddress = await query.FirstOrDefaultAsync(ca => ca.Id == id || ca.AddressId == id, cancellationToken);
        }
        else if (Guid.TryParse(addressIdentifier, out var publicId))
        {
            customerAddress = await query.FirstOrDefaultAsync(ca => ca.PublicId == publicId || ca.Address.PublicId == publicId, cancellationToken);
        }
        else
        {
            customerAddress = await query.FirstOrDefaultAsync(ca => ca.AddressLabel == addressIdentifier, cancellationToken);
        }

        if (customerAddress == null)
        {
            throw new NotFoundException("CustomerAddress", addressIdentifier);
        }

        return customerAddress;
    }

    private async Task<long> ResolveCountryIdAsync(long? countryId, CancellationToken cancellationToken)
    {
        if (countryId.HasValue && countryId.Value > 0)
        {
            var exists = await _context.Countries.AnyAsync(c => c.Id == countryId.Value, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("Country", countryId.Value);
            }
            return countryId.Value;
        }

        var defaultCountry = await _context.Countries.FirstOrDefaultAsync(cancellationToken);
        if (defaultCountry != null)
        {
            return defaultCountry.Id;
        }

        defaultCountry = new Country
        {
            Code = "VN",
            Alpha3Code = "VNM",
            Name = "Vietnam",
            PhonePrefix = "+84",
            IsActive = true
        };
        await _context.Countries.AddAsync(defaultCountry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return defaultCountry.Id;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> GetAddressesAsync(
        string customerIdentifier,
        string? addressType,
        bool? isDefault,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        var query = _context.CustomerAddresses
            .AsNoTracking()
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Country)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Province)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.District)
            .Include(ca => ca.Address)
                .ThenInclude(a => a.Ward)
            .Where(ca => ca.CustomerId == customer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(addressType))
        {
            query = query.Where(ca => ca.AddressType == addressType);
        }

        if (isDefault.HasValue)
        {
            query = query.Where(ca => ca.IsDefault == isDefault.Value);
        }

        return await query
            .OrderByDescending(ca => ca.IsDefault)
            .ThenBy(ca => ca.AddressType)
            .Select(ca => MapToDto(ca))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAddressDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string addressIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var customerAddress = await ResolveCustomerAddressAsync(customer.Id, addressIdentifier, cancellationToken);
        return MapToDto(customerAddress);
    }

    public async Task<CustomerAddressDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        var countryId = await ResolveCountryIdAsync(request.CountryId, cancellationToken);

        // Validate province, district, ward if provided
        string? provName = null, distName = null, wardName = null;
        if (request.ProvinceId.HasValue)
        {
            var p = await _context.Provinces.FirstOrDefaultAsync(x => x.Id == request.ProvinceId.Value, cancellationToken);
            if (p == null) throw new NotFoundException("Province", request.ProvinceId.Value);
            provName = p.Name;
        }

        if (request.DistrictId.HasValue)
        {
            var d = await _context.Districts.FirstOrDefaultAsync(x => x.Id == request.DistrictId.Value, cancellationToken);
            if (d == null) throw new NotFoundException("District", request.DistrictId.Value);
            distName = d.Name;
        }

        if (request.WardId.HasValue)
        {
            var w = await _context.Wards.FirstOrDefaultAsync(x => x.Id == request.WardId.Value, cancellationToken);
            if (w == null) throw new NotFoundException("Ward", request.WardId.Value);
            wardName = w.Name;
        }

        var formattedAddress = !string.IsNullOrWhiteSpace(request.FormattedAddress)
            ? request.FormattedAddress
            : string.Join(", ", new[]
              {
                  request.AddressLine1,
                  request.AddressLine2,
                  wardName,
                  distName,
                  provName
              }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrWhiteSpace(formattedAddress))
        {
            formattedAddress = request.AddressLine1;
        }

        // Handle single default rule per address type
        if (request.IsDefault)
        {
            var existingDefaults = await _context.CustomerAddresses
                .Where(ca => ca.CustomerId == customer.Id && ca.AddressType == request.AddressType && ca.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var ed in existingDefaults)
            {
                ed.IsDefault = false;
                ed.UpdatedAt = DateTime.UtcNow;
                ed.UpdatedBy = _currentUserService.Username;
            }
        }

        var address = new Address
        {
            ContactName = request.ContactName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            CountryId = countryId,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardId = request.WardId,
            PostalCode = request.PostalCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            FormattedAddress = formattedAddress,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username
        };

        await _context.Addresses.AddAsync(address, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var customerAddress = new CustomerAddress
        {
            CustomerId = customer.Id,
            AddressId = address.Id,
            AddressType = request.AddressType,
            IsDefault = request.IsDefault,
            AddressLabel = request.AddressLabel,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username
        };

        await _context.CustomerAddresses.AddAsync(customerAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Create",
            schemaName: "crm",
            tableName: "CustomerAddresses",
            primaryKey: customerAddress.Id.ToString(),
            oldValues: null,
            newValues: MapToDto(customerAddress),
            notes: $"Added {customerAddress.AddressType} address to customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return await GetByIdOrPublicIdAsync(customer.Id.ToString(), customerAddress.Id.ToString(), cancellationToken) ?? MapToDto(customerAddress);
    }

    public async Task<CustomerAddressDto> UpdateAsync(
        string customerIdentifier,
        string addressIdentifier,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var customerAddress = await ResolveCustomerAddressAsync(customer.Id, addressIdentifier, cancellationToken);

        var address = await _context.Addresses.FirstAsync(a => a.Id == customerAddress.AddressId, cancellationToken);

        if (request.IsDefault && !customerAddress.IsDefault)
        {
            var otherDefaults = await _context.CustomerAddresses
                .Where(ca => ca.CustomerId == customer.Id && ca.AddressType == request.AddressType && ca.IsDefault && ca.Id != customerAddress.Id)
                .ToListAsync(cancellationToken);

            foreach (var od in otherDefaults)
            {
                od.IsDefault = false;
                od.UpdatedAt = DateTime.UtcNow;
                od.UpdatedBy = _currentUserService.Username;
            }
        }

        var countryId = await ResolveCountryIdAsync(request.CountryId, cancellationToken);

        string? provName = null, distName = null, wardName = null;
        if (request.ProvinceId.HasValue)
        {
            var p = await _context.Provinces.FirstOrDefaultAsync(x => x.Id == request.ProvinceId.Value, cancellationToken);
            if (p == null) throw new NotFoundException("Province", request.ProvinceId.Value);
            provName = p.Name;
        }

        if (request.DistrictId.HasValue)
        {
            var d = await _context.Districts.FirstOrDefaultAsync(x => x.Id == request.DistrictId.Value, cancellationToken);
            if (d == null) throw new NotFoundException("District", request.DistrictId.Value);
            distName = d.Name;
        }

        if (request.WardId.HasValue)
        {
            var w = await _context.Wards.FirstOrDefaultAsync(x => x.Id == request.WardId.Value, cancellationToken);
            if (w == null) throw new NotFoundException("Ward", request.WardId.Value);
            wardName = w.Name;
        }

        var formattedAddress = !string.IsNullOrWhiteSpace(request.FormattedAddress)
            ? request.FormattedAddress
            : string.Join(", ", new[]
              {
                  request.AddressLine1,
                  request.AddressLine2,
                  wardName,
                  distName,
                  provName
              }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrWhiteSpace(formattedAddress))
        {
            formattedAddress = request.AddressLine1;
        }

        customerAddress.AddressType = request.AddressType;
        customerAddress.IsDefault = request.IsDefault;
        customerAddress.AddressLabel = request.AddressLabel;
        customerAddress.UpdatedAt = DateTime.UtcNow;
        customerAddress.UpdatedBy = _currentUserService.Username;

        address.ContactName = request.ContactName;
        address.PhoneNumber = request.PhoneNumber;
        address.Email = request.Email;
        address.AddressLine1 = request.AddressLine1;
        address.AddressLine2 = request.AddressLine2;
        address.CountryId = countryId;
        address.ProvinceId = request.ProvinceId;
        address.DistrictId = request.DistrictId;
        address.WardId = request.WardId;
        address.PostalCode = request.PostalCode;
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;
        address.FormattedAddress = formattedAddress;
        address.UpdatedAt = DateTime.UtcNow;
        address.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Update",
            schemaName: "crm",
            tableName: "CustomerAddresses",
            primaryKey: customerAddress.Id.ToString(),
            oldValues: null,
            newValues: MapToDto(customerAddress),
            notes: $"Updated address for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return await GetByIdOrPublicIdAsync(customer.Id.ToString(), customerAddress.Id.ToString(), cancellationToken) ?? MapToDto(customerAddress);
    }

    public async Task SetDefaultAsync(
        string customerIdentifier,
        string addressIdentifier,
        SetDefaultAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var customerAddress = await ResolveCustomerAddressAsync(customer.Id, addressIdentifier, cancellationToken);

        if (request.IsDefault && !customerAddress.IsDefault)
        {
            var otherDefaults = await _context.CustomerAddresses
                .Where(ca => ca.CustomerId == customer.Id && ca.AddressType == customerAddress.AddressType && ca.IsDefault && ca.Id != customerAddress.Id)
                .ToListAsync(cancellationToken);

            foreach (var od in otherDefaults)
            {
                od.IsDefault = false;
                od.UpdatedAt = DateTime.UtcNow;
                od.UpdatedBy = _currentUserService.Username;
            }
        }

        customerAddress.IsDefault = request.IsDefault;
        customerAddress.UpdatedAt = DateTime.UtcNow;
        customerAddress.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "SetDefault",
            schemaName: "crm",
            tableName: "CustomerAddresses",
            primaryKey: customerAddress.Id.ToString(),
            oldValues: new { IsDefault = !request.IsDefault },
            newValues: new { IsDefault = request.IsDefault },
            notes: $"Set default flag to {request.IsDefault} for customer {customer.CustomerCode} address type {customerAddress.AddressType}",
            cancellationToken: cancellationToken);
    }

    public async Task UpdateStatusAsync(
        string customerIdentifier,
        string addressIdentifier,
        UpdateCustomerAddressStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var customerAddress = await ResolveCustomerAddressAsync(customer.Id, addressIdentifier, cancellationToken);

        var currentLabel = customerAddress.AddressLabel ?? string.Empty;
        if (!request.IsActive)
        {
            if (!currentLabel.StartsWith("[Inactive]"))
            {
                customerAddress.AddressLabel = $"[Inactive] {currentLabel}".Trim();
            }
        }
        else
        {
            if (currentLabel.StartsWith("[Inactive]"))
            {
                customerAddress.AddressLabel = currentLabel.Replace("[Inactive]", string.Empty).Trim();
            }
        }

        customerAddress.UpdatedAt = DateTime.UtcNow;
        customerAddress.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: request.IsActive ? "Activate" : "Deactivate",
            schemaName: "crm",
            tableName: "CustomerAddresses",
            primaryKey: customerAddress.Id.ToString(),
            oldValues: new { AddressLabel = currentLabel },
            newValues: new { AddressLabel = customerAddress.AddressLabel },
            notes: $"Customer address status updated to {(request.IsActive ? "Active" : "Inactive")}",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        string customerIdentifier,
        string addressIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var customerAddress = await ResolveCustomerAddressAsync(customer.Id, addressIdentifier, cancellationToken);

        _context.CustomerAddresses.Remove(customerAddress);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Delete",
            schemaName: "crm",
            tableName: "CustomerAddresses",
            primaryKey: customerAddress.Id.ToString(),
            oldValues: MapToDto(customerAddress),
            newValues: null,
            notes: $"Removed address link from customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);
    }

    public static CustomerAddressDto MapToDto(CustomerAddress ca)
    {
        var a = ca.Address;
        return new CustomerAddressDto(
            ca.Id,
            ca.PublicId,
            ca.CustomerId,
            ca.AddressId,
            a?.PublicId ?? Guid.Empty,
            ca.AddressType,
            ca.IsDefault,
            ca.AddressLabel,
            a?.ContactName ?? string.Empty,
            a?.PhoneNumber ?? string.Empty,
            a?.Email,
            a?.AddressLine1 ?? string.Empty,
            a?.AddressLine2,
            a?.CountryId ?? 0,
            a?.Country?.Name,
            a?.ProvinceId,
            a?.Province?.Name,
            a?.DistrictId,
            a?.District?.Name,
            a?.WardId,
            a?.Ward?.Name,
            a?.PostalCode,
            a?.Latitude,
            a?.Longitude,
            a?.FormattedAddress ?? string.Empty,
            ca.CreatedAt,
            ca.UpdatedAt
        );
    }
}
#endregion

#region Customer Contract Service
public class CustomerContractService : ICustomerContractService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CustomerContractService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Customer> ResolveCustomerAsync(string customerIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = _context.Customers;
        Customer? customer;
        if (long.TryParse(customerIdentifier, out var id))
        {
            customer = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(customerIdentifier, out var publicId))
        {
            customer = await query.FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);
        }
        else
        {
            customer = await query.FirstOrDefaultAsync(c => c.CustomerCode == customerIdentifier, cancellationToken);
        }

        if (customer == null)
        {
            throw new NotFoundException("Customer", customerIdentifier);
        }

        EnsureBranchAccess(customer.BranchId);
        return customer;
    }

    private async Task<CustomerContract> ResolveContractAsync(long customerId, string contractIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<CustomerContract> query = _context.CustomerContracts.Where(cc => cc.CustomerId == customerId);
        CustomerContract? contract;
        if (long.TryParse(contractIdentifier, out var id))
        {
            contract = await query.FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(contractIdentifier, out var publicId))
        {
            contract = await query.FirstOrDefaultAsync(cc => cc.PublicId == publicId, cancellationToken);
        }
        else
        {
            contract = await query.FirstOrDefaultAsync(cc => cc.ContractNumber == contractIdentifier, cancellationToken);
        }

        if (contract == null)
        {
            throw new NotFoundException("CustomerContract", contractIdentifier);
        }

        return contract;
    }

    public async Task<IReadOnlyList<CustomerContractDto>> GetContractsAsync(
        string customerIdentifier,
        string? search,
        string? status,
        bool? activeOnly,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        var query = _context.CustomerContracts
            .AsNoTracking()
            .Where(cc => cc.CustomerId == customer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(cc =>
                cc.ContractNumber.ToLower().Contains(term) ||
                cc.Title.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(cc => cc.Status == status);
        }

        if (activeOnly.HasValue && activeOnly.Value)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(cc => cc.Status == "Active" && cc.StartDate <= today && cc.EndDate >= today);
        }

        return await query
            .OrderByDescending(cc => cc.StartDate)
            .Select(cc => MapToDto(cc, customer.CustomerCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerContractDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string contractIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contract = await ResolveContractAsync(customer.Id, contractIdentifier, cancellationToken);
        return MapToDto(contract, customer.CustomerCode);
    }

    public async Task<CustomerContractDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        var numExists = await _context.CustomerContracts.AnyAsync(cc => cc.ContractNumber == request.ContractNumber, cancellationToken);
        if (numExists)
        {
            throw new ConflictException("CustomerContract", "ContractNumber", request.ContractNumber);
        }

        if (request.Status == "Active")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today < request.StartDate || today > request.EndDate)
            {
                throw new ConflictException("Contract cannot be created with Active status outside its validity period.");
            }
        }

        var contract = new CustomerContract
        {
            CustomerId = customer.Id,
            ContractNumber = request.ContractNumber,
            Title = request.Title,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            DiscountRate = request.DiscountRate,
            PaymentTermDays = request.PaymentTermDays,
            SignedAt = request.SignedAt,
            FileAttachmentId = request.FileAttachmentId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username
        };

        await _context.CustomerContracts.AddAsync(contract, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Create",
            schemaName: "crm",
            tableName: "CustomerContracts",
            primaryKey: contract.Id.ToString(),
            oldValues: null,
            newValues: MapToDto(contract, customer.CustomerCode),
            notes: $"Created contract {contract.ContractNumber} for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return MapToDto(contract, customer.CustomerCode);
    }

    public async Task<CustomerContractDto> UpdateAsync(
        string customerIdentifier,
        string contractIdentifier,
        UpdateCustomerContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contract = await ResolveContractAsync(customer.Id, contractIdentifier, cancellationToken);

        if (contract.Status == "Terminated" && request.Status != "Terminated")
        {
            throw new ConflictException("Terminated contracts cannot be transitioned back to another status.");
        }

        if (request.Status == "Active")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today < request.StartDate || today > request.EndDate)
            {
                throw new ConflictException("Contract cannot be set to Active status outside its validity period.");
            }
        }

        var oldValues = new
        {
            contract.Title,
            contract.StartDate,
            contract.EndDate,
            contract.Status,
            contract.DiscountRate,
            contract.PaymentTermDays,
            contract.SignedAt,
            contract.FileAttachmentId
        };

        contract.Title = request.Title;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.Status = request.Status;
        contract.DiscountRate = request.DiscountRate;
        contract.PaymentTermDays = request.PaymentTermDays;
        contract.SignedAt = request.SignedAt;
        contract.FileAttachmentId = request.FileAttachmentId;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Update",
            schemaName: "crm",
            tableName: "CustomerContracts",
            primaryKey: contract.Id.ToString(),
            oldValues: oldValues,
            newValues: MapToDto(contract, customer.CustomerCode),
            notes: $"Updated contract {contract.ContractNumber} for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return MapToDto(contract, customer.CustomerCode);
    }

    public async Task UpdateStatusAsync(
        string customerIdentifier,
        string contractIdentifier,
        UpdateCustomerContractStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var contract = await ResolveContractAsync(customer.Id, contractIdentifier, cancellationToken);

        if (contract.Status == "Terminated" && request.Status != "Terminated")
        {
            throw new ConflictException("Terminated contracts cannot be transitioned back to another status.");
        }

        if (request.Status == "Active")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today < contract.StartDate || today > contract.EndDate)
            {
                throw new ConflictException("Contract cannot be activated outside its validity period.");
            }
        }

        var oldStatus = contract.Status;
        contract.Status = request.Status;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "UpdateStatus",
            schemaName: "crm",
            tableName: "CustomerContracts",
            primaryKey: contract.Id.ToString(),
            oldValues: new { Status = oldStatus },
            newValues: new { Status = contract.Status, request.Reason },
            notes: $"Contract {contract.ContractNumber} status transitioned to {contract.Status}",
            cancellationToken: cancellationToken);
    }

    public static CustomerContractDto MapToDto(CustomerContract cc, string? customerCode = null)
    {
        return new CustomerContractDto(
            cc.Id,
            cc.PublicId,
            cc.CustomerId,
            customerCode,
            cc.ContractNumber,
            cc.Title,
            cc.StartDate,
            cc.EndDate,
            cc.Status,
            cc.DiscountRate,
            cc.PaymentTermDays,
            cc.SignedAt,
            cc.FileAttachmentId,
            cc.CreatedAt,
            cc.UpdatedAt
        );
    }
}
#endregion

#region Customer Pricing Rule Service
public class CustomerPricingRuleService : ICustomerPricingRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CustomerPricingRuleService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Customer> ResolveCustomerAsync(string customerIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = _context.Customers;
        Customer? customer;
        if (long.TryParse(customerIdentifier, out var id))
        {
            customer = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(customerIdentifier, out var publicId))
        {
            customer = await query.FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);
        }
        else
        {
            customer = await query.FirstOrDefaultAsync(c => c.CustomerCode == customerIdentifier, cancellationToken);
        }

        if (customer == null)
        {
            throw new NotFoundException("Customer", customerIdentifier);
        }

        EnsureBranchAccess(customer.BranchId);
        return customer;
    }

    private async Task<CustomerPricingRule> ResolvePricingRuleAsync(long customerId, string ruleIdentifier, CancellationToken cancellationToken)
    {
        IQueryable<CustomerPricingRule> query = _context.CustomerPricingRules
            .Include(pr => pr.CustomerContract)
            .Include(pr => pr.ShippingRateCard)
            .Where(pr => pr.CustomerId == customerId);

        CustomerPricingRule? rule;
        if (long.TryParse(ruleIdentifier, out var id))
        {
            rule = await query.FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(ruleIdentifier, out var publicId))
        {
            rule = await query.FirstOrDefaultAsync(pr => pr.PublicId == publicId, cancellationToken);
        }
        else
        {
            rule = await query.FirstOrDefaultAsync(pr => pr.Name == ruleIdentifier, cancellationToken);
        }

        if (rule == null)
        {
            throw new NotFoundException("CustomerPricingRule", ruleIdentifier);
        }

        return rule;
    }

    public async Task<IReadOnlyList<CustomerPricingRuleDto>> GetPricingRulesAsync(
        string customerIdentifier,
        bool? isActive,
        long? rateCardId,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        var query = _context.CustomerPricingRules
            .AsNoTracking()
            .Include(pr => pr.CustomerContract)
            .Include(pr => pr.ShippingRateCard)
            .Where(pr => pr.CustomerId == customer.Id)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(pr => pr.IsActive == isActive.Value);
        }

        if (rateCardId.HasValue && rateCardId.Value > 0)
        {
            query = query.Where(pr => pr.ShippingRateCardId == rateCardId.Value);
        }

        return await query
            .OrderBy(pr => pr.Priority)
            .ThenByDescending(pr => pr.EffectiveFrom)
            .Select(pr => MapToDto(pr, customer.CustomerCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerPricingRuleDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string ruleIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var rule = await ResolvePricingRuleAsync(customer.Id, ruleIdentifier, cancellationToken);
        return MapToDto(rule, customer.CustomerCode);
    }

    public async Task<IReadOnlyList<CustomerPricingRuleDto>> GetApplicablePricingRulesAsync(
        string customerIdentifier,
        DateTime? targetDate,
        long? shippingRateCardId,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var date = targetDate ?? DateTime.UtcNow;

        var query = _context.CustomerPricingRules
            .AsNoTracking()
            .Include(pr => pr.CustomerContract)
            .Include(pr => pr.ShippingRateCard)
            .Where(pr => pr.CustomerId == customer.Id &&
                         pr.IsActive &&
                         pr.EffectiveFrom <= date &&
                         (pr.EffectiveTo == null || pr.EffectiveTo >= date))
            .AsQueryable();

        if (shippingRateCardId.HasValue && shippingRateCardId.Value > 0)
        {
            query = query.Where(pr => pr.ShippingRateCardId == null || pr.ShippingRateCardId == shippingRateCardId.Value);
        }

        return await query
            .OrderBy(pr => pr.Priority)
            .ThenByDescending(pr => pr.DiscountPercent)
            .ThenByDescending(pr => pr.EffectiveFrom)
            .Select(pr => MapToDto(pr, customer.CustomerCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerPricingRuleDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerPricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);

        if (request.CustomerContractId.HasValue)
        {
            var contractExists = await _context.CustomerContracts
                .AnyAsync(cc => cc.Id == request.CustomerContractId.Value && cc.CustomerId == customer.Id, cancellationToken);
            if (!contractExists)
            {
                throw new NotFoundException("CustomerContract", request.CustomerContractId.Value);
            }
        }

        if (request.ShippingRateCardId.HasValue)
        {
            var cardExists = await _context.ShippingRateCards.AnyAsync(rc => rc.Id == request.ShippingRateCardId.Value, cancellationToken);
            if (!cardExists)
            {
                throw new NotFoundException("ShippingRateCard", request.ShippingRateCardId.Value);
            }
        }

        var rule = new CustomerPricingRule
        {
            CustomerId = customer.Id,
            CustomerContractId = request.CustomerContractId,
            ShippingRateCardId = request.ShippingRateCardId,
            Name = request.Name,
            DiscountPercent = request.DiscountPercent,
            FlatDiscountAmount = request.FlatDiscountAmount,
            Priority = request.Priority,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username
        };

        await _context.CustomerPricingRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Create",
            schemaName: "crm",
            tableName: "CustomerPricingRules",
            primaryKey: rule.Id.ToString(),
            oldValues: null,
            newValues: MapToDto(rule, customer.CustomerCode),
            notes: $"Created pricing rule {rule.Name} for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return await GetByIdOrPublicIdAsync(customer.Id.ToString(), rule.Id.ToString(), cancellationToken) ?? MapToDto(rule, customer.CustomerCode);
    }

    public async Task<CustomerPricingRuleDto> UpdateAsync(
        string customerIdentifier,
        string ruleIdentifier,
        UpdateCustomerPricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var rule = await ResolvePricingRuleAsync(customer.Id, ruleIdentifier, cancellationToken);

        if (request.CustomerContractId.HasValue)
        {
            var contractExists = await _context.CustomerContracts
                .AnyAsync(cc => cc.Id == request.CustomerContractId.Value && cc.CustomerId == customer.Id, cancellationToken);
            if (!contractExists)
            {
                throw new NotFoundException("CustomerContract", request.CustomerContractId.Value);
            }
        }

        if (request.ShippingRateCardId.HasValue)
        {
            var cardExists = await _context.ShippingRateCards.AnyAsync(rc => rc.Id == request.ShippingRateCardId.Value, cancellationToken);
            if (!cardExists)
            {
                throw new NotFoundException("ShippingRateCard", request.ShippingRateCardId.Value);
            }
        }

        var oldValues = new
        {
            rule.CustomerContractId,
            rule.ShippingRateCardId,
            rule.Name,
            rule.DiscountPercent,
            rule.FlatDiscountAmount,
            rule.Priority,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.IsActive
        };

        rule.CustomerContractId = request.CustomerContractId;
        rule.ShippingRateCardId = request.ShippingRateCardId;
        rule.Name = request.Name;
        rule.DiscountPercent = request.DiscountPercent;
        rule.FlatDiscountAmount = request.FlatDiscountAmount;
        rule.Priority = request.Priority;
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.EffectiveTo = request.EffectiveTo;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Update",
            schemaName: "crm",
            tableName: "CustomerPricingRules",
            primaryKey: rule.Id.ToString(),
            oldValues: oldValues,
            newValues: MapToDto(rule, customer.CustomerCode),
            notes: $"Updated pricing rule {rule.Name} for customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);

        return await GetByIdOrPublicIdAsync(customer.Id.ToString(), rule.Id.ToString(), cancellationToken) ?? MapToDto(rule, customer.CustomerCode);
    }

    public async Task UpdateStatusAsync(
        string customerIdentifier,
        string ruleIdentifier,
        UpdateCustomerPricingRuleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var rule = await ResolvePricingRuleAsync(customer.Id, ruleIdentifier, cancellationToken);

        var oldStatus = rule.IsActive;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedBy = _currentUserService.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: request.IsActive ? "Activate" : "Deactivate",
            schemaName: "crm",
            tableName: "CustomerPricingRules",
            primaryKey: rule.Id.ToString(),
            oldValues: new { IsActive = oldStatus },
            newValues: new { IsActive = rule.IsActive },
            notes: $"Pricing rule {rule.Name} status updated to {(rule.IsActive ? "Active" : "Inactive")}",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        string customerIdentifier,
        string ruleIdentifier,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(customerIdentifier, cancellationToken);
        var rule = await ResolvePricingRuleAsync(customer.Id, ruleIdentifier, cancellationToken);

        _context.CustomerPricingRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Delete",
            schemaName: "crm",
            tableName: "CustomerPricingRules",
            primaryKey: rule.Id.ToString(),
            oldValues: MapToDto(rule, customer.CustomerCode),
            newValues: null,
            notes: $"Deleted pricing rule {rule.Name} from customer {customer.CustomerCode}",
            cancellationToken: cancellationToken);
    }

    public static CustomerPricingRuleDto MapToDto(CustomerPricingRule pr, string? customerCode = null)
    {
        return new CustomerPricingRuleDto(
            pr.Id,
            pr.PublicId,
            pr.CustomerId,
            customerCode,
            pr.CustomerContractId,
            pr.CustomerContract?.ContractNumber,
            pr.ShippingRateCardId,
            pr.ShippingRateCard?.Name,
            pr.Name,
            pr.DiscountPercent,
            pr.FlatDiscountAmount,
            pr.Priority,
            pr.EffectiveFrom,
            pr.EffectiveTo,
            pr.IsActive,
            pr.CreatedAt,
            pr.UpdatedAt
        );
    }
}
#endregion
