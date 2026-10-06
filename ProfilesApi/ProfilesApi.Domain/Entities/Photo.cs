using ProfilesApi.Domain.Common;

namespace ProfilesApi.Domain.Entities;

public sealed class Photo : BaseEntity
{
    public string? Url { get; set; }

    public Photo(string? url)
    {
        Url = url;
    }
    protected Photo() { }
}