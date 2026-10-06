using System.Text.RegularExpressions;
using ProfilesApi.Domain.Common;
using ProfilesApi.Domain.Enums;
using ProfilesApi.Domain.Interfaces;

namespace ProfilesApi.Domain.Entities;

public sealed class Account : SoftDeletableEntity, IAuditable
{
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public DateTime Birthday { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public Roles Role { get; set; }
    public Guid? PhotoId { get; set; }
    public Photo? Photo { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    protected Account() { }
    public Account(string firstname, string lastname, DateTime birthday, string phoneNumber, string email, string passwordHash, Roles role, Guid createdBy, Guid updatedBy)
    {
        Firstname = firstname;
        Lastname = lastname;
        Birthday = birthday;
        PhoneNumber = phoneNumber;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedBy = createdBy;
        UpdatedBy = updatedBy;
    }
}