using FluentValidation;

namespace Accounting.Application.FinancialStatements.Queries.GetFsTemplates;

public sealed class GetFsTemplatesQueryValidator : AbstractValidator<GetFsTemplatesQuery>
{
    public GetFsTemplatesQueryValidator()
    {
        RuleFor(x => x.Framework).IsInEnum().When(x => x.Framework.HasValue);
    }
}
