using System.Globalization;
using System.Text.Json.Serialization;
using Accounting.Application.Common.BankDisk;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.BankCards;

/*
 * کارت حساب جاری (عملیات) — معادل صفحهٔ «اسناد حساب جاری» (current-account) و BankCartDetailController سیستم
 * قدیم، روی همان جدول TB_BANKCARTDETAIL. جهت مبالغ عین مرجع: واریز بانک = CREDITOR، برداشت = DEBTOR.
 * مغایرت‌گیری عین ReconciliationCommandHandler مرجع: ردیف چک ↔ چکِ دسته‌چک همین حساب با ردیف سند به همان
 * شماره و مبلغ؛ ردیف فیش/حواله ↔ فیش با همان شماره و مبلغ. نتیجه در CHECK_ID/RECEIP_ID ردیف کارت و
 * DATE_RSID روی چک/فیش. اصلاح‌ها نسبت به مرجع: مبلغ چک از بدهکار کارت (مرجع بستانکار را می‌گرفت که برای
 * چک صفر است)، فیش با بدهکارِ سند (مرجع بستانکار)، و شناسهٔ فیش در RECEIP_ID (مرجع در CHECK_ID می‌نوشت).
 */

/// <summary>کارت یک حساب در یک ماه.</summary>
public sealed record GetBankCardQuery(string Year, Guid BankAccountId, string Month) : IRequest<BankCardDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>صورت مغایرت تا پایان ماه.</summary>
public sealed record GetBankCardReconciliationQuery(string Year, Guid BankAccountId, string Month)
    : IRequest<BankCardReconciliationDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ردیف دستی. <paramref name="Date"/> تاریخ رسید/اجرا (YYYYMMDD)؛ ماه از آن.</summary>
public sealed record SaveBankCardRowCommand(
    Guid? Id,
    string Year,
    Guid BankAccountId,
    string Date,
    string? Number,
    CheckReceiptType Type,
    bool IsDeposit,
    decimal Amount) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record DeleteBankCardRowCommand(Guid Id) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ورود دیسکت بانک رفاه برای یک حساب و یک ماه.</summary>
public sealed record ImportBankCardDiskCommand(string Year, Guid BankAccountId, string Month, string? FileName, byte[] Content)
    : IRequest<BankCardImportResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record BankCardImportResult(int Imported, int Skipped);

/// <summary>مغایرت‌گیری خودکار ردیف‌های یک ماه با چک/فیش‌های دفتر.</summary>
public sealed record ReconcileBankCardCommand(string Year, Guid BankAccountId, string Month)
    : IRequest<BankCardReconcileResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record BankCardReconcileResult(int Matched, int Remaining);

