using InnoClinic.Shared.Events;
using MassTransit;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Entities;

namespace ProfilesApi.Application.Consumers;

public class ProfileRegisteredConsumer : IConsumer<IProfileRegisteredEvent>
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfileRegisteredConsumer(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<IProfileRegisteredEvent> context)
    {
        var msg = context.Message;

        var account = new Account(
            firstname: msg.Firstname,
            lastname: msg.Lastname,
            birthday: msg.Birthday,
            phoneNumber: msg.PhoneNumber,
            email: msg.Email,
            passwordHash: "ManagedByKeycloak",
            role: (ProfilesApi.Domain.Enums.Roles)msg.Role,
            createdBy: msg.KeycloakId,
            updatedBy: msg.KeycloakId
        )
        {
            Id = msg.KeycloakId,
            PhotoId = msg.PhotoId,
            IsActive = true
        };

        _unitOfWork.Accounts.Add(account);

        if (msg.Role == Roles.Patient)
        {
            _unitOfWork.Patients.Add(new Patient(msg.KeycloakId)
            {
                Account = account
            });
        }
        else if (msg.Role == Roles.Doctor && msg.OfficeId.HasValue && msg.SpecializationId.HasValue && msg.CareerStartDate.HasValue)
        {
            _unitOfWork.Doctors.Add(new Doctor(
                accountId: msg.KeycloakId,
                specializationId: msg.SpecializationId.Value,
                officeId: msg.OfficeId.Value,
                careerStartDate: msg.CareerStartDate.Value,
                gapInMonths: msg.GapInMonths ?? 0,
                degree: msg.Degree ?? string.Empty
            )
            {
                Account = account
            });
        }
        else if (msg.Role == Roles.Administrator && msg.OfficeId.HasValue && msg.CareerStartDate.HasValue)
        {
            _unitOfWork.Administrators.Add(new Administrator(
                accountId: msg.KeycloakId,
                officeId: msg.OfficeId.Value,
                careerStartDate: msg.CareerStartDate.Value,
                gapInMonths: msg.GapInMonths ?? 0
            )
            {
                Account = account
            });
        }

        await _unitOfWork.CompleteAsync();
    }
}