using FluentValidation;

namespace Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
///
/// <c>.IsInEnum()</c> on <c>Flag</c>/<c>AttribSum</c>/<c>ControlId</c> and
/// <c>InclusiveBetween(0, 9)</c> on <c>AttribBoxNo</c> mirror
/// <c>CreateAttribForAccountCodeCommandValidator</c> exactly — added in phase 27 batch 2 alongside
/// the <c>bool</c>/<c>bool?</c>-to-enum (and to-<see cref="short"/>) fix for these four columns.
/// </summary>
public sealed class UpdateAttribForAccountCodeCommandValidator : AbstractValidator<UpdateAttribForAccountCodeCommand>
{
    public UpdateAttribForAccountCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        RuleFor(x => x.AttribBoxNo)
            .InclusiveBetween((short)0, (short)9);

        RuleFor(x => x.Flag)
            .IsInEnum();

        RuleFor(x => x.AttribSum)
            .IsInEnum();

        RuleFor(x => x.ControlId)
            .IsInEnum()
            .When(x => x.ControlId.HasValue);

        // Deliberate second belt, not dead code: by the time this validator runs,
        // VahedScopeBehavior (registered ahead of ValidationBehavior — see
        // DependencyInjection.cs) has already overwritten UpdateAttribForAccountCodeCommand.VahedCode
        // with the server-assigned value, so this rule now validates that value rather than
        // anything the caller supplied.
        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
