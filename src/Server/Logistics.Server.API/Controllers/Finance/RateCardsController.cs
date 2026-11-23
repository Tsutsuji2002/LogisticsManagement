using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Finance.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Finance;

/// <summary>
/// Management of shipping rate cards and rate rules.
/// </summary>
[Route("api/v1/finance/rate-cards")]
public class RateCardsController : BaseApiController
{
    private readonly IRateManagementService _rateService;
    private readonly IValidator<CreateShippingRateCardRequest> _createCardValidator;
    private readonly IValidator<UpdateShippingRateCardRequest> _updateCardValidator;
    private readonly IValidator<CreateShippingRateRuleRequest> _createRuleValidator;
    private readonly IValidator<UpdateShippingRateRuleRequest> _updateRuleValidator;

    public RateCardsController(
        IRateManagementService rateService,
        IValidator<CreateShippingRateCardRequest> createCardValidator,
        IValidator<UpdateShippingRateCardRequest> updateCardValidator,
        IValidator<CreateShippingRateRuleRequest> createRuleValidator,
        IValidator<UpdateShippingRateRuleRequest> updateRuleValidator)
    {
        _rateService = rateService;
        _createCardValidator = createCardValidator;
        _updateCardValidator = updateCardValidator;
        _createRuleValidator = createRuleValidator;
        _updateRuleValidator = updateRuleValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Finance.RatesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ShippingRateCardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRateCards(
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _rateService.GetRateCardsAsync(isActive, search, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ShippingRateCardDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Finance.RatesRead)]
    [ProducesResponseType(typeof(ApiResponse<ShippingRateCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRateCardById(long id, CancellationToken cancellationToken)
    {
        var card = await _rateService.GetRateCardByIdAsync(id, cancellationToken);
        if (card == null) throw new NotFoundException("ShippingRateCard", id.ToString());
        return Ok(ApiResponse<ShippingRateCardDto>.Ok(card));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Finance.RatesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShippingRateCardDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRateCard([FromBody] CreateShippingRateCardRequest request, CancellationToken cancellationToken)
    {
        var val = await _createCardValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var card = await _rateService.CreateRateCardAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRateCardById), new { id = card.Id }, ApiResponse<ShippingRateCardDto>.Ok(card));
    }

    [HttpPut("{id:long}")]
    [HasPermission(AppPermissions.Finance.RatesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShippingRateCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRateCard(long id, [FromBody] UpdateShippingRateCardRequest request, CancellationToken cancellationToken)
    {
        var val = await _updateCardValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var card = await _rateService.UpdateRateCardAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ShippingRateCardDto>.Ok(card));
    }

    [HttpPatch("{id:long}/status")]
    [HasPermission(AppPermissions.Finance.RatesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShippingRateCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleRateCardStatus(long id, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var card = await _rateService.ToggleRateCardStatusAsync(id, isActive, cancellationToken);
        return Ok(ApiResponse<ShippingRateCardDto>.Ok(card));
    }

    [HttpGet("{id:long}/rules")]
    [HasPermission(AppPermissions.Finance.RatesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShippingRateRuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRateRules(long id, CancellationToken cancellationToken)
    {
        var rules = await _rateService.GetRateRulesAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShippingRateRuleDto>>.Ok(rules));
    }

    [HttpPost("{id:long}/rules")]
    [HasPermission(AppPermissions.Finance.RatesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShippingRateRuleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRateRule(long id, [FromBody] CreateShippingRateRuleRequest request, CancellationToken cancellationToken)
    {
        var val = await _createRuleValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var rule = await _rateService.CreateRateRuleAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetRateRules), new { id }, ApiResponse<ShippingRateRuleDto>.Ok(rule));
    }

    [HttpPut("{id:long}/rules/{ruleId:long}")]
    [HasPermission(AppPermissions.Finance.RatesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShippingRateRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRateRule(long id, long ruleId, [FromBody] UpdateShippingRateRuleRequest request, CancellationToken cancellationToken)
    {
        var val = await _updateRuleValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var rule = await _rateService.UpdateRateRuleAsync(id, ruleId, request, cancellationToken);
        return Ok(ApiResponse<ShippingRateRuleDto>.Ok(rule));
    }

    [HttpDelete("{id:long}/rules/{ruleId:long}")]
    [HasPermission(AppPermissions.Finance.RatesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRateRule(long id, long ruleId, CancellationToken cancellationToken)
    {
        await _rateService.DeleteRateRuleAsync(id, ruleId, cancellationToken);
        return NoContent();
    }
}
