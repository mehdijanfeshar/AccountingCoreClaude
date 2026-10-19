namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// تفکیک وظایف حسابیار (G1): ثبت‌کنندهٔ یک سند حسابیار (کسی که ردیف <c>TB_OP_EXECUTION</c> را ساخته) نمی‌تواند
/// همان سند را به «بررسی‌شده» ببرد. فقط برای سندهایی که ردیف اجرا دارند؛ بقیهٔ سندها بدون تغییر. ۴۰۳.
/// </summary>
public sealed class HesabyarSelfReviewDeniedException : Exception, IPublicProblemException
{
    public HesabyarSelfReviewDeniedException()
        : base("Caller created this Hesabyar voucher and cannot move it to reviewed (segregation of duties).")
    {
    }

    public int StatusCode => 403;

    public string PublicDetail =>
        "این سند را خودتان با حسابیار ثبت کرده‌اید و نمی‌توانید آن را «بررسی‌شده» کنید؛ بررسی باید توسط شخص دیگری انجام شود.";
}
