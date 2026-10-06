using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Domain.Interfaces;

public interface IAdministratorRepository : IGenericRepository<Administrator>
{
    Task<Administrator?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Administrator?> GetWithDetailsAsync(Guid administratorId, CancellationToken cancellationToken = default);
}