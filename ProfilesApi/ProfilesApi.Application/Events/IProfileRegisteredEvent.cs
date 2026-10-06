namespace InnoClinic.Shared.Events;

public interface IProfileRegisteredEvent
{
    Guid KeycloakId { get; }
    string Email { get; }
    string Firstname { get; }
    string Lastname { get; }
    string PhoneNumber { get; }
    DateTime Birthday { get; }
    Roles Role { get; }

    Guid? PhotoId { get; }
    Guid? OfficeId { get; }
    Guid? SpecializationId { get; }
    DateTime? CareerStartDate { get; }
    int? GapInMonths { get; }
    string? Degree { get; }
}