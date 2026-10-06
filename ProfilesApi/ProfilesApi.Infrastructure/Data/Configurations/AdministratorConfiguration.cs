using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Infrastructure.Data.Configurations;

public sealed class AdministratorConfiguration : IEntityTypeConfiguration<Administrator>
{
    public void Configure(EntityTypeBuilder<Administrator> builder)
    {
        builder.HasQueryFilter(a => a.IsActive);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.GapInMonths)
            .HasColumnType("smallint");

        builder.HasOne(a => a.Account)
            .WithOne()
            .HasForeignKey<Administrator>(a => a.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Office)
            .WithMany()
            .HasForeignKey(a => a.OfficeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.AccountId)
            .IsUnique();

        builder.HasIndex(a => a.OfficeId);
    }
}