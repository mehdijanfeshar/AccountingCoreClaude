--------------------------------------------------------------------------------------------------
-- 057_fs_runs.sql
--
-- ماژول «صورت‌های مالی»، بخش ۴۵-ب (۲۰۲۶-۰۹-۳۰): اجرای تهیهٔ صورت‌ها + Snapshot تغییرناپذیر.
-- طراحی: docs/fs-module.md §۷ — پیش از تغییر، آن را بخوان.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- NOT executed anywhere as of authoring time (2026-09-30). Same standing rule as 044-056.
--
-- Snapshot خودبسنده است: هر اجرا کپی ردیف‌های قالب (عنوان، قالب‌بندی، …) را همراه مبالغ نگه
-- می‌دارد، تا تغییر یا حذف قالب/پیش‌نویس بعدی هرگز صورت اجراشدهٔ قبلی را عوض نکند. ردیف‌های
-- TB_FS_RUN_ROW و TB_FS_RUN_ACCOUNT فقط درج می‌شوند (هرگز به‌روز نمی‌شوند).
--
-- تفکیک واحد (تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰): VAHEDCODE (واحد صاحب اجرا) و RUN_ID روی هر چهار
-- جدول تکرار می‌شوند تا هر جدول بدون جوین قابل فیلتر/ایندکس بر واحد باشد. TB_FS_RUN_ACCOUNT
-- سهم هر معین را به تفکیک «زیرواحد سطح اول» اجرا نگه می‌دارد (SOURCE_VAHEDCODE؛ مثلاً اداره کل در
-- اجرای ستاد) — نه تک‌تک شعبه‌ها، تا حجم هر اجرا محدود بماند.
--
-- ⚠️ نسخهٔ اول این اسکریپت این ستون‌ها را نداشت؛ دیتابیسی که آن را اجرا کرده،
-- 058_fs_unit_scope_patch.sql را اجرا کند.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN — یک اجرای «تهیهٔ صورت‌های مالی» برای یک واحد (و اختیاری زیرمجموعه‌هایش) و یک دوره.
-- دوره = از ابتدای YEAR تا پایان ماه TO_MONTH (تجمعی). STATE: 1=Draft (بخش ۴۵-د گردش تأیید را
-- اضافه می‌کند).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN
(
    ID               CHAR(36)       NOT NULL,
    RUN_NO           NUMBER(10)     NOT NULL,
    VAHEDCODE        VARCHAR2(4)    NOT NULL,
    VAHEDNAME        VARCHAR2(200),
    INCLUDE_SUBUNITS NUMBER(1)      DEFAULT 0 NOT NULL,
    UNIT_COUNT       NUMBER(6)      NOT NULL,
    FRAMEWORK        NUMBER(2)      NOT NULL,
    YEAR             VARCHAR2(4)    NOT NULL,
    TO_MONTH         NUMBER(2)      NOT NULL,
    FROM_DATE        VARCHAR2(8)    NOT NULL,
    TO_DATE          VARCHAR2(8)    NOT NULL,
    MIN_DOCLIFE      NUMBER(1)      NOT NULL,
    HAS_PRIOR        NUMBER(1)      DEFAULT 0 NOT NULL,
    USES_DRAFT       NUMBER(1)      DEFAULT 0 NOT NULL,
    STATE            NUMBER(2)      NOT NULL,
    DESCRIPTION      VARCHAR2(1000),
    CONTENT_HASH     VARCHAR2(64),
    DURATION_MS      NUMBER(10),
    CREATEDDATE      TIMESTAMP      NOT NULL,
    UPDATEDDATE      TIMESTAMP,
    ADDUSERID        VARCHAR2(10)   NOT NULL,
    CHANGEUSERID     VARCHAR2(10),
    ISDELETED        NUMBER(1)      DEFAULT 0 NOT NULL,
    CONSTRAINT PK_FS_RUN PRIMARY KEY (ID),
    CONSTRAINT UK_FS_RUN_NO UNIQUE (RUN_NO)
);

CREATE INDEX IDX_FS_RUN_VAHED ON TB_FS_RUN (VAHEDCODE, YEAR);

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN_STATEMENT — هر صورت داخل یک اجرا و نسخهٔ قالبی که با آن محاسبه شد.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_STATEMENT
(
    ID             CHAR(36)       NOT NULL,
    RUN_ID         CHAR(36)       NOT NULL,
    VAHEDCODE      VARCHAR2(4)    NOT NULL,
    TEMPLATE_ID    CHAR(36)       NOT NULL,
    VERSION_ID     CHAR(36)       NOT NULL,
    TEMPLATE_CODE  VARCHAR2(50)   NOT NULL,
    TITLE_FA       VARCHAR2(200)  NOT NULL,
    STATEMENT_TYPE NUMBER(2)      NOT NULL,
    ORDER_NO       NUMBER(5)      NOT NULL,
    VERSION_NO     NUMBER(5)      NOT NULL,
    VERSION_STATE  NUMBER(2)      NOT NULL,  -- وضعیت نسخه در لحظهٔ اجرا (Draft = پیش‌نمایش)
    CONSTRAINT PK_FS_RUN_STATEMENT PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_STATEMENT_RUN FOREIGN KEY (RUN_ID) REFERENCES TB_FS_RUN (ID)
);

