using FluentValidation;
using ProfilesApi.Application.Dto.Accounts;
using ProfilesApi.Application.Validation.Shared;

namespace ProfilesApi.Application.Validation.Accounts;

public sealed class AccountDtoValidator : AbstractValidator<AccountDto>
{
    public AccountDtoValidator()
    {
        RuleFor(x => x.Firstname).FirstnameRules();
        RuleFor(x => x.Lastname).LastnameRules();
        RuleFor(x => x.Birthday).BirthdayRules();
        RuleFor(x => x.PhoneNumber).PhoneNumberRules();
        RuleFor(x => x.Email).EmailRules();

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("Invalid photo ID format.")
            .When(x => x.PhotoId.HasValue);

        RuleFor(x => x.Role)
            .NotEmpty()
            .IsInEnum()
            .WithMessage("Undefined role.");
    }
}