namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// گارد وابستگی حذف نرم (ریسک‌های #۶، #۷، ۲۰۲۶-۱۰-۰۷). چون حذف نرم است، Oracle با
/// <c>ORA-02292</c> جلوی حذف والدِ دارای فرزند را نمی‌گیرد؛ این بررسی جای آن را می‌گیرد.
/// هر متد اولین مانع را به‌صورت متن فارسی برمی‌گرداند، یا <c>null</c> اگر حذف آزاد است.
/// فقط ردیف‌های فعال (<c>ISDELETED</c> نه‌درست) شمرده می‌شوند.
/// </summary>
public interface IDeleteDependencyChecker
{
    Task<string?> FindAccountCodeBlockerAsync(Guid accountCodeId, CancellationToken cancellationToken = default);

    Task<string?> FindLevelTafsilBlockerAsync(Guid levelId, CancellationToken cancellationToken = default);

    Task<string?> FindTafsilGroupBlockerAsync(Guid tafsilGroupId, CancellationToken cancellationToken = default);

    /// <summary>گارد حذف بقیهٔ Entityها (ریسک ۲-الف، فاز ۵۲). null = قابل حذف؛ وگرنه دلیل فارسی.</summary>
    Task<string?> FindBlockerAsync(DeleteGuardTarget target, Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Entityهایی که <see cref="IDeleteDependencyChecker.FindBlockerAsync"/> برایشان قاعده دارد.</summary>
public enum DeleteGuardTarget
{
    AttribForAccountCode,
    ChequeType,
    Expense,
    IdentityGroup,
    IdentitySubGroup,
    Rabet,
    RevolvingFund,
    ElamHead,
    PayReciveHead,
    Tafsili,
}
