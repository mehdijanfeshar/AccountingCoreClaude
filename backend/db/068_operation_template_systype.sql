--------------------------------------------------------------------------------------------------
-- 068_operation_template_systype.sql
--
-- Agent-UX (۲۰۲۶-۱۰-۰۶): «نوع سند» در الگوی عملیات — سندی که از الگو ساخته می‌شود همین نوع
-- (TB_VOUCHERSHEAD.SYSTEM_TYPE) را می‌گیرد. پیش‌نیاز: 067. بدون این اسکریپت همهٔ /api/operations خطا می‌دهد
-- (ستون SYSTEM_TYPE_ID در نگاشت هست).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

ALTER TABLE TB_OP_TEMPLATE ADD (SYSTEM_TYPE_ID CHAR(36));

ALTER TABLE TB_OP_TEMPLATE ADD CONSTRAINT FK_OP_TEMPLATE_SYSTYPE
    FOREIGN KEY (SYSTEM_TYPE_ID) REFERENCES TB_SYSTYPE (ID);
