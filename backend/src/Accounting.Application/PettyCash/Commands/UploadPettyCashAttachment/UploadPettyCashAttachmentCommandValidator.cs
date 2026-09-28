using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.UploadPettyCashAttachment;

public sealed class UploadPettyCashAttachmentCommandValidator : AbstractValidator<UploadPettyCashAttachmentCommand>
{
    /// <summary>
    /// The project's attachment size cap (<c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲،
    /// پیوست: "سقف اندازه در FluentValidation (پیشنهاد ۱۰MB)، نه در DB"). Named constant, not a
    /// magic number, since <see cref="Accounting.Application.Common.Exceptions.PettyCashAttachmentTooLargeException"/>
    /// and the handler both need the same value.
    /// </summary>
    public const int MaxAttachmentSizeBytes = 10 * 1024 * 1024;

    public UploadPettyCashAttachmentCommandValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();

        RuleFor(x => x.AttachName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType).MaximumLength(100);

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => content.Length <= MaxAttachmentSizeBytes)
            .WithMessage("حجم فایل پیوست بیشتر از حد مجاز (۱۰ مگابایت) است.");
    }
}
