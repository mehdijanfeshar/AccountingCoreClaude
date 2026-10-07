using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.Commands.ChangeVoucherState;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;
using Accounting.Application.Vouchers.Commands.UpdateVoucherHead;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.SaveVoucher;

/// <summary>
/// ذخیرهٔ کامل سند از فرم — سرسند + همهٔ ردیف‌ها (با تفصیلی، چک و شناسه/فیش) + ردیف‌های حذف‌شده،
/// <b>در یک تراکنش</b> (رفع ریسک #۲۱، فاز ۵۲). یا همه ثبت می‌شود یا هیچ؛ دیگر سرسند بی‌ردیف یا سند
/// نیمه‌کاره در دیتابیس نمی‌ماند.
///
/// خودش منطق تازه‌ای ندارد: همان Commandهای تکی (ساخت/ویرایش سرسند، ساخت/ویرایش/حذف ردیف، تغییر
/// وضعیت) را با <see cref="ISender"/> پشت سر هم می‌فرستد، پس هر Validator، گارد تفصیلی الزامی، قفل
/// وضعیت و Scope واحد همان‌طور اعمال می‌شود. همه روی همان DbContext (Scoped) اجرا می‌شوند، پس
/// <c>SaveChanges</c> هر کدام داخل تراکنش بیرونی است و خطای هر مرحله همه را برمی‌گرداند.
///
/// ترتیب: ردیف‌های حذفی ← ردیف‌ها ← سرسند/وضعیت. وضعیت آخر از همه عوض می‌شود تا کنترل تراز روی
/// ردیف‌های نهایی انجام شود؛ و اگر سند پس از ذخیره در وضعیتی غیر از یادداشت باشد، تراز دوباره
/// کنترل می‌شود (ویرایش یک سند موقت نباید ناترازش کند).
/// </summary>
/// <param name="HeadId">null = سند جدید؛ وگرنه ویرایش همین سند.</param>
public sealed record SaveVoucherCommand(
    Guid? HeadId,
    SaveVoucherHeadInput Head,
    IReadOnlyList<SaveVoucherLineInput> Lines,
    IReadOnlyList<Guid>? DeletedLineIds = null,
    VoucherConcurrencyToken? Concurrency = null) : IRequest<SaveVoucherResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>همان فیلدهای <see cref="CreateVoucherHeadCommand"/> بدون <c>InitialDetails</c>.</summary>
public sealed record SaveVoucherHeadInput(
    string DocNum,
    string DateDoc,
    DocLife? DocLife,
    string? HeadDesc,
    string? Apendix,
    Guid? SystemTypeId,
    decimal? FlagState,
    string Year,
    bool? IsAutomatic,
    string? SndVahedCode,
    Guid? ParentHeadId,
    string? AttachFileName,
    string? AtfNum);

/// <summary>یک ردیف؛ <paramref name="Id"/> = null یعنی ردیف تازه.</summary>
public sealed record SaveVoucherLineInput(
    Guid? Id,
    Guid? AccountId,
    Guid? ReceiptId,
    Guid? CheckId,
    Guid? LowLevelCodeId,
    Guid? EtebarId,
    string? Description,
    int? Radif,
    decimal? Debtor,
    decimal? Creditor,
    IReadOnlyList<VoucherDetailTafsiliLinkInput>? TafsiliLinks = null,
    VoucherChequeInfoInput? Cheque = null,
    VoucherLineExtrasInput? Extras = null);

public sealed record SaveVoucherResult(Guid HeadId);

/// <summary>
/// کنترل ویرایش هم‌زمان (optimistic): <c>UPDATEDDATE</c> سرسند همان‌طور که فرم بارش کرده. هر ذخیرهٔ
/// سند از این مسیر سرسند را هم به‌روز می‌کند، پس هر تغییر دیگری در فاصله این مقدار را عوض کرده است.
/// Legacy ستون rowversion ندارد؛ مقایسه تا دقت ثانیه است.
/// </summary>
public sealed record VoucherConcurrencyToken(DateTime? UpdatedDate);

public sealed class SaveVoucherCommandValidator : AbstractValidator<SaveVoucherCommand>
{
    public SaveVoucherCommandValidator()
    {
        RuleFor(x => x.Head).NotNull();
        RuleFor(x => x.Lines).NotNull().Must(l => l is { Count: > 0 }).WithMessage("سند دست‌کم یک ردیف لازم دارد.");
        RuleFor(x => x.Lines).Must(l => l is null || l.Count <= 1000).WithMessage("تعداد ردیف‌های سند بیش از حد مجاز است.");
        RuleFor(x => x.Lines)
            .Must(l => l is null || l.Where(x => x.Id is not null).GroupBy(x => x.Id).All(g => g.Count() == 1))
            .WithMessage("یک ردیف دو بار در سند آمده است.");
        RuleFor(x => x.DeletedLineIds)
            .Must(d => d is null || d.Count == 0)
            .When(x => x.HeadId is null)
            .WithMessage("سند جدید ردیف حذف‌شده ندارد.");
    }
}

