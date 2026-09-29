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

بخش ۲ و بخش ۳-الف endpoint‌های خودشان را افزودند — رجوع به «API افزوده» زیر همان بخش‌ها و بخش ۹ (بخش ۳ — طراحی، ۲۰۲۶-۰۹-۲۸).

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

### تصمیم‌های تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸): نقش‌ها، تأیید دومرحله‌ای، قفل فیلد

پیاده‌سازی نهایی صفحات ۷/۸/۱۲/۱۳ پاورپوینت روی هستهٔ بخش ۲ بالا.

**نقش‌ها (ص ۱۳):** enum جدید `PettyCashRole`: `Inspector=1` (بازرس مالی)، `FinanceManager=2`
(مدیر مالی)، `ChiefExecutive=3` (مدیرعامل). تنخواه‌دار نقش جداگانه نیست — همان
`TB_PC_FUND.CUSTODIAN_USERID`. `TB_PC_REVIEWER.ROLE NUMBER(2) DEFAULT 1 NOT NULL`؛ `UK_PC_REVIEWER`
به `(FUND_ID, REVIEWER_USERID, ROLE)` عریض شد — یک کاربر می‌تواند روی یک تنخواه بیش از یک نقش
داشته باشد. `TB_PC_FUND.FINANCE_MANAGER_APPROVAL_LIMIT NUMBER(25) DEFAULT 500000000 NOT NULL`
(«تا ۵۰۰ م»، در Create/Update فاند الزامی و `> 0`). DDL: `backend/db/048_petty_cash_roles.sql`.

**ثبت سند فقط توسط تنخواه‌دار:** Create/Update/Submit/Delete صورت‌هزینه اکنون
`ICurrentUser.UserId == TB_PC_FUND.CUSTODIAN_USERID` را چک می‌کنند — وگرنه ۴۰۳
(`PettyCashNotCustodianException`).

**تأیید دومرحله‌ای (ص ۱۲):** `TB_PC_EXPENSE_DOC` ستون‌های جدید `VERIFIED_BY_USERID`/`VERIFIED_DATE`
گرفت. `PettyCashDocAction` مقادیر جدید `Verify=9` و `FinalApprove=10` گرفت (`Approve=6` برای دادهٔ
تاریخی می‌ماند، دیگر تولید نمی‌شود).

- `POST expense-docs/{id}/verify` `{ note? }`: فقط `PendingReview`، فقط `Inspector` همان تنخواه،
  ≠ ایجادکننده، و هنوز verify نشده (وگرنه ۴۰۹ `PettyCashAlreadyVerifiedException`). وضعیت سند
  **تغییر نمی‌کند**؛ فقط `VERIFIED_BY_USERID/DATE` ست و رویداد `Verify` (from=to=PendingReview)
  ثبت می‌شود.
- `POST expense-docs/{id}/approve` = «تأیید نهایی»: فقط `PendingReview` و فقط اگر verify شده
  (وگرنه ۴۰۹ `PettyCashNotVerifiedException`). کاربر باید ≠ ایجادکننده (۴۰۹، مثل قبل) و ≠
  `VERIFIED_BY_USERID` (۴۰۹ جدید `PettyCashVerifierCannotApproveException`). مبلغ کل ≤
  `FINANCE_MANAGER_APPROVAL_LIMIT` ⇒ نقش `FinanceManager` یا `ChiefExecutive` کافی است؛ بیشتر ⇒
  فقط `ChiefExecutive` (وگرنه ۴۰۳ `PettyCashApprovalAuthorityExceededException`، پیام «سقف
  اختیار»). منطق در سرویس مشترک `PettyCashFinalApprovalService` است، نه دیگر
  `PettyCashReviewTransitionService` (که فقط StartReview/Return/Reject را پوشش می‌دهد).
- `bulk-approve`: همان `PettyCashFinalApprovalService` برای هر id (all-or-nothing)؛ دلایل شکست
  جدید در `failedIds`: `not-verified`، `over-authority` (و `self-review` هم برای تعارض ایجادکننده
  هم برای تعارض verifier).
