using AutoMapper;
using ProfilesApi.Application.Dto.Accounts;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Enums;

namespace ProfilesApi.Application.Mappings;

public sealed class AccountMappingProfile : Profile
{
    public AccountMappingProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<Account, AdministratorDto>();

        CreateMap<CreateAdministratorDto, Account>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => Roles.Administrator)
            );

        CreateMap<EditAdministratorProfileDto, Account>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => Roles.Administrator)
            )
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<Account, DoctorDto>();

        CreateMap<CreateDoctorDto, Account>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => Roles.Doctor)
            );

        CreateMap<EditDoctorProfileDto, Account>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => Roles.Doctor)
            )
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<Account, PatientDto>();
        CreateMap<EditPatientProfileDto, Account>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
        CreateMap<RegisterPatientDto, Account>();
    }
}