--------------------------------------------------------------------------------------------------
-- 056_fs_templates.sql
--
-- ماژول «صورت‌های مالی»، بخش ۴۵-الف (۲۰۲۶-۰۹-۳۰): قالب صورت + نسخه + ردیف. سه جدول کاملاً جدید با
-- پیشوند TB_FS_ (Financial Statements). طراحی: docs/fs-module.md §۲ — پیش از تغییر، آن را بخوان.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- NOT executed anywhere as of authoring time (2026-09-30). Same standing rule as 044-055.
--
-- مالکیت قالب (تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰): TB_FS_TEMPLATE.VAHEDCODE خالی = قالب مشترک همهٔ
-- واحدها (فقط ستاد می‌سازد/ویرایش می‌کند)؛ پُر = قالب اختصاصی آن واحد/شرکت و زیرمجموعه‌هایش، که در
-- اجرا بر قالب مشترکِ هم‌کد مقدم است. نسخه و ردیف از طریق قالب به واحد می‌رسند.
-- قالب‌های پیش‌فرض با POST api/fs/templates/seed-defaults ساخته می‌شوند، نه با INSERT.
--
-- ⚠️ نسخهٔ اول این اسکریپت (پیش از ۲۰۲۶-۰۹-۳۰ عصر) ستون VAHEDCODE نداشت؛ دیتابیسی که آن را اجرا
-- کرده، 058_fs_unit_scope_patch.sql را اجرا کند.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_FS_TEMPLATE — شناسهٔ منطقی یک صورت. CODE در هر مالک (VAHEDCODE) یکتا و تغییرناپذیر است و پس از
-- حذف نرم هم آزاد نمی‌شود. UNIQUE ترکیبی اوراکل ردیف‌های (NULL, 'X') را هم با هم مقایسه می‌کند، پس
-- دو قالب مشترک هم‌کد ممکن نیست ولی قالب اختصاصی هم‌کد با قالب مشترک ممکن است (همان «جایگزینی»).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_TEMPLATE
(
    ID             CHAR(36)       NOT NULL,
    VAHEDCODE      VARCHAR2(4),              -- NULL = مشترک
    FRAMEWORK      NUMBER(2)      NOT NULL,  -- FsFramework: 1=Pension,2=Commercial,3=Public,4=Management
    CODE           VARCHAR2(50)   NOT NULL,
    TITLE_FA       VARCHAR2(200)  NOT NULL,
    TITLE_EN       VARCHAR2(200),
    STATEMENT_TYPE NUMBER(2)      NOT NULL,  -- FsStatementType: 1..10
    ORDER_NO       NUMBER(5)      DEFAULT 0 NOT NULL,
    CREATEDDATE    TIMESTAMP      NOT NULL,
    UPDATEDDATE    TIMESTAMP,
    ADDUSERID      VARCHAR2(10)   NOT NULL,
    CHANGEUSERID   VARCHAR2(10),
    ISDELETED      NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_TEMPLATE PRIMARY KEY (ID),
    CONSTRAINT UK_FS_TEMPLATE_CODE UNIQUE (VAHEDCODE, CODE)
);

CREATE INDEX IDX_FS_TEMPLATE_VAHED ON TB_FS_TEMPLATE (VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- TB_FS_TEMPLATE_VERSION — نسخهٔ قالب. STATE: 1=Draft (قابل ویرایش)، 2=Active (تغییرناپذیر)،
-- 3=Retired. «حداکثر یک Draft در هر قالب» و «یک Active به‌ازای هر EFFECTIVE_FROM_YEAR» سمت
-- Application کنترل می‌شود.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_TEMPLATE_VERSION
(
    ID                  CHAR(36)       NOT NULL,
    TEMPLATE_ID         CHAR(36)       NOT NULL,
    VERSION_NO          NUMBER(5)      NOT NULL,
    STATE               NUMBER(2)      NOT NULL,
    EFFECTIVE_FROM_YEAR NUMBER(4),
    DESCRIPTION         VARCHAR2(1000),
    ACTIVATED_BY        VARCHAR2(10),
    ACTIVATED_DATE      TIMESTAMP,
    CONTENT_HASH        VARCHAR2(64),
    CREATEDDATE         TIMESTAMP      NOT NULL,
    UPDATEDDATE         TIMESTAMP,
    ADDUSERID           VARCHAR2(10)   NOT NULL,
    CHANGEUSERID        VARCHAR2(10),
    ISDELETED           NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_TEMPLATE_VERSION PRIMARY KEY (ID),
    CONSTRAINT UK_FS_TEMPLATE_VERSION UNIQUE (TEMPLATE_ID, VERSION_NO),
    CONSTRAINT FK_FS_TEMPLATE_VERSION_TPL FOREIGN KEY (TEMPLATE_ID) REFERENCES TB_FS_TEMPLATE (ID)
);

--------------------------------------------------------------------------------------------------
-- TB_FS_TEMPLATE_ROW — ردیف یک نسخه. حذف سخت (فرزند تعبیه‌شدهٔ نسخهٔ پیش‌نویس، docs/fs-module.md).
-- SELECTOR/FORMULA متن خام‌اند؛ نحوشان هنگام ذخیره سمت Application تجزیه و اعتبارسنجی می‌شود.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_TEMPLATE_ROW
(
    ID                  CHAR(36)       NOT NULL,
    VERSION_ID          CHAR(36)       NOT NULL,
    CODE                VARCHAR2(20)   NOT NULL,
    PARENT_ID           CHAR(36),
    ORDER_NO            NUMBER(5)      NOT NULL,
    ROW_TYPE            NUMBER(2)      NOT NULL,  -- FsRowType: 1=Header,2=Account,3=Formula,4=External,5=Text,6=Blank
    TITLE_FA            VARCHAR2(500),
    TITLE_EN            VARCHAR2(500),
    NOTE_REF            VARCHAR2(20),
    NORMAL_BALANCE      NUMBER(1),                -- FsNormalBalance: 1=Debit,2=Credit
    SELECTOR            VARCHAR2(1000),
    VALUE_TYPE          NUMBER(2),                -- FsValueType: 1=Closing,2=Opening,3=Movement,4=Debit,5=Credit
    FORMULA             VARCHAR2(1000),
    FORMAT_JSON         VARCHAR2(1000),
    IS_DRILLABLE        NUMBER(1)      DEFAULT 1 NOT NULL,
    ALLOW_MANUAL_ADJUST NUMBER(1)      DEFAULT 0 NOT NULL,
    CREATEDDATE         TIMESTAMP      NOT NULL,
    UPDATEDDATE         TIMESTAMP,
    ADDUSERID           VARCHAR2(10)   NOT NULL,
    CHANGEUSERID        VARCHAR2(10),
    CONSTRAINT PK_FS_TEMPLATE_ROW PRIMARY KEY (ID),
    CONSTRAINT UK_FS_TEMPLATE_ROW_CODE UNIQUE (VERSION_ID, CODE),
    CONSTRAINT FK_FS_TEMPLATE_ROW_VER FOREIGN KEY (VERSION_ID) REFERENCES TB_FS_TEMPLATE_VERSION (ID),
    CONSTRAINT FK_FS_TEMPLATE_ROW_PARENT FOREIGN KEY (PARENT_ID) REFERENCES TB_FS_TEMPLATE_ROW (ID)
);

CREATE INDEX IDX_FS_TEMPLATE_ROW_VER ON TB_FS_TEMPLATE_ROW (VERSION_ID, ORDER_NO);
