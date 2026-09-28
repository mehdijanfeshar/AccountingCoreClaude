--------------------------------------------------------------------------------------------------
-- 046_petty_cash_attachment.sql
--
-- Petty-cash module ("تنخواه و خزانه‌داری"), chunk 2, part 2-ب — file attachments on a
-- صورت‌هزینه.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
-- Same standing exception as backend/db/044_petty_cash.sql and 045_petty_cash_review.sql
-- (docs/tankhah-khazaneh-module.md §0/§3, decision dated 2026-09-27) — narrow, owner-approved,
-- this module only. NOT executed against any database yet, including the dev database.
--
-- Design reference: docs/tankhah-khazaneh-module.md, "تصمیم‌های بخش ۲" section (پیوست), below §8.
-- Read that section before changing this script.
--------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------
-- TB_PC_ATTACHMENT — file attachments on a TB_PC_EXPENSE_DOC (صورت‌هزینه). BLOB stored in the
-- database, same shape as the Legacy TB_ATTACH table — deliberately NOT a new column on TB_ATTACH
-- itself (that table is keyed to VOUCHERSHEAD_ID/TAFSILI_ID/PAYRECEIVE_ID, none of which apply
-- here) and NOT filesystem/blob storage, which has no precedent in this project. Only addable/
-- removable while the parent document is Draft or Returned (DOC_STATE 1 or 4) — enforced in
-- Accounting.Application, not here; see PettyCashDocEditability.
--------------------------------------------------------------------------------------------------
CREATE TABLE TB_PC_ATTACHMENT
(
    ID              CHAR(36)      NOT NULL,
    EXPENSE_DOC_ID  CHAR(36)      NOT NULL,
    ATTACH_NAME     VARCHAR2(255) NOT NULL,
    ATTACH_SIZE     NUMBER(10)    NOT NULL,  -- bytes; capped at 10 MiB in FluentValidation, not here
    CONTENT_TYPE    VARCHAR2(100),
    ATTACH_FILE     BLOB          NOT NULL,
    ATTACH_RADIF    NUMBER(2)     NOT NULL,  -- display order within the document; max(existing)+1
    CREATEDDATE     TIMESTAMP     NOT NULL,
    UPDATEDDATE     TIMESTAMP,
    ADDUSERID       VARCHAR2(10)  NOT NULL,
    CHANGEUSERID    VARCHAR2(10),
    VAHEDCODE       VARCHAR2(4),
    YEAR            VARCHAR2(4),
    ISDELETED       NUMBER(1)     DEFAULT 0 NOT NULL,
    CONSTRAINT PK_PC_ATTACHMENT PRIMARY KEY (ID),
    CONSTRAINT FK_PC_ATTACHMENT_EXPENSEDOC FOREIGN KEY (EXPENSE_DOC_ID)
        REFERENCES TB_PC_EXPENSE_DOC (ID)
);

-- Backs "every attachment for this صورت‌هزینه" (list/download) and the max(ATTACH_RADIF)+1
-- computation on insert.
CREATE INDEX IDX_PC_ATTACHMENT_EXPENSEDOC ON TB_PC_ATTACHMENT (EXPENSE_DOC_ID);
