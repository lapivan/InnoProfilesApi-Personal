using ProfilesApi.Domain.Common;

namespace ProfilesApi.Domain.Entities;

public sealed class Patient : SoftDeletableEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; }
    protected Patient() { }

    public Patient(Guid accountId)
    {
        AccountId = accountId;
    }
}