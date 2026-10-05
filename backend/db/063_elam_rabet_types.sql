--------------------------------------------------------------------------------------------------
-- 063_elam_rabet_types.sql
--
-- اعلامیه (عملیات، ۲۰۲۶-۱۰-۰۵): انواع رابط موردنیاز گردش اعلامیه. فقط داده (DML)، بدون تغییر ساختار.
-- کد نوع رابط عین ElamType مرجع: 1 = سایر اعلامیهٔ صادره، 2 = سایر اعلامیهٔ رسیده، 3 = اعلامیهٔ صادرهٔ درآمد.
-- برنامه حساب رابط هر نوع را از TB_RABET (RABETTYPE_ID → ACCOUNTCODE_ID) می‌خواند؛ اگر چند حساب برای
-- یک نوع ثبت شده باشد، حساب با کمترین کد معین برداشته می‌شود — پس برای هر نوع دقیقاً یک حساب بگذارید.
--
-- ⚠️ روی دیتابیس توسعه نوع «1» با عنوان «اعلاميه-رسيده» و ۷ حساب ثبت است، که با مرجع (1 = صادره)
-- نمی‌خواند. پیش از اجرا عنوان/حساب‌ها را با صاحب پروژه تطبیق دهید.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

INSERT INTO TB_RABET_TYPE (ID, RABETCODE, TITLE)
SELECT LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')), '2', 'اعلامیه-رسیده'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM TB_RABET_TYPE WHERE RABETCODE = '2');

INSERT INTO TB_RABET_TYPE (ID, RABETCODE, TITLE)
SELECT LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')), '3', 'اعلامیه-صادره-درآمد'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM TB_RABET_TYPE WHERE RABETCODE = '3');

-- سپس برای هر نوع، حساب رابط را در TB_RABET تعریف کنید (صفحهٔ UI ندارد؛ API: POST api/rabets). الگو:
-- INSERT INTO TB_RABET (ID, RABETTYPE_ID, ACCOUNTCODE_ID, CREATEDDATE, ISDELETED)
-- SELECT LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')),
--        t.ID, a.ID, SYSTIMESTAMP, 0
-- FROM TB_RABET_TYPE t, TB_ACCOUNTCODE a
-- WHERE t.RABETCODE = '2' AND a.ACCCODE = '<کد معین رابط رسیده>';

COMMIT;
