using System.Data;
using AutoMapper;
using InnoClinic.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;

namespace ProfilesApi.Application.Services;

public sealed class AdministratorService : IAdministratorService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdministratorService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public AdministratorService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<AdministratorService> logger, IPublishEndpoint publishEndpoint)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint = publishEndpoint;
    }

    public async Task<AdministratorDto> CreateAdministratorAsync(CreateAdministratorDto createAdministratorDto, Guid createdById,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Creating new administrator account for Email: {Email}.", createAdministratorDto.Email);
            var emailExists = await _unitOfWork.Accounts.ExistsAsync(a => a.Email == createAdministratorDto.Email, ct);
            var numberExists = await _unitOfWork.Accounts.ExistsAsync(a => a.PhoneNumber == createAdministratorDto.PhoneNumber, ct);

            if (emailExists)
            {
                _logger.LogWarning("Administrator creation failed. Email {Email} is already in use.", createAdministratorDto.Email);
                throw new ConflictException("Email is already in use by another account.");
            }

            if (numberExists)
            {
                _logger.LogWarning("Administrator creation failed. Phone number {PhoneNumber} is already in use.", createAdministratorDto.PhoneNumber);
                throw new ConflictException("Phone number is already in use by another account.");
            }

            var officeExists = await _unitOfWork.Offices.ExistsAsync(o => o.Id == createAdministratorDto.OfficeId, ct);
            if (!officeExists)
            {
                _logger.LogWarning("Administrator creation failed. OfficeId {OfficeId} was not found.", createAdministratorDto.OfficeId);
                throw new NotFoundException($"Office with ID '{createAdministratorDto.OfficeId}' was not found.");
            }

            var account = _mapper.Map<Account>(createAdministratorDto);
            if (createdById == Guid.Empty)
            {
                createdById = account.Id;
            }
            account.CreatedBy = createdById;
            account.UpdatedBy = createdById;
            account.PasswordHash = _passwordHasher.HashPassword(createAdministratorDto.Password);

            var administrator = _mapper.Map<Administrator>(createAdministratorDto);

            administrator.AccountId = account.Id;
            administrator.Account = account;

            _unitOfWork.Accounts.Add(account);
            _unitOfWork.Administrators.Add(administrator);

            await _publishEndpoint.Publish<IStaffCreatedEvent>(new
            {
                AccountId = account.Id,
                Email = createAdministratorDto.Email,
                Password = createAdministratorDto.Password,
                Firstname = createAdministratorDto.Firstname,
                Lastname = createAdministratorDto.Lastname,
                Role = InnoClinic.Shared.Events.Roles.Administrator
            }, ct);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully created administrator with ID: {AdministratorId} for OfficeId: {OfficeId}.", administrator.Id, administrator.OfficeId);
            return _mapper.Map<AdministratorDto>(administrator);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task DeleteAdministratorAsync(Guid id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Deleting administrator with ID: {AdministratorId}", id);
            var administrator = await _unitOfWork.Administrators.GetWithDetailsAsync(id, ct);

            if (administrator == null)
            {
                _logger.LogWarning("Administrator deleting failed. Administrator with ID '{AdministratorId}' was not found.", id);
                throw new NotFoundException($"Administrator with ID '{id}' was not found.");
            }

            var totalAdminsCount = await _unitOfWork.Administrators.ExistsAsync(a => a.Id != id, ct);
            if (!totalAdminsCount)
            {
                _logger.LogWarning("Administrator deleting failed. Administrator with ID '{AdministratorId}' is the last in the system and cannot be deleted.", id);
                throw new ConflictException("Cannot delete the last administrator in the system.");
            }

            _unitOfWork.Administrators.Delete(administrator);
            _unitOfWork.Accounts.Delete(administrator.Account);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully deleted administrator with ID: {AdministratorId}", id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<AdministratorDto> EditAdministratorProfileAsync(EditAdministratorProfileDto editAdministratorProfileDto, Guid editedById,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Editing administrator with ID: {AdministratorId}.", editAdministratorProfileDto.Id);
            var administrator = await _unitOfWork.Administrators.GetWithDetailsAsync(editAdministratorProfileDto.Id, ct);

            if (administrator == null)
            {
                _logger.LogWarning("Administrator editing failed. Administrator with ID '{AdministratorId}' was not found.", editAdministratorProfileDto.Id);
                throw new NotFoundException($"Administrator with ID '{editAdministratorProfileDto.Id}' was not found.");
            }

            var officeExists = await _unitOfWork.Offices.ExistsAsync(o => o.Id == editAdministratorProfileDto.OfficeId, ct);
            if (!officeExists)
            {
                _logger.LogWarning("Administrator editing failed. OfficeId {OfficeId} was not found.", editAdministratorProfileDto.OfficeId);
                throw new NotFoundException($"Office with ID '{editAdministratorProfileDto.OfficeId}' was not found.");
            }

            var phoneExists = await _unitOfWork.Accounts.ExistsAsync(
                a => a.Id != administrator.AccountId && a.PhoneNumber == editAdministratorProfileDto.PhoneNumber, ct);
            if (phoneExists)
            {
                _logger.LogWarning("Administrator editing failed. Phone number {PhoneNumber} is already in use.", editAdministratorProfileDto.PhoneNumber);
                throw new ConflictException("Phone number is already in use by another account.");
            }

            var emailExists = await _unitOfWork.Accounts.ExistsAsync(
                a => a.Id != administrator.AccountId && a.Email == editAdministratorProfileDto.Email, ct);
            if (emailExists)
            {
                _logger.LogWarning("Administrator editing failed. Email {Email} is already in use.", editAdministratorProfileDto.Email);
                throw new ConflictException("Email is already in use by another account.");
            }

            _mapper.Map(editAdministratorProfileDto, administrator);

            administrator.Account.UpdatedBy = editedById;

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Administrator with ID: {AdministratorId} successfully updated.", administrator.Id);
            return _mapper.Map<AdministratorDto>(administrator);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<AdministratorDto> GetAdministratorAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get administrator with ID: {AdministratorId}", id);
        var administrator = await _unitOfWork.Administrators.GetWithDetailsAsync(id, ct);

        if (administrator == null)
        {
            _logger.LogWarning("Administrator getting failed. Administrator with ID '{AdministratorId}' was not found.", id);
            throw new NotFoundException($"Administrator with ID '{id}' was not found.");
        }

        _logger.LogInformation("Administrator with ID: {AdministratorId} successfully retrieved.", id);
        return _mapper.Map<AdministratorDto>(administrator);
    }

    public async Task<IEnumerable<AdministratorDto>> GetAdministratorsAsync(
        SearchFilteredAdministratorListDto? filteredAdministratorListDto,
        CancellationToken ct = default)
    {
        var searchTerm = filteredAdministratorListDto?.SearchTerm?.Trim().ToLower();
        var officeId = filteredAdministratorListDto?.OfficeId;

        _logger.LogInformation("Fetching administrators with SearchTerm: {SearchTerm}, OfficeId: {OfficeId}", searchTerm, officeId);

        var administrators = (await _unitOfWork.Administrators.GetAllAsync(
            filter: a =>
                (!officeId.HasValue || a.OfficeId == officeId.Value) &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 a.Account.Firstname.ToLower().Contains(searchTerm) ||
                 a.Account.Lastname.ToLower().Contains(searchTerm) ||
                 (a.Account.Firstname + " " + a.Account.Lastname).ToLower().Contains(searchTerm) ||
                 (a.Account.Lastname + " " + a.Account.Firstname).ToLower().Contains(searchTerm)),

            cancellationToken: ct,

            includesProperties:
            [
                a => a.Account,
                a => a.Office
            ]
        )).ToList();

        _logger.LogInformation("Retrieved {Count} administrator(s) matching filter", administrators.Count);
        return _mapper.Map<IEnumerable<AdministratorDto>>(administrators);
    }

    public async Task<PagedResult<AdministratorDto>> GetAdministratorsPagedAsync(
        SearchPagedAdministratorDto searchPagedAdministratorDto,
        CancellationToken ct = default)
    {
        var searchTerm = searchPagedAdministratorDto?.SearchTerm?.Trim().ToLower();
        var officeId = searchPagedAdministratorDto?.OfficeId;
        var pageNumber = searchPagedAdministratorDto?.PageNumber ?? 1;
        var pageSize = searchPagedAdministratorDto?.PageSize ?? 10;

        _logger.LogInformation(
            "Fetching paged administrators. PageNumber: {PageNumber}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, OfficeId: {OfficeId}",
            pageNumber, pageSize, searchTerm, officeId);

        var (administrators, totalCount) = await _unitOfWork.Administrators.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: a =>
                (!officeId.HasValue || a.OfficeId == officeId.Value) &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 a.Account.Firstname.ToLower().Contains(searchTerm) ||
                 a.Account.Lastname.ToLower().Contains(searchTerm) ||
                 (a.Account.Firstname + " " + a.Account.Lastname).ToLower().Contains(searchTerm) ||
                 (a.Account.Lastname + " " + a.Account.Firstname).ToLower().Contains(searchTerm)),
            cancellationToken: ct,
            includesProperties:
            [
                a => a.Account,
                a => a.Office
            ]
        );

        _logger.LogInformation(
            "Retrieved page {PageNumber} of administrators ({ItemCount} item(s) on this page, {TotalCount} total matching)",
            pageNumber, administrators.Count(), totalCount);

        var dtos = _mapper.Map<IEnumerable<AdministratorDto>>(administrators);

        return new PagedResult<AdministratorDto>(
            items: dtos,
            totalCount: totalCount,
            pageNumber: pageNumber,
            pageSize: pageSize);
    }

    public async Task<AdministratorDto> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get administrator with account ID: {AccountId}", accountId);
        var administrator = await _unitOfWork.Administrators.GetByAccountIdAsync(accountId, ct);

        if (administrator == null)
        {
            _logger.LogWarning("Administrator getting failed. Administrator with account ID '{AccountId}' was not found.", accountId);
            throw new NotFoundException($"Administrator with account ID '{accountId}' was not found.");
        }

        _logger.LogInformation("Administrator with account ID: {AccountId} successfully retrieved.", accountId);
        return _mapper.Map<AdministratorDto>(administrator);
    }
}