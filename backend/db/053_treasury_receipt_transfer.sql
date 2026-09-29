--------------------------------------------------------------------------------------------------
-- 053_treasury_receipt_transfer.sql
--
-- خزانه‌داری ("تنخواه و خزانه‌داری")، بخش ۴-ج (۲۰۲۶-۰۹-۲۹ owner decisions): دریافت وجه (Receipt) و
-- انتقال وجه بین دو حساب بانکی (Transfer). دو جدول جانبی کاملاً جدید + یک جدول رویداد (فقط برای
-- Transfer — Receipt گردش تأیید ندارد) + سه ستون تازه روی TB_TR_SETTING.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- NOT executed anywhere as of authoring time (2026-09-29). Same standing rule as every
-- 044-052_*.sql script.
--
-- Design reference: docs/tankhah-khazaneh-module.md §۱۰ (خزانه‌داری)، بخش ۴-ج. Read that section
-- before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_TR_SETTING — سه ستون تازهٔ بخش ۴-ج. هر سه NULL-پذیرند، بدون مقدار پیش‌فرض (همان الگوی
-- بخش‌های ۴-الف/۴-ب — تا وقتی مدیر مالی تعریف نکند، عملیات وابسته با ۴۰۹ رد می‌شود).
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_TR_SETTING ADD RECEIVABLES_ACCOUNT_ID CHAR(36);
ALTER TABLE TB_TR_SETTING ADD CUSTOMER_TAFSIL_GROUP_ID CHAR(36);
ALTER TABLE TB_TR_SETTING ADD DAILY_TRANSFER_LIMIT NUMBER(25);

--------------------------------------------------------------------------------------------------
-- TB_TR_RECEIPT — «دریافت وجه» (RCV-xxxxxx). بدون زنجیرهٔ تأیید — فقط register (خزانه‌دار) صادر
-- می‌کند. هیچ ستون شناسه‌ای اینجا FK واقعی روی TB_ACCOUNTCODE/TB_ACCOUNT/TB_TAFSILI ندارد؛ همان
-- الگوی ریسک باز #۹/#۱۴ — عمداً تکرار شده.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_RECEIPT
(
    ID                CHAR(36)      NOT NULL,
    CODE              VARCHAR2(20)  NOT NULL,   -- "RCV-" + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)
    PAYER_TAFSILI_ID  CHAR(36)      NOT NULL,   -- TB_TAFSILI.ID، عضو TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID
    PAYER_NAME        VARCHAR2(200) NOT NULL,   -- عکس‌فوری نام تفصیلی در لحظهٔ ثبت
    AMOUNT            NUMBER(25)    NOT NULL,
    BANK_ACCOUNT_ID   CHAR(36)      NOT NULL,   -- TB_ACCOUNT.ID، بدون FK واقعی (بالا را ببین)
    RECEIPT_METHOD    NUMBER(1)     NOT NULL,   -- TreasuryPaymentMethod: 1=ساتنا,2=پایا,3=چک,4=نقد
    RECEIPT_DATE      VARCHAR2(8)   NOT NULL,   -- YYYYMMDD شمسی
    BANK_REFERENCE    VARCHAR2(100) NOT NULL,   -- یکتایی سمت Application، در میان دریافت‌های زندهٔ غیرلغوشدهٔ همان BANK_ACCOUNT_ID
    INVOICE_REF       VARCHAR2(100),            -- آزاد، بدون تخصیص واقعی (بدون ماژول فاکتور فروش)
    DESCRIPTION       VARCHAR2(1000),
    STATE             NUMBER(1)     NOT NULL,   -- ReceiptState: 1=Draft,2=Registered,3=Cancelled
    VOUCHER_ID        CHAR(36),                 -- register پر می‌کند
    PAYRECIVHEAD_ID   CHAR(36),                 -- register پر می‌کند (TB_PAYRECIVHEAD نوع دریافت)
    REGISTERED_BY     VARCHAR2(10),
    REGISTERED_DATE   TIMESTAMP,
    CREATEDDATE       TIMESTAMP     NOT NULL,
    UPDATEDDATE       TIMESTAMP,
    ADDUSERID         VARCHAR2(10)  NOT NULL,
    CHANGEUSERID      VARCHAR2(10),
    VAHEDCODE         VARCHAR2(4),
    YEAR              VARCHAR2(4),
    ISDELETED         NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_RECEIPT PRIMARY KEY (ID)
);

-- Backs GET receipts?state=&search=&pageNumber=&pageSize= و stateCounts.
CREATE INDEX IDX_TR_RECEIPT_STATE ON TB_TR_RECEIPT (VAHEDCODE, YEAR, STATE);

-- Backs تولید CODE بعدی.
CREATE INDEX IDX_TR_RECEIPT_CODE ON TB_TR_RECEIPT (VAHEDCODE, YEAR);

