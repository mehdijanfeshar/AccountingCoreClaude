using FluentValidation;

namespace Accounting.Application.WorkShops.Commands.UpdateWorkShop;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
///
/// ⚠️ The <c>RuleFor(x => x.VahedCode)</c> below currently does NOT execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason).
/// <see cref="UpdateWorkShopCommand.VahedCode"/> is still safe at runtime only because
/// <c>VahedScopeBehavior</c> unconditionally overwrites it before the handler runs, not because
/// of this rule.
/// </summary>
public sealed class UpdateWorkShopCommandValidator : AbstractValidator<UpdateWorkShopCommand>
{
    public UpdateWorkShopCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.WorkShopName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.WorkShopCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