public sealed class SaveVoucherCommandHandler : IRequestHandler<SaveVoucherCommand, SaveVoucherResult>
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVoucherHeadRepository _headRepository;
    private readonly IVoucherDetailRepository _detailRepository;

    public SaveVoucherCommandHandler(
        ISender sender,
        IUnitOfWork unitOfWork,
        IVoucherHeadRepository headRepository,
        IVoucherDetailRepository detailRepository)
    {
        _sender = sender;
        _unitOfWork = unitOfWork;
        _headRepository = headRepository;
        _detailRepository = detailRepository;
    }

    public async Task<SaveVoucherResult> Handle(SaveVoucherCommand request, CancellationToken ct)
    {
        var h = request.Head;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            Guid headId;
            if (request.HeadId is { } existingId)
            {
                headId = existingId;

                if (request.Concurrency is { } token)
                {
                    var current = await _headRepository.GetForUpdateAsync(headId, request.VahedCode, ct)
                        ?? throw new NotFoundException(nameof(Domain.Entity.TB_VOUCHERSHEAD), headId);
                    if (ToSecond(current.UPDATEDDATE) != ToSecond(token.UpdatedDate))
                    {
                        throw new BusinessRuleException(
                            "این سند پس از باز شدن فرم توسط کاربر دیگری تغییر کرده است. فرم را دوباره باز کنید تا آخرین نسخه را ببینید؛ تغییرات شما ذخیره نشد.");
                    }
                }

                foreach (var deletedId in (request.DeletedLineIds ?? []).Distinct())
                {
                    await _sender.Send(new DeleteVoucherDetailCommand(deletedId), ct);
                }

                await SendLinesAsync(headId, request, ct);

                // ویرایش سرسند آخر از همه، تا اگر وضعیت عوض شد تراز روی ردیف‌های نهایی سنجیده شود.
                await _sender.Send(new UpdateVoucherHeadCommand(
                    headId, h.DocNum, h.DateDoc, h.DocLife, h.HeadDesc, h.Apendix, h.SystemTypeId, h.FlagState,
                    h.Year, h.IsAutomatic, h.SndVahedCode, h.ParentHeadId, h.AttachFileName, h.AtfNum), ct);
            }
            else
            {
                // سرسند اول یادداشت ساخته می‌شود (ردیف‌ها هنوز نیستند)؛ وضعیت خواسته‌شده بعد از ردیف‌ها.
                headId = await _sender.Send(new CreateVoucherHeadCommand(
                    h.DocNum, h.DateDoc, DocLife.Draft, h.HeadDesc, h.Apendix, h.SystemTypeId, h.FlagState,
                    h.Year, h.IsAutomatic, h.SndVahedCode, h.ParentHeadId, h.AttachFileName, h.AtfNum), ct);

                await SendLinesAsync(headId, request, ct);

                if (h.DocLife is { } target && target != DocLife.Draft)
                {
                    await _sender.Send(new ChangeVoucherStateCommand([headId], target), ct);
                }
            }

            // سند غیر یادداشت پس از ذخیره باید تراز باشد — از جمله ویرایش ردیف‌های یک سند موقت.
            var head = await _headRepository.GetForUpdateAsync(headId, request.VahedCode, ct)
                ?? throw new NotFoundException(nameof(Domain.Entity.TB_VOUCHERSHEAD), headId);
            if (VoucherBalanceGuard.RequiresBalance(head.DOCLIFE))
            {
                await VoucherBalanceGuard.EnsureBalancedAsync(_detailRepository, [head], ct);
            }

            await _unitOfWork.CommitTransactionAsync(ct);
            return new SaveVoucherResult(headId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private static long? ToSecond(DateTime? value) => value is null ? null : value.Value.Ticks / TimeSpan.TicksPerSecond;

    private async Task SendLinesAsync(Guid headId, SaveVoucherCommand request, CancellationToken ct)
    {
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var l = request.Lines[i];
            try
            {
                if (l.Id is { } lineId)
                {
                    await _sender.Send(new UpdateVoucherDetailCommand(
                        lineId, l.AccountId, l.ReceiptId, l.CheckId, l.LowLevelCodeId, l.EtebarId, l.Description,
                        i + 1, l.Debtor, l.Creditor, request.Head.Year, l.TafsiliLinks, l.Cheque, l.Extras), ct);
                }
                else
                {
                    await _sender.Send(new CreateVoucherDetailCommand(
                        headId, l.AccountId, l.ReceiptId, l.CheckId, l.LowLevelCodeId, l.EtebarId, l.Description,
                        i + 1, l.Debtor, l.Creditor, request.Head.Year, l.TafsiliLinks, l.Cheque, l.Extras), ct);
                }
            }
            catch (ValidationException ex)
            {
                // خطای اعتبارسنجی ردیف با شمارهٔ ردیف برمی‌گردد (Lines[2].Debtor) تا فرم همان ردیف را باز کند.
                throw new ValidationException(ex.Errors.Select(e =>
                    new ValidationFailure($"Lines[{i}].{e.PropertyName}", $"ردیف {i + 1}: {e.ErrorMessage}")));
            }
        }
    }
}
