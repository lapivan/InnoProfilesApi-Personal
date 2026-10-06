using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Interfaces;

namespace ProfilesApi.Infrastructure.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _currentDirectory;
    private readonly PhysicalFileProvider _fileProvider;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var rawPath = configuration["FileStorageSettings:StoragePath"] ?? "wwwroot/photos";

        _currentDirectory = Path.GetFullPath(rawPath);
        if (!Directory.Exists(_currentDirectory))
        {
            Directory.CreateDirectory(_currentDirectory);
            _logger.LogInformation("Created physical file storage directory at path: {StoragePath}", _currentDirectory);
        }
        else
        {
            _logger.LogInformation("Initialized physical file storage at path: {StoragePath}", _currentDirectory);
        }

        _fileProvider = new PhysicalFileProvider(_currentDirectory);
    }

    public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading physical file for OriginalName: {OriginalFileName}", fileName);
        var extension = Path.GetExtension(fileName);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_currentDirectory, uniqueFileName);

        using var destinationStream = File.Create(fullPath);
        await fileStream.CopyToAsync(destinationStream, ct);

        var relativeUrl = $"/photos/{uniqueFileName}";
        _logger.LogInformation("Successfully saved file to disk. SavedFileName: {SavedFileName}, RelativeUrl: {RelativeUrl}", uniqueFileName, relativeUrl);

        return relativeUrl;
    }

    public void DeletePhoto(string fileUrl, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(fileUrl);
        var fullPath = Path.Combine(_currentDirectory, fileName);

        _logger.LogInformation("Deleting physical file: {FileName} from storage", fileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Successfully deleted file: {FileName} from disk", fileName);
        }
        else
        {
            _logger.LogWarning("Failed to delete physical file. File {FileName} was not found on disk at path: {FullPath}", fileName, fullPath);
        }
    }

    public async Task<(Stream Stream, string ContentType)?> GetPhotoAsync(string fileName, CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving physical file stream for FileName: {FileName}", fileName);
        IFileInfo fileInfo = _fileProvider.GetFileInfo(fileName);

        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Physical file request failed. File {FileName} does not exist on disk", fileName);
            return await Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        Stream stream = fileInfo.CreateReadStream();

        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType);

        var resolvedContentType = contentType ?? "application/octet-stream";
        _logger.LogInformation("Successfully opened file stream for FileName: {FileName} with ContentType: {ContentType}", fileName, resolvedContentType);

        return await Task.FromResult<(Stream Stream, string ContentType)?>((stream, resolvedContentType));
    }
}