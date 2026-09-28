using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashAttachmentFile;

public sealed class GetPettyCashAttachmentFileQueryValidator : AbstractValidator<GetPettyCashAttachmentFileQuery>
{
    public GetPettyCashAttachmentFileQueryValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();
        RuleFor(x => x.AttachmentId).NotEmpty();
    }
}
