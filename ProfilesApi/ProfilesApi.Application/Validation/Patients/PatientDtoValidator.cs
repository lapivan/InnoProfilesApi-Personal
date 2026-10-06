using FluentValidation;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Patients;

public sealed class PatientDtoValidator : AbstractValidator<PatientDto>
{
    public PatientDtoValidator()
    {
        RuleFor(x => x.Firstname).FirstnameRules();
        RuleFor(x => x.Lastname).LastnameRules();
        RuleFor(x => x.Birthday).BirthdayRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
        RuleFor(x => x.Email).EmailRules();
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty()
            .IsInEnum()
            .WithMessage("Undefined role.");

        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.PhotoId.HasValue);
    }
}