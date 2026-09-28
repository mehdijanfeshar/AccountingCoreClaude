# ماژول «تنخواه و خزانه‌داری» — طراحی و نقشهٔ اجرا (فاز ۴۴ به بعد)

> منبع: `prototype_tankhah_daryaft_pardakht_v3.pptx` (۲۲ صفحه) + `CHANGELOG_v3_for_codex.md` که صاحب پروژه در ۲۰۲۶-۰۹-۲۷ داد.
> این سند **مرجع واحد** بک‌اند و فرانت برای این ماژول است. شمارهٔ «ص N» یعنی صفحهٔ N پاورپوینت.

## ۰. تصمیم‌های صاحب پروژه (۲۰۲۶-۰۹-۲۷)

1. **ماژول جدا در منو:** گروه جدید «تنخواه و خزانه‌داری» در کنار «اطلاعات پایه / عملیات / گزارش‌ها». منوهای قبلی «تنخواه»، «هزینه» و «دریافت و پرداخت» **دست نمی‌خورند**؛ وقتی ماژول جدید تأیید شد، آن‌ها غیرفعال می‌شوند.
2. **استثنا بر قانون «هیچ جدول جدیدی ساخته نمی‌شود»** — فقط برای همین ماژول و فقط برای داده‌ای که در schema Legacy جایی ندارد. هستهٔ داده روی جدول‌های Legacy می‌ماند؛ جدول‌های جدید «جانبی» و کم‌تعدادند و پیشوند `TB_PC_` (تنخواه) / `TB_TR_` (خزانه) دارند.
3. **بخش فروش نداریم** (سازمان تأمین اجتماعی). «تخصیص دریافت به فاکتورهای باز مشتری» (ص ۱۸) ساخته **نمی‌شود**؛ فقط طوری طراحی می‌کنیم که بعداً بشود اضافه‌اش کرد (هیچ جدول/ستونی برای آن ساخته نمی‌شود).
4. **ترتیب کار:** به ترتیب پاورپوینت — اول تنخواه، بعد خزانه.

### تصمیم ۲۰۲۶-۰۹-۲۸: جدول مستقل تنخواه

**چه چیزی عوض شد:** تنخواهِ این ماژول دیگر روی `TB_REVOLVING_FUND` نیست. جدول جدید و کاملاً مستقل
`TB_PC_FUND` (فیلدها: کد، عنوان، تنخواه‌دار، سقف تنخواه = `CEILING`، سقف هر سند = `PER_DOC_LIMIT`
(اینجا الزامی، برخلاف `TB_PC_FUND_SETTING` قدیم)، آستانهٔ هشدار، حساب معین، دورهٔ تسویه، وضعیت
فعال/غیرفعال) جایگزین **هر دوِ** `TB_REVOLVING_FUND` (برای این ماژول) و `TB_PC_FUND_SETTING`
(که کاملاً حذف شد) می‌شود. `TB_PC_EXPENSE_DOC.REVOLVINGFUND_ID` و `TB_PC_REVIEWER.REVOLVINGFUND_ID`
به `FUND_ID` تغییر نام یافتند و اکنون به `TB_PC_FUND` اشاره می‌کنند. DDL: `backend/db/047_petty_cash_fund.sql`
(شامل پاک‌سازی دادهٔ تستِ موجود این ماژول — همه به تنخواه‌های قدیمی اشاره داشتند).

**چرا:** تصمیم صریح صاحب پروژه — این ماژول نباید هیچ وابستگی‌ای به `TB_REVOLVING_FUND` یا
تنخواه‌های قبلی (که در صفحهٔ قدیمی اطلاعات پایه ساخته/ویرایش می‌شدند) داشته باشد. این عمداً
**استثنایی دومی** روی قاعدهٔ Legacy-as-Domain است، روی استثنای اول (تصمیم ۲۰۲۶-۰۹-۲۷، بند ۲ بالا)
سوار شده — `TB_REVOLVING_FUND` برای بقیهٔ پروژه (صفحهٔ قدیمی تنخواه) دست‌نخورده می‌ماند.

