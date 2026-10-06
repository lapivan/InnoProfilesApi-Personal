using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Infrastructure.Data.Configurations;

public sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Address)
            .HasMaxLength(256);

        builder.Property(o => o.PhoneNumber)
            .HasMaxLength(20);

        builder.HasOne(o => o.Photo)
            .WithOne()
            .HasForeignKey<Office>(o => o.PhotoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.PhoneNumber).IsUnique();
    }
}