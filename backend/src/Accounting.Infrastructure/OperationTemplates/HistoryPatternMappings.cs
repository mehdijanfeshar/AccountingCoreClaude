using Accounting.Domain.OperationTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>الگوهای سابقه — DDL 085. جدول محاسبه‌شده؛ از لاگ تغییرات مستثنا است (AuditTableCatalog.ExcludedTables).</summary>
public sealed class HistoryPatternConfig : IEntityTypeConfiguration<HistoryPattern>
{
    public void Configure(EntityTypeBuilder<HistoryPattern> b)
    {
        b.ToTable("TB_OP_HISTORY_PATTERN");
        b.HasKey(x => x.Id).HasName("PK_OP_HISTORY_PATTERN");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.Signature).HasColumnName("SIGNATURE").HasMaxLength(1000).IsUnicode(false).IsRequired();
        b.HasIndex(x => x.Signature, "UK_OP_HISTORY_PATTERN_SIG").IsUnique();
        b.Property(x => x.LinesJson).HasColumnName("LINES_JSON").HasColumnType("CLOB");
        b.Property(x => x.VoucherCount).HasColumnName("VOUCHER_COUNT").HasColumnType("NUMBER(10)");
        b.Property(x => x.UnitCount).HasColumnName("UNIT_COUNT").HasColumnType("NUMBER(10)");
        b.Property(x => x.Years).HasColumnName("YEARS").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.FromYear).HasColumnName("FROM_YEAR").HasColumnType("NUMBER(4)");
        b.Property(x => x.ToYear).HasColumnName("TO_YEAR").HasColumnType("NUMBER(4)");
        b.Property(x => x.MedianAmount).HasColumnName("MEDIAN_AMOUNT").HasColumnType("NUMBER(18)");
        b.Property(x => x.LastDate).HasColumnName("LAST_DATE").HasMaxLength(8).IsUnicode(false);
        b.Property(x => x.KeywordsJson).HasColumnName("KEYWORDS").HasMaxLength(2000).IsUnicode(true);
        b.Property(x => x.SampleDescsJson).HasColumnName("SAMPLE_DESCS_JSON").HasMaxLength(2000).IsUnicode(true);
        b.Property(x => x.VahedTypes).HasColumnName("VAHED_TYPES").HasMaxLength(400).IsUnicode(false);
        b.Property(x => x.RebuiltAtUtc).HasColumnName("REBUILT_AT").HasPrecision(6);
        b.HasIndex(x => x.VoucherCount, "IX_OP_HISTORY_PATTERN_COUNT");
        b.HasIndex(x => new { x.FromYear, x.ToYear }, "IX_OP_HISTORY_PATTERN_YEARS");
    }
}

public sealed class HistoryPatternUnitConfig : IEntityTypeConfiguration<HistoryPatternUnit>
{
    public void Configure(EntityTypeBuilder<HistoryPatternUnit> b)
    {
        b.ToTable("TB_OP_HISTORY_PATTERN_UNIT");
        b.HasKey(x => new { x.PatternId, x.VahedCode }).HasName("PK_OP_HISTORY_PATTERN_UNIT");
        b.Property(x => x.PatternId).Char36("PATTERN_ID").ValueGeneratedNever();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false);
        b.Property(x => x.VoucherCount).HasColumnName("VOUCHER_COUNT").HasColumnType("NUMBER(10)");
        b.Property(x => x.KeywordsJson).HasColumnName("KEYWORDS").HasMaxLength(1000).IsUnicode(true);
        b.HasIndex(x => new { x.VahedCode, x.PatternId }, "IX_OP_HISTORY_UNIT_VAHED");
        b.HasOne<HistoryPattern>().WithMany().HasForeignKey(x => x.PatternId)
         .HasConstraintName("FK_OP_HISTORY_UNIT_PATTERN").OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>پیشنهادهای حذف‌شده — DDL 086 (در لاگ تغییرات می‌ماند).</summary>
public sealed class HistoryDismissedConfig : IEntityTypeConfiguration<HistoryDismissed>
{
    public void Configure(EntityTypeBuilder<HistoryDismissed> b)
    {
        b.ToTable("TB_OP_HISTORY_DISMISSED");
        b.HasKey(x => x.Signature).HasName("PK_OP_HISTORY_DISMISSED");
        b.Property(x => x.Signature).HasColumnName("SIGNATURE").HasMaxLength(1000).IsUnicode(false).ValueGeneratedNever();
        b.Property(x => x.AddUserId).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
    }
}

/// <summary>بردار معنایی الگو — DDL 089. بدون FK به الگو (شناسهٔ پایدار؛ بردار از بازسازی جان سالم به در می‌برد). از لاگ تغییرات مستثنا.</summary>
public sealed class HistoryPatternVectorConfig : IEntityTypeConfiguration<HistoryPatternVector>
{
    public void Configure(EntityTypeBuilder<HistoryPatternVector> b)
    {
        b.ToTable("TB_OP_HISTORY_PATTERN_VEC");
        b.HasKey(x => x.PatternId).HasName("PK_OP_HISTORY_PATTERN_VEC");
        b.Property(x => x.PatternId).Char36("PATTERN_ID").ValueGeneratedNever();
        b.Property(x => x.Model).HasColumnName("MODEL").HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.TextHash).HasColumnName("TEXT_HASH").HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.Dim).HasColumnName("DIM").HasColumnType("NUMBER(5)");
        b.Property(x => x.Vector).HasColumnName("VECTOR").HasColumnType("BLOB");
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
    }
}

/// <summary>معناهای الگو — DDL 089. بدون FK (مثل بردارها)؛ از لاگ تغییرات مستثنا.</summary>
public sealed class HistoryPatternMeaningConfig : IEntityTypeConfiguration<HistoryPatternMeaning>
{
    public void Configure(EntityTypeBuilder<HistoryPatternMeaning> b)
    {
        b.ToTable("TB_OP_HISTORY_MEANING");
        b.HasKey(x => new { x.PatternId, x.Index }).HasName("PK_OP_HISTORY_MEANING");
        b.Property(x => x.PatternId).Char36("PATTERN_ID").ValueGeneratedNever();
        b.Property(x => x.Index).HasColumnName("IDX").HasColumnType("NUMBER(3)").ValueGeneratedNever();
        b.Property(x => x.Title).HasColumnName("TITLE").HasMaxLength(200).IsUnicode(true).IsRequired();
        b.Property(x => x.Share).HasColumnName("SHARE").HasColumnType("NUMBER(5,1)");
        b.Property(x => x.SampleCount).HasColumnName("SAMPLE_COUNT").HasColumnType("NUMBER(10)");
        b.Property(x => x.SamplesJson).HasColumnName("SAMPLES_JSON").HasMaxLength(1000).IsUnicode(true);
        b.Property(x => x.KeywordsJson).HasColumnName("KEYWORDS").HasMaxLength(1000).IsUnicode(true);
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
    }
}
