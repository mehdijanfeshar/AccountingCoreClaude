using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashAttachments;

public sealed class GetPettyCashAttachmentsQueryValidator : AbstractValidator<GetPettyCashAttachmentsQuery>
{
    public GetPettyCashAttachmentsQueryValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();
    }
}
