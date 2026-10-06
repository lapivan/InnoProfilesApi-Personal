using FluentValidation;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Doctors;

public sealed class DoctorDtoValidator : AbstractValidator<DoctorDto>
{
    public DoctorDtoValidator()
    {
        RuleFor(x => x.Firstname).FirstnameRules();
        RuleFor(x => x.Lastname).LastnameRules();
        RuleFor(x => x.Birthday).BirthdayRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
        RuleFor(x => x.Email).EmailRules();
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.PhotoId.HasValue);

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office ID is required.");

        RuleFor(x => x.SpecializationId)
            .NotEmpty().WithMessage("Specialization is required.");

        RuleFor(x => x.Degree)
            .NotEmpty().WithMessage("Degree is required.")
            .MaximumLength(50).WithMessage("Degree cannot be longer than 50 characters.");
    }
}
