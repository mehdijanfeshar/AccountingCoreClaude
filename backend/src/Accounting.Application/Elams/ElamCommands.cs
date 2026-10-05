using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Elams;

/// <summary>سرسند مشترک «سایر اعلامیهٔ صادره».</summary>
public interface IOtherElamInput
{
    string Year { get; }
    string Date { get; }
    string Description { get; }
    ElamCase Case { get; }
    string CounterVahedCode { get; }
    string? DabirNo { get; }
    string? DabirDate { get; }
    IReadOnlyList<ElamDetailInput> Details { get; }
}

/// <summary>ثبت سایر اعلامیهٔ صادره (سرسند + ردیف‌ها، یکجا). وضعیت ⇐ ۵ «تهیه».</summary>
public sealed record CreateOtherElamCommand(
    string Year,
    string Date,
    string Description,
    ElamCase Case,
    string CounterVahedCode,
    string? DabirNo,
    string? DabirDate,
    IReadOnlyList<ElamDetailInput> Details) : IRequest<Guid>, IVahedScopedCommand, IOtherElamInput
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ویرایش سایر اعلامیهٔ صادره — فقط پیش از صدور سند؛ ردیف‌ها کامل جایگزین می‌شوند.</summary>
public sealed record UpdateOtherElamCommand(
    Guid Id,
    string Year,
    string Date,
    string Description,
    ElamCase Case,
    string CounterVahedCode,
    string? DabirNo,
    string? DabirDate,
    IReadOnlyList<ElamDetailInput> Details) : IRequest<Unit>, IVahedScopedCommand, IOtherElamInput
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>سرسند مشترک «اعلامیهٔ صادرهٔ درآمد» (یک ردیف).</summary>
public interface IRevenueElamInput
{
    string Year { get; }
    string Date { get; }
    string Description { get; }
    DaramElamhType RevenueType { get; }
    string CounterVahedCode { get; }
    Guid AccountId { get; }
    decimal Amount { get; }
    string? DetailDescription { get; }
    Guid? WorkshopId { get; }
    string? WorkshopCode { get; }
    string? WorkshopName { get; }
    string? DebtNo { get; }
    string? DebtDate { get; }
    string? LastMonth { get; }
    string? ElamYear { get; }
    string? PeimanNo { get; }
    string? PayNo { get; }
}

/// <summary>ثبت اعلامیهٔ صادرهٔ درآمد. ماهیت همیشه «بستانکار» و ردیف بدهکار (عین مرجع). وضعیت ⇐ ۱.</summary>
public sealed record CreateRevenueElamCommand(
    string Year,
    string Date,
    string Description,
    DaramElamhType RevenueType,
    string CounterVahedCode,
    Guid AccountId,
    decimal Amount,
    string? DetailDescription,
    Guid? WorkshopId,
    string? WorkshopCode,
    string? WorkshopName,
    string? DebtNo,
    string? DebtDate,
    string? LastMonth,
    string? ElamYear,
    string? PeimanNo,
    string? PayNo) : IRequest<Guid>, IVahedScopedCommand, IRevenueElamInput
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record UpdateRevenueElamCommand(
    Guid Id,
    string Year,
    string Date,
    string Description,
    DaramElamhType RevenueType,
    string CounterVahedCode,
    Guid AccountId,
    decimal Amount,
    string? DetailDescription,
    Guid? WorkshopId,
    string? WorkshopCode,
    string? WorkshopName,
    string? DebtNo,
    string? DebtDate,
    string? LastMonth,
    string? ElamYear,
    string? PeimanNo,
    string? PayNo) : IRequest<Unit>, IVahedScopedCommand, IRevenueElamInput
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>حذف نرم اعلامیه با ردیف‌ها و تفصیلی‌ها — فقط پیش از صدور سند.</summary>
public sealed record DeleteElamCommand(Guid Id) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>صدور سند موقت اعلامیه. شمارهٔ سند برمی‌گردد.</summary>
public sealed record IssueElamVoucherCommand(Guid Id) : IRequest<ElamVoucherIssuedDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record ElamVoucherIssuedDto(Guid VoucherHeadId, string DocNum);

