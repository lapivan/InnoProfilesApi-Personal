using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Infrastructure.Data.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasQueryFilter(a => a.IsActive);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Firstname)
            .HasMaxLength(50);

        builder.Property(a => a.Lastname)
            .HasMaxLength(50);

        builder.Property(a => a.Email)
            .HasMaxLength(256);

        builder.Property(a => a.PhoneNumber)
            .HasMaxLength(20);

        builder.HasOne(a => a.Photo)
            .WithOne()
            .HasForeignKey<Account>(a => a.PhotoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.Email)
            .IsUnique();

        builder.HasIndex(a => a.PhoneNumber)
            .IsUnique();
    }
}