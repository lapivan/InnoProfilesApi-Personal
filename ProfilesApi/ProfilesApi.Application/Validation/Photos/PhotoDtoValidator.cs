using FluentValidation;
using ProfilesApi.Application.Dto.Photos;

namespace ProfilesApi.Application.Validation.Photos;

public sealed class PhotoDtoValidator : AbstractValidator<PhotoDto>
{
    public PhotoDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("The id cannot be empty.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("The url cannot be empty.");
    }
}