/// <summary>
/// تأیید اولیه (<paramref name="Final"/> = false) یا نهایی و ارسال. تأیید نهایی صادره، اعلامیهٔ رسیده و
/// سندش را خودکار در واحد مقصد می‌سازد. پاسخ = شناسهٔ اعلامیهٔ رسیده (فقط در تأیید نهایی صادره).
/// </summary>
public sealed record ConfirmElamCommand(Guid Id, bool Final) : IRequest<Guid?>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ElamCommandHandlers :
    IRequestHandler<CreateOtherElamCommand, Guid>,
    IRequestHandler<UpdateOtherElamCommand, Unit>,
    IRequestHandler<CreateRevenueElamCommand, Guid>,
    IRequestHandler<UpdateRevenueElamCommand, Unit>,
    IRequestHandler<DeleteElamCommand, Unit>,
    IRequestHandler<IssueElamVoucherCommand, ElamVoucherIssuedDto>,
    IRequestHandler<ConfirmElamCommand, Guid?>
{
    private readonly IElamWorkflowRepository _repository;
    private readonly ElamWorkflowService _workflow;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRevenueElamSender _revenueSender;

    public ElamCommandHandlers(
        IElamWorkflowRepository repository,
        ElamWorkflowService workflow,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRevenueElamSender revenueSender)
    {
        _revenueSender = revenueSender;
        _repository = repository;
        _workflow = workflow;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateOtherElamCommand request, CancellationToken cancellationToken)
    {
        await EnsureCounterUnitAsync(request.CounterVahedCode, request.VahedCode, cancellationToken);
        var rabet = await _workflow.GetRabetAsync(ElamKind.Sent, cancellationToken);
        var head = new TB_ELAMHEAD
        {
            ID = Guid.NewGuid(),
            ELAMH_SERIALNO = await _repository.GetNextSerialAsync(request.VahedCode, request.Year, rabet.AccCode, cancellationToken),
            ELAMH_CODE = rabet.AccCode,
            WEB_STAT = (byte)ElamWebStat.CreateOther,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };
        ApplyOther(head, request);
        await _repository.AddHeadAsync(head, cancellationToken);
        await _workflow.StageDetailsAsync(head, request.Details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return head.ID;
    }

    public async Task<Unit> Handle(UpdateOtherElamCommand request, CancellationToken cancellationToken)
    {
        var head = await LoadEditableAsync(request.Id, request.VahedCode, ElamKind.Sent, cancellationToken);
        await EnsureCounterUnitAsync(request.CounterVahedCode, request.VahedCode, cancellationToken);
        ApplyOther(head, request);
        Touch(head);
        _workflow.SoftDeleteDetails(head);
        await _workflow.StageDetailsAsync(head, request.Details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Guid> Handle(CreateRevenueElamCommand request, CancellationToken cancellationToken)
    {
        var rabet = await _workflow.GetRabetAsync(ElamKind.Revenue, cancellationToken);
        var head = new TB_ELAMHEAD
        {
            ID = Guid.NewGuid(),
            ELAMH_SERIALNO = await _repository.GetNextSerialAsync(request.VahedCode, request.Year, rabet.AccCode, cancellationToken),
            ELAMH_CODE = rabet.AccCode,
            WEB_STAT = (byte)ElamWebStat.CreateDramad,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };
        ApplyRevenue(head, request);
        await _repository.AddHeadAsync(head, cancellationToken);
        await _workflow.StageDetailsAsync(head, RevenueDetail(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return head.ID;
    }

    public async Task<Unit> Handle(UpdateRevenueElamCommand request, CancellationToken cancellationToken)
    {
        var head = await LoadEditableAsync(request.Id, request.VahedCode, ElamKind.Revenue, cancellationToken);
        ApplyRevenue(head, request);
        Touch(head);
        _workflow.SoftDeleteDetails(head);
        await _workflow.StageDetailsAsync(head, RevenueDetail(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteElamCommand request, CancellationToken cancellationToken)
    {
        var head = await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        if (!ElamWorkflowService.IsEditable(head))
            throw new ElamConflictException("اعلامیه‌ای که سند دارد یا ارسال شده یا رسیده است قابل حذف نیست.");
        _workflow.SoftDeleteDetails(head);
        head.ISDELETED = true;
        Touch(head);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<ElamVoucherIssuedDto> Handle(IssueElamVoucherCommand request, CancellationToken cancellationToken)
    {
        var head = await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        if (head.VOUCHERSHEAD_ID is not null)
            throw new ElamConflictException("برای این اعلامیه قبلاً سند صادر شده است.");

        var next = ElamWorkflowService.KindOf(head.WEB_STAT) switch
        {
            ElamKind.Sent when head.WEB_STAT == (byte)ElamWebStat.CreateOther => ElamWebStat.VoucherOther,
            ElamKind.Revenue when head.WEB_STAT == (byte)ElamWebStat.CreateDramad => ElamWebStat.VoucherDramad,
            ElamKind.Received => ElamWebStat.ConfirmRcvInfo,
            _ => throw new ElamConflictException("در این وضعیت صدور سند ممکن نیست."),
        };

        var details = ActiveDetails(head);
        var docNum = await _workflow.IssueVoucherAsync(head, details, cancellationToken);
        head.WEB_STAT = (byte)next;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ElamVoucherIssuedDto(head.VOUCHERSHEAD_ID!.Value, docNum);
    }

    public async Task<Guid?> Handle(ConfirmElamCommand request, CancellationToken cancellationToken)
    {
        var head = await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        if (head.VOUCHERSHEAD_ID is null)
            throw new ElamConflictException("ابتدا برای اعلامیهٔ انتخابی سند صادر نمایید.");

        var kind = ElamWorkflowService.KindOf(head.WEB_STAT);
        if (kind == ElamKind.Received)
            throw new ElamConflictException("اعلامیهٔ رسیده تأیید اولیه/نهایی ندارد.");

        var (voucherState, firstState, finalState) = kind == ElamKind.Revenue
            ? (ElamWebStat.VoucherDramad, ElamWebStat.FirstConfirmDramad, ElamWebStat.FinalConfirmDramad)
            : (ElamWebStat.VoucherOther, ElamWebStat.FirstConfirmOther, ElamWebStat.FinalConfirmOther);

        Guid? result = null;
        if (!request.Final)
        {
            if (head.WEB_STAT != (byte)voucherState)
                throw new ElamConflictException(head.WEB_STAT == (byte)finalState
                    ? "اعلامیه قبلاً ارسال شده است."
                    : "تأیید اولیه فقط پس از صدور سند و پیش از تأیید نهایی ممکن است.");
            head.WEB_STAT = (byte)firstState;
        }
        else
        {
            if (head.WEB_STAT == (byte)finalState)
                throw new ElamConflictException("اعلامیه قبلاً ارسال شده است.");
            if (head.WEB_STAT != (byte)firstState)
                throw new ElamConflictException("ابتدا تأیید اولیه را انجام دهید.");

            if (kind == ElamKind.Revenue)
            {
                RevenueSendRules.EnsureReadyToSend(head);
                var detail = ActiveDetails(head).FirstOrDefault()
                    ?? throw new ElamConflictException("اعلامیه ردیفی ندارد.");
                var letter = RevenueElamLetterBuilder.Build(head, detail);

                string response;
                try
                {
                    response = await _revenueSender.SendAsync(letter, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new ElamConflictException("ارتباط با سامانهٔ سبا (درآمد) برقرار نشد. دوباره تلاش کنید.");
                }

                // عین مرجع: «1» در ابتدای پاسخ = موفق؛ ۱۵ کاراکتر بعد شمارهٔ پیمان.
                if (string.IsNullOrEmpty(response) || response[0] != '1')
                    throw new ElamConflictException("ارسال به سامانهٔ سبا (درآمد) با خطا مواجه شد.");
                if (response.Length > 1)
                    head.PEIMAN_NO = response.Substring(1, Math.Min(15, response.Length - 1)) is var p && p.Length > 12 ? p[..12] : p;
                head.WEB_STAT = (byte)finalState;
            }
            else
            {
                var rcv = await _workflow.CreateReceivedAsync(head, ActiveDetails(head), cancellationToken);
                head.WEB_STAT = (byte)finalState;
                result = rcv.ID;
            }
        }

        Touch(head);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<TB_ELAMHEAD> LoadAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
        => await _repository.GetWithDetailsForUpdateAsync(id, vahedCode, cancellationToken)
            ?? throw new NotFoundException("Elam", id);

    private async Task<TB_ELAMHEAD> LoadEditableAsync(Guid id, string vahedCode, ElamKind kind, CancellationToken cancellationToken)
    {
        var head = await LoadAsync(id, vahedCode, cancellationToken);
        if (ElamWorkflowService.KindOf(head.WEB_STAT) != kind)
            throw new ElamConflictException("نوع اعلامیه با این فرم سازگار نیست.");
        if (!ElamWorkflowService.IsEditable(head))
            throw new ElamConflictException("اعلامیه‌ای که سند دارد یا ارسال شده است قابل ویرایش نیست.");
        return head;
    }

    private static IReadOnlyList<TB_ELAMDETAIL> ActiveDetails(TB_ELAMHEAD head)
        => head.TB_ELAMDETAILs.Where(d => d.ISDELETED != true).ToList();

    private async Task EnsureCounterUnitAsync(string counter, string own, CancellationToken cancellationToken)
    {
        if (counter == own)
            throw new ElamConflictException("واحد گیرنده نمی‌تواند همان واحد صادرکننده باشد.");
        if (await _repository.GetVahedNameAsync(counter, cancellationToken) is null)
            throw new ElamConflictException($"واحد {counter} یافت نشد.");
    }

    private void Touch(TB_ELAMHEAD head)
    {
        head.UPDATEDDATE = DateTime.UtcNow;
        head.CHANGEUSERID = _currentUser.UserId;
    }

    private static void ApplyOther(TB_ELAMHEAD head, IOtherElamInput input)
    {
        head.ELAMH_DATE = input.Date;
        head.ELAMH_DESC = input.Description.Trim();
        head.ELAMH_CASE = input.Case;
        head.ELAMH_SENDRCVVAHED = input.CounterVahedCode;
        head.ELAMH_DABIRNO = Blank(input.DabirNo);
        head.ELAMH_DABIRDATE = Blank(input.DabirDate);
    }

    private static void ApplyRevenue(TB_ELAMHEAD head, IRevenueElamInput input)
    {
        head.ELAMH_DATE = input.Date;
        head.ELAMH_DESC = input.Description.Trim();
        head.ELAMH_CASE = ElamCase.Creditor;
        head.ELAMHDRAMAD_TYPE = input.RevenueType;
        head.ELAMH_SENDRCVVAHED = input.CounterVahedCode;
        head.WORKSHOP_ID = input.WorkshopId;
        head.ELAMH_WORKSHOPCODE = Blank(input.WorkshopCode);
        head.ELAMH_WORKSHOPNAME = Blank(input.WorkshopName);
        head.ELAMH_RCVNO = Blank(input.DebtNo);
        head.ELAMH_RCVDT = Blank(input.DebtDate);
        head.ELAMH_LSTMON = Blank(input.LastMonth);
        head.ELAMH_YEAR = Blank(input.ElamYear);
        head.PEIMAN_NO = Blank(input.PeimanNo);
        head.PAY_NO = Blank(input.PayNo);
    }

    private static IReadOnlyList<ElamDetailInput> RevenueDetail(IRevenueElamInput input)
        => new[] { new ElamDetailInput(input.AccountId, input.Amount, input.DetailDescription, null, null) };

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>
/// اعتبارسنجی پیش از ارسال اعلامیهٔ درآمد — عین <c>SendElmDrmdCommandHandler.CheckData</c> مرجع.
/// </summary>
public static class RevenueSendRules
{
    public static void EnsureReadyToSend(TB_ELAMHEAD head)
    {
        if (head.ELAMH_WORKSHOPCODE?.Length != 10)
            throw new ElamConflictException("کد کارگاه باید ۱۰ کاراکتر باشد.");
        if (string.IsNullOrEmpty(head.ELAMH_RCVNO))
            throw new ElamConflictException("شمارهٔ بدهی باید تکمیل شود.");
        if (string.IsNullOrEmpty(head.ELAMH_WORKSHOPNAME))
            throw new ElamConflictException("نام کارگاه را وارد نمایید.");
        if ((head.YEAR + head.ELAMH_LSTMON).Length != 8)
            throw new ElamConflictException("ماه بدهی باید وارد شود.");
        if (head.VAHEDCODE?.Length != 4)
            throw new ElamConflictException("کد واحد اسناد پزشکی را وارد نمایید.");
        if (head.ELAMH_SENDRCVVAHED?.Length != 4)
            throw new ElamConflictException("کد واحد شعبهٔ مورد نظر را وارد نمایید.");
    }
}

internal static class ElamRules
{
    public const string Date = "^[0-9]{8}$";

    public static void Detail(InlineValidator<ElamDetailInput> v)
    {
        v.RuleFor(x => x.AccountId).NotEmpty().WithMessage("معین الزامی است.");
        v.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ باید بزرگ‌تر از صفر باشد.");
        v.RuleFor(x => x.Description).MaximumLength(800);
        v.RuleFor(x => x.AttribNo).MaximumLength(10).WithMessage("شناسه حداکثر ۱۰ کاراکتر است.");
    }

    public static void Head<T>(AbstractValidator<T> v) where T : IOtherElamInput
    {
        v.RuleFor(x => x.Year).Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم باشد.");
        v.RuleFor(x => x.Date).Matches(Date).WithMessage("تاریخ اعلامیه باید به شکل YYYYMMDD باشد.");
        v.RuleFor(x => x).Must(x => x.Date.StartsWith(x.Year, StringComparison.Ordinal))
            .WithMessage("تاریخ اعلامیه باید در سال مالی جاری باشد.").When(x => x.Date?.Length == 8);
        v.RuleFor(x => x.Description).NotEmpty().WithMessage("شرح اعلامیه الزامی است.").MaximumLength(300);
        v.RuleFor(x => x.Case).IsInEnum().WithMessage("ماهیت اعلامیه نامعتبر است.");
        v.RuleFor(x => x.CounterVahedCode).Length(4).WithMessage("واحد گیرنده الزامی است.");
        v.RuleFor(x => x.DabirNo).MaximumLength(10);
        v.RuleFor(x => x.DabirDate).Matches(Date).WithMessage("تاریخ دبیرخانه باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.DabirDate));
        v.RuleFor(x => x.Details).NotEmpty().WithMessage("اعلامیه دست‌کم یک ردیف لازم دارد.");
        var detail = new InlineValidator<ElamDetailInput>();
        Detail(detail);
        v.RuleForEach(x => x.Details).SetValidator(detail);
    }

    public static void Revenue<T>(AbstractValidator<T> v) where T : IRevenueElamInput
    {
        v.RuleFor(x => x.Year).Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم باشد.");
        v.RuleFor(x => x.Date).Matches(Date).WithMessage("تاریخ اعلامیه باید به شکل YYYYMMDD باشد.");
        v.RuleFor(x => x).Must(x => x.Date.StartsWith(x.Year, StringComparison.Ordinal))
            .WithMessage("تاریخ اعلامیه باید در سال مالی جاری باشد.").When(x => x.Date?.Length == 8);
        v.RuleFor(x => x.Description).NotEmpty().WithMessage("شرح اعلامیه الزامی است.").MaximumLength(300);
        v.RuleFor(x => x.RevenueType).IsInEnum().WithMessage("نوع درآمد نامعتبر است.");
        v.RuleFor(x => x.CounterVahedCode).Length(4).WithMessage("کد واحد شعبه الزامی است.");
        v.RuleFor(x => x.AccountId).NotEmpty().WithMessage("معین الزامی است.");
        v.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ باید بزرگ‌تر از صفر باشد.");
        v.RuleFor(x => x.DetailDescription).MaximumLength(800);
        v.RuleFor(x => x.WorkshopCode).MaximumLength(10);
        v.RuleFor(x => x.WorkshopName).MaximumLength(100);
        v.RuleFor(x => x.DebtNo).MaximumLength(14);
        v.RuleFor(x => x.DebtDate).Matches(Date).WithMessage("تاریخ بدهی باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.DebtDate));
        v.RuleFor(x => x.LastMonth).Matches("^(0[1-9]|1[0-2])$").WithMessage("ماه باید دو رقم ۰۱ تا ۱۲ باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.LastMonth));
        v.RuleFor(x => x.ElamYear).Matches("^[0-9]{2}$").WithMessage("سال اعلامیه باید دو رقم باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ElamYear));
        v.RuleFor(x => x.PeimanNo).MaximumLength(12);
        v.RuleFor(x => x.PayNo).MaximumLength(15);
    }
}

public sealed class CreateOtherElamCommandValidator : AbstractValidator<CreateOtherElamCommand>
{
    public CreateOtherElamCommandValidator() => ElamRules.Head(this);
}

public sealed class UpdateOtherElamCommandValidator : AbstractValidator<UpdateOtherElamCommand>
{
    public UpdateOtherElamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        ElamRules.Head(this);
    }
}

public sealed class CreateRevenueElamCommandValidator : AbstractValidator<CreateRevenueElamCommand>
{
    public CreateRevenueElamCommandValidator() => ElamRules.Revenue(this);
}

public sealed class UpdateRevenueElamCommandValidator : AbstractValidator<UpdateRevenueElamCommand>
{
    public UpdateRevenueElamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        ElamRules.Revenue(this);
    }
}

public sealed class DeleteElamCommandValidator : AbstractValidator<DeleteElamCommand>
{
    public DeleteElamCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class IssueElamVoucherCommandValidator : AbstractValidator<IssueElamVoucherCommand>
{
    public IssueElamVoucherCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class ConfirmElamCommandValidator : AbstractValidator<ConfirmElamCommand>
{
    public ConfirmElamCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
