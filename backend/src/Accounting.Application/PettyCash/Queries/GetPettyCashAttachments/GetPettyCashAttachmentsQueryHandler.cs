using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashAttachments;

public sealed class GetPettyCashAttachmentsQueryHandler
    : IRequestHandler<GetPettyCashAttachmentsQuery, IReadOnlyList<PettyCashAttachmentDto>>
{
    private readonly IPettyCashExpenseDocReadRepository _expenseDocReadRepository;
    private readonly IPettyCashAttachmentReadRepository _attachmentReadRepository;

    public GetPettyCashAttachmentsQueryHandler(
        IPettyCashExpenseDocReadRepository expenseDocReadRepository,
        IPettyCashAttachmentReadRepository attachmentReadRepository)
    {
        _expenseDocReadRepository = expenseDocReadRepository;
        _attachmentReadRepository = attachmentReadRepository;
    }

    public async Task<IReadOnlyList<PettyCashAttachmentDto>> Handle(
        GetPettyCashAttachmentsQuery request, CancellationToken cancellationToken)
    {
        // Existence/ownership of the parent document — throws UnitAccessDeniedException (403) for
        // a cross-unit document; a missing one becomes NotFoundException (404) here rather than
        // silently returning an empty list. Same pattern as GetPettyCashDocEventsQueryHandler.
        var expenseDoc = await _expenseDocReadRepository.GetByIdAsync(request.ExpenseDocId, request.VahedCode, cancellationToken);

        if (expenseDoc is null)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.ExpenseDocId);
        }

        return await _attachmentReadRepository.GetByExpenseDocIdAsync(request.ExpenseDocId, cancellationToken);
    }
}
