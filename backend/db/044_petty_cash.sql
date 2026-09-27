--------------------------------------------------------------------------------------------------
-- 044_petty_cash.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری"), chunk 1 — three new "side" tables.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- This project's standing rule is "no new tables" — this script is an explicit, narrow,
-- owner-approved exception for this module only (docs/tankhah-khazaneh-module.md §0/§3,
-- decision dated 2026-09-27). Every other table in this schema (CENTRALACCOUNT) was reverse-
-- engineered from a live, pre-existing Oracle database; this script is the first (and, as of
-- chunk 1, only) DDL authored by this codebase. EXECUTED on the dev database (setadidevdb,
-- schema CENTRALACCOUNT) on 2026-09-27 with explicit owner approval. Not run anywhere else.
--
-- Naming: prefix TB_PC_ ("Petty Cash" / تنخواه), matching the TB_XXX convention of every other
-- table in this schema, to avoid signalling that these rows are somehow less first-class.
--
-- Design reference: docs/tankhah-khazaneh-module.md §2 (states), §3 (this DDL's source), §4
-- (server-side rules these tables support). Read that file before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_PC_FUND_SETTING — "تنظیمات تنخواه": one row per TB_REVOLVING_FUND (1:1), holding the
-- per-document ceiling, the alert threshold, the custodian ("تنخواه‌دار"), and the settlement
-- period. None of this has anywhere to live on TB_REVOLVING_FUND itself.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_FUND_SETTING
(
    ID                       CHAR(36)      NOT NULL,
    REVOLVINGFUND_ID         CHAR(36)      NOT NULL,
    CUSTODIAN_USERID         VARCHAR2(10),
    CUSTODIAN_NAME           VARCHAR2(200),
    PER_DOC_LIMIT            NUMBER(25),
    ALERT_THRESHOLD_PERCENT  NUMBER(3),
    SETTLEMENT_PERIOD        NUMBER(1),     -- 1=ماهانه, 2=فصلی (PettyCashSettlementPeriod)
    CREATEDDATE              TIMESTAMP     NOT NULL,
    UPDATEDDATE              TIMESTAMP,
    ADDUSERID                VARCHAR2(10)  NOT NULL,
    CHANGEUSERID             VARCHAR2(10),
    VAHEDCODE                VARCHAR2(4),
    YEAR                     VARCHAR2(4),
    ISDELETED                NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_FUND_SETTING PRIMARY KEY (ID),
    CONSTRAINT UK_PC_FUND_SETTING UNIQUE (REVOLVINGFUND_ID),
    CONSTRAINT FK_PC_FUND_SETTING_REVOLVING FOREIGN KEY (REVOLVINGFUND_ID)
        REFERENCES TB_REVOLVING_FUND (ID)
);

--------------------------------------------------------------------------------------------------
-- TB_PC_EXPENSE_DOC — "صورت‌هزینهٔ تنخواه" (TH-xxxxx): one row per Legacy
-- TB_CHARGEANDCOST_HEAD of type هزینه‌کرد (1:1), holding everything the Legacy head/detail pair
-- has no column for — vendor, invoice, VAT, the seven-value کارتابل state, and which تنخواه the
-- expense is drawn from (TB_CHARGEANDCOST_DETAIL.REVOLVINGFUND_ID is always NULL on existing
-- live rows — see design doc §1 — so this column is the real source of truth).
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_EXPENSE_DOC
(
    ID                    CHAR(36)      NOT NULL,
    CHARGEANDCOSTHEAD_ID  CHAR(36)      NOT NULL,
    REVOLVINGFUND_ID      CHAR(36)      NOT NULL,
    DOC_STATE             NUMBER(2)     NOT NULL,  -- PettyCashDocState, 1..7 (design §2)
    VENDOR_NAME           VARCHAR2(200),
    VENDOR_NATIONAL_ID    VARCHAR2(11),
    INVOICE_NO            VARCHAR2(50),
    INVOICE_DATE          VARCHAR2(8),             -- YYYYMMDD Jalali, project-wide convention
    EVIDENCE_TYPE         NUMBER(1),               -- 1=فاکتور رسمی, 2=رسید, 3=سایر
    AMOUNT_BEFORE_TAX     NUMBER(25),
    VAT_AMOUNT            NUMBER(25),
    SUBMITTED_DATE        TIMESTAMP,
    RETURN_DEADLINE       VARCHAR2(8),
    CREATEDDATE           TIMESTAMP     NOT NULL,
    UPDATEDDATE           TIMESTAMP,
    ADDUSERID             VARCHAR2(10)  NOT NULL,
    CHANGEUSERID          VARCHAR2(10),
    VAHEDCODE             VARCHAR2(4),
    YEAR                  VARCHAR2(4),
    ISDELETED             NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_EXPENSE_DOC PRIMARY KEY (ID),
    CONSTRAINT UK_PC_EXPENSE_DOC UNIQUE (CHARGEANDCOSTHEAD_ID),
    CONSTRAINT FK_PC_EXPENSE_DOC_HEAD FOREIGN KEY (CHARGEANDCOSTHEAD_ID)
        REFERENCES TB_CHARGEANDCOST_HEAD (ID),
    CONSTRAINT FK_PC_EXPENSE_DOC_REVOLVING FOREIGN KEY (REVOLVINGFUND_ID)
        REFERENCES TB_REVOLVING_FUND (ID)
);

-- Backs the کارتابل list's (unit, year, state) filter/count and the funds-list balance
-- aggregation's per-state SUM/COUNT (design §2's "موجودی نقد" formula).
CREATE INDEX IDX_PC_EXPENSE_DOC_STATE ON TB_PC_EXPENSE_DOC (VAHEDCODE, YEAR, DOC_STATE);

