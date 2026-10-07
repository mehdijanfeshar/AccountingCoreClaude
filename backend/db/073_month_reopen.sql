--------------------------------------------------------------------------------------------------
-- 073_month_reopen.sql
--
-- برگشت صورتحساب ماه (۲۰۲۶-۱۰-۰۷): ماهی که اسنادش «تأیید دائم» شده صورتحساب‌شده است و اسنادش
-- قابل اصلاح نیست، مگر با مجوز ستاد. ستاد برای (واحد، سال، ماه) رمز برگشت صادر می‌کند؛ واحد با
-- واردکردن رمز، اسناد تأیید دائم همان ماه را به «بررسی‌شده» برمی‌گرداند. رمز با الگوریتم (HMAC روی
-- واحد/سال/ماه/شمارهٔ دفعه + کلید محرمانهٔ MonthReopen:Secret) ساخته و در سمت برگشت دوباره محاسبه
-- می‌شود؛ هر رمز یک‌بار مصرف است و پس از ۵ ورود نادرست باطل می‌شود. هر ردیف = یک رمز صادرشده.
-- بدون این اسکریپت فقط endpointهای api/month-reopen خطا می‌دهند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_MONTH_REOPEN
(
    ID               CHAR(36)        NOT NULL,
    VAHEDCODE        VARCHAR2(4)     NOT NULL,
    YEAR             CHAR(4)         NOT NULL,
    MONTH            NUMBER(2)       NOT NULL,
    SEQ              NUMBER(4)       NOT NULL,
    REASON           VARCHAR2(1000),
    ISSUEDBY         VARCHAR2(10)    NOT NULL,
    ISSUEDDATE       TIMESTAMP       NOT NULL,
    FAILED_ATTEMPTS  NUMBER(2)       DEFAULT 0 NOT NULL,
    USEDBY           VARCHAR2(10),
    USEDDATE         TIMESTAMP,
    REVERTED_COUNT   NUMBER(6),
    CONSTRAINT PK_MONTH_REOPEN PRIMARY KEY (ID),
    CONSTRAINT UK_MONTH_REOPEN_SEQ UNIQUE (VAHEDCODE, YEAR, MONTH, SEQ),
    CONSTRAINT CK_MONTH_REOPEN_MONTH CHECK (MONTH BETWEEN 1 AND 12)
);

COMMENT ON TABLE TB_MONTH_REOPEN IS 'رمزهای برگشت صورتحساب ماه (صادره توسط ستاد) و مصرف آن‌ها';