- `return`/`reject`: مجاز برای `Inspector` یا `FinanceManager` یا `ChiefExecutive` همان تنخواه، ≠
  ایجادکننده (بدون تغییر نسبت به بخش ۲ قدیم، فقط اکنون نقش‌محور). `return` علاوه بر رفتار قبلی،
  `VERIFIED_BY_USERID/DATE` را پاک می‌کند — سند برگشتی دوباره باید کنترل شود.
- `IPettyCashReviewAuthorizer.EnsureCanReviewAsync` اکنون `allowedRoles` می‌گیرد و زیرمجموعهٔ
  نقش‌های فعال کاربر که در `allowedRoles` است را برمی‌گرداند — منطق نقش/SoD یک‌جا مانده.

**قفل فیلدبه‌فیلد (ص ۸) — پیاده شد:** کلاس ایستای `PettyCashReturnFieldPolicy`
(`Accounting.Application.Common.Security`) نگاشت دلیل برگشت → فیلدهای مجاز:
`AttachmentIncomplete` → فقط پیوست (هیچ فیلد فرم)؛ `ExpenseAccountIncorrect` → `expenseId`؛
`AmountMismatch` → `amountBeforeTax, vatAmount, invoiceNo, invoiceDate, evidenceType`؛
`DescriptionNeedsClarification` → `description`؛ `Other` → همهٔ فیلدها (`null` در API، یعنی
بدون محدودیت). `UpdatePettyCashExpenseDocCommandHandler` فیلدهای واقعاً تغییرکرده را (روی
`TB_PC_EXPENSE_DOC` + سرسند/دیتیل Legacy برای `registerDate`/`description`/`expenseId`) با آخرین
دلایل رویداد `Return` (`IPettyCashDocEventReadRepository.GetLastReturnEventAsync`) می‌سنجد؛ خارج از
مجموعهٔ مجاز ⇒ ۴۰۹ (`PettyCashReturnFieldLockedException`، نام فیلدها در پیام). آپلود/حذف پیوست
روی سند برگشتی همین سیاست را چک می‌کند (`PettyCashReturnFieldPolicy.CanEditAttachments`).
`GetPettyCashExpenseDocById` فیلد `editableFields: string[] | null` را برمی‌گرداند (`null` = همه/
قاعدهٔ عادی؛ در غیر این صورت نام‌های camelCase بدنهٔ update + `"attachments"` در صورت مجاز بودن).

جزئیات کامل و کد در `docs/phase-log.md` بخش «فاز ۴۴ (بخش ۲، تکمیل)».

## ۹. بخش ۳ — طراحی (۲۰۲۶-۰۹-۲۸): درخواست ترمیم، استرداد وجه، داشبورد، گزارش گردش

پیاده‌سازی صفحات ۹/۴/۱۱ پاورپوینت. کد: `Accounting.Application/PettyCash/{Commands,Queries}/...*Replenishment*`, `...*Refund*`, `GetPettyCashFundDashboard`, `GetPettyCashFundLedger`. DDL: `backend/db/049_petty_cash_replenishment.sql` (اجرا نشده).

### نقش‌ها
`PettyCashRole` گسترش یافت: `SeniorAccountant=4` (فعلاً بدون consumer — رزرو برای بعد)، `Treasurer=5`. روی همان `TB_PC_REVIEWER.ROLE`.

### ترمیم/شارژ (`TB_PC_REPLENISHMENT`)
۱:۱ با `TB_CHARGEANDCOST_HEAD` نوع `Charge` (`ACCOUNT_ID` = حساب بانکی مبدأ). هر صورت‌هزینهٔ منظورشده یک `TB_CHARGE_LINK_COST` (`CHARGE_ID`→head, `COST_ID`→`TB_CHARGEANDCOST_DETAIL.ID`). `STATE`: `Draft→PendingFinanceManager→PendingTreasurer→Paid`، یا `Rejected` از هر Pending. کد نمایشی کامل `"RCH-" + شمارهٔ ۵رقمی` (همان شمارندهٔ `IChargeAndCostRepository.GetNextCodeAsync` با `ChargeAndCostType.Charge`، فقط با پیشوند متفاوت). `STATUS` Legacy فقط از `PettyCashStatusMap.ToLegacyStatus(PettyCashReplenishmentState)` — فقط `Paid`→`Accepted`.

