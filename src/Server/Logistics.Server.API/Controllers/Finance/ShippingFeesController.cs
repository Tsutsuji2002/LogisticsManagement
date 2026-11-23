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
/// Shipping fee calculation engine and shipment charge management.
/// </summary>
[Route("api/v1/finance/shipping-fees")]
public class ShippingFeesController : BaseApiController
{
    private readonly IShippingFeeCalculationService _calculationService;
    private readonly IShipmentChargeService _chargeService;
    private readonly IValidator<CalculateShippingFeeRequest> _calcValidator;
    private readonly IValidator<CreateChargeAdjustmentRequest> _adjustmentValidator;

    public ShippingFeesController(
        IShippingFeeCalculationService calculationService,
        IShipmentChargeService chargeService,
        IValidator<CalculateShippingFeeRequest> calcValidator,
        IValidator<CreateChargeAdjustmentRequest> adjustmentValidator)
    {
        _calculationService = calculationService;
        _chargeService = chargeService;
        _calcValidator = calcValidator;
        _adjustmentValidator = adjustmentValidator;
    }

    [HttpPost("calculate")]
    [HasPermission(AppPermissions.Finance.ChargesCalculate)]
    [ProducesResponseType(typeof(ApiResponse<ShippingFeeCalculationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateFee([FromBody] CalculateShippingFeeRequest request, CancellationToken cancellationToken)
    {
        var val = await _calcValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _calculationService.CalculateFeeAsync(request, cancellationToken);
        return Ok(ApiResponse<ShippingFeeCalculationResultDto>.Ok(result));
    }

    [HttpPost("calculate-and-save/{shipmentId:long}")]
    [HasPermission(AppPermissions.Finance.ChargesCalculate)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentChargeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateAndSaveShipmentCharges(long shipmentId, CancellationToken cancellationToken)
    {
        var charges = await _calculationService.CalculateAndSaveShipmentChargesAsync(shipmentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentChargeDto>>.Ok(charges));
    }

    [HttpGet("charges")]
    [HasPermission(AppPermissions.Finance.ChargesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ShipmentChargeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCharges(
        [FromQuery] long? shipmentId,
        [FromQuery] long? customerId,
        [FromQuery] bool? isInvoiced,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _chargeService.GetChargesAsync(shipmentId, customerId, isInvoiced, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ShipmentChargeDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("charges/{id:long}")]
    [HasPermission(AppPermissions.Finance.ChargesRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentChargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChargeById(long id, CancellationToken cancellationToken)
    {
        var charge = await _chargeService.GetChargeByIdAsync(id, cancellationToken);
        if (charge == null) throw new NotFoundException("ShipmentCharge", id.ToString());
        return Ok(ApiResponse<ShipmentChargeDto>.Ok(charge));
    }

    [HttpPost("charges/{shipmentId:long}/adjustments")]
    [HasPermission(AppPermissions.Finance.ChargesAdjust)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentChargeDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddAdjustmentCharge(long shipmentId, [FromBody] CreateChargeAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var val = await _adjustmentValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var charge = await _chargeService.AddAdjustmentChargeAsync(shipmentId, request, cancellationToken);
        return CreatedAtAction(nameof(GetChargeById), new { id = charge.Id }, ApiResponse<ShipmentChargeDto>.Ok(charge));
    }
}
