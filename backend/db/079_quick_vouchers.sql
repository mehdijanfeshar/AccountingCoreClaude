--------------------------------------------------------------------------------------------------
-- 079_quick_vouchers.sql
--
-- حسابیار / سندهای پرتکرار (۲۰۲۶-۱۰-۱۵): هر ردیف = یک الگوی عملیات + جواب‌های ثابت (طرف حساب، بانک،
-- شرح، ...) برای یک واحد. هنگام ثبت فقط پارامترهای ثابت‌نشده (معمولاً مبلغ) پرسیده می‌شود و تاریخ سند
-- همان روز است. سند را همان موتور الگو و مسیر Execute می‌سازد؛ این جدول فقط تعریف است.
-- حذف نرم با ISDELETED. پیش‌نیاز: 067. بدون این اسکریپت فقط endpointهای api/operations/quick خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_QUICK_VOUCHER
(
    ID              CHAR(36)        NOT NULL,
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    TEMPLATE_ID     CHAR(36)        NOT NULL,
    TITLE           VARCHAR2(200)   NOT NULL,
    VALUES_JSON     VARCHAR2(4000)  NOT NULL,
    ISDELETED       NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID       VARCHAR2(10)    NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_OP_QUICK_VOUCHER PRIMARY KEY (ID),
    CONSTRAINT FK_OP_QUICK_VOUCHER_TEMPLATE FOREIGN KEY (TEMPLATE_ID) REFERENCES TB_OP_TEMPLATE (ID),
    CONSTRAINT CK_OP_QUICK_VOUCHER_DELETED CHECK (ISDELETED IN (0, 1))
);

CREATE INDEX IX_OP_QUICK_VOUCHER_VAHED ON TB_OP_QUICK_VOUCHER (VAHEDCODE, ISDELETED);
