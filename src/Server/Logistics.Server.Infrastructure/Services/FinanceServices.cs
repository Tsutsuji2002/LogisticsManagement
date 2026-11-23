using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Finance.Services;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
//  1. FINANCIAL LEDGER SERVICE
// ═══════════════════════════════════════════════════════════════

public class FinancialLedgerService : IFinancialLedgerService
{
    private readonly ApplicationDbContext _context;
    private readonly INumberSequenceService _numberSequence;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FinancialLedgerService> _logger;

    public FinancialLedgerService(
        ApplicationDbContext context,
        INumberSequenceService numberSequence,
        ICurrentUserService currentUser,
        ILogger<FinancialLedgerService> logger)
    {
        _context = context;
        _numberSequence = numberSequence;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<FinancialLedgerEntryDto> PostEntryAsync(
        long customerId,
        string entryType,
        string referenceType,
        long referenceId,
        string referenceNumber,
        decimal debitAmount,
        decimal creditAmount,
        long currencyId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync([customerId], cancellationToken)
            ?? throw new NotFoundException("Customer", customerId);

        // Fetch last running balance for this customer
        var lastEntry = await _context.FinancialLedgerEntries
            .Where(e => e.CustomerId == customerId)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var currentBalance = lastEntry?.RunningBalance ?? 0m;
        // In customer accounting: Debit increases receivable balance (customer owes more), Credit reduces it (payment made)
        var newRunningBalance = currentBalance + debitAmount - creditAmount;

        var entryNumber = await _numberSequence.GetNextNumberAsync("FLE", cancellationToken);

        var entry = new FinancialLedgerEntry
        {
            EntryNumber = entryNumber,
            CustomerId = customerId,
            EntryType = entryType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            RunningBalance = newRunningBalance,
            CurrencyId = currencyId,
            EntryDate = DateTime.UtcNow,
            Description = description,
            CreatedBy = _currentUser.Username
        };

        _context.FinancialLedgerEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Posted FinancialLedgerEntry {EntryNumber} for Customer {CustomerId}: Debit={Debit}, Credit={Credit}, NewBalance={Balance}",
            entryNumber, customerId, debitAmount, creditAmount, newRunningBalance);

        return MapToDto(entry, customer.CustomerCode, customer.Name, entry.Currency?.Code ?? string.Empty);
    }

    public async Task<PagedResult<FinancialLedgerEntryDto>> GetLedgerEntriesAsync(
        long? customerId,
        string? entryType,
        string? referenceType,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FinancialLedgerEntries
            .Include(e => e.Customer)
            .Include(e => e.Currency)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.AccessibleBranchIds.Count > 0)
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(e => branches.Contains(e.Customer.BranchId));
        }

        if (customerId.HasValue) query = query.Where(e => e.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(entryType)) query = query.Where(e => e.EntryType == entryType);
        if (!string.IsNullOrWhiteSpace(referenceType)) query = query.Where(e => e.ReferenceType == referenceType);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(e => MapToDto(e, e.Customer?.CustomerCode, e.Customer?.Name, e.Currency?.Code)).ToList();
        return new PagedResult<FinancialLedgerEntryDto>(items, totalCount, request.Page, request.PageSize);
    }

    private static FinancialLedgerEntryDto MapToDto(FinancialLedgerEntry e, string? customerCode = null, string? customerName = null, string? currencyCode = null) => new()
    {
        Id = e.Id,
        PublicId = e.PublicId,
        EntryNumber = e.EntryNumber,
        CustomerId = e.CustomerId,
        CustomerCode = customerCode ?? e.Customer?.CustomerCode ?? string.Empty,
        CustomerName = customerName ?? e.Customer?.Name ?? string.Empty,
        EntryType = e.EntryType,
        ReferenceType = e.ReferenceType,
        ReferenceId = e.ReferenceId,
        ReferenceNumber = e.ReferenceNumber,
        DebitAmount = e.DebitAmount,
        CreditAmount = e.CreditAmount,
        RunningBalance = e.RunningBalance,
        CurrencyId = e.CurrencyId,
        CurrencyCode = currencyCode ?? e.Currency?.Code ?? string.Empty,
        EntryDate = e.EntryDate,
        Description = e.Description
    };
}

// ═══════════════════════════════════════════════════════════════
//  2. RATE MANAGEMENT SERVICE
// ═══════════════════════════════════════════════════════════════

