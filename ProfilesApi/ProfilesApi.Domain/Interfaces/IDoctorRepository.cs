using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Domain.Interfaces;

public interface IDoctorRepository : IGenericRepository<Doctor>
{
    Task<Doctor?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Doctor>> SearchDoctorByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Doctor?> GetWithDetailsAsync(Guid doctorId, CancellationToken cancellationToken = default);
}