--------------------------------------------------------------------------------------------------
-- 087_op_agent_conversation.sql
--
-- حسابیار فاز ۶۵ (۲۰۲۶-۱۰-۲۰): گفتگو با دستیار. هر گفتگو مال یک کاربر در یک واحد است؛ پیام‌ها به ترتیب SEQ.
-- نقش پیام: 1 کاربر، 2 دستیار، 3 ابزار (فقط ردپا). پیام دستیار می‌تواند پیشنهاد سند (PROPOSAL_JSON) و
-- ردپای هوش مصنوعی (AI_TRACE_ID ⇒ TB_OP_AI_TRACE) داشته باشد. حذف نرم با ISDELETED.
-- پیش‌نیاز: 083. ⚠️ بدون این اسکریپت فقط صفحهٔ «گفتگو با حسابیار» خطا می‌دهد؛ بقیهٔ حسابیار کار می‌کند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_AGENT_CONV
(
    ID          CHAR(36)        NOT NULL,
    VAHEDCODE   VARCHAR2(4)     NOT NULL,
    TITLE       NVARCHAR2(100)  NOT NULL,
    ADDUSERID   VARCHAR2(10)    NOT NULL,
    CREATEDDATE TIMESTAMP       NOT NULL,
    UPDATEDDATE TIMESTAMP       NOT NULL,
    ISDELETED   NUMBER(1)       DEFAULT 0 NOT NULL,
    CONSTRAINT PK_OP_AGENT_CONV PRIMARY KEY (ID),
    CONSTRAINT CK_OP_AGENT_CONV_DELETED CHECK (ISDELETED IN (0, 1))
);

CREATE INDEX IX_OP_AGENT_CONV_USER ON TB_OP_AGENT_CONV (ADDUSERID, VAHEDCODE, UPDATEDDATE);

CREATE TABLE TB_OP_AGENT_MSG
(
    ID            CHAR(36)       NOT NULL,
    CONV_ID       CHAR(36)       NOT NULL,
    SEQ           NUMBER(5)      NOT NULL,
    ROLE          NUMBER(1)      NOT NULL,
    CONTENT       CLOB           NOT NULL,
    TOOL_NAME     VARCHAR2(50),
    PROPOSAL_JSON CLOB,
    AI_TRACE_ID   CHAR(36),
    CREATEDDATE   TIMESTAMP      NOT NULL,
    CONSTRAINT PK_OP_AGENT_MSG PRIMARY KEY (ID),
    CONSTRAINT FK_OP_AGENT_MSG_CONV FOREIGN KEY (CONV_ID) REFERENCES TB_OP_AGENT_CONV (ID),
    CONSTRAINT FK_OP_AGENT_MSG_TRACE FOREIGN KEY (AI_TRACE_ID) REFERENCES TB_OP_AI_TRACE (ID),
    CONSTRAINT UK_OP_AGENT_MSG_SEQ UNIQUE (CONV_ID, SEQ),
    CONSTRAINT CK_OP_AGENT_MSG_ROLE CHECK (ROLE IN (1, 2, 3))
);