public class RateManagementService : IRateManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RateManagementService> _logger;

    public RateManagementService(
        ApplicationDbContext context,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger<RateManagementService> logger)
    {
        _context = context;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<ShippingRateCardDto>> GetRateCardsAsync(
        bool? isActive,
        string? search,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShippingRateCards
            .Include(c => c.BaseCurrency)
            .Include(c => c.Rules)
            .AsNoTracking()
            .AsQueryable();

        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.Code.Contains(s) || c.Name.Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(c => MapToCardDto(c)).ToList();
        return new PagedResult<ShippingRateCardDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<ShippingRateCardDto?> GetRateCardByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var card = await _context.ShippingRateCards
            .Include(c => c.BaseCurrency)
            .Include(c => c.Rules)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return card == null ? null : MapToCardDto(card);
    }

    public async Task<ShippingRateCardDto> CreateRateCardAsync(CreateShippingRateCardRequest request, CancellationToken cancellationToken = default)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await _context.ShippingRateCards.AnyAsync(c => c.Code == codeUpper, cancellationToken);
        if (exists)
            throw new ConflictException("ShippingRateCard", "Code", codeUpper);

        var currency = await _context.Currencies.FindAsync([request.BaseCurrencyId], cancellationToken)
            ?? throw new NotFoundException("Currency", request.BaseCurrencyId);

        if (request.IsDefault)
        {
            var currentDefaults = await _context.ShippingRateCards.Where(c => c.IsDefault).ToListAsync(cancellationToken);
            foreach (var d in currentDefaults) d.IsDefault = false;
        }

        var card = new ShippingRateCard
        {
            Code = codeUpper,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BaseCurrencyId = request.BaseCurrencyId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Username
        };

        _context.ShippingRateCards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateRateCard", "finance", "ShippingRateCards", card.Id.ToString(),
            null, new { card.Code, card.Name, card.IsDefault }, notes: null, cancellationToken: cancellationToken);

        return MapToCardDto(card, currency.Code);
    }

    public async Task<ShippingRateCardDto> UpdateRateCardAsync(long id, UpdateShippingRateCardRequest request, CancellationToken cancellationToken = default)
    {
        var card = await _context.ShippingRateCards
            .Include(c => c.BaseCurrency)
            .Include(c => c.Rules)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("ShippingRateCard", id);

        var currency = await _context.Currencies.FindAsync([request.BaseCurrencyId], cancellationToken)
            ?? throw new NotFoundException("Currency", request.BaseCurrencyId);

        if (request.IsDefault && !card.IsDefault)
        {
            var currentDefaults = await _context.ShippingRateCards.Where(c => c.IsDefault && c.Id != id).ToListAsync(cancellationToken);
            foreach (var d in currentDefaults) d.IsDefault = false;
        }

        var oldState = new { card.Name, card.IsDefault, card.IsActive, card.EffectiveFrom, card.EffectiveTo };

        card.Name = request.Name.Trim();
        card.Description = request.Description?.Trim();
        card.BaseCurrencyId = request.BaseCurrencyId;
        card.EffectiveFrom = request.EffectiveFrom;
        card.EffectiveTo = request.EffectiveTo;
        card.IsDefault = request.IsDefault;
        card.IsActive = request.IsActive;
        card.UpdatedAt = DateTime.UtcNow;
        card.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("UpdateRateCard", "finance", "ShippingRateCards", card.Id.ToString(),
            oldState, new { card.Name, card.IsDefault, card.IsActive, card.EffectiveFrom, card.EffectiveTo },
            notes: null, cancellationToken: cancellationToken);

        return MapToCardDto(card, currency.Code);
    }

    public async Task<ShippingRateCardDto> ToggleRateCardStatusAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var card = await _context.ShippingRateCards
            .Include(c => c.BaseCurrency)
            .Include(c => c.Rules)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("ShippingRateCard", id);

        var oldStatus = card.IsActive;
        card.IsActive = isActive;
        card.UpdatedAt = DateTime.UtcNow;
        card.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("ToggleRateCardStatus", "finance", "ShippingRateCards", card.Id.ToString(),
            new { IsActive = oldStatus }, new { IsActive = isActive }, notes: null, cancellationToken: cancellationToken);

        return MapToCardDto(card);
    }

    public async Task<IReadOnlyList<ShippingRateRuleDto>> GetRateRulesAsync(long rateCardId, CancellationToken cancellationToken = default)
    {
        var cardExists = await _context.ShippingRateCards.AnyAsync(c => c.Id == rateCardId, cancellationToken);
        if (!cardExists) throw new NotFoundException("ShippingRateCard", rateCardId);

        var rules = await _context.ShippingRateRules
            .Include(r => r.OriginProvince)
            .Include(r => r.DestinationProvince)
            .Where(r => r.ShippingRateCardId == rateCardId)
            .OrderBy(r => r.ServiceType)
            .ThenBy(r => r.FromWeightKg)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rules.Select(MapToRuleDto).ToList();
    }

    public async Task<ShippingRateRuleDto> CreateRateRuleAsync(long rateCardId, CreateShippingRateRuleRequest request, CancellationToken cancellationToken = default)
    {
        var card = await _context.ShippingRateCards.FindAsync([rateCardId], cancellationToken)
            ?? throw new NotFoundException("ShippingRateCard", rateCardId);

        if (request.OriginProvinceId.HasValue)
        {
            var pExists = await _context.Provinces.AnyAsync(p => p.Id == request.OriginProvinceId.Value, cancellationToken);
            if (!pExists) throw new NotFoundException("Province", request.OriginProvinceId.Value);
        }

        if (request.DestinationProvinceId.HasValue)
        {
            var pExists = await _context.Provinces.AnyAsync(p => p.Id == request.DestinationProvinceId.Value, cancellationToken);
            if (!pExists) throw new NotFoundException("Province", request.DestinationProvinceId.Value);
        }

        var rule = new ShippingRateRule
        {
            ShippingRateCardId = rateCardId,
            ServiceType = request.ServiceType.Trim(),
            OriginProvinceId = request.OriginProvinceId,
            DestinationProvinceId = request.DestinationProvinceId,
            FromWeightKg = request.FromWeightKg,
            ToWeightKg = request.ToWeightKg,
            BaseRate = request.BaseRate,
            AdditionalPerKgRate = request.AdditionalPerKgRate,
            SurchargePercent = request.SurchargePercent,
            MinimumFee = request.MinimumFee,
            EstimatedDays = request.EstimatedDays
        };

        _context.ShippingRateRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateRateRule", "finance", "ShippingRateRules", rule.Id.ToString(),
            null, new { rule.ServiceType, rule.FromWeightKg, rule.ToWeightKg, rule.BaseRate }, notes: null, cancellationToken: cancellationToken);

        return MapToRuleDto(rule);
    }

    public async Task<ShippingRateRuleDto> UpdateRateRuleAsync(long rateCardId, long ruleId, UpdateShippingRateRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await _context.ShippingRateRules
            .Include(r => r.OriginProvince)
            .Include(r => r.DestinationProvince)
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.ShippingRateCardId == rateCardId, cancellationToken)
            ?? throw new NotFoundException("ShippingRateRule", ruleId);

        rule.ServiceType = request.ServiceType.Trim();
        rule.OriginProvinceId = request.OriginProvinceId;
        rule.DestinationProvinceId = request.DestinationProvinceId;
        rule.FromWeightKg = request.FromWeightKg;
        rule.ToWeightKg = request.ToWeightKg;
        rule.BaseRate = request.BaseRate;
        rule.AdditionalPerKgRate = request.AdditionalPerKgRate;
        rule.SurchargePercent = request.SurchargePercent;
        rule.MinimumFee = request.MinimumFee;
        rule.EstimatedDays = request.EstimatedDays;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToRuleDto(rule);
    }

    public async Task DeleteRateRuleAsync(long rateCardId, long ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.ShippingRateRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.ShippingRateCardId == rateCardId, cancellationToken)
            ?? throw new NotFoundException("ShippingRateRule", ruleId);

        _context.ShippingRateRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ShippingRateCardDto MapToCardDto(ShippingRateCard c, string? currencyCode = null) => new()
    {
        Id = c.Id,
        PublicId = c.PublicId,
        Code = c.Code,
        Name = c.Name,
        Description = c.Description,
        BaseCurrencyId = c.BaseCurrencyId,
        BaseCurrencyCode = currencyCode ?? c.BaseCurrency?.Code ?? string.Empty,
        EffectiveFrom = c.EffectiveFrom,
        EffectiveTo = c.EffectiveTo,
        IsDefault = c.IsDefault,
        IsActive = c.IsActive,
        RulesCount = c.Rules?.Count ?? 0
    };

    private static ShippingRateRuleDto MapToRuleDto(ShippingRateRule r) => new()
    {
        Id = r.Id,
        ShippingRateCardId = r.ShippingRateCardId,
        ServiceType = r.ServiceType,
        OriginProvinceId = r.OriginProvinceId,
        OriginProvinceName = r.OriginProvince?.Name,
        DestinationProvinceId = r.DestinationProvinceId,
        DestinationProvinceName = r.DestinationProvince?.Name,
        FromWeightKg = r.FromWeightKg,
        ToWeightKg = r.ToWeightKg,
        BaseRate = r.BaseRate,
        AdditionalPerKgRate = r.AdditionalPerKgRate,
        SurchargePercent = r.SurchargePercent,
        MinimumFee = r.MinimumFee,
        EstimatedDays = r.EstimatedDays
    };
}

// ═══════════════════════════════════════════════════════════════
//  3. SHIPPING FEE CALCULATION SERVICE
// ═══════════════════════════════════════════════════════════════

