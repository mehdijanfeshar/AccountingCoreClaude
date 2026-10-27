using Accounting.Domain.OperationTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.OperationTemplates;

// حسابیار فاز ۶۵ — نگاشت گفتگو با دستیار (DDL 087) و سنجش هوش مصنوعی (DDL 088).

/// <summary>گفتگو با دستیار — DDL 087.</summary>
public sealed class AgentConversationConfig : IEntityTypeConfiguration<AgentConversation>
{
    public void Configure(EntityTypeBuilder<AgentConversation> b)
    {
        b.ToTable("TB_OP_AGENT_CONV");
        b.HasKey(x => x.Id).HasName("PK_OP_AGENT_CONV");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.Title).HasColumnName("TITLE").HasMaxLength(100).IsUnicode(true).IsRequired();
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.Property(x => x.UpdatedAtUtc).HasColumnName("UPDATEDDATE").HasPrecision(6);
        b.Property(x => x.IsDeleted).HasColumnName("ISDELETED").HasColumnType("NUMBER(1)");
        b.HasIndex(x => new { x.CreatedBy, x.VahedCode, x.UpdatedAtUtc }, "IX_OP_AGENT_CONV_USER");
    }
}

/// <summary>پیام گفتگو — DDL 087.</summary>
public sealed class AgentMessageConfig : IEntityTypeConfiguration<AgentMessage>
{
    public void Configure(EntityTypeBuilder<AgentMessage> b)
    {
        b.ToTable("TB_OP_AGENT_MSG");
        b.HasKey(x => x.Id).HasName("PK_OP_AGENT_MSG");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.ConversationId).Char36("CONV_ID").IsRequired();
        b.Property(x => x.Seq).HasColumnName("SEQ");
        b.Property(x => x.Role).HasColumnName("ROLE").HasConversion<int>(); // enum NUMBER(1): نوع ستون صریح نمی‌گذاریم
        b.Property(x => x.Content).HasColumnName("CONTENT").HasColumnType("CLOB").IsRequired();
        b.Property(x => x.ToolName).HasColumnName("TOOL_NAME").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.ProposalJson).HasColumnName("PROPOSAL_JSON").HasColumnType("CLOB");
        b.Property(x => x.AiTraceId).Char36("AI_TRACE_ID");
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => new { x.ConversationId, x.Seq }, "UK_OP_AGENT_MSG_SEQ").IsUnique();
        // رابطه‌ها باید در مدل باشند تا EF ترتیب درج را درست بچیند (گفتگو و ردپا پیش از پیام؛ وگرنه ORA-02291)
        b.HasOne<AgentConversation>().WithMany().HasForeignKey(x => x.ConversationId)
         .HasConstraintName("FK_OP_AGENT_MSG_CONV").OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationAiTrace>().WithMany().HasForeignKey(x => x.AiTraceId)
         .HasConstraintName("FK_OP_AGENT_MSG_TRACE").OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>نمونهٔ طلایی — DDL 088.</summary>
public sealed class AiGoldenCaseConfig : IEntityTypeConfiguration<AiGoldenCase>
{
    public void Configure(EntityTypeBuilder<AiGoldenCase> b)
    {
        b.ToTable("TB_OP_AI_GOLDEN");
        b.HasKey(x => x.Id).HasName("PK_OP_AI_GOLDEN");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.Sentence).HasColumnName("SENTENCE").HasMaxLength(1000).IsUnicode(true).IsRequired();
        b.Property(x => x.ExpectedTemplateCode).HasColumnName("EXPECTED_TEMPLATE").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.ExpectedAnswersJson).HasColumnName("EXPECTED_ANSWERS_JSON").HasColumnType("CLOB");
        b.Property(x => x.Note).HasColumnName("NOTE").HasMaxLength(500).IsUnicode(true);
        b.Property(x => x.SourceExecutionId).Char36("SOURCE_EXECUTION_ID");
        b.Property(x => x.IsActive).HasColumnName("ISACTIVE").HasColumnType("NUMBER(1)");
        b.Property(x => x.IsDeleted).HasColumnName("ISDELETED").HasColumnType("NUMBER(1)");
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => x.SourceExecutionId, "IX_OP_AI_GOLDEN_SOURCE");
    }
}

