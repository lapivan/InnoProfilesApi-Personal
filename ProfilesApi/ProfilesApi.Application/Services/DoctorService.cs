using System.Data;
using AutoMapper;
using InnoClinic.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;

namespace ProfilesApi.Application.Services;

public sealed class DoctorService : IDoctorService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DoctorService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public DoctorService(IMapper mapper,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<DoctorService> logger, IPublishEndpoint publishEndpoint)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint = publishEndpoint;
    }

    public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto, Guid? createdById = null, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Creating new doctor account for Email: {Email}.", createDoctorDto.Email);
            var emailExists = await _unitOfWork.Accounts.ExistsAsync(a => a.Email == createDoctorDto.Email, ct);
            var numberExists = await _unitOfWork.Accounts.ExistsAsync(a => a.PhoneNumber == createDoctorDto.PhoneNumber, ct);

            if (emailExists)
            {
                _logger.LogWarning("Doctor creation failed. Email {Email} is already in use.", createDoctorDto.Email);
                throw new ConflictException("Email is already in use by another account.");
            }

            if (numberExists)
            {
                _logger.LogWarning("Doctor creation failed. Phone number {PhoneNumber} is already in use.", createDoctorDto.PhoneNumber);
                throw new ConflictException("Phone number is already in use by another account.");
            }

            var officeExists = await _unitOfWork.Offices.ExistsAsync(o => o.Id == createDoctorDto.OfficeId, ct);
            if (!officeExists)
            {
                _logger.LogWarning("Doctor creation failed. OfficeId {OfficeId} was not found.", createDoctorDto.OfficeId);
                throw new NotFoundException($"Office with ID '{createDoctorDto.OfficeId}' was not found.");
            }

            var specializationExists = await _unitOfWork.Specializations.ExistsAsync(o => o.Id == createDoctorDto.SpecializationId, ct);
            if (!specializationExists)
            {
                _logger.LogWarning("Doctor creation failed. SpecializationId {SpecializationId} was not found.", createDoctorDto.SpecializationId);
                throw new NotFoundException($"Specialization with ID '{createDoctorDto.SpecializationId}' was not found.");
            }

            var account = _mapper.Map<Account>(createDoctorDto);

            if (!createdById.HasValue || createdById == Guid.Empty)
            {
                createdById = account.Id;
            }

            account.CreatedBy = createdById.Value;
            account.UpdatedBy = createdById.Value;
            account.PasswordHash = _passwordHasher.HashPassword(createDoctorDto.Password);

            var doctor = _mapper.Map<Doctor>(createDoctorDto);

            doctor.AccountId = account.Id;
            doctor.Account = account;

            _unitOfWork.Accounts.Add(account);
            _unitOfWork.Doctors.Add(doctor);

            await _publishEndpoint.Publish<IStaffCreatedEvent>(new
            {
                AccountId = account.Id,
                Email = createDoctorDto.Email,
                Password = createDoctorDto.Password,
                Firstname = createDoctorDto.Firstname,
                Lastname = createDoctorDto.Lastname,
                Role = InnoClinic.Shared.Events.Roles.Doctor
            }, ct);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully created doctor with ID: {DoctorId} for OfficeId: {OfficeId}.", doctor.Id, doctor.OfficeId);
            return _mapper.Map<DoctorDto>(doctor);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task DeleteDoctorAsync(Guid id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Deleting doctor with ID: {DoctorId}", id);
            var doctor = await _unitOfWork.Doctors.GetWithDetailsAsync(id, ct);

            if (doctor == null)
            {
                _logger.LogWarning("Failed to delete doctor. Doctor with ID '{DoctorId}' was not found.", id);
                throw new NotFoundException($"Doctor with ID '{id}' was not found.");
            }

            _unitOfWork.Doctors.Delete(doctor);
            _unitOfWork.Accounts.Delete(doctor.Account);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully deleted doctor with ID: {DoctorId}", id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<DoctorDto> EditDoctorProfileAsync(EditDoctorProfileDto editDoctorProfileDto, Guid? editedById = null,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Editing doctor with ID: {DoctorId}.", editDoctorProfileDto.Id);
            var doctor = await _unitOfWork.Doctors.GetWithDetailsAsync(editDoctorProfileDto.Id, ct);

            if (doctor == null)
            {
                _logger.LogWarning("Failed to edit doctor. Doctor with ID '{DoctorId}' was not found.", editDoctorProfileDto.Id);
                throw new NotFoundException($"Doctor with ID '{editDoctorProfileDto.Id}' was not found.");
            }

            var officeExists = await _unitOfWork.Offices.ExistsAsync(o => o.Id == editDoctorProfileDto.OfficeId, ct);
            if (!officeExists)
            {
                _logger.LogWarning("Failed to edit doctor. OfficeId {OfficeId} was not found.", editDoctorProfileDto.OfficeId);
                throw new NotFoundException($"Office with ID '{editDoctorProfileDto.OfficeId}' was not found.");
            }

            var specializationExists = await _unitOfWork.Specializations.ExistsAsync(o => o.Id == editDoctorProfileDto.SpecializationId, ct);
            if (!specializationExists)
            {
                _logger.LogWarning("Failed to edit doctor. SpecializationId {SpecializationId} was not found.", editDoctorProfileDto.SpecializationId);
                throw new NotFoundException($"Specialization with ID '{editDoctorProfileDto.SpecializationId}' was not found.");
            }

            var phoneExists = await _unitOfWork.Accounts.ExistsAsync(
                a => a.Id != doctor.AccountId && a.PhoneNumber == editDoctorProfileDto.PhoneNumber, ct);
            if (phoneExists)
            {
                _logger.LogWarning("Failed to edit doctor. Phone number {PhoneNumber} is already in use.", editDoctorProfileDto.PhoneNumber);
                throw new ConflictException("Phone number is already in use by another account.");
            }

            var emailExists = await _unitOfWork.Accounts.ExistsAsync(
                a => a.Id != doctor.AccountId && a.Email == editDoctorProfileDto.Email, ct);
            if (emailExists)
            {
                _logger.LogWarning("Failed to edit doctor. Email {Email} is already in use.", editDoctorProfileDto.Email);
                throw new ConflictException("Email is already in use by another account.");
            }

            _mapper.Map(editDoctorProfileDto, doctor);

            doctor.Account.UpdatedBy = editedById ?? doctor.Account.Id;

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Doctor with ID: {DoctorId} successfully updated.", doctor.Id);
            return _mapper.Map<DoctorDto>(doctor);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<DoctorDto> GetDoctorAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get doctor with ID: {DoctorId}", id);
        var doctor = await _unitOfWork.Doctors.GetWithDetailsAsync(id, ct);

        if (doctor == null)
        {
            _logger.LogWarning("Failed to retrieve doctor. Doctor with ID '{DoctorId}' was not found.", id);
            throw new NotFoundException($"Doctor with ID '{id}' was not found.");
        }

        _logger.LogInformation("Doctor with ID: {DoctorId} successfully retrieved.", id);
        return _mapper.Map<DoctorDto>(doctor);
    }

    public async Task<IEnumerable<DoctorDto>> GetDoctorsAsync(SearchFilteredDoctorListDto? filteredDoctorListDto, CancellationToken ct = default)
    {
        var searchTerm = filteredDoctorListDto?.SearchTerm?.Trim().ToLower();
        var officeId = filteredDoctorListDto?.OfficeId;
        var specializationId = filteredDoctorListDto?.SpecializationId;
        var minExperienceInYears = filteredDoctorListDto?.MinExperienceYears;

        DateTime? maxCareerStartDate = minExperienceInYears.HasValue
            ? DateTime.UtcNow.AddYears(-minExperienceInYears.Value)
            : null;

        _logger.LogInformation("Fetching doctors with SearchTerm: {SearchTerm}, OfficeId: {OfficeId}, SpecializationId: {SpecializationId}", searchTerm, officeId, specializationId);

        var doctors = (await _unitOfWork.Doctors.GetAllAsync(
            filter: d =>
                (!maxCareerStartDate.HasValue || d.CareerStartDate.AddMonths(d.GapInMonths) <= maxCareerStartDate.Value) &&
                (!specializationId.HasValue || d.SpecializationId == specializationId.Value) &&
                (!officeId.HasValue || d.OfficeId == officeId.Value) &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 d.Account.Firstname.ToLower().Contains(searchTerm) ||
                 d.Account.Lastname.ToLower().Contains(searchTerm) ||
                 (d.Account.Firstname + " " + d.Account.Lastname).ToLower().Contains(searchTerm) ||
                 (d.Account.Lastname + " " + d.Account.Firstname).ToLower().Contains(searchTerm)),

            cancellationToken: ct,

            includesProperties:
            [
                d => d.Account,
                d => d.Office,
                d => d.Specialization
            ]
        )).ToList();

        _logger.LogInformation("Retrieved {Count} doctor(s) matching filter", doctors.Count);
        return _mapper.Map<IEnumerable<DoctorDto>>(doctors);
    }

    public async Task<PagedResult<DoctorDto>> GetDoctorsPagedAsync(
    SearchPagedDoctorDto searchPagedDoctorDto,
    CancellationToken ct = default)
    {
        var searchTerm = searchPagedDoctorDto?.SearchTerm?.Trim().ToLower();
        var officeId = searchPagedDoctorDto?.OfficeId;
        var specializationId = searchPagedDoctorDto?.SpecializationId;
        var minExperienceInYears = searchPagedDoctorDto?.MinExperienceYears;
        var pageNumber = searchPagedDoctorDto?.PageNumber ?? 1;
        var pageSize = searchPagedDoctorDto?.PageSize ?? 10;

        DateTime? maxCareerStartDate = minExperienceInYears.HasValue
            ? DateTime.UtcNow.AddYears(-minExperienceInYears.Value)
            : null;

        _logger.LogInformation(
            "Fetching paged doctors. PageNumber: {PageNumber}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, OfficeId: {OfficeId}, SpecializationId: {SpecializationId}",
            pageNumber, pageSize, searchTerm, officeId, specializationId);

        var (doctors, totalCount) = await _unitOfWork.Doctors.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: d =>
                (!maxCareerStartDate.HasValue || d.CareerStartDate.AddMonths(d.GapInMonths) <= maxCareerStartDate.Value) &&
                (!specializationId.HasValue || d.SpecializationId == specializationId.Value) &&
                (!officeId.HasValue || d.OfficeId == officeId.Value) &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 d.Account.Firstname.ToLower().Contains(searchTerm) ||
                 d.Account.Lastname.ToLower().Contains(searchTerm) ||
                 (d.Account.Firstname + " " + d.Account.Lastname).ToLower().Contains(searchTerm) ||
                 (d.Account.Lastname + " " + d.Account.Firstname).ToLower().Contains(searchTerm)),
            cancellationToken: ct,
            includesProperties:
            [
                d => d.Account,
            d => d.Office,
            d => d.Specialization
            ]
        );

        _logger.LogInformation(
            "Retrieved page {PageNumber} of doctors ({ItemCount} item(s) on this page, {TotalCount} total matching)",
            pageNumber, doctors.Count(), totalCount);

        var dtos = _mapper.Map<IEnumerable<DoctorDto>>(doctors);

        return new PagedResult<DoctorDto>(
            items: dtos,
            totalCount: totalCount,
            pageNumber: pageNumber,
            pageSize: pageSize);
    }

    public async Task<DoctorDto> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get doctor with account ID: {AccountId}", accountId);
        var doctor = await _unitOfWork.Doctors.GetByAccountIdAsync(accountId, ct);

        if (doctor == null)
        {
            _logger.LogWarning("Failed to retrieve doctor. Doctor with account ID '{AccountId}' was not found.", accountId);
            throw new NotFoundException($"Doctor with account ID '{accountId}' was not found.");
        }

        _logger.LogInformation("Doctor with account ID: {AccountId} successfully retrieved.", accountId);
        return _mapper.Map<DoctorDto>(doctor);
    }
}