public class ShippingFeeCalculationService : IShippingFeeCalculationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ShippingFeeCalculationService> _logger;

    public ShippingFeeCalculationService(
        ApplicationDbContext context,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger<ShippingFeeCalculationService> logger)
    {
        _context = context;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ShippingFeeCalculationResultDto> CalculateFeeAsync(CalculateShippingFeeRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        EnsureBranchAccess(customer.BranchId);

        var calcDate = request.EffectiveDate ?? DateTime.UtcNow;

        // 1. Resolve Pricing Resolution Priority:
        //    a. CustomerPricingRule (active for customer and date)
        //    b. CustomerContract (active contract with discount rate)
        //    c. Default/Standard RateCard
        var customerRules = await _context.CustomerPricingRules
            .Include(pr => pr.ShippingRateCard)
            .Where(pr => pr.CustomerId == customer.Id && pr.IsActive &&
                         pr.EffectiveFrom <= calcDate &&
                         (!pr.EffectiveTo.HasValue || pr.EffectiveTo.Value >= calcDate))
            .OrderBy(pr => pr.Priority)
            .ToListAsync(cancellationToken);

        CustomerPricingRule? activePricingRule = customerRules.FirstOrDefault();

        // Resolve rate card:
        ShippingRateCard? rateCard = null;
        if (activePricingRule?.ShippingRateCardId.HasValue == true)
        {
            rateCard = await _context.ShippingRateCards
                .Include(c => c.BaseCurrency)
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Id == activePricingRule.ShippingRateCardId.Value && c.IsActive, cancellationToken);
        }

        if (rateCard == null)
        {
            // Default rate card
            rateCard = await _context.ShippingRateCards
                .Include(c => c.BaseCurrency)
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive &&
                                         c.EffectiveFrom <= calcDate &&
                                         (!c.EffectiveTo.HasValue || c.EffectiveTo.Value >= calcDate), cancellationToken)
                ?? await _context.ShippingRateCards
                    .Include(c => c.BaseCurrency)
                    .Include(c => c.Rules)
                    .FirstOrDefaultAsync(c => c.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("No active shipping rate card is available for fee calculation.");
        }

        // 2. Resolve matching rule:
        var weight = request.WeightKg;
        var rules = await _context.ShippingRateRules
            .Where(r => r.ShippingRateCardId == rateCard.Id && r.ServiceType == request.ServiceType)
            .ToListAsync(cancellationToken);

        // Find best match:
        // Try specific origin + dest
        var matchedRule = rules.FirstOrDefault(r =>
            r.OriginProvinceId == request.OriginProvinceId &&
            r.DestinationProvinceId == request.DestinationProvinceId &&
            r.FromWeightKg <= weight && r.ToWeightKg >= weight);

        // Try origin match
        matchedRule ??= rules.FirstOrDefault(r =>
            r.OriginProvinceId == request.OriginProvinceId &&
            !r.DestinationProvinceId.HasValue &&
            r.FromWeightKg <= weight && r.ToWeightKg >= weight);

        // Try destination match
        matchedRule ??= rules.FirstOrDefault(r =>
            !r.OriginProvinceId.HasValue &&
            r.DestinationProvinceId == request.DestinationProvinceId &&
            r.FromWeightKg <= weight && r.ToWeightKg >= weight);

        // Try generic match
        matchedRule ??= rules.FirstOrDefault(r =>
            !r.OriginProvinceId.HasValue &&
            !r.DestinationProvinceId.HasValue &&
            r.FromWeightKg <= weight && r.ToWeightKg >= weight);

        // Fallback to closest weight rule
        matchedRule ??= rules.OrderBy(r => Math.Abs(r.FromWeightKg - weight)).FirstOrDefault();

        decimal baseRate = matchedRule?.BaseRate ?? 25000m;
        decimal additionalRate = matchedRule?.AdditionalPerKgRate ?? 5000m;
        decimal surchargePct = matchedRule?.SurchargePercent ?? 0m;
        decimal minimumFee = matchedRule?.MinimumFee ?? 20000m;

        decimal fromWeight = matchedRule?.FromWeightKg ?? 0m;
        decimal excessWeight = Math.Max(0m, weight - fromWeight);
        decimal weightCharge = decimal.Round(excessWeight * additionalRate, 2, MidpointRounding.AwayFromZero);

        decimal subtotal = baseRate + weightCharge;
        decimal surchargeAmount = decimal.Round(subtotal * (surchargePct / 100m), 2, MidpointRounding.AwayFromZero);
        decimal grossAmount = subtotal + surchargeAmount;

        if (minimumFee > 0 && grossAmount < minimumFee)
        {
            grossAmount = minimumFee;
        }

        // Additional fees:
        decimal insuranceFee = 0m;
        if (request.DeclaredValue > 0)
        {
            insuranceFee = decimal.Round(request.DeclaredValue * 0.005m, 2, MidpointRounding.AwayFromZero); // 0.5%
        }

        decimal codFee = 0m;
        if (request.CodAmount > 0)
        {
            codFee = decimal.Round(Math.Max(10000m, request.CodAmount * 0.01m), 2, MidpointRounding.AwayFromZero); // 1% min 10k
        }

        // 3. Apply Customer Discounts:
        decimal discountAmount = 0m;
        if (activePricingRule != null)
        {
            if (activePricingRule.DiscountPercent > 0)
            {
                discountAmount += decimal.Round(subtotal * (activePricingRule.DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
            }
            if (activePricingRule.FlatDiscountAmount > 0)
            {
                discountAmount += activePricingRule.FlatDiscountAmount;
            }
        }
        else
        {
            // Check active customer contract discount
            var contract = await _context.CustomerContracts
                .Where(cc => cc.CustomerId == customer.Id && cc.Status == "Active" &&
                             cc.StartDate <= DateOnly.FromDateTime(calcDate) &&
                             cc.EndDate >= DateOnly.FromDateTime(calcDate))
                .FirstOrDefaultAsync(cancellationToken);

            if (contract != null && contract.DiscountRate > 0)
            {
                discountAmount += decimal.Round(subtotal * (contract.DiscountRate / 100m), 2, MidpointRounding.AwayFromZero);
            }
        }

        if (discountAmount > grossAmount) discountAmount = grossAmount;

        decimal surchargesTotal = surchargeAmount + insuranceFee + codFee;
        decimal totalAmount = decimal.Round(grossAmount - discountAmount + insuranceFee + codFee, 2, MidpointRounding.AwayFromZero);

        return new ShippingFeeCalculationResultDto
        {
            BaseRate = baseRate,
            WeightCharge = weightCharge,
            SurchargeAmount = surchargeAmount,
            InsuranceFee = insuranceFee,
            CodFee = codFee,
            GrossAmount = grossAmount,
            CustomerDiscountAmount = discountAmount,
            SurchargesTotal = surchargesTotal,
            TotalAmount = totalAmount,
            CurrencyId = rateCard.BaseCurrencyId,
            CurrencyCode = rateCard.BaseCurrency?.Code ?? "VND",
            AppliedRateCardId = rateCard.Id,
            AppliedRateCardCode = rateCard.Code,
            AppliedRateRuleId = matchedRule?.Id,
            AppliedPricingRuleId = activePricingRule?.Id,
            AppliedPricingRuleName = activePricingRule?.Name,
            CalculatedAt = DateTime.UtcNow,
            Notes = $"Calculated via RateCard {rateCard.Code} ({request.ServiceType}, weight: {weight:F2}kg)"
        };
    }

    public async Task<IReadOnlyList<ShipmentChargeDto>> CalculateAndSaveShipmentChargesAsync(long shipmentId, CancellationToken cancellationToken = default)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Customer)
            .Include(s => s.Addresses)
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new NotFoundException("Shipment", shipmentId);

        EnsureBranchAccess(shipment.OriginBranchId);

        var senderAddr = shipment.Addresses.FirstOrDefault(a => a.AddressType == "Sender");
        var recipientAddr = shipment.Addresses.FirstOrDefault(a => a.AddressType == "Recipient");

        var effectiveWeight = shipment.ChargeableWeightKg > 0 ? shipment.ChargeableWeightKg : shipment.ActualWeightKg;
        if (effectiveWeight <= 0) effectiveWeight = 1.0m;

        var feeResult = await CalculateFeeAsync(new CalculateShippingFeeRequest
        {
            ShipmentId = shipment.Id,
            CustomerId = shipment.CustomerId,
            ServiceType = shipment.ServiceType,
            OriginProvinceId = senderAddr?.ProvinceId,
            DestinationProvinceId = recipientAddr?.ProvinceId,
            WeightKg = effectiveWeight,
            DeclaredValue = shipment.DeclaredValue,
            CodAmount = shipment.CodAmount,
            CurrencyId = shipment.CurrencyId
        }, cancellationToken);

        // Check for existing uninvoiced base charges to prevent duplication
        var existingUninvoiced = shipment.Charges.Where(c => !c.IsInvoiced).ToList();
        if (existingUninvoiced.Count > 0)
        {
            _context.ShipmentCharges.RemoveRange(existingUninvoiced);
        }

        var newCharges = new List<ShipmentCharge>();

        // 1. Base Freight Charge (Base + Weight - Discount)
        decimal netFreight = Math.Max(0m, (feeResult.BaseRate + feeResult.WeightCharge) - feeResult.CustomerDiscountAmount);
        newCharges.Add(new ShipmentCharge
        {
            ShipmentId = shipment.Id,
            ChargeType = "BaseFreight",
            Amount = netFreight,
            CurrencyId = feeResult.CurrencyId,
            Description = $"Base freight ({shipment.ServiceType}, {effectiveWeight:F2}kg)",
            IsInvoiced = false,
            CreatedAt = DateTime.UtcNow
        });

        // 2. Surcharges
        if (feeResult.SurchargeAmount > 0)
        {
            newCharges.Add(new ShipmentCharge
            {
                ShipmentId = shipment.Id,
                ChargeType = "FuelSurcharge",
                Amount = feeResult.SurchargeAmount,
                CurrencyId = feeResult.CurrencyId,
                Description = "Fuel & operational surcharge",
                IsInvoiced = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 3. Insurance Fee
        if (feeResult.InsuranceFee > 0)
        {
            newCharges.Add(new ShipmentCharge
            {
                ShipmentId = shipment.Id,
                ChargeType = "Insurance",
                Amount = feeResult.InsuranceFee,
                CurrencyId = feeResult.CurrencyId,
                Description = $"Goods insurance for declared value {shipment.DeclaredValue:N0}",
                IsInvoiced = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 4. COD Fee
        if (feeResult.CodFee > 0)
        {
            newCharges.Add(new ShipmentCharge
            {
                ShipmentId = shipment.Id,
                ChargeType = "CodFee",
                Amount = feeResult.CodFee,
                CurrencyId = feeResult.CurrencyId,
                Description = $"COD handling fee for amount {shipment.CodAmount:N0}",
                IsInvoiced = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.ShipmentCharges.AddRange(newCharges);

        // Update Shipment pricing fields
        shipment.ShippingFee = netFreight;
        shipment.InsuranceFee = feeResult.InsuranceFee;
        shipment.SurchargesTotal = feeResult.SurchargeAmount + feeResult.CodFee;
        shipment.TotalAmount = feeResult.TotalAmount;
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = _currentUser.Username;

        // Update CustomerBalance.UnbilledChargesAmount
        var balance = await _context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == shipment.CustomerId, cancellationToken);
        if (balance == null)
        {
            balance = new CustomerBalance
            {
                CustomerId = shipment.CustomerId,
                CurrentOutstandingBalance = 0m,
                UnbilledChargesAmount = feeResult.TotalAmount,
                TotalCodHoldingAmount = 0m,
                LastUpdatedDate = DateTime.UtcNow
            };
            _context.CustomerBalances.Add(balance);
        }
        else
        {
            // Remove previous uninvoiced charges sum and add new total
            decimal previousSum = existingUninvoiced.Sum(c => c.Amount);
            balance.UnbilledChargesAmount = Math.Max(0m, balance.UnbilledChargesAmount - previousSum + feeResult.TotalAmount);
            balance.LastUpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CalculateShipmentCharges", "finance", "ShipmentCharges", shipment.Id.ToString(),
            null, new { shipment.TrackingNumber, feeResult.TotalAmount, ChargesCount = newCharges.Count },
            notes: null, cancellationToken: cancellationToken);

        return newCharges.Select(c => new ShipmentChargeDto
        {
            Id = c.Id,
            ShipmentId = c.ShipmentId,
            ShipmentTrackingNumber = shipment.TrackingNumber,
            ChargeType = c.ChargeType,
            Amount = c.Amount,
            CurrencyId = c.CurrencyId,
            CurrencyCode = feeResult.CurrencyCode,
            Description = c.Description,
            IsInvoiced = c.IsInvoiced,
            InvoiceId = c.InvoiceId,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (!_currentUser.HasBranchAccess(branchId))
            throw new ForbiddenAccessException("You do not have access to financial records for this branch.");
    }
}

// ═══════════════════════════════════════════════════════════════
//  4. SHIPMENT CHARGE SERVICE
// ═══════════════════════════════════════════════════════════════

public class ShipmentChargeService : IShipmentChargeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public ShipmentChargeService(
        ApplicationDbContext context,
        IAuditService audit,
        ICurrentUserService currentUser)
    {
        _context = context;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ShipmentChargeDto>> GetChargesAsync(
        long? shipmentId,
        long? customerId,
        bool? isInvoiced,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShipmentCharges
            .Include(c => c.Shipment)
            .Include(c => c.Currency)
            .Include(c => c.Invoice)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.AccessibleBranchIds.Count > 0)
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(c => branches.Contains(c.Shipment.OriginBranchId) || branches.Contains(c.Shipment.DestinationBranchId));
        }

        if (shipmentId.HasValue) query = query.Where(c => c.ShipmentId == shipmentId.Value);
        if (customerId.HasValue) query = query.Where(c => c.Shipment.CustomerId == customerId.Value);
        if (isInvoiced.HasValue) query = query.Where(c => c.IsInvoiced == isInvoiced.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(c => new ShipmentChargeDto
        {
            Id = c.Id,
            ShipmentId = c.ShipmentId,
            ShipmentTrackingNumber = c.Shipment?.TrackingNumber ?? string.Empty,
            ChargeType = c.ChargeType,
            Amount = c.Amount,
            CurrencyId = c.CurrencyId,
            CurrencyCode = c.Currency?.Code ?? string.Empty,
            Description = c.Description,
            IsInvoiced = c.IsInvoiced,
            InvoiceId = c.InvoiceId,
            InvoiceNumber = c.Invoice?.InvoiceNumber,
            CreatedAt = c.CreatedAt
        }).ToList();

        return new PagedResult<ShipmentChargeDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<ShipmentChargeDto?> GetChargeByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var charge = await _context.ShipmentCharges
            .Include(c => c.Shipment)
            .Include(c => c.Currency)
            .Include(c => c.Invoice)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (charge == null) return null;

        if (!_currentUser.HasBranchAccess(charge.Shipment.OriginBranchId))
            throw new ForbiddenAccessException();

        return new ShipmentChargeDto
        {
            Id = charge.Id,
            ShipmentId = charge.ShipmentId,
            ShipmentTrackingNumber = charge.Shipment?.TrackingNumber ?? string.Empty,
            ChargeType = charge.ChargeType,
            Amount = charge.Amount,
            CurrencyId = charge.CurrencyId,
            CurrencyCode = charge.Currency?.Code ?? string.Empty,
            Description = charge.Description,
            IsInvoiced = charge.IsInvoiced,
            InvoiceId = charge.InvoiceId,
            InvoiceNumber = charge.Invoice?.InvoiceNumber,
            CreatedAt = charge.CreatedAt
        };
    }

    public async Task<ShipmentChargeDto> AddAdjustmentChargeAsync(long shipmentId, CreateChargeAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Currency)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new NotFoundException("Shipment", shipmentId);

        if (!_currentUser.HasBranchAccess(shipment.OriginBranchId))
            throw new ForbiddenAccessException();

        var currency = await _context.Currencies.FindAsync([request.CurrencyId], cancellationToken)
            ?? throw new NotFoundException("Currency", request.CurrencyId);

        var charge = new ShipmentCharge
        {
            ShipmentId = shipmentId,
            ChargeType = request.ChargeType,
            Amount = request.Amount,
            CurrencyId = request.CurrencyId,
            Description = request.Description.Trim(),
            IsInvoiced = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShipmentCharges.Add(charge);

        // Update shipment surcharges and total
        shipment.SurchargesTotal += request.Amount;
        shipment.TotalAmount += request.Amount;
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = _currentUser.Username;

        // Update customer balance unbilled charges
        var balance = await _context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == shipment.CustomerId, cancellationToken);
        if (balance != null)
        {
            balance.UnbilledChargesAmount += request.Amount;
            balance.LastUpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("AddChargeAdjustment", "finance", "ShipmentCharges", charge.Id.ToString(),
            null, new { shipment.TrackingNumber, request.ChargeType, request.Amount }, notes: null, cancellationToken: cancellationToken);

        return new ShipmentChargeDto
        {
            Id = charge.Id,
            ShipmentId = charge.ShipmentId,
            ShipmentTrackingNumber = shipment.TrackingNumber,
            ChargeType = charge.ChargeType,
            Amount = charge.Amount,
            CurrencyId = charge.CurrencyId,
            CurrencyCode = currency.Code,
            Description = charge.Description,
            IsInvoiced = false,
            CreatedAt = charge.CreatedAt
        };
    }
}

// ═══════════════════════════════════════════════════════════════
//  5. INVOICE SERVICE
// ═══════════════════════════════════════════════════════════════

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly INumberSequenceService _numberSequence;
    private readonly IFinancialLedgerService _ledger;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext context,
        INumberSequenceService numberSequence,
        IFinancialLedgerService ledger,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _numberSequence = numberSequence;
        _ledger = ledger;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(
        long? customerId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Include(i => i.PaymentAllocations)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.AccessibleBranchIds.Count > 0)
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(i => branches.Contains(i.Customer.BranchId));
        }

        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.Status == status);
        if (fromDate.HasValue) query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.InvoiceDate <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(MapToInvoiceDto).ToList();
        return new PagedResult<InvoiceDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
                .ThenInclude(ii => ii.Shipment)
            .Include(i => i.PaymentAllocations)
                .ThenInclude(pa => pa.Payment)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null) return null;

        if (!_currentUser.HasBranchAccess(invoice.Customer.BranchId))
            throw new ForbiddenAccessException();

        return MapToInvoiceDto(invoice);
    }

    public async Task<InvoiceDto> CreateDraftInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        if (!_currentUser.HasBranchAccess(customer.BranchId))
            throw new ForbiddenAccessException();

        var invoiceNumber = await _numberSequence.GetNextNumberAsync("INV", cancellationToken);
        var dueDate = request.DueDate ?? request.InvoiceDate.AddDays(request.PaymentTermsDays);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = request.CustomerId,
            InvoiceDate = request.InvoiceDate,
            DueDate = dueDate,
            PeriodStartDate = request.PeriodStartDate,
            PeriodEndDate = request.PeriodEndDate,
            TaxRate = request.TaxRate,
            PaymentTermsDays = request.PaymentTermsDays,
            Notes = request.Notes?.Trim(),
            Status = "Draft",
            PaidAmount = 0m,
            CreatedBy = _currentUser.Username
        };

        decimal subtotal = 0m;
        decimal totalTax = 0m;

        if (request.Items.Count > 0)
        {
            foreach (var itemReq in request.Items)
            {
                decimal itemAmount = itemReq.Quantity * itemReq.UnitPrice;
                decimal itemTax = itemReq.TaxAmount ?? decimal.Round(itemAmount * (request.TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
                decimal itemTotal = itemAmount + itemTax;

                subtotal += itemAmount;
                totalTax += itemTax;

                var invItem = new InvoiceItem
                {
                    ShipmentId = itemReq.ShipmentId,
                    ShipmentChargeId = itemReq.ShipmentChargeId,
                    Description = itemReq.Description.Trim(),
                    Quantity = itemReq.Quantity,
                    UnitPrice = itemReq.UnitPrice,
                    Amount = itemAmount,
                    TaxAmount = itemTax,
                    TotalAmount = itemTotal
                };

                invoice.InvoiceItems.Add(invItem);

                // If associated with a shipment charge, link it
                if (itemReq.ShipmentChargeId.HasValue)
                {
                    var charge = await _context.ShipmentCharges.FindAsync([itemReq.ShipmentChargeId.Value], cancellationToken);
                    if (charge != null)
                    {
                        charge.IsInvoiced = true;
                    }
                }
            }
        }

        invoice.SubTotal = subtotal;
        invoice.TaxAmount = totalTax;
        invoice.TotalAmount = subtotal + totalTax;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateDraftInvoice", "finance", "Invoices", invoice.Id.ToString(),
            null, new { invoice.InvoiceNumber, invoice.TotalAmount, invoice.Status }, notes: null, cancellationToken: cancellationToken);

        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task<InvoiceDto> AddInvoiceItemAsync(long invoiceId, CreateInvoiceItemRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", invoiceId);

        if (invoice.Status != "Draft")
            throw new ConflictException("Cannot add items to an invoice that is not in Draft status.");

        if (!_currentUser.HasBranchAccess(invoice.Customer.BranchId))
            throw new ForbiddenAccessException();

        decimal itemAmount = request.Quantity * request.UnitPrice;
        decimal itemTax = request.TaxAmount ?? decimal.Round(itemAmount * (invoice.TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
        decimal itemTotal = itemAmount + itemTax;

        var invItem = new InvoiceItem
        {
            InvoiceId = invoice.Id,
            ShipmentId = request.ShipmentId,
            ShipmentChargeId = request.ShipmentChargeId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Amount = itemAmount,
            TaxAmount = itemTax,
            TotalAmount = itemTotal
        };

        if (request.ShipmentChargeId.HasValue)
        {
            var charge = await _context.ShipmentCharges.FindAsync([request.ShipmentChargeId.Value], cancellationToken);
            if (charge != null)
            {
                charge.IsInvoiced = true;
                charge.InvoiceId = invoice.Id;
            }
        }

        invoice.InvoiceItems.Add(invItem);
        invoice.SubTotal += itemAmount;
        invoice.TaxAmount += itemTax;
        invoice.TotalAmount += itemTotal;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task<InvoiceDto> RemoveInvoiceItemAsync(long invoiceId, long itemId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", invoiceId);

        if (invoice.Status != "Draft")
            throw new ConflictException("Cannot remove items from an invoice that is not in Draft status.");

        if (!_currentUser.HasBranchAccess(invoice.Customer.BranchId))
            throw new ForbiddenAccessException();

        var item = invoice.InvoiceItems.FirstOrDefault(ii => ii.Id == itemId)
            ?? throw new NotFoundException("InvoiceItem", itemId);

        if (item.ShipmentChargeId.HasValue)
        {
            var charge = await _context.ShipmentCharges.FindAsync([item.ShipmentChargeId.Value], cancellationToken);
            if (charge != null)
            {
                charge.IsInvoiced = false;
                charge.InvoiceId = null;
            }
        }

        invoice.SubTotal = Math.Max(0m, invoice.SubTotal - item.Amount);
        invoice.TaxAmount = Math.Max(0m, invoice.TaxAmount - item.TaxAmount);
        invoice.TotalAmount = Math.Max(0m, invoice.TotalAmount - item.TotalAmount);
        _context.InvoiceItems.Remove(item);
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task<InvoiceDto> IssueInvoiceAsync(long invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", invoiceId);

        if (invoice.Status != "Draft")
            throw new ConflictException($"Invoice {invoice.InvoiceNumber} is already in status '{invoice.Status}' and cannot be issued.");

        if (invoice.TotalAmount <= 0)
            throw new ConflictException("Cannot issue an invoice with zero or negative total amount.");

        if (!_currentUser.HasBranchAccess(invoice.Customer.BranchId))
            throw new ForbiddenAccessException();

        invoice.Status = "Issued";
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = _currentUser.Username;

        // 1. Post to Financial Ledger: Debit customer receivable
        var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.IsDefault, cancellationToken)
            ?? await _context.Currencies.FirstAsync(cancellationToken);

        await _ledger.PostEntryAsync(
            customerId: invoice.CustomerId,
            entryType: "InvoiceCharge",
            referenceType: "Invoice",
            referenceId: invoice.Id,
            referenceNumber: invoice.InvoiceNumber,
            debitAmount: invoice.TotalAmount,
            creditAmount: 0m,
            currencyId: currency.Id,
            description: $"Issued Invoice {invoice.InvoiceNumber} for Customer {invoice.Customer.CustomerCode}",
            cancellationToken: cancellationToken);

        // 2. Update Customer Balance
        var balance = await _context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == invoice.CustomerId, cancellationToken);
        if (balance == null)
        {
            balance = new CustomerBalance
            {
                CustomerId = invoice.CustomerId,
                CurrentOutstandingBalance = invoice.TotalAmount,
                UnbilledChargesAmount = 0m,
                TotalCodHoldingAmount = 0m,
                LastUpdatedDate = DateTime.UtcNow
            };
            _context.CustomerBalances.Add(balance);
        }
        else
        {
            balance.CurrentOutstandingBalance += invoice.TotalAmount;
            // Subtract subtotal from unbilled charges if shipment charges were included
            var chargedSum = invoice.InvoiceItems.Where(ii => ii.ShipmentChargeId.HasValue).Sum(ii => ii.Amount);
            balance.UnbilledChargesAmount = Math.Max(0m, balance.UnbilledChargesAmount - chargedSum);
            balance.LastUpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("IssueInvoice", "finance", "Invoices", invoice.Id.ToString(),
            new { Status = "Draft" }, new { Status = "Issued", invoice.TotalAmount },
            notes: null, cancellationToken: cancellationToken);

        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(long invoiceId, CancelInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", invoiceId);

        if (invoice.Status == "Cancelled")
            throw new ConflictException("Invoice is already cancelled.");

        if (invoice.PaidAmount > 0)
            throw new ConflictException("Cannot cancel an invoice with existing payments. Reverse allocations first.");

        if (!_currentUser.HasBranchAccess(invoice.Customer.BranchId))
            throw new ForbiddenAccessException();

        var wasIssued = invoice.Status == "Issued";
        invoice.Status = "Cancelled";
        invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
            ? $"Cancelled: {request.Reason.Trim()}"
            : $"{invoice.Notes} | Cancelled: {request.Reason.Trim()}";
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = _currentUser.Username;

        // Unlink charges
        foreach (var item in invoice.InvoiceItems.Where(ii => ii.ShipmentChargeId.HasValue))
        {
            var charge = await _context.ShipmentCharges.FindAsync([item.ShipmentChargeId!.Value], cancellationToken);
            if (charge != null)
            {
                charge.IsInvoiced = false;
                charge.InvoiceId = null;
            }
        }

        if (wasIssued)
        {
            // Reverse in ledger
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.IsDefault, cancellationToken)
                ?? await _context.Currencies.FirstAsync(cancellationToken);

            await _ledger.PostEntryAsync(
                customerId: invoice.CustomerId,
                entryType: "CreditNote",
                referenceType: "Invoice",
                referenceId: invoice.Id,
                referenceNumber: invoice.InvoiceNumber,
                debitAmount: 0m,
                creditAmount: invoice.TotalAmount,
                currencyId: currency.Id,
                description: $"Cancelled Invoice {invoice.InvoiceNumber}: {request.Reason.Trim()}",
                cancellationToken: cancellationToken);

            // Revert customer balance
            var balance = await _context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == invoice.CustomerId, cancellationToken);
            if (balance != null)
            {
                balance.CurrentOutstandingBalance = Math.Max(0m, balance.CurrentOutstandingBalance - invoice.TotalAmount);
                var chargedSum = invoice.InvoiceItems.Where(ii => ii.ShipmentChargeId.HasValue).Sum(ii => ii.Amount);
                balance.UnbilledChargesAmount += chargedSum;
                balance.LastUpdatedDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CancelInvoice", "finance", "Invoices", invoice.Id.ToString(),
            new { Status = wasIssued ? "Issued" : "Draft" }, new { Status = "Cancelled", request.Reason },
            notes: null, cancellationToken: cancellationToken);

        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }

    private static InvoiceDto MapToInvoiceDto(Invoice i) => new()
    {
        Id = i.Id,
        PublicId = i.PublicId,
        InvoiceNumber = i.InvoiceNumber,
        CustomerId = i.CustomerId,
        CustomerCode = i.Customer?.CustomerCode ?? string.Empty,
        CustomerName = i.Customer?.Name ?? string.Empty,
        InvoiceDate = i.InvoiceDate,
        DueDate = i.DueDate,
        PeriodStartDate = i.PeriodStartDate,
        PeriodEndDate = i.PeriodEndDate,
        SubTotal = i.SubTotal,
        TaxRate = i.TaxRate,
        TaxAmount = i.TaxAmount,
        TotalAmount = i.TotalAmount,
        PaidAmount = i.PaidAmount,
        OutstandingAmount = i.TotalAmount - i.PaidAmount,
        Status = i.Status,
        PaymentTermsDays = i.PaymentTermsDays,
        Notes = i.Notes,
        Items = i.InvoiceItems.Select(ii => new InvoiceItemDto
        {
            Id = ii.Id,
            InvoiceId = ii.InvoiceId,
            ShipmentId = ii.ShipmentId,
            ShipmentTrackingNumber = ii.Shipment?.TrackingNumber,
            ShipmentChargeId = ii.ShipmentChargeId,
            Description = ii.Description,
            Quantity = ii.Quantity,
            UnitPrice = ii.UnitPrice,
            Amount = ii.Amount,
            TaxAmount = ii.TaxAmount,
            TotalAmount = ii.TotalAmount
        }).ToList(),
        Allocations = i.PaymentAllocations.Select(pa => new PaymentAllocationDto
        {
            Id = pa.Id,
            PaymentId = pa.PaymentId,
            PaymentNumber = pa.Payment?.PaymentNumber ?? string.Empty,
            InvoiceId = pa.InvoiceId,
            InvoiceNumber = i.InvoiceNumber,
            AllocatedAmount = pa.AllocatedAmount,
            AllocatedAt = pa.AllocatedAt,
            AllocatedByUserId = pa.AllocatedByUserId
        }).ToList()
    };
}

// ═══════════════════════════════════════════════════════════════
//  6. PAYMENT & ALLOCATION SERVICE
// ═══════════════════════════════════════════════════════════════

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly INumberSequenceService _numberSequence;
    private readonly IFinancialLedgerService _ledger;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        INumberSequenceService numberSequence,
        IFinancialLedgerService ledger,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _numberSequence = numberSequence;
        _ledger = ledger;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(
        long? customerId,
        string? status,
        string? paymentMethod,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Currency)
            .Include(p => p.ConfirmedByUser)
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Invoice)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.AccessibleBranchIds.Count > 0)
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(p => branches.Contains(p.Customer.BranchId));
        }

        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(paymentMethod)) query = query.Where(p => p.PaymentMethod == paymentMethod);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(MapToPaymentDto).ToList();
        return new PagedResult<PaymentDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Currency)
            .Include(p => p.ConfirmedByUser)
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Invoice)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payment == null) return null;

        if (!_currentUser.HasBranchAccess(payment.Customer.BranchId))
            throw new ForbiddenAccessException();

        return MapToPaymentDto(payment);
    }

    public async Task<PaymentDto> RegisterPaymentAsync(RegisterPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        if (!_currentUser.HasBranchAccess(customer.BranchId))
            throw new ForbiddenAccessException();

        var currency = await _context.Currencies.FindAsync([request.CurrencyId], cancellationToken)
            ?? throw new NotFoundException("Currency", request.CurrencyId);

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == _currentUser.Username, cancellationToken)
            ?? await _context.UserAccounts.FirstAsync(cancellationToken);

        var paymentNumber = await _numberSequence.GetNextNumberAsync("PAY", cancellationToken);
        var paymentDate = request.PaymentDate ?? DateTime.UtcNow;

        var payment = new Payment
        {
            PaymentNumber = paymentNumber,
            CustomerId = request.CustomerId,
            PaymentDate = paymentDate,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount,
            CurrencyId = request.CurrencyId,
            TransactionReference = request.TransactionReference?.Trim(),
            BankName = request.BankName?.Trim(),
            BankAccount = request.BankAccount?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = "Confirmed",
            ConfirmedByUserId = user.Id,
            ConfirmedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Username
        };

        _context.Payments.Add(payment);

        // 1. Post to Financial Ledger: Credit customer receivable (reducing balance)
        await _ledger.PostEntryAsync(
            customerId: customer.Id,
            entryType: "PaymentReceipt",
            referenceType: "Payment",
            referenceId: 0, // Will update or set after save
            referenceNumber: paymentNumber,
            debitAmount: 0m,
            creditAmount: request.Amount,
            currencyId: request.CurrencyId,
            description: $"Payment {paymentNumber} received via {request.PaymentMethod}",
            cancellationToken: cancellationToken);

        // 2. Update Customer Balance
        var balance = await _context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == customer.Id, cancellationToken);
        if (balance == null)
        {
            balance = new CustomerBalance
            {
                CustomerId = customer.Id,
                CurrentOutstandingBalance = -request.Amount, // Credit position
                UnbilledChargesAmount = 0m,
                TotalCodHoldingAmount = 0m,
                LastUpdatedDate = DateTime.UtcNow
            };
            _context.CustomerBalances.Add(balance);
        }
        else
        {
            balance.CurrentOutstandingBalance -= request.Amount;
            balance.LastUpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Process Initial Allocations if requested
        if (request.InitialAllocations?.Count > 0)
        {
            decimal totalToAllocate = request.InitialAllocations.Sum(a => a.Amount);
            if (totalToAllocate > request.Amount)
                throw new ConflictException($"Total allocation amount ({totalToAllocate}) exceeds payment amount ({request.Amount}).");

            foreach (var allocReq in request.InitialAllocations)
            {
                await AllocatePaymentInternalAsync(payment, allocReq.InvoiceId, allocReq.Amount, user.Id, cancellationToken);
            }
        }

        await _audit.RecordAsync("RegisterPayment", "finance", "Payments", payment.Id.ToString(),
            null, new { payment.PaymentNumber, payment.Amount, payment.PaymentMethod }, notes: null, cancellationToken: cancellationToken);

        return (await GetPaymentByIdAsync(payment.Id, cancellationToken))!;
    }

    public async Task<PaymentDto> AllocatePaymentAsync(long paymentId, CreatePaymentAllocationRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException("Payment", paymentId);

        if (payment.Status != "Confirmed")
            throw new ConflictException($"Payment {payment.PaymentNumber} is not in Confirmed status.");

        if (!_currentUser.HasBranchAccess(payment.Customer.BranchId))
            throw new ForbiddenAccessException();

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == _currentUser.Username, cancellationToken);
        long userId = user?.Id ?? payment.ConfirmedByUserId;

        await AllocatePaymentInternalAsync(payment, request.InvoiceId, request.Amount, userId, cancellationToken);

        await _audit.RecordAsync("AllocatePayment", "finance", "PaymentAllocations", payment.Id.ToString(),
            null, new { PaymentId = payment.Id, request.InvoiceId, request.Amount }, notes: null, cancellationToken: cancellationToken);

        return (await GetPaymentByIdAsync(payment.Id, cancellationToken))!;
    }

    private async Task AllocatePaymentInternalAsync(Payment payment, long invoiceId, decimal amount, long userId, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.PaymentAllocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", invoiceId);

        if (invoice.CustomerId != payment.CustomerId)
            throw new ConflictException("Invoice customer does not match payment customer.");

        if (invoice.Status != "Issued" && invoice.Status != "PartiallyPaid")
            throw new ConflictException($"Cannot allocate payment to invoice in '{invoice.Status}' status. Invoice must be Issued or PartiallyPaid.");

        decimal currentAllocated = payment.Allocations.Sum(a => a.AllocatedAmount);
        decimal availableOnPayment = payment.Amount - currentAllocated;
        if (amount > availableOnPayment)
            throw new ConflictException($"Requested allocation {amount:N2} exceeds remaining payment balance {availableOnPayment:N2}.");

        decimal invoiceOutstanding = invoice.TotalAmount - invoice.PaidAmount;
        if (amount > invoiceOutstanding)
            throw new ConflictException($"Requested allocation {amount:N2} exceeds invoice outstanding amount {invoiceOutstanding:N2}.");

        var allocation = new PaymentAllocation
        {
            PaymentId = payment.Id,
            InvoiceId = invoice.Id,
            AllocatedAmount = amount,
            AllocatedAt = DateTime.UtcNow,
            AllocatedByUserId = userId
        };

        payment.Allocations.Add(allocation);
        invoice.PaidAmount += amount;

        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = "Paid";
        }
        else
        {
            invoice.Status = "PartiallyPaid";
        }
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentDto> ReverseAllocationAsync(long paymentId, long allocationId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException("Payment", paymentId);

        if (!_currentUser.HasBranchAccess(payment.Customer.BranchId))
            throw new ForbiddenAccessException();

        var allocation = payment.Allocations.FirstOrDefault(a => a.Id == allocationId)
            ?? throw new NotFoundException("PaymentAllocation", allocationId);

        var invoice = await _context.Invoices.FindAsync([allocation.InvoiceId], cancellationToken)
            ?? throw new NotFoundException("Invoice", allocation.InvoiceId);

        invoice.PaidAmount = Math.Max(0m, invoice.PaidAmount - allocation.AllocatedAmount);
        invoice.Status = invoice.PaidAmount == 0m ? "Issued" : "PartiallyPaid";
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = _currentUser.Username;

        _context.PaymentAllocations.Remove(allocation);

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("ReversePaymentAllocation", "finance", "PaymentAllocations", allocationId.ToString(),
            new { allocation.AllocatedAmount, allocation.InvoiceId }, null, notes: null, cancellationToken: cancellationToken);

        return (await GetPaymentByIdAsync(payment.Id, cancellationToken))!;
    }

    private static PaymentDto MapToPaymentDto(Payment p)
    {
        decimal allocatedSum = p.Allocations?.Sum(a => a.AllocatedAmount) ?? 0m;
        return new PaymentDto
        {
            Id = p.Id,
            PublicId = p.PublicId,
            PaymentNumber = p.PaymentNumber,
            CustomerId = p.CustomerId,
            CustomerCode = p.Customer?.CustomerCode ?? string.Empty,
            CustomerName = p.Customer?.Name ?? string.Empty,
            PaymentDate = p.PaymentDate,
            PaymentMethod = p.PaymentMethod,
            Amount = p.Amount,
            AllocatedAmount = allocatedSum,
            UnallocatedAmount = p.Amount - allocatedSum,
            CurrencyId = p.CurrencyId,
            CurrencyCode = p.Currency?.Code ?? string.Empty,
            TransactionReference = p.TransactionReference,
            BankName = p.BankName,
            BankAccount = p.BankAccount,
            Notes = p.Notes,
            Status = p.Status,
            ConfirmedByUserId = p.ConfirmedByUserId,
            ConfirmedByUsername = p.ConfirmedByUser?.Username,
            ConfirmedAt = p.ConfirmedAt,
            Allocations = p.Allocations?.Select(a => new PaymentAllocationDto
            {
                Id = a.Id,
                PaymentId = a.PaymentId,
                PaymentNumber = p.PaymentNumber,
                InvoiceId = a.InvoiceId,
                InvoiceNumber = a.Invoice?.InvoiceNumber ?? string.Empty,
                AllocatedAmount = a.AllocatedAmount,
                AllocatedAt = a.AllocatedAt,
                AllocatedByUserId = a.AllocatedByUserId,
                AllocatedByUsername = a.AllocatedByUser?.Username
            }).ToList() ?? []
        };
    }
}

