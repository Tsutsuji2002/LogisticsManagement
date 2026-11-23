using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Organization;

/// <summary>
/// Company profile and organizational overview endpoints.
/// </summary>
[Route("api/v1/organization")]
public class OrganizationCompanyController : BaseApiController
{
    private readonly ICompanyService _companyService;
    private readonly IValidator<UpdateCompanyRequest> _updateValidator;

    public OrganizationCompanyController(
        ICompanyService companyService,
        IValidator<UpdateCompanyRequest> updateValidator)
    {
        _companyService = companyService;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Gets primary company details.
    /// </summary>
    [HttpGet("company")]
    [HasPermission(AppPermissions.Organization.CompanyRead)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompany(CancellationToken cancellationToken)
    {
        var company = await _companyService.GetPrimaryCompanyAsync(cancellationToken);
        if (company == null)
        {
            return NotFound(ApiResponse<CompanyDto>.Fail("No company record found."));
        }
        return Ok(ApiResponse<CompanyDto>.Ok(company));
    }

    /// <summary>
    /// Updates company information.
    /// </summary>
    [HttpPut("company")]
    [HasPermission(AppPermissions.Organization.CompanyUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var updated = await _companyService.UpdateCompanyAsync(request, cancellationToken);
        return Ok(ApiResponse<CompanyDto>.Ok(updated, "Company details updated successfully."));
    }

    /// <summary>
    /// Gets high-level organizational overview and aggregate entity counts.
    /// </summary>
    [HttpGet("overview")]
    [HasPermission(AppPermissions.Organization.OverviewRead)]
    [ProducesResponseType(typeof(ApiResponse<OrganizationOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await _companyService.GetOverviewAsync(cancellationToken);
        return Ok(ApiResponse<OrganizationOverviewDto>.Ok(overview));
    }
}
