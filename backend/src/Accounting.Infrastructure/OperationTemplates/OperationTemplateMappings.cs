using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.OperationTemplates;

// Agent-UX فاز ۱ — نگاشت جدول‌های DDL 067 (backend/db/067_operation_templates.sql).
// همان قرارداد پروژه: نام جدول/ستون بزرگ، CHAR(36) ⇄ Guid، bool و enum روی NUMBER(1).

internal static class OperationTemplateMappingExtensions
{
    public static PropertyBuilder<Guid> Char36(this PropertyBuilder<Guid> p, string column) =>
        p.HasColumnName(column).HasMaxLength(36).IsUnicode(false).IsFixedLength()
         .HasConversion(GuidToChar36Converter.Instance);

    public static PropertyBuilder<Guid?> Char36(this PropertyBuilder<Guid?> p, string column) =>
        p.HasColumnName(column).HasMaxLength(36).IsUnicode(false).IsFixedLength()
         .HasConversion(GuidToChar36Converter.Instance);
}

public sealed class OperationTemplateConfig : IEntityTypeConfiguration<OperationTemplate>
{
    public void Configure(EntityTypeBuilder<OperationTemplate> b)
    {
        b.ToTable("TB_OP_TEMPLATE");
        b.HasKey(x => x.Id).HasName("PK_OP_TEMPLATE");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.Code).HasColumnName("CODE").HasMaxLength(50).IsUnicode(false).IsRequired();
        b.HasIndex(x => x.Code, "UK_OP_TEMPLATE_CODE").IsUnique();
        b.Property(x => x.Title).HasColumnName("TITLE").HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(2000).IsUnicode(false).IsRequired();
        b.Property(x => x.VoucherDescriptionPattern).HasColumnName("VOUCHER_DESC_PATTERN").HasMaxLength(500).IsUnicode(false).IsRequired();
        b.Property(x => x.IsActive).HasColumnName("ISACTIVE").HasColumnType("NUMBER(1)");
        b.Property(x => x.SystemTypeId).Char36("SYSTEM_TYPE_ID"); // DDL 068
        b.Property(x => x.Keywords).HasColumnName("KEYWORDS").HasMaxLength(1000).IsUnicode(false); // DDL 070
        b.Property(x => x.AllowedVahedTypes).HasColumnName("ALLOWED_VAHED_TYPES").HasMaxLength(400).IsUnicode(false); // DDL 071
        // DDL 084 (G7): سیاست الگو. enumها nullable با HasConversion<int?> (NUMBER(1) چندمقداری).
        b.Property(x => x.PlainPattern).HasColumnName("PLAIN_PATTERN").HasMaxLength(1000).IsUnicode(true);
        b.Property(x => x.MaxAmount).HasColumnName("MAX_AMOUNT").HasColumnType("NUMBER(18)");
        b.Property(x => x.RequireAttachment).HasColumnName("REQUIRE_ATTACHMENT").HasColumnType("NUMBER(1)");
        b.Property(x => x.RiskBase).HasColumnName("RISK_BASE").HasConversion<int?>();
        b.Property(x => x.Category).HasColumnName("CATEGORY").HasConversion<int?>();
        b.HasOne<TB_SYSTYPE>().WithMany().HasForeignKey(x => x.SystemTypeId)
         .HasConstraintName("FK_OP_TEMPLATE_SYSTYPE").OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasMany(x => x.Parameters).WithOne().HasForeignKey(p => p.OperationTemplateId)
         .HasConstraintName("FK_OP_PARAM_TEMPLATE").OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OperationTemplateId)
         .HasConstraintName("FK_OP_LINE_TEMPLATE").OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TemplateParameterConfig : IEntityTypeConfiguration<TemplateParameter>
{
    public void Configure(EntityTypeBuilder<TemplateParameter> b)
    {
        b.ToTable("TB_OP_TEMPLATE_PARAM");
        b.HasKey(x => x.Id).HasName("PK_OP_TEMPLATE_PARAM");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.OperationTemplateId).Char36("TEMPLATE_ID");
        b.Property(x => x.Key).HasColumnName("PARAM_KEY").HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.Title).HasColumnName("TITLE").HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(x => x.Type).HasColumnName("PARAM_TYPE").HasConversion<int>(); // enum: NUMBER(1) فقط در DDL — نوع ستون صریح، اوراکل را به خواندن bool (۲ و ۳ ⇒ ۱) می‌کشاند
        b.Property(x => x.DetailGroupId).Char36("TAFSILGROUP_ID");
        b.Property(x => x.IsRequired).HasColumnName("ISREQUIRED").HasColumnType("NUMBER(1)");
        b.Property(x => x.AskPrompt).HasColumnName("ASK_PROMPT").HasMaxLength(500).IsUnicode(false).IsRequired();
        b.Property(x => x.SortOrder).HasColumnName("SORT_ORDER");
        b.Property(x => x.OptionsJson).HasColumnName("OPTIONS_JSON").HasMaxLength(2000).IsUnicode(true); // DDL 084 (G8)
        b.HasIndex(x => new { x.OperationTemplateId, x.Key }, "UK_OP_PARAM_KEY").IsUnique();
        b.HasOne<TB_TAFSIL_GROUP>().WithMany().HasForeignKey(x => x.DetailGroupId)
         .HasConstraintName("FK_OP_PARAM_TAFSILGROUP").OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateLineConfig : IEntityTypeConfiguration<TemplateLine>
{
    public void Configure(EntityTypeBuilder<TemplateLine> b)
    {
        b.ToTable("TB_OP_TEMPLATE_LINE");
        b.HasKey(x => x.Id).HasName("PK_OP_TEMPLATE_LINE");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.OperationTemplateId).Char36("TEMPLATE_ID");
        b.Property(x => x.Side).HasColumnName("SIDE").HasConversion<int>(); // enum: NUMBER(1) فقط در DDL — نوع ستون صریح، اوراکل را به خواندن bool (۲ و ۳ ⇒ ۱) می‌کشاند
        b.Property(x => x.SubsidiaryAccountId).Char36("ACCOUNT_ID");
        b.Property(x => x.AmountParameterKey).HasColumnName("AMOUNT_PARAM_KEY").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Percent).HasColumnName("PERCENT_VALUE").HasPrecision(9, 4);
        b.Property(x => x.IsBalancingLine).HasColumnName("ISBALANCING").HasColumnType("NUMBER(1)");
        b.Property(x => x.DescriptionPattern).HasColumnName("DESC_PATTERN").HasMaxLength(500).IsUnicode(false);
        b.Property(x => x.SortOrder).HasColumnName("SORT_ORDER");
        // DDL 084 (G8/G9)
        b.Property(x => x.ConditionParamKey).HasColumnName("CONDITION_PARAM_KEY").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.ConditionValue).HasColumnName("CONDITION_VALUE").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.AmountSource).HasColumnName("AMOUNT_SOURCE").HasConversion<int>();
        b.HasMany(x => x.Details).WithOne().HasForeignKey(d => d.TemplateLineId)
         .HasConstraintName("FK_OP_LINEDET_LINE").OnDelete(DeleteBehavior.Cascade);
        b.HasOne<TB_ACCOUNTCODE>().WithMany().HasForeignKey(x => x.SubsidiaryAccountId)
         .HasConstraintName("FK_OP_LINE_ACCOUNT").OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateLineDetailConfig : IEntityTypeConfiguration<TemplateLineDetail>
{
    public void Configure(EntityTypeBuilder<TemplateLineDetail> b)
    {
        b.ToTable("TB_OP_TEMPLATE_LINE_DETAIL");
        b.HasKey(x => x.Id).HasName("PK_OP_TEMPLATE_LINE_DETAIL");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.TemplateLineId).Char36("LINE_ID");
        b.Property(x => x.Level).HasColumnName("LEVEL_NO");
        b.Property(x => x.ParameterKey).HasColumnName("PARAM_KEY").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.FixedDetailId).Char36("FIXED_TAFSILI_ID");
        b.HasIndex(x => new { x.TemplateLineId, x.Level }, "UK_OP_LINEDET_LEVEL").IsUnique();
        // CHECK «دقیقاً یکی از PARAM_KEY / FIXED_TAFSILI_ID» فقط در DDL 067 است (نحو SQL وابسته به پایگاه).
        b.HasOne<TB_TAFSILI>().WithMany().HasForeignKey(x => x.FixedDetailId)
         .HasConstraintName("FK_OP_LINEDET_TAFSILI").OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OperationExecutionConfig : IEntityTypeConfiguration<OperationExecution>
{
    public void Configure(EntityTypeBuilder<OperationExecution> b)
    {
        b.ToTable("TB_OP_EXECUTION");
        b.HasKey(x => x.Id).HasName("PK_OP_EXECUTION");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.ClientRequestId).Char36("CLIENT_REQUEST_ID");
        b.Property(x => x.OperationTemplateId).Char36("TEMPLATE_ID");
        b.Property(x => x.TemplateCode).HasColumnName("TEMPLATE_CODE").HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.InputJson).HasColumnName("INPUT_JSON").HasMaxLength(4000).IsUnicode(false).IsRequired();
        b.Property(x => x.VoucherId).Char36("VOUCHER_ID");
        b.Property(x => x.VoucherNo).HasColumnName("VOUCHER_NO").HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(x => x.Channel).HasColumnName("CHANNEL").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.Property(x => x.TemplateSnapshot).HasColumnName("TEMPLATE_SNAPSHOT").HasColumnType("CLOB"); // DDL 082
        b.Property(x => x.TemplateHash).HasColumnName("TEMPLATE_HASH").HasMaxLength(64).IsUnicode(false); // DDL 082
        b.Property(x => x.AiTraceId).Char36("AI_TRACE_ID"); // DDL 083
        // DDL 084
        b.Property(x => x.UserSentence).HasColumnName("USER_SENTENCE").HasMaxLength(1000).IsUnicode(true);
        b.Property(x => x.AnswerSourcesJson).HasColumnName("ANSWER_SOURCES_JSON").HasMaxLength(2000).IsUnicode(false);
        b.Property(x => x.AiConfirmed).HasColumnName("AI_CONFIRMED").HasColumnType("NUMBER(1)");
        b.Property(x => x.RiskLevel).HasColumnName("RISK_LEVEL").HasConversion<int?>();
        b.Property(x => x.RiskReasonsJson).HasColumnName("RISK_REASONS_JSON").HasMaxLength(2000).IsUnicode(true);
        b.Property(x => x.WarningsJson).HasColumnName("WARNINGS_JSON").HasMaxLength(2000).IsUnicode(true);
        b.Property(x => x.TotalAmount).HasColumnName("TOTAL_AMOUNT").HasColumnType("NUMBER(18)");
        b.HasIndex(x => x.ClientRequestId, "UK_OP_EXEC_CLIENT_REQ").IsUnique();
        b.HasIndex(x => new { x.VahedCode, x.CreatedAtUtc }, "IX_OP_EXEC_VAHED");
        b.HasIndex(x => x.VoucherId, "IX_OP_EXEC_VOUCHER");
        b.HasOne<OperationTemplate>().WithMany().HasForeignKey(x => x.OperationTemplateId)
         .HasConstraintName("FK_OP_EXEC_TEMPLATE").OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TB_VOUCHERSHEAD>().WithMany().HasForeignKey(x => x.VoucherId)
         .HasConstraintName("FK_OP_EXEC_VOUCHER").OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>گزارش‌های ذخیره‌شدهٔ حسابیار — DDL 072.</summary>
