using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ پیوست مدرک سندهای حسابیار (روی TB_ATTACH Legacy — بدون DDL) ═══════════════════════
//
// همان جدولی که برنامهٔ قدیم برای پیوست سند به کار می‌برد (VOUCHERSHEAD_ID، ردیف پیوسته در واحد/سال)، پس
// پیوست در هر دو برنامه دیده می‌شود. حذف نرم با ISDELETED (برنامهٔ قدیم حذف سخت می‌کرد).
// افزودن/حذف: فقط ثبت‌کنندهٔ سند و فقط تا سند «یادداشت» یا «موقت» است. دیدن/دریافت: ثبت‌کننده یا بررسی‌کننده.

public interface IVoucherAttachmentRepository
{
    /// <summary>پیوست‌های حذف‌نشدهٔ یک سند، بدون محتوای فایل، به ترتیب ردیف.</summary>
    Task<IReadOnlyList<VoucherAttachmentDto>> ListAsync(Guid voucherId, CancellationToken ct);

    /// <summary>یک پیوست حذف‌نشدهٔ همان سند، با محتوا (tracked — برای حذف نرم هم).</summary>
    Task<TB_ATTACH?> FindAsync(Guid voucherId, Guid attachmentId, CancellationToken ct);

    /// <summary>بیشترین ATTACH_RADIF در واحد/سال (همان شمارندهٔ برنامهٔ قدیم)، یا ۰.</summary>
    Task<int> GetMaxRadifAsync(string vahedCode, string year, CancellationToken ct);

    Task AddAsync(TB_ATTACH attachment, CancellationToken ct);

    /// <summary>سند و وضعیت فعلی‌اش (حذف‌شده = null).</summary>
    Task<(string? Year, DocLife? Life)?> GetVoucherAsync(Guid voucherId, CancellationToken ct);
}

public sealed record VoucherAttachmentDto(Guid Id, string FileName, string? Title, int Size, DateTime? CreatedAt, string? AddedBy);

public sealed record VoucherAttachmentFile(string FileName, byte[] Content);

public static class VoucherAttachmentAccessLimits
{
    public const int MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxFilesPerVoucher = 10;
}

internal static class VoucherAttachmentAccess
{
    public const int MaxFileBytes = VoucherAttachmentAccessLimits.MaxFileBytes;
    public const int MaxFilesPerVoucher = VoucherAttachmentAccessLimits.MaxFilesPerVoucher;

    /// <summary>اجرای حسابیارِ همین سند در همین واحد؛ وگرنه ۴۰۴ (سند دستی یا واحد دیگر).</summary>
    public static async Task<OperationExecution> ExecutionAsync(IHesabyarAuditRepository repo, Guid voucherId, string vahedCode, CancellationToken ct)
    {
        var execution = (await repo.ListExecutionsByVouchersAsync([voucherId], ct)).FirstOrDefault(e => e.VahedCode == vahedCode);
        return execution ?? throw new NotFoundException("OperationExecution", voucherId);
    }

    public static bool IsCreator(OperationExecution e, ICurrentUser user) =>
        string.Equals(e.CreatedBy, user.UserId, StringComparison.Ordinal);

    public static void EnsureCanRead(OperationExecution e, ICurrentUser user)
    {
        if (!IsCreator(e, user)) ReviewAccess.EnsureReviewer(user);
    }

    public static async Task<string> EnsureCanChangeAsync(IVoucherAttachmentRepository attachments, OperationExecution e, ICurrentUser user, CancellationToken ct)
    {
        if (!IsCreator(e, user))
            throw new RoleAccessDeniedException("فقط ثبت‌کنندهٔ سند می‌تواند مدرک پیوست یا حذف کند.");
        var voucher = await attachments.GetVoucherAsync(e.VoucherId, ct) ?? throw new NotFoundException("TB_VOUCHERSHEAD", e.VoucherId);
        if (voucher.Life is not (DocLife.Draft or DocLife.Temporary))
            throw new BusinessRuleException("سند بررسی و تأیید شده است؛ پیوست آن دیگر تغییر نمی‌کند.");
        return voucher.Year ?? string.Empty;
    }
}

// ─────────── فهرست ───────────

public sealed record ListVoucherAttachmentsQuery(Guid VoucherId) : IRequest<IReadOnlyList<VoucherAttachmentDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListVoucherAttachmentsHandler : IRequestHandler<ListVoucherAttachmentsQuery, IReadOnlyList<VoucherAttachmentDto>>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IVoucherAttachmentRepository _attachments;
    private readonly ICurrentUser _currentUser;

    public ListVoucherAttachmentsHandler(IHesabyarAuditRepository repo, IVoucherAttachmentRepository attachments, ICurrentUser currentUser)
    { _repo = repo; _attachments = attachments; _currentUser = currentUser; }

    public async Task<IReadOnlyList<VoucherAttachmentDto>> Handle(ListVoucherAttachmentsQuery q, CancellationToken ct)
    {
        var execution = await VoucherAttachmentAccess.ExecutionAsync(_repo, q.VoucherId, q.VahedCode, ct);
        VoucherAttachmentAccess.EnsureCanRead(execution, _currentUser);
        return await _attachments.ListAsync(q.VoucherId, ct);
    }
}

// ─────────── دریافت فایل ───────────

