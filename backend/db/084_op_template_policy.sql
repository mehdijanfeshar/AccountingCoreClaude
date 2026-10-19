--------------------------------------------------------------------------------------------------
-- 084_op_template_policy.sql
--
-- حسابیار فاز ۶۲ (۲۰۲۶-۱۰-۱۸): سیاست الگو (G7)، سؤال انتخابی و ردیف شرطی (G8)، مالیات تاریخ‌دار (G9)،
-- ریسک و هشدار روی اجرا (G7/G10)، جملهٔ کاربر و منبع جواب‌ها، پاسخ به حسابدار و «موضوع را بفرستید».
-- پیش‌نیاز: 067، 081، 082، 083 و (برای مالیات تاریخ‌دار) 080. همهٔ ستون‌های جدید nullable یا دارای پیش‌فرض‌اند؛
-- ردیف‌های قدیمی بدون تغییر کار می‌کنند. بدون این اسکریپت ذخیرهٔ الگو/اجرا و صف بررسی خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

-- ── G7: سیاست الگو ──
ALTER TABLE TB_OP_TEMPLATE ADD
(
    PLAIN_PATTERN       NVARCHAR2(1000),                       -- جملهٔ ساده؛ همان جایگزینی {key} شرح سند
    MAX_AMOUNT          NUMBER(18),                            -- بالاتر از این مبلغ: ریسک بالا + تأیید مدیر مالی
    REQUIRE_ATTACHMENT  NUMBER(1)      DEFAULT 0 NOT NULL,     -- مدرک الزامی
    RISK_BASE           NUMBER(1),                             -- 1 پایین، 2 متوسط، 3 بالا (خالی = پایین)
    CATEGORY            NUMBER(1)                              -- 1 هزینه، 2 پرداخت، 3 دریافت، 4 خرید، 5 تنخواه، 6 فروش، 9 سایر
);

ALTER TABLE TB_OP_TEMPLATE ADD CONSTRAINT CK_OP_TEMPLATE_REQ_ATT CHECK (REQUIRE_ATTACHMENT IN (0, 1));
ALTER TABLE TB_OP_TEMPLATE ADD CONSTRAINT CK_OP_TEMPLATE_RISK CHECK (RISK_BASE IS NULL OR RISK_BASE IN (1, 2, 3));
ALTER TABLE TB_OP_TEMPLATE ADD CONSTRAINT CK_OP_TEMPLATE_CATEGORY CHECK (CATEGORY IS NULL OR CATEGORY IN (1, 2, 3, 4, 5, 6, 9));
ALTER TABLE TB_OP_TEMPLATE ADD CONSTRAINT CK_OP_TEMPLATE_MAX_AMOUNT CHECK (MAX_AMOUNT IS NULL OR MAX_AMOUNT > 0);

-- ── G8: سؤال انتخابی (PARAM_TYPE = 5) و ردیف شرطی ──
ALTER TABLE TB_OP_TEMPLATE_PARAM ADD (OPTIONS_JSON NVARCHAR2(2000));   -- [{"value":"yes","label":"بله"},...]

ALTER TABLE TB_OP_TEMPLATE_PARAM DROP CONSTRAINT CK_OP_PARAM_TYPE;
ALTER TABLE TB_OP_TEMPLATE_PARAM ADD CONSTRAINT CK_OP_PARAM_TYPE CHECK (PARAM_TYPE IN (1, 2, 3, 4, 5));
-- CK_OP_PARAM_GROUP (نوع ۲ ⇔ گروه تفصیلی) برای نوع ۵ بدون تغییر درست است.

ALTER TABLE TB_OP_TEMPLATE_LINE ADD
(
    CONDITION_PARAM_KEY  VARCHAR2(40),                         -- کلید سؤال انتخابی
    CONDITION_VALUE      VARCHAR2(30),                         -- ردیف فقط وقتی جواب برابر این مقدار باشد ساخته می‌شود
    AMOUNT_SOURCE        NUMBER(1)     DEFAULT 1 NOT NULL      -- G9: 1 پارامتر (پیش‌فرض)، 2 ارزش افزودهٔ پارامتر در تاریخ سند
);