public sealed class SavedReportConfig : IEntityTypeConfiguration<SavedReport>
{
    public void Configure(EntityTypeBuilder<SavedReport> b)
    {
        b.ToTable("TB_OP_SAVED_REPORT");
        b.HasKey(x => x.Id).HasName("PK_OP_SAVED_REPORT");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.Code).HasColumnName("CODE").HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(x => x.Title).HasColumnName("TITLE").HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(1000).IsUnicode(false);
        b.Property(x => x.Keywords).HasColumnName("KEYWORDS").HasMaxLength(1000).IsUnicode(false);
        b.Property(x => x.ReportKind).HasColumnName("REPORT_KIND").HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.SettingsJson).HasColumnName("SETTINGS_JSON").HasMaxLength(4000).IsUnicode(false).IsRequired();
        b.Property(x => x.AllowedVahedTypes).HasColumnName("ALLOWED_VAHED_TYPES").HasMaxLength(400).IsUnicode(false);
        b.Property(x => x.IsActive).HasColumnName("ISACTIVE").HasColumnType("NUMBER(1)");
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => x.Code, "UK_OP_SAVED_REPORT_CODE").IsUnique();
    }
}

/// <summary>سندهای پرتکرار حسابیار — DDL 079.</summary>
public sealed class QuickVoucherConfig : IEntityTypeConfiguration<QuickVoucher>
{
    public void Configure(EntityTypeBuilder<QuickVoucher> b)
    {
        b.ToTable("TB_OP_QUICK_VOUCHER");
        b.HasKey(x => x.Id).HasName("PK_OP_QUICK_VOUCHER");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.TemplateId).Char36("TEMPLATE_ID");
        b.Property(x => x.Title).HasColumnName("TITLE").HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(x => x.ValuesJson).HasColumnName("VALUES_JSON").HasMaxLength(4000).IsUnicode(false).IsRequired();
        b.Property(x => x.IsDeleted).HasColumnName("ISDELETED").HasColumnType("NUMBER(1)");
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => new { x.VahedCode, x.IsDeleted }, "IX_OP_QUICK_VOUCHER_VAHED");
    }
}

