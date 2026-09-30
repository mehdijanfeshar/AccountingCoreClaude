--------------------------------------------------------------------------------------------------
-- 055_treasury_payment_request_patch.sql
--
-- وصله برای دیتابیس‌هایی که نسخهٔ قدیمی 051 (commit c589905) رویشان اجرا شده. اصلاح ۴-الف
-- (۲۰۲۶-۰۹-۲۹) فایل 051 را درجا عوض کرد، پس این دو تغییر روی آن دیتابیس‌ها نیامده:
--   (الف) ستون TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID  — بدونش GET/POST settings و هر خواندن
--        تنظیمات خزانه (سند شناسایی بدهی، دریافت، کارمزد) ORA-00904 می‌دهد.
--   (ب) جدول TB_TR_PAYMENT_REQUEST_LINK_TAFSILI — تفصیلی‌های چندسطحی مرکز هزینه.
-- ستون قدیمی TB_TR_PAYMENT_REQUEST.COST_CENTER_TAFSILI_ID (nullable) بی‌خطر است و عمداً حذف
-- نمی‌شود (کد دیگر نمی‌نویسدش).
--
-- اگر 051 جدید (پس از d4bc184) را اجرا کرده‌اید، این فایل را اجرا نکنید.
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

ALTER TABLE TB_TR_SETTING ADD (BENEFICIARY_TAFSIL_GROUP_ID CHAR(36));

CREATE TABLE TB_TR_PAYMENT_REQUEST_LINK_TAFSILI
(
    ID                   CHAR(36)      NOT NULL,
    PAYMENT_REQUEST_ID   CHAR(36)      NOT NULL,
    TAFSILI_ID           CHAR(36)      NOT NULL,
    LEVEL_ID             CHAR(36)      NOT NULL,
    CREATEDDATE          TIMESTAMP     NOT NULL,
    UPDATEDDATE          TIMESTAMP,
    ADDUSERID            VARCHAR2(10)  NOT NULL,
    CHANGEUSERID         VARCHAR2(10),
    VAHEDCODE            VARCHAR2(4),
    YEAR                 VARCHAR2(4),
    ISDELETED            NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_TR_PAYREQ_LINK_TAFSILI PRIMARY KEY (ID),
    CONSTRAINT FK_TR_PAYREQ_LINK_TAF_PAYREQ FOREIGN KEY (PAYMENT_REQUEST_ID)
        REFERENCES TB_TR_PAYMENT_REQUEST (ID),
    CONSTRAINT FK_TR_PAYREQ_LINK_TAF_TAF FOREIGN KEY (TAFSILI_ID)
        REFERENCES TB_TAFSILI (ID),
    CONSTRAINT FK_TR_PAYREQ_LINK_TAF_LVL FOREIGN KEY (LEVEL_ID)
        REFERENCES TB_LEVEL_TAFSIL (ID)
);

-- Backs reconcile (Update، full-replace) و GET payment-requests/{id}.
CREATE INDEX IDX_TR_PAYREQ_LINK_TAF_PAYREQ ON TB_TR_PAYMENT_REQUEST_LINK_TAFSILI (PAYMENT_REQUEST_ID);
