using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Dto.Specializations;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;

namespace ProfilesApi.Application.Services;

public sealed class SpecializationService : ISpecializationService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SpecializationService> _logger;

    public SpecializationService(IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<SpecializationService> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SpecializationDto> CreateSpecializationAsync(CreateSpecializationDto createSpecializationDto, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Creating new specialization with Name: {Name}.", createSpecializationDto.Name);
            var specialization = _mapper.Map<Specialization>(createSpecializationDto);

            bool alreadyExists =
                await _unitOfWork.Specializations.ExistsAsync(
                    s => s.Name.ToLower() == createSpecializationDto.Name.ToLower(), ct);

            if (alreadyExists)
            {
                _logger.LogWarning("Specialization creation failed. Specialization with Name {Name} already exists.", createSpecializationDto.Name);
                throw new ConflictException("Specialization already exists");
            }

            _unitOfWork.Specializations.Add(specialization);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully created specialization with ID: {SpecializationId}.", specialization.Id);
            return _mapper.Map<SpecializationDto>(specialization);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<IEnumerable<SpecializationDto>> GetSpecializationsAsync(
        SearchQueryDto? searchQueryDto = null,
        CancellationToken ct = default)
    {
        var searchTerm = searchQueryDto?.SearchTerm?.Trim().ToLower();

        _logger.LogInformation("Fetching specializations with SearchTerm: {SearchTerm}", searchTerm);

        List<Specialization> specializations;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            specializations = (await _unitOfWork.Specializations.SearchByTerm(searchTerm, ct)).ToList();
        }
        else
        {
            specializations = (await _unitOfWork.Specializations.GetAllAsync(cancellationToken: ct)).ToList();
        }

        _logger.LogInformation("Retrieved {Count} specialization(s) matching filter", specializations.Count);
        return _mapper.Map<IEnumerable<SpecializationDto>>(specializations);
    }

    public async Task<SpecializationDto> GetSpecializationByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get specialization with ID: {SpecializationId}", id);
        var specialization = await _unitOfWork.Specializations.GetByIdAsync(id, ct);

        if (specialization == null)
        {
            _logger.LogWarning("Failed to retrieve specialization. Specialization with ID '{SpecializationId}' was not found.", id);
            throw new NotFoundException($"Specialization with ID '{id}' was not found.");
        }

        _logger.LogInformation("Specialization with ID: {SpecializationId} successfully retrieved.", id);
        return _mapper.Map<SpecializationDto>(specialization);
    }

    public async Task<PagedResult<SpecializationDto>> GetSpecializationsPagedAsync(
        SearchPagedSpecializationDto searchPagedSpecializationDto,
        CancellationToken ct = default)
    {
        var searchTerm = searchPagedSpecializationDto?.SearchTerm?.Trim().ToLower();
        var pageNumber = searchPagedSpecializationDto?.PageNumber ?? 1;
        var pageSize = searchPagedSpecializationDto?.PageSize ?? 10;

        _logger.LogInformation(
            "Fetching paged specializations. PageNumber: {PageNumber}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
            pageNumber, pageSize, searchTerm);

        var (specializations, totalCount) = await _unitOfWork.Specializations.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: s => string.IsNullOrWhiteSpace(searchTerm) ||
                         s.Name.ToLower().Contains(searchTerm),
            cancellationToken: ct
        );

        _logger.LogInformation(
            "Retrieved page {PageNumber} of specializations ({ItemCount} item(s) on this page, {TotalCount} total matching)",
            pageNumber, specializations.Count(), totalCount);

        var dtos = _mapper.Map<IEnumerable<SpecializationDto>>(specializations);

        return new PagedResult<SpecializationDto>(
            items: dtos,
            totalCount: totalCount,
            pageNumber: pageNumber,
            pageSize: pageSize);
    }

    public async Task DeleteSpecializationAsync(Guid id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Deleting specialization with ID: {SpecializationId}", id);
            var specialization = await _unitOfWork.Specializations.GetByIdAsync(id, ct);

            if (specialization == null)
            {
                _logger.LogWarning("Failed to delete specialization. Specialization with ID '{SpecializationId}' was not found.", id);
                throw new NotFoundException($"Specialization with ID '{id}' was not found.");
            }

            var hasAssociatedDoctors = await _unitOfWork.Doctors.ExistsAsync(d => d.SpecializationId == id, ct);

            if (hasAssociatedDoctors)
            {
                _logger.LogWarning("Failed to delete specialization. SpecializationId {SpecializationId} is assigned to active doctors.", id);
                throw new ConflictException(
                    "Cannot delete specialization because it is assigned to one or more doctors.");
            }

            _unitOfWork.Specializations.Delete(specialization);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully deleted specialization with ID: {SpecializationId}", id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task EditSpecializationAsync(EditSpecializationInformationDto editSpecializationInformationDto,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Editing specialization with ID: {SpecializationId}.", editSpecializationInformationDto.Id);
            var existingSpecialization =
                await _unitOfWork.Specializations.GetByIdAsync(editSpecializationInformationDto.Id, ct);

            if (existingSpecialization == null)
            {
                _logger.LogWarning("Failed to edit specialization. Specialization with ID '{SpecializationId}' was not found.", editSpecializationInformationDto.Id);
                throw new NotFoundException("Specialization was not found.");
            }

            _mapper.Map(editSpecializationInformationDto, existingSpecialization);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Specialization with ID: {SpecializationId} successfully updated.", editSpecializationInformationDto.Id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}