-- Backs چک یکتایی BANK_REFERENCE در میان دریافت‌های زندهٔ همان حساب بانکی.
CREATE INDEX IDX_TR_RECEIPT_BANKREF ON TB_TR_RECEIPT (BANK_ACCOUNT_ID, BANK_REFERENCE);

--------------------------------------------------------------------------------------------------
-- TB_TR_TRANSFER — «انتقال وجه» بین دو حساب بانکی (TRF-xxxxxx). تک‌مرحله‌ای تأیید (فقط خزانه‌دار).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_TRANSFER
(
    ID                     CHAR(36)      NOT NULL,
    CODE                   VARCHAR2(20)  NOT NULL,   -- "TRF-" + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)
    SOURCE_BANK_ACCOUNT_ID CHAR(36)      NOT NULL,   -- TB_ACCOUNT.ID، بدون FK واقعی
    DEST_BANK_ACCOUNT_ID   CHAR(36)      NOT NULL,   -- TB_ACCOUNT.ID، بدون FK واقعی — باید با مبدأ متفاوت باشد (سمت Application)
    AMOUNT                 NUMBER(25)    NOT NULL,
    TRANSFER_DATE          VARCHAR2(8)   NOT NULL,   -- YYYYMMDD شمسی
    TRANSFER_METHOD        NUMBER(1)     NOT NULL,   -- TreasuryPaymentMethod
    REASON                 VARCHAR2(1000) NOT NULL,
    STATE                  NUMBER(1)     NOT NULL,   -- TransferState: 1=Draft,2=PendingTreasurer,3=Executed,4=Returned,5=Rejected
    BANK_REFERENCE         VARCHAR2(100),            -- approve پر می‌کند، از آن پس الزامی
    VOUCHER_ID             CHAR(36),                 -- approve پر می‌کند
    APPROVED_BY            VARCHAR2(10),
    APPROVED_DATE          TIMESTAMP,
    RETURN_REASON          VARCHAR2(1000),
    CREATEDDATE            TIMESTAMP     NOT NULL,
    UPDATEDDATE            TIMESTAMP,
    ADDUSERID              VARCHAR2(10)  NOT NULL,
    CHANGEUSERID           VARCHAR2(10),
    VAHEDCODE              VARCHAR2(4),
    YEAR                   VARCHAR2(4),
    ISDELETED              NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_TRANSFER PRIMARY KEY (ID)
);

-- Backs GET transfers?state=&search=&pageNumber=&pageSize= و stateCounts، و کارتابل تأیید
-- (PendingTreasurer).
CREATE INDEX IDX_TR_TRANSFER_STATE ON TB_TR_TRANSFER (VAHEDCODE, YEAR, STATE);

-- Backs تولید CODE بعدی.
CREATE INDEX IDX_TR_TRANSFER_CODE ON TB_TR_TRANSFER (VAHEDCODE, YEAR);

-- Backs چک سقف روزانهٔ انتقال (مجموع Executed از همان مبدأ، همان TRANSFER_DATE).
CREATE INDEX IDX_TR_TRANSFER_SOURCE_DATE ON TB_TR_TRANSFER (SOURCE_BANK_ACCOUNT_ID, TRANSFER_DATE, STATE);

--------------------------------------------------------------------------------------------------
-- TB_TR_TRANSFER_EVENT — «گردش عملیات» یک انتقال. Insert-only، همان الگوی
-- TB_TR_PAYMENT_REQUEST_EVENT. Receipt معادلی ندارد (بدون زنجیرهٔ تأیید).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_TRANSFER_EVENT
(
    ID              CHAR(36)      NOT NULL,
    TRANSFER_ID     CHAR(36)      NOT NULL,
    ACTION          NUMBER(2)     NOT NULL,  -- TransferEventAction
    FROM_STATE      NUMBER(2),               -- TransferState، nullable (Create has no "from")
    TO_STATE        NUMBER(2),               -- TransferState
    NOTE            VARCHAR2(1000),
    CLIENT_IP       VARCHAR2(45),
    CREATEDDATE     TIMESTAMP     NOT NULL,
    UPDATEDDATE     TIMESTAMP,
    ADDUSERID       VARCHAR2(10)  NOT NULL,
    CHANGEUSERID    VARCHAR2(10),
    VAHEDCODE       VARCHAR2(4),
    YEAR            VARCHAR2(4),
    ISDELETED       NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_TRANSFER_EVENT PRIMARY KEY (ID),
    CONSTRAINT FK_TR_TRANSFER_EVENT_TRANSFER FOREIGN KEY (TRANSFER_ID)
        REFERENCES TB_TR_TRANSFER (ID)
);

-- Backs GET transfers/{id} (events)، ordered by CREATEDDATE.
CREATE INDEX IDX_TR_TRANSFER_EVENT_TRANSFER ON TB_TR_TRANSFER_EVENT (TRANSFER_ID);
