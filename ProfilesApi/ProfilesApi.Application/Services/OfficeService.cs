using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Offices;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;

namespace ProfilesApi.Application.Services;

public sealed class OfficeService : IOfficeService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OfficeService> _logger;

    public OfficeService(IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<OfficeService> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OfficeDto> CreateOfficeAsync(CreateOfficeDto createOfficeDto, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Creating new office at Address: {Address}.", createOfficeDto.Address);
            var office = _mapper.Map<Office>(createOfficeDto);

            var alreadyExists = await _unitOfWork.Offices.ExistsAsync(o => o.Address.ToLower() == createOfficeDto.Address.ToLower() || o.PhoneNumber == createOfficeDto.PhoneNumber, ct);

            if (alreadyExists)
            {
                _logger.LogWarning("Office creation failed. Office with address {Address} or phone number {PhoneNumber} already exists.", createOfficeDto.Address, createOfficeDto.PhoneNumber);
                throw new ConflictException("Office with this address or phone number already exists.");
            }

            // _unitOfWork.Offices.Add(office);
            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully created office with ID: {OfficeId}.", office.Id);
            return _mapper.Map<OfficeDto>(office);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<IEnumerable<OfficeDto>> GetOfficeListAsync(SearchQueryDto? searchQueryDto, CancellationToken ct = default)
    {
        var searchQuery = searchQueryDto?.SearchTerm?.Trim().ToLower();

        _logger.LogInformation("Fetching offices with SearchTerm: {SearchTerm}", searchQuery);

        var filteredOffices = (await _unitOfWork.Offices.GetAllAsync(o => string.IsNullOrEmpty(searchQuery)
                                                                          || o.Address.ToLower().Contains(searchQuery)
                                                                          || o.PhoneNumber.Contains(searchQuery), ct)).ToList();

        _logger.LogInformation("Retrieved {Count} office(s) matching filter", filteredOffices.Count);
        return _mapper.Map<IEnumerable<OfficeDto>>(filteredOffices);
    }

    public async Task<PagedResult<OfficeDto>> GetOfficeListPagedAsync(
        SearchPagedOfficeDto pagedOfficeDto,
        CancellationToken ct = default)
    {
        var searchQuery = pagedOfficeDto?.SearchTerm?.Trim().ToLower();
        var pageNumber = pagedOfficeDto?.PageNumber ?? 1;
        var pageSize = pagedOfficeDto?.PageSize ?? 10;

        _logger.LogInformation(
            "Fetching paged offices. PageNumber: {PageNumber}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
            pageNumber, pageSize, searchQuery);

        var (offices, totalCount) = await _unitOfWork.Offices.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: o => string.IsNullOrEmpty(searchQuery)
                         || o.Address.ToLower().Contains(searchQuery)
                         || o.PhoneNumber.Contains(searchQuery),
            cancellationToken: ct
        );

        _logger.LogInformation(
            "Retrieved page {PageNumber} of offices ({ItemCount} item(s) on this page, {TotalCount} total matching)",
            pageNumber, offices.Count(), totalCount);

        var dtos = _mapper.Map<IEnumerable<OfficeDto>>(offices);

        return new PagedResult<OfficeDto>(
            items: dtos,
            totalCount: totalCount,
            pageNumber: pageNumber,
            pageSize: pageSize);
    }

    public async Task<OfficeDto> GetOfficeByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get office with ID: {OfficeId}", id);
        var office = await _unitOfWork.Offices.GetByIdAsync(id, ct);

        if (office == null)
        {
            _logger.LogWarning("Failed to retrieve office. Office with ID '{OfficeId}' was not found.", id);
            throw new ConflictException($"Office with id {id} does not exist");
        }

        _logger.LogInformation("Office with ID: {OfficeId} successfully retrieved.", id);
        return _mapper.Map<OfficeDto>(office);
    }

    public async Task DeleteOfficeAsync(Guid id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Deleting office with ID: {OfficeId}", id);
            var office = await _unitOfWork.Offices.GetByIdAsync(id, ct);

            if (office == null)
            {
                _logger.LogWarning("Failed to delete office. Office with ID '{OfficeId}' was not found.", id);
                throw new NotFoundException($"Office with ID '{id}' was not found.");
            }

            var hasAssociatedDoctors = await _unitOfWork.Doctors.ExistsAsync(d => d.OfficeId == id, ct);
            var hasAssociatedAdministrators = await _unitOfWork.Administrators.ExistsAsync(a => a.OfficeId == id, ct);

            if (hasAssociatedDoctors || hasAssociatedAdministrators)
            {
                _logger.LogWarning("Failed to delete office. OfficeId {OfficeId} has assigned personnel.", id);
                throw new ConflictException("Cannot delete office because people work here.");
            }

            _unitOfWork.Offices.Delete(office);
            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully deleted office with ID: {OfficeId}", id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task EditOfficeAsync(EditOfficeInformationDto editOfficeInformationDto, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Editing office with ID: {OfficeId}.", editOfficeInformationDto.Id);
            var existingOffice = await _unitOfWork.Offices.GetByIdAsync(editOfficeInformationDto.Id, ct);

            if (existingOffice == null)
            {
                _logger.LogWarning("Failed to edit office. Office with ID '{OfficeId}' was not found.", editOfficeInformationDto.Id);
                throw new NotFoundException("Office was not found.");
            }

            var isDuplicate = await _unitOfWork.Offices.ExistsAsync(
                o => o.Id != editOfficeInformationDto.Id &&
                     (o.Address.ToLower() == editOfficeInformationDto.Address.ToLower() ||
                      o.PhoneNumber == editOfficeInformationDto.PhoneNumber),
                ct
            );

            if (isDuplicate)
            {
                _logger.LogWarning("Failed to edit office. Another office with address {Address} or phone number {PhoneNumber} already exists.", editOfficeInformationDto.Address, editOfficeInformationDto.PhoneNumber);
                throw new ConflictException("Another office with this address or phone number already exists.");
            }

            _mapper.Map(editOfficeInformationDto, existingOffice);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Office with ID: {OfficeId} successfully updated.", editOfficeInformationDto.Id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}