using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfilesApi.API.Constants;
using ProfilesApi.Application.Dto.Offices;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ProfilesApi.API.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Authorize]
public sealed class OfficesController : ControllerBase
{
    private readonly IOfficeService _officeService;

    public OfficesController(IOfficeService officeService)
    {
        _officeService = officeService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Adds a new office",
        Description = "Registers a new office with the specified details.",
        OperationId = "CreateOffice"
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Office was created successfully", typeof(OfficeDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Office with this address or phone number already exists")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> CreateOffice([FromBody] CreateOfficeDto createOfficeDto, CancellationToken ct = default)
    {
        var result = await _officeService.CreateOfficeAsync(createOfficeDto, ct);
        return Created($"/offices/{result.Id}", result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Deletes an office",
        Description = "Permanently removes an office by its unique identifier.",
        OperationId = "DeleteOffice"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Office was successfully deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Office with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Cannot delete office because staff members are assigned to it")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> DeleteOffice([FromRoute] Guid id, CancellationToken ct = default)
    {
        await _officeService.DeleteOfficeAsync(id, ct);
        return NoContent();
    }

    [HttpPut]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Edits office information",
        Description = "Edits specified office details.",
        OperationId = "EditOffice"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Office was successfully edited")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Office with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Another office with this address or phone number already exists")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> EditOffice([FromBody] EditOfficeInformationDto editOfficeInformationDto, CancellationToken ct = default)
    {
        await _officeService.EditOfficeAsync(editOfficeInformationDto, ct);
        return NoContent();
    }

    [HttpGet("{officeId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets an office by ID",
        Description = "Retrieves detailed information for a specific office using its unique identifier.",
        OperationId = "GetOfficeById"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Office retrieved successfully", typeof(OfficeDto))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Office with specified ID does not exist")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetOffice([FromRoute] Guid officeId, CancellationToken ct = default)
    {
        var office = await _officeService.GetOfficeByIdAsync(officeId, ct);
        return Ok(office);
    }

    [HttpPost("search")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a list of offices",
        Description = "Retrieves a paginated and filtered list of offices based on search parameters.",
        OperationId = "GetOffices"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of offices retrieved successfully", typeof(IEnumerable<OfficeDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetOffices(
        [FromBody] SearchQueryDto filteredOfficeListDto, CancellationToken ct = default)
    {
        var offices = await _officeService.GetOfficeListAsync(filteredOfficeListDto, ct);
        return Ok(offices);
    }

    [HttpPost("search/paged")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a paged list of offices",
        Description = "Retrieves a paginated and filtered list of offices based on search parameters.",
        OperationId = "GetOfficesPaged"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged list of offices retrieved successfully", typeof(PagedResult<OfficeDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetOfficesPaged(
        [FromBody] SearchPagedOfficeDto pagedOfficeDto, CancellationToken ct = default)
    {
        var pagedOffices = await _officeService.GetOfficeListPagedAsync(pagedOfficeDto, ct);
        return Ok(pagedOffices);
    }
}