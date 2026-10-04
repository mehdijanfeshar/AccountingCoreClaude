--------------------------------------------------------------------------------------------------
-- 061_fs_completion.sql
--
-- ماژول «صورت‌های مالی»، تکمیل (۲۰۲۶-۱۰-۰۴): قطعه‌های ح-۲ به بعد — docs/fs-module.md §۱۳. پس از 056..060.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--
-- هر قطعه بخش جدای خودش را در این فایل دارد؛ ستون‌های افزوده default دارند و جدول‌های تازه مستقل‌اند.
--------------------------------------------------------------------------------------------------

-- ح-۲: برچسب «تجدید ارائه‌شده» روی ستون سال قبل (فقط برچسب؛ تعدیلات سنواتی محاسبه نمی‌شود).
ALTER TABLE TB_FS_RUN ADD (
    PRIOR_RESTATED NUMBER(1) DEFAULT 0 NOT NULL
);

--------------------------------------------------------------------------------------------------
-- ح-۳: «نظر» روی ردیف/کنترل/اجرا و ارجاع کنترل ناموفق به مسئول با مهلت (سند منبع §۱۰ و §۱۲-۳).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_COMMENT
(
    ID          CHAR(36)       NOT NULL,
    RUN_ID      CHAR(36)       NOT NULL,
    VAHEDCODE   VARCHAR2(4)    NOT NULL,
    ROW_ID      CHAR(36),                   -- TB_FS_RUN_ROW.ID (بدون FK)؛ خالی = کل اجرا یا کنترل
    CHECK_ID    CHAR(36),                   -- TB_FS_RUN_CHECK.ID
    BODY        VARCHAR2(2000) NOT NULL,
    ADDUSERID   VARCHAR2(10)   NOT NULL,
    CREATEDDATE TIMESTAMP      NOT NULL,
    ISDELETED   NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_RUN_COMMENT PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_COMMENT_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_FS_RUN (ID)
);
CREATE INDEX IDX_FS_RUN_COMMENT_RUN ON TB_FS_RUN_COMMENT (RUN_ID, VAHEDCODE);

-- ASSIGN_STATE: 1=باز، 2=رفع‌شده (FsCheckAssignState). DUE_DATE = شمسی YYYYMMDD.
ALTER TABLE TB_FS_RUN_CHECK ADD (
    ASSIGNEE_USERID VARCHAR2(10),
    ASSIGNEE_NAME   VARCHAR2(200),
    DUE_DATE        CHAR(8),
    ASSIGN_STATE    NUMBER(1),
    ASSIGNED_BY     VARCHAR2(10)
);

--------------------------------------------------------------------------------------------------
-- ح-۴: گردش تأیید چندمرحله‌ای قابل تنظیم (سند منبع §۱۱). مالکیت مثل TB_FS_CHECK_RULE (VAHEDCODE NULL = مشترک).
-- APPROVER_USERIDS = کدهای کاربری با ویرگول؛ خالی = هر کاربری جز تهیه‌کننده/ارسال‌کننده/تأییدکنندگان قبلی.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_APPROVAL_STEP
(
    ID               CHAR(36)      NOT NULL,
    VAHEDCODE        VARCHAR2(4),
    FRAMEWORK        NUMBER(2)     NOT NULL,
    STEP_NO          NUMBER(2)     NOT NULL,
    TITLE_FA         VARCHAR2(200) NOT NULL,
    APPROVER_USERIDS VARCHAR2(500),
    IS_ACTIVE        NUMBER(1)     DEFAULT 1 NOT NULL,
    CREATEDDATE      TIMESTAMP     NOT NULL,
    UPDATEDDATE      TIMESTAMP,
    ADDUSERID        VARCHAR2(10)  NOT NULL,
    CHANGEUSERID     VARCHAR2(10),
    ISDELETED        NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_APPROVAL_STEP PRIMARY KEY (ID)
);

