--------------------------------------------------------------------------------------------------
-- 091_op_template_category_accrual.sql
--
-- حسابداری تعهدی، فاز ۶۶ (۲۰۲۶-۱۰-۳۰): دستهٔ الگوی «تعهدی» (TemplateCategory.Accrual = 7).
-- قید CK_OP_TEMPLATE_CATEGORY از DDL 084 فقط 1..6 و 9 را می‌پذیرفت ⇒ ذخیرهٔ الگو با دستهٔ «تعهدی»
-- (و هر نمونهٔ آمادهٔ تعهدی) ORA-02290 و خطای ۵۰۰ می‌داد. فقط همین قید از نو ساخته می‌شود؛ داده دست نمی‌خورد.
-- پس از 090 اجرا شود.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

ALTER TABLE TB_OP_TEMPLATE DROP CONSTRAINT CK_OP_TEMPLATE_CATEGORY;

ALTER TABLE TB_OP_TEMPLATE ADD CONSTRAINT CK_OP_TEMPLATE_CATEGORY
    CHECK (CATEGORY IS NULL OR CATEGORY IN (1, 2, 3, 4, 5, 6, 7, 9));
