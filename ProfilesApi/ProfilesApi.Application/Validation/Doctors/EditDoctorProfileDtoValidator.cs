using FluentValidation;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Doctors;

public sealed class EditDoctorProfileDtoValidator : AbstractValidator<EditDoctorProfileDto>
{
    public EditDoctorProfileDtoValidator()
    {
        RuleFor(x => x.Firstname).FirstnameRules();
        RuleFor(x => x.Lastname).LastnameRules();
        RuleFor(x => x.Birthday).BirthdayRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
        RuleFor(x => x.Email).EmailRules();

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

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

        RuleFor(x => x.SpecializationId)
            .NotEmpty().WithMessage("Specialization is required.");

        RuleFor(x => x.Degree)
            .NotEmpty().WithMessage("Degree is required.")
            .MaximumLength(50).WithMessage("Degree cannot be longer than 50 characters.");

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