**تصمیم محافظه‌کارانه (فیلد اضافه‌شده):** `POST replenishments` علاوه بر `{fundId, sourceBankAccountId, paymentMethod, note?, submit}`، دو فیلد الزامی `registerDate`/`year` هم می‌گیرد — `TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_DATE`/`YEAR` در Legacy `NOT NULL`اند و سند طراحی شکل بدنه را برایشان مشخص نکرده بود؛ همان الگوی «تصمیم‌های محافظه‌کارانهٔ بخش ۱» برای `year` تکرار شد.

مجوز ایجاد/ارسال/حذف پیش‌نویس: فقط نقش `FinanceManager` یا `Treasurer` همان تنخواه (تصمیم محافظه‌کارانه — صفحهٔ ۹ نقش فعال «مدیر مالی» است، ولی خزانه‌دار هم منطقاً باید بتواند). `approve` فقط `FinanceManager`، ≠ ایجادکننده. `record-payment` فقط `Treasurer`، ≠ تأییدکننده. `reject` هر دو نقش، بدون چک تعارض ایجادکننده (سند صراحتاً نگفته بود).

**«هیچ سندی دو بار ترمیم نمی‌شود»:** `IChargeAndCostRepository.ExistsActiveLinkForCostAsync` بلافاصله پیش از درج هر لینک دوباره چک می‌شود (نه فقط روی snapshot پیش‌نمایش) — ۴۰۹ `PettyCashDocAlreadyReplenishedException` اگر رقابت رخ دهد.

**`record-payment` موقت است:** فقط `STATE` را `Paid` می‌کند؛ هیچ سند حسابداری (بدهکار تنخواه/بستانکار بانک) صادر نمی‌شود — طبق صفحهٔ ۱۰، آن سند به اجرای پرداخت خزانه (بخش ۴+) تعلق دارد.

### استرداد وجه (`TB_PC_REFUND`)
بدون معادل Legacy. `TB_PC_FUND.REFUND_RECORDER` (enum `PettyCashRefundRecorder`، پیش‌فرض `Treasurer`) تعیین می‌کند چه کسی مجاز به ثبت/حذف است — `Custodian` یعنی `TB_PC_FUND.CUSTODIAN_USERID`، بقیه یعنی نقش فعال متناظر در `TB_PC_REVIEWER`. چک مشترک در `PettyCashRefundRecorderAuthorizer` (Create و Delete هر دو صدایش می‌زنند). کد نمایشی `"REF-" + شمارهٔ ۵رقمی` (شمارندهٔ مستقل `IPettyCashRefundRepository.GetNextCodeAsync`، بدون `Year`، چون `TB_PC_REFUND.YEAR` — مثل بقیهٔ ستون‌های Audit این ماژول — اختیاری است).

⚠️ **باز مانده برای بخش ۳-ب:** حذف استرداد باید در دورهٔ تسویهٔ نهایی‌شده مسدود شود؛ چون این پروژه هنوز دوره‌ای برای تنخواه ندارد (`TB_PC_SETTLEMENT_PERIOD` بخش ۳-ب)، این چک اینجا اعمال نشده.

### فرمول موجودی نقد (متمرکز در `PettyCashBalanceCalculator`)
```
موجودی نقد = CEILING − Σ(مبلغ اسناد New,PendingReview,Returned,Approved) + Σ(TOTAL_AMOUNT ترمیم‌های Paid) + Σ(AMOUNT استردادهای حذف‌نشده)
```
همه‌جا از همین تابع استفاده می‌شود: `PettyCashFundReadRepository` (فهرست/تک تنخواه)، `PettyCashSubmitRuleChecker` (کنترل سقف Submit)، پیش‌نمایش ترمیم، داشبورد. اسناد Approved حتی بعد از ترمیم `Paid` هم در این جمع می‌مانند (چون هنوز `Settled` نشده‌اند، بخش ۳-ب)؛ جملهٔ «+ترمیم Paid» دقیقاً همان مبلغ را جبران می‌کند چون `TOTAL_AMOUNT` یک ترمیم از روی همان اسناد ساخته شده — این تعادل تصریحاً بررسی و تأیید شد.

