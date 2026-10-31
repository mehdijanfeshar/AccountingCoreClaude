--------------------------------------------------------------------------------------------------
-- 090_accrual_schedule.sql
--
-- حسابداری تعهدی، فاز ۶۶ (۲۰۲۶-۱۰-۳۰) — طرح: docs/accrual-phase66-task.md
-- تنها جدول جدید ماژول: زمان‌بندی تعهدی. هر سطر = سندی که در DUE_DATE ساخته می‌شود:
--   KIND 1 برگشت برآورد (روز اول ماه بعد؛ برآورد اسفند برگشت خودکار ندارد)،
--   KIND 2/3 سهم یک ماه از پیش‌پرداخت / درآمد انتقالی (آخر هر ماه).
-- سند از مسیر سند خودکار موجود («موقت»، ISAUTOMATIC=1) ساخته می‌شود؛ یکتایی (SOURCE_VOUCHER_ID, KIND, LINE_NO) آخرین
-- سد اجرای دوباره است. تاریخ‌ها شمسی yyyyMMdd. مبلغ ریال صحیح. حذف نرم با ISDELETED.
-- دو ستون اختیاری الگوی حسابیار؛ الگوهای قبلی بدون تغییر کار می‌کنند.
-- بدون این اسکریپت: api/accrual خطا می‌دهد و اجرا/ذخیره/خواندن الگوی حسابیار هم ORA-00904 می‌گیرد
-- (ستون‌های AUTO_REVERSE و SCHEDULE_KIND در نگاشت هستند).
-- دسترسی منو در کد است (MenuCatalog: /accrual و /accrual/schedules، ماژول Accrual)؛ DML جدا لازم نیست.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_AC_SCHEDULE
(
    ID                  CHAR(36)        NOT NULL,
    VAHEDCODE           VARCHAR2(4)     NOT NULL,
    YEAR                VARCHAR2(4)     NOT NULL,
    SOURCE_VOUCHER_ID   CHAR(36)        NOT NULL,
    EXECUTION_ID        CHAR(36),
    KIND                NUMBER(1)       NOT NULL,
    LINE_NO             NUMBER(3)       NOT NULL,
    DUE_DATE            VARCHAR2(8)     NOT NULL,
    AMOUNT              NUMBER(18)      NOT NULL,
    DEBIT_ACCOUNT_ID    CHAR(36),
    CREDIT_ACCOUNT_ID   CHAR(36),
    TAFSILI_JSON        CLOB,
    DESCRIPTION         VARCHAR2(500),
    STATE               NUMBER(1)       DEFAULT 1 NOT NULL,
    RESULT_VOUCHER_ID   CHAR(36),
    ISDELETED           NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID           VARCHAR2(10)    NOT NULL,
    CREATEDDATE         TIMESTAMP       NOT NULL,
    CHANGEUSERID        VARCHAR2(10),
    UPDATEDDATE         TIMESTAMP,
    CONSTRAINT PK_AC_SCHEDULE PRIMARY KEY (ID),
    CONSTRAINT UK_AC_SCHEDULE_LINE UNIQUE (SOURCE_VOUCHER_ID, KIND, LINE_NO),
    CONSTRAINT CK_AC_SCHEDULE_KIND CHECK (KIND IN (1, 2, 3)),
    CONSTRAINT CK_AC_SCHEDULE_STATE CHECK (STATE IN (1, 2, 3)),
    CONSTRAINT CK_AC_SCHEDULE_AMT CHECK (AMOUNT > 0)
);

CREATE INDEX IX_AC_SCHEDULE_DUE ON TB_AC_SCHEDULE (VAHEDCODE, STATE, DUE_DATE);

COMMENT ON TABLE TB_AC_SCHEDULE IS 'زمان‌بندی حسابداری تعهدی: برگشت برآورد و سهم ماهانهٔ پیش‌پرداخت/درآمد انتقالی';
COMMENT ON COLUMN TB_AC_SCHEDULE.SOURCE_VOUCHER_ID IS 'TB_VOUCHERSHEAD.ID سند اصلی';
COMMENT ON COLUMN TB_AC_SCHEDULE.EXECUTION_ID IS 'TB_OP_EXECUTION.ID اجرای حسابیار';
COMMENT ON COLUMN TB_AC_SCHEDULE.KIND IS '1 برگشت برآورد، 2 پیش‌پرداخت، 3 درآمد انتقالی';
COMMENT ON COLUMN TB_AC_SCHEDULE.DUE_DATE IS 'سررسید شمسی yyyyMMdd = تاریخ سند ساخته‌شده';
COMMENT ON COLUMN TB_AC_SCHEDULE.TAFSILI_JSON IS 'تفصیلی دو طرف سهم ماهانه: {debit:[{level,levelId,detailId,title}],credit:[...]}';
COMMENT ON COLUMN TB_AC_SCHEDULE.STATE IS '1 باز، 2 اجراشده، 3 لغو';
COMMENT ON COLUMN TB_AC_SCHEDULE.RESULT_VOUCHER_ID IS 'TB_VOUCHERSHEAD.ID سند ساخته‌شده';

ALTER TABLE TB_OP_TEMPLATE ADD (
    AUTO_REVERSE    NUMBER(1) DEFAULT 0 NOT NULL,
    SCHEDULE_KIND   NUMBER(1),
    CONSTRAINT CK_OP_TEMPLATE_SCHED CHECK (SCHEDULE_KIND IS NULL OR SCHEDULE_KIND IN (2, 3))
);

COMMENT ON COLUMN TB_OP_TEMPLATE.AUTO_REVERSE IS 'سند این الگو روز اول ماه بعد خودکار برگردانده شود (برآورد)';
COMMENT ON COLUMN TB_OP_TEMPLATE.SCHEDULE_KIND IS 'تقسیم چندماهه: null ندارد، 2 پیش‌پرداخت، 3 درآمد انتقالی';
