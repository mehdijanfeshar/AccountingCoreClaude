--------------------------------------------------------------------------------------------------
-- 074_month_close_log.sql
--
-- صورتحساب ماه (۲۰۲۶-۱۰-۰۷): ستاد اسناد «بررسی‌شده» یک ماه را برای گروهی از واحدها (درمانی/بیمه‌ای/
-- ستادی/همه) یا یک واحد «تأیید دائم» می‌کند. واحدی که در آن ماه سند یادداشت/موقت یا اعلامیهٔ صادرهٔ
-- ارسال‌نشده دارد کلاً صورتحساب نمی‌شود. هر ردیف = نتیجهٔ یک واحد در یک اجرا (BATCH_ID)، با دلیل؛
-- خود واحد هم لاگش را می‌بیند. بدون این اسکریپت فقط endpointهای api/month-close خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_MONTH_CLOSE_LOG
(
    ID                CHAR(36)        NOT NULL,
    BATCH_ID          CHAR(36)        NOT NULL,
    VAHEDCODE         VARCHAR2(4)     NOT NULL,
    YEAR              CHAR(4)         NOT NULL,
    MONTH             NUMBER(2)       NOT NULL,
    RESULT            NUMBER(1)       NOT NULL,
    ACCEPTED_COUNT    NUMBER(6)       DEFAULT 0 NOT NULL,
    PENDING_VOUCHERS  NUMBER(6)       DEFAULT 0 NOT NULL,
    PENDING_ELAMS     NUMBER(6)       DEFAULT 0 NOT NULL,
    REASON            VARCHAR2(1000),
    USERID            VARCHAR2(10)    NOT NULL,
    CREATEDDATE       TIMESTAMP       NOT NULL,
    CONSTRAINT PK_MONTH_CLOSE_LOG PRIMARY KEY (ID),
    CONSTRAINT CK_MONTH_CLOSE_LOG_MONTH CHECK (MONTH BETWEEN 1 AND 12),
    CONSTRAINT CK_MONTH_CLOSE_LOG_RESULT CHECK (RESULT IN (1, 2))
);

CREATE INDEX IDX_MONTH_CLOSE_LOG_UNIT ON TB_MONTH_CLOSE_LOG (VAHEDCODE, YEAR, MONTH);

COMMENT ON TABLE TB_MONTH_CLOSE_LOG IS 'لاگ صورتحساب ماه — RESULT: 1=صورتحساب شد، 2=رد شد (با دلیل)';
