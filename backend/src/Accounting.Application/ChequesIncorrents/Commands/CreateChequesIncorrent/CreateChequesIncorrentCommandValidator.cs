using FluentValidation;

namespace Accounting.Application.ChequesIncorrents.Commands.CreateChequesIncorrent;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. <see cref="CreateChequesIncorrentCommand.CheckId"/>
/// carries no rule beyond nullability since it is genuinely optional — see the command XML doc
/// for the no-FK warning. <see cref="CreateChequesIncorrentCommand.Creditor"/> carries no
/// range/precision rule — see the command XML doc for the amount-type open question.
/// </summary>
public sealed class CreateChequesIncorrentCommandValidator : AbstractValidator<CreateChequesIncorrentCommand>
{
    public CreateChequesIncorrentCommandValidator()
    {
        RuleFor(x => x.DocNum)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.DocDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.CheqNo)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.CheqDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.PaperDesc)
            .MaximumLength(200);

        RuleFor(x => x.PayTo)
            .MaximumLength(200);

        RuleFor(x => x.RecivDate)
            .MaximumLength(8);

        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .MaximumLength(13);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
