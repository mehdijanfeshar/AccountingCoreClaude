using FluentValidation;

namespace Accounting.Application.Queries.WhiteAndBlackLists.GetWhiteAndBlackListById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetWhiteAndBlackListByIdQueryValidator : AbstractValidator<GetWhiteAndBlackListByIdQuery>
{
    public GetWhiteAndBlackListByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
