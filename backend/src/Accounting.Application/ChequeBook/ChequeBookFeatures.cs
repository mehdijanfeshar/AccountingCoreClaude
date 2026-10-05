using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.ChequeBook;

/*
 * دفتر چک (عملیات) — معادل «دفتر چک» سیستم قدیم (CheckBookCartableQuery + چاپ چک PrintHtmlQuery).
 * «دستور پرداخت» و «تاییدیه چک» که در سیستم قدیم کاغذی امضا می‌شدند، به تصمیم صاحب پروژه (۲۰۲۶-۱۰-۰۵)
 * کارتابل شدند: صدور دستور پرداخت (هر کاربر واحد) ⇒ تأیید رئیس حسابداری (نقش SeniorAccountant) ⇒
 * تأیید مدیر واحد (نقش UnitManager) = تاییدیه چک ⇒ فقط آنگاه چاپ. تفکیک وظایف: تأییدکننده ≠ صادرکننده،
 * و مدیر ≠ تأییدکنندهٔ حسابداری. نقش‌ها همان TB_TR_ROLE خزانه‌داری است.
 */

public sealed record GetChequeBookQuery(
    string Year,
    int PageNumber = 1,
    int PageSize = 50,
    Guid? BankAccountId = null,
    string? FromDate = null,
    string? ToDate = null,
    bool? Canceled = null,
    bool? Printed = null,
    string? ChequeNo = null,
    decimal? Amount = null,
    string? Description = null,
    ChequeApprovalState? ApprovalState = null,
    bool OnlyUnissued = false) : IRequest<PagedResult<ChequeBookItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>چک‌های قابل انتخاب برای ردیف سند (اختیاری: فقط دسته‌چک‌های حساب‌هایی که به این معین وصل‌اند).</summary>
public sealed record GetAvailableChequesQuery(Guid? AccountCodeId, string? Search) : IRequest<IReadOnlyList<AvailableChequeDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>دسته‌چک‌های صوری قابل انتخاب در ردیف سند.</summary>
public sealed record GetSoriChequeBooksQuery(Guid? AccountCodeId) : IRequest<IReadOnlyList<SoriChequeBookDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetSoriChequeBooksQueryHandler : IRequestHandler<GetSoriChequeBooksQuery, IReadOnlyList<SoriChequeBookDto>>
{
    private readonly IChequeBookRepository _repository;

    public GetSoriChequeBooksQueryHandler(IChequeBookRepository repository) => _repository = repository;

    public Task<IReadOnlyList<SoriChequeBookDto>> Handle(GetSoriChequeBooksQuery request, CancellationToken cancellationToken)
        => _repository.GetSoriBooksAsync(request.VahedCode, request.AccountCodeId, cancellationToken);
}

public sealed record GetChequeApprovalEventsQuery(Guid CheckId) : IRequest<IReadOnlyList<ChequeApprovalEventDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>خلاصهٔ یک چک (برای فرم سند در حالت ویرایش).</summary>
public sealed record GetChequeQuery(Guid CheckId) : IRequest<ChequeSummaryDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><paramref name="IsSori"/>: برگ دسته‌چک صوری (اعلامیه) — «در وجه» و تاریخ برایش الزامی نیست.</summary>
public sealed record ChequeSummaryDto(Guid CheckId, string ChequeNo, string? PayTo, string? ChequeDate, string? Description, bool IsCanceled, bool IsPrinted, bool IsSori);

/// <summary>داده‌های چاپ چک — فقط چک تأییدشده و ابطال‌نشده.</summary>
public sealed record GetChequePrintQuery(Guid CheckId) : IRequest<ChequePrintDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>اقدام گروهی روی کارتابل: صدور دستور پرداخت، تأیید (مرحلهٔ جاری هر چک) یا برگشت.</summary>
public sealed record ChequeApprovalCommand(ChequeApprovalAction Action, IReadOnlyList<Guid> CheckIds, string? Note)
    : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ابطال یا برگرداندن ابطال چک.</summary>
