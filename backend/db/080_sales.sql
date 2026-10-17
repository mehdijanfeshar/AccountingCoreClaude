--------------------------------------------------------------------------------------------------
-- 080_sales.sql
--
-- ماژول فروش، فاز ۱ (۲۰۲۶-۱۰-۱۷) — طرح: docs/sales-module.md
-- تنظیمات و نگاشت حساب هر واحد، نرخ ارزش افزوده با تاریخ مؤثر، کالا/خدمت با موجودی ساده، پروفایل فروش
-- مشتری روی تفصیلی، نقش فروش کاربر، سند فروش (پیش‌فاکتور، سفارش، حواله، فاکتور، برگشت) و تاریخچه.
-- تاریخ‌ها شمسی yyyyMMdd (مثل DATE_DOC). مبلغ‌ها ریال صحیح. حذف نرم با ISDELETED.
-- بدون این اسکریپت فقط endpointهای api/sales خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_SL_SETTING
(
    VAHEDCODE               VARCHAR2(4)     NOT NULL,
    DISC_APPROVAL_PCT       NUMBER(5,2)     DEFAULT 10 NOT NULL,
    CREDIT_POLICY           NUMBER(1)       DEFAULT 3 NOT NULL,   -- 1 هشدار، 2 توقف، 3 ارجاع برای تأیید
    SOD                     NUMBER(1)       DEFAULT 1 NOT NULL,
    BILL_BEFORE_DELIVERY    NUMBER(1)       DEFAULT 0 NOT NULL,
    ALLOW_NEGATIVE_STOCK    NUMBER(1)       DEFAULT 0 NOT NULL,
    SYSTEM_TYPE_ID          CHAR(36),
    SELLER_ID               VARCHAR2(20),
    MEMORY_ID               VARCHAR2(20),
    ADDUSERID               VARCHAR2(10)    NOT NULL,
    CREATEDDATE             TIMESTAMP       NOT NULL,
    CONSTRAINT PK_SL_SETTING PRIMARY KEY (VAHEDCODE),
    CONSTRAINT CK_SL_SETTING_CREDIT CHECK (CREDIT_POLICY IN (1, 2, 3))
);

CREATE TABLE TB_SL_ACCOUNT_MAP
(
    ID              CHAR(36)        NOT NULL,
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    ROLE_KEY        VARCHAR2(20)    NOT NULL,
    ACCOUNT_ID      CHAR(36)        NOT NULL,
    TAFSILI_ID      CHAR(36),
    CONSTRAINT PK_SL_ACCOUNT_MAP PRIMARY KEY (ID),
    CONSTRAINT UK_SL_ACCOUNT_MAP UNIQUE (VAHEDCODE, ROLE_KEY),
    CONSTRAINT CK_SL_ACCOUNT_MAP_ROLE CHECK (ROLE_KEY IN ('ar', 'sales', 'service', 'disc', 'ret', 'vat', 'contract', 'cogs', 'inv'))
);

CREATE TABLE TB_SL_VAT_RATE
(
    ID              CHAR(36)        NOT NULL,
    FROM_DATE       VARCHAR2(8)     NOT NULL,
    RATE            NUMBER(5,2)     NOT NULL,
    NOTE            VARCHAR2(200),
    ISDELETED       NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_SL_VAT_RATE PRIMARY KEY (ID)
);

CREATE TABLE TB_SL_ITEM
(
    ID              CHAR(36)        NOT NULL,
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    CODE            VARCHAR2(30)    NOT NULL,
    NAME            VARCHAR2(200)   NOT NULL,
    KIND            NUMBER(1)       NOT NULL,                     -- 1 کالا، 2 خدمت
    GROUP_NAME      VARCHAR2(100),
    UNIT_NAME       VARCHAR2(30)    NOT NULL,
    PRICE           NUMBER(18)      DEFAULT 0 NOT NULL,
    STD_COST        NUMBER(18)      DEFAULT 0 NOT NULL,
    TAX_CODE        VARCHAR2(13),
    IS_EXEMPT       NUMBER(1)       DEFAULT 0 NOT NULL,
    STOCK           NUMBER(18,3)    DEFAULT 0 NOT NULL,
    ISACTIVE        NUMBER(1)       DEFAULT 1 NOT NULL,
    ISDELETED       NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_SL_ITEM PRIMARY KEY (ID),
    CONSTRAINT CK_SL_ITEM_KIND CHECK (KIND IN (1, 2))
);
CREATE UNIQUE INDEX UK_SL_ITEM_CODE ON TB_SL_ITEM (CASE WHEN ISDELETED = 0 THEN VAHEDCODE || '|' || CODE END);

