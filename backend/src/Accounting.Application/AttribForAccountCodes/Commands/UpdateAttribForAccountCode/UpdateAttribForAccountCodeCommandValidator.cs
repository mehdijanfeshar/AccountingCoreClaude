using FluentValidation;

namespace Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdateAttribForAccountCodeCommandValidator : AbstractValidator<UpdateAttribForAccountCodeCommand>
{
    public UpdateAttribForAccountCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