-- Backs GetPettyCashFundsQuery's per-fund exposure grouping and the Submit balance check.
CREATE INDEX IDX_PC_EXPENSE_DOC_FUND ON TB_PC_EXPENSE_DOC (REVOLVINGFUND_ID);

--------------------------------------------------------------------------------------------------
-- TB_PC_DOC_EVENT — "گردش عملیات" (audit trail). Insert-only: no update, no delete, ever.
-- One row per state-changing action on a TB_PC_EXPENSE_DOC row (design §4's last rule), written
-- in the SAME transaction as the action itself.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_DOC_EVENT
(
    ID              CHAR(36)      NOT NULL,
    EXPENSE_DOC_ID  CHAR(36)      NOT NULL,
    ACTION          NUMBER(2),               -- PettyCashDocAction
    FROM_STATE      NUMBER(2),               -- PettyCashDocState, nullable (Create has no "from")
    TO_STATE        NUMBER(2),               -- PettyCashDocState
    NOTE            VARCHAR2(1000),
    RETURN_REASONS  VARCHAR2(200),           -- comma-separated reason codes, بخش ۲ only
    CLIENT_IP       VARCHAR2(45),            -- IPv4 or IPv6 text
    CREATEDDATE     TIMESTAMP     NOT NULL,
    UPDATEDDATE     TIMESTAMP,
    ADDUSERID       VARCHAR2(10)  NOT NULL,
    CHANGEUSERID    VARCHAR2(10),
    VAHEDCODE       VARCHAR2(4),
    YEAR            VARCHAR2(4),
    ISDELETED       NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_DOC_EVENT PRIMARY KEY (ID),
    CONSTRAINT FK_PC_DOC_EVENT_EXPENSEDOC FOREIGN KEY (EXPENSE_DOC_ID)
        REFERENCES TB_PC_EXPENSE_DOC (ID)
);

-- Backs GET .../expense-docs/{id}/events, ordered by CREATEDDATE.
CREATE INDEX IDX_PC_DOC_EVENT_EXPENSEDOC ON TB_PC_DOC_EVENT (EXPENSE_DOC_ID);