/// <summary>تاریخچهٔ بررسی سندهای حسابیار — DDL 081.</summary>
public sealed class OperationReviewConfig : IEntityTypeConfiguration<OperationReview>
{
    public void Configure(EntityTypeBuilder<OperationReview> b)
    {
        b.ToTable("TB_OP_REVIEW");
        b.HasKey(x => x.Id).HasName("PK_OP_REVIEW");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.ExecutionId).Char36("EXECUTION_ID");
        b.Property(x => x.VoucherId).Char36("VOUCHER_ID");
        b.Property(x => x.Action).HasColumnName("ACTION").HasConversion<int>(); // enum NUMBER(1): نوع ستون صریح نمی‌گذاریم (اوراکل NUMBER(1) را bool می‌خواند)
        b.Property(x => x.ReasonCode).HasColumnName("REASON_CODE").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Message).HasColumnName("MESSAGE").HasMaxLength(500).IsUnicode(true);
        b.Property(x => x.ReviewerId).HasColumnName("REVIEWER_ID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => new { x.ExecutionId, x.CreatedAtUtc }, "IX_OP_REVIEW_EXECUTION");
        b.HasIndex(x => new { x.VahedCode, x.CreatedAtUtc }, "IX_OP_REVIEW_VAHED");
    }
}

