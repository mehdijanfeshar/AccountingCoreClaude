using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UploadPettyCashAttachment;

public sealed class UploadPettyCashAttachmentCommandHandler : IRequestHandler<UploadPettyCashAttachmentCommand, Guid>
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashAttachmentRepository _attachmentRepository;
    private readonly IPettyCashDocEventReadRepository _eventReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UploadPettyCashAttachmentCommandHandler(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashAttachmentRepository attachmentRepository,
        IPettyCashDocEventReadRepository eventReadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _expenseDocRepository = expenseDocRepository;
        _attachmentRepository = attachmentRepository;
        _eventReadRepository = eventReadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UploadPettyCashAttachmentCommand request, CancellationToken cancellationToken)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(request.ExpenseDocId, request.VahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.ExpenseDocId);
        }

        // Same rule as Update/Submit — reused, not copied.
        PettyCashDocEditability.EnsureEditable(doc.ID, doc.DOC_STATE);

        // Plain ownership, not the §2 reviewer/SoD authorizer — attachment add/delete is not a
        // بررسی action (docs/tankhah-khazaneh-module.md, تصمیم‌های بخش ۲، پیوست).
        if (!string.Equals(_currentUser.UserId, doc.ADDUSERID, StringComparison.Ordinal))
        {
            throw new PettyCashAttachmentOwnerOnlyException(doc.ID);
        }

        // تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، «قفل فیلدبه‌فیلد»، صفحهٔ ۸): on a Returned document, attachments
        // may only be touched when the return reasons include AttachmentIncomplete or Other.
        if (doc.DOC_STATE == PettyCashDocState.Returned)
        {
            var lastReturn = await _eventReadRepository.GetLastReturnEventAsync(doc.ID, cancellationToken);
            var reasonCodes = PettyCashReturnFieldPolicy.ParseReasonCodes(lastReturn?.ReturnReasons);

            if (!PettyCashReturnFieldPolicy.CanEditAttachments(reasonCodes))
            {
                throw new PettyCashReturnFieldLockedException(doc.ID, new[] { "attachments" });
            }
        }

        // Belt-and-suspenders against UploadPettyCashAttachmentCommandValidator's identical rule
        // — see PettyCashAttachmentTooLargeException XML doc.
        if (request.Content.Length > UploadPettyCashAttachmentCommandValidator.MaxAttachmentSizeBytes)
        {
            throw new PettyCashAttachmentTooLargeException(
                request.Content.Length, UploadPettyCashAttachmentCommandValidator.MaxAttachmentSizeBytes);
        }

        var nextRadif = await _attachmentRepository.GetMaxRadifAsync(doc.ID, cancellationToken) + 1;
        var now = DateTime.UtcNow;

        var attachment = new TB_PC_ATTACHMENT
        {
            ID = Guid.NewGuid(),
            EXPENSE_DOC_ID = doc.ID,
            ATTACH_NAME = request.AttachName,
            ATTACH_SIZE = request.Content.Length,
            CONTENT_TYPE = request.ContentType,
            ATTACH_FILE = request.Content,
            ATTACH_RADIF = nextRadif,
            VAHEDCODE = request.VahedCode,
            YEAR = doc.YEAR,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attachment.ID;
    }
}
