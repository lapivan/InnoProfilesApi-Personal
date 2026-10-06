using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Domain.Common;

public abstract class SoftDeletableEntity : BaseEntity
{
    public bool IsActive { get; set; } = true;
}