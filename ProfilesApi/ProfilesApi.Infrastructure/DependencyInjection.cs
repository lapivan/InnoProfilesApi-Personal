using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Domain.Interfaces;
using ProfilesApi.Infrastructure.Data;
using ProfilesApi.Infrastructure.Repositories;
using ProfilesApi.Infrastructure.Services;

namespace ProfilesApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAdministratorRepository, AdministratorRepository>();
        services.AddScoped<IOfficeRepository, OfficeRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<ISpecializationRepository, SpecializationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}