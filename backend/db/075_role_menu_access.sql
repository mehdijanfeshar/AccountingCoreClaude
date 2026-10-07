--------------------------------------------------------------------------------------------------
-- 075_role_menu_access.sql
--
-- دسترسی نقش‌ها به منوها (فاز ۵۴، ۲۰۲۶-۱۰-۰۷): مدیر ستاد در صفحهٔ «دسترسی نقش‌ها» برای هر نقش (نقش‌های
-- سامانهٔ ورود / Keycloak) تعیین می‌کند هر منو را نبیند، فقط ببیند یا ثبت و تغییر هم بکند. سرور نوشتن
-- (Command) را با همین جدول کنترل می‌کند. هر ردیف = (نقش، منو).
--   ACCESS_LEVEL: 0 = بدون دسترسی، 1 = مشاهده، 2 = ثبت و تغییر.
-- تا این جدول خالی است (یا اجرا نشده)، رفتار ثابت قبلی (AppRoles) بی‌تغییر می‌ماند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_ROLE_MENU_ACCESS
(
    ID            CHAR(36)        NOT NULL,
    ROLE_NAME     VARCHAR2(100)   NOT NULL,
    MENU_KEY      VARCHAR2(100)   NOT NULL,
    ACCESS_LEVEL  NUMBER(2)       DEFAULT 0 NOT NULL,
    ADDUSERID     VARCHAR2(10)    NOT NULL,
    CREATEDDATE   TIMESTAMP       NOT NULL,
    CHANGEUSERID  VARCHAR2(10),
    UPDATEDDATE   TIMESTAMP,
    CONSTRAINT PK_ROLE_MENU_ACCESS PRIMARY KEY (ID),
    CONSTRAINT UK_ROLE_MENU_ACCESS UNIQUE (ROLE_NAME, MENU_KEY),
    CONSTRAINT CK_ROLE_MENU_ACCESS_LEVEL CHECK (ACCESS_LEVEL IN (0, 1, 2))
);

COMMENT ON TABLE TB_ROLE_MENU_ACCESS IS 'دسترسی هر نقش به هر منو (۰ بدون دسترسی، ۱ مشاهده، ۲ ثبت و تغییر)';
COMMENT ON COLUMN TB_ROLE_MENU_ACCESS.ROLE_NAME IS 'نام نقش، عین claim نقش در توکن (مثل FINANCIAL CORE USER)';
COMMENT ON COLUMN TB_ROLE_MENU_ACCESS.MENU_KEY IS 'کلید منو = مسیر صفحه در فرانت (مثل /operation/voucher-heads)';
