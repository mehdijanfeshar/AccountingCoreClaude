--------------------------------------------------------------------------------------------------
-- 045_petty_cash_review.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری"), chunk 2, part 1 — one new "side" table for RBAC.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- Same standing exception as backend/db/044_petty_cash.sql (docs/tankhah-khazaneh-module.md
-- §0/§3, decision dated 2026-09-27) — narrow, owner-approved, this module only. NOT executed
-- against any database yet; unlike 044, this script has not been run even on the dev database as
-- of authoring time.
--
-- Design reference: docs/tankhah-khazaneh-module.md, "تصمیم‌های بخش ۲" section below §8. Read
-- that section before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_PC_REVIEWER — "بررسی‌کنندهٔ تنخواه": which users may StartReview/Approve/Return/Reject a
-- given TB_REVOLVING_FUND's صورت‌هزینه documents. Deliberately NOT TB_PERSON_ACTION — that
-- table's USERID is a کد ملی (a different identity space from ICurrentUser.UserId/ADDUSERID) and
-- its OPERATORROLE is an organizational financial-signature role with no current consumer; see
-- design doc for the full rationale.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_REVIEWER
(
    ID                CHAR(36)      NOT NULL,
    REVOLVINGFUND_ID  CHAR(36)      NOT NULL,
    REVIEWER_USERID   VARCHAR2(10)  NOT NULL,  -- ICurrentUser.UserId/ADDUSERID identity space, NOT کد ملی
    REVIEWER_NAME     VARCHAR2(200),
    CREATEDDATE       TIMESTAMP     NOT NULL,
    UPDATEDDATE       TIMESTAMP,
    ADDUSERID         VARCHAR2(10)  NOT NULL,
    CHANGEUSERID      VARCHAR2(10),
    VAHEDCODE         VARCHAR2(4),
    YEAR              VARCHAR2(4),
    ISDELETED         NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_REVIEWER PRIMARY KEY (ID),
    CONSTRAINT UK_PC_REVIEWER UNIQUE (REVOLVINGFUND_ID, REVIEWER_USERID),
    CONSTRAINT FK_PC_REVIEWER_REVOLVING FOREIGN KEY (REVOLVINGFUND_ID)
        REFERENCES TB_REVOLVING_FUND (ID)
);

-- Backs the SoD/authorization check every StartReview/Approve/Return/Reject action runs
-- (IPettyCashReviewAuthorizer: "does this user have an active reviewer row for this fund").
CREATE INDEX IDX_PC_REVIEWER_FUND ON TB_PC_REVIEWER (REVOLVINGFUND_ID);