CREATE TABLE TB_SL_CUSTOMER
(
    ID              CHAR(36)        NOT NULL,
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    TAFSILI_ID      CHAR(36)        NOT NULL,
    PERSON_KIND     NUMBER(1)       NOT NULL,                     -- 1 حقیقی، 2 حقوقی
    NATIONAL_ID     VARCHAR2(11),
    ECONOMIC_CODE   VARCHAR2(14),
    POSTAL_CODE     VARCHAR2(10),
    CREDIT_LIMIT    NUMBER(18)      DEFAULT 0 NOT NULL,
    PAY_TERMS       NUMBER(4)       DEFAULT 0 NOT NULL,
    IS_BLOCKED      NUMBER(1)       DEFAULT 0 NOT NULL,
    ISDELETED       NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_SL_CUSTOMER PRIMARY KEY (ID),
    CONSTRAINT CK_SL_CUSTOMER_KIND CHECK (PERSON_KIND IN (1, 2))
);
CREATE UNIQUE INDEX UK_SL_CUSTOMER_TAFSILI ON TB_SL_CUSTOMER (CASE WHEN ISDELETED = 0 THEN VAHEDCODE || '|' || TAFSILI_ID END);

CREATE TABLE TB_SL_USER_ROLE
(
    ID              CHAR(36)        NOT NULL,
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    USER_ID         VARCHAR2(10)    NOT NULL,
    ROLE            NUMBER(1)       NOT NULL,                     -- 1 کارشناس فروش، 2 مدیر فروش، 3 حسابدار
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_SL_USER_ROLE PRIMARY KEY (ID),
    CONSTRAINT UK_SL_USER_ROLE UNIQUE (VAHEDCODE, USER_ID, ROLE),
    CONSTRAINT CK_SL_USER_ROLE CHECK (ROLE IN (1, 2, 3))
);

