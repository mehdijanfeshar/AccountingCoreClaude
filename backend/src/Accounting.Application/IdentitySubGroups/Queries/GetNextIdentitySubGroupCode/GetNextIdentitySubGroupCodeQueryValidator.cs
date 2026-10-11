using FluentValidation;

namespace Accounting.Application.IdentitySubGroups.Queries.GetNextIdentitySubGroupCode;

public sealed class GetNextIdentitySubGroupCodeQueryValidator : AbstractValidator<GetNextIdentitySubGroupCodeQuery>
{
    public GetNextIdentitySubGroupCodeQueryValidator()
    {
        RuleFor(x => x.Year).NotEmpty().MaximumLength(4);
    }
}
