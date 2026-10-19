--------------------------------------------------------------------------------------------------
-- 083_op_ai_trace.sql
--
-- حسابیار فاز ۶۰ / G4 (۲۰۲۶-۱۰-۱۸): ردپای هوش مصنوعی. هر «فهم جمله» (interpret) یک ردیف می‌نویسد:
-- جملهٔ پوشانده‌شده (هرگز جملهٔ خام)، ارائه‌دهنده، مدل، نسخهٔ پرامپت، نتیجه، اطمینان، تأخیر، موفقیت.
-- اجرا با ستون AI_TRACE_ID به همان ردیف وصل می‌شود. پیش‌نیاز: 067 (و 082 برای یکپارچگی ستون‌های اجرا).
-- ⚠️ بدون این اسکریپت interpret و همهٔ endpointهای خواندن/نوشتن TB_OP_EXECUTION خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_AI_TRACE
(
    ID              CHAR(36)        NOT NULL,
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    MASKED_SENTENCE NVARCHAR2(1000) NOT NULL,
    PROVIDER        VARCHAR2(20)    NOT NULL,
    MODEL           VARCHAR2(100),
    PROMPT_VERSION  VARCHAR2(30)    NOT NULL,
    RESULT_JSON     CLOB,
    CONFIDENCE      NUMBER(5,4),
    LATENCY_MS      NUMBER(9)       NOT NULL,
    SUCCEEDED       NUMBER(1)       DEFAULT 0 NOT NULL,
    ERROR           VARCHAR2(200),
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_OP_AI_TRACE PRIMARY KEY (ID),
    CONSTRAINT CK_OP_AI_TRACE_SUCCEEDED CHECK (SUCCEEDED IN (0, 1))
);

CREATE INDEX IX_OP_AI_TRACE_VAHED ON TB_OP_AI_TRACE (VAHEDCODE, CREATEDDATE);

ALTER TABLE TB_OP_EXECUTION ADD (AI_TRACE_ID CHAR(36));
ALTER TABLE TB_OP_EXECUTION ADD CONSTRAINT FK_OP_EXEC_AI_TRACE FOREIGN KEY (AI_TRACE_ID) REFERENCES TB_OP_AI_TRACE (ID);
CREATE INDEX IX_OP_EXEC_AI_TRACE ON TB_OP_EXECUTION (AI_TRACE_ID);