/// <summary>ردپای هوش مصنوعی حسابیار — DDL 083.</summary>
public sealed class OperationAiTraceConfig : IEntityTypeConfiguration<OperationAiTrace>
{
    public void Configure(EntityTypeBuilder<OperationAiTrace> b)
    {
        b.ToTable("TB_OP_AI_TRACE");
        b.HasKey(x => x.Id).HasName("PK_OP_AI_TRACE");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.MaskedSentence).HasColumnName("MASKED_SENTENCE").HasMaxLength(1000).IsUnicode(true).IsRequired();
        b.Property(x => x.Provider).HasColumnName("PROVIDER").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Model).HasColumnName("MODEL").HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.PromptVersion).HasColumnName("PROMPT_VERSION").HasMaxLength(30).IsUnicode(false).IsRequired();
        b.Property(x => x.ResultJson).HasColumnName("RESULT_JSON").HasColumnType("CLOB");
        b.Property(x => x.Confidence).HasColumnName("CONFIDENCE").HasPrecision(5, 4);
        b.Property(x => x.LatencyMs).HasColumnName("LATENCY_MS");
        b.Property(x => x.Succeeded).HasColumnName("SUCCEEDED").HasColumnType("NUMBER(1)");
        b.Property(x => x.Error).HasColumnName("ERROR").HasMaxLength(200).IsUnicode(false);
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => new { x.VahedCode, x.CreatedAtUtc }, "IX_OP_AI_TRACE_VAHED");
    }
}

/// <summary>«موضوع را برای حسابدار بفرستید» — DDL 084.</summary>
public sealed class OperationRequestConfig : IEntityTypeConfiguration<OperationRequest>
{
    public void Configure(EntityTypeBuilder<OperationRequest> b)
    {
        b.ToTable("TB_OP_REQUEST");
        b.HasKey(x => x.Id).HasName("PK_OP_REQUEST");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.Sentence).HasColumnName("SENTENCE").HasMaxLength(1000).IsUnicode(true).IsRequired();
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.Property(x => x.Status).HasColumnName("STATUS").HasConversion<int>();
        b.Property(x => x.HandledBy).HasColumnName("HANDLED_BY").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.HandledAtUtc).HasColumnName("HANDLED_AT").HasPrecision(6);
        b.Property(x => x.Note).HasColumnName("NOTE").HasMaxLength(500).IsUnicode(true);
        b.HasIndex(x => new { x.VahedCode, x.Status, x.CreatedAtUtc }, "IX_OP_REQUEST_VAHED");
    }
}
