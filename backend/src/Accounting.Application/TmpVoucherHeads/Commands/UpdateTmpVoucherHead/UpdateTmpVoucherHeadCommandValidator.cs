using FluentValidation;

namespace Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c> and mirroring <c>CreateTmpVoucherHeadCommandValidator</c> rule for
/// rule, plus the route-bound <c>Id</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants must NOT be re-created here.
/// </summary>
public sealed class UpdateTmpVoucherHeadCommandValidator : AbstractValidator<UpdateTmpVoucherHeadCommand>
{
    public UpdateTmpVoucherHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.DateDoc)
            .MaximumLength(8);

        RuleFor(x => x.HeadDesc)
            .MaximumLength(250);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);

        RuleFor(x => x.SysType)
            .MaximumLength(1);
    }
}
