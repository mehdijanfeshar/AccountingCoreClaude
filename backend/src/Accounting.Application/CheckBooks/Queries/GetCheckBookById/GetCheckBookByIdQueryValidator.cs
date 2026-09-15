using FluentValidation;

namespace Accounting.Application.CheckBooks.Queries.GetCheckBookById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetCheckBookByIdQueryValidator : AbstractValidator<GetCheckBookByIdQuery>
{
    public GetCheckBookByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