-- اجرا: تعداد مراحل تأییدشده در دور جاری؛ اقدام: شمارهٔ مرحله.
ALTER TABLE TB_FS_RUN ADD (
    APPROVAL_STEP NUMBER(2) DEFAULT 0 NOT NULL
);
ALTER TABLE TB_FS_RUN_ACTION ADD (
    STEP_NO NUMBER(2)
);

--------------------------------------------------------------------------------------------------
-- ح-۵: بستن دورهٔ صورت‌ها (سند منبع §۱۱). تصمیم صاحب پروژه: فقط برای صورت‌های مالی (V-11 و انتشار)؛
-- ثبت سند در ماژول اسناد دست نمی‌خورد. STATE: 1=باز، 2=بستهٔ موقت، 3=قفل. نبود ردیف = باز.
-- قفل یک واحد زیرمجموعه‌اش را هم قفل‌شده حساب می‌کند. ACTION (لاگ): 1=بستن،2=قفل،3=بازگشایی،
-- 4=درخواست بازگشایی،5=تأیید درخواست،6=رد درخواست.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_PERIOD
(
    ID                    CHAR(36)       NOT NULL,
    VAHEDCODE             VARCHAR2(4)    NOT NULL,
    YEAR                  CHAR(4)        NOT NULL,
    STATE                 NUMBER(1)      NOT NULL,
    REOPEN_REASON         VARCHAR2(1000),
    REOPEN_REQUESTED_BY   VARCHAR2(10),
    REOPEN_REQUESTED_DATE TIMESTAMP,
    CREATEDDATE           TIMESTAMP      NOT NULL,
    UPDATEDDATE           TIMESTAMP,
    ADDUSERID             VARCHAR2(10)   NOT NULL,
    CHANGEUSERID          VARCHAR2(10),
    CONSTRAINT PK_FS_PERIOD PRIMARY KEY (ID),
    CONSTRAINT UK_FS_PERIOD UNIQUE (VAHEDCODE, YEAR)
);

CREATE TABLE TB_FS_PERIOD_LOG
(
    ID          CHAR(36)       NOT NULL,
    PERIOD_ID   CHAR(36)       NOT NULL,
    VAHEDCODE   VARCHAR2(4)    NOT NULL,
    YEAR        CHAR(4)        NOT NULL,
    ACTION      NUMBER(1)      NOT NULL,
    FROM_STATE  NUMBER(1)      NOT NULL,
    TO_STATE    NUMBER(1)      NOT NULL,
    USERID      VARCHAR2(10)   NOT NULL,
    REASON      VARCHAR2(1000),
    CREATEDDATE TIMESTAMP      NOT NULL,
    CONSTRAINT PK_FS_PERIOD_LOG PRIMARY KEY (ID),
    CONSTRAINT FK_FS_PERIOD_LOG_PERIOD FOREIGN KEY (PERIOD_ID) REFERENCES TB_FS_PERIOD (ID)
);
CREATE INDEX IDX_FS_PERIOD_LOG_UNIT ON TB_FS_PERIOD_LOG (VAHEDCODE, YEAR);

--------------------------------------------------------------------------------------------------
-- ح-۶: یادداشت‌های توضیحی متنی (سند منبع §۹). CONTENT_JSON = سند Tiptap با متغیرهای {{...}} که هنگام نمایش
-- از Snapshot اجرا مقدار می‌گیرند. STATE: 1=پیش‌نویس،2=در بازبینی،3=تأییدشده،4=نیازمند اصلاح.
-- هنگام انتشار اجرا، متن‌ها در TB_FS_RUN_NARRATIVE کپی می‌شوند تا صورت منتشرشده ثابت بماند.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_NARRATIVE
(
    ID                   CHAR(36)       NOT NULL,
    VAHEDCODE            VARCHAR2(4)    NOT NULL,
    FRAMEWORK            NUMBER(2)      NOT NULL,
    YEAR                 CHAR(4)        NOT NULL,
    ORDER_NO             NUMBER(5)      NOT NULL,
    TITLE_FA             VARCHAR2(500)  NOT NULL,
    LINKED_TEMPLATE_CODE VARCHAR2(50),
    CONTENT_JSON         CLOB,
    STATE                NUMBER(1)      NOT NULL,
    RESPONSIBLE_USERID   VARCHAR2(10),
    REVIEW_COMMENT       VARCHAR2(1000),
    VERSION_NO           NUMBER(5)      DEFAULT 1 NOT NULL,
    CREATEDDATE          TIMESTAMP      NOT NULL,
    UPDATEDDATE          TIMESTAMP,
    ADDUSERID            VARCHAR2(10)   NOT NULL,
    CHANGEUSERID         VARCHAR2(10),
    ISDELETED            NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_NARRATIVE PRIMARY KEY (ID)
);
CREATE INDEX IDX_FS_NARRATIVE_SET ON TB_FS_NARRATIVE (VAHEDCODE, FRAMEWORK, YEAR);

