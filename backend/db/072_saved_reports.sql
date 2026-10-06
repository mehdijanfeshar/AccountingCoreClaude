--------------------------------------------------------------------------------------------------
-- 072_saved_reports.sql
--
-- Agent-UX / گزارش‌ساز حسابیار (۲۰۲۶-۱۰-۰۶): گزارش‌های ذخیره‌شده. هر ردیف = یکی از گزارش‌های موجود
-- (تراز آزمایشی، ماتریسی، مرور حساب‌ها، مرور اسناد) با تنظیمات ثابت + فهرست پارامترهایی که هر بار از
-- کاربر پرسیده می‌شود. فقط تعریف است؛ عددها را همان گزارش‌های فعلی می‌سازند. ثبت/ویرایش از خود برنامه
-- (مدیر ستاد)؛ این اسکریپت فقط یک بار اجرا می‌شود. پیش‌نیاز: 067.
-- بدون این اسکریپت فقط endpointهای api/operations/reports خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_SAVED_REPORT
(
    ID                   CHAR(36)        NOT NULL,
    CODE                 VARCHAR2(50)    NOT NULL,
    TITLE                VARCHAR2(200)   NOT NULL,
    DESCRIPTION          VARCHAR2(1000),
    KEYWORDS             VARCHAR2(1000),
    REPORT_KIND          VARCHAR2(40)    NOT NULL,
    SETTINGS_JSON        VARCHAR2(4000)  NOT NULL,
    ALLOWED_VAHED_TYPES  VARCHAR2(400),
    ISACTIVE             NUMBER(1)       DEFAULT 1 NOT NULL,
    ADDUSERID            VARCHAR2(10)    NOT NULL,
    CREATEDDATE          TIMESTAMP       NOT NULL,
    CONSTRAINT PK_OP_SAVED_REPORT PRIMARY KEY (ID),
    CONSTRAINT UK_OP_SAVED_REPORT_CODE UNIQUE (CODE),
    CONSTRAINT CK_OP_SAVED_REPORT_KIND CHECK (REPORT_KIND IN ('trial-balance', 'matrix', 'account-review', 'voucher-review')),
    CONSTRAINT CK_OP_SAVED_REPORT_ACTIVE CHECK (ISACTIVE IN (0, 1))
);
