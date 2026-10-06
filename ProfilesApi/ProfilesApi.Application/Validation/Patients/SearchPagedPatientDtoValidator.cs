using FluentValidation;
using ProfilesApi.Application.Dto.Patients;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Patients;

public sealed class SearchPagedPatientDtoValidator : AbstractValidator<SearchPagedPatientDto>
{
    public SearchPagedPatientDtoValidator()
    {
        RuleFor(x => x.SearchTerm).SearchTermRules();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(ValidationConstants.PhoneNumberPattern)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Email)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}