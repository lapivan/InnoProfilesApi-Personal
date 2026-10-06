using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Infrastructure.Data.Configurations;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasQueryFilter(a => a.IsActive);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.GapInMonths)
            .HasColumnType("smallint");

        builder.Property(d => d.Degree)
            .HasMaxLength(30);

        builder.HasOne(d => d.Specialization)
            .WithMany()
            .HasForeignKey(d => d.SpecializationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Office)
            .WithMany()
            .HasForeignKey(d => d.OfficeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Account)
            .WithOne()
            .HasForeignKey<Doctor>(d => d.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.AccountId)
            .IsUnique();
    }
}