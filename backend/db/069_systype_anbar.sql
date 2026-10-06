--------------------------------------------------------------------------------------------------
-- 069_systype_anbar.sql
--
-- نوع سند «انبار» (۲۰۲۶-۱۰-۰۶، درخواست صاحب پروژه). فقط داده (DML) روی lookup ‏TB_SYSTYPE، بدون تغییر ساختار.
-- کد ۸ = بعد از ۷ ردیف موجود دیتابیس توسعه (۱ حسابداری … ۷ سند اعلامیه رسیده). تکرارپذیر: اگر کد ۸ یا
-- عنوان «انبار» از قبل باشد، چیزی درج نمی‌شود. شناسه با همان قالب Guid اسکریپت 063 ساخته می‌شود (ریسک #۱۱).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

INSERT INTO TB_SYSTYPE (ID, SYS_COD, SYS_NAME)
SELECT LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()), '(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5')), '8', 'انبار'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM TB_SYSTYPE WHERE SYS_COD = '8' OR SYS_NAME = 'انبار');

COMMIT;
