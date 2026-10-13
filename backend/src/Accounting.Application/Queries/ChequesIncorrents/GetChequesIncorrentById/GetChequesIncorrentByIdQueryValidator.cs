using FluentValidation;

namespace Accounting.Application.Queries.ChequesIncorrents.GetChequesIncorrentById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetChequesIncorrentByIdQueryValidator : AbstractValidator<GetChequesIncorrentByIdQuery>
{
    public GetChequesIncorrentByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
