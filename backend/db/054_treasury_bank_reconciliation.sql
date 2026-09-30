--------------------------------------------------------------------------------------------------
-- 054_treasury_bank_reconciliation.sql
--
-- خزانه‌داری ("تنخواه و خزانه‌داری")، بخش ۴-د (۲۰۲۶-۰۹-۲۹ owner decisions): مغایرت‌گیری بانکی
-- (Bank Reconciliation) + یک ستون تازه روی TB_TR_SETTING برای سند «کارمزد بانکی». داشبورد خزانه
-- (بخش ۴-د دوم) جدول تازه‌ای ندارد — فقط کوئری روی جدول‌های موجود.
--
-- صورت‌حساب بانکی («دیسکت») می‌تواند دستی وارد شود یا (بعداً، وقتی صاحب پروژه قالب فایل بانک را
-- بدهد) از فایل import شود — TB_TR_BANK_STATEMENT.SOURCE این دو را افتراق می‌دهد. Seam برای
-- import: Accounting.Application.Common.Interfaces.IBankStatementFileParser (بدون پیاده‌سازی
-- ثبت‌شده تا وقتی قالب بیاید).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- NOT executed anywhere as of authoring time (2026-09-29). Same standing rule as every
-- 044-053_*.sql script.
--
-- Design reference: docs/tankhah-khazaneh-module.md §۱۰ (خزانه‌داری)، بخش ۴-د. Read that section
-- before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_TR_SETTING — یک ستون تازهٔ بخش ۴-د. NULL-پذیر، بدون مقدار پیش‌فرض (همان الگوی بخش‌های
-- ۴-الف/۴-ب/۴-ج — تا وقتی مدیر مالی تعریف نکند، صدور سند کارمزد بانکی با ۴۰۹ رد می‌شود).
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_TR_SETTING ADD BANK_FEE_ACCOUNT_ID CHAR(36);

--------------------------------------------------------------------------------------------------
-- TB_TR_BANK_STATEMENT — «صورت‌حساب بانکی» یک حساب بانکی در یک بازهٔ تاریخی (BST-xxxxxx). هیچ
-- ستون شناسه‌ای اینجا FK واقعی روی TB_ACCOUNT ندارد؛ همان الگوی ریسک باز #۹/#۱۴ — عمداً تکرار شده.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_BANK_STATEMENT
(
    ID               CHAR(36)      NOT NULL,
    CODE             VARCHAR2(20)  NOT NULL,   -- "BST-" + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)
    BANK_ACCOUNT_ID  CHAR(36)      NOT NULL,   -- TB_ACCOUNT.ID، بدون FK واقعی (بالا را ببین)
    FROM_DATE        VARCHAR2(8)   NOT NULL,   -- YYYYMMDD شمسی
    TO_DATE          VARCHAR2(8)   NOT NULL,   -- YYYYMMDD شمسی
    CLOSING_BALANCE  NUMBER(25)    NOT NULL,   -- مانده پایانی طبق صورت‌حساب بانک
    SOURCE           NUMBER(1)     NOT NULL,   -- BankStatementSource: 1=دستی,2=Import
    STATE            NUMBER(1)     NOT NULL,   -- BankStatementState: 1=باز,2=بسته
    DESCRIPTION      VARCHAR2(1000),
    CREATEDDATE      TIMESTAMP(6)  NOT NULL,
    UPDATEDDATE      TIMESTAMP(6),
    ADDUSERID        VARCHAR2(10)  NOT NULL,
    CHANGEUSERID     VARCHAR2(10),
    VAHEDCODE        VARCHAR2(4),
    YEAR             VARCHAR2(4),
    ISDELETED        NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_BANK_STATEMENT PRIMARY KEY (ID)
);

CREATE INDEX IDX_TR_BSTMT_STATE ON TB_TR_BANK_STATEMENT (VAHEDCODE, YEAR, STATE);
CREATE INDEX IDX_TR_BSTMT_CODE ON TB_TR_BANK_STATEMENT (VAHEDCODE, YEAR);
CREATE INDEX IDX_TR_BSTMT_BANKACC ON TB_TR_BANK_STATEMENT (BANK_ACCOUNT_ID);

--------------------------------------------------------------------------------------------------
-- TB_TR_BANK_STATEMENT_LINE — یک ردیف صورت‌حساب بانکی (برداشت یا واریز). دقیقاً یکی از
-- WITHDRAWAL/DEPOSIT باید > 0 باشد (کنترل سمت Application).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_BANK_STATEMENT_LINE
(
    ID                       CHAR(36)      NOT NULL,
    STATEMENT_ID             CHAR(36)      NOT NULL,
    LINE_DATE                VARCHAR2(8)   NOT NULL,   -- YYYYMMDD شمسی
    BANK_REFERENCE           VARCHAR2(100),
    DESCRIPTION              VARCHAR2(1000),
    WITHDRAWAL               NUMBER(25)    DEFAULT 0 NOT NULL,
    DEPOSIT                  NUMBER(25)    DEFAULT 0 NOT NULL,
    BALANCE                  NUMBER(25),
    MATCH_STATE              NUMBER(1)     NOT NULL,   -- BankStatementLineMatchState: 1=نامنطبق,2=خودکار,3=دستی,4=حل‌شده
    MATCHED_VOUCHERDETAIL_ID CHAR(36),                  -- TB_VOUCHERSDETAIL.ID — بدون FK واقعی
    RESOLUTION_TYPE          NUMBER(1),                 -- BankStatementLineResolutionType: 1=سند کارمزد,2=دریافت مرتبط,3=نادیده‌گرفته‌شده
    RESOLUTION_VOUCHER_ID    CHAR(36),                  -- سند GL موقت «کارمزد بانکی» — فقط RESOLUTION_TYPE=1
    RESOLUTION_RECEIPT_ID    CHAR(36),                  -- TB_TR_RECEIPT.ID — فقط RESOLUTION_TYPE=2
    RESOLUTION_NOTE          VARCHAR2(1000),
    CREATEDDATE              TIMESTAMP(6)  NOT NULL,
    UPDATEDDATE              TIMESTAMP(6),
    ADDUSERID                VARCHAR2(10)  NOT NULL,
    CHANGEUSERID             VARCHAR2(10),
    VAHEDCODE                VARCHAR2(4),
    YEAR                     VARCHAR2(4),
    ISDELETED                NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_BSTMT_LINE PRIMARY KEY (ID),
    CONSTRAINT FK_TR_BSTMT_LINE_STMT FOREIGN KEY (STATEMENT_ID) REFERENCES TB_TR_BANK_STATEMENT (ID)
);

CREATE INDEX IDX_TR_BSTMT_LINE_STMT ON TB_TR_BANK_STATEMENT_LINE (STATEMENT_ID, MATCH_STATE);
CREATE INDEX IDX_TR_BSTMT_LINE_VDETAIL ON TB_TR_BANK_STATEMENT_LINE (MATCHED_VOUCHERDETAIL_ID);
