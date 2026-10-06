using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfilesApi.API.Constants;
using ProfilesApi.Application.Dto.Photos;
using ProfilesApi.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ProfilesApi.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public PhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    [HttpDelete("{photoId:guid}")]
    [Consumes("application/json")]
    [Authorize(Policy = AuthPolicies.RequireAllRoles)]
    [SwaggerOperation(
        Summary = "Deletes a photo",
        Description = "Permanently removes a photo by its unique identifier.",
        OperationId = "DeletePhoto"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Photo was successfully deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Photo with specified ID was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> DeletePhoto([FromRoute] Guid photoId, CancellationToken ct = default)
    {
        await _photoService.DeletePhotoAsync(photoId, ct);
        return NoContent();
    }

    [HttpGet("{photoId:guid}")]
    [Consumes("application/json")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Gets a photo by ID",
        Description = "Retrieves detailed information for a specific photo using its unique identifier.",
        OperationId = "GetPhotoById"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Photo stream retrieved successfully", typeof(FileStreamResult))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Photo record or physical file was not found")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> GetPhoto([FromRoute] Guid photoId, CancellationToken ct = default)
    {
        var photo = await _photoService.GetPhotoAsync(photoId, ct);
        return File(photo.Stream, photo.ContentType);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = AuthPolicies.RequireAllRoles)]
    [SwaggerOperation(
        Summary = "Uploads a photo",
        Description = "Uploads a new photo file to the system.",
        OperationId = "UploadPhoto"
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Photo was uploaded successfully", typeof(PhotoDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid file format or missing photo file")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();

        var result = await _photoService.UploadPhotoAsync(
            stream,
            file.FileName,
            file.ContentType,
            ct);

        return Created($"/photos/{result.Id}", result);
    }
}