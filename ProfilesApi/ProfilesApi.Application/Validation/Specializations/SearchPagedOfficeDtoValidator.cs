using FluentValidation;
using ProfilesApi.Application.Dto.Specializations;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Specializations;

public sealed class SearchPagedSpecializationDtoValidator : AbstractValidator<SearchPagedSpecializationDto>
{
    public SearchPagedSpecializationDtoValidator()
    {
        RuleFor(x => x.SearchTerm).SearchTermRules();

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}