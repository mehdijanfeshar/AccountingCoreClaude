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
