using Accounting.Application.FinancialStatements.Commands.Common;
using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.ImportFsTemplateRows;

public sealed class ImportFsTemplateRowsCommandValidator : AbstractValidator<ImportFsTemplateRowsCommand>
{
    public ImportFsTemplateRowsCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Rows).NotEmpty().Must(r => r.Count <= 1000).WithMessage("حداکثر ۱۰۰۰ ردیف.");
        RuleForEach(x => x.Rows).SetValidator(new FsTemplateRowInputValidator());
    }
}
