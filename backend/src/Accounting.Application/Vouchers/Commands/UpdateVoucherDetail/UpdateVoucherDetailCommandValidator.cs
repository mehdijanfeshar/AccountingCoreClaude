using Accounting.Application.Vouchers.Commands.Common;
using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants were deliberately discarded and must NOT be
/// re-created here.
/// </summary>
public sealed class UpdateVoucherDetailCommandValidator : AbstractValidator<UpdateVoucherDetailCommand>
{
    public UpdateVoucherDetailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(200);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);

        // No-op when TafsiliLinks is null ("leave existing links untouched" — see the command's
        // XML doc for why null and empty differ here). An EMPTY list is still valid and is NOT a
        // validation failure: it is the caller explicitly asking for "no links on this line".
        RuleForEach(x => x.TafsiliLinks)
            .SetValidator(new VoucherDetailTafsiliLinkInputValidator())
            .When(x => x.TafsiliLinks is not null);
    }
}