public sealed record SetChequeCanceledCommand(Guid CheckId, bool Canceled) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ثبت «چاپ شد» پس از چاپ چک.</summary>
public sealed record MarkChequePrintedCommand(Guid CheckId) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ChequeBookHandlers :
    IRequestHandler<GetChequeBookQuery, PagedResult<ChequeBookItemDto>>,
    IRequestHandler<GetAvailableChequesQuery, IReadOnlyList<AvailableChequeDto>>,
    IRequestHandler<GetChequeApprovalEventsQuery, IReadOnlyList<ChequeApprovalEventDto>>,
    IRequestHandler<GetChequePrintQuery, ChequePrintDto>,
    IRequestHandler<GetChequeQuery, ChequeSummaryDto>,
    IRequestHandler<ChequeApprovalCommand, int>,
    IRequestHandler<SetChequeCanceledCommand, Unit>,
    IRequestHandler<MarkChequePrintedCommand, Unit>
{
    private readonly IChequeBookRepository _repository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ChequeBookHandlers(
        IChequeBookRepository repository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ChequeBookItemDto>> Handle(GetChequeBookQuery r, CancellationToken cancellationToken)
        => _repository.GetPagedAsync(
            new ChequeBookFilter(
                r.VahedCode, r.Year, r.PageNumber, r.PageSize, r.BankAccountId, Blank(r.FromDate), Blank(r.ToDate),
                r.Canceled, r.Printed, Blank(r.ChequeNo), r.Amount, Blank(r.Description), r.ApprovalState, r.OnlyUnissued),
            cancellationToken);

    public Task<IReadOnlyList<AvailableChequeDto>> Handle(GetAvailableChequesQuery r, CancellationToken cancellationToken)
        => _repository.GetAvailableAsync(r.VahedCode, r.AccountCodeId, Blank(r.Search), cancellationToken);

    public async Task<IReadOnlyList<ChequeApprovalEventDto>> Handle(GetChequeApprovalEventsQuery r, CancellationToken cancellationToken)
    {
        _ = await LoadCheckAsync(r.CheckId, r.VahedCode, cancellationToken);
        return await _repository.GetEventsAsync(r.CheckId, cancellationToken);
    }

    public async Task<ChequeSummaryDto> Handle(GetChequeQuery r, CancellationToken cancellationToken)
    {
        var c = await LoadCheckAsync(r.CheckId, r.VahedCode, cancellationToken);
        return new ChequeSummaryDto(c.ID, c.CHEQ_NO, c.PAYTO, c.CHEQ_DATE, c.PAPER_DESC,
            c.EBTAL == CheckCancelStatus.Canceled, c.PRINT == CheckPrintStatus.Printed,
            Accounting.Application.CheckBooks.CheckBookLeaves.IsSori(c.CHECKBOOK?.CHECKBOOK_TYPE));
    }

    public async Task<ChequePrintDto> Handle(GetChequePrintQuery r, CancellationToken cancellationToken)
    {
        var cheque = await LoadCheckAsync(r.CheckId, r.VahedCode, cancellationToken);
        EnsurePrintable(cheque, await _repository.GetApprovalForUpdateAsync(cheque.ID, cancellationToken));
        return await _repository.GetPrintDataAsync(cheque.ID, cancellationToken)
            ?? throw new ChequeConflictException($"چک {cheque.CHEQ_NO} در سندی به کار نرفته است.");
    }

    public async Task<Unit> Handle(MarkChequePrintedCommand r, CancellationToken cancellationToken)
    {
        var cheque = await LoadCheckAsync(r.CheckId, r.VahedCode, cancellationToken);
        EnsurePrintable(cheque, await _repository.GetApprovalForUpdateAsync(cheque.ID, cancellationToken));
        cheque.PRINT = CheckPrintStatus.Printed;
        cheque.CHANGEUSERID = _currentUser.UserId;
        cheque.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetChequeCanceledCommand r, CancellationToken cancellationToken)
    {
        var cheque = await LoadCheckAsync(r.CheckId, r.VahedCode, cancellationToken);
        cheque.EBTAL = r.Canceled ? CheckCancelStatus.Canceled : CheckCancelStatus.NotCanceled;
        cheque.CHANGEUSERID = _currentUser.UserId;
        cheque.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<int> Handle(ChequeApprovalCommand r, CancellationToken cancellationToken)
    {
        var user = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var done = 0;
        foreach (var checkId in r.CheckIds.Distinct())
        {
            var cheque = await LoadCheckAsync(checkId, r.VahedCode, cancellationToken);
            var approval = await _repository.GetApprovalForUpdateAsync(checkId, cancellationToken);
            if (approval is { ISDELETED: true })
                approval = null;
            var from = approval?.STATE;

            switch (r.Action)
            {
                case ChequeApprovalAction.IssuePaymentOrder:
                {
                    if (approval is not null && approval.STATE != ChequeApprovalState.Returned)
                        throw new ChequeConflictException($"برای چک {cheque.CHEQ_NO} قبلاً دستور پرداخت صادر شده است.");
                    if (cheque.EBTAL == CheckCancelStatus.Canceled)
                        throw new ChequeConflictException($"چک {cheque.CHEQ_NO} ابطال شده است.");
                    var detail = await _repository.GetActiveDetailAsync(checkId, cancellationToken)
                        ?? throw new ChequeConflictException($"چک {cheque.CHEQ_NO} در هیچ سندی به کار نرفته است.");
                    if (string.IsNullOrWhiteSpace(cheque.PAYTO) || string.IsNullOrWhiteSpace(cheque.CHEQ_DATE))
                        throw new ChequeConflictException($"«در وجه» و تاریخ چک {cheque.CHEQ_NO} در سند وارد نشده است.");

                    if (approval is null)
                    {
                        approval = new TB_CHECK_APPROVAL
                        {
                            ID = Guid.NewGuid(),
                            CHECK_ID = checkId,
                            VAHEDCODE = r.VahedCode,
                            YEAR = detail.YEAR,
                            ADDUSERID = user,
                            CREATEDDATE = now,
                            ISDELETED = false,
                        };
                        await _repository.AddApprovalAsync(approval, cancellationToken);
                    }

                    approval.STATE = ChequeApprovalState.PendingAccounting;
                    approval.PREPARED_BY = user;
                    approval.PREPARED_DATE = now;
                    approval.ACCOUNTING_BY = null;
                    approval.ACCOUNTING_DATE = null;
                    approval.MANAGER_BY = null;
                    approval.MANAGER_DATE = null;
                    approval.NOTE = Blank(r.Note);
                    break;
                }

                case ChequeApprovalAction.ApproveAccounting:
                case ChequeApprovalAction.ApproveManager:
                {
                    if (approval is null)
                        throw new ChequeConflictException($"برای چک {cheque.CHEQ_NO} دستور پرداخت صادر نشده است.");
                    if (cheque.EBTAL == CheckCancelStatus.Canceled)
                        throw new ChequeConflictException($"چک {cheque.CHEQ_NO} ابطال شده است.");

                    if (approval.STATE == ChequeApprovalState.PendingAccounting)
                    {
                        await _roleAuthorizer.EnsureHasRoleAsync(r.VahedCode, [TreasuryRole.SeniorAccountant], cancellationToken);
                        if (approval.PREPARED_BY == user)
                            throw new ChequeConflictException($"صادرکنندهٔ دستور پرداخت چک {cheque.CHEQ_NO} نمی‌تواند آن را تأیید کند.");
                        approval.STATE = ChequeApprovalState.PendingManager;
                        approval.ACCOUNTING_BY = user;
                        approval.ACCOUNTING_DATE = now;
                    }
                    else if (approval.STATE == ChequeApprovalState.PendingManager)
                    {
                        await _roleAuthorizer.EnsureHasRoleAsync(r.VahedCode, [TreasuryRole.UnitManager], cancellationToken);
                        if (approval.PREPARED_BY == user || approval.ACCOUNTING_BY == user)
                            throw new ChequeConflictException($"تأییدکنندهٔ قبلی چک {cheque.CHEQ_NO} نمی‌تواند تأیید مدیر را هم انجام دهد.");
                        approval.STATE = ChequeApprovalState.Confirmed;
                        approval.MANAGER_BY = user;
                        approval.MANAGER_DATE = now;
                    }
                    else
                    {
                        throw new ChequeConflictException($"چک {cheque.CHEQ_NO} در انتظار تأیید نیست.");
                    }

                    break;
                }

                case ChequeApprovalAction.Return:
                {
                    if (approval is null || approval.STATE == ChequeApprovalState.Returned)
                        throw new ChequeConflictException($"چک {cheque.CHEQ_NO} در کارتابل نیست.");
                    if (cheque.PRINT == CheckPrintStatus.Printed)
                        throw new ChequeConflictException($"چک {cheque.CHEQ_NO} چاپ شده و قابل برگشت نیست.");
                    if (string.IsNullOrWhiteSpace(r.Note))
                        throw new ChequeConflictException("برای برگشت، علت را بنویسید.");
                    await _roleAuthorizer.EnsureHasRoleAsync(
                        r.VahedCode, [TreasuryRole.SeniorAccountant, TreasuryRole.UnitManager], cancellationToken);
                    approval.STATE = ChequeApprovalState.Returned;
                    approval.NOTE = r.Note.Trim();
                    break;
                }

                default:
                    throw new ChequeConflictException("اقدام نامعتبر است.");
            }

            approval.CHANGEUSERID = user;
            approval.UPDATEDDATE = now;
            await _repository.AddApprovalEventAsync(new TB_CHECK_APPROVAL_EVENT
            {
                ID = Guid.NewGuid(),
                APPROVAL_ID = approval.ID,
                ACTION = r.Action == ChequeApprovalAction.ApproveManager && from == ChequeApprovalState.PendingAccounting
                    ? ChequeApprovalAction.ApproveAccounting
                    : r.Action == ChequeApprovalAction.ApproveAccounting && from == ChequeApprovalState.PendingManager
                        ? ChequeApprovalAction.ApproveManager
                        : r.Action,
                FROM_STATE = from,
                TO_STATE = approval.STATE,
                USERID = user,
                NOTE = Blank(r.Note),
                CREATEDDATE = now,
            }, cancellationToken);
            done++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return done;
    }

    private async Task<TB_CHECK> LoadCheckAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
        => await _repository.GetCheckForUpdateAsync(id, vahedCode, cancellationToken)
            ?? throw new NotFoundException("Check", id);

    private static void EnsurePrintable(TB_CHECK cheque, TB_CHECK_APPROVAL? approval)
    {
        if (cheque.EBTAL == CheckCancelStatus.Canceled)
            throw new ChequeConflictException($"چک {cheque.CHEQ_NO} ابطال شده است.");
        if (approval is not { ISDELETED: false, STATE: ChequeApprovalState.Confirmed })
            throw new ChequeConflictException($"چک {cheque.CHEQ_NO} هنوز تأیید نهایی (تاییدیه) نشده است.");
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public sealed class GetChequeBookQueryValidator : AbstractValidator<GetChequeBookQuery>
{
    public GetChequeBookQueryValidator()
    {
        RuleFor(x => x.Year).Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم باشد.");
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);
        RuleFor(x => x.FromDate).Matches("^[0-9]{8}$").When(x => !string.IsNullOrWhiteSpace(x.FromDate));
        RuleFor(x => x.ToDate).Matches("^[0-9]{8}$").When(x => !string.IsNullOrWhiteSpace(x.ToDate));
        RuleFor(x => x.ChequeNo).MaximumLength(10);
        RuleFor(x => x.Description).MaximumLength(200);
    }
}

public sealed class ChequeApprovalCommandValidator : AbstractValidator<ChequeApprovalCommand>
{
    public ChequeApprovalCommandValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.CheckIds).NotEmpty().WithMessage("دست‌کم یک چک انتخاب کنید.");
        RuleFor(x => x.CheckIds.Count).LessThanOrEqualTo(200).WithMessage("حداکثر ۲۰۰ چک در هر بار.");
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
