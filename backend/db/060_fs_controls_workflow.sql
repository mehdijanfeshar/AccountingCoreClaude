--------------------------------------------------------------------------------------------------
-- 060_fs_controls_workflow.sql
--
-- ماژول «صورت‌های مالی»، بخش ۴۵-ه (۲۰۲۶-۰۹-۳۰): کنترل‌ها، گردش تأیید، مقادیر دستی، کهنگی.
-- طراحی: docs/fs-module.md §۱۱. پس از 056..059.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--
-- ستون‌های افزوده روی TB_FS_RUN nullable‌اند؛ جدول‌های تازه مستقل‌اند — روی جدول‌های پُر بی‌خطر.
--------------------------------------------------------------------------------------------------

-- اجرا: اثر انگشت مانده‌های منبع (برای تشخیص «کهنه») و اجرایی که این یکی از رویش ساخته شد
-- (ورود مقادیر دستی = اجرای تازه با همان پارامترها؛ Snapshot قبلی دست نمی‌خورد).
-- STATE حالا: 1=Draft، 2=InReview، 3=Approved، 4=Published، 5=Superseded.
ALTER TABLE TB_FS_RUN ADD (
    BALANCE_HASH  VARCHAR2(64),
    SOURCE_RUN_ID CHAR(36)
);

--------------------------------------------------------------------------------------------------
-- TB_FS_CHECK_RULE — قاعدهٔ کنترل تساوی بین صورت‌ها، به‌صورت داده (سند منبع §۱۰): LEFT_EXPR = RIGHT_EXPR
-- با زبان فرمول قالب و فقط ارجاع STMT(...). مثل قالب، مشترک (VAHEDCODE NULL، فقط ستاد) یا اختصاصی.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_CHECK_RULE
(
    ID           CHAR(36)       NOT NULL,
    VAHEDCODE    VARCHAR2(4),
    FRAMEWORK    NUMBER(2)      NOT NULL,
    CODE         VARCHAR2(20)   NOT NULL,   -- مثل V-04
    TITLE_FA     VARCHAR2(500)  NOT NULL,
    LEFT_EXPR    VARCHAR2(1000) NOT NULL,
    RIGHT_EXPR   VARCHAR2(1000) NOT NULL,
    TOLERANCE    NUMBER(28)     DEFAULT 0 NOT NULL,   -- ریال
    SEVERITY     NUMBER(1)      NOT NULL,   -- FsCheckSeverity: 1=Info,2=Warning,3=Blocking
    IS_ACTIVE    NUMBER(1)      DEFAULT 1 NOT NULL,
    CREATEDDATE  TIMESTAMP      NOT NULL,
    UPDATEDDATE  TIMESTAMP,
    ADDUSERID    VARCHAR2(10)   NOT NULL,
    CHANGEUSERID VARCHAR2(10),
    ISDELETED    NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_CHECK_RULE PRIMARY KEY (ID),
    CONSTRAINT UK_FS_CHECK_RULE UNIQUE (VAHEDCODE, FRAMEWORK, CODE)
);

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN_CHECK — نتیجهٔ هر کنترل در یک اجرا (فقط درج). ROW_REF = «قالب/ردیف» مرتبط، اگر باشد.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_CHECK
(
    ID           CHAR(36)       NOT NULL,
    RUN_ID       CHAR(36)       NOT NULL,
    VAHEDCODE    VARCHAR2(4)    NOT NULL,
    CODE         VARCHAR2(20)   NOT NULL,
    TITLE_FA     VARCHAR2(500)  NOT NULL,
    SEVERITY     NUMBER(1)      NOT NULL,
    PASSED       NUMBER(1)      NOT NULL,
    MESSAGE      VARCHAR2(1000),
    DIFFERENCE   NUMBER(28),
    ROW_REF      VARCHAR2(80),
    CONSTRAINT PK_FS_RUN_CHECK PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_CHECK_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_FS_RUN (ID)
);

CREATE INDEX IDX_FS_RUN_CHECK_RUN ON TB_FS_RUN_CHECK (RUN_ID, VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN_ACTION — تاریخچهٔ گردش تأیید (فقط درج): ارسال، تأیید، برگشت (با دلیل)، انتشار، جایگزینی.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_ACTION
(
    ID           CHAR(36)       NOT NULL,
    RUN_ID       CHAR(36)       NOT NULL,
    VAHEDCODE    VARCHAR2(4)    NOT NULL,
    ACTION       NUMBER(2)      NOT NULL,   -- FsRunAction: 1=Submit,2=Approve,3=Return,4=Publish,5=Supersede
    FROM_STATE   NUMBER(2)      NOT NULL,
    TO_STATE     NUMBER(2)      NOT NULL,
    USERID       VARCHAR2(10)   NOT NULL,
    COMMENTS     VARCHAR2(1000),
    CREATEDDATE  TIMESTAMP      NOT NULL,
    CONSTRAINT PK_FS_RUN_ACTION PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_ACTION_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_FS_RUN (ID)
);

CREATE INDEX IDX_FS_RUN_ACTION_RUN ON TB_FS_RUN_ACTION (RUN_ID, VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN_MANUAL — مقدار دستی ردیف «مقدار دستی» (External) در یک اجرا، با دلیل. مبلغ به علامت
-- «نمایشی» (همان که کاربر دید و وارد کرد)؛ موتور با ماهیت ردیف به علامت حسابداری برمی‌گرداند.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_MANUAL
(
    ID            CHAR(36)       NOT NULL,
    RUN_ID        CHAR(36)       NOT NULL,
    VAHEDCODE     VARCHAR2(4)    NOT NULL,
    TEMPLATE_CODE VARCHAR2(50)   NOT NULL,
    ROW_CODE      VARCHAR2(20)   NOT NULL,
    AMOUNT_CUR    NUMBER(28),
    AMOUNT_PRV    NUMBER(28),
    REASON        VARCHAR2(1000) NOT NULL,
    ADDUSERID     VARCHAR2(10)   NOT NULL,
    CREATEDDATE   TIMESTAMP      NOT NULL,
    CONSTRAINT PK_FS_RUN_MANUAL PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_MANUAL_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_FS_RUN (ID)
);

CREATE INDEX IDX_FS_RUN_MANUAL_RUN ON TB_FS_RUN_MANUAL (RUN_ID, VAHEDCODE);
