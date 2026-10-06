using FluentValidation;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Administrators;

public sealed class AdministratorDtoValidator : AbstractValidator<AdministratorDto>
{
    public AdministratorDtoValidator()
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

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office ID is required.");

        RuleFor(x => x.TotalExperience)
            .NotEmpty()
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total experience must be greater than or equal to 0.");
    }
}