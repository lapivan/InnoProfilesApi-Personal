using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;

namespace ProfilesApi.Application.Services;

public sealed class PatientService : IPatientService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IMapper mapper,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<PatientService> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PatientDto> CreatePatientAsync(
        RegisterPatientDto registerPatientDto,
        IRegistrationPublisher publisher,
        Guid? customAccountId = null,
        Guid? createdById = null,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Creating new patient account for Email: {Email}.", registerPatientDto.Email);

            if (customAccountId.HasValue)
            {
                var accountExists = await _unitOfWork.Accounts.ExistsAsync(a => a.Id == customAccountId.Value, ct);
                if (accountExists)
                {
                    _logger.LogWarning("Patient account with ID {AccountId} already exists. Skipping creation.", customAccountId.Value);
                    return await GetByAccountIdAsync(customAccountId.Value, ct);
                }
            }

            var emailExists = await _unitOfWork.Accounts.ExistsAsync(a => a.Email == registerPatientDto.Email, ct);
            var numberExists = await _unitOfWork.Accounts.ExistsAsync(a => a.PhoneNumber == registerPatientDto.PhoneNumber, ct);

            if (emailExists)
            {
                _logger.LogWarning("Patient creation failed. Email {Email} is already in use.", registerPatientDto.Email);
                throw new ConflictException("Email is already in use by another account.");
            }

            if (numberExists)
            {
                _logger.LogWarning("Patient creation failed. Phone number {PhoneNumber} is already in use.", registerPatientDto.PhoneNumber);
                throw new ConflictException("Phone number is already in use by another account.");
            }

            var patient = _mapper.Map<Patient>(registerPatientDto);
            var account = _mapper.Map<Account>(registerPatientDto);

            var accountId = customAccountId ?? account.Id;

            account.Id = accountId;
            account.CreatedBy = createdById ?? accountId;
            account.UpdatedBy = createdById ?? accountId;
            account.PasswordHash = _passwordHasher.HashPassword(registerPatientDto.Password);
            account.Role = Domain.Enums.Roles.Patient;

            patient.AccountId = accountId;
            patient.Account = account;

            _unitOfWork.Accounts.Add(account);
            _unitOfWork.Patients.Add(patient);

            await publisher.PublishCreatedAsync(account, registerPatientDto.Password, InnoClinic.Shared.Events.Roles.Patient, ct);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully created patient with ID: {PatientId}.", patient.Id);
            return _mapper.Map<PatientDto>(patient);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<PatientDto> GetPatientAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get patient with ID: {PatientId}", id);
        var patient = await _unitOfWork.Patients.GetWithDetailsAsync(id, ct);

        if (patient == null)
        {
            _logger.LogWarning("Failed to retrieve patient. Patient with ID '{PatientId}' was not found.", id);
            throw new NotFoundException($"Patient with ID '{id}' was not found.");
        }

        _logger.LogInformation("Patient with ID: {PatientId} successfully retrieved.", id);
        return _mapper.Map<PatientDto>(patient);
    }

    public async Task<IEnumerable<PatientDto>> GetPatientsAsync(SearchFilteredPatientListDto? filteredPatientListDto, CancellationToken ct = default)
    {
        var searchTerm = filteredPatientListDto?.SearchTerm?.Trim().ToLower();
        var phoneNumber = filteredPatientListDto?.PhoneNumber?.Trim();
        var email = filteredPatientListDto?.Email?.Trim();

        _logger.LogInformation("Fetching patients with SearchTerm: {SearchTerm}, PhoneNumber: {PhoneNumber}, Email: {Email}", searchTerm, phoneNumber, email);

        var patients = (await _unitOfWork.Patients.GetAllAsync(
            filter: a =>
                (string.IsNullOrWhiteSpace(phoneNumber) || a.Account.PhoneNumber.Contains(phoneNumber)) &&
                (string.IsNullOrWhiteSpace(email) || a.Account.Email.Contains(email)) &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 a.Account.Firstname.ToLower().Contains(searchTerm) ||
                 a.Account.Lastname.ToLower().Contains(searchTerm) ||
                 (a.Account.Firstname + " " + a.Account.Lastname).ToLower().Contains(searchTerm) ||
                 (a.Account.Lastname + " " + a.Account.Firstname).ToLower().Contains(searchTerm)),

            cancellationToken: ct,

            includesProperties:
            [
                a => a.Account,
            ]
        )).ToList();

        _logger.LogInformation("Retrieved {Count} patient(s) matching filter", patients.Count);
        return _mapper.Map<IEnumerable<PatientDto>>(patients);
    }

    public async Task<PagedResult<PatientDto>> GetPatientsPagedAsync(
        SearchPagedPatientDto searchPagedPatientDto,
        CancellationToken ct = default)
    {
        var searchTerm = searchPagedPatientDto?.SearchTerm?.Trim().ToLower();
        var pageNumber = searchPagedPatientDto?.PageNumber ?? 1;
        var pageSize = searchPagedPatientDto?.PageSize ?? 10;

        _logger.LogInformation(
            "Fetching paged patients. PageNumber: {PageNumber}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
            pageNumber, pageSize, searchTerm);

        var (patients, totalCount) = await _unitOfWork.Patients.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: p => string.IsNullOrWhiteSpace(searchTerm) ||
                         p.Account.Firstname.ToLower().Contains(searchTerm) ||
                         p.Account.Lastname.ToLower().Contains(searchTerm) ||
                         (p.Account.Firstname + " " + p.Account.Lastname).ToLower().Contains(searchTerm) ||
                         (p.Account.Lastname + " " + p.Account.Firstname).ToLower().Contains(searchTerm),
            cancellationToken: ct,
            includesProperties:
            [
                p => p.Account
            ]
        );

        _logger.LogInformation(
            "Retrieved page {PageNumber} of patients ({ItemCount} item(s) on this page, {TotalCount} total matching)",
            pageNumber, patients.Count(), totalCount);

        var dtos = _mapper.Map<IEnumerable<PatientDto>>(patients);

        return new PagedResult<PatientDto>(
            items: dtos,
            totalCount: totalCount,
            pageNumber: pageNumber,
            pageSize: pageSize);
    }

    public async Task<PatientDto> EditPatientAsync(EditPatientProfileDto editPatientProfileDto, Guid? editdById = null, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Editing patient with ID: {PatientId}.", editPatientProfileDto.Id);
            var patient = await _unitOfWork.Patients.GetWithDetailsAsync(editPatientProfileDto.Id, ct);

            if (patient == null)
            {
                _logger.LogWarning("Failed to edit patient. Patient with ID '{PatientId}' was not found.", editPatientProfileDto.Id);
                throw new NotFoundException($"Patient with ID '{editPatientProfileDto.Id}' was not found.");
            }

            var phoneExists = await _unitOfWork.Accounts.ExistsAsync(
                a => a.Id != patient.AccountId && a.PhoneNumber == editPatientProfileDto.PhoneNumber, ct);
            if (phoneExists)
            {
                _logger.LogWarning("Failed to edit patient. Phone number {PhoneNumber} is already in use.", editPatientProfileDto.PhoneNumber);
                throw new ConflictException("Phone number is already in use by another account.");
            }

            var emailExists = await _unitOfWork.Accounts.ExistsAsync(
                a => a.Id != patient.AccountId && a.Email == editPatientProfileDto.Email, ct);
            if (emailExists)
            {
                _logger.LogWarning("Failed to edit patient. Email {Email} is already in use.", editPatientProfileDto.Email);
                throw new ConflictException("Email is already in use by another account.");
            }

            _mapper.Map(editPatientProfileDto, patient);

            patient.Account.UpdatedBy = editdById ?? patient.Account.Id;

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Patient with ID: {PatientId} successfully updated.", patient.Id);
            return _mapper.Map<PatientDto>(patient);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task DeletePatientAsync(Guid id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            _logger.LogInformation("Deleting patient with ID: {PatientId}", id);
            var patient = await _unitOfWork.Patients.GetWithDetailsAsync(id, ct);

            if (patient == null)
            {
                _logger.LogWarning("Failed to delete patient. Patient with ID '{PatientId}' was not found.", id);
                throw new NotFoundException($"Patient with ID '{id}' was not found.");
            }

            _unitOfWork.Patients.Delete(patient);
            _unitOfWork.Accounts.Delete(patient.Account);

            await _unitOfWork.CompleteAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully deleted patient with ID: {PatientId}", id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<PatientDto> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
    {
        _logger.LogInformation("Trying to get patient with account ID: {AccountId}", accountId);
        var patient = await _unitOfWork.Patients.GetByAccountIdAsync(accountId, ct);

        if (patient == null)
        {
            _logger.LogWarning("Failed to retrieve patient. Patient with account ID '{AccountId}' was not found.", accountId);
            throw new NotFoundException($"Patient with account ID '{accountId}' was not found.");
        }

        _logger.LogInformation("Patient with account ID: {AccountId} successfully retrieved.", accountId);
        return _mapper.Map<PatientDto>(patient);
    }
}