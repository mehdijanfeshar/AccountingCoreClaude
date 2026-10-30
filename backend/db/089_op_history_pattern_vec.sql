--------------------------------------------------------------------------------------------------
-- 089_op_history_pattern_vec.sql
--
-- حسابیار: جستجوی معنایی (RAG) در «کشف الگو از اسناد گذشته». بردار هر الگوی سابقه (مدل embedding داخلی، bge-m3).
-- پیش‌نیاز: 085. اوراکل 19 نوع VECTOR ندارد ⇒ BLOB (float32)؛ مقایسه در حافظهٔ برنامه انجام می‌شود.
-- بدون این اسکریپت تطبیق جمله مثل قبل فقط کلمه‌ای کار می‌کند (خطا نمی‌دهد).
-- جدول محاسبه‌شده است و در لاگ تغییرات ثبت نمی‌شود. FK ندارد: شناسهٔ الگو پایدار است و بردار از بازسازی می‌ماند.
--
-- ⚠️⚠️ DO NOT RUN THIS AGAINST ANY DATABASE WITHOUT EXPLICIT PROJECT-OWNER APPROVAL. ⚠️⚠️
--------------------------------------------------------------------------------------------------

CREATE TABLE TB_OP_HISTORY_PATTERN_VEC
(
    PATTERN_ID   CHAR(36)      NOT NULL,   -- همان ID جدول TB_OP_HISTORY_PATTERN
    MODEL        VARCHAR2(100) NOT NULL,   -- نام مدل embedding
    TEXT_HASH    VARCHAR2(64),             -- SHA-256 متن ورودی + مدل
    DIM          NUMBER(5),
    VECTOR       BLOB,                     -- float32 little-endian، نرمال‌شده
    CREATEDDATE  TIMESTAMP,
    CONSTRAINT PK_OP_HISTORY_PATTERN_VEC PRIMARY KEY (PATTERN_ID)
);

-- معناهای هر الگو: گروه‌بندی معنایی نمونه‌شرح‌ها (یک مجموعه حساب، چند کار متفاوت). الگوی یک‌معنایی یک ردیف IDX=0.
CREATE TABLE TB_OP_HISTORY_MEANING
(
    PATTERN_ID    CHAR(36)        NOT NULL,   -- همان ID جدول TB_OP_HISTORY_PATTERN
    IDX           NUMBER(3)       NOT NULL,   -- ۰ = بزرگ‌ترین معنا
    TITLE         NVARCHAR2(200)  NOT NULL,   -- نماینده‌ترین شرح (بدون نام طرف حساب و رقم)
    SHARE         NUMBER(5,1),                -- درصد از نمونه‌شرح‌های الگو
    SAMPLE_COUNT  NUMBER(10),
    SAMPLES_JSON  NVARCHAR2(1000),
    KEYWORDS      NVARCHAR2(1000),
    CREATEDDATE   TIMESTAMP,
    CONSTRAINT PK_OP_HISTORY_MEANING PRIMARY KEY (PATTERN_ID, IDX)
);
