--------------------------------------------------------------------------------------------------
-- 058_fs_unit_scope_patch.sql
--
-- وصلهٔ تفکیک واحد برای ماژول صورت‌های مالی (تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰) — فقط برای دیتابیسی که
-- نسخهٔ «اول» 056_fs_templates.sql و 057_fs_runs.sql را اجرا کرده است. دیتابیس تازه نسخهٔ فعلی
-- 056/057 را اجرا کند و این فایل را نه (همان الگوی 055 برای 051).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--
-- پیش‌شرط: چهار جدول TB_FS_RUN* خالی باشند (ستون‌های NOT NULL جدید بدون مقدار پیش‌فرض اضافه
-- می‌شوند). اجراها پیش‌نویس و بازتولیدپذیرند؛ اگر ردیفی هست، اول پاکشان کنید:
--   DELETE FROM TB_FS_RUN_ACCOUNT; DELETE FROM TB_FS_RUN_ROW;
--   DELETE FROM TB_FS_RUN_STATEMENT; DELETE FROM TB_FS_RUN;
--------------------------------------------------------------------------------------------------

-- قالب: مالک اختیاری (NULL = مشترک) و یکتایی کد در هر مالک.
ALTER TABLE TB_FS_TEMPLATE ADD (VAHEDCODE VARCHAR2(4));
ALTER TABLE TB_FS_TEMPLATE DROP CONSTRAINT UK_FS_TEMPLATE_CODE;
ALTER TABLE TB_FS_TEMPLATE ADD CONSTRAINT UK_FS_TEMPLATE_CODE UNIQUE (VAHEDCODE, CODE);
CREATE INDEX IDX_FS_TEMPLATE_VAHED ON TB_FS_TEMPLATE (VAHEDCODE);

-- اجرا: واحد صاحب اجرا و شناسهٔ اجرا روی هر جدول فرزند.
ALTER TABLE TB_FS_RUN_STATEMENT ADD (VAHEDCODE VARCHAR2(4) NOT NULL);
CREATE INDEX IDX_FS_RUN_STATEMENT_VAHED ON TB_FS_RUN_STATEMENT (VAHEDCODE);

ALTER TABLE TB_FS_RUN_ROW ADD (RUN_ID CHAR(36) NOT NULL, VAHEDCODE VARCHAR2(4) NOT NULL);
CREATE INDEX IDX_FS_RUN_ROW_RUN ON TB_FS_RUN_ROW (RUN_ID, VAHEDCODE);

ALTER TABLE TB_FS_RUN_ACCOUNT ADD (
    RUN_ID           CHAR(36)    NOT NULL,
    VAHEDCODE        VARCHAR2(4) NOT NULL,
    SOURCE_VAHEDCODE VARCHAR2(4) NOT NULL
);
CREATE INDEX IDX_FS_RUN_ACCOUNT_RUN ON TB_FS_RUN_ACCOUNT (RUN_ID, SOURCE_VAHEDCODE);
