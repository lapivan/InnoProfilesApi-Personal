using FluentValidation;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Administrators;

public sealed class SearchPagedAdministratorDtoValidator : AbstractValidator<SearchPagedAdministratorDto>
{
    public SearchPagedAdministratorDtoValidator()
    {
        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.OfficeId.HasValue);

        RuleFor(x => x.SearchTerm).SearchTermRules();

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}