## ۱. نگاشت پاورپوینت به schema

| مفهوم پاورپوینت | Legacy (هسته) | جدول جدید (جانبی) |
|---|---|---|
| ~~تنخواه (سقف، حساب معین)~~ | ~~`TB_REVOLVING_FUND` (`DEFAULTAMOUNT` = سقف، `ACCOUNTCODE_ID` = معین)~~ | ~~`TB_PC_FUND_SETTING`~~ — **منسوخ، ۲۰۲۶-۰۹-۲۸** |
| تنخواه (سقف، حساب معین، تنخواه‌دار، سقف هر سند، …) | — (مستقل از Legacy) | `TB_PC_FUND` — همهٔ فیلدهای تعریف تنخواه، یک‌جا (تصمیم ۲۰۲۶-۰۹-۲۸ بالا) |
| صورت‌هزینه (TH-xxxxx) | `TB_CHARGEANDCOST_HEAD` نوع ۲ (هزینه‌کرد) + **یک** ردیف `TB_CHARGEANDCOST_DETAIL` (`EXPENSE_ID`, `PAYTO`, `CREDITOR`) | `TB_PC_EXPENSE_DOC` — فروشنده، فاکتور، ارزش افزوده، **وضعیت ۷تایی**، تنخواه |
| گردش عملیات (Audit Trail، ص ۱۲) | — | `TB_PC_DOC_EVENT` |
| ترمیم / شارژ (RCH، ص ۹) | `TB_CHARGEANDCOST_HEAD` نوع ۱ + `TB_CHARGE_LINK_COST` (کدام هزینه با کدام شارژ ترمیم شد) | (در بخش سوم تصمیم‌گیری می‌شود) |
| دریافت/پرداخت، انتقال | `TB_PAYRECIVHEAD/DETAIL` | `TB_TR_*` (در بخش خزانه) |
| مغایرت بانکی | `TB_BANKCARTDETAIL` | — |

### یافته‌های اوراکل زنده (۲۰۲۶-۰۹-۲۷، فقط SELECT)
- `TB_CHARGEANDCOST_HEAD`: ۲۷ ردیف، همه آشکارا تستی؛ `STATUS` فقط ۰ و ۱ دیده شد؛ `TB_CHARGE_LINK_COST` **خالی**.
- ⚠️ `CHARGEANDCOST_TYPE` (۱/۲) و `STATUS` (۰/۱/۲) در Entity ما `bool` نگاشت شده‌اند — همان باگ ریسک #۲. پیش‌نیاز هر کاری در این ماژول است.
- ردیف‌های هزینه‌کرد موجود **هیچ `REVOLVINGFUND_ID` ندارند** — در Legacy معلوم نیست هزینه از کدام تنخواه است. پس تنخواه صورت‌هزینه در `TB_PC_EXPENSE_DOC` نگه داشته می‌شود و ردیف Legacy مثل داده‌های موجود `REVOLVINGFUND_ID = null` می‌ماند.

## ۲. وضعیت‌ها (ص ۳)

`PettyCashDocState` (enum، ذخیره در `TB_PC_EXPENSE_DOC.DOC_STATE`):

| مقدار | نام | معنا | `STATUS` Legacy |
|---|---|---|---|
| 1 | `Draft` پیش‌نویس | ذخیره شده، ارسال نشده | 0 موقت |
| 2 | `New` جدید | ارسال شده، هنوز باز نشده | 0 |
| 3 | `PendingReview` در انتظار بررسی | در کارتابل بازرس | 0 |
| 4 | `Returned` برگشتی | نیازمند اصلاح تنخواه‌دار | 0 |
| 5 | `Approved` تأییدشده | منتظر ترمیم | 2 تایید دائم |
| 6 | `Rejected` ردشده | پایانی، بدون ترمیم | 0 |
| 7 | `Settled` تسویه‌شده | در سند تسویه دوره منظور شد | 2 |

