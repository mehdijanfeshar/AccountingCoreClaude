--------------------------------------------------------------------------------------------------
-- 088_op_ai_evaluation.sql
--
-- حسابیار فاز ۶۵ (۲۰۲۶-۱۰-۲۰): سنجش هوش مصنوعی — نمونه‌های طلایی و اجرای سایه.
--   TB_OP_AI_GOLDEN    : جمله + الگو و جواب‌های درست (تفصیلی = نام). از اجرای واقعی هم ساخته می‌شود.
--   TB_OP_AI_EVAL_RUN  : هر دور سنجش (KIND 1 = طلایی، 2 = سایه) با شمارنده‌ها و میانگین تأخیر.
--   TB_OP_AI_EVAL_ITEM : نتیجهٔ هر جمله (جملهٔ پوشانده، الگوی انتظار/پاسخ، تعداد جواب درست، اختلاف‌ها).
-- پیش‌نیاز: 067. ⚠️ بدون این اسکریپت فقط صفحهٔ «سنجش هوش مصنوعی» خطا می‌دهد.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_AI_GOLDEN
(
    ID                    CHAR(36)        NOT NULL,
    SENTENCE              NVARCHAR2(1000) NOT NULL,
    EXPECTED_TEMPLATE     VARCHAR2(50),
    EXPECTED_ANSWERS_JSON CLOB,
    NOTE                  NVARCHAR2(500),
    SOURCE_EXECUTION_ID   CHAR(36),
    ISACTIVE              NUMBER(1)       DEFAULT 1 NOT NULL,
    ISDELETED             NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID             VARCHAR2(10)    NOT NULL,
    CREATEDDATE           TIMESTAMP       NOT NULL,
    CONSTRAINT PK_OP_AI_GOLDEN PRIMARY KEY (ID),
    CONSTRAINT CK_OP_AI_GOLDEN_ACTIVE CHECK (ISACTIVE IN (0, 1)),
    CONSTRAINT CK_OP_AI_GOLDEN_DELETED CHECK (ISDELETED IN (0, 1))
);

CREATE INDEX IX_OP_AI_GOLDEN_SOURCE ON TB_OP_AI_GOLDEN (SOURCE_EXECUTION_ID);

CREATE TABLE TB_OP_AI_EVAL_RUN
(
    ID             CHAR(36)      NOT NULL,
    KIND           NUMBER(1)     NOT NULL,
    VAHEDCODE      VARCHAR2(4)   NOT NULL,
    PROVIDER       VARCHAR2(20)  NOT NULL,
    MODEL          VARCHAR2(100),
    PROMPT_VERSION VARCHAR2(30)  NOT NULL,
    TOTAL          NUMBER(5)     NOT NULL,
    TEMPLATE_HITS  NUMBER(5)     NOT NULL,
    ANSWER_TOTAL   NUMBER(6)     NOT NULL,
    ANSWER_HITS    NUMBER(6)     NOT NULL,
    FAILURES       NUMBER(5)     NOT NULL,
    AVG_LATENCY_MS NUMBER(9)     NOT NULL,
    ADDUSERID      VARCHAR2(10)  NOT NULL,
    CREATEDDATE    TIMESTAMP     NOT NULL,
    CONSTRAINT PK_OP_AI_EVAL_RUN PRIMARY KEY (ID),
    CONSTRAINT CK_OP_AI_EVAL_RUN_KIND CHECK (KIND IN (1, 2))
);

CREATE INDEX IX_OP_AI_EVAL_RUN_DATE ON TB_OP_AI_EVAL_RUN (CREATEDDATE);

CREATE TABLE TB_OP_AI_EVAL_ITEM
(
    ID                CHAR(36)        NOT NULL,
    RUN_ID            CHAR(36)        NOT NULL,
    GOLDEN_ID         CHAR(36),
    EXECUTION_ID      CHAR(36),
    MASKED_SENTENCE   NVARCHAR2(1000) NOT NULL,
    EXPECTED_TEMPLATE VARCHAR2(50),
    ACTUAL_TEMPLATE   VARCHAR2(50),
    TEMPLATE_OK       NUMBER(1)       DEFAULT 0 NOT NULL,
    ANSWERS_EXPECTED  NUMBER(3)       NOT NULL,
    ANSWERS_OK        NUMBER(3)       NOT NULL,
    DIFF_JSON         CLOB,
    LATENCY_MS        NUMBER(9)       NOT NULL,
    ERROR             VARCHAR2(200),
    CONSTRAINT PK_OP_AI_EVAL_ITEM PRIMARY KEY (ID),
    CONSTRAINT FK_OP_AI_EVAL_ITEM_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_OP_AI_EVAL_RUN (ID),
    CONSTRAINT CK_OP_AI_EVAL_ITEM_OK CHECK (TEMPLATE_OK IN (0, 1))
);

CREATE INDEX IX_OP_AI_EVAL_ITEM_RUN ON TB_OP_AI_EVAL_ITEM (RUN_ID);
