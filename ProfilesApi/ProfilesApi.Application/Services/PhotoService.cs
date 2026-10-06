using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Photos;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;

namespace ProfilesApi.Application.Services;

public sealed class PhotoService : IPhotoService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(IMapper mapper,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ILogger<PhotoService> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PhotoDto> UploadPhotoAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading new photo with FileName: {FileName}.", fileName);

        var photoUrl = await _fileStorageService.UploadPhotoAsync(fileStream, fileName, ct);

        var photo = new Photo(photoUrl);

        _unitOfWork.Photos.Add(photo);
        await _unitOfWork.CompleteAsync(ct);

        _logger.LogInformation("Successfully uploaded photo with ID: {PhotoId}.", photo.Id);
        return _mapper.Map<PhotoDto>(photo);
    }

    public async Task DeletePhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Deleting photo with ID: {PhotoId}", photoId);
            var photo = await _unitOfWork.Photos.GetByIdAsync(photoId, ct);

            if (photo == null)
            {
                _logger.LogWarning("Failed to delete photo. Photo with ID '{PhotoId}' was not found.", photoId);
                throw new NotFoundException($"Photo with ID '{photoId}' was not found.");
            }

            // Важная логика удаления физического файла из твоей ветки improvement
            if (!string.IsNullOrWhiteSpace(photo.Url))
            {
                _fileStorageService.DeletePhoto(photo.Url, ct);
            }

            _unitOfWork.Photos.Delete(photo);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully deleted photo with ID: {PhotoId}", photoId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType)> GetPhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Trying to get photo stream with ID: {PhotoId}", photoId);
            var photo = await _unitOfWork.Photos.GetByIdAsync(photoId, ct);

            if (photo == null)
            {
                _logger.LogWarning("Failed to retrieve photo. Photo with ID '{PhotoId}' was not found.", photoId);
                throw new NotFoundException($"Photo with ID '{photoId}' was not found.");
            }

            var fileName = Path.GetFileName(photo.Url);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Failed to retrieve photo. Invalid or empty filename in URL for photo ID '{PhotoId}'.", photoId);
                throw new NotFoundException($"Photo with ID '{photoId}' was not found.");
            }

            var fileResult = await _fileStorageService.GetPhotoAsync(fileName, ct);
            if (fileResult == null)
            {
                _logger.LogWarning("Failed to retrieve photo stream. Physical file for photo ID '{PhotoId}' was not found on storage.", photoId);
                throw new NotFoundException($"Physical file for photo ID '{photoId}' was not found on storage.");
            }

            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Photo stream for ID: {PhotoId} successfully retrieved.", photoId);
            return fileResult.Value;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}