### داشبورد (`GET funds/{fundId}/dashboard`)
معادلهٔ تراز صفحهٔ ۴ («سقف = نقد + تأییدشده + در جریان») با فرمول بالا جایگزین شد؛ استخراج جبری نشان می‌دهد فرم همیشه‌درستِ آن (پس از ساده‌سازیِ جملهٔ «ترمیم پرداخت‌شدهٔ هنوز‌تسویه‌نشده» که دقیقاً حذف می‌شود):
```
CEILING = Cash + AwaitingReplenishment + InFlight − RefundTotal
```
(`AwaitingReplenishment` = تأییدشده‌های بدون لینک به ترمیم Paid = `ApprovedAmount − PaidReplenishmentTotal`). فیلد `replenishedNotSettled` در پاسخ فقط نمایشی است؛ `balanced` از فرم کامل‌ترِ معادله (شامل همان جمله، به‌عنوان یک تست صحت دادهٔ واقعی) حساب می‌شود — جزئیات در `PettyCashDashboardBalanceCheckDto` XML doc.

### گزارش گردش تنخواه (`GET funds/{fundId}/ledger?from=&to=&type=`)
⚠️ **تصمیم موجودی اولیه:** `CEILING` همیشه پیش از هر بازهٔ درخواستی («تخصیص سقف اولیه») لحاظ می‌شود — پیشنهاد خودِ سند طراحی. ⚠️ **تصمیم مهم‌تر:** این گزارش «تاریخچهٔ حرکت واقعی وجه» است، نه معادل لحظه‌ایِ فرمول موجودی نقد — فقط صورت‌هزینه‌های `Approved`/`Settled` ردیف «پرداخت» می‌سازند (نه `New`/`PendingReview`/`Returned`، که فرمول §۲ آن‌ها را هم کم می‌کند). پس `closingBalance` وقتی `to`=امروز باشد با `cashBalance` فعلی برابر **نیست** — اختلافشان دقیقاً مجموع اسناد در جریان است؛ عمدی، نه باگ (مستند در `PettyCashFundLedgerDto` XML doc). فیلتر/گروه‌بندی روی جدول‌های ساده انجام می‌شود (سه کوئری تخت جدا برای ترمیم/صورت‌هزینه/استرداد، merge و jمع در C#) — نه یک projection رکوردی حاصل LeftJoin، طبق هشدار خودِ این سند دربارهٔ شکستن روی اوراکل. `PAID_DATE` (تنها ستون `TIMESTAMP` این بخش) با `PersianCalendar` به رشتهٔ شمسی `YYYYMMDD` تبدیل می‌شود تا با بقیهٔ ستون‌های تاریخ این ماژول (که همه از ابتدا Persian `VARCHAR2(8)`اند) قابل‌مقایسه بماند.

### API افزوده (بخش ۳-الف)
```
GET  api/petty-cash/funds/{fundId}/replenishment-preview
POST api/petty-cash/replenishments                 { fundId, sourceBankAccountId, paymentMethod, registerDate, year, note?, submit }
GET  api/petty-cash/replenishments?fundId=&state=&pageNumber=&pageSize=
GET  api/petty-cash/replenishments/{id}
POST api/petty-cash/replenishments/{id}/submit
POST api/petty-cash/replenishments/{id}/approve
POST api/petty-cash/replenishments/{id}/reject               { note? }
POST api/petty-cash/replenishments/{id}/record-payment       { paidDate? }
POST api/petty-cash/replenishments/{id}/delete
GET  api/petty-cash/funds/{fundId}/refunds
POST api/petty-cash/refunds                         { fundId, amount, refundDate, reason? }
POST api/petty-cash/refunds/{id}/delete
GET  api/petty-cash/funds/{fundId}/dashboard
GET  api/petty-cash/funds/{fundId}/ledger?from=&to=&type=
```

### بخش ۳-ب (۲۰۲۶-۰۹-۲۸، پیاده شد): تسویهٔ دوره و صدور سند حسابداری

جدول‌های جدید (DDL: `backend/db/050_petty_cash_settlement.sql`، اجرا نشده): `TB_PC_SETTLEMENT_PERIOD` (دورهٔ تسویه، `STATE`: 1=Draft 2=Final)، `TB_PC_FUND_LINK_TAFSILI` (تفصیلی حساب معین تنخواه، permanently-embedded مثل هر `*_LINK_TAFSIL*` دیگر). `PettyCashDocAction.Settle=11` اضافه شد.

**دوره:** مرزها از `TB_PC_FUND.SETTLEMENT_PERIOD` با `PettyCashSettlementPeriodCalculator` (ماهانه/فصلی، تقویم شمسی). دورهٔ جاری یک تنخواه همیشه «بلافاصله بعد از آخرین دورهٔ Final» است (یا دورهٔ ایجاد تنخواه اگر هیچ Final‌ای نیست) — هیچ‌وقت به «امروز» نگاه نمی‌کند، همین دوره‌ها را پشت‌سرهم نگه می‌دارد. `IPettyCashSettlementPeriodProvisioner` (`ComputeCurrentAsync` فقط‌خواندنی برای پیش‌نمایش، `EnsureDraftAsync` idempotent برای count/finalize) تنها جایی است که این را حساب می‌کند.

**مانده ابتدای دوره:** برای اولین دوره = `CEILING`؛ برای بقیه = `COUNTED_BALANCE` قفل‌شدهٔ دورهٔ Final قبلی (نه بازمحاسبهٔ زنده — تصمیم آگاهانه برای جلوگیری از انحراف اگر سندی با تاریخ قدیمی دیر تأیید شود). `IPettyCashSettlementReadRepository.GetPeriodMovementAsync` عمداً از `PettyCashLedgerReadRepository` استفاده نمی‌کند (معناهای «مانده قفل‌شده» با معنای زندهٔ گزارش گردش فرق دارد)؛ به‌جایش سه کوئری تخت جدا (ترمیم/استرداد/صورت‌هزینه) دارد.

**اسناد منظورشده:** صورت‌هزینه‌های `Approved` با `CHARGEANDCOST_DATE ≤ PERIOD_END` — بدون کف پایینی (سند دیرتأییدشده هم منظور می‌شود). گروه‌بندی روی `TB_CHARGEANDCOST_DETAIL.EXPENSE_ID` (نه مستقیم حساب) چون `TB_EXPENCE_LINK_TAFSILI` هم روی همین کلید است — یک گروه = دقیقاً یک ترکیب (حساب، مجموعهٔ تفصیلی).

**سند GL:** `PettyCashSettlementVoucherBuilder` (Application) روی همان `IVoucherHeadRepository`/`IVoucherDetailRepository`/`IVoucherTafsiliLevelGuard` که `CreateVoucherHeadCommandHandler`/`CreateVoucherDetailCommandHandler`/`ReverseVoucherCommandHandler` استفاده می‌کنند سوار است — چیزی موازی بازسازی نشد. `DOCLIFE=Temporary` (موقت)، `ISAUTOMATIC=true`، تاریخ=`PERIOD_END`. تراز پیش از `SaveChangesAsync` صریحاً چک می‌شود (`PettyCashSettlementUnbalancedException`، دفاعی — با ساخت ردیف بستانکار از جمع بدهکارها همیشه برقرار است).

**Endpointها (`FinalizePettyCashSettlementCommandHandler` — تراکنش صریح `BeginTransactionAsync`/`CommitTransactionAsync`):**
```
GET  api/petty-cash/funds/{fundId}/settlement                    → PettyCashSettlementPreviewDto
POST api/petty-cash/funds/{fundId}/settlement/count      { countedBalance }
POST api/petty-cash/funds/{fundId}/settlement/finalize   { acknowledgeInFlightTransfer }
GET  api/petty-cash/funds/{fundId}/settlements                   → تاریخچهٔ Final
GET  api/petty-cash/funds/{fundId}/tafsilis
POST api/petty-cash/funds/{fundId}/tafsilis               { tafsilis: [{tafsiliId, levelId}] }  (جایگزینی کامل)
```
`count` بدون محدودیت نقش (§۹ فقط برای `finalize` نقش تعیین کرده). `finalize` فقط `SeniorAccountant`، SoD (کاربر ≠ سازندهٔ هیچ سند منظورشده)، `acknowledgeInFlightTransfer` اگر سند در جریان هست، `countedBalance` باید ثبت و برابر مانده محاسبه‌شده باشد.

**قفل:** `DeletePettyCashRefundCommandHandler` اکنون حذف استرداد را وقتی `REFUND_DATE` داخل بازهٔ یک دورهٔ Final باشد مسدود می‌کند (`PettyCashRefundLockedBySettledPeriodException`، ۴۰۹) — TODO بخش ۳-الف بسته شد. صورت‌هزینهٔ `Settled` از قبل غیرقابل‌ویرایش/حذف/پیوست است (وضعیت پایانی در state machine بخش ۱/۲).

## ۱۰. خزانه‌داری (۲۰۲۶-۰۹-۲۸/۲۰۲۶-۰۹-۲۹) — نقشهٔ بخش‌های ۴-الف..۴-د

نقشهٔ راه: **۴-الف** درخواست پرداخت + کارتابل تأیید (این بخش، پیاده شد) · **۴-ب** اجرای پرداخت (ص ۱۷ پاورپوینت؛ پر کردن Legacy `TB_PAYRECIVHEAD/DETAIL` + دو سند GL «شناسایی بدهی» و «پرداخت»، ص ۲۰؛ و جایگزینی احتمالی `record-payment` ترمیم تنخواه) · **۴-ج** دریافت و انتقال وجه (ص ۱۸–۱۹) · **۴-د** مغایرت‌گیری (ص ۲۱) + داشبورد خزانه (ص ۱۴). هیچ‌کدام از ۴-ب/۴-ج/۴-د هنوز طراحی/پیاده نشده‌اند.

### بخش ۴-الف — طراحی و پیاده‌سازی: درخواست پرداخت + کارتابل تأیید

جدول‌های جدید (DDL: `backend/db/051_treasury_payment_request.sql`، اجرا نشده): `TB_TR_SETTING` (تنظیمات واحد، بدون مقدار پیش‌فرض)، `TB_TR_ROLE` (نقش خزانه، جدا از `TB_PC_REVIEWER`)، `TB_TR_PAYMENT_REQUEST` (درخواست پرداخت، `PAY-xxxxxx`)، `TB_TR_PAYMENT_REQUEST_EVENT` (گردش عملیات، insert-only). پیشوند `TB_TR_` («Treasury») عمداً متفاوت از `TB_PC_` — این جدول‌ها خاص تنخواه نیستند.

**سه تصمیم صاحب پروژه (۲۰۲۶-۰۹-۲۸):**
1. آستانهٔ تأیید مدیرعامل (`CEO_APPROVAL_THRESHOLD`) و سقف تأیید گروهی (`BULK_APPROVE_LIMIT`) در `TB_TR_SETTING` — بدون هیچ hardcode یا پیش‌فرض در کد/DDL؛ تا وقتی مدیر مالی واحد آن‌ها را تعریف نکند، Submit با ۴۰۹ رد می‌شود.
2. نقش‌های خزانه (`TB_TR_ROLE`) فهرستی کاملاً جدا از `TB_PC_REVIEWER` است — یک واحد می‌تواند FinanceManager متفاوتی برای تنخواه و خزانه داشته باشد.
3. تأیید فاکتور تیکی دستی ثبت‌کننده است (`INVOICE_APPROVED`) — ماژول فاکتور مستقلی وجود ندارد.

**محاسبهٔ VAT/کسور بیمه (تصمیم این پیاده‌سازی، مستندشده در `PaymentRequestAmountCalculator`):** اگر درصد وارد شود، سرور مبلغ را از روی آن حساب می‌کند (مبلغ ورودی نادیده گرفته می‌شود). اگر درصد خالی باشد، مبلغ ورودی مستقیماً استفاده می‌شود (برای کسور مقطوع بدون درصد مشخص). `NET_PAYABLE_AMOUNT` همیشه = `AMOUNT_BEFORE_TAX + VAT_AMOUNT − INSURANCE_DEDUCTION_AMOUNT`، هرگز ورودی مستقیم.

**گردش (`PaymentRequestState`):** `Draft=1 → PendingUnitManager=2 → PendingFinanceManager=3 → (فقط اگر NET_PAYABLE_AMOUNT > آستانه) PendingCeo=4 → ReadyForExecution=5`؛ از هر Pending: `Returned=6` (دلیل اجباری، دوباره Submit از مدیر واحد شروع می‌شود) یا `Rejected=7` (پایانی). نقش لازم هر مرحله: `PaymentRequestStageRoleMap` (مشترک بین `PaymentRequestApprovalService` و کارتابل).

**SoD (`PaymentRequestApprovalService`):** تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کننده (۴۰۹، `PaymentRequestApproverConflictException`)؛ تأییدکنندهٔ یک مرحله ≠ تأییدکنندهٔ مرحلهٔ بلافصل قبل — با خواندن آخرین رویداد `Approve` همان درخواست (۴۰۹، `PaymentRequestConsecutiveApproverConflictException`). این قاعدهٔ دوم فقط روی `Approve` چک می‌شود، نه `Return`/`Reject` (آن‌ها گذار در زنجیرهٔ تأیید نیستند).

**قواعد Submit (`IPaymentRequestSubmitRuleChecker`، مشترک بین Create+submit و Submit جدا):** تنظیمات واحد باید وجود داشته باشد (۴۰۹) · `DUE_DATE` نباید گذشته باشد (۴۰۰) · `INVOICE_REF` پر بدون `INVOICE_APPROVED` (۴۰۰) · تکراری بودن (شناسهٔ ملی + شمارهٔ فاکتور، هر دو غیرخالی) بین درخواست‌های زندهٔ غیر-Rejected (۴۰۹). `PAYMENT_ACCOUNT_ID`/`EXPENSE_ACCOUNT_ID` همیشه (نه فقط Submit) در برابر `TB_ACCOUNT`/`TB_ACCOUNTCODE` چک می‌شوند (۴۰۴) — این دو ستون FK واقعی در EF ندارند (ریسک #۹/#۱۴ تکرارشده، نه استثنای جدید).

**تأیید گروهی:** فقط درخواست‌های `NET_PAYABLE_AMOUNT ≤ BULK_APPROVE_LIMIT` در مرحلهٔ نقش کاربر؛ all-or-nothing با `failedIds` (همان الگوی `bulk-approve` تنخواه).

**نقش‌ها — bootstrap:** ایجاد/حذف نقش و تنظیمات فقط `FinanceManager` همان واحد، **به‌جز**: اگر واحد هیچ `FinanceManager` فعالی ندارد، هر کاربر احرازشدهٔ واحد می‌تواند یک نقش ثبت کند (`CreateTreasuryRoleCommandHandler`، `ITreasuryRoleRepository.HasActiveFinanceManagerAsync`) — تنها راه خروج از بن‌بست «هیچ‌کس نمی‌تواند اولین FinanceManager را بسازد».

**کارتابل تأیید (`GetApprovalCartableQueryHandler`):** دو منبع مستقل، merge در C# (هرگز join سراسری روی جدول‌های دو ماژول): (۱) درخواست‌های پرداخت Pending* این واحد، (۲) ترمیم‌های تنخواه `PendingTreasurer` از `IPettyCashReplenishmentReadRepository` موجود (فقط نمایشی — این ماژول چیزی به تنخواه اضافه/تغییر نمی‌دهد؛ `pendingForMe` از نقش `Treasurer` در `TB_PC_REVIEWER` همان تنخواه، با یک کوئری per-fund). مرتب بر اساس عمر (قدیمی‌ترین اول)، صفحه‌بندی در حافظه.

### Endpointها (`api/treasury/...`، همه `IVahedScoped`، فقط `GET`/`POST`)

```
GET/POST api/treasury/settings                                    { ceoApprovalThreshold, bulkApproveLimit }
GET      api/treasury/roles
POST     api/treasury/roles                                       { userId, userName?, role }
POST     api/treasury/roles/{id}/delete

GET  api/treasury/payment-requests?state=&search=&pageNumber=&pageSize=   → { page, stateCounts }
GET  api/treasury/payment-requests/{id}                            → + events
POST api/treasury/payment-requests          body کامل + submit      → 201 { id }
POST api/treasury/payment-requests/{id}/update
POST api/treasury/payment-requests/{id}/delete
POST api/treasury/payment-requests/{id}/submit
POST api/treasury/payment-requests/{id}/approve          { note? }
POST api/treasury/payment-requests/{id}/return           { reason }
POST api/treasury/payment-requests/{id}/reject           { reason }
POST api/treasury/payment-requests/bulk-approve          { ids: [] }
GET  api/treasury/approval-cartable?pageNumber=&pageSize=
```

فایل‌ها: `Accounting.Application/Treasury/**`، `Accounting.Infrastructure/Repositories/{PaymentRequest,TreasurySetting,TreasuryRole}*.cs`، `Accounting.Api/Controllers/TreasuryController.cs`.
