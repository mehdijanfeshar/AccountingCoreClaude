using Accounting.Application.FinancialStatements;
using FluentValidation;

namespace Accounting.Application.Queries.FinancialStatements.GetFsTemplates;

public sealed class GetFsTemplatesQueryValidator : AbstractValidator<GetFsTemplatesQuery>
{
    public GetFsTemplatesQueryValidator()
    {
        RuleFor(x => x.Framework).IsInEnum().When(x => x.Framework.HasValue);
    }
}
