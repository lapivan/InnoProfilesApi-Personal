using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Domain.Interfaces;

public interface IPatientRepository : IGenericRepository<Patient>
{
    Task<Patient?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Patient?> GetWithDetailsAsync(Guid patientId, CancellationToken cancellationToken = default);
}