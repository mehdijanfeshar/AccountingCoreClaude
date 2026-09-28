--------------------------------------------------------------------------------------------------
-- 050_petty_cash_settlement.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری") — بخش ۳-ب (۲۰۲۶-۰۹-۲۸ owner decision): تسویهٔ دوره و
-- صدور سند حسابداری (TB_PC_SETTLEMENT_PERIOD), تفصیلی حساب معین تنخواه (TB_PC_FUND_LINK_TAFSILI).
-- See docs/tankhah-khazaneh-module.md §۹ برای قاعده‌های کامل — این اسکریپت فقط schema است.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- Same standing rule as 044..049_petty_cash*.sql. NOT executed anywhere as of authoring time
-- (2026-09-28).
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- بخش ۱ — TB_PC_SETTLEMENT_PERIOD
--
-- یک دورهٔ تسویهٔ یک تنخواه (ماهانه/فصلی — TB_PC_FUND.SETTLEMENT_PERIOD). بدون DEFAULT sys_guid()
-- (ریسک #۱۱) — id همیشه application-side تولید می‌شود، مثل بقیهٔ TB_PC_*. VOUCHERSHEAD_ID فقط پس
-- از نهایی‌سازی (STATE=2) مقدار می‌گیرد.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_SETTLEMENT_PERIOD
(
    ID                    CHAR(36)      NOT NULL,
    FUND_ID               CHAR(36)      NOT NULL,
    PERIOD_START          VARCHAR2(8)   NOT NULL,  -- شمسی YYYYMMDD
    PERIOD_END            VARCHAR2(8)   NOT NULL,  -- شمسی YYYYMMDD
    OPENING_BALANCE       NUMBER(25)    NOT NULL,
    COUNTED_BALANCE       NUMBER(25),
    STATE                 NUMBER(1)     NOT NULL,  -- PettyCashSettlementState: 1=Draft 2=Final
    VOUCHERSHEAD_ID       CHAR(36),
    FINALIZED_BY_USERID   VARCHAR2(10),
    FINALIZED_DATE        TIMESTAMP,
    CREATEDDATE           TIMESTAMP     NOT NULL,
    UPDATEDDATE           TIMESTAMP,
    ADDUSERID             VARCHAR2(10)  NOT NULL,
    CHANGEUSERID          VARCHAR2(10),
    VAHEDCODE             VARCHAR2(4),
    YEAR                  VARCHAR2(4),
    ISDELETED             NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_SETTLEMENT_PERIOD PRIMARY KEY (ID),
    CONSTRAINT UK_PC_SETTLEMENT_PERIOD UNIQUE (FUND_ID, PERIOD_START, PERIOD_END),
    CONSTRAINT FK_PC_SETTLEMENT_PERIOD_FUND FOREIGN KEY (FUND_ID)
        REFERENCES TB_PC_FUND (ID),
    CONSTRAINT FK_PC_SETTLEMENT_PERIOD_VCHR FOREIGN KEY (VOUCHERSHEAD_ID)
        REFERENCES TB_VOUCHERSHEAD (ID)
);

CREATE INDEX IDX_PC_SETTLEMENT_PERIOD_FUND ON TB_PC_SETTLEMENT_PERIOD (FUND_ID, STATE);

--------------------------------------------------------------------------------------------------
-- بخش ۲ — TB_PC_FUND_LINK_TAFSILI
--
-- تفصیلی(های) حساب معین تنخواه — هم‌شکل TB_REVOLVINGFUND_LINK_TAFSILI Legacy، فقط FUND_ID به
-- TB_PC_FUND اشاره می‌کند (نه TB_REVOLVING_FUND) — تصمیم ۲۰۲۶-۰۹-۲۸ (§۰). جدول permanently-
-- embedded، مثل هر *_LINK_TAFSIL* دیگر — بدون AddAsync/GetForUpdateAsync مستقل خودش.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_FUND_LINK_TAFSILI
(
    ID             CHAR(36)      NOT NULL,
    FUND_ID        CHAR(36)      NOT NULL,
    TAFSILI_ID     CHAR(36)      NOT NULL,
    LEVEL_ID       CHAR(36)      NOT NULL,
    CREATEDDATE    TIMESTAMP     NOT NULL,
    UPDATEDDATE    TIMESTAMP,
    ADDUSERID      VARCHAR2(10)  NOT NULL,
    CHANGEUSERID   VARCHAR2(10),
    VAHEDCODE      VARCHAR2(4),
    YEAR           VARCHAR2(4),
    ISDELETED      NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_FUND_LINK_TAFSILI PRIMARY KEY (ID),
    CONSTRAINT FK_PC_FUND_LINK_TAFSILI_FUND FOREIGN KEY (FUND_ID)
        REFERENCES TB_PC_FUND (ID),
    CONSTRAINT FK_PC_FUND_LINK_TAFSILI_TAF FOREIGN KEY (TAFSILI_ID)
        REFERENCES TB_TAFSILI (ID),
    CONSTRAINT FK_PC_FUND_LINK_TAFSILI_LVL FOREIGN KEY (LEVEL_ID)
        REFERENCES TB_LEVEL_TAFSIL (ID)
);

CREATE INDEX IDX_PC_FUND_LINK_TAFSILI_FUND ON TB_PC_FUND_LINK_TAFSILI (FUND_ID);

--------------------------------------------------------------------------------------------------
-- بخش ۳ — PettyCashDocAction.Settle=11 نیاز به تغییر schema ندارد (TB_PC_DOC_EVENT.ACTION از قبل
-- NUMBER است، مقدار جدید همان ستون موجود را استفاده می‌کند).
--------------------------------------------------------------------------------------------------
