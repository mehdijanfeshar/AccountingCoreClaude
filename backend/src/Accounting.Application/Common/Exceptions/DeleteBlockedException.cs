namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// حذف رکوردی که هنوز جای دیگری استفاده می‌شود (ریسک‌های #۶، #۷). 409: کاربر اول باید
/// وابستگی را بردارد. متن عمومی فقط نام کسب‌وکاری وابستگی را دارد، نه نام جدول.
/// </summary>
public sealed class DeleteBlockedException : Exception, IPublicProblemException
{
    public DeleteBlockedException(string entityName, Guid id, string publicDetail)
        : base($"{entityName} {id} cannot be deleted: {publicDetail}")
    {
        PublicDetail = publicDetail;
    }

    public int StatusCode => 409;

    public string PublicDetail { get; }
}
