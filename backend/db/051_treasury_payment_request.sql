--------------------------------------------------------------------------------------------------
-- 051_treasury_payment_request.sql
--
-- خزانه‌داری ("تنخواه و خزانه‌داری")، بخش ۴-الف (۲۰۲۶-۰۹-۲۸ owner decision): درخواست پرداخت +
-- کارتابل تأیید. چهار جدول جانبی کاملاً جدید، به همان استثنای صریح صاحب پروژه که فاز ۴۴ (تنخواه)
-- استفاده کرد (docs/tankhah-khazaneh-module.md §۰/§۳/§۱۰) — «no new tables» به‌جز این ماژول.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- NOT executed anywhere as of authoring time (2026-09-29). Same standing rule as every
-- 044-050_petty_cash*.sql script.
--
-- Naming: prefix TB_TR_ ("Treasury" / خزانه‌داری) — a fresh prefix, deliberately NOT TB_PC_,
-- because these tables are not petty-cash-specific: TB_TR_ROLE is its own separate role list
-- (independent of TB_PC_REVIEWER) and TB_TR_PAYMENT_REQUEST has nothing to do with a تنخواه fund.
--
-- Design reference: docs/tankhah-khazaneh-module.md §۱۰ (خزانه‌داری), بخش ۴-الف. Read that
-- section before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_TR_SETTING — تنظیمات خزانه‌داری یک واحد. یک ردیف به‌ازای VAHEDCODE (UNIQUE). هیچ مقدار
-- پیش‌فرض ندارد — تا وقتی مدیر مالی تعریف نکند این ردیف اصلاً وجود ندارد (صاحب پروژه، ۲۰۲۶-۰۹-۲۸؛
-- برخلاف TB_PC_FUND.FINANCE_MANAGER_APPROVAL_LIMIT که یک پیش‌فرض hardcode دارد).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_SETTING
(
    ID                       CHAR(36)      NOT NULL,
    VAHEDCODE                VARCHAR2(4)   NOT NULL,
    CEO_APPROVAL_THRESHOLD   NUMBER(25)    NOT NULL,
    BULK_APPROVE_LIMIT       NUMBER(25)    NOT NULL,
    CREATEDDATE              TIMESTAMP     NOT NULL,
    UPDATEDDATE              TIMESTAMP,
    ADDUSERID                VARCHAR2(10)  NOT NULL,
    CHANGEUSERID             VARCHAR2(10),
    ISDELETED                NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_SETTING PRIMARY KEY (ID),
    CONSTRAINT UK_TR_SETTING UNIQUE (VAHEDCODE)
);

--------------------------------------------------------------------------------------------------
-- TB_TR_ROLE — نقش خزانه‌داری یک کاربر در یک واحد. فهرست کاملاً جدا و مستقل از TB_PC_REVIEWER
-- (صاحب پروژه، ۲۰۲۶-۰۹-۲۸). یک کاربر می‌تواند بیش از یک نقش در همان واحد داشته باشد.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_ROLE
(
    ID            CHAR(36)      NOT NULL,
    VAHEDCODE     VARCHAR2(4)   NOT NULL,
    USERID        VARCHAR2(10)  NOT NULL,
    USER_NAME     VARCHAR2(200),
    ROLE          NUMBER(2)     NOT NULL,  -- TreasuryRole: 1=UnitManager,2=FinanceManager,3=Ceo,4=SeniorAccountant,5=Treasurer
    CREATEDDATE   TIMESTAMP     NOT NULL,
    UPDATEDDATE   TIMESTAMP,
    ADDUSERID     VARCHAR2(10)  NOT NULL,
    CHANGEUSERID  VARCHAR2(10),
    YEAR          VARCHAR2(4),
    ISDELETED     NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_ROLE PRIMARY KEY (ID),
    CONSTRAINT UK_TR_ROLE UNIQUE (VAHEDCODE, USERID, ROLE)
);

