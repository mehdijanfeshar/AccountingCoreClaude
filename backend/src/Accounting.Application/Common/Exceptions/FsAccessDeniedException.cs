namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// تغییر قالب صورت مالی از واحدی که مالکش نیست (یا قالب مشترک از غیرستاد) — ۴۰۳ با پیام فارسی.
/// فاز ۴۵ (<c>docs/fs-module.md</c> §۸).
/// </summary>
public sealed class FsAccessDeniedException : Exception
{
    public FsAccessDeniedException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
