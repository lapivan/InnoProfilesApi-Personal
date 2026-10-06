using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Interfaces;
using ProfilesApi.Infrastructure.Data;

namespace ProfilesApi.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly Lazy<IAccountRepository> _accountsRepository;
    private readonly Lazy<IAdministratorRepository> _administratorsRepository;
    private readonly Lazy<IDoctorRepository> _doctorsRepository;
    private readonly Lazy<IOfficeRepository> _officesRepository;
    private readonly Lazy<IPatientRepository> _patientsRepository;
    private readonly Lazy<IPhotoRepository> _photosRepository;
    private readonly Lazy<ISpecializationRepository> _specializationsRepository;
    private readonly AppDbContext _context;
    private bool _disposed;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;

        _accountsRepository = new Lazy<IAccountRepository>(() => new AccountRepository(_context));
        _administratorsRepository = new Lazy<IAdministratorRepository>(() => new AdministratorRepository(_context));
        _doctorsRepository = new Lazy<IDoctorRepository>(() => new DoctorRepository(_context));
        _officesRepository = new Lazy<IOfficeRepository>(() => new OfficeRepository(_context));
        _patientsRepository = new Lazy<IPatientRepository>(() => new PatientRepository(_context));
        _photosRepository = new Lazy<IPhotoRepository>(() => new PhotoRepository(_context));
        _specializationsRepository = new Lazy<ISpecializationRepository>(() => new SpecializationRepository(_context));
    }

    public IAccountRepository Accounts => _accountsRepository.Value;
    public IAdministratorRepository Administrators => _administratorsRepository.Value;
    public IDoctorRepository Doctors => _doctorsRepository.Value;
    public IOfficeRepository Offices => _officesRepository.Value;
    public IPatientRepository Patients => _patientsRepository.Value;
    public IPhotoRepository Photos => _photosRepository.Value;
    public ISpecializationRepository Specializations => _specializationsRepository.Value;

    public async Task<int> CompleteAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await _context.Database.BeginTransactionAsync(isolationLevel, ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null) return;

        try
        {
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null) return;

        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public int Complete()
    {
        return _context.SaveChanges();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
            _context.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }

            await _context.DisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}