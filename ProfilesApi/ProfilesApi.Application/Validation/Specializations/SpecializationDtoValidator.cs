using FluentValidation;
using ProfilesApi.Application.Dto.Specializations;

namespace ProfilesApi.Application.Validation.Specializations;

public sealed class SpecializationDtoValidator : AbstractValidator<SpecializationDto>
{
    public SpecializationDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Specialization name must not be empty.")
            .MaximumLength(100).WithMessage("Specialization name must not exceed 100 characters.");
    }
}