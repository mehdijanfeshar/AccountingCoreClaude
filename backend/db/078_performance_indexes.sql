-- ═══════════════════════════════════════════════════════════════════════════
-- 078 — ایندکس‌های کارایی برای حجم عملیاتی (میلیون‌ها سطر سند)
-- فقط به دست صاحب پروژه اجرا شود. ایمن برای اجرای مجدد: اگر ایندکسی با همین نام
-- یا با همین فهرست ستون‌ها از قبل باشد، ساخته نمی‌شود.
-- پیش از اجرا در محیط واقعی: ساخت ایندکس روی جدول بزرگ را در ساعت کم‌بار انجام بدهید
-- (ONLINE اضافه شده) و پس از آن آمار را جمع کنید (بخش آخر).
-- پشتیبانی: تراز آزمایشی، دفتر کل، دفتر روزنامه، مرور اسناد/حساب، ماتریسی، صورت‌های مالی.
-- ═══════════════════════════════════════════════════════════════════════════

DECLARE
    PROCEDURE create_index(p_name VARCHAR2, p_table VARCHAR2, p_columns VARCHAR2) IS
        v_by_name NUMBER;
        v_same_cols NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_by_name FROM USER_INDEXES WHERE INDEX_NAME = p_name;

        -- ایندکس دیگری با همین ستون‌ها و همین ترتیب؟
        SELECT COUNT(*) INTO v_same_cols FROM (
            SELECT i.INDEX_NAME,
                   LISTAGG(c.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY c.COLUMN_POSITION) AS COLS
            FROM USER_INDEXES i
            JOIN USER_IND_COLUMNS c ON c.INDEX_NAME = i.INDEX_NAME
            WHERE i.TABLE_NAME = p_table
            GROUP BY i.INDEX_NAME
        ) WHERE COLS = p_columns;

        IF v_by_name = 0 AND v_same_cols = 0 THEN
            EXECUTE IMMEDIATE 'CREATE INDEX ' || p_name || ' ON ' || p_table || ' (' || p_columns || ') ONLINE';
        END IF;
    END;
BEGIN
    -- ── سر سند: فیلتر استاندارد همهٔ گزارش‌ها (سال + واحد + حذف‌نشده + تاریخ) ──
    create_index('IX_VHEAD_YEAR_VAHED_DATE', 'TB_VOUCHERSHEAD', 'YEAR, VAHEDCODE, ISDELETED, DATE_DOC');
    create_index('IX_VHEAD_YEAR_VAHED_DOCNUM', 'TB_VOUCHERSHEAD', 'YEAR, VAHEDCODE, DOC_NUM');

    -- ── ردیف سند: join به سر سند و به حساب ──
    create_index('IX_VDETAIL_HEAD', 'TB_VOUCHERSDETAIL', 'VOUCHERSHEAD_ID');
    create_index('IX_VDETAIL_ACCOUNT_HEAD', 'TB_VOUCHERSDETAIL', 'ACCOUNT_ID, VOUCHERSHEAD_ID');
    create_index('IX_VDETAIL_VAHED_ACCOUNT', 'TB_VOUCHERSDETAIL', 'VAHEDCODE, ACCOUNT_ID');

    -- ── پیوند ردیف سند به تفصیلی/شناسه (مغایرت‌گیری و گزارش‌های تفصیلی) ──
    create_index('IX_VDLINK_TAFSILI_DETAIL', 'TB_VOUCHERDETAIL_LINK_TAFSILI', 'TAFSILI_ID, VOUCHERSDETAIL_ID');
    create_index('IX_VDLINK_DETAIL', 'TB_VOUCHERDETAIL_LINK_TAFSILI', 'VOUCHERSDETAIL_ID');

    -- ── کدینگ: پیمایش سلسله‌مراتب (self-join سه‌سطحی) ──
    create_index('IX_ACCOUNTCODE_PARENT', 'TB_ACCOUNTCODE', 'PARENTID');
    create_index('IX_ACCOUNTCODE_CODE', 'TB_ACCOUNTCODE', 'ACCCODE');
END;
/

-- ── آمار برای Optimizer (بعد از ساخت ایندکس‌ها) ──
BEGIN
    DBMS_STATS.GATHER_TABLE_STATS(USER, 'TB_VOUCHERSHEAD', CASCADE => TRUE);
    DBMS_STATS.GATHER_TABLE_STATS(USER, 'TB_VOUCHERSDETAIL', CASCADE => TRUE);
    DBMS_STATS.GATHER_TABLE_STATS(USER, 'TB_VOUCHERDETAIL_LINK_TAFSILI', CASCADE => TRUE);
    DBMS_STATS.GATHER_TABLE_STATS(USER, 'TB_ACCOUNTCODE', CASCADE => TRUE);
END;
/