/// <summary>برگرداندن مغایرت‌گیری یک ردیف (و پاک کردن تاریخ وصول چک/فیش).</summary>
public sealed record UnreconcileBankCardRowCommand(Guid Id) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class BankCardHandlers :
    IRequestHandler<GetBankCardQuery, BankCardDto>,
    IRequestHandler<GetBankCardReconciliationQuery, BankCardReconciliationDto>,
    IRequestHandler<SaveBankCardRowCommand, Guid>,
    IRequestHandler<DeleteBankCardRowCommand, Unit>,
    IRequestHandler<ImportBankCardDiskCommand, BankCardImportResult>,
    IRequestHandler<ReconcileBankCardCommand, BankCardReconcileResult>,
    IRequestHandler<UnreconcileBankCardRowCommand, Unit>
{
    private readonly IBankCardRepository _repository;
    private readonly ITreasuryBankAccountBalanceReadRepository _balanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public BankCardHandlers(
        IBankCardRepository repository,
        ITreasuryBankAccountBalanceReadRepository balanceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _balanceRepository = balanceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<BankCardDto> Handle(GetBankCardQuery request, CancellationToken cancellationToken)
    {
        var account = await LoadAccountAsync(request.BankAccountId, request.VahedCode, cancellationToken);
        var rows = (await _repository.GetRowsAsync(request.VahedCode, request.Year, account.AccountNumber, request.Month, cancellationToken))
            .OrderBy(r => r.RECIVDATE)
            .ThenBy(r => r.CHEQNO)
            .Select(BankCardRowMapper.ToDto)
            .ToList();
        return new BankCardDto(
            account.Id, account.AccountNumber, request.Year, request.Month, rows,
            rows.Sum(r => r.Deposit), rows.Sum(r => r.Withdrawal), rows.Count(r => r.IsReconciled));
    }

    public async Task<BankCardReconciliationDto> Handle(GetBankCardReconciliationQuery request, CancellationToken cancellationToken)
    {
        var account = await LoadAccountAsync(request.BankAccountId, request.VahedCode, cancellationToken);
        var accountCodeId = account.AccountCodeId
            ?? throw new BankCardConflictException("حساب بانکی به معین وصل نیست؛ ماندهٔ دفتری قابل محاسبه نیست.");
        var toDate = MonthEnd(request.Year, request.Month);

        var bookBalance = await _balanceRepository.GetBalanceAsync(
            accountCodeId, account.TafsiliIds, request.VahedCode, request.Year, toDate, cancellationToken);

        // اقلام باز بانک: همهٔ ردیف‌های مغایرت‌گیری‌نشدهٔ کارت از ابتدای سال تا این ماه.
        var openRows = (await _repository.GetRowsAsync(request.VahedCode, request.Year, account.AccountNumber, null, cancellationToken))
            .Where(r => r.CHECK_ID == null && r.RECEIP_ID == null && string.CompareOrdinal(r.MONTH, request.Month) <= 0)
            .OrderBy(r => r.RECIVDATE)
            .Select(BankCardRowMapper.ToDto)
            .ToList();

        var bookItems = await _repository.GetOutstandingBookItemsAsync(
            accountCodeId, account.TafsiliIds, request.VahedCode, request.Year, toDate, cancellationToken);

        return new BankCardReconciliationDto(
            account.Id, account.AccountNumber, account.AccountHolder, request.Year, request.Month, toDate, bookBalance,
            openRows.Where(r => r.Deposit > 0).ToList(),
            openRows.Where(r => r.Withdrawal > 0).ToList(),
            bookItems.Where(b => b.Debit > 0).ToList(),
            bookItems.Where(b => b.Credit > 0).ToList());
    }

    public async Task<Guid> Handle(SaveBankCardRowCommand request, CancellationToken cancellationToken)
    {
        var account = await LoadAccountAsync(request.BankAccountId, request.VahedCode, cancellationToken);
        var now = DateTime.UtcNow;
        TB_BANKCARTDETAIL row;
        if (request.Id is { } id)
        {
            row = await LoadRowAsync(id, request.VahedCode, cancellationToken);
            EnsureNotReconciled(row, "ویرایش");
            row.UPDATEDDATE = now;
            row.CHANGEUSERID = _currentUser.UserId;
        }
        else
        {
            row = NewRow(account, request.VahedCode, request.Year, now);
            await _repository.AddRowAsync(row, cancellationToken);
        }

        row.RECIVDATE = request.Date;
        row.MONTH = request.Date.Substring(4, 2);
        row.CHEQNO = string.IsNullOrWhiteSpace(request.Number) ? null : request.Number.Trim();
        row.CHECKRECEIPTTYPE = request.Type;
        row.CREDITOR = request.IsDeposit ? request.Amount : 0;
        row.DEBTOR = request.IsDeposit ? 0 : request.Amount;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return row.ID;
    }

    public async Task<Unit> Handle(DeleteBankCardRowCommand request, CancellationToken cancellationToken)
    {
        var row = await LoadRowAsync(request.Id, request.VahedCode, cancellationToken);
        EnsureNotReconciled(row, "حذف");
        row.ISDELETED = true;
        row.UPDATEDDATE = DateTime.UtcNow;
        row.CHANGEUSERID = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<BankCardImportResult> Handle(ImportBankCardDiskCommand request, CancellationToken cancellationToken)
    {
        RefahDiskFormat.EnsureFileName(request.FileName);
        var account = await LoadAccountAsync(request.BankAccountId, request.VahedCode, cancellationToken);
        await using var stream = new MemoryStream(request.Content);
        var records = await RefahDiskFormat.ParseAsync(stream, account.AccountNumber, cancellationToken);

        var other = records.FirstOrDefault(r => r.Date[..4] != request.Year || r.Date.Substring(4, 2) != request.Month);
        if (other is not null)
            throw new BankCardConflictException(
                $"دیسکت مربوط به ماه انتخابی نیست (ردیف {other.LineNo} تاریخ {other.Date}).");

        var existing = await _repository.GetRowsAsync(request.VahedCode, request.Year, account.AccountNumber, request.Month, cancellationToken);
        var keys = existing.Select(Key).ToHashSet();
        var noticeNo = existing.Count(r => r.CHEQNO?.StartsWith("اعلا-", StringComparison.Ordinal) == true);
        var now = DateTime.UtcNow;
        var imported = 0;
        var skipped = 0;
        foreach (var rec in records)
        {
            var row = NewRow(account, request.VahedCode, request.Year, now);
            row.RECIVDATE = rec.Date;
            row.MONTH = request.Month;
            row.CREDITOR = rec.IsDeposit ? rec.Amount : 0;
            row.DEBTOR = rec.IsDeposit ? 0 : rec.Amount;
            row.CHECKRECEIPTTYPE = rec.DocType switch
            {
                RefahDiskDocType.BankNotice => CheckReceiptType.SoriCheck,
                RefahDiskDocType.Fish => CheckReceiptType.Fish,
                _ => CheckReceiptType.RealCheck,
            };
            row.CHEQNO = rec.Number;
            if (keys.Contains(Key(row)))
            {
                skipped++;
                continue;
            }

            if (rec.DocType == RefahDiskDocType.BankNotice)
                row.CHEQNO = $"اعلا-{++noticeNo:D2}";
            keys.Add(Key(row));
            await _repository.AddRowAsync(row, cancellationToken);
            imported++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new BankCardImportResult(imported, skipped);
    }

    public async Task<BankCardReconcileResult> Handle(ReconcileBankCardCommand request, CancellationToken cancellationToken)
    {
        var account = await LoadAccountAsync(request.BankAccountId, request.VahedCode, cancellationToken);
        var yearRows = await _repository.GetRowsAsync(request.VahedCode, request.Year, account.AccountNumber, null, cancellationToken);
        var usedChecks = yearRows.Where(r => r.CHECK_ID != null).Select(r => r.CHECK_ID!.Value).ToHashSet();
        var usedReceipts = yearRows.Where(r => r.RECEIP_ID != null).Select(r => r.RECEIP_ID!.Value).ToHashSet();
        var open = yearRows
            .Where(r => r.MONTH == request.Month && r.CHECK_ID == null && r.RECEIP_ID == null && Normalize(r.CHEQNO) is not null)
            .ToList();

        var chequeRows = open.Where(r => (r.DEBTOR ?? 0) > 0
            && r.CHECKRECEIPTTYPE is CheckReceiptType.RealCheck or CheckReceiptType.SoriCheck).ToList();
        var receiptRows = open.Where(r => (r.CREDITOR ?? 0) > 0
            && r.CHECKRECEIPTTYPE is CheckReceiptType.Fish or CheckReceiptType.Havale).ToList();

        var matched = 0;
        if (chequeRows.Count > 0)
        {
            var cheques = await _repository.FindChequesAsync(
                account.Id, request.VahedCode, request.Year, chequeRows.Select(r => Normalize(r.CHEQNO)!).Distinct().ToList(), cancellationToken);
            foreach (var row in chequeRows)
            {
                var hit = Single(cheques, row.CHEQNO, row.DEBTOR ?? 0, usedChecks);
                if (hit is null)
                    continue;
                row.CHECK_ID = hit.DocumentId;
                usedChecks.Add(hit.DocumentId);
                await _repository.SetCheckReceivedDateAsync(hit.DocumentId, row.RECIVDATE, cancellationToken);
                Touch(row);
                matched++;
            }
        }

        if (receiptRows.Count > 0 && account.AccountCodeId is { } accountCodeId)
        {
            var receipts = await _repository.FindReceiptsAsync(
                accountCodeId, request.VahedCode, request.Year, receiptRows.Select(r => Normalize(r.CHEQNO)!).Distinct().ToList(), cancellationToken);
            foreach (var row in receiptRows)
            {
                var hit = Single(receipts, row.CHEQNO, row.CREDITOR ?? 0, usedReceipts);
                if (hit is null)
                    continue;
                row.RECEIP_ID = hit.DocumentId;
                usedReceipts.Add(hit.DocumentId);
                await _repository.SetReceiptReceivedDateAsync(hit.DocumentId, row.RECIVDATE, cancellationToken);
                Touch(row);
                matched++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var monthOpen = yearRows.Count(r => r.MONTH == request.Month && r.CHECK_ID == null && r.RECEIP_ID == null);
        return new BankCardReconcileResult(matched, monthOpen);
    }

    public async Task<Unit> Handle(UnreconcileBankCardRowCommand request, CancellationToken cancellationToken)
    {
        var row = await LoadRowAsync(request.Id, request.VahedCode, cancellationToken);
        if (row.CHECK_ID is { } checkId)
            await _repository.SetCheckReceivedDateAsync(checkId, null, cancellationToken);
        if (row.RECEIP_ID is { } receiptId)
            await _repository.SetReceiptReceivedDateAsync(receiptId, null, cancellationToken);
        row.CHECK_ID = null;
        row.RECEIP_ID = null;
        Touch(row);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async Task<BankCardAccount> LoadAccountAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountAsync(id, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", id);
        if (account.AccountNumber.Length > 13)
            throw new BankCardConflictException("شمارهٔ این حساب بیش از ۱۳ رقم است و در کارت حساب جاری جا نمی‌شود.");
        return account;
    }

    private async Task<TB_BANKCARTDETAIL> LoadRowAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
        => await _repository.GetRowForUpdateAsync(id, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankCartDetail", id);

    private static void EnsureNotReconciled(TB_BANKCARTDETAIL row, string action)
    {
        if (row.CHECK_ID != null || row.RECEIP_ID != null)
            throw new BankCardConflictException($"ردیف مغایرت‌گیری‌شده قابل {action} نیست؛ ابتدا مغایرت‌گیری آن را برگردانید.");
    }

    private TB_BANKCARTDETAIL NewRow(BankCardAccount account, string vahedCode, string year, DateTime now) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNTNUMBER = account.AccountNumber,
        BANK_ID = account.BankId,
        BRANCH_ID = account.BranchId,
        VAHEDCODE = vahedCode,
        YEAR = year,
        ADDUSERID = _currentUser.UserId,
        CREATEDDATE = now,
        ISDELETED = false,
    };

    private void Touch(TB_BANKCARTDETAIL row)
    {
        row.UPDATEDDATE = DateTime.UtcNow;
        row.CHANGEUSERID = _currentUser.UserId;
    }

    private static (string?, string?, CheckReceiptType?, decimal, decimal) Key(TB_BANKCARTDETAIL r)
        => (r.RECIVDATE,
            r.CHECKRECEIPTTYPE == CheckReceiptType.SoriCheck ? null : Normalize(r.CHEQNO),
            r.CHECKRECEIPTTYPE,
            r.CREDITOR ?? 0,
            r.DEBTOR ?? 0);

    private static BankCardMatch? Single(IReadOnlyList<BankCardMatch> pool, string? number, decimal amount, HashSet<Guid> used)
    {
        var no = Normalize(number);
        var hits = pool.Where(m => Normalize(m.Number) == no && m.Amount == amount && !used.Contains(m.DocumentId)).ToList();
        return hits.Count == 1 ? hits[0] : null;
    }

    /// <summary>فقط شمارهٔ عددی، بی صفر پیشرو؛ «اعلا-01» ⇒ null.</summary>
    public static string? Normalize(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v) || !v.All(char.IsAsciiDigit))
            return null;
        var t = v.TrimStart('0');
        return t.Length == 0 ? null : t;
    }

    public static string MonthEnd(string year, string month)
    {
        var days = new PersianCalendar().GetDaysInMonth(int.Parse(year, CultureInfo.InvariantCulture), int.Parse(month, CultureInfo.InvariantCulture));
        return $"{year}{month}{days:D2}";
    }
}

internal static class BankCardRules
{
    public static IRuleBuilderOptions<T, string> Year<T>(IRuleBuilder<T, string> r)
        => r.Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم باشد.");

    public static IRuleBuilderOptions<T, string> Month<T>(IRuleBuilder<T, string> r)
        => r.Matches("^(0[1-9]|1[0-2])$").WithMessage("ماه باید دو رقم ۰۱ تا ۱۲ باشد.");
}

public sealed class GetBankCardQueryValidator : AbstractValidator<GetBankCardQuery>
{
    public GetBankCardQueryValidator()
    {
        BankCardRules.Year(RuleFor(x => x.Year));
        BankCardRules.Month(RuleFor(x => x.Month));
        RuleFor(x => x.BankAccountId).NotEmpty().WithMessage("حساب جاری الزامی است.");
    }
}

public sealed class GetBankCardReconciliationQueryValidator : AbstractValidator<GetBankCardReconciliationQuery>
{
    public GetBankCardReconciliationQueryValidator()
    {
        BankCardRules.Year(RuleFor(x => x.Year));
        BankCardRules.Month(RuleFor(x => x.Month));
        RuleFor(x => x.BankAccountId).NotEmpty().WithMessage("حساب جاری الزامی است.");
    }
}

public sealed class SaveBankCardRowCommandValidator : AbstractValidator<SaveBankCardRowCommand>
{
    public SaveBankCardRowCommandValidator()
    {
        BankCardRules.Year(RuleFor(x => x.Year));
        RuleFor(x => x.BankAccountId).NotEmpty().WithMessage("حساب جاری الزامی است.");
        RuleFor(x => x.Date).Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.");
        RuleFor(x => x).Must(x => x.Date.StartsWith(x.Year, StringComparison.Ordinal))
            .WithMessage("تاریخ باید در سال مالی جاری باشد.").When(x => x.Date?.Length == 8);
        RuleFor(x => x.Number).MaximumLength(8).WithMessage("شمارهٔ چک/فیش حداکثر ۸ کاراکتر است.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("نوع اوراق بانکی نامعتبر است.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ باید بزرگ‌تر از صفر باشد.");
    }
}

public sealed class ImportBankCardDiskCommandValidator : AbstractValidator<ImportBankCardDiskCommand>
{
    public ImportBankCardDiskCommandValidator()
    {
        BankCardRules.Year(RuleFor(x => x.Year));
        BankCardRules.Month(RuleFor(x => x.Month));
        RuleFor(x => x.BankAccountId).NotEmpty().WithMessage("حساب جاری الزامی است.");
        RuleFor(x => x.Content).NotEmpty().WithMessage("فایل دیسکت الزامی است.");
    }
}

public sealed class ReconcileBankCardCommandValidator : AbstractValidator<ReconcileBankCardCommand>
{
    public ReconcileBankCardCommandValidator()
    {
        BankCardRules.Year(RuleFor(x => x.Year));
        BankCardRules.Month(RuleFor(x => x.Month));
        RuleFor(x => x.BankAccountId).NotEmpty().WithMessage("حساب جاری الزامی است.");
    }
}
