using Accounting.Application.FinancialStatements;
using FluentValidation;

namespace Accounting.Application.Commands.FinancialStatements.DeleteFsTemplateRow;

public sealed class DeleteFsTemplateRowCommandValidator : AbstractValidator<DeleteFsTemplateRowCommand>
{
    public DeleteFsTemplateRowCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.RowId).NotEqual(Guid.Empty);
    }
}
