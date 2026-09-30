using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for the financial-statement templates — فاز ۴۵-الف
/// (<c>docs/fs-module.md</c>). Only stages changes; the handler owns SaveChanges via
/// <see cref="IUnitOfWork"/>. Rows are hard-deleted (embedded children of a Draft version).
/// </summary>
public interface IFsTemplateRepository
{
    Task AddTemplateAsync(TB_FS_TEMPLATE template, CancellationToken cancellationToken = default);

    Task AddVersionAsync(TB_FS_TEMPLATE_VERSION version, CancellationToken cancellationToken = default);

    Task AddRowsAsync(IEnumerable<TB_FS_TEMPLATE_ROW> rows, CancellationToken cancellationToken = default);

    void RemoveRows(IEnumerable<TB_FS_TEMPLATE_ROW> rows);

    /// <summary>Non-deleted template, change-tracked, or <see langword="null"/>.</summary>
    Task<TB_FS_TEMPLATE?> GetTemplateForUpdateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a template of the same owner (<paramref name="ownerVahedCode"/>; <see langword="null"/> =
    /// shared) — deleted or not, codes are never reused — already has <paramref name="code"/>.
    /// </summary>
    Task<bool> TemplateCodeExistsAsync(string? ownerVahedCode, string code, CancellationToken cancellationToken = default);

    /// <summary>Non-deleted version with its <c>TEMPLATE</c> loaded, change-tracked, or <see langword="null"/>.</summary>
    Task<TB_FS_TEMPLATE_VERSION?> GetVersionForUpdateAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>All non-deleted versions of a template, change-tracked.</summary>
    Task<IReadOnlyList<TB_FS_TEMPLATE_VERSION>> GetVersionsForUpdateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>Highest <c>VERSION_NO</c> ever used by the template (deleted versions included), 0 if none.</summary>
    Task<int> GetMaxVersionNoAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>All rows of a version, change-tracked, ordered by <c>ORDER_NO</c>.</summary>
    Task<IReadOnlyList<TB_FS_TEMPLATE_ROW>> GetRowsForUpdateAsync(Guid versionId, CancellationToken cancellationToken = default);
}
