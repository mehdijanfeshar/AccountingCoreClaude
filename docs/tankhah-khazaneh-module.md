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

نقشهٔ راه: **۴-الف** درخواست پرداخت + کارتابل تأیید (پیاده شد) · **۴-ب** اجرای پرداخت (ص ۱۷ پاورپوینت؛ پر کردن Legacy `TB_PAYRECIVHEAD/DETAIL` + دو سند GL «شناسایی بدهی» و «پرداخت»، ص ۲۰ — پیاده شد، ۲۰۲۶-۰۹-۲۹؛ جایگزینی `record-payment` ترمیم تنخواه هنوز باز ماند) · **۴-ج** دریافت و انتقال وجه (ص ۱۸–۱۹ — پیاده شد، ۲۰۲۶-۰۹-۲۹) · **۴-د** مغایرت‌گیری (ص ۲۱) + داشبورد خزانه (ص ۱۴). ۴-د هنوز طراحی/پیاده نشده.

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
GET  api/treasury/beneficiary-tafsilis?search=&pageNumber=&pageSize=
```

فایل‌ها: `Accounting.Application/Treasury/**`، `Accounting.Infrastructure/Repositories/{PaymentRequest,TreasurySetting,TreasuryRole,TreasuryBeneficiaryTafsili}*.cs`، `Accounting.Api/Controllers/TreasuryController.cs`.

### اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — تفصیلی چندسطحی مرکز هزینه + تفصیلی ذی‌نفع اختیاری

دو تصمیم صاحب پروژه، هر دو روی DDL اجرانشدهٔ `051_treasury_payment_request.sql` (در جا ویرایش شد، فایل جدید ساخته نشد):

**آ) مرکز هزینه اکنون چندسطحی است.** ستون تک‌سطحی `COST_CENTER_TAFSILI_ID` حذف شد؛ به‌جایش جدول جدید `TB_TR_PAYMENT_REQUEST_LINK_TAFSILI` (هم‌شکل دقیق `TB_PC_FUND_LINK_TAFSILI` بخش ۳-ب/۹ — `ID, PAYMENT_REQUEST_ID, TAFSILI_ID, LEVEL_ID` + ستون‌های audit + `ISDELETED`؛ FK روی `TB_TR_PAYMENT_REQUEST`/`TB_TAFSILI`/`TB_LEVEL_TAFSIL`). جدول permanently-embedded — بدون `AddAsync`/`GetForUpdateAsync` مستقل (`IPaymentRequestRepository.AddCostCenterTafsiliLinkAsync`/`GetActiveCostCenterTafsiliLinksAsync`، parent-scoped، `NoIndependentLinkTableWritePathTests`-safe). `Create`/`UpdatePaymentRequestCommand` اکنون `costCenterTafsilis: [{ tafsiliId, levelId }]` می‌گیرند (Update = جایگزینی کامل، reconcile هم‌شکل `UpsertPettyCashFundTafsilisCommandHandler`). الزام سطح‌ها با همان `IVoucherTafsiliLevelGuard.EnsureSatisfiedAsync(expenseAccountId, links)` سند حسابداری کنترل می‌شود (خطاهای موجودِ ۴۰۰ — سطح الزامی جا افتاده / سطح غیرمجاز)؛ وجود هر `tafsiliId` هم جدا چک می‌شود (۴۰۴). منطق مشترک: `IPaymentRequestTafsiliValidator` (`Accounting.Application/Treasury/Commands/Common`)، هم در Create هم در Update فراخوانی می‌شود.

**ب) تفصیلی ذی‌نفع اختیاری، از یک گروه تفصیلی تعریف‌شده به‌ازای واحد.** `TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID CHAR(36) NULL` اضافه شد (بدون FK واقعی روی `TB_TAFSIL_GROUP`، وجودش سمت Application چک می‌شود). `POST api/treasury/settings` این مقدار را می‌گیرد (۴۰۴ اگر گروه وجود نداشته باشد/حذف‌شده باشد). `Create`/`UpdatePaymentRequestCommand` همان `beneficiaryTafsiliId` قبلی را (که همیشه null بود) اکنون واقعاً می‌پذیرند: اگر مقدار دارد ولی واحد گروهی تعریف نکرده → ۴۰۹ (`PaymentRequestBeneficiaryGroupNotConfiguredException`)؛ اگر گروه تعریف شده ولی تفصیلی عضوش نیست (چک از `TB_TAFSIL_LINK_TAFSILGROUP`، غیرحذف‌شده) → ۴۰۰ (`PaymentRequestBeneficiaryTafsiliNotInGroupException`). Endpoint جدید `GET api/treasury/beneficiary-tafsilis?search=&pageNumber=&pageSize=` برای picker فرانت — گروه واحد را از `TB_TR_SETTING` می‌خواند و صفحه‌ای از تفصیلی‌های عضو آن گروه برمی‌گرداند؛ صفحهٔ خالی (نه خطا) اگر گروهی تعریف نشده.

**Detail DTO (`GET payment-requests/{id}`)** اکنون `costCenterTafsilis: [{ levelId, levelName, tafsiliId, tafsiliCode, tafsiliName }]` (به‌جای `costCenterTafsiliId`) و `beneficiaryTafsiliCode`/`beneficiaryTafsiliName` را کنار `beneficiaryTafsiliId` برمی‌گرداند. List DTO تغییری نکرد (بدون نمایش تفصیلی — ارزان نگه داشته شد).

فایل‌های تازه/تغییریافتهٔ کلیدی: `TB_TR_PAYMENT_REQUEST_LINK_TAFSILI` (Domain entity + `LegacyDbContext` mapping)، `Accounting.Application/Treasury/Commands/Common/{PaymentRequestTafsiliLinkInput,IPaymentRequestTafsiliValidator,PaymentRequestTafsiliValidator}.cs`، `Accounting.Application/Common/Interfaces/ITreasuryBeneficiaryTafsiliReadRepository.cs` + `Accounting.Infrastructure/Repositories/TreasuryBeneficiaryTafsiliReadRepository.cs`، `Accounting.Application/Treasury/Queries/GetBeneficiaryTafsilis/**`، دو exception جدید (`PaymentRequestBeneficiaryGroupNotConfiguredException` ۴۰۹، `PaymentRequestBeneficiaryTafsiliNotInGroupException` ۴۰۰) در `GlobalExceptionHandler`.

### بخش ۴-ب (۲۰۲۶-۰۹-۲۹، پیاده شد): اجرای پرداخت + دو سند GL خودکار

DDL: `backend/db/052_treasury_payment_execution.sql` (اجرا نشده) — فقط `ALTER` روی `TB_TR_SETTING`/`TB_TR_PAYMENT_REQUEST` (جدول جدیدی اضافه نشد).

**تصمیم‌های صاحب پروژه (۲۰۲۶-۰۹-۲۹):**
1. **بدون یکپارچگی بانکی.** خزانه‌دار خودش در بانک پرداخت می‌کند، بعد اجرا را در برنامه ثبت می‌کند: شمارهٔ پیگیری/مرجع بانکی (الزامی)، تاریخ واقعی پرداخت (شمسی، الزامی)، شبای مقصد (اختیاری، فرمت `IR` + ۲۴ رقم چک می‌شود، بدون checksum)، روش پرداخت (اختیاری، اگر بیاید `PAYMENT_METHOD` درخواست را بازنویسی می‌کند). بدون OTP.
2. **سند ۱ «شناسایی بدهی» درست در لحظهٔ تأیید نهایی صادر می‌شود** — همان لحظه‌ای که درخواست به `ReadyForExecution` می‌رسد (از تأیید تکی و از تأیید گروهی، هرکدام آخرین مرحله باشد: مدیر مالی یا مدیرعامل)، در همان `SaveChangesAsync` گذار وضعیت (`PaymentRequestApprovalService.ApproveAsync`، نه یک handler جدا) — اگر ساخت سند شکست بخورد، خود تأیید هم رد می‌شود (۴۰۹) و هیچ‌چیز ذخیره نمی‌شود. تاریخ سند = تاریخ تأیید (امروز شمسی، `PersianCalendar`، نه ورودی کاربر).
   ردیف‌ها: بدهکار حساب هزینه (`EXPENSE_ACCOUNT_ID`) = `AMOUNT_BEFORE_TAX` با تفصیلی(های) مرکز هزینهٔ درخواست · بدهکار حساب اعتبار مالیات بر ارزش‌افزوده = `VAT_AMOUNT` (فقط اگر > ۰) · بستانکار حساب بستانکاران = `NET_PAYABLE_AMOUNT` · بستانکار حساب بستانکاران بیمه = `INSURANCE_DEDUCTION_AMOUNT` (فقط اگر > ۰). تراز به‌خودیِ‌خود برقرار است (`before+vat=net+insurance`)، ولی پیش از ذخیره صریحاً هم چک می‌شود (`PaymentRequestVoucherUnbalancedException`، دفاعی).
3. **سند ۲ «پرداخت» در لحظهٔ `execute`**: بدهکار بستانکاران = `NET`، بستانکار بانک معین = `NET`. بانک معین = `TB_ACCOUNT.ACCOUNTCODE_ID` حساب بانکی درخواست (`PAYMENT_ACCOUNT_ID`)؛ تفصیلی بانک از `TB_ACCOUNT_LINK_TAFSILI` همان حساب بانکی (مستقیم کپی، بدون منطق cardinality).
4. **حساب‌های بستانکاران/اعتبار مالیات/بستانکاران بیمه از تنظیمات خزانه می‌آیند** — سه ستون nullable تازه در `TB_TR_SETTING`: `PAYABLES_ACCOUNT_ID`، `VAT_CREDIT_ACCOUNT_ID`، `INSURANCE_PAYABLE_ACCOUNT_ID` (شناسهٔ `TB_ACCOUNTCODE`، بدون FK واقعی — همان الگوی ریسک #۹/#۱۴). `POST api/treasury/settings` هر سه را می‌گیرد (۴۰۴ اگر وجود نداشته باشند)؛ `GET/POST` کد+نام هرکدام را هم برمی‌گرداند. اگر حسابی لازم ولی تنظیم‌نشده باشد ⇒ ۴۰۹ (`PaymentRequestTreasurySettingAccountMissingException`).
   تفصیلی ردیف بستانکاران = تفصیلی ذی‌نفع درخواست، با منطق cardinality (`IPaymentRequestPayablesTafsiliResolver`، مشترک بین سند ۱ و ۲): صفر سطح الزامی ⇒ بدون لینک؛ دقیقاً یک سطح ⇒ `BENEFICIARY_TAFSILI_ID` در همان سطح (نبودش ⇒ ۴۰۹ «برای این حساب بستانکاران، تفصیلی ذی‌نفع لازم است»)؛ بیش از یک سطح، یا حساب اعتبار مالیات/بستانکاران بیمه با هر سطح الزامی ⇒ ۴۰۹ خطای پیکربندی با نام حساب (`PaymentRequestVoucherAccountConfigException`). لینک‌های نهایی همه‌جا از `IVoucherTafsiliLevelGuard.EnsureSatisfiedAsync` هم رد می‌شوند (دفاع مضاعف؛ شکست آن به `PaymentRequestVoucherTafsiliMissingException` ترجمه می‌شود).
5. **هر دو سند «موقت»‌اند** (`DOCLIFE=Temporary`, `ISAUTOMATIC=true`) — همان الگوی سند تسویهٔ تنخواه (بخش ۳-ب). شرح فارسی شامل کد درخواست و نام ذی‌نفع.

**گردش اجرا (`PaymentRequestState`):** `Executed=8`، `Suspended=9` تازه‌اند. `ReadyForExecution → Executed` (`execute`)، `ReadyForExecution ↔ Suspended` (`suspend` با دلیل اجباری / `resume`). از `ReadyForExecution`/`Suspended` بازگشت/رد وجود ندارد (سند شناسایی بدهی از قبل صادر شده). فقط کاربر با نقش `Treasurer` در `TB_TR_ROLE` همان واحد می‌تواند `execute`/`suspend`/`resume` کند (`IPaymentRequestExecutionAuthorizer`، ۴۰۳ `PaymentRequestTreasurerRoleRequiredException` اگر نداشته باشد)؛ SoD فقط روی `execute`: اجراکننده ≠ ثبت‌کننده (۴۰۹ `PaymentRequestExecutorConflictException` — کلمهٔ «اجراکننده»، نه «تأییدکننده/برگشت‌دهنده/ردکننده» تا با پیام SoD زنجیرهٔ تأیید اشتباه نشود).

`execute` همچنین Legacy `TB_PAYRECIVHEAD`/`TB_PAYRECIVDETAIL`/`TB_PAYRECIVDETAIL_LINK_TAFSILI` می‌نویسد (`PAYRECIVTYPE=Pay`، `VOUCHERSHEAD_ID`=سند ۲؛ ردیف‌های Detail دقیقاً همان دو ردیف سند ۲ را آینه می‌کنند — `IPaymentRequestPaymentVoucherBuilder` خط‌ها را با تفصیلی‌های حل‌شده برمی‌گرداند تا `ExecutePaymentRequestCommandHandler` چیزی را دوباره حساب نکند) و `PAYRECIVHEAD_ID` درخواست را ست می‌کند. کد `PAYRECIVCODE` (ستون فقط ۵ کاراکتری Legacy — جا برای پیشوند نیست، برخلاف `CODE` خودِ `TB_TR_PAYMENT_REQUEST`) با شمارندهٔ تازهٔ `IPayReciveHeadRepository.GetNextCodeAsync(vahedCode, year)` ساخته می‌شود (همان الگوی client-side-parsed-MAX، صفر تا حالا الگویی برای این ستون وجود نداشت — تصمیم این پیاده‌سازی). `TB_PAYRECIVDETAIL`/`TB_PAYRECIVDETAIL_LINK_TAFSILI` نوشتن مستقل ندارند؛ دو متد parent-scoped تازه روی همان `IPayReciveHeadRepository` اضافه شد (`AddDetailAsync`/`AddDetailTafsiliLinkAsync`) — رجوع بخش ۲ `docs/tamin-core-entity-reference.md`: در پروژهٔ مرجع `PayReciveDetail` کپسوله نیست، ولی این پیاده‌سازی همچنان CRUD مستقلی برایش نساخت چون بخش ۴-ب تنها مسیر نوشتن آن است و همیشه کنار یک `TB_PAYRECIVHEAD` تازه در همان تراکنش.

**تراکنش/Concurrency:** `ExecutePaymentRequestCommandHandler` تراکنش صریح `BeginTransactionAsync`/`CommitTransactionAsync` دارد (سند + PayReciv + گذار وضعیت، همان الگوی `FinalizePettyCashSettlementCommandHandler`). `PaymentRequestLiabilityVoucherBuilder` (سند ۱، داخل `PaymentRequestApprovalService.ApproveAsync`) به‌جای تراکنش صریح یک رزرو شمارهٔ سند درون‌حافظه‌ای per-(vahedCode,year) نگه می‌دارد (Scoped، طول عمر یک HTTP request) — چون `GetNextDocNumAsync` روی خودِ دیتابیس (`AsNoTracking`) می‌خواند نه روی change tracker، `bulk-approve`ای که چند درخواست را در یک فراخوانی به `ReadyForExecution` می‌برد بدون این رزرو همان «شمارهٔ بعدی» را چندبار می‌گرفت و `DOC_NUM` تکراری می‌ساخت — تصمیم این پیاده‌سازی، مثل حافظهٔ per-معین در `VoucherTafsiliLevelGuard`. مسابقهٔ بین دو HTTP request همزمان همچنان همان ریسک پذیرفته‌شدهٔ مستندشدهٔ هر تولیدکنندهٔ `DOC_NUM` دیگر این پروژه است (بدون UNIQUE constraint دیتابیسی).

**رویدادها:** چهار مقدار تازه در `PaymentRequestEventAction`: `LiabilityVoucherIssued=8` (کنار رویداد `Approve` همان گذار، `FROM_STATE=TO_STATE=ReadyForExecution` — گذار وضعیتی نیست، فقط رویداد الحاقی)، `Execute=9`، `Suspend=10`، `Resume=11`.

**ستون‌های تازهٔ `TB_TR_PAYMENT_REQUEST`:** `LIABILITY_VOUCHER_ID`، `PAYMENT_VOUCHER_ID`، `BANK_REFERENCE`، `PAID_DATE`، `DESTINATION_IBAN`، `EXECUTED_BY`، `EXECUTED_DATE` (timestamp)، `SUSPEND_REASON`.

### Endpointها (بخش ۴-ب، `api/treasury/...`، همه `IVahedScoped`، فقط `GET`/`POST`)

```
POST payment-requests/{id}/execute   { bankReference, paidDate, destinationIban?, paymentMethod? }
POST payment-requests/{id}/suspend   { reason }
POST payment-requests/{id}/resume
GET  payment-requests/{id}/accounting  → { liabilityVoucher?, paymentVoucher?, payRecivCode? }
     هرکدام از دو سند: { id, voucherNumber, date, state, lines:[{accountCode,accountName,tafsiliLabels,debit,credit}], totalDebit, totalCredit }
```

`GET payment-requests?...` یک پارامتر تازه گرفت: `forExecution=true` (نه `state` چندمقداری با کاما — تصمیم این پیاده‌سازی، سادگی) — وقتی `true` باشد `state` نادیده گرفته می‌شود و فقط `ReadyForExecution`+`Suspended` با هم برمی‌گردند (صفحهٔ اجرای پرداخت خزانه‌دار این دو را با هم لازم دارد). Detail DTO (`GET payment-requests/{id}`) اکنون `liabilityVoucherId`/`liabilityVoucherNumber`/`paymentVoucherId`/`paymentVoucherNumber`/`bankReference`/`paidDate`/`destinationIban`/`executedBy`/`executedDate`/`suspendReason` هم دارد. `stateCounts` (بدون تغییر ساختار) اکنون مقادیر ۸/۹ را هم می‌تواند برگرداند چون از `GroupBy` زنده می‌آید.

**خارج از دامنه (باز ماند برای بعد، مستندشده طبق خواستهٔ صاحب پروژه):** جایگزینی `record-payment` ترمیم تنخواه (بخش ۳-الف) با این الگوی سند GL — دست‌نخورده ماند. کنترل موجودی بانک قبل از `execute` انجام نمی‌شود.

فایل‌های تازه/تغییریافتهٔ کلیدی: `backend/db/052_treasury_payment_execution.sql`؛ `TB_TR_SETTING`/`TB_TR_PAYMENT_REQUEST` (ستون‌های تازه، Domain entity + `LegacyDbContext` mapping)؛ `Accounting.Application/Treasury/Commands/Common/{IPaymentRequestPayablesTafsiliResolver,PaymentRequestPayablesTafsiliResolver,PaymentRequestVoucherBuildResult,IPaymentRequestLiabilityVoucherBuilder,PaymentRequestLiabilityVoucherBuilder,IPaymentRequestPaymentVoucherBuilder,PaymentRequestPaymentVoucherBuilder,IPaymentRequestExecutionAuthorizer,PaymentRequestExecutionAuthorizer}.cs`؛ `Accounting.Application/Treasury/Commands/{ExecutePaymentRequest,SuspendPaymentRequest,ResumePaymentRequest}/**`؛ `Accounting.Application/Treasury/Queries/{PaymentRequestAccountingDto.cs,GetPaymentRequestAccounting/**}`؛ `IPayReciveHeadRepository`/`PayReciveHeadRepository` (سه متد تازه)؛ هفت exception تازه (`PaymentRequestTreasurySettingAccountMissingException`، `PaymentRequestVoucherAccountConfigException`، `PaymentRequestPayablesBeneficiaryRequiredException`، `PaymentRequestVoucherTafsiliMissingException`، `PaymentRequestVoucherUnbalancedException`، `PaymentRequestExecutorConflictException`، `PaymentRequestTreasurerRoleRequiredException`) در `GlobalExceptionHandler`؛ `TreasuryController` (سه endpoint تازه + `UpsertTreasurySettingRequest`/`TreasurySettingDto` گسترش‌یافته).

### بخش ۴-ج (۲۰۲۶-۰۹-۲۹، پیاده شد): دریافت وجه + انتقال وجه

DDL: `backend/db/053_treasury_receipt_transfer.sql` (اجرا نشده) — دو جدول کاملاً جدید (`TB_TR_RECEIPT`، `TB_TR_TRANSFER`) + یک جدول رویداد (`TB_TR_TRANSFER_EVENT` — دریافت رویداد ندارد، «keep it simple») + سه ستون تازه روی `TB_TR_SETTING` (`RECEIVABLES_ACCOUNT_ID`، `CUSTOMER_TAFSIL_GROUP_ID`، `DAILY_TRANSFER_LIMIT`، هر سه بدون پیش‌فرض).

**تصمیم‌های صاحب پروژه (۲۰۲۶-۰۹-۲۹):**
1. دریافت وجه بدون تخصیص فاکتور فروش (ماژول فاکتور فروش نداریم) — فقط `INVOICE_REF` آزاد اختیاری.
2. پرداخت‌کننده (`PAYER_TAFSILI_ID`) از یک گروه تفصیلی «مشتریان» جداگانه می‌آید (`TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID`، مستقل از `BENEFICIARY_TAFSIL_GROUP_ID` بخش ۴-الف)؛ حساب دریافتنی هم در تنظیمات (`RECEIVABLES_ACCOUNT_ID`).
3. کنترل‌های انتقال مسدودکننده‌اند: موجودی مبدأ کافی + سقف روزانهٔ انتقال (تنظیم، بدون پیش‌فرض؛ نبودش ⇒ ۴۰۹).
4. هر سند GL خودکار این بخش «موقت» است (`DOCLIFE=Temporary`, `ISAUTOMATIC=true`) — همان الگوی بخش‌های ۴-الف/۴-ب.

**تصمیم‌های این پیاده‌سازی (مستندشده، صاحب پروژه صراحتاً نپرسیده بود):**
- دریافت وجه گردش تأیید ندارد (بدون جدول رویداد) — فقط `Draft → Registered` (فقط خزانه‌دار) یا `Draft → Cancelled`؛ ثبت‌شده از طریق سندش اصلاح می‌شود، نه از این مسیر.
- Update/Delete/Cancel دریافت فقط چک وضعیت (Draft) دارند، بدون قید «فقط ثبت‌کننده» — برخلاف درخواست پرداخت بخش ۴-الف؛ مطابق دستور صریح «keep it simple» صاحب پروژه.
- انتقال وجه SoD (تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کننده) روی هر سه اقدام خزانه‌دار (`approve`/`return`/`reject`) اعمال شد، نه فقط `approve` — همان الگوی `PaymentRequestApprovalService`، چون صاحب پروژه فقط explicit روی `approve` گفته بود.
- «GL balance» بانک معین (`ITreasuryBankAccountBalanceReadRepository`) روی حساب معین بانک، محدود به ردیف‌هایی که همهٔ تفصیلی‌های همان حساب بانکی (`TB_ACCOUNT_LINK_TAFSILI`) را دارند، محاسبه می‌شود (چند حساب بانکی معمولاً یک معین مشترک دارند؛ فقط حساب بانکی بدون تفصیلی روی کل معین حساب می‌شود) و شامل اسناد موقت هم می‌شود؛ `GET bank-accounts/{id}/balance` سال شمسی جاری را می‌خواند (سرور محاسبه می‌کند)، `approve` انتقال سال `TRANSFER_DATE` خودِ انتقال را.
- «هر دو حساب بانکی متعلق به واحد باشند» با همان الگوی موجود پروژه (404 اگر نباشد، 403 اگر واحد دیگری باشد) پیاده شد، نه ۴۰۰ آزاد — برای هم‌راستایی با بقیهٔ پروژه؛ فقط «مبدأ ≠ مقصد» واقعاً ۴۰۰ است (FluentValidation).
- تفصیلی «حساب‌های دریافتنی» با همان قاعدهٔ cardinality resolver بستانکاران بخش ۴-ب حل می‌شود (۰ سطح ⇒ بدون لینک، ۱ سطح ⇒ `PAYER_TAFSILI_ID`، >۱ سطح ⇒ خطای پیکربندی) — چون `PAYER_TAFSILI_ID` همیشه الزامی است (برخلاف ذی‌نفع اختیاری بخش ۴-الف)، حالت «تفصیلی لازم ولی نیامده» اصلاً رخ نمی‌دهد.

**Reuse از بخش ۴-ب (به‌جای کپی):** `PaymentRequestVoucherBuildResult`/`PaymentRequestVoucherLineResult` (نتیجهٔ ساخت سند) عیناً همان رکوردها؛ `IPayReciveHeadRepository` (دریافت وجه از همان متدهای `GetNextCodeAsync`/`AddAsync`/`AddDetailAsync`/`AddDetailTafsiliLinkAsync` استفاده می‌کند، با `PAYRECIVTYPE=Recive`)؛ `IVoucherTafsiliLevelGuard`/`ITafsiliLookupReadRepository` برای الگوی cardinality؛ `ITreasuryBeneficiaryTafsiliReadRepository` عیناً (بدون تغییر) برای `customer-tafsilis` هم استفاده شد — این رپازیتوری از قبل روی `tafsilGroupId` جنریک بود. جدید استخراج‌شده: `IVoucherAccountingReader` — پروجکشن سند GL که قبلاً private در `PaymentRequestReadRepository` بود، حالا سرویس مشترک بین `payment-requests/{id}/accounting`/`receipts/{id}/accounting`/`transfers/{id}/accounting`.

**گردش‌ها:** `ReceiptState`: Draft=1 → Registered=2 (`register`, فقط خزانه‌دار) یا Cancelled=3 (`cancel`, فقط از Draft). `TransferState`: Draft=1 ⇄ Returned=4 → (`submit`) PendingTreasurer=2 → (`approve`, فقط خزانه‌دار، ≠ ثبت‌کننده) Executed=3، یا (`return`/`reject`) Returned=4/Rejected=5.

**تراکنش/Concurrency:** `RegisterReceiptCommandHandler`/`CreateReceiptCommandHandler` (وقتی `register=true`) و `ApproveTransferCommandHandler` هر سه `BeginTransactionAsync`/`CommitTransactionAsync` صریح دارند (سند GL + نوشتن Legacy/گذار وضعیت در یک تراکنش) — همان الگوی `ExecutePaymentRequestCommandHandler`. بدون رزرو DOC_NUM درون‌حافظه‌ای (بدون bulk-approve در این بخش، پس بدون آن ریسک).

### Endpointها (بخش ۴-ج، `api/treasury/...`، همه `IVahedScoped`، فقط `GET`/`POST`)

```
GET/POST receipts ; GET receipts/{id} ; POST receipts/{id}/update|delete|register|cancel ; GET receipts/{id}/accounting
GET/POST transfers ; GET transfers/{id} ; POST transfers/{id}/update|delete|submit|approve|return|reject ; GET transfers/{id}/accounting
GET bank-accounts/{id}/balance ; GET customer-tafsilis
```

`POST/GET api/treasury/settings` سه فیلد تازه گرفت/برگرداند: `receivablesAccountId`/`customerTafsilGroupId`/`dailyTransferLimit` (+ کد/نام نمایشیِ دو مورد اول). کارتابل تأیید (`GetApprovalCartableQueryHandler`) سومین منبع را اضافه کرد: انتقال‌های `PendingTreasurer`، با `pendingForMe` از همان `callerTreasuryRoles` (نقش `Treasurer`) قبلاً واکشی‌شده برای درخواست پرداخت.

⚠️ **collision نام‌گذاری کشف‌شده:** یک `ReceiptsController`/`TB_RECEIP` کاملاً مستقل و قبلی (`api/receipts`، فيش/حوالهٔ بانکی Legacy) از قبل در پروژه وجود داشت؛ DTOهای Api-layer این بخش (`Create/Update/Delete/Register/CancelReceipt*`) به `CreateTreasuryReceiptRequest`/... تغییر نام یافتند تا با آن تصادم نکنند — نام‌های Application-layer (`ReceiptDto` در `Treasury.Queries`) چون namespace متفاوت دارند تصادم نکردند.

فایل‌های تازه/تغییریافتهٔ کلیدی: `backend/db/053_treasury_receipt_transfer.sql`؛ `TB_TR_RECEIPT`/`TB_TR_TRANSFER`/`TB_TR_TRANSFER_EVENT` (Domain entity + `LegacyDbContext` mapping) + سه ستون تازهٔ `TB_TR_SETTING`؛ enumهای تازه `ReceiptState`/`TransferState`/`TransferEventAction`؛ `Accounting.Application/Treasury/Commands/Common/{ITreasuryTreasurerAuthorizer,TreasuryTreasurerAuthorizer,IReceiptPayerValidator,ReceiptPayerValidator,IReceiptVoucherBuilder,ReceiptVoucherBuilder,IReceiptRegistrationService,ReceiptRegistrationService,ITransferVoucherBuilder,TransferVoucherBuilder,ITransferApprovalService,TransferApprovalService}.cs`؛ `Accounting.Application/Treasury/Commands/{CreateReceipt,UpdateReceipt,DeleteReceipt,RegisterReceipt,CancelReceipt,CreateTransfer,UpdateTransfer,DeleteTransfer,SubmitTransfer,ApproveTransfer,ReturnTransfer,RejectTransfer}/**`؛ `Accounting.Application/Treasury/Queries/{ReceiptDto,ReceiptListItemDto,ReceiptAccountingDto,TransferDto,TransferListItemDto,TransferAccountingDto,TransferEventDto,BankAccountBalanceDto}.cs` + `Accounting.Application/Treasury/Queries/{GetReceipts,GetReceiptById,GetReceiptAccounting,GetTransfers,GetTransferById,GetTransferAccounting,GetBankAccountBalance,GetCustomerTafsilis}/**`؛ `IVoucherAccountingReader`/`VoucherAccountingReader` (استخراج‌شده از `PaymentRequestReadRepository`)؛ `ITreasuryReceiptRepository`/`TreasuryReceiptRepository`، `ITreasuryReceiptReadRepository`/`TreasuryReceiptReadRepository`، `ITreasuryTransferRepository`/`TreasuryTransferRepository`، `ITreasuryTransferReadRepository`/`TreasuryTransferReadRepository`، `ITreasuryTransferEventRepository`/`TreasuryTransferEventRepository`، `ITreasuryBankAccountBalanceReadRepository`/`TreasuryBankAccountBalanceReadRepository`؛ ده exception تازه (`TreasurySettingValueMissingException`، `TreasuryVoucherAccountConfigException`، `TreasuryVoucherTafsiliMissingException`، `TreasuryTafsiliNotInGroupException`، `TreasuryTreasurerRoleRequiredException`، `TreasuryReceiptStateConflictException`، `TreasuryReceiptDuplicateBankReferenceException`، `TreasuryTransferStateConflictException`، `TreasuryTransferApproverConflictException`، `TreasuryTransferInsufficientBalanceException`، `TreasuryTransferDailyLimitExceededException`) در `GlobalExceptionHandler`؛ `TreasuryController` (۱۹ endpoint تازه + DTOهای `CreateTreasuryReceiptRequest`/... + `UpsertTreasurySettingRequest`/`TreasurySettingDto` گسترش‌یافته).

### بخش ۴-د (۲۰۲۶-۰۹-۳۰، بک‌اند پیاده شد): مغایرت‌گیری بانکی + داشبورد خزانه

**تصمیم صاحب پروژه (۲۰۲۶-۰۹-۲۹):** صورت‌حساب بانک به‌صورت فایل متنی «دیسکت بانک» می‌رسد (قالب را صاحب پروژه بعداً می‌دهد) و ورود دستی ردیف‌ها هم لازم است. ورود دستی کامل پیاده شد؛ برای فایل فقط درز `IBankStatementFileParser` هست و هیچ پیاده‌سازی‌ای ثبت نشده — `POST statements/{id}/import` تا آن زمان ۴۰۹ «قالب فایل دیسکت بانک هنوز تعریف نشده است» می‌دهد. **افزودن قالب = یک کلاس parser + یک خط DI.**

جدول‌ها (DDL `backend/db/054_treasury_bank_reconciliation.sql`، اجرا نشده): `TB_TR_BANK_STATEMENT` (سر: حساب بانکی، بازهٔ تاریخ، مانده طبق بانک، منبع Manual/Import، وضعیت Open/Closed، کد `BST-`) و `TB_TR_BANK_STATEMENT_LINE` (تاریخ، مرجع بانک، شرح، برداشت/واریز — دقیقاً یکی > ۰، مانده، `MATCH_STATE`: Unmatched=1/AutoMatched=2/ManualMatched=3/Resolved=4، ردیف سند تطبیق‌شده، نوع رفع: BankFeeVoucher=1/LinkedReceipt=2/Ignored=3). تنظیم جدید: `TB_TR_SETTING.BANK_FEE_ACCOUNT_ID`.

**تطبیق خودکار:** برای هر ردیف تطبیق‌نیافته، کاندیدها = ردیف‌های سند روی معین بانک با همهٔ تفصیلی‌های همان حساب بانکی، در بازهٔ ±۳ روز (`ToleranceDays`)، که هنوز به ردیف زندهٔ دیگری وصل نیستند؛ واریز ↔ بدهکار دفتر، برداشت ↔ بستانکار دفتر. اولویت: (۱) مرجع بانک برابر `BANK_REFERENCE` یک سند خزانه (درخواست پرداخت/دریافت/انتقال) با مبلغ برابر، (۲) مبلغ برابر و همان تاریخ — فقط کاندید یکتا، (۳) مبلغ برابر در بازهٔ تحمل — فقط کاندید یکتا. مبهم → تطبیق‌نیافته می‌ماند.

**رفع ردیف تطبیق‌نیافته:** سند کارمزد (فقط برداشت؛ سند موقت بدهکار حساب کارمزد / بستانکار بانک با تفصیلی بانک) · اتصال به دریافت ثبت‌شدهٔ همان حساب و همان مبلغ (فقط واریز) · نادیده‌گرفتن با یادداشت اجباری. «برگرداندن رفع» برای سند کارمزد فقط وقتی سند هنوز موقت است (سند حذف نرم می‌شود)؛ سند دائم‌شده → ۴۰۹.

**خلاصهٔ صورت‌حساب:** مانده طبق بانک، مانده دفتری تا `TO_DATE` (`ITreasuryBankAccountBalanceReadRepository` با پارامتر اختیاری `asOfDate`)، مغایرت خالص، شمار هر وضعیت، و اقلام «فقط در دفتر».

**داشبورد (`GET dashboard`، ص ۱۴):** موجودی کل و هر حساب بانکی، تعهدات ۷ روز آینده (درخواست‌های آمادهٔ اجرا/معلق) با نسبت پوشش، در انتظار تأیید، دریافت/پرداخت امروز، اقلام باز، هشدار سررسید ≤ ۳ روز.

Endpointها (`api/treasury/...`):
```
GET/POST statements ; GET statements/{id} ; POST statements/{id}/update|delete|close|reopen|auto-match|import
POST statements/{id}/lines ; POST statements/{id}/lines/{lineId}/update|delete|match|unmatch|resolve|unresolve
GET statements/{id}/book-candidates?lineId=
GET dashboard
```