/// <summary>دور سنجش — DDL 088.</summary>
public sealed class AiEvalRunConfig : IEntityTypeConfiguration<AiEvalRun>
{
    public void Configure(EntityTypeBuilder<AiEvalRun> b)
    {
        b.ToTable("TB_OP_AI_EVAL_RUN");
        b.HasKey(x => x.Id).HasName("PK_OP_AI_EVAL_RUN");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.Kind).HasColumnName("KIND").HasConversion<int>(); // enum NUMBER(1): نوع ستون صریح نمی‌گذاریم
        b.Property(x => x.VahedCode).HasColumnName("VAHEDCODE").HasMaxLength(4).IsUnicode(false).IsRequired();
        b.Property(x => x.Provider).HasColumnName("PROVIDER").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Model).HasColumnName("MODEL").HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.PromptVersion).HasColumnName("PROMPT_VERSION").HasMaxLength(30).IsUnicode(false).IsRequired();
        b.Property(x => x.Total).HasColumnName("TOTAL");
        b.Property(x => x.TemplateHits).HasColumnName("TEMPLATE_HITS");
        b.Property(x => x.AnswerTotal).HasColumnName("ANSWER_TOTAL");
        b.Property(x => x.AnswerHits).HasColumnName("ANSWER_HITS");
        b.Property(x => x.Failures).HasColumnName("FAILURES");
        b.Property(x => x.AvgLatencyMs).HasColumnName("AVG_LATENCY_MS");
        b.Property(x => x.CreatedBy).HasColumnName("ADDUSERID").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("CREATEDDATE").HasPrecision(6);
        b.HasIndex(x => x.CreatedAtUtc, "IX_OP_AI_EVAL_RUN_DATE");
    }
}

/// <summary>نتیجهٔ هر جمله در دور سنجش — DDL 088.</summary>
public sealed class AiEvalItemConfig : IEntityTypeConfiguration<AiEvalItem>
{
    public void Configure(EntityTypeBuilder<AiEvalItem> b)
    {
        b.ToTable("TB_OP_AI_EVAL_ITEM");
        b.HasKey(x => x.Id).HasName("PK_OP_AI_EVAL_ITEM");
        b.Property(x => x.Id).Char36("ID").ValueGeneratedNever();
        b.Property(x => x.RunId).Char36("RUN_ID").IsRequired();
        b.Property(x => x.GoldenCaseId).Char36("GOLDEN_ID");
        b.Property(x => x.ExecutionId).Char36("EXECUTION_ID");
        b.Property(x => x.MaskedSentence).HasColumnName("MASKED_SENTENCE").HasMaxLength(1000).IsUnicode(true).IsRequired();
        b.Property(x => x.ExpectedTemplateCode).HasColumnName("EXPECTED_TEMPLATE").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.ActualTemplateCode).HasColumnName("ACTUAL_TEMPLATE").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.TemplateOk).HasColumnName("TEMPLATE_OK").HasColumnType("NUMBER(1)");
        b.Property(x => x.AnswersExpected).HasColumnName("ANSWERS_EXPECTED");
        b.Property(x => x.AnswersOk).HasColumnName("ANSWERS_OK");
        b.Property(x => x.DiffJson).HasColumnName("DIFF_JSON").HasColumnType("CLOB");
        b.Property(x => x.LatencyMs).HasColumnName("LATENCY_MS");
        b.Property(x => x.Error).HasColumnName("ERROR").HasMaxLength(200).IsUnicode(false);
        b.HasIndex(x => x.RunId, "IX_OP_AI_EVAL_ITEM_RUN");
        b.HasOne<AiEvalRun>().WithMany().HasForeignKey(x => x.RunId)
         .HasConstraintName("FK_OP_AI_EVAL_ITEM_RUN").OnDelete(DeleteBehavior.Restrict);
    }
}
