using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_DOC_EVENT"/> ("گردش عملیات") — insert-only, per
/// that entity's XML doc. Only stages the row — never calls SaveChanges; every write handler
/// stages its event alongside the document/head/detail rows it also touches, so all of it is
/// persisted by the SAME <see cref="IUnitOfWork.SaveChangesAsync"/> call
/// (<c>docs/tankhah-khazaneh-module.md</c> §4: "هر تغییر وضعیت یک ردیف TB_PC_DOC_EVENT در همان
/// تراکنش").
/// </summary>
public interface IPettyCashDocEventRepository
{
    Task AddAsync(TB_PC_DOC_EVENT docEvent, CancellationToken cancellationToken = default);
}
