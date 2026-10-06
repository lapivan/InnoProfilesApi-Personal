using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfilesApi.API.Constants;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ProfilesApi.API.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AdministratorsController : ControllerBase
{
    private readonly IAdministratorService _administratorService;

    public AdministratorsController(IAdministratorService administratorService)
    {
        _administratorService = administratorService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Adds a new administrator",
        Description = "Registers a new system administrator with the specified details",
        OperationId = "AddAdministrator"
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Administrator was created successfully", typeof(AdministratorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Office with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email or phone number is already in use by another account")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> CreateAdministrator([FromBody] CreateAdministratorDto createAdministratorDto, CancellationToken ct = default)
    {
        var createdById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _administratorService.CreateAdministratorAsync(createAdministratorDto, createdById, ct);
        return Created($"/administrators/{result.Id}", result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Deletes an administrator",
        Description = "Permanently removes a system administrator account by its unique identifier.",
        OperationId = "DeleteAdministrator"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Administrator was successfully deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Administrator with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Cannot delete the last administrator in the system")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> DeleteAdministrator([FromRoute] Guid id, CancellationToken ct = default)
    {
        await _administratorService.DeleteAdministratorAsync(id, ct);
        return NoContent();
    }

    [HttpPut]
    [SwaggerOperation(
        Summary = "Edits an administrator profile",
        Description = "Edits system administrators specified details",
        OperationId = "EditAdministrator"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Administrator profile was successfully edited", typeof(AdministratorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Administrator or office with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email or phone number is already in use by another account")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> EditAdministratorProfile([FromBody] EditAdministratorProfileDto editAdministratorProfileDto, CancellationToken ct = default)
    {
        var editedById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var editedAdministrator = await _administratorService.EditAdministratorProfileAsync(editAdministratorProfileDto, editedById, ct);
        return Ok(editedAdministrator);
    }

    [HttpGet("{administratorId:guid}")]
    [SwaggerOperation(
        Summary = "Gets an administrator by ID",
        Description = "Retrieves detailed information for a specific administrator using their unique identifier.",
        OperationId = "GetAdministratorById"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Administrator retrieved successfully", typeof(AdministratorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Administrator with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetAdministrator([FromRoute] Guid administratorId, CancellationToken ct = default)
    {
        var administrator = await _administratorService.GetAdministratorAsync(administratorId, ct);
        return Ok(administrator);
    }

    [HttpGet("accounts/{accountId:guid}")]
    [SwaggerOperation(
        Summary = "Gets an administrator by account ID",
        Description = "Retrieves administrator details associated with a specific user account ID.",
        OperationId = "GetAdministratorByAccountId"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Administrator retrieved successfully", typeof(AdministratorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Administrator with specified account ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetByAccountId([FromRoute] Guid accountId, CancellationToken ct = default)
    {
        var administrator = await _administratorService.GetByAccountIdAsync(accountId, ct);
        return Ok(administrator);
    }

    [HttpPost("search")]
    [SwaggerOperation(
        Summary = "Gets a list of administrators",
        Description = "Retrieves a paginated and filtered list of administrators based on search parameters.",
        OperationId = "GetAdministrators"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of administrators retrieved successfully", typeof(IEnumerable<AdministratorDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetAdministrators(
        [FromBody] SearchFilteredAdministratorListDto filteredAdministratorListDto, CancellationToken ct = default)
    {
        var administrators = await _administratorService.GetAdministratorsAsync(filteredAdministratorListDto, ct);
        return Ok(administrators);
    }

    [HttpPost("search/paged")]
    [SwaggerOperation(
        Summary = "Gets a paged list of administrators",
        Description = "Retrieves a paginated and filtered list of administrators based on search parameters.",
        OperationId = "GetAdministratorsPaged"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged list of administrators retrieved successfully", typeof(PagedResult<AdministratorDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid search or filter parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetAdministratorsPaged(
        [FromBody] SearchPagedAdministratorDto searchPagedAdministratorDto, CancellationToken ct = default)
    {
        var pagedAdministrators = await _administratorService.GetAdministratorsPagedAsync(searchPagedAdministratorDto, ct);
        return Ok(pagedAdministrators);
    }
}