CREATE INDEX IDX_FS_RUN_STATEMENT_RUN ON TB_FS_RUN_STATEMENT (RUN_ID);
CREATE INDEX IDX_FS_RUN_STATEMENT_VAHED ON TB_FS_RUN_STATEMENT (VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN_ROW — کپی ردیف قالب + مبلغ هر ستون. مبلغ با علامت حسابداری (بدهکار مثبت) است؛
-- ماهیت فقط در نمایش اعمال می‌شود. NULL = ردیف بدون مقدار (عنوان/متن/خالی) یا ستون محاسبه‌نشده.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_ROW
(
    ID                CHAR(36)       NOT NULL,
    RUN_STATEMENT_ID  CHAR(36)       NOT NULL,
    RUN_ID            CHAR(36)       NOT NULL,
    VAHEDCODE         VARCHAR2(4)    NOT NULL,
    ROW_CODE          VARCHAR2(20)   NOT NULL,
    PARENT_CODE       VARCHAR2(20),
    ORDER_NO          NUMBER(5)      NOT NULL,
    ROW_TYPE          NUMBER(2)      NOT NULL,
    TITLE_FA          VARCHAR2(500),
    TITLE_EN          VARCHAR2(500),
    NOTE_REF          VARCHAR2(20),
    NORMAL_BALANCE    NUMBER(1),
    SELECTOR          VARCHAR2(1000),
    VALUE_TYPE        NUMBER(2),
    FORMULA           VARCHAR2(1000),
    FORMAT_JSON       VARCHAR2(1000),
    IS_DRILLABLE      NUMBER(1)      DEFAULT 0 NOT NULL,
    AMOUNT_CUR        NUMBER(28),
    AMOUNT_PRV        NUMBER(28),
    CONSTRAINT PK_FS_RUN_ROW PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_ROW_STMT FOREIGN KEY (RUN_STATEMENT_ID) REFERENCES TB_FS_RUN_STATEMENT (ID)
);

CREATE INDEX IDX_FS_RUN_ROW_STMT ON TB_FS_RUN_ROW (RUN_STATEMENT_ID, ORDER_NO);
CREATE INDEX IDX_FS_RUN_ROW_RUN ON TB_FS_RUN_ROW (RUN_ID, VAHEDCODE);

--------------------------------------------------------------------------------------------------
-- TB_FS_RUN_ACCOUNT — سهم هر معین در هر ردیف «حساب»، به تفکیک زیرواحد سطح اول اجرا
-- (SOURCE_VAHEDCODE؛ برای اسناد خود واحد اجرا = همان واحد). جمع ردیف‌های یک (RUN_ROW_ID, ACCCODE)
-- = سهم آن معین. سطح «حساب» و «واحد» Drill-down (بخش ۴۵-ج).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_FS_RUN_ACCOUNT
(
    ID               CHAR(36)       NOT NULL,
    RUN_ROW_ID       CHAR(36)       NOT NULL,
    RUN_ID           CHAR(36)       NOT NULL,
    VAHEDCODE        VARCHAR2(4)    NOT NULL,
    SOURCE_VAHEDCODE VARCHAR2(4)    NOT NULL,
    ACCCODE          VARCHAR2(50)   NOT NULL,
    ACCNAME          VARCHAR2(500),
    AMOUNT_CUR       NUMBER(28),
    AMOUNT_PRV       NUMBER(28),
    CONSTRAINT PK_FS_RUN_ACCOUNT PRIMARY KEY (ID),
    CONSTRAINT FK_FS_RUN_ACCOUNT_ROW FOREIGN KEY (RUN_ROW_ID) REFERENCES TB_FS_RUN_ROW (ID)
);

CREATE INDEX IDX_FS_RUN_ACCOUNT_ROW ON TB_FS_RUN_ACCOUNT (RUN_ROW_ID);
CREATE INDEX IDX_FS_RUN_ACCOUNT_RUN ON TB_FS_RUN_ACCOUNT (RUN_ID, SOURCE_VAHEDCODE);
