using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProfilesApi.Application.Dto.Accounts;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Application.Mappings;
using ProfilesApi.Application.Publishers;
using ProfilesApi.Application.Services;

namespace ProfilesApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AccountDto>();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AccountMappingProfile).Assembly));

        services.AddKeyedScoped<IRegistrationPublisher, StaffCreatedPublisher>("ApiContext");
        services.AddKeyedScoped<IRegistrationPublisher, NullRegistrationPublisher>("ConsumerContext");

        services.AddScoped<IAdministratorService, AdministratorService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IOfficeService, OfficeService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<ISpecializationService, SpecializationService>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}