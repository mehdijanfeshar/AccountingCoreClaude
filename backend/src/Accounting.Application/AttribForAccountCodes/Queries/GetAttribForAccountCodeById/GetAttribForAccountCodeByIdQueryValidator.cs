using FluentValidation;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetAttribForAccountCodeByIdQueryValidator : AbstractValidator<GetAttribForAccountCodeByIdQuery>
{
    public GetAttribForAccountCodeByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
