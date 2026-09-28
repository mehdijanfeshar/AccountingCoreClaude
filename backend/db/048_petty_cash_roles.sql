--------------------------------------------------------------------------------------------------
-- 048_petty_cash_roles.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری") — تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸ owner decision): roles
-- (Inspector/FinanceManager/ChiefExecutive), two-stage approval (Verify + FinalApprove), and the
-- field-by-field lock on Returned documents. See docs/tankhah-khazaneh-module.md, بخش «تصمیم‌های
-- تکمیل بخش ۲» for the full rule set — this script is schema-only.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- Same standing rule as 044/045/046/047_petty_cash*.sql. NOT executed anywhere as of authoring
-- time (2026-09-28).
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- بخش ۱ — نقش‌ها روی TB_PC_REVIEWER
--
-- ROLE پیش‌فرض ۱ (Inspector/بازرس) می‌گیرد، پس ردیف‌های موجود (همگی پیش از این تصمیم — بخش ۱/۲
-- قدیم) بدون نیاز به backfill دستی بازرس محسوب می‌شوند. کلید یکتا از (FUND_ID, REVIEWER_USERID)
-- به (FUND_ID, REVIEWER_USERID, ROLE) عریض می‌شود — یک کاربر می‌تواند روی یک تنخواه بیش از یک
-- نقش داشته باشد (مثلاً هم بازرس هم مدیر مالی).
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_PC_REVIEWER ADD ROLE NUMBER(2) DEFAULT 1 NOT NULL;

ALTER TABLE TB_PC_REVIEWER DROP CONSTRAINT UK_PC_REVIEWER;
ALTER TABLE TB_PC_REVIEWER ADD CONSTRAINT UK_PC_REVIEWER UNIQUE (FUND_ID, REVIEWER_USERID, ROLE);

--------------------------------------------------------------------------------------------------
-- بخش ۲ — سقف اختیار تأیید نهایی مدیر مالی روی TB_PC_FUND
--
-- پیش‌فرض ۵۰۰،۰۰۰،۰۰۰ («تا ۵۰۰ میلیون»، صفحهٔ ۱۳ پاورپوینت) — بیشتر از این فقط مدیرعامل می‌تواند
-- تأیید نهایی کند.
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_PC_FUND ADD FINANCE_MANAGER_APPROVAL_LIMIT NUMBER(25) DEFAULT 500000000 NOT NULL;

--------------------------------------------------------------------------------------------------
-- بخش ۳ — تأیید دومرحله‌ای روی TB_PC_EXPENSE_DOC
--
-- VERIFIED_BY_USERID/VERIFIED_DATE ست می‌شوند وقتی بازرس کنترل سند را در PendingReview تأیید کند
-- (اکشن Verify — وضعیت سند تغییر نمی‌کند). Return این دو ستون را پاک می‌کند (سند برگشتی دوباره
-- باید کنترل شود).
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_PC_EXPENSE_DOC ADD VERIFIED_BY_USERID VARCHAR2(10);
ALTER TABLE TB_PC_EXPENSE_DOC ADD VERIFIED_DATE TIMESTAMP;
