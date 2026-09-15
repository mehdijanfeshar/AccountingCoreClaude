using FluentValidation;

namespace Accounting.Application.ChequesIncorrents.Commands.DeleteChequesIncorrent;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteChequesIncorrentCommandValidator : AbstractValidator<DeleteChequesIncorrentCommand>
{
    public DeleteChequesIncorrentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
