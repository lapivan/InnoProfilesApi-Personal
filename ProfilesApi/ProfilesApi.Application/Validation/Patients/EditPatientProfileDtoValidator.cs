using FluentValidation;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Patients;

public sealed class EditPatientProfileDtoValidator : AbstractValidator<EditPatientProfileDto>
{
    public EditPatientProfileDtoValidator()
    {
        RuleFor(x => x.Firstname).FirstnameRules();
        RuleFor(x => x.Lastname).LastnameRules();
        RuleFor(x => x.Birthday).BirthdayRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
        RuleFor(x => x.Email).EmailRules();

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Invalid patient ID.");

        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.PhotoId.HasValue);
    }
}