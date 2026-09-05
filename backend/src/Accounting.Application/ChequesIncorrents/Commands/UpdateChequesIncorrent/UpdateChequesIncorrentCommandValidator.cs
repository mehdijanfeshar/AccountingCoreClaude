using FluentValidation;

namespace Accounting.Application.ChequesIncorrents.Commands.UpdateChequesIncorrent;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
/// </summary>
public sealed class UpdateChequesIncorrentCommandValidator : AbstractValidator<UpdateChequesIncorrentCommand>
{
    public UpdateChequesIncorrentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
