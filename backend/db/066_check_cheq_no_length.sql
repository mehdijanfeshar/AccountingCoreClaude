--------------------------------------------------------------------------------------------------
-- 066_check_cheq_no_length.sql
--
-- چک صوری (فاز ۴۹-ب، ۲۰۲۶-۱۰-۰۵): شمارهٔ چک صوری «سال + کد واحد + ۰۰۰۱..۱۰۰۰» دوازده رقمی است
-- (مثلاً 140511550001)، ولی TB_CHECK.CHEQ_NO در Scaffold طول ۱۰ دارد. هم‌طول FROMCHECKNUMBER/TOCHECKNUMBER
-- دسته‌چک (۱۴) می‌شود. فقط افزایش طول است؛ دادهٔ موجود دست نمی‌خورد.
-- اگر ستون در دیتابیس شما از قبل ۱۴ یا بیشتر است، اجرای این فایل لازم نیست:
--   SELECT DATA_LENGTH FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'TB_CHECK' AND COLUMN_NAME = 'CHEQ_NO';
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

ALTER TABLE TB_CHECK MODIFY CHEQ_NO VARCHAR2(14);
