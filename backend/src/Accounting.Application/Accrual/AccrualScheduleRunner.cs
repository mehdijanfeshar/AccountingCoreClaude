using Accounting.Application.Commands.Vouchers.ReverseVoucher;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Accrual;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Accrual;

public enum AccrualRunOutcome { Ran, AlreadyDone, Skipped }

public sealed record AccrualRunResult(AccrualRunOutcome Outcome, Guid? VoucherId, string? VoucherNo);

/// <summary>
/// اجرای یک سطر زمان‌بندی در تراکنش صریح: قفل سطر ⇒ اگر باز نیست نتیجهٔ قبلی (idempotent) ⇒ برگشت با
/// <see cref="VoucherReverser"/> یا سهم ماه با <see cref="IVoucherWriter"/> (سند «موقت»، تاریخ = سررسید) ⇒ اجراشده ⇒ commit.
/// مشترک بین دکمهٔ «اجرا الان» و کار روزانه.
/// </summary>
public sealed class AccrualScheduleRunner
{
    private readonly IAccrualScheduleRepository _repo;
    private readonly VoucherReverser _reverser;
    private readonly IVoucherWriter _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AccrualScheduleRunner(IAccrualScheduleRepository repo, VoucherReverser reverser, IVoucherWriter writer,
        IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _reverser = reverser; _writer = writer; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    /// <param name="vahedCode">واحد کاربر (اجرای دستی)؛ null = کار روزانه برای همهٔ واحدها.</param>
    /// <param name="skipLocked">کار روزانه: سطری که نمونهٔ دیگری قفل کرده رد می‌شود (<see cref="AccrualRunOutcome.Skipped"/>).</param>
    public async Task<AccrualRunResult> RunAsync(Guid id, string? vahedCode, bool skipLocked, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var row = await _repo.LockAsync(id, skipLocked, ct);
            if (row is null && skipLocked)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return new(AccrualRunOutcome.Skipped, null, null);
            }
            // سطر واحد دیگر مثل «نیست» — وجودش هم افشا نمی‌شود.
            if (row is null || (vahedCode is not null && row.VahedCode != vahedCode))
                throw new NotFoundException("AccrualSchedule", id);

            if (row.State == AccrualScheduleState.Done)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return new(AccrualRunOutcome.AlreadyDone, row.ResultVoucherId, null);
            }
            if (row.State == AccrualScheduleState.Cancelled)
                throw new BusinessRuleException("این سطر لغو شده و اجرا نمی‌شود.");

            var (voucherId, voucherNo) = row.Kind == AccrualScheduleKind.Reversal
                ? await _reverser.ReverseAsync(row.SourceVoucherId, row.VahedCode, new VoucherReverseOptions(
                    DateDoc: row.DueDate, DocLife: DocLife.Temporary, HeadDescription: Truncate(row.Description ?? "برگشت خودکار برآورد", 250),
                    IsAutomatic: true, RequireBalanced: true), ct)
                : await _writer.CreateDraftVoucherAsync(BuildShareDraft(row), ct);

            row.State = AccrualScheduleState.Done;
            row.ResultVoucherId = voucherId;
            row.ChangedBy = _currentUser.UserId;
            row.UpdatedAtUtc = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            return new(AccrualRunOutcome.Ran, voucherId, voucherNo);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    /// <summary>سند سهم یک ماه: بدهکار و بستانکار هر دو = مبلغ سطر (تراز ساختاری؛ <see cref="IVoucherWriter"/> دوباره کنترل می‌کند).</summary>
    public static VoucherDraft BuildShareDraft(AccrualSchedule row)
    {
        if (row.DebitAccountId is not Guid debit || row.CreditAccountId is not Guid credit)
            throw new BusinessRuleException("حساب سهم ماهانه در این سطر ثبت نشده است؛ سطر را لغو و سند را دستی ثبت کنید.");

        var sides = AccrualSchedulePlanner.ReadSides(row.TafsiliJson);
        var description = row.Description ?? "سهم ماهانه";
        return new VoucherDraft(
            row.VahedCode,
            AccrualDates.ToDate(row.DueDate),
            Truncate(description, 250),
            "ACCRUAL",
            [
                new VoucherDraftLine(debit, "", sides.Debit.Select(ToDetail).ToList(), row.Amount, 0, Truncate(description, 200)),
                new VoucherDraftLine(credit, "", sides.Credit.Select(ToDetail).ToList(), 0, row.Amount, Truncate(description, 200)),
            ]);
    }

    private static VoucherDraftDetail ToDetail(AccrualTafsili t) => new(t.Level, t.LevelId, t.DetailId, t.Title ?? "");

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
