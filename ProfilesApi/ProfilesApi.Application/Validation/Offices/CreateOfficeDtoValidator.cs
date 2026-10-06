using FluentValidation;
using ProfilesApi.Application.Dto.Offices;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Offices;

public sealed class CreateOfficeDtoValidator : AbstractValidator<CreateOfficeDto>
{
    public CreateOfficeDtoValidator()
    {
        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.PhotoId.HasValue);

        RuleFor(x => x.Address).AddressRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
    }
}