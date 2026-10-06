using FluentValidation;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Administrators;

public sealed class CreateAdministratorDtoValidator : AbstractValidator<CreateAdministratorDto>
{
    public CreateAdministratorDtoValidator()
    {
        RuleFor(x => x.Firstname).FirstnameRules();
        RuleFor(x => x.Lastname).LastnameRules();
        RuleFor(x => x.Birthday).BirthdayRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
        RuleFor(x => x.Email).EmailRules();
        RuleFor(x => x.Password).PasswordRules();

        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.PhotoId.HasValue);

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office ID is required.");

        RuleFor(x => x.CareerStartDate)
            .NotEmpty().WithMessage("Career start date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Career start date cannot be in the future.");

        RuleFor(x => x.GapInMonths)
            .GreaterThanOrEqualTo(0).WithMessage("Gap in months cannot be negative.");

        RuleFor(x => x.CareerStartDate)
            .GreaterThan(x => x.Birthday)
            .WithMessage("Career start date must be after the birthday.");

        RuleFor(x => x.GapInMonths)
            .Must((dto, gap) =>
            {
                var today = DateTime.UtcNow;
                var totalMonths = ((today.Year - dto.CareerStartDate.Year) * 12)
                    + today.Month - dto.CareerStartDate.Month;

                return gap <= totalMonths;
            })
            .WithMessage("Gap in months cannot be greater than total career duration.");
    }
}