نگاشت به `STATUS` Legacy **فقط در یک جا** انجام می‌شود (`PettyCashStatusMap`). دلیل «تأییدشده → ۲»: در پروژهٔ مرجع کوئری `GetAllAccepted` همان منبع ساخت شارژ/ترمیم است.

**معادلهٔ تراز تنخواه (ص ۴):** `سقف = موجودی نقد + تأییدشده منتظر ترمیم + در جریان`، پس
`موجودی نقد = DEFAULTAMOUNT − Σ(مبلغ کل اسناد در وضعیت New, PendingReview, Returned, Approved)`.
پیش‌نویس، ردشده و تسویه‌شده در این جمع **نیستند**. این فرمول تا ساخت ترمیم (بخش ۳) معتبر است و آنجا بازبینی می‌شود.

## ۳. جدول‌های جدید (DDL در `backend/db/044_petty_cash.sql`)

همه با ستون‌های Audit استاندارد پروژه (`CREATEDDATE`, `UPDATEDDATE`, `ADDUSERID`, `CHANGEUSERID`, `VAHEDCODE`, `YEAR`, `ISDELETED`) و `ID CHAR(36)` بدون `DEFAULT sys_guid()` (ریسک #۱۱). مبلغ‌ها `NUMBER(25)` مثل Legacy.

- ~~**`TB_PC_FUND_SETTING`** — ۱:۱ با `TB_REVOLVING_FUND` (UNIQUE روی `REVOLVINGFUND_ID`):
  `CUSTODIAN_USERID VARCHAR2(10)`, `CUSTODIAN_NAME VARCHAR2(200)`, `PER_DOC_LIMIT NUMBER(25)`, `ALERT_THRESHOLD_PERCENT NUMBER(3)`, `SETTLEMENT_PERIOD NUMBER(1)` (1=ماهانه، 2=فصلی).~~ — **منسوخ، ۲۰۲۶-۰۹-۲۸** (`DROP TABLE`، `backend/db/047_petty_cash_fund.sql`؛ ادغام‌شده در `TB_PC_FUND`، به بخش ۰ رجوع کنید).
- **`TB_PC_FUND`** (۲۰۲۶-۰۹-۲۸، `backend/db/047_petty_cash_fund.sql`) — مستقل، بدون FK به Legacy، UNIQUE روی `(VAHEDCODE, CODE)`:
  `CODE VARCHAR2(50) NOT NULL`, `NAME VARCHAR2(200) NOT NULL`, `CUSTODIAN_USERID VARCHAR2(10) NOT NULL`, `CUSTODIAN_NAME VARCHAR2(200)`, `CEILING NUMBER(25) NOT NULL` (سقف تنخواه)، `PER_DOC_LIMIT NUMBER(25) NOT NULL` (سقف هر سند، اینجا الزامی)، `ALERT_THRESHOLD_PERCENT NUMBER(3)`, `ACCOUNTCODE_ID CHAR(36)` (FK به `TB_ACCOUNTCODE`)، `SETTLEMENT_PERIOD NUMBER(1)` (1=ماهانه، 2=فصلی)، `IS_ACTIVE NUMBER(1) DEFAULT 1 NOT NULL`.
- **`TB_PC_EXPENSE_DOC`** — ۱:۱ با `TB_CHARGEANDCOST_HEAD` (UNIQUE روی `CHARGEANDCOSTHEAD_ID`):
  `FUND_ID CHAR(36) NOT NULL` (تا ۲۰۲۶-۰۹-۲۷ به نام `REVOLVINGFUND_ID` و اشاره به `TB_REVOLVING_FUND`؛ از ۲۰۲۶-۰۹-۲۸ به `TB_PC_FUND`)، `DOC_STATE NUMBER(2) NOT NULL`, `VENDOR_NAME VARCHAR2(200)`, `VENDOR_NATIONAL_ID VARCHAR2(11)`, `INVOICE_NO VARCHAR2(50)`, `INVOICE_DATE VARCHAR2(8)`, `EVIDENCE_TYPE NUMBER(1)` (1=فاکتور رسمی، 2=رسید، 3=سایر)، `AMOUNT_BEFORE_TAX NUMBER(25)`, `VAT_AMOUNT NUMBER(25)`, `SUBMITTED_DATE TIMESTAMP`, `RETURN_DEADLINE VARCHAR2(8)`.
- **`TB_PC_DOC_EVENT`** — تاریخچهٔ اقدامات (فقط درج، هرگز ویرایش/حذف):
  `EXPENSE_DOC_ID CHAR(36) NOT NULL`, `ACTION NUMBER(2)`, `FROM_STATE NUMBER(2)`, `TO_STATE NUMBER(2)`, `NOTE VARCHAR2(1000)`, `RETURN_REASONS VARCHAR2(200)` (کدهای دلیل با `,`), `CLIENT_IP VARCHAR2(45)`.

⚠️ **اجرای DDL روی دیتابیس فقط با تأیید صریح صاحب پروژه.** تا آن زمان تست‌ها روی SQLite اجرا می‌شوند.

## ۴. قواعد سمت سرور (بخش اول)

| قاعده | کِی | نتیجه |
|---|---|---|
| فقط `Draft` و `Returned` قابل ویرایش‌اند | Update | ۴۰۹ |
| فقط `Draft` قابل حذف است | Delete | ۴۰۹ |
| مبلغ کل = قبل از مالیات + ارزش افزوده؛ ارزش افزوده ≥ ۰ و ≤ مبلغ قبل از مالیات | همه | ۴۰۰ |
| مبلغ کل ≤ `TB_PC_FUND.PER_DOC_LIMIT` («سقف هر سند» — از ۲۰۲۶-۰۹-۲۸ همیشه تعریف‌شده، الزامی) | Submit | ۴۰۰ |
| مبلغ کل ≤ موجودی نقد تنخواه (`CEILING` منهای در جریان/تأییدشده) | Submit | ۴۰۰ |
| تکراری نبودن (شناسه ملی فروشنده + شماره فاکتور) بین اسناد حذف‌نشده و ردنشده | Create/Update | ۴۰۹ |
| تاریخ فاکتور در سال مالی جاری (`INVOICE_DATE` با `YEAR` شروع شود) | Submit | ۴۰۰ |
| نرخ ارزش افزوده **در سرور hardcode نمی‌شود** (CHANGELOG §۵: قابل تنظیم)؛ فرم ۱۰٪ پیشنهاد می‌دهد و کاربر تأیید می‌کند | — | — |
| هر تغییر وضعیت یک ردیف `TB_PC_DOC_EVENT` در **همان تراکنش** | همه | — |
| تنخواهٔ غیرفعال (`TB_PC_FUND.IS_ACTIVE = false`) هیچ صورت‌هزینهٔ جدیدی نمی‌پذیرد (**۲۰۲۶-۰۹-۲۸**) | Create/Submit | ۴۰۹ |
| کد تنخواه در همان واحد تکراری نباشد (**۲۰۲۶-۰۹-۲۸**) | Create/Update (فاند) | ۴۰۹ |
| حذف تنخواه فقط وقتی صورت‌هزینهٔ حذف‌نشده‌ای ندارد (**۲۰۲۶-۰۹-۲۸**) | Delete (فاند) | ۴۰۹ |

شمارهٔ سند: `CHARGEANDCOST_CODE` = بیشینهٔ عددی کد نوع ۲ در (واحد، سال) + ۱، با `PadLeft(5,'0')`؛ نمایش `TH-` + کد.

## ۵. API (بخش اول) — همه `IVahedScoped`، فقط `GET`/`POST`

```
GET  api/petty-cash/funds                          تنخواه‌ها (TB_PC_FUND) + خلاصهٔ موجودی (نقد/تأییدشده/در جریان)
GET  api/petty-cash/funds/{fundId}                 یک تنخواه
POST api/petty-cash/funds                          ساخت تنخواه (۲۰۲۶-۰۹-۲۸)
POST api/petty-cash/funds/{fundId}/update          (۲۰۲۶-۰۹-۲۸)
POST api/petty-cash/funds/{fundId}/delete          حذف نرم؛ ۴۰۹ اگر صورت‌هزینهٔ حذف‌نشده دارد (۲۰۲۶-۰۹-۲۸)
GET  api/petty-cash/expense-docs?state=&fundId=&search=&pageNumber=&pageSize=   + شمارش هر وضعیت
GET  api/petty-cash/expense-docs/{id}
POST api/petty-cash/expense-docs                   ساخت پیش‌نویس (با submit=true مستقیم ارسال)
POST api/petty-cash/expense-docs/{id}/update
POST api/petty-cash/expense-docs/{id}/submit
POST api/petty-cash/expense-docs/{id}/delete
GET  api/petty-cash/expense-docs/{id}/events       گردش عملیات
```

## ۶. منوی فرانت — گروه «تنخواه و خزانه‌داری»

به ترتیب پاورپوینت؛ موردهای ساخته‌نشده «به‌زودی» نمایش داده می‌شوند (الگوی موجود `navConfig.tsx`):

- تنخواه: داشبورد تنخواه (ص ۴)، **کارتابل تنخواه (ص ۵)**، **ثبت صورت‌هزینه (ص ۶)**، شارژ و ترمیم (ص ۹)، تسویه دوره (ص ۱۰)، گزارش گردش تنخواه (ص ۱۱)، **تعریف تنخواه (ص ۱۳)**
- خزانه: داشبورد خزانه (ص ۱۴)، درخواست پرداخت (ص ۱۵)، کارتابل تأیید (ص ۱۶)، اجرای پرداخت (ص ۱۷)، دریافت و انتقال (ص ۱۸–۱۹)، مغایرت بانکی (ص ۲۱)

مسیرها: `/treasury/petty-cash/...` و `/treasury/khazaneh/...`.

## ۷. نقشهٔ بخش‌ها

| بخش | محتوا | صفحات |
|---|---|---|
| **۱** | پیش‌نیاز enum + ۳ جدول + تعریف تنخواه + ثبت صورت‌هزینه + کارتابل | ۱۳، ۶، ۵ |
| ۲ | بررسی سند، برگشت چندعلتی با مهلت، رد، تأیید (+ تأیید گروهی)، SoD «ایجادکننده ≠ بررسی‌کننده»، گردش عملیات، پیوست | ۷، ۸، ۱۲ |
| ۳ | درخواست ترمیم (`TB_CHARGE_LINK_COST`)، تسویه دوره + صدور سند GL، داشبورد، گزارش گردش | ۹، ۱۰، ۴، ۱۱ |
| ۴+ | خزانه: درخواست پرداخت، کارتابل تأیید، اجرا، دریافت/انتقال، زنجیرهٔ حسابداری، مغایرت | ۱۴–۲۱ |

## ۸. خارج از دامنه / باز

- **مرکز هزینه:** در پاورپوینت هست، معادل مشخصی در schema ندارد (احتمالاً یک سطح تفصیلی) — بخش ۱ ندارد.
- **فروش / حساب‌های دریافتنی:** خارج از دامنه (تصمیم ۳).

### تصمیم‌های بخش ۲ (accounting-domain، ۲۰۲۶-۰۹-۲۷) — نقش‌ها، پیوست، دلایل برگشت

**نقش‌ها (RBAC):** جدول جانبی جدید `TB_PC_REVIEWER`، نه `TB_PERSON_ACTION`. دلیل: `TB_PERSON_ACTION.USERID` کد ملی است (فضای هویتی جدا از `ICurrentUser.UserId`/`ADDUSERID` که بقیهٔ پروژه با آن مقایسه هویت می‌کند)، و `OPERATORROLE` یک نقش امضای مالی سازمانی بدون هیچ consumer فعلی است — تحمیل معنای «بررسی‌کنندهٔ این تنخواهِ خاص» رویش حدسی و مستندنشده است.

```sql
TB_PC_REVIEWER
  ID CHAR(36) PK, FUND_ID CHAR(36) NOT NULL FK->TB_PC_FUND,  -- تا ۲۰۲۶-۰۹-۲۷: REVOLVINGFUND_ID FK->TB_REVOLVING_FUND
  REVIEWER_USERID VARCHAR2(10) NOT NULL,   -- فضای هویتی ICurrentUser.UserId/ADDUSERID، نه کد ملی
  REVIEWER_NAME VARCHAR2(200),
  + ستون‌های Audit استاندارد + UNIQUE(FUND_ID, REVIEWER_USERID)
```

**قاعدهٔ SoD:** اکشن‌های بررسی/تأیید/برگشت/رد فقط برای کاربری مجازند که (۱) ردیف فعال در `TB_PC_REVIEWER` برای همان `FUND_ID` دارد (وگرنه ۴۰۳) و (۲) `ICurrentUser.UserId != TB_PC_EXPENSE_DOC.ADDUSERID` (وگرنه ۴۰۹ — تعارض منافع، نه نبود دسترسی).

**پیوست:** جدول جانبی جدید `TB_PC_ATTACHMENT` (BLOB در DB، هم‌الگو با `TB_ATTACH` Legacy — نه ستون جدید روی آن، نه فایل‌سیستم/Blob storage که در پروژه سابقه ندارد):

```sql
TB_PC_ATTACHMENT
  ID CHAR(36) PK, EXPENSE_DOC_ID CHAR(36) NOT NULL FK->TB_PC_EXPENSE_DOC,
  ATTACH_NAME VARCHAR2(255) NOT NULL, ATTACH_SIZE NUMBER(10) NOT NULL,
  CONTENT_TYPE VARCHAR2(100), ATTACH_FILE BLOB NOT NULL, ATTACH_RADIF NUMBER(2) NOT NULL,
  + ستون‌های Audit استاندارد
```
فقط در `DOC_STATE ∈ {Draft, Returned}` قابل افزودن/حذف (هم‌قاعدهٔ ویرایش §۴). سقف اندازه در FluentValidation (پیشنهاد ۱۰MB)، نه در DB. لیست پیوست بدون بایت برمی‌گردد؛ دانلود endpoint جدا.

**دلایل برگشت:** enum ثابت `PettyCashReturnReason` (نه جدول قابل‌تنظیم — هم‌الگو با بقیهٔ enumهای این ماژول)، کدها در `TB_PC_DOC_EVENT.RETURN_REASONS` موجود (کاما-جدا) ذخیره می‌شوند، DDL عوض نمی‌شود. ⚠️ متن دقیق دلایل از پاورپوینت استخراج نشده — پیش‌نویس (نیازمند تأیید صاحب پروژه روی عبارت‌ها): فاکتور/رسید ناقص، تاریخ فاکتور نامعتبر، مغایرت مبلغ، اطلاعات فروشنده ناقص، مدارک پشتیبان ناقص، خارج از سقف/ضوابط تنخواه، سایر.

**`RETURN_DEADLINE`:** بدون ستون «مهلت پیش‌فرض» در تنظیمات؛ بررسی‌کننده مهلت را در همان درخواست Return وارد می‌کند (الزامی، تاریخ بعد از امروز). عبور از مهلت فعلاً هیچ اکشن خودکاری ندارد (فقط اطلاعاتی).

**گسترش `PettyCashDocAction`** (مقادیر ۵+ که در بخش ۱ رزرو شده بودند): `StartReview=5` (New→PendingReview)، `Approve=6` (PendingReview→Approved)، `Return=7` (PendingReview→Returned)، `Reject=8` (PendingReview→Rejected). تأیید گروهی enum جدا نمی‌خواهد — یک Command با لیست id، یک تراکنش، all-or-nothing.

**API افزوده:** `POST expense-docs/{id}/start-review|approve|return|reject`، `POST expense-docs/bulk-approve`، `GET/POST funds/{fundId}/reviewers`، `POST funds/{fundId}/reviewers/{id}/delete`، `POST/GET expense-docs/{id}/attachments[...]`.

**ریسک باز جدید:** `TB_PC_REVIEWER` مستقل از سیستم نقش مرکزی است؛ اگر IDP واقعی/RBAC مرکزی پیاده شد (ریسک #۱۰)، باید یکپارچه یا حذف شود — به `docs/open-decisions.md` اضافه شود.

### تصمیم‌های محافظه‌کارانهٔ پیاده‌سازی بخش ۱ (۲۰۲۶-۰۹-۲۷) — ابهام‌هایی که این سند صراحتاً پوشش نداده بود

این سند فیلد `year` را در بدنهٔ `POST api/petty-cash/expense-docs`/`.../update` نگذاشته بود، در حالی که هر ردیف Legacy/جانبی به یک سال مالی نیاز دارد. به‌جای توقف، گزینهٔ محافظه‌کارانه انتخاب شد:

- ~~`YEAR` = چهار کاراکتر اول `registerDate`~~ — **اصلاح شد (تصمیم صاحب پروژه، ۲۰۲۶-۰۹-۲۷):** فیلد `year` به بدنهٔ Create/Update اضافه شد و فرانت سال مالی انتخاب‌شده در جلسه (`SessionContext.financialYear`) را می‌فرستد — همان الگوی صدور سند. اسکریپت `044_petty_cash.sql` همان روز روی دیتابیس توسعه اجرا شد.
- **قاعدهٔ تکراری‌نبودن (§۴)** فقط وقتی `vendorNationalId` **و** `invoiceNo` هر دو غیرخالی باشند اعمال می‌شود؛ در غیر این صورت معنای «تکراری» نامعلوم است.
- **`invoiceDate` فقط هنگام `submit=true` الزامی است** — قاعدهٔ «سال مالی جاری» روی یک پیش‌نویس چیزی برای بررسی ندارد.
- **حذف صورت‌هزینه، سرسند و دیتیل Legacy (`TB_CHARGEANDCOST_HEAD`/`DETAIL`) را هم نرم حذف می‌کند** — سند این را صریح نگفته بود؛ انتخاب شد چون این پروژه هرگز حذف فیزیکی انجام نمی‌دهد و یتیم‌ماندن سرسند در جدول‌های اصلی حسابداری نامطلوب است.
- **`GET api/petty-cash/funds` آرایهٔ خام برمی‌گرداند** (نه `PagedResult`) و ~~**`GET .../settings` برای تنخواهِ بدون تنظیمات ۴۰۴ می‌دهد**~~ — به درخواست هماهنگ‌سازی قرارداد با فرانت (که موازی ساخته می‌شد)، نه از خودِ این سند. **منسوخ، ۲۰۲۶-۰۹-۲۸:** endpoint جداگانهٔ `.../settings` حذف شد؛ تنظیمات حالا ستون‌های خودِ `TB_PC_FUND` هستند و با CRUD معمول فاند خوانده/نوشته می‌شوند (به بخش ۰ رجوع کنید).
- **پارامتر کوئری `states` (کاما-جدا)** به لیست صورت‌هزینه‌ها اضافه شد؛ در سند اصلی نبود، از همان درخواست هماهنگ‌سازی آمد.

جزئیات کامل و کد در `docs/phase-log.md` بخش «فاز ۴۴ (بخش ۱)».
