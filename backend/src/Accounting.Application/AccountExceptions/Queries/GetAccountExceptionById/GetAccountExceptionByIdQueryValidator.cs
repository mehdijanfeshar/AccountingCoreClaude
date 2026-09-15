using FluentValidation;

namespace Accounting.Application.AccountExceptions.Queries.GetAccountExceptionById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetAccountExceptionByIdQueryValidator : AbstractValidator<GetAccountExceptionByIdQuery>
{
    public GetAccountExceptionByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
