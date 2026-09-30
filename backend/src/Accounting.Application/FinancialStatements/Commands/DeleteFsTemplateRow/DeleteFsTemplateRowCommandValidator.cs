using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateRow;

public sealed class DeleteFsTemplateRowCommandValidator : AbstractValidator<DeleteFsTemplateRowCommand>
{
    public DeleteFsTemplateRowCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.RowId).NotEqual(Guid.Empty);
    }
}