public sealed record GetVoucherAttachmentFileQuery(Guid VoucherId, Guid AttachmentId) : IRequest<VoucherAttachmentFile>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetVoucherAttachmentFileHandler : IRequestHandler<GetVoucherAttachmentFileQuery, VoucherAttachmentFile>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IVoucherAttachmentRepository _attachments;
    private readonly ICurrentUser _currentUser;

    public GetVoucherAttachmentFileHandler(IHesabyarAuditRepository repo, IVoucherAttachmentRepository attachments, ICurrentUser currentUser)
    { _repo = repo; _attachments = attachments; _currentUser = currentUser; }

    public async Task<VoucherAttachmentFile> Handle(GetVoucherAttachmentFileQuery q, CancellationToken ct)
    {
        var execution = await VoucherAttachmentAccess.ExecutionAsync(_repo, q.VoucherId, q.VahedCode, ct);
        VoucherAttachmentAccess.EnsureCanRead(execution, _currentUser);
        var a = await _attachments.FindAsync(q.VoucherId, q.AttachmentId, ct);
        if (a?.ATTACH_FILE is null) throw new NotFoundException("TB_ATTACH", q.AttachmentId);
        return new(a.ATTACH_NAME, a.ATTACH_FILE);
    }
}

// ─────────── افزودن ───────────

public sealed record UploadVoucherAttachmentCommand(Guid VoucherId, string FileName, string? Title, byte[] Content) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class UploadVoucherAttachmentValidator : AbstractValidator<UploadVoucherAttachmentCommand>
{
    public UploadVoucherAttachmentValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Title).MaximumLength(250);
        RuleFor(x => x.Content).NotEmpty().WithMessage("فایل خالی است.")
            .Must(c => c.Length <= VoucherAttachmentAccess.MaxFileBytes).WithMessage("حجم فایل بیشتر از ۱۰ مگابایت است.");
    }
}

public sealed class UploadVoucherAttachmentHandler : IRequestHandler<UploadVoucherAttachmentCommand, Guid>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IVoucherAttachmentRepository _attachments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UploadVoucherAttachmentHandler(IHesabyarAuditRepository repo, IVoucherAttachmentRepository attachments, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _attachments = attachments; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<Guid> Handle(UploadVoucherAttachmentCommand c, CancellationToken ct)
    {
        var execution = await VoucherAttachmentAccess.ExecutionAsync(_repo, c.VoucherId, c.VahedCode, ct);
        var year = await VoucherAttachmentAccess.EnsureCanChangeAsync(_attachments, execution, _currentUser, ct);
        if ((await _attachments.ListAsync(c.VoucherId, ct)).Count >= VoucherAttachmentAccess.MaxFilesPerVoucher)
            throw new BusinessRuleException($"هر سند حداکثر {VoucherAttachmentAccess.MaxFilesPerVoucher} پیوست دارد.");

        var attachment = new TB_ATTACH
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = c.VoucherId,
            ATTACH_NAME = c.FileName,
            TITLE = string.IsNullOrWhiteSpace(c.Title) ? null : c.Title.Trim(),
            ATTACH_SIZE = c.Content.Length,
            ATTACH_FILE = c.Content,
            ATTACH_RADIF = (short)Math.Min(short.MaxValue, await _attachments.GetMaxRadifAsync(c.VahedCode, year, ct) + 1),
            VAHEDCODE = c.VahedCode,
            YEAR = year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.Now,
            ISDELETED = false,
        };
        await _attachments.AddAsync(attachment, ct);
        if (AttachmentRiskSync.Apply(execution, attachmentRequired: true, hasAttachment: true))
            await _repo.UpdateExecutionRiskAsync(execution, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return attachment.ID;
    }
}

// ─────────── حذف (نرم) ───────────

public sealed record DeleteVoucherAttachmentCommand(Guid VoucherId, Guid AttachmentId) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class DeleteVoucherAttachmentHandler : IRequestHandler<DeleteVoucherAttachmentCommand, Unit>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IVoucherAttachmentRepository _attachments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IOperationTemplateRepository? _templates;

    public DeleteVoucherAttachmentHandler(IHesabyarAuditRepository repo, IVoucherAttachmentRepository attachments, IUnitOfWork unitOfWork, ICurrentUser currentUser,
        IOperationTemplateRepository? templates = null)
    { _repo = repo; _attachments = attachments; _unitOfWork = unitOfWork; _currentUser = currentUser; _templates = templates; }

    public async Task<Unit> Handle(DeleteVoucherAttachmentCommand c, CancellationToken ct)
    {
        var execution = await VoucherAttachmentAccess.ExecutionAsync(_repo, c.VoucherId, c.VahedCode, ct);
        await VoucherAttachmentAccess.EnsureCanChangeAsync(_attachments, execution, _currentUser, ct);
        var a = await _attachments.FindAsync(c.VoucherId, c.AttachmentId, ct) ?? throw new NotFoundException("TB_ATTACH", c.AttachmentId);
        a.ISDELETED = true;
        a.UPDATEDDATE = DateTime.Now;
        a.CHANGEUSERID = _currentUser.UserId;

        // آخرین مدرک حذف شد و الگو مدرک الزامی دارد ⇒ دلیل «پیوست نشده» به ریسک برمی‌گردد.
        if (_templates is not null && execution.OperationTemplateId is { } templateId
            && (await _attachments.ListAsync(c.VoucherId, ct)).All(x => x.Id == a.ID)
            && await _templates.GetFullAsync(templateId, ct) is { RequireAttachment: true }
            && AttachmentRiskSync.Apply(execution, attachmentRequired: true, hasAttachment: false))
            await _repo.UpdateExecutionRiskAsync(execution, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
