using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.GenerateFsRun;

public sealed class GenerateFsRunCommandValidator : AbstractValidator<GenerateFsRunCommand>
{
    public GenerateFsRunCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Year)
            .NotEmpty()
            .Matches("^1[34][0-9]{2}$")
            .WithMessage("سال مالی باید سال شمسی چهاررقمی باشد.");
        RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.MinDocLife).InclusiveBetween(1, 4);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.NoteStartNo).InclusiveBetween(1, 999);
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
