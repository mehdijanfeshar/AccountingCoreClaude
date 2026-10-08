using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Constructs the <see cref="TB_VOUCHERSHEAD"/> Domain entity from the command, stages it via
/// <see cref="IVoucherHeadRepository"/>, and — when <see cref="CreateVoucherHeadCommand.InitialDetails"/>
/// is supplied — also constructs and stages one <see cref="TB_VOUCHERSDETAIL"/> per entry via
/// <see cref="IVoucherDetailRepository"/>, wiring each line's <c>VOUCHERSHEAD_ID</c>,
/// <c>VAHEDCODE</c> and <c>YEAR</c> to the head just built (never taken from the line input —
/// see <see cref="CreateVoucherHeadDetailInput"/> XML doc). Owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly ONCE for the head AND every
/// initial detail line together, so a partially-created voucher (head without its opening
/// lines, or vice versa) can never be observed. <c>ADDUSERID</c> — on the head and on every
/// detail line — is sourced from <see cref="ICurrentUser"/> (the authenticated caller) — never
/// from the request — so it cannot be forged by the client. <c>CREATEDDATE</c> is computed once
/// (<c>now</c>) and reused for the head and every detail line, so they share one creation
/// timestamp instead of drifting by however long line construction takes.
///
/// This handler just maps <see cref="CreateVoucherHeadCommand.VahedCode"/> onto the head — and,
/// via <c>request.VahedCode</c>, onto every composite-created detail line — at face value; it
/// does not read <see cref="ICurrentUser.VahedCode"/> directly. Forgery prevention (overwriting
/// whatever the caller supplied with the authenticated caller's own unit code) is
/// <c>VahedScopeBehavior</c>'s job, which runs before this handler for every
/// <see cref="Accounting.Application.Common.Security.IVahedScopedCommand"/>. By the time
/// <see cref="Handle"/> executes, <c>request.VahedCode</c> is already the server-assigned value,
/// so mapping it onto both the head AND every detail line is exactly what closes IDOR risk #1 on
/// this composite-create path.
/// </summary>
public sealed class CreateVoucherHeadCommandHandler : IRequestHandler<CreateVoucherHeadCommand, Guid>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IAccountEntryPolicy? _accountEntryPolicy;

    public CreateVoucherHeadCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IAccountEntryPolicy? accountEntryPolicy = null)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _accountEntryPolicy = accountEntryPolicy;
    }

    public async Task<Guid> Handle(CreateVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var entity = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = request.DocNum,
            DATE_DOC = request.DateDoc,
            // Null used to fall through to the column's DEFAULT 0 — outside the enum, so the new
            // voucher was immediately non-editable. Null now means یادداشت (2026-10-07).
            DOCLIFE = request.DocLife ?? DocLife.Draft,
            HEAD_DESC = request.HeadDesc,
            APENDIX = request.Apendix,
            SYSTEM_TYPE = request.SystemTypeId,
            FLAG_STATE = request.FlagState,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ISAUTOMATIC = request.IsAutomatic,
            SNDVAHEDCODE = request.SndVahedCode,
            PARENTHEAD_ID = request.ParentHeadId,
            ATTACHFILE_NAME = request.AttachFileName,
            // شمارهٔ عطف را سرور هنگام ذخیره تخصیص می‌دهد (VoucherAtfNumberInterceptor)؛ مقدار ورودی نادیده گرفته می‌شود.
            ATF_NUM = null,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        // Risk #3: a voucher created straight into موقت must already be balanced.
        if (VoucherBalanceGuard.RequiresBalance(entity.DOCLIFE))
        {
            VoucherBalanceGuard.EnsureBalanced(
                entity.ID,
                entity.DOC_NUM,
                (request.InitialDetails ?? []).Select(d => (d.Debtor, d.Creditor)));
        }

        await _voucherHeadRepository.AddAsync(entity, cancellationToken);

        if (request.InitialDetails is { Count: > 0 } initialDetails)
        {
            // Every line is checked before any is staged, so a composite create is all-or-nothing
            // against «تفصیلی الزامی» too — a voucher never lands with some lines validated and the
            // rest rejected.
            //
            // ⚠️ CreateVoucherHeadDetailInput has no tafsiliLinks field (open risk #21), so a line
            // here can only ever be created WITHOUT تفصیلی. That means this path now rejects any
            // initial line whose معین requires تفصیلی — which is the rule working, not a
            // regression: such a line was always invalid, it was just accepted silently before.
            // The frontend voucher form is unaffected because it deliberately never uses
            // initialDetails; it posts the head, then each line through CreateVoucherDetail.
            foreach (var detailInput in initialDetails)
            {
                await _tafsiliLevelGuard.EnsureSatisfiedAsync(detailInput.AccountId, [], cancellationToken);
            }

            if (_accountEntryPolicy is not null)
            {
                // ماتریس دسترسی کدینگ (ریسک #۲۷) — مسیر ثبت دستی.
                await _accountEntryPolicy.EnsureManualEntryAllowedAsync(
                    request.VahedCode, request.DateDoc, initialDetails.Select(d => d.AccountId), cancellationToken);
            }

            foreach (var detailInput in initialDetails)
            {
                var detailEntity = new TB_VOUCHERSDETAIL
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSHEAD_ID = entity.ID,
                    ACCOUNT_ID = detailInput.AccountId,
                    RECEIP_ID = detailInput.ReceiptId,
                    CHECK_ID = detailInput.CheckId,
                    LOWLEVELCODE_ID = detailInput.LowLevelCodeId,
                    ETEBAR_ID = detailInput.EtebarId,
                    DESCRIPTION = detailInput.Description,
                    RADIF = detailInput.Radif,
                    DEBTOR = detailInput.Debtor,
                    CREDITOR = detailInput.Creditor,
                    VAHEDCODE = request.VahedCode,
                    YEAR = request.Year,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };

                await _voucherDetailRepository.AddAsync(detailEntity, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
