using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// وضعیت دورهٔ یک واحد در یک سال مالی برای صورت‌های مالی (ح-۵). نبودِ ردیف = «باز». قفلِ یک واحد کل
/// زیرمجموعه‌اش را هم قفل‌شده حساب می‌کند. بازگشایی دورهٔ قفل فقط با درخواست و دلیل و تأیید ستاد. DDL 061.
/// </summary>
public partial class TB_FS_PERIOD
{
    public Guid ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public FsPeriodState STATE { get; set; }

    public string? REOPEN_REASON { get; set; }

    public string? REOPEN_REQUESTED_BY { get; set; }

    public DateTime? REOPEN_REQUESTED_DATE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }
}
