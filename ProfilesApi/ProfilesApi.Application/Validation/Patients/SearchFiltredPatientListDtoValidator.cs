using FluentValidation;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Patients;

public sealed class SearchFilteredPatientListDtoValidator : AbstractValidator<SearchFilteredPatientListDto>
{
    public SearchFilteredPatientListDtoValidator()
    {
        RuleFor(x => x.SearchTerm).SearchTermRules();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(ValidationConstants.PhoneNumberPattern)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Email)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
