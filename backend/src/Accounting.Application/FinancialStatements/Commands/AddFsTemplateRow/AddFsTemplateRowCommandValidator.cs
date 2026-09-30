using Accounting.Application.FinancialStatements.Commands.Common;
using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.AddFsTemplateRow;

public sealed class AddFsTemplateRowCommandValidator : AbstractValidator<AddFsTemplateRowCommand>
{
    public AddFsTemplateRowCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Row).NotNull().SetValidator(new FsTemplateRowInputValidator());
    }
}
