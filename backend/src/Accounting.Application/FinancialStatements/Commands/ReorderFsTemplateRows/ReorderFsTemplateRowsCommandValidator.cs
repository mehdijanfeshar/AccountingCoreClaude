using Accounting.Application.FinancialStatements.Engine;
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
        RuleFor(x => x.ParentChanges)
            .Must(c => c!.Select(p => p.RowId).Distinct().Count() == c!.Count)
            .WithMessage("شناسهٔ ردیف تکراری در تغییر والد.")
            .When(x => x.ParentChanges is not null);
        RuleForEach(x => x.ParentChanges)
            .Must(c => c.ParentCode is null || FsText.IsValidRowCode(c.ParentCode))
            .WithMessage("کد ردیف والد نامعتبر است.")
            .When(x => x.ParentChanges is not null);
    }
}