// ═══════════════════════════════════════════════════════════════
//  7. CUSTOMER BALANCE SERVICE
// ═══════════════════════════════════════════════════════════════

public class CustomerBalanceService : ICustomerBalanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CustomerBalanceService> _logger;

    public CustomerBalanceService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<CustomerBalanceService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CustomerBalanceDto?> GetCustomerBalanceAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync([customerId], cancellationToken)
            ?? throw new NotFoundException("Customer", customerId);

        if (!_currentUser.HasBranchAccess(customer.BranchId))
            throw new ForbiddenAccessException();

        var balance = await _context.CustomerBalances
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.CustomerId == customerId, cancellationToken);

        if (balance == null)
        {
            // Auto-initialize zero balance record
            balance = new CustomerBalance
            {
                CustomerId = customerId,
                CurrentOutstandingBalance = 0m,
                UnbilledChargesAmount = 0m,
                TotalCodHoldingAmount = 0m,
                LastUpdatedDate = DateTime.UtcNow
            };
            _context.CustomerBalances.Add(balance);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new CustomerBalanceDto
        {
            Id = balance.Id,
            CustomerId = balance.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.Name,
            CurrentOutstandingBalance = balance.CurrentOutstandingBalance,
            UnbilledChargesAmount = balance.UnbilledChargesAmount,
            TotalCodHoldingAmount = balance.TotalCodHoldingAmount,
            LastUpdatedDate = balance.LastUpdatedDate
        };
    }

    public async Task<PagedResult<CustomerBalanceDto>> GetBalancesAsync(long? branchId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerBalances
            .Include(b => b.Customer)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.AccessibleBranchIds.Count > 0)
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(b => branches.Contains(b.Customer.BranchId));
        }

        if (branchId.HasValue) query = query.Where(b => b.Customer.BranchId == branchId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(b => b.CurrentOutstandingBalance)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(b => new CustomerBalanceDto
        {
            Id = b.Id,
            CustomerId = b.CustomerId,
            CustomerCode = b.Customer?.CustomerCode ?? string.Empty,
            CustomerName = b.Customer?.Name ?? string.Empty,
            CurrentOutstandingBalance = b.CurrentOutstandingBalance,
            UnbilledChargesAmount = b.UnbilledChargesAmount,
            TotalCodHoldingAmount = b.TotalCodHoldingAmount,
            LastUpdatedDate = b.LastUpdatedDate
        }).ToList();

        return new PagedResult<CustomerBalanceDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CustomerBalanceReconciliationDto> ReconcileCustomerBalanceAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync([customerId], cancellationToken)
            ?? throw new NotFoundException("Customer", customerId);

        if (!_currentUser.HasBranchAccess(customer.BranchId))
            throw new ForbiddenAccessException();

        var balance = await _context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == customerId, cancellationToken);
        if (balance == null)
        {
            balance = new CustomerBalance
            {
                CustomerId = customerId,
                CurrentOutstandingBalance = 0m,
                UnbilledChargesAmount = 0m,
                TotalCodHoldingAmount = 0m,
                LastUpdatedDate = DateTime.UtcNow
            };
            _context.CustomerBalances.Add(balance);
        }

        // Recompute issued invoice total
        var issuedInvoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId && (i.Status == "Issued" || i.Status == "PartiallyPaid" || i.Status == "Overdue"))
            .ToListAsync(cancellationToken);

        decimal recomputedOutstanding = issuedInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

        // Recompute unbilled charges
        var unbilledCharges = await _context.ShipmentCharges
            .Include(c => c.Shipment)
            .Where(c => c.Shipment.CustomerId == customerId && !c.IsInvoiced)
            .ToListAsync(cancellationToken);

        decimal recomputedUnbilled = unbilledCharges.Sum(c => c.Amount);

        bool isConsistent = (balance.CurrentOutstandingBalance == recomputedOutstanding) &&
                            (balance.UnbilledChargesAmount == recomputedUnbilled);

        string details = isConsistent
            ? "Customer balance is fully consistent with active invoices and shipment charges."
            : $"Adjusted balance from ({balance.CurrentOutstandingBalance:N2}, {balance.UnbilledChargesAmount:N2}) to ({recomputedOutstanding:N2}, {recomputedUnbilled:N2}).";

        if (!isConsistent)
        {
            balance.CurrentOutstandingBalance = recomputedOutstanding;
            balance.UnbilledChargesAmount = recomputedUnbilled;
            balance.LastUpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new CustomerBalanceReconciliationDto
        {
            CustomerId = customerId,
            CustomerCode = customer.CustomerCode,
            RecordedOutstandingBalance = balance.CurrentOutstandingBalance,
            RecomputedOutstandingBalance = recomputedOutstanding,
            RecordedUnbilledCharges = balance.UnbilledChargesAmount,
            RecomputedUnbilledCharges = recomputedUnbilled,
            IsConsistent = isConsistent,
            Details = details
        };
    }
}
