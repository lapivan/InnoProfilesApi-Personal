using Microsoft.EntityFrameworkCore;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Interfaces;
using ProfilesApi.Infrastructure.Data;

namespace ProfilesApi.Infrastructure.Repositories;

public sealed class AccountRepository : GenericRepository<Account>, IAccountRepository
{
    public AccountRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Account>> SearchByTerm(string name, CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = Entities.AsQueryable();
        query = query.Where(a => a.Firstname.Contains(name) ||
                                 a.Lastname.Contains(name) ||
                                 (a.Firstname + " " + a.Lastname).Contains(name) ||
                                 (a.Lastname + " " + a.Firstname).Contains(name));
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetWithDetailsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(a => a.Photo)
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
    }

    public async Task<Account?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = Entities.AsQueryable();
        query = query.Where(a => a.Email == email);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Account?> GetByPhoneNumber(string phoneNumber, CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = Entities.AsQueryable();
        query = query.Where(a => a.PhoneNumber == phoneNumber);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByName(string name, CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = Entities.AsQueryable();
        query = query.Where(a => a.Firstname == (name) ||
                                 a.Lastname == (name) ||
                                 (a.Firstname + " " + a.Lastname) == (name) ||
                                 (a.Lastname + " " + a.Firstname) == (name));
        return await query.ToListAsync(cancellationToken);
    }
}