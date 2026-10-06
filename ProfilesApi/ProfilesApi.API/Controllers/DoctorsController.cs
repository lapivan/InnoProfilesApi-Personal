using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfilesApi.API.Constants;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ProfilesApi.API.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Authorize]
public sealed class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Adds a new doctor",
        Description = "Registers a new system doctor with the specified details",
        OperationId = "AddDoctor"
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Doctor was created successfully", typeof(DoctorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Office or specialization with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email or phone number is already in use by another account")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto createDoctorDto, CancellationToken ct = default)
    {
        var createdById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _doctorService.CreateDoctorAsync(createDoctorDto, createdById, ct);
        return Created($"/doctors/{result.Id}", result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [SwaggerOperation(
        Summary = "Deletes an doctor",
        Description = "Permanently removes a system doctor account by its unique identifier.",
        OperationId = "DeleteDoctor"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Doctor was successfully deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Doctor with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> DeleteDoctor([FromRoute] Guid id, CancellationToken ct = default)
    {
        await _doctorService.DeleteDoctorAsync(id, ct);
        return NoContent();
    }

    [HttpPut]
    [Authorize(Policy = AuthPolicies.RequireStaff)]
    [SwaggerOperation(
        Summary = "Edits an doctor profile",
        Description = "Edits system doctor specified details",
        OperationId = "EditDoctor"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Doctor profile was successfully edited", typeof(DoctorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Doctor, office, or specialization with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email or phone number is already in use by another account")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> EditDoctorProfile([FromBody] EditDoctorProfileDto editDoctorProfileDto, CancellationToken ct = default)
    {
        var editedById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var editedDoctor = await _doctorService.EditDoctorProfileAsync(editDoctorProfileDto, editedById, ct);
        return Ok(editedDoctor);
    }

    [HttpGet("{doctorId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets an doctor by ID",
        Description = "Retrieves detailed information for a specific doctor using their unique identifier.",
        OperationId = "GetDoctorById"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Doctor retrieved successfully", typeof(DoctorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Doctor with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetDoctor([FromRoute] Guid doctorId, CancellationToken ct = default)
    {
        var doctor = await _doctorService.GetDoctorAsync(doctorId, ct);
        return Ok(doctor);
    }

    [HttpGet("accounts/{accountId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets an doctor by account ID",
        Description = "Retrieves doctor details associated with a specific user account ID.",
        OperationId = "GetDoctorByAccountId"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Doctor retrieved successfully", typeof(DoctorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Doctor with specified account ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetByAccountId([FromRoute] Guid accountId, CancellationToken ct = default)
    {
        var doctor = await _doctorService.GetByAccountIdAsync(accountId, ct);
        return Ok(doctor);
    }

    [HttpPost("search")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a list of doctors",
        Description = "Retrieves a paginated and filtered list of doctors based on search parameters.",
        OperationId = "GetDoctors"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of doctors retrieved successfully", typeof(IEnumerable<DoctorDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetDoctors(
        [FromBody] SearchFilteredDoctorListDto filteredDoctorListDto, CancellationToken ct = default)
    {
        var doctors = await _doctorService.GetDoctorsAsync(filteredDoctorListDto, ct);
        return Ok(doctors);
    }

    [HttpPost("search/paged")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a paged list of doctors",
        Description = "Retrieves a paginated and filtered list of doctors based on search parameters.",
        OperationId = "GetDoctorsPaged"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged list of doctors retrieved successfully", typeof(PagedResult<DoctorDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetDoctorsPaged(
        [FromBody] SearchPagedDoctorDto searchPagedDoctorDto, CancellationToken ct = default)
    {
        var pagedDoctors = await _doctorService.GetDoctorsPagedAsync(searchPagedDoctorDto, ct);
        return Ok(pagedDoctors);
    }
}