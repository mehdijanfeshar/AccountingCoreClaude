using Accounting.Domain.Accrual;
using Accounting.Infrastructure.OperationTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Accrual;

// حسابداری تعهدی — نگاشت backend/db/090_accrual_schedule.sql. قرارداد پروژه: CHAR(36) ⇄ Guid، bool روی NUMBER(1)؛
// enum بدون نوع ستون صریح (NUMBER(1) صریح اوراکل را به خواندن bool می‌کشاند).

public sealed class AccrualScheduleConfig : IEntityTypeConfiguration<AccrualSchedule>
{
    public void Configure(EntityTypeBuilder<AccrualSchedule> b)
    {
        b.ToTable("TB_AC_SCHEDULE");
        b.HasKey(x => x.Id).HasName("PK_AC_SCHEDULE");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.Year).HasColumnName("YEAR").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.SourceVoucherId).Char36("SOURCE_VOUCHER_ID");
        b.Property(x => x.ExecutionId).Char36("EXECUTION_ID");
        b.Property(x => x.Kind).HasColumnName("KIND").HasConversion<int>();
        b.Property(x => x.LineNo).HasColumnName("LINE_NO");
        b.Property(x => x.DueDate).HasColumnName("DUE_DATE").HasMaxLength(8).IsUnicode(false).IsRequired();
        b.Property(x => x.Amount).HasColumnName("AMOUNT").HasColumnType("NUMBER(18)");
        b.Property(x => x.DebitAccountId).Char36("DEBIT_ACCOUNT_ID");
        b.Property(x => x.CreditAccountId).Char36("CREDIT_ACCOUNT_ID");
        b.Property(x => x.TafsiliJson).HasColumnName("TAFSILI_JSON");
        b.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(500).IsUnicode(false);
        b.Property(x => x.State).HasColumnName("STATE").HasConversion<int>();
        b.Property(x => x.ResultVoucherId).Char36("RESULT_VOUCHER_ID");
        b.Property(x => x.IsDeleted).HasColumnName("ISDELETED").HasColumnType("NUMBER(1)");
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.Property(x => x.ChangedBy).HasColumnName("CHANGEUSERID").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.UpdatedAtUtc).HasColumnName("UPDATEDDATE").HasPrecision(6);
        b.HasIndex(x => new { x.SourceVoucherId, x.Kind, x.LineNo }, "UK_AC_SCHEDULE_LINE").IsUnique();
        b.HasIndex(x => new { x.VahedCode, x.State, x.DueDate }, "IX_AC_SCHEDULE_DUE");
    }
}
