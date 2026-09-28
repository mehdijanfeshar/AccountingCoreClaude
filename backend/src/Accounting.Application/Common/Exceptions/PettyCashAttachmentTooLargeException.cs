namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>UploadPettyCashAttachmentCommandHandler</c> when the uploaded content exceeds the
/// project's 10 MiB attachment cap
/// (<see cref="Accounting.Application.PettyCash.Commands.UploadPettyCashAttachment.UploadPettyCashAttachmentCommandValidator.MaxAttachmentSizeBytes"/>,
/// <c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲، پیوست). The handler re-checks
/// <c>Content.Length</c> itself rather than relying solely on
/// <c>UploadPettyCashAttachmentCommandValidator</c>'s identical rule — belt-and-suspenders, so the
/// limit still holds even if a future caller constructs/handles the command outside the normal
/// MediatR + <c>ValidationBehavior</c> pipeline (e.g. a test or another handler).
///
/// <b>400, not 413</b> — same reasoning as the other §4/بخش-۲ "this request's own data does not
/// fit a rule" exceptions (e.g. <c>PettyCashPerDocLimitExceededException</c>): this project maps
/// every such case to a <c>ProblemDetails</c> 400 rather than distinguishing HTTP's
/// payload-too-large status, to keep the client-side error handling uniform.
/// </summary>
public sealed class PettyCashAttachmentTooLargeException : Exception
{
    public PettyCashAttachmentTooLargeException(int actualSizeBytes, int maxSizeBytes)
        : base($"Attachment size {actualSizeBytes} bytes exceeds the {maxSizeBytes}-byte cap.")
    {
        ActualSizeBytes = actualSizeBytes;
        MaxSizeBytes = maxSizeBytes;
    }

    public int ActualSizeBytes { get; }

    public int MaxSizeBytes { get; }

    public string PublicDetail => "حجم فایل پیوست بیشتر از حد مجاز (۱۰ مگابایت) است.";
}
