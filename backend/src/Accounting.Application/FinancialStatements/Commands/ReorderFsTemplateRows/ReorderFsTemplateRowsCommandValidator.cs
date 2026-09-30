using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.ReorderFsTemplateRows;

public sealed class ReorderFsTemplateRowsCommandValidator : AbstractValidator<ReorderFsTemplateRowsCommand>
{
    public ReorderFsTemplateRowsCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.RowIds)
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("شناسهٔ ردیف تکراری در فهرست ترتیب.");
    }
}
