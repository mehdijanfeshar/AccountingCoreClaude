--------------------------------------------------------------------------------------------------
-- 049_petty_cash_replenishment.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری") — بخش ۳-الف (۲۰۲۶-۰۹-۲۸ owner decision): درخواست
-- ترمیم/شارژ (TB_PC_REPLENISHMENT), استرداد وجه (TB_PC_REFUND), و ستون جدید
-- TB_PC_FUND.REFUND_RECORDER. See docs/tankhah-khazaneh-module.md, بخش «بخش ۳ — طراحی
-- (۲۰۲۶-۰۹-۲۸)» for the full rule set — this script is schema-only.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- Same standing rule as 044/045/046/047/048_petty_cash*.sql. NOT executed anywhere as of
-- authoring time (2026-09-28).
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- بخش ۱ — TB_PC_REPLENISHMENT
--
-- «ترمیم/شارژ تنخواه» (RCH-xxxxx) — ۱:۱ با TB_CHARGEANDCOST_HEAD نوع ۱ (Charge). بدون
-- DEFAULT sys_guid() (ریسک #۱۱) — id همیشه application-side تولید می‌شود، مثل بقیهٔ TB_PC_*.
-- حساب بانکی مبدأ روی خودِ سرسند Legacy (ACCOUNT_ID) نگه داشته می‌شود، نه اینجا — رجوع به
-- docs/tankhah-khazaneh-module.md، «بخش ۳ — طراحی».
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_REPLENISHMENT
(
    ID                    CHAR(36)      NOT NULL,
    CHARGEANDCOSTHEAD_ID  CHAR(36)      NOT NULL,
    FUND_ID               CHAR(36)      NOT NULL,
    CODE                  VARCHAR2(50)  NOT NULL,  -- "RCH-" + شماره
    PAYMENT_METHOD        NUMBER(1)     NOT NULL,  -- PettyCashPaymentMethod: 1=پایا 2=چک 3=نقد 4=سایر
    STATE                 NUMBER(2)     NOT NULL,  -- PettyCashReplenishmentState: 1..5
    TOTAL_AMOUNT          NUMBER(25)    NOT NULL,
    NOTE                  VARCHAR2(1000),
    PAID_DATE             TIMESTAMP,
    PAID_BY_USERID        VARCHAR2(10),
    APPROVED_BY_USERID    VARCHAR2(10),
    CREATEDDATE           TIMESTAMP     NOT NULL,
    UPDATEDDATE           TIMESTAMP,
    ADDUSERID             VARCHAR2(10)  NOT NULL,
    CHANGEUSERID          VARCHAR2(10),
    VAHEDCODE             VARCHAR2(4),
    YEAR                  VARCHAR2(4),
    ISDELETED             NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_REPLENISHMENT PRIMARY KEY (ID),
    CONSTRAINT UK_PC_REPLENISHMENT UNIQUE (CHARGEANDCOSTHEAD_ID),
    CONSTRAINT FK_PC_REPLENISHMENT_HEAD FOREIGN KEY (CHARGEANDCOSTHEAD_ID)
        REFERENCES TB_CHARGEANDCOST_HEAD (ID),
    CONSTRAINT FK_PC_REPLENISHMENT_FUND FOREIGN KEY (FUND_ID)
        REFERENCES TB_PC_FUND (ID)
);

CREATE INDEX IDX_PC_REPLENISHMENT_FUND_STATE ON TB_PC_REPLENISHMENT (FUND_ID, STATE);

--------------------------------------------------------------------------------------------------
-- بخش ۲ — TB_PC_REFUND
--
-- «استرداد وجه تنخواه» (REF-xxxxx) — بدون معادل Legacy. چه کسی مجاز به ثبت است را
-- TB_PC_FUND.REFUND_RECORDER (بخش ۳ زیر) تعیین می‌کند.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_REFUND
(
    ID                   CHAR(36)      NOT NULL,
    FUND_ID              CHAR(36)      NOT NULL,
    CODE                 VARCHAR2(50)  NOT NULL,  -- "REF-" + شماره
    AMOUNT               NUMBER(25)    NOT NULL,
    REASON               VARCHAR2(500),
    REFUND_DATE          VARCHAR2(8)   NOT NULL,  -- شمسی YYYYMMDD
    RECORDED_BY_USERID   VARCHAR2(10)  NOT NULL,
    CREATEDDATE          TIMESTAMP     NOT NULL,
    UPDATEDDATE          TIMESTAMP,
    ADDUSERID            VARCHAR2(10)  NOT NULL,
    CHANGEUSERID         VARCHAR2(10),
    VAHEDCODE            VARCHAR2(4),
    YEAR                 VARCHAR2(4),
    ISDELETED            NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_REFUND PRIMARY KEY (ID),
    CONSTRAINT FK_PC_REFUND_FUND FOREIGN KEY (FUND_ID)
        REFERENCES TB_PC_FUND (ID)
);

CREATE INDEX IDX_PC_REFUND_FUND ON TB_PC_REFUND (FUND_ID);

--------------------------------------------------------------------------------------------------
-- بخش ۳ — TB_PC_FUND.REFUND_RECORDER
--
-- چه نقشی مجاز به ثبت استرداد وجه روی این تنخواه است (PettyCashRefundRecorder: 1=تنخواه‌دار،
-- 2=خزانه‌دار، 3=حسابدار ارشد، 4=مدیر مالی). پیش‌فرض ۲ (خزانه‌دار). صاحب پروژه: نباید در کد
-- ثابت باشد؛ مدیر مالی در تعریف/ویرایش تنخواه تعیین می‌کند.
--------------------------------------------------------------------------------------------------
ALTER TABLE TB_PC_FUND ADD REFUND_RECORDER NUMBER(1) DEFAULT 2 NOT NULL;