CREATE TABLE TB_FS_NARRATIVE_VERSION
(
    ID           CHAR(36)      NOT NULL,
    NARRATIVE_ID CHAR(36)      NOT NULL,
    VERSION_NO   NUMBER(5)     NOT NULL,
    TITLE_FA     VARCHAR2(500) NOT NULL,
    CONTENT_JSON CLOB,
    ADDUSERID    VARCHAR2(10)  NOT NULL,
    CREATEDDATE  TIMESTAMP     NOT NULL,
    CONSTRAINT PK_FS_NARRATIVE_VERSION PRIMARY KEY (ID),
    CONSTRAINT FK_FS_NARRATIVE_VER FOREIGN KEY (NARRATIVE_ID) REFERENCES TB_FS_NARRATIVE (ID)
);
CREATE INDEX IDX_FS_NARRATIVE_VER ON TB_FS_NARRATIVE_VERSION (NARRATIVE_ID);

CREATE TABLE TB_FS_RUN_NARRATIVE
(
    ID                   CHAR(36)      NOT NULL,
    RUN_ID               CHAR(36)      NOT NULL,
    VAHEDCODE            VARCHAR2(4)   NOT NULL,
    NARRATIVE_ID         CHAR(36)      NOT NULL,
    ORDER_NO             NUMBER(5)     NOT NULL,
    TITLE_FA             VARCHAR2(500) NOT NULL,
    LINKED_TEMPLATE_CODE VARCHAR2(50),
    CONTENT_JSON         CLOB,
    VERSION_NO           NUMBER(5)     NOT NULL,
    CONSTRAINT PK_FS_RUN_NARRATIVE PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_NARRATIVE_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_FS_RUN (ID)
);
CREATE INDEX IDX_FS_RUN_NARRATIVE_RUN ON TB_FS_RUN_NARRATIVE (RUN_ID, VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- ح-۸: نسبت‌های مالی به‌صورت داده (سند منبع §۱۲-۳). صورت/مخرج با زبان فرمول قالب (فقط STMT، عدد، عملگر)
-- روی مبلغ نمایشی. FORMAT: 1=درصد، 2=برابر، 3=مبلغ. مالکیت مثل TB_FS_CHECK_RULE.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RATIO
(
    ID               CHAR(36)       NOT NULL,
    VAHEDCODE        VARCHAR2(4),
    FRAMEWORK        NUMBER(2)      NOT NULL,
    CODE             VARCHAR2(20)   NOT NULL,
    TITLE_FA         VARCHAR2(500)  NOT NULL,
    NUMERATOR_EXPR   VARCHAR2(1000) NOT NULL,
    DENOMINATOR_EXPR VARCHAR2(1000),
    FORMAT           NUMBER(1)      DEFAULT 1 NOT NULL,
    ORDER_NO         NUMBER(5)      DEFAULT 0 NOT NULL,
    IS_ACTIVE        NUMBER(1)      DEFAULT 1 NOT NULL,
    CREATEDDATE      TIMESTAMP      NOT NULL,
    UPDATEDDATE      TIMESTAMP,
    ADDUSERID        VARCHAR2(10)   NOT NULL,
    CHANGEUSERID     VARCHAR2(10),
    ISDELETED        NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_RATIO PRIMARY KEY (ID)
);
