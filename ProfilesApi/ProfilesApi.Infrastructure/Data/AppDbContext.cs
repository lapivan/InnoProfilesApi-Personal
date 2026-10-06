using Microsoft.EntityFrameworkCore;
using ProfilesApi.Domain.Common;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Interfaces;

namespace ProfilesApi.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Office> Offices { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Specialization> Specializations { get; set; }

    public override int SaveChanges()
    {
        UpdateEntitiesBeforeSave();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateEntitiesBeforeSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateEntitiesBeforeSave();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateEntitiesBeforeSave();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private void UpdateEntitiesBeforeSave()
    {
        foreach (var entry in ChangeTracker.Entries<SoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsActive = false;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}