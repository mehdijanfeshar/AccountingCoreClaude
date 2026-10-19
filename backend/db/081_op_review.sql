--------------------------------------------------------------------------------------------------
-- 081_op_review.sql
--
-- حسابیار فاز ۶۱ / صف بررسی (۲۰۲۶-۱۰-۱۸): تاریخچهٔ بررسی سندهای حسابیار. هر «بررسی شد»، «برگشت با دلیل»
-- و «ارسال دوباره» یک ردیف. وضعیت فعلی سند از TB_VOUCHERSHEAD.DOCLIFE می‌آید؛ این جدول فقط تاریخچه و پیام
-- برگشت را نگه می‌دارد. پیش‌نیاز: 067. بدون این اسکریپت: api/operations/reviews، api/operations/mine
-- و تغییر وضعیت سندِ دارای ردیف TB_OP_EXECUTION (ثبت تاریخچه) خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_REVIEW
(
    ID              CHAR(36)        NOT NULL,
    EXECUTION_ID    CHAR(36)        NOT NULL,
    VOUCHER_ID      CHAR(36)        NOT NULL,
    ACTION          NUMBER(1)       NOT NULL,              -- 1 بررسی شد، 2 برگشت داده شد، 3 ارسال دوباره
    REASON_CODE     VARCHAR2(30),
    MESSAGE         NVARCHAR2(500),
    REVIEWER_ID     VARCHAR2(10)    NOT NULL,              -- کد ملی (همان هویت ADDUSERID)
    VAHEDCODE       VARCHAR2(4)     NOT NULL,
    CREATEDDATE     TIMESTAMP       NOT NULL,
    CONSTRAINT PK_OP_REVIEW PRIMARY KEY (ID),
    CONSTRAINT FK_OP_REVIEW_EXECUTION FOREIGN KEY (EXECUTION_ID) REFERENCES TB_OP_EXECUTION (ID),
    CONSTRAINT FK_OP_REVIEW_VOUCHER FOREIGN KEY (VOUCHER_ID) REFERENCES TB_VOUCHERSHEAD (ID),
    CONSTRAINT CK_OP_REVIEW_ACTION CHECK (ACTION IN (1, 2, 3))
);

CREATE INDEX IX_OP_REVIEW_EXECUTION ON TB_OP_REVIEW (EXECUTION_ID, CREATEDDATE);
CREATE INDEX IX_OP_REVIEW_VAHED ON TB_OP_REVIEW (VAHEDCODE, CREATEDDATE);
