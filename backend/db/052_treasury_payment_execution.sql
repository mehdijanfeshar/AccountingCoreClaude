--------------------------------------------------------------------------------------------------
-- 052_treasury_payment_execution.sql
--
-- خزانه‌داری ("تنخواه و خزانه‌داری")، بخش ۴-ب (۲۰۲۶-۰۹-۲۹ owner decisions): اجرای پرداخت + دو سند
-- GL خودکار («شناسایی بدهی» در لحظهٔ تأیید نهایی، «پرداخت» در لحظهٔ اجرا) + نوشتن Legacy
-- TB_PAYRECIVHEAD/DETAIL. فقط ALTER روی جدول‌های موجودِ بخش ۴-الف (051) — جدول جدیدی اینجا نیست.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- NOT executed anywhere as of authoring time (2026-09-29). Same standing rule as every
-- 044-051_*.sql script.
--
-- Design reference: docs/tankhah-khazaneh-module.md §۱۰ (خزانه‌داری)، بخش ۴-ب. Read that section
-- before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_TR_SETTING — سه حساب معین تازه که سند «شناسایی بدهی»/«پرداخت» به آن‌ها نیاز دارد. هر سه
-- NULL-پذیرند (تا وقتی مدیر مالی تعریف نکند، صدور سند شناسایی بدهی با ۴۰۹ رد می‌شود). بدون FK
-- واقعی روی TB_ACCOUNTCODE (همان الگوی ریسک #۹/#۱۴ — تکرار عمدی).
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_TR_SETTING ADD PAYABLES_ACCOUNT_ID CHAR(36);
ALTER TABLE TB_TR_SETTING ADD VAT_CREDIT_ACCOUNT_ID CHAR(36);
ALTER TABLE TB_TR_SETTING ADD INSURANCE_PAYABLE_ACCOUNT_ID CHAR(36);

--------------------------------------------------------------------------------------------------
-- TB_TR_PAYMENT_REQUEST — دو سند GL خودکار + فیلدهای اجرای دستی پرداخت (بدون یکپارچگی بانکی؛
-- خزانه‌دار خودش در بانک پرداخت می‌کند، بعد اجرا را در برنامه ثبت می‌کند).
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD LIABILITY_VOUCHER_ID CHAR(36);
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD PAYMENT_VOUCHER_ID CHAR(36);
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD BANK_REFERENCE VARCHAR2(100);
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD PAID_DATE VARCHAR2(8);       -- شمسی YYYYMMDD
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD DESTINATION_IBAN VARCHAR2(26);
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD EXECUTED_BY VARCHAR2(10);
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD EXECUTED_DATE TIMESTAMP;
ALTER TABLE TB_TR_PAYMENT_REQUEST ADD SUSPEND_REASON VARCHAR2(1000);

-- REQUEST_STATE گسترش یافت (PaymentRequestState): 8=Executed, 9=Suspended — بدون CHECK constraint
-- (همان الگوی بدون-enum-constraint-بودن ستون‌های عددی چندمقداری این پروژه؛ کنترل سمت Application).
