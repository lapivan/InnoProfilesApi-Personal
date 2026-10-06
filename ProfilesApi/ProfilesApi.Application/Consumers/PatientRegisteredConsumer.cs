using InnoClinic.Shared.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Application.Interfaces;

namespace ProfilesApi.Application.Consumers;

public sealed class PatientRegisteredConsumer : IConsumer<IPatientRegisteredEvent>
{
    private readonly IPatientService _patientService;
    private readonly IRegistrationPublisher _publisher;
    private readonly ILogger<PatientRegisteredConsumer> _logger;

    public PatientRegisteredConsumer(
        IPatientService patientService,
        [FromKeyedServices("ConsumerContext")] IRegistrationPublisher publisher,
        ILogger<PatientRegisteredConsumer> logger)
    {
        _patientService = patientService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IPatientRegisteredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming IPatientRegisteredEvent for AccountId: {AccountId}, Email: {Email}", message.AccountId, message.Email);

        var registerDto = new RegisterPatientDto
        {
            Firstname = message.Firstname,
            Lastname = message.Lastname,
            Email = message.Email,
            PhoneNumber = message.PhoneNumber,
            Password = message.Password,
            Birthday = message.Birthday
        };

        await _patientService.CreatePatientAsync(
            registerDto,
            _publisher,
            customAccountId: message.AccountId,
            ct: context.CancellationToken);
    }
}