--------------------------------------------------------------------------------------------------
-- 064_cheque_approval.sql
--
-- دفتر چک (عملیات، ۲۰۲۶-۱۰-۰۵): کارتابل تأیید چک — جایگزین «دستور پرداخت» و «تاییدیه چک» کاغذی سیستم
-- قدیم. گردش: صدور دستور پرداخت (1) ⇒ تأیید رئیس حسابداری (2) ⇒ تأیید مدیر واحد = تاییدیه چک (3)؛
-- برگشت (4). فقط چک «تأییدشده» چاپ می‌شود. یک ردیف به‌ازای هر چک (UK روی CHECK_ID).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_CHECK_APPROVAL
(
    ID               CHAR(36)      NOT NULL,
    CHECK_ID         CHAR(36)      NOT NULL,
    STATE            NUMBER(1)     NOT NULL,
    PREPARED_BY      VARCHAR2(10)  NOT NULL,
    PREPARED_DATE    TIMESTAMP     NOT NULL,
    ACCOUNTING_BY    VARCHAR2(10),
    ACCOUNTING_DATE  TIMESTAMP,
    MANAGER_BY       VARCHAR2(10),
    MANAGER_DATE     TIMESTAMP,
    NOTE             VARCHAR2(500),
    VAHEDCODE        VARCHAR2(4)   NOT NULL,
    YEAR             VARCHAR2(4),
    CREATEDDATE      TIMESTAMP     NOT NULL,
    UPDATEDDATE      TIMESTAMP,
    ADDUSERID        VARCHAR2(10)  NOT NULL,
    CHANGEUSERID     VARCHAR2(10),
    ISDELETED        NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_CHECK_APPROVAL PRIMARY KEY (ID),
    CONSTRAINT UK_CHECK_APPROVAL_CHECK UNIQUE (CHECK_ID),
    CONSTRAINT FK_CHECK_APPROVAL_CHECK FOREIGN KEY (CHECK_ID) REFERENCES TB_CHECK (ID)
);

CREATE INDEX IX_CHECK_APPROVAL_VAHED ON TB_CHECK_APPROVAL (VAHEDCODE, STATE);

CREATE TABLE TB_CHECK_APPROVAL_EVENT
(
    ID           CHAR(36)      NOT NULL,
    APPROVAL_ID  CHAR(36)      NOT NULL,
    ACTION       NUMBER(1)     NOT NULL,
    FROM_STATE   NUMBER(1),
    TO_STATE     NUMBER(1)     NOT NULL,
    USERID       VARCHAR2(10)  NOT NULL,
    NOTE         VARCHAR2(500),
    CREATEDDATE  TIMESTAMP     NOT NULL,
    CONSTRAINT PK_CHECK_APPROVAL_EVENT PRIMARY KEY (ID),
    CONSTRAINT FK_CHECK_APPROVAL_EVENT FOREIGN KEY (APPROVAL_ID) REFERENCES TB_CHECK_APPROVAL (ID)
);

CREATE INDEX IX_CHECK_APPROVAL_EVENT ON TB_CHECK_APPROVAL_EVENT (APPROVAL_ID);
