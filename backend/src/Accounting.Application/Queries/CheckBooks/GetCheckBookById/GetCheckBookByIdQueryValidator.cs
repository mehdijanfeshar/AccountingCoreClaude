using Accounting.Application.CheckBooks;
using FluentValidation;

namespace Accounting.Application.Queries.CheckBooks.GetCheckBookById;

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
