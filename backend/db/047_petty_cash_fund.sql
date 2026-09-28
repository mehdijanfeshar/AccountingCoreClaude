--------------------------------------------------------------------------------------------------
-- 047_petty_cash_fund.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری") — 2026-09-28 owner decision: give this module its own,
-- fully independent تنخواه table (TB_PC_FUND), replacing TB_REVOLVING_FUND/TB_PC_FUND_SETTING for
-- this module entirely. This is a SECOND, narrower exception layered on top of the original
-- "no new tables" one (docs/tankhah-khazaneh-module.md §0, decision dated 2026-09-27) — the owner
-- explicitly does not want this module's تنخواه rows to live anywhere near TB_REVOLVING_FUND
-- anymore, including its دادهٔ تست so far.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- Same standing rule as backend/db/044_petty_cash.sql and 045_petty_cash_review.sql. NOT executed
-- anywhere as of authoring time (2026-09-28) — unlike 044, which was run on the dev database.
--
-- Design reference: docs/tankhah-khazaneh-module.md §0 ("تصمیم ۲۰۲۶-۰۹-۲۸: جدول مستقل تنخواه").
-- Read that section before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- بخش ۱ — پاک‌سازی دادهٔ تست این ماژول
--
-- ⚠️⚠️ مخرب است — حذف فیزیکی، نه نرم. ⚠️⚠️ هر ردیف موجود در این ماژول (TB_PC_EXPENSE_DOC و همهٔ
-- جدول‌های وابسته به آن، به‌علاوهٔ TB_PC_REVIEWER) به یکی از تنخواه‌های قدیمی (TB_REVOLVING_FUND)
-- اشاره دارد که دیگر برای این ماژول استفاده نمی‌شود؛ همه آشکارا دادهٔ تست‌اند (فاز ۴۴، هنوز به
-- تولید نرسیده). فقط ردیف‌های Legacy (TB_CHARGEANDCOST_HEAD/DETAIL) که از طریق
-- TB_PC_EXPENSE_DOC.CHARGEANDCOSTHEAD_ID به این ماژول تعلق دارند حذف می‌شوند — نه هیچ ردیف Legacy
-- دیگری.
--
-- ترتیب حذف باید FKها را رعایت کند: TB_PC_EXPENSE_DOC خودش به TB_CHARGEANDCOST_HEAD ارجاع دارد
-- (FK_PC_EXPENSE_DOC_HEAD)، پس هزینه‌کردهای Legacy را نمی‌توان پیش از حذف TB_PC_EXPENSE_DOC حذف
-- کرد — اما همان لحظه که TB_PC_EXPENSE_DOC حذف شود، دیگر نمی‌دانیم کدام HEADها متعلق به این ماژول
-- بودند. راه‌حل: idهای HEAD را پیش از حذف TB_PC_EXPENSE_DOC در یک جدول موقت نگه می‌داریم.
--------------------------------------------------------------------------------------------------

-- idهای TB_CHARGEANDCOST_HEAD متعلق به این ماژول را پیش از هر حذفی نگه می‌داریم.
CREATE TABLE TMP_PC_CLEANUP_HEADS AS
SELECT CHARGEANDCOSTHEAD_ID AS ID FROM TB_PC_EXPENSE_DOC;

-- گردش عملیات و پیوست‌ها، هر دو FK مستقیم به TB_PC_EXPENSE_DOC دارند — اول اینها.
DELETE FROM TB_PC_DOC_EVENT;
DELETE FROM TB_PC_ATTACHMENT;

-- بررسی‌کنندگان به REVOLVINGFUND_ID (تنخواهٔ قدیمی) ارجاع دارند، نه به TB_PC_EXPENSE_DOC — مستقل
-- حذف می‌شوند؛ همه به تنخواه‌های قدیمی اشاره دارند و در جدول جدید بی‌معنی‌اند.
DELETE FROM TB_PC_REVIEWER;

-- دیتیل Legacy قبل از هد (FK استاندارد TB_CHARGEANDCOST_DETAIL -> TB_CHARGEANDCOST_HEAD).
DELETE FROM TB_CHARGEANDCOST_DETAIL WHERE CHARGEANDCOSTHEAD_ID IN (SELECT ID FROM TMP_PC_CLEANUP_HEADS);

-- خودِ TB_PC_EXPENSE_DOC — بعد از این، FK_PC_EXPENSE_DOC_HEAD دیگر مانع حذف HEAD نیست.
DELETE FROM TB_PC_EXPENSE_DOC;

-- در آخر، هدهای Legacy — با استفاده از idهای نگه‌داشته‌شده، نه یک subquery زندهٔ روی
-- TB_PC_EXPENSE_DOC (که دیگر خالی است).
DELETE FROM TB_CHARGEANDCOST_HEAD WHERE ID IN (SELECT ID FROM TMP_PC_CLEANUP_HEADS);

DROP TABLE TMP_PC_CLEANUP_HEADS;

--------------------------------------------------------------------------------------------------
-- بخش ۲ — حذف TB_PC_FUND_SETTING
--
-- دادهٔ آن (تنخواه‌دار، سقف هر سند، آستانهٔ هشدار، دورهٔ تسویه) به ستون‌های خودِ TB_PC_FUND ادغام
-- می‌شود؛ دیگر جدول جانبی جدا لازم نیست.
--------------------------------------------------------------------------------------------------
DROP TABLE TB_PC_FUND_SETTING;

