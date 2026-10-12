--------------------------------------------------------------------------------------------------
-- 077_audit_log.sql
--
-- لاگ تغییرات (۲۰۲۶-۱۰-۱۲): هر ثبت/اصلاح/حذف/بازگردانی در هر جدول، با کاربر (کد ملی)، نام، ساعت، IP، مقدار
-- قبلی و جدید، در TB_AUDITLOG (AuditLogInterceptor). فقط نقش «FINANCIAL CORE AUDIT» (مدیر مالی) می‌بیند.
--
-- بخش ۱: اگر TB_AUDITLOG نیست، ساخته می‌شود؛ ستون‌های تازه اضافه می‌شوند (IP، شناسهٔ ذخیره، نام کاربر،
--        برچسب رکورد مثل شمارهٔ سند، شناسهٔ سرسند برای ردیف‌ها).
-- بخش ۲: کلید اصلی و ایندکس‌های جست‌وجو (واحد+زمان، رکورد، والد، کاربر).
-- بخش ۳: تریگر «فقط درج» — اصلاح یا حذف ردیف لاگ خطا می‌دهد، حتی از SQL مستقیم.
--
-- ⚠️ تا این اسکریپت اجرا نشده، هر ذخیره‌ای در برنامه خطا می‌دهد (ORA-00904)، مگر Audit:Enabled=false در
--    appsettings. اول این اسکریپت، بعد استقرار برنامه.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

-- ── بخش ۱: جدول و ستون‌ها ──
DECLARE
    v_count NUMBER;

    PROCEDURE add_column(p_column VARCHAR2, p_definition VARCHAR2) IS
        v_exists NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_exists FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'TB_AUDITLOG' AND COLUMN_NAME = p_column;
        IF v_exists = 0 THEN
            EXECUTE IMMEDIATE 'ALTER TABLE TB_AUDITLOG ADD (' || p_column || ' ' || p_definition || ')';
        END IF;
    END;
BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_TABLES WHERE TABLE_NAME = 'TB_AUDITLOG';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE '
            CREATE TABLE TB_AUDITLOG (
                ID          CHAR(36)       NOT NULL,
                TABLENAME   VARCHAR2(255)  NOT NULL,
                RECORDID    CHAR(36)       NOT NULL,
                COLUMNNAME  VARCHAR2(255),
                OLDVALUE    CLOB,
                NEWVALUE    CLOB,
                ACTIONTYPE  VARCHAR2(50)   NOT NULL,
                USERID      VARCHAR2(10)   NOT NULL,
                VAHEDCODE   VARCHAR2(4),
                YEAR        VARCHAR2(4),
                CHANGEDATE  TIMESTAMP(6)   DEFAULT SYSTIMESTAMP
            )';
    END IF;

    add_column('IPADDRESS', 'VARCHAR2(45)');
    add_column('OPERATIONID', 'CHAR(36)');
    add_column('USERFULLNAME', 'NVARCHAR2(200)');
    add_column('RECORDLABEL', 'NVARCHAR2(200)');
    add_column('PARENTID', 'CHAR(36)');
END;
/

-- ── بخش ۲: کلید و ایندکس‌ها ──
DECLARE
    v_count NUMBER;

    PROCEDURE create_index(p_name VARCHAR2, p_columns VARCHAR2) IS
        v_exists NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_exists FROM USER_INDEXES WHERE INDEX_NAME = p_name;
        IF v_exists = 0 THEN
            EXECUTE IMMEDIATE 'CREATE INDEX ' || p_name || ' ON TB_AUDITLOG (' || p_columns || ')';
        END IF;
    END;
BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_CONSTRAINTS WHERE TABLE_NAME = 'TB_AUDITLOG' AND CONSTRAINT_TYPE = 'P';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE TB_AUDITLOG ADD CONSTRAINT PK_AUDITLOG PRIMARY KEY (ID)';
    END IF;

    create_index('IX_AUDITLOG_VAHED_DATE', 'VAHEDCODE, CHANGEDATE');
    create_index('IX_AUDITLOG_RECORD', 'RECORDID');
    create_index('IX_AUDITLOG_PARENT', 'PARENTID');
    create_index('IX_AUDITLOG_USER_DATE', 'USERID, CHANGEDATE');
    create_index('IX_AUDITLOG_OPERATION', 'OPERATIONID');
END;
/

-- ── بخش ۳: فقط درج ──
CREATE OR REPLACE TRIGGER TRG_AUDITLOG_APPEND_ONLY
BEFORE UPDATE OR DELETE ON TB_AUDITLOG
BEGIN
    RAISE_APPLICATION_ERROR(-20077, 'لاگ تغییرات قابل اصلاح یا حذف نیست.');
END;
/

-- بررسی: ستون‌های تازه و تریگر.
SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'TB_AUDITLOG' ORDER BY COLUMN_ID;
SELECT TRIGGER_NAME, STATUS FROM USER_TRIGGERS WHERE TABLE_NAME = 'TB_AUDITLOG';