ALTER TABLE TB_OP_TEMPLATE_LINE ADD CONSTRAINT CK_OP_LINE_AMOUNT_SOURCE CHECK (AMOUNT_SOURCE IN (1, 2));
ALTER TABLE TB_OP_TEMPLATE_LINE ADD CONSTRAINT CK_OP_LINE_CONDITION
    CHECK ((CONDITION_PARAM_KEY IS NULL AND CONDITION_VALUE IS NULL)
        OR (CONDITION_PARAM_KEY IS NOT NULL AND CONDITION_VALUE IS NOT NULL));

-- ── G7/G10: ریسک، هشدار، جملهٔ کاربر و منبع جواب‌ها روی اجرا ──
ALTER TABLE TB_OP_EXECUTION ADD
(
    USER_SENTENCE        NVARCHAR2(1000),                      -- جملهٔ کامل کاربر (فقط هنگام ارسال به هوش مصنوعی بیرونی پوشانده می‌شود)
    ANSWER_SOURCES_JSON  VARCHAR2(2000),                       -- {"amount":"sentence","customer":"ai",...}
    AI_CONFIRMED         NUMBER(1),                            -- کاربر جواب‌های هوش مصنوعی را صریحاً تأیید کرد؟
    RISK_LEVEL           NUMBER(1),                            -- 1 پایین، 2 متوسط، 3 بالا
    RISK_REASONS_JSON    NVARCHAR2(2000),
    WARNINGS_JSON        NVARCHAR2(2000),
    TOTAL_AMOUNT         NUMBER(18)                            -- جمع بدهکار سند؛ مبنای میانهٔ ناهنجاری و تکرار محتوایی
);

ALTER TABLE TB_OP_EXECUTION ADD CONSTRAINT CK_OP_EXEC_RISK CHECK (RISK_LEVEL IS NULL OR RISK_LEVEL IN (1, 2, 3));
ALTER TABLE TB_OP_EXECUTION ADD CONSTRAINT CK_OP_EXEC_AI_CONFIRMED CHECK (AI_CONFIRMED IS NULL OR AI_CONFIRMED IN (0, 1));

CREATE INDEX IX_OP_EXEC_TEMPLATE ON TB_OP_EXECUTION (TEMPLATE_ID, VAHEDCODE, CREATEDDATE);
CREATE INDEX IX_OP_EXEC_RISK ON TB_OP_EXECUTION (VAHEDCODE, RISK_LEVEL);

-- ── «پاسخ به حسابدار»: عمل چهارم تاریخچهٔ بررسی ──
ALTER TABLE TB_OP_REVIEW DROP CONSTRAINT CK_OP_REVIEW_ACTION;
ALTER TABLE TB_OP_REVIEW ADD CONSTRAINT CK_OP_REVIEW_ACTION CHECK (ACTION IN (1, 2, 3, 4));   -- 4 = پاسخ ثبت‌کننده

-- ── «موضوع را برای حسابدار بفرستید» ──
CREATE TABLE TB_OP_REQUEST
(
    ID           CHAR(36)        NOT NULL,
    VAHEDCODE    VARCHAR2(4)     NOT NULL,
    SENTENCE     NVARCHAR2(1000) NOT NULL,
    ADDUSERID    VARCHAR2(10)    NOT NULL,                      -- کد ملی فرستنده
    CREATEDDATE  TIMESTAMP       NOT NULL,
    STATUS       NUMBER(1)       DEFAULT 1 NOT NULL,            -- 1 باز، 2 انجام شد
    HANDLED_BY   VARCHAR2(10),
    HANDLED_AT   TIMESTAMP,
    NOTE         NVARCHAR2(500),
    CONSTRAINT PK_OP_REQUEST PRIMARY KEY (ID),
    CONSTRAINT CK_OP_REQUEST_STATUS CHECK (STATUS IN (1, 2))
);

CREATE INDEX IX_OP_REQUEST_VAHED ON TB_OP_REQUEST (VAHEDCODE, STATUS, CREATEDDATE);
