using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Infrastructure.Data.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasQueryFilter(a => a.IsActive);

        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Account)
            .WithOne()
            .HasForeignKey<Patient>(p => p.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.AccountId)
            .IsUnique();
    }
}