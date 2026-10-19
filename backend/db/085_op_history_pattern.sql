--------------------------------------------------------------------------------------------------
-- 085_op_history_pattern.sql
--
-- حسابیار فاز ۶۳ (۲۰۲۶-۱۰-۱۸): یادگیری از سوابق. وقتی جملهٔ کاربر با هیچ الگوی ستاد جور نشود، حسابیار از
-- سندهای دائم/بررسی‌شدهٔ سال‌های قبل که ساختار حساب مشابه دارند پیش‌نویس می‌سازد (همیشه ریسک بالا و صف حسابدار)؛
-- و مدیر ستاد الگوهای پرتکرار همهٔ واحدها را می‌بیند و به الگو تبدیل می‌کند (G13).
-- جدول‌ها محاسبه‌شده‌اند: «بازسازی» (POST api/operations/history/patterns/rebuild) کل محتوا را در یک تراکنش
-- جایگزین می‌کند. پیش‌نیاز: 067 (TB_OP_EXECUTION). همهٔ ستون‌ها (غیر از کلیدها و SIGNATURE) nullable‌اند.
-- بدون این اسکریپت همهٔ endpointهای history پاسخ ۵۰۳ «جدول سوابق ساخته نشده» می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_HISTORY_PATTERN
(
    ID               CHAR(36)        NOT NULL,
    SIGNATURE        VARCHAR2(1000)  NOT NULL,   -- «moinId:side|moinId:side» مرتب‌شده؛ side 1 بدهکار، 2 بستانکار
    LINES_JSON       CLOB,                       -- [{moinId, side, share, levels:[{level, levelId, groupIds[]}]}]
    VOUCHER_COUNT    NUMBER(10),
    UNIT_COUNT       NUMBER(10),
    YEARS            VARCHAR2(20),               -- مثل 1401-1404
    FROM_YEAR        NUMBER(4),                  -- برای فیلتر بازهٔ سال
    TO_YEAR          NUMBER(4),
    MEDIAN_AMOUNT    NUMBER(18),
    LAST_DATE        VARCHAR2(8),                -- yyyyMMdd شمسی
    KEYWORDS         NVARCHAR2(2000),            -- [{"w":"دارو","c":12},...]
    SAMPLE_DESCS_JSON NVARCHAR2(2000),           -- حداکثر ۵ شرح نمونه
    VAHED_TYPES      VARCHAR2(400),              -- کد نوع واحدها با «,» جدا
    REBUILT_AT       TIMESTAMP,
    CONSTRAINT PK_OP_HISTORY_PATTERN PRIMARY KEY (ID),
    CONSTRAINT UK_OP_HISTORY_PATTERN_SIG UNIQUE (SIGNATURE)
);

CREATE INDEX IX_OP_HISTORY_PATTERN_COUNT ON TB_OP_HISTORY_PATTERN (VOUCHER_COUNT);
CREATE INDEX IX_OP_HISTORY_PATTERN_YEARS ON TB_OP_HISTORY_PATTERN (FROM_YEAR, TO_YEAR);

CREATE TABLE TB_OP_HISTORY_PATTERN_UNIT
(
    PATTERN_ID       CHAR(36)        NOT NULL,
    VAHEDCODE        VARCHAR2(4)     NOT NULL,
    VOUCHER_COUNT    NUMBER(10),
    KEYWORDS         NVARCHAR2(1000),            -- کلمه‌های پرتکرار شرح سندهای همین واحد
    CONSTRAINT PK_OP_HISTORY_PATTERN_UNIT PRIMARY KEY (PATTERN_ID, VAHEDCODE),
    CONSTRAINT FK_OP_HISTORY_UNIT_PATTERN FOREIGN KEY (PATTERN_ID)
        REFERENCES TB_OP_HISTORY_PATTERN (ID) ON DELETE CASCADE
);

-- تطبیق جمله فقط ردیف‌های واحد کاربر را می‌خواند
CREATE INDEX IX_OP_HISTORY_UNIT_VAHED ON TB_OP_HISTORY_PATTERN_UNIT (VAHEDCODE, PATTERN_ID);

-- اجرای مبتنی بر سابقه TEMPLATE_ID خالی، TEMPLATE_CODE = 'HISTORY:<patternId>' و CHANNEL = 'History' دارد؛
-- ستون‌های TB_OP_EXECUTION نیازی به تغییر ندارند (TEMPLATE_ID از قبل nullable، CHANNEL بدون CHECK، TEMPLATE_CODE 50 نویسه).
