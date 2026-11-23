using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Crm;

/// <summary>
/// Endpoints for managing negotiated rate discounts, fuel surcharges, and volume pricing exceptions.
/// </summary>
[Route("api/v1/customers/{customerId}/pricing-rules")]
public class CustomerPricingRulesController : BaseApiController
{
    private readonly ICustomerPricingRuleService _pricingRuleService;
    private readonly IValidator<CreateCustomerPricingRuleRequest> _createValidator;
    private readonly IValidator<UpdateCustomerPricingRuleRequest> _updateValidator;

    public CustomerPricingRulesController(
        ICustomerPricingRuleService pricingRuleService,
        IValidator<CreateCustomerPricingRuleRequest> createValidator,
        IValidator<UpdateCustomerPricingRuleRequest> updateValidator)
    {
        _pricingRuleService = pricingRuleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists custom pricing rules configured for a customer.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Crm.PricingRulesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerPricingRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPricingRules(
        string customerId,
        [FromQuery] bool? isActive,
        [FromQuery] long? rateCardId,
        CancellationToken cancellationToken)
    {
        var rules = await _pricingRuleService.GetPricingRulesAsync(customerId, isActive, rateCardId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerPricingRuleDto>>.Ok(rules));
    }

    /// <summary>
    /// Evaluates and returns effective pricing rules ordered by precedence for a given calculation date.
    /// </summary>
    [HttpGet("applicable")]
    [HasPermission(AppPermissions.Crm.PricingRulesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerPricingRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicable(
        string customerId,
        [FromQuery] DateTime? date,
        [FromQuery] long? shippingRateCardId,
        CancellationToken cancellationToken)
    {
        var rules = await _pricingRuleService.GetApplicablePricingRulesAsync(customerId, date, shippingRateCardId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerPricingRuleDto>>.Ok(rules));
    }

    /// <summary>
    /// Retrieves a single customer pricing rule.
    /// </summary>
    [HttpGet("{ruleId}")]
    [HasPermission(AppPermissions.Crm.PricingRulesRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerPricingRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPricingRule(
        string customerId,
        string ruleId,
        CancellationToken cancellationToken)
    {
        var rule = await _pricingRuleService.GetByIdOrPublicIdAsync(customerId, ruleId, cancellationToken);
        if (rule == null)
        {
            return NotFound(ApiResponse<CustomerPricingRuleDto>.Fail($"Pricing rule '{ruleId}' not found."));
        }
        return Ok(ApiResponse<CustomerPricingRuleDto>.Ok(rule));
    }

    /// <summary>
    /// Creates a custom pricing rule for a customer.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Crm.PricingRulesCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerPricingRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePricingRule(
        string customerId,
        [FromBody] CreateCustomerPricingRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var rule = await _pricingRuleService.CreateAsync(customerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetPricingRule), new { customerId, ruleId = rule.PublicId }, ApiResponse<CustomerPricingRuleDto>.Ok(rule, "Pricing rule created successfully."));
    }

    /// <summary>
    /// Updates an existing pricing rule.
    /// </summary>
    [HttpPut("{ruleId}")]
    [HasPermission(AppPermissions.Crm.PricingRulesUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerPricingRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePricingRule(
        string customerId,
        string ruleId,
        [FromBody] UpdateCustomerPricingRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var rule = await _pricingRuleService.UpdateAsync(customerId, ruleId, request, cancellationToken);
        return Ok(ApiResponse<CustomerPricingRuleDto>.Ok(rule, "Pricing rule updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a pricing rule.
    /// </summary>
    [HttpPatch("{ruleId}/status")]
    [HasPermission(AppPermissions.Crm.PricingRulesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string customerId,
        string ruleId,
        [FromBody] UpdateCustomerPricingRuleStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _pricingRuleService.UpdateStatusAsync(customerId, ruleId, request, cancellationToken);
        return Ok(ApiResponse.Ok("Pricing rule status updated successfully."));
    }

    /// <summary>
    /// Deletes a pricing rule.
    /// </summary>
    [HttpDelete("{ruleId}")]
    [HasPermission(AppPermissions.Crm.PricingRulesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePricingRule(
        string customerId,
        string ruleId,
        CancellationToken cancellationToken)
    {
        await _pricingRuleService.DeleteAsync(customerId, ruleId, cancellationToken);
        return Ok(ApiResponse.Ok("Pricing rule deleted successfully."));
    }
}
