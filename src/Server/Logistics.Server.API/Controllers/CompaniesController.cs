using FluentValidation;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers;

/// <summary>
/// Manages company operations.
/// </summary>
public class CompaniesController : BaseApiController
{
    private readonly ICompanyService _companyService;
    private readonly IValidator<CreateCompanyRequest> _createValidator;

    public CompaniesController(ICompanyService companyService, IValidator<CreateCompanyRequest> createValidator)
    {
        _companyService = companyService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Gets a paginated list of companies.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CompanyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.GetCompaniesAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CompanyDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets a company by its public identifier.
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompany(Guid publicId, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetByPublicIdAsync(publicId, cancellationToken);
        if (company == null)
        {
            throw new NotFoundException("Company", publicId);
        }
        return Ok(ApiResponse<CompanyDto>.Ok(company));
    }

    /// <summary>
    /// Creates a new company.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

        var company = await _companyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCompany), new { publicId = company.PublicId }, ApiResponse<CompanyDto>.Ok(company));
    }
}
