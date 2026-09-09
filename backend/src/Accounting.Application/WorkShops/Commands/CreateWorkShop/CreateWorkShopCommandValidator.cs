using FluentValidation;

namespace Accounting.Application.WorkShops.Commands.CreateWorkShop;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. <see cref="CreateWorkShopCommand.BranchId"/>
/// carries no rule beyond nullability since it is genuinely optional (<c>BRANCH_ID</c> is
/// nullable); <see cref="CreateWorkShopCommand.CheckFile"/> carries no size rule — see the
/// command XML doc.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateWorkShopCommand.VahedCode"/> with the server-assigned value, so this rule now
/// validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreateWorkShopCommandValidator : AbstractValidator<CreateWorkShopCommand>
{
    public CreateWorkShopCommandValidator()
    {
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
