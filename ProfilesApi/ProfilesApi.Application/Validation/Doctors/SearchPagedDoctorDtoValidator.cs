using FluentValidation;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Doctors;

public sealed class SearchPagedDoctorDtoValidator : AbstractValidator<SearchPagedDoctorDto>
{
    public SearchPagedDoctorDtoValidator()
    {
        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.OfficeId.HasValue);

        RuleFor(x => x.SearchTerm).SearchTermRules();

        RuleFor(x => x.SpecializationId)
            .NotEmpty().WithMessage("Invalid specialization ID format.")
            .When(x => x.SpecializationId.HasValue);

        RuleFor(x => x.MinExperienceYears)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum experience years must be greater than or equal to zero.")
            .When(x => x.MinExperienceYears.HasValue);
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}