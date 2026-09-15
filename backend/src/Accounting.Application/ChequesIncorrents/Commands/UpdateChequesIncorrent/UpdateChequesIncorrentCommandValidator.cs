using FluentValidation;

namespace Accounting.Application.ChequesIncorrents.Commands.UpdateChequesIncorrent;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
///
/// ⚠️ Unlike the Create-side validator, the <c>RuleFor(x => x.VahedCode)</c> rule below currently
/// does NOT execute at runtime: <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c>
/// constraint is never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so
/// the DI container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason). This is safe at runtime only
/// because <c>VahedScopeBehavior</c> unconditionally overwrites <c>VahedCode</c> before the
/// handler runs, not because of this rule.
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
