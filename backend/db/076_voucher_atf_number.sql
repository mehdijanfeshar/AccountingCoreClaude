--------------------------------------------------------------------------------------------------
-- 076_voucher_atf_number.sql
--
-- شمارهٔ عطف سند (۲۰۲۶-۱۰-۰۸): سال (۴) + کد واحد (۴) + ردیف ۷ رقمی = ۱۵ رقم، مثلاً 140411550000001.
-- از این پس سرور هنگام ایجاد سند تخصیص می‌دهد (VoucherAtfNumberInterceptor) و هرگز عوض نمی‌شود.
--
-- بخش ۱ (DML): به اسنادی که عطف ندارند، به ترتیب زمان ایجاد، عطف می‌دهد — ادامه از بزرگ‌ترین عطفِ
--   همان سال و واحد (هر دو قالب: جدید سال+واحد و قدیم واحد+سال؛ اسناد حذف‌شده هم حساب‌اند).
--   عطف‌های موجود (قالب قدیم) دست نمی‌خورند.
-- بخش ۲ (DDL): ایندکس یکتا روی ATF_NUM تا دو ذخیرهٔ هم‌زمان نتوانند یک شماره بگیرند
--   (دومی خطا می‌دهد و کاربر دوباره ذخیره می‌کند). اول بخش ۱ را اجرا و بررسی کنید.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

-- پیش‌نمایش (بدون تغییر): کدام سند چه عطفی می‌گیرد.
SELECT h.ID, h.YEAR, h.VAHEDCODE, h.DOC_NUM, h.CREATEDDATE,
       h.YEAR || h.VAHEDCODE || LPAD(NVL(m.MX, 0) + ROW_NUMBER() OVER (PARTITION BY h.YEAR, h.VAHEDCODE ORDER BY h.CREATEDDATE, h.ID), 7, '0') AS NEW_ATF
FROM TB_VOUCHERSHEAD h
LEFT JOIN (
    SELECT YEAR, VAHEDCODE, MAX(TO_NUMBER(SUBSTR(ATF_NUM, 9, 7))) MX
    FROM TB_VOUCHERSHEAD
    WHERE TRIM(ATF_NUM) IS NOT NULL AND REGEXP_LIKE(SUBSTR(ATF_NUM, 9, 7), '^[0-9]{7}$')
      AND (ATF_NUM LIKE YEAR || VAHEDCODE || '%' OR ATF_NUM LIKE VAHEDCODE || YEAR || '%')
    GROUP BY YEAR, VAHEDCODE
) m ON m.YEAR = h.YEAR AND m.VAHEDCODE = h.VAHEDCODE
WHERE TRIM(h.ATF_NUM) IS NULL AND LENGTH(h.YEAR) = 4 AND LENGTH(h.VAHEDCODE) = 4
ORDER BY h.YEAR, h.VAHEDCODE, NEW_ATF;

-- ── بخش ۱: پرکردن عطف اسناد بدون عطف ──
MERGE INTO TB_VOUCHERSHEAD t
USING (
    SELECT h.ID,
           h.YEAR || h.VAHEDCODE || LPAD(NVL(m.MX, 0) + ROW_NUMBER() OVER (PARTITION BY h.YEAR, h.VAHEDCODE ORDER BY h.CREATEDDATE, h.ID), 7, '0') AS NEW_ATF
    FROM TB_VOUCHERSHEAD h
    LEFT JOIN (
        SELECT YEAR, VAHEDCODE, MAX(TO_NUMBER(SUBSTR(ATF_NUM, 9, 7))) MX
        FROM TB_VOUCHERSHEAD
        WHERE TRIM(ATF_NUM) IS NOT NULL AND REGEXP_LIKE(SUBSTR(ATF_NUM, 9, 7), '^[0-9]{7}$')
          AND (ATF_NUM LIKE YEAR || VAHEDCODE || '%' OR ATF_NUM LIKE VAHEDCODE || YEAR || '%')
        GROUP BY YEAR, VAHEDCODE
    ) m ON m.YEAR = h.YEAR AND m.VAHEDCODE = h.VAHEDCODE
    WHERE TRIM(h.ATF_NUM) IS NULL AND LENGTH(h.YEAR) = 4 AND LENGTH(h.VAHEDCODE) = 4
) s
ON (t.ID = s.ID)
WHEN MATCHED THEN UPDATE SET t.ATF_NUM = s.NEW_ATF;

-- بررسی: نباید هیچ عطف تکراری باشد (خروجی خالی).
SELECT ATF_NUM, COUNT(*) FROM TB_VOUCHERSHEAD WHERE ATF_NUM IS NOT NULL GROUP BY ATF_NUM HAVING COUNT(*) > 1;

COMMIT;

-- ── بخش ۲: یکتایی (فقط وقتی بررسی بالا خالی بود) ──
CREATE UNIQUE INDEX UK_VOUCHERHEAD_ATF ON TB_VOUCHERSHEAD (ATF_NUM);
