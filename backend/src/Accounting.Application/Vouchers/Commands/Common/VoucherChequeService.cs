using Accounting.Application.CheckBooks;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// اطلاعات چک ردیف سند (دفتر چک): در وجه، تاریخ چک (YYYYMMDD) و بابت — روی <c>TB_CHECK</c> نوشته
/// می‌شوند (<c>PAYTO</c>، <c>CHEQ_DATE</c>، <c>PAPER_DESC</c>). null = دست نزن.
/// <paramref name="SoriCheckBookId"/>: چک صوری — وقتی ردیف هنوز چک ندارد، شمارهٔ بعدی این دسته‌چک صوری صادر و
/// برگش در <c>TB_CHECK</c> ساخته می‌شود.
/// </summary>
public sealed record VoucherChequeInfoInput(string? PayTo, string? ChequeDate, string? Description, Guid? SoriCheckBookId = null);

/// <summary>
/// چکِ ردیف سند — اعتبارسنجی و ثبت اطلاعات چک (دفتر چک). در Create/Update ردیف سند صدا زده می‌شود.
/// قواعد: چک همین واحد؛ ابطال‌نشده؛ در ردیف سند فعال دیگری نیامده؛ چک چاپ‌شده یا در مرحلهٔ تأیید
/// مدیر/تأییدشده قابل تغییر اطلاعات نیست.
/// </summary>
public interface IVoucherChequeService
{
    /// <summary>چک مؤثر ردیف را برمی‌گرداند (برای چک صوری، شناسهٔ برگی که همین‌جا ساخته شد).</summary>
    Task<Guid?> ApplyAsync(
        Guid? checkId, Guid detailId, bool checkChanged, VoucherChequeInfoInput? info, string vahedCode,
        CancellationToken cancellationToken = default);
}

public sealed class VoucherChequeService : IVoucherChequeService
{
    private readonly IChequeBookRepository _repository;
    private readonly ICheckBookRepository _checkBooks;
    private readonly ICurrentUser _currentUser;

    public VoucherChequeService(IChequeBookRepository repository, ICheckBookRepository checkBooks, ICurrentUser currentUser)
    {
        _repository = repository;
        _checkBooks = checkBooks;
        _currentUser = currentUser;
    }

    public async Task<Guid?> ApplyAsync(
        Guid? checkId, Guid detailId, bool checkChanged, VoucherChequeInfoInput? info, string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (checkId is null && info?.SoriCheckBookId is { } soriBookId)
            return await IssueSoriAsync(soriBookId, info, vahedCode, cancellationToken);

        if (checkId is not { } id)
            return null;

        var cheque = await _repository.GetCheckForUpdateAsync(id, vahedCode, cancellationToken)
            ?? throw new NotFoundException("Check", id);

        if (checkChanged)
        {
            if (cheque.EBTAL == CheckCancelStatus.Canceled)
                throw new ChequeConflictException($"چک {cheque.CHEQ_NO} ابطال شده است.");
            if (await _repository.IsUsedByOtherDetailAsync(id, detailId, cancellationToken))
                throw new ChequeConflictException($"چک {cheque.CHEQ_NO} در ردیف سند دیگری استفاده شده است.");
        }

        if (info is null)
            return id;

        var payTo = Blank(info.PayTo);
        var date = Blank(info.ChequeDate);
        var desc = Blank(info.Description);
        if (payTo == cheque.PAYTO && date == cheque.CHEQ_DATE && desc == cheque.PAPER_DESC)
            return id;

        var approval = await _repository.GetApprovalForUpdateAsync(id, cancellationToken);
        if (cheque.PRINT == CheckPrintStatus.Printed
            || approval is { ISDELETED: false, STATE: ChequeApprovalState.PendingManager or ChequeApprovalState.Confirmed })
        {
            throw new ChequeConflictException(
                $"چک {cheque.CHEQ_NO} چاپ یا تأیید شده است؛ «در وجه»، تاریخ و شرح آن قابل تغییر نیست.");
        }

        cheque.PAYTO = payTo;
        cheque.CHEQ_DATE = date;
        cheque.PAPER_DESC = desc;
        cheque.UPDATEDDATE = DateTime.UtcNow;
        return id;
    }

    /// <summary>
    /// چک صوری — عین <c>GetValidSoriCheckNumberAsync</c> مرجع: شمارهٔ بعدی = بزرگ‌ترین شمارهٔ موجود + ۱ (یا اولین
    /// شمارهٔ بازه)؛ برگ <c>TB_CHECK</c> همین‌جا با «در وجه»/تاریخ/بابت ساخته می‌شود.
    /// </summary>
    private async Task<Guid?> IssueSoriAsync(
        Guid checkBookId, VoucherChequeInfoInput info, string vahedCode, CancellationToken cancellationToken)
    {
        var book = await _checkBooks.GetForUpdateAsync(checkBookId, vahedCode, cancellationToken);
        if (book is null || book.ISDELETED)
            throw new NotFoundException("CheckBook", checkBookId);
        if (!CheckBookLeaves.IsSori(book.CHECKBOOK_TYPE))
            throw new ChequeConflictException("دسته‌چک انتخابی صوری نیست؛ برگ چک واقعی را از فهرست برگ‌ها انتخاب کنید.");

        var next = CheckBookLeaves.NextSoriNumber(book, await _checkBooks.GetMaxChequeNoAsync(book.ID, cancellationToken))
            ?? throw new ChequeConflictException(
                $"شماره‌های دسته‌چک صوری {book.FROMCHECKNUMBER} تا {book.TOCHECKNUMBER} تمام شده است.");

        var now = DateTime.UtcNow;
        var cheque = new TB_CHECK
        {
            ID = Guid.NewGuid(),
            CHECKBOOK_ID = book.ID,
            CHEQ_NO = next,
            CHEQ_DATE = Blank(info.ChequeDate),
            PAYTO = Blank(info.PayTo),
            PAPER_DESC = Blank(info.Description),
            EBTAL = CheckCancelStatus.NotCanceled,
            PRINT = CheckPrintStatus.None,
            VAHEDCODE = book.VAHEDCODE,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };
        book.TB_CHECKs.Add(cheque);
        return cheque.ID;
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>قواعد اطلاعات چک در Validatorهای ردیف سند.</summary>
public sealed class VoucherChequeInfoInputValidator : AbstractValidator<VoucherChequeInfoInput>
{
    public VoucherChequeInfoInputValidator()
    {
        RuleFor(x => x.PayTo).MaximumLength(200).WithMessage("«در وجه» حداکثر ۲۰۰ کاراکتر است.");
        RuleFor(x => x.Description).MaximumLength(800).WithMessage("شرح چک حداکثر ۸۰۰ کاراکتر است.");
        RuleFor(x => x.ChequeDate).Matches("^[0-9]{8}$").WithMessage("تاریخ چک باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ChequeDate));
    }
}
