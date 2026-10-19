--------------------------------------------------------------------------------------------------
-- 086_op_history_dismissed.sql
--
-- حسابیار فاز ۶۳ (ادامه): «حذف» پیشنهاد کشف‌شدهٔ سوابق. امضای حذف‌شده در بازسازی‌های بعدی دوباره ساخته نمی‌شود.
-- پیش‌نیاز: 085. بدون این اسکریپت «حذف» پاسخ ۵۰۳ می‌دهد و بازسازی همچنان کار می‌کند (مجموعهٔ حذف‌شده‌ها خالی فرض می‌شود).
-- این جدول عمداً در لاگ تغییرات می‌ماند (حذف پیشنهاد عمل کاربر است).
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_HISTORY_DISMISSED
(
    SIGNATURE    VARCHAR2(1000) NOT NULL,   -- همان SIGNATURE جدول TB_OP_HISTORY_PATTERN
    ADDUSERID    VARCHAR2(10),              -- کد ملی کاربری که حذف کرد
    CREATEDDATE  TIMESTAMP,
    CONSTRAINT PK_OP_HISTORY_DISMISSED PRIMARY KEY (SIGNATURE)
);
