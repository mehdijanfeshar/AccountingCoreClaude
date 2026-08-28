using FluentValidation;

namespace Accounting.Application.WorkShops.Commands.UpdateWorkShop;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
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
