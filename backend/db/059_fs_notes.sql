--------------------------------------------------------------------------------------------------
-- 059_fs_notes.sql
--
-- ماژول «صورت‌های مالی»، بخش ۴۵-ج (۲۰۲۶-۰۹-۳۰): یادداشت‌ها و زیر‌یادداشت‌های عددی.
-- طراحی: docs/fs-module.md §۹. پس از 056/057 (و 058 برای دیتابیسی که نسخهٔ اول آن دو را اجرا کرده).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--
-- یادداشت = قالبی با STATEMENT_TYPE = 11 که به یک ردیف صورت وصل است. همهٔ ستون‌ها nullable‌اند (یا
-- پیش‌فرض دارند)، پس روی جدول‌های پُر هم بی‌خطر اجرا می‌شود.
--------------------------------------------------------------------------------------------------

-- قالب: ارتباط یادداشت با ردیف صورت (با کد، نه شناسه) و ردیف جمع برای کنترل V-08.
ALTER TABLE TB_FS_TEMPLATE ADD (
    NOTE_PARENT_TEMPLATE_CODE VARCHAR2(50),
    NOTE_PARENT_ROW_CODE      VARCHAR2(20),
    NOTE_TOTAL_ROW_CODE       VARCHAR2(20)
);

-- اجرا: شمارهٔ اولین یادداشت عددی.
ALTER TABLE TB_FS_RUN ADD (NOTE_START_NO NUMBER(4) DEFAULT 1 NOT NULL);

-- Snapshot یادداشت: شماره، والد، ردیف جمع و اختلاف V-08 به‌ازای هر ستون.
ALTER TABLE TB_FS_RUN_STATEMENT ADD (
    IS_NOTE              NUMBER(1)    DEFAULT 0 NOT NULL,
    NOTE_NO              VARCHAR2(20),
    PARENT_TEMPLATE_CODE VARCHAR2(50),
    PARENT_ROW_CODE      VARCHAR2(20),
    TOTAL_ROW_CODE       VARCHAR2(20),
    CHECK_DIFF_CUR       NUMBER(28),
    CHECK_DIFF_PRV       NUMBER(28)
);