--------------------------------------------------------------------------------------------------
-- بخش ۳ — TB_PC_FUND
--
-- «تنخواه» — تعریف کامل و مستقل این ماژول. جایگزین TB_REVOLVING_FUND برای این ماژول (که خودش
-- دست‌نخورده برای بقیهٔ پروژه می‌ماند) + TB_PC_FUND_SETTING (که این‌جا ادغام شد). بدون
-- DEFAULT sys_guid() (ریسک #۱۱) — id همیشه application-side تولید می‌شود، مثل بقیهٔ TB_PC_*.
-- UNIQUE فقط روی (VAHEDCODE, CODE) — بدون YEAR، چون تنخواه نهادی پایاست، نه چیزی که هرسال از نو
-- تعریف شود.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_FUND
(
    ID                       CHAR(36)      NOT NULL,
    CODE                     VARCHAR2(50)  NOT NULL,
    NAME                     VARCHAR2(200) NOT NULL,
    CUSTODIAN_USERID         VARCHAR2(10)  NOT NULL,  -- ICurrentUser.UserId/ADDUSERID identity space
    CUSTODIAN_NAME           VARCHAR2(200),
    CEILING                  NUMBER(25)    NOT NULL,  -- سقف تنخواه (قبلاً TB_REVOLVING_FUND.DEFAULTAMOUNT)
    PER_DOC_LIMIT            NUMBER(25)    NOT NULL,  -- سقف هر سند (قبلاً اختیاری در TB_PC_FUND_SETTING؛ اینجا الزامی)
    ALERT_THRESHOLD_PERCENT  NUMBER(3),
    ACCOUNTCODE_ID           CHAR(36),
    SETTLEMENT_PERIOD        NUMBER(1),               -- 1=ماهانه, 2=فصلی (PettyCashSettlementPeriod)
    IS_ACTIVE                NUMBER(1)     DEFAULT 1 NOT NULL,
    CREATEDDATE              TIMESTAMP     NOT NULL,
    UPDATEDDATE              TIMESTAMP,
    ADDUSERID                VARCHAR2(10)  NOT NULL,
    CHANGEUSERID             VARCHAR2(10),
    VAHEDCODE                VARCHAR2(4),
    YEAR                     VARCHAR2(4),
    ISDELETED                NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_FUND PRIMARY KEY (ID),
    CONSTRAINT UK_PC_FUND_CODE UNIQUE (VAHEDCODE, CODE),
    CONSTRAINT FK_PC_FUND_ACCOUNTCODE FOREIGN KEY (ACCOUNTCODE_ID)
        REFERENCES TB_ACCOUNTCODE (ID)
);

--------------------------------------------------------------------------------------------------
-- بخش ۴ — TB_PC_EXPENSE_DOC و TB_PC_REVIEWER: از TB_REVOLVING_FUND به TB_PC_FUND
--
-- در هر دو جدول، REVOLVINGFUND_ID به FUND_ID تغییر نام می‌یابد و FK قدیمی (به TB_REVOLVING_FUND)
-- با یک FK جدید (به TB_PC_FUND) جایگزین می‌شود. توجه: RENAME COLUMN در اوراکل ارجاع ستون را در
-- ایندکس‌ها و Constraintهای همان جدول هم به‌طور خودکار به‌روز می‌کند — IDX_PC_EXPENSE_DOC_FUND و
-- IDX_PC_REVIEWER_FUND نیازی به بازسازی ندارند؛ UK_PC_REVIEWER اینجا صرفاً برای وضوح صریح Drop و
-- دوباره Add می‌شود (رفتارش با رفتار ضمنی rename یکسان است).
--------------------------------------------------------------------------------------------------

-- TB_PC_EXPENSE_DOC
ALTER TABLE TB_PC_EXPENSE_DOC DROP CONSTRAINT FK_PC_EXPENSE_DOC_REVOLVING;
ALTER TABLE TB_PC_EXPENSE_DOC RENAME COLUMN REVOLVINGFUND_ID TO FUND_ID;
ALTER TABLE TB_PC_EXPENSE_DOC ADD CONSTRAINT FK_PC_EXPENSE_DOC_FUND FOREIGN KEY (FUND_ID)
    REFERENCES TB_PC_FUND (ID);

-- TB_PC_REVIEWER
ALTER TABLE TB_PC_REVIEWER DROP CONSTRAINT UK_PC_REVIEWER;
ALTER TABLE TB_PC_REVIEWER DROP CONSTRAINT FK_PC_REVIEWER_REVOLVING;
ALTER TABLE TB_PC_REVIEWER RENAME COLUMN REVOLVINGFUND_ID TO FUND_ID;
ALTER TABLE TB_PC_REVIEWER ADD CONSTRAINT FK_PC_REVIEWER_FUND FOREIGN KEY (FUND_ID)
    REFERENCES TB_PC_FUND (ID);
ALTER TABLE TB_PC_REVIEWER ADD CONSTRAINT UK_PC_REVIEWER UNIQUE (FUND_ID, REVIEWER_USERID);