CREATE TABLE TB_SL_DOC
(
    ID                  CHAR(36)        NOT NULL,
    VAHEDCODE           VARCHAR2(4)     NOT NULL,
    YEAR                VARCHAR2(4)     NOT NULL,
    DOC_TYPE            NUMBER(1)       NOT NULL,                 -- 1 پیش‌فاکتور، 2 سفارش، 3 حواله، 4 فاکتور، 5 برگشت
    DOC_NO              VARCHAR2(20),
    DOC_DATE            VARCHAR2(8)     NOT NULL,
    CUSTOMER_ID         CHAR(36)        NOT NULL,
    SOURCE_ID           CHAR(36),
    STATUS              NUMBER(1)       NOT NULL,                 -- 1 پیش‌نویس، 2 در انتظار تأیید، 3 تأییدشده، 4 قطعی، 9 باطل
    PAY_TERMS           NUMBER(4)       DEFAULT 0 NOT NULL,
    VALID_UNTIL         VARCHAR2(8),
    DELIVERY_DATE       VARCHAR2(8),
    SALES_TYPE          NUMBER(1)       DEFAULT 1 NOT NULL,       -- 1 داخلی، 2 صادراتی، 3 خدمات
    REF_NO              VARCHAR2(50),
    NOTE                VARCHAR2(1000),
    PRINT_NOTE          VARCHAR2(1000),
    HEADER_DISCOUNT     NUMBER(18)      DEFAULT 0 NOT NULL,
    GROSS               NUMBER(18)      DEFAULT 0 NOT NULL,
    DISCOUNT            NUMBER(18)      DEFAULT 0 NOT NULL,
    TAXABLE             NUMBER(18)      DEFAULT 0 NOT NULL,
    VAT                 NUMBER(18)      DEFAULT 0 NOT NULL,
    TOTAL               NUMBER(18)      DEFAULT 0 NOT NULL,
    APPROVAL_REASONS    VARCHAR2(1000),
    VOUCHER_ID          CHAR(36),
    APPROVED_BY         VARCHAR2(10),
    POSTED_BY           VARCHAR2(10),
    ISDELETED           NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID           VARCHAR2(10)    NOT NULL,
    CREATEDDATE         TIMESTAMP       NOT NULL,
    UPDATEDDATE         TIMESTAMP,
    CONSTRAINT PK_SL_DOC PRIMARY KEY (ID),
    CONSTRAINT FK_SL_DOC_CUSTOMER FOREIGN KEY (CUSTOMER_ID) REFERENCES TB_SL_CUSTOMER (ID),
    CONSTRAINT FK_SL_DOC_SOURCE FOREIGN KEY (SOURCE_ID) REFERENCES TB_SL_DOC (ID),
    CONSTRAINT CK_SL_DOC_TYPE CHECK (DOC_TYPE IN (1, 2, 3, 4, 5)),
    CONSTRAINT CK_SL_DOC_STATUS CHECK (STATUS IN (1, 2, 3, 4, 9))
);
-- شمارهٔ خالی (پیش از شماره‌گذاری) در ایندکس نمی‌آید.
CREATE UNIQUE INDEX UK_SL_DOC_NO ON TB_SL_DOC (CASE WHEN DOC_NO IS NOT NULL THEN VAHEDCODE || '|' || YEAR || '|' || DOC_TYPE || '|' || DOC_NO END);
CREATE INDEX IX_SL_DOC_VAHED ON TB_SL_DOC (VAHEDCODE, YEAR, DOC_TYPE, STATUS);
CREATE INDEX IX_SL_DOC_SOURCE ON TB_SL_DOC (SOURCE_ID);
CREATE INDEX IX_SL_DOC_CUSTOMER ON TB_SL_DOC (CUSTOMER_ID);

CREATE TABLE TB_SL_DOC_LINE
(
    ID              CHAR(36)        NOT NULL,
    DOC_ID          CHAR(36)        NOT NULL,
    RADIF           NUMBER(4)       NOT NULL,
    ITEM_ID         CHAR(36)        NOT NULL,
    QTY             NUMBER(18,3)    NOT NULL,
    PRICE           NUMBER(18)      NOT NULL,
    DISC_PCT        NUMBER(5,2)     DEFAULT 0 NOT NULL,
    GROSS           NUMBER(18)      NOT NULL,
    LINE_DISC       NUMBER(18)      NOT NULL,
    HEADER_DISC     NUMBER(18)      NOT NULL,
    TAXABLE         NUMBER(18)      NOT NULL,
    VAT_RATE        NUMBER(5,2)     NOT NULL,
    VAT             NUMBER(18)      NOT NULL,
    TOTAL           NUMBER(18)      NOT NULL,
    UNIT_COST       NUMBER(18)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_SL_DOC_LINE PRIMARY KEY (ID),
    CONSTRAINT FK_SL_DOC_LINE_DOC FOREIGN KEY (DOC_ID) REFERENCES TB_SL_DOC (ID),
    CONSTRAINT FK_SL_DOC_LINE_ITEM FOREIGN KEY (ITEM_ID) REFERENCES TB_SL_ITEM (ID)
);
CREATE INDEX IX_SL_DOC_LINE_DOC ON TB_SL_DOC_LINE (DOC_ID);

CREATE TABLE TB_SL_DOC_HISTORY
(
    ID              CHAR(36)        NOT NULL,
    DOC_ID          CHAR(36)        NOT NULL,
    ACTION          VARCHAR2(500)   NOT NULL,
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_SL_DOC_HISTORY PRIMARY KEY (ID),
    CONSTRAINT FK_SL_DOC_HISTORY_DOC FOREIGN KEY (DOC_ID) REFERENCES TB_SL_DOC (ID)
);
CREATE INDEX IX_SL_DOC_HISTORY_DOC ON TB_SL_DOC_HISTORY (DOC_ID);