CREATE INDEX IDX_TR_ROLE_VAHED ON TB_TR_ROLE (VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- TB_TR_PAYMENT_REQUEST — «درخواست پرداخت» (PAY-xxxxxx). جدول جانبی کاملاً جدید؛ Legacy
-- TB_PAYRECIVHEAD/DETAIL فقط در بخش ۴-ب (اجرای پرداخت) پر می‌شود — PAYRECIVHEAD_ID تا آن زمان
-- NULL می‌ماند. هیچ ستون شناسه‌ای اینجا FK واقعی روی TB_ACCOUNTCODE/TB_ACCOUNT/TB_TAFSILI ندارد؛
-- کنترل وجودشان سمت Application انجام می‌شود (همان الگوی ریسک باز #۹/#۱۴ — «ستون‌های شناسهٔ بدون
-- FK» — عمداً تکرار شده تا این جدول جدید هم به همان دسته اضافه شود، نه یک استثنای جدید).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_PAYMENT_REQUEST
(
    ID                             CHAR(36)      NOT NULL,
    CODE                           VARCHAR2(20)  NOT NULL,   -- "PAY-" + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)
    BENEFICIARY_NAME               VARCHAR2(200) NOT NULL,
    BENEFICIARY_NATIONAL_ID        VARCHAR2(11),
    BENEFICIARY_TAFSILI_ID         CHAR(36),
    PAYMENT_TYPE                   NUMBER(1)     NOT NULL,   -- TreasuryPaymentType: 1=تأمین‌کننده/پیمانکار,2=کارمند,3=سایر
    INVOICE_REF                    VARCHAR2(100),
    INVOICE_APPROVED               NUMBER(1)     DEFAULT 0 NOT NULL,
    EXPENSE_ACCOUNT_ID             CHAR(36)      NOT NULL,   -- TB_ACCOUNTCODE.ID، بدون FK واقعی (بالا را ببین)
    COST_CENTER_TAFSILI_ID         CHAR(36),
    AMOUNT_BEFORE_TAX              NUMBER(25)    NOT NULL,
    VAT_PERCENT                    NUMBER(5, 2),
    VAT_AMOUNT                     NUMBER(25)    NOT NULL,
    INSURANCE_DEDUCTION_PERCENT    NUMBER(5, 2),
    INSURANCE_DEDUCTION_AMOUNT     NUMBER(25)    DEFAULT 0 NOT NULL,
    NET_PAYABLE_AMOUNT             NUMBER(25)    NOT NULL,   -- سرور: AMOUNT_BEFORE_TAX + VAT_AMOUNT - INSURANCE_DEDUCTION_AMOUNT
    DUE_DATE                       VARCHAR2(8)   NOT NULL,   -- YYYYMMDD شمسی
    PAYMENT_ACCOUNT_ID             CHAR(36)      NOT NULL,   -- TB_ACCOUNT.ID، بدون FK واقعی (بالا را ببین)
    PAYMENT_METHOD                 NUMBER(1),                -- TreasuryPaymentMethod: 1=ساتنا,2=پایا,3=چک,4=نقد
    DESCRIPTION                    VARCHAR2(1000),
    REQUEST_STATE                  NUMBER(2)     NOT NULL,   -- PaymentRequestState
    SUBMITTED_DATE                 TIMESTAMP,
    PAYRECIVHEAD_ID                CHAR(36),                 -- بخش ۴-ب پر می‌کند
    CREATEDDATE                    TIMESTAMP     NOT NULL,
    UPDATEDDATE                    TIMESTAMP,
    ADDUSERID                      VARCHAR2(10)  NOT NULL,
    CHANGEUSERID                   VARCHAR2(10),
    VAHEDCODE                      VARCHAR2(4),
    YEAR                           VARCHAR2(4),
    ISDELETED                      NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_PAYMENT_REQUEST PRIMARY KEY (ID)
);

-- Backs GET payment-requests?state=&pageNumber=&pageSize= و stateCounts.
CREATE INDEX IDX_TR_PAYMENT_REQUEST_STATE ON TB_TR_PAYMENT_REQUEST (VAHEDCODE, YEAR, REQUEST_STATE);

-- Backs کارتابل تأیید (هر واحد، هر وضعیت Pending*).
CREATE INDEX IDX_TR_PAYMENT_REQUEST_VAHED ON TB_TR_PAYMENT_REQUEST (VAHEDCODE);

-- Backs تولید CODE بعدی (شمارندهٔ (VAHEDCODE, YEAR)) — همان الگوی IDX_CHARGEANDCOST_HEAD_CODE.
CREATE INDEX IDX_TR_PAYMENT_REQUEST_CODE ON TB_TR_PAYMENT_REQUEST (VAHEDCODE, YEAR);

--------------------------------------------------------------------------------------------------
-- TB_TR_PAYMENT_REQUEST_EVENT — «گردش عملیات» یک درخواست پرداخت. Insert-only، همان الگوی
-- TB_PC_DOC_EVENT. هر تغییر وضعیت یک ردیف اینجا، در همان تراکنش.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_TR_PAYMENT_REQUEST_EVENT
(
    ID                   CHAR(36)      NOT NULL,
    PAYMENT_REQUEST_ID   CHAR(36)      NOT NULL,
    ACTION               NUMBER(2)     NOT NULL,  -- PaymentRequestEventAction
    FROM_STATE           NUMBER(2),               -- PaymentRequestState، nullable (Create has no "from")
    TO_STATE             NUMBER(2),               -- PaymentRequestState
    NOTE                 VARCHAR2(1000),
    CLIENT_IP            VARCHAR2(45),
    CREATEDDATE          TIMESTAMP     NOT NULL,
    UPDATEDDATE          TIMESTAMP,
    ADDUSERID            VARCHAR2(10)  NOT NULL,
    CHANGEUSERID         VARCHAR2(10),
    VAHEDCODE            VARCHAR2(4),
    YEAR                 VARCHAR2(4),
    ISDELETED            NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_PAYMENT_REQUEST_EVENT PRIMARY KEY (ID),
    CONSTRAINT FK_TR_PAYREQ_EVENT_PAYREQ FOREIGN KEY (PAYMENT_REQUEST_ID)
        REFERENCES TB_TR_PAYMENT_REQUEST (ID)
);

-- Backs GET payment-requests/{id} (events)، ordered by CREATEDDATE.
CREATE INDEX IDX_TR_PAYREQ_EVENT_PAYREQ ON TB_TR_PAYMENT_REQUEST_EVENT (PAYMENT_REQUEST_ID);
