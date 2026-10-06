using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfilesApi.API.Constants;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Dto.Specializations;
using ProfilesApi.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ProfilesApi.API.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Authorize]
public sealed class SpecializationsController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationsController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Adds a new specialization",
        Description = "Registers a new specialization with the specified details.",
        OperationId = "CreateSpecialization"
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Specialization was created successfully", typeof(SpecializationDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Specialization with this name already exists")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> CreateSpecialization([FromBody] CreateSpecializationDto createSpecializationDto, CancellationToken ct = default)
    {
        var result = await _specializationService.CreateSpecializationAsync(createSpecializationDto, ct);
        return Created($"/specializations/{result.Id}", result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Deletes a specialization",
        Description = "Permanently removes a specialization by its unique identifier.",
        OperationId = "DeleteSpecialization"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Specialization was successfully deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Specialization with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Cannot delete specialization because it is assigned to one or more doctors")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> DeleteSpecialization([FromRoute] Guid id, CancellationToken ct = default)
    {
        await _specializationService.DeleteSpecializationAsync(id, ct);
        return NoContent();
    }

    [HttpPut]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Edits specialization information",
        Description = "Edits specified specialization details.",
        OperationId = "EditSpecialization"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Specialization was successfully edited")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Specialization with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> EditSpecialization([FromBody] EditSpecializationInformationDto editSpecializationInformationDto, CancellationToken ct = default)
    {
        await _specializationService.EditSpecializationAsync(editSpecializationInformationDto, ct);
        return NoContent();
    }

    [HttpGet("{specializationId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a specialization by ID",
        Description = "Retrieves detailed information for a specific specialization using its unique identifier.",
        OperationId = "GetSpecializationById"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Specialization retrieved successfully", typeof(SpecializationDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Specialization with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetSpecialization([FromRoute] Guid specializationId, CancellationToken ct = default)
    {
        var specialization = await _specializationService.GetSpecializationByIdAsync(specializationId, ct);
        return Ok(specialization);
    }

    [HttpPost("search")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a list of specializations",
        Description = "Retrieves a paginated and filtered list of specializations based on search parameters.",
        OperationId = "GetSpecializations"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of specializations retrieved successfully", typeof(IEnumerable<SpecializationDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetSpecializations(
        [FromBody] SearchQueryDto filteredSpecializationListDto, CancellationToken ct = default)
    {
        var specializations = await _specializationService.GetSpecializationsAsync(filteredSpecializationListDto, ct);
        return Ok(specializations);
    }

    [HttpPost("search/paged")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a paged list of specializations",
        Description = "Retrieves a paginated and filtered list of specializations based on search parameters.",
        OperationId = "GetSpecializationsPaged"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged list of specializations retrieved successfully", typeof(PagedResult<SpecializationDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetSpecializationsPaged(
        [FromBody] SearchPagedSpecializationDto searchPagedSpecializationDto, CancellationToken ct = default)
    {
        var pagedSpecializations = await _specializationService.GetSpecializationsPagedAsync(searchPagedSpecializationDto, ct);
        return Ok(pagedSpecializations);
    }
}