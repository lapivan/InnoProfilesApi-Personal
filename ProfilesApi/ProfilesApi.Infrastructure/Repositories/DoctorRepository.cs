using Microsoft.EntityFrameworkCore;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Interfaces;
using ProfilesApi.Infrastructure.Data;

namespace ProfilesApi.Infrastructure.Repositories;

public sealed class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
{
    public DoctorRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Doctor?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(d => d.Account)
            .FirstOrDefaultAsync(d => d.AccountId == accountId, cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> SearchDoctorByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(d => d.Account)
            .Where(d => d.Account.Firstname.ToLower().Contains(name) ||
                        d.Account.Lastname.ToLower().Contains(name))
            .ToListAsync(cancellationToken);
    }

    public async Task<Doctor?> GetWithDetailsAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(d => d.Account)
            .Include(d => d.Office)
            .Include(d => d.Specialization)
            .FirstOrDefaultAsync(a => a.Id == doctorId, cancellationToken);
    }
}