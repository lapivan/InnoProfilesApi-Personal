using FluentValidation;
using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Doctors;

public sealed class SearchFiltredDoctorListDtoValidator : AbstractValidator<SearchFilteredDoctorListDto>
{
    public SearchFiltredDoctorListDtoValidator()
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
    }
}