using Accounting.Application.FinancialStatements;
using Accounting.Application.Commands.FinancialStatements.Common;
using FluentValidation;

namespace Accounting.Application.Commands.FinancialStatements.AddFsTemplateRow;

public sealed class AddFsTemplateRowCommandValidator : AbstractValidator<AddFsTemplateRowCommand>
{
    public AddFsTemplateRowCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Row).NotNull().SetValidator(new FsTemplateRowInputValidator());
    }
}
