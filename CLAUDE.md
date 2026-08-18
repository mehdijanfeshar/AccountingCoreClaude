# پروژه: سیستم حسابداری (Accounting System)

این فایل حافظهٔ اصلی پروژه است. Claude Code آن را در ابتدای هر سشن می‌خواند. آن را همیشه به‌روز نگه دار.

## معماری کلی

- **Backend:** .NET، معماری Clean Architecture + CQRS (MediatR)
- **Frontend:** React (Vite + TypeScript)
- **Database:** Oracle
- **کدینگ حسابداری:** ساختار schema Legacy (`CENTRALACCOUNT`) مبنای نهایی است — رجوع به «تصمیم معماری دوم» پایین‌تر. سند `docs/chart-of-accounts.md` (طرح کدینگ شناور با سلسله‌مراتب ثابت گروه/کل/معین) از ۲۰۲۶-۰۸-۱۷ **منسوخ (SUPERSEDED)** است و فقط به‌عنوان سابقهٔ طراحی نگه داشته شده.

### تصمیم معماری: Legacy-as-Domain (۲۰۲۶-۰۸-۱۷)

**به درخواست صریح صاحب پروژه**، قانون قبلی («Entityهای Legacy الزاماً Domain Entity نیستند») معکوس شد:

> **Entityهای Legacy الزاماً Domain Entity هستند.**

پیامدها:
- لایهٔ جداگانهٔ Anti-Corruption بین Legacy و Domain ساخته نمی‌شود.
- Entityهای جدول‌های Legacy در `backend/src/Accounting.Domain/Entity/` با namespace `Accounting.Domain.Entity` زندگی می‌کنند و شهروند درجه‌یک دامنه‌اند. (این مسیر/namespace در ۲۰۲۶-۰۸-۱۸ از `Legacy/Entities/` + `Accounting.Domain.Legacy` تغییر نام یافت — رجوع به «تصمیم معماری سوم» پایین‌تر. نام خودِ تصمیم «Legacy-as-Domain» باقی است چون به مفهوم اشاره دارد نه به namespace.)
- نقش ایجنت `entity-mapper` از «جلوگیری از نشت Legacy به Domain» به «ادغام کنترل‌شدهٔ Legacy در Domain» تغییر کرد.

قیدی که معکوس **نشد** و همچنان برقرار است:
- `Accounting.Domain` هیچ وابستگی خارجی ندارد → فقط POCO به Domain می‌رود؛ `LegacyDbContext` و Fluent Mapping در `Accounting.Infrastructure` می‌مانند.

### تصمیم معماری دوم: Legacy جایگزین کامل مدل Rich (۲۰۲۶-۰۸-۱۷)

**به انتخاب صریح صاحب پروژه** (گزینهٔ «ج» از سه گزینه‌ای که `team-lead` مطرح کرد):

> **مدل Rich کنار گذاشته می‌شود و ساختار Legacy مبنای نهایی مدل نوشتن است.**

کاربر با علم به پیامدها این را انتخاب کرد. invariantهای زیر **آگاهانه** کنار گذاشته شدند:
- تراز اجباری بدهکار/بستانکار سند (`VoucherNotBalancedException`)
- تغییرناپذیری سند پس از Post (`VoucherImmutableException`)
- الزامی‌بودن تفصیلی بر اساس `Requirement` (قانون ۴ در `docs/chart-of-accounts.md`)
- سلسله‌مراتب ثابت سه‌سطحی گروه/کل/معین با طول کد ثابت
- یک‌طرفه بودن بدهکار/بستانکار در هر ردیف

**نحوهٔ اجرا — حذف فیزیکی (انجام شد در ۲۰۲۶-۰۸-۱۷، به درخواست صریح کاربر):**
مرحلهٔ اول با `[Obsolete]` انجام شد (چون git هنوز init نشده بود). پس از اینکه کاربر `git init` + commit اولیه (`9f760ad`) را زد، حذف قابل‌بازگشت شد و کاربر صریحاً حذف فیزیکی را درخواست کرد. **۲۲ فایل حذف شدند:**
- ۹ Entity: `AccountGroup`, `GeneralLedgerAccount`, `SubsidiaryAccount`, `DetailAccount`, `DetailAccountType`, `SubsidiaryDetailTypeLink`, `Voucher`, `VoucherLine`, `VoucherLineDetailValue`
- `Rules/VoucherPostingValidator.cs` (پوشهٔ `Rules/` و `Entities/` خالی و حذف شدند)
- `ValueObjects/DetailRequirement.cs`, `ValueObjects/SubsidiaryDetailPolicy.cs`
- ۳ فایل Exception که پس از حذف بالا هیچ مصرف‌کننده‌ای نداشتند: `VoucherExceptions.cs` (۴ کلاس)، `VoucherLineExceptions.cs` (۴ کلاس)، `DetailTypeExceptions.cs` (۱ کلاس). همچنین `DuplicateAccountCodeException` از داخل `CodingExceptions.cs` حذف شد (بقیهٔ کلاس‌های آن فایل هنوز استفاده می‌شوند).
- ۷ فایل تست وابسته به تایپ‌های حذف‌شده (شامل `TestSupport/DomainFactory.cs` و `TestSupport/EntityIdAssigner.cs`).

**آنچه عمداً باقی ماند** (چون هنوز مصرف‌کنندهٔ واقعی دارند و قابل بازاستفاده در مدل Legacy‌اند):
`Money`, `AccountCode`, `AccountNature`, `VoucherStatus`, `Common/Guard.cs`, `Exceptions/DomainException.cs`, `Exceptions/MoneyExceptions.cs`, `Exceptions/CodingExceptions.cs` (شامل `InvalidAccountCodeException` و `InvalidTitleException`).

تست‌ها از ۳۳ به **۱۲** رسید (`AccountCodeTests` + `MoneyTests`)، همگی سبز. `NoWarn CS0618` از csproj تست حذف شد چون دیگر لازم نیست.

`docs/chart-of-accounts.md` در بالای فایل با بنر ⚠️ SUPERSEDED علامت خورد و **پاک نشد** (به‌عنوان سابقهٔ تصمیم‌های طراحی نگه داشته شد).

⚠️ **پیامدی که باید بدانید:** با این تصمیم، تضمین «بدهکار = بستانکار» دیگر در سطح کد وجود ندارد. اگر بعداً این تضمین لازم شد، باید صریحاً در لایهٔ Application/DB (constraint یا validation) بازسازی شود.

### تصمیم معماری سوم: مسطح‌سازی پوشهٔ Entity و namespace جدید (۲۰۲۶-۰۸-۱۸)

**به درخواست صریح صاحب پروژه** («همهٔ entityها الان داخل پوشهٔ Legacy هستند، در حالی که باید مستقیم داخل Domain/Entity باشند»)، از میان گزینه‌های مطرح‌شده کاربر **مسطح‌سازی کامل + namespace جدید** را انتخاب کرد:

| قبل | بعد |
|---|---|
| `backend/src/Accounting.Domain/Legacy/Entities/*.cs` | `backend/src/Accounting.Domain/Entity/*.cs` |
| `namespace Accounting.Domain.Legacy` | `namespace Accounting.Domain.Entity` |

- پوشهٔ میانی `Entities/` حذف شد (یک سطح مسطح‌تر) و پوشه‌های خالی `Legacy/` و `Legacy/Entities/` پاک شدند.
- انتقال با `git mv` انجام شد تا تاریخچهٔ فایل‌ها حفظ شود.
- **دلیل معنایی:** با تصمیم دوم، این Entityها دیگر «مدل Legacy در کنار مدل اصلی» نیستند؛ آن‌ها **خودِ** مدل دامنه‌اند. نگه‌داشتن نام `Legacy` در مسیر/namespace این پیام غلط را می‌داد که مدل دیگری هم وجود دارد.
- **آنچه عمداً تغییر **نکرد**:** سمت Infrastructure دست‌نخورده ماند — پوشهٔ `backend/src/Accounting.Infrastructure/Legacy/`، نام کلاس `LegacyDbContext` و `GuidToChar36Converter` همگی به همان نام باقی‌اند (آنجا واژهٔ Legacy هنوز درست است: نگاشت به schema قدیمی `CENTRALACCOUNT`). فقط `using` داخل `LegacyDbContext.cs` به `Accounting.Domain.Entity` به‌روز شد.
- **نام خودِ تصمیم‌های «Legacy-as-Domain» و «Legacy جایگزین کامل» معتبر می‌ماند** — آن‌ها به مفهوم اشاره دارند، نه به namespace.

## تیم ایجنت‌ها

کار این پروژه توسط تیمی از ساب‌ایجنت‌های تعریف‌شده در `.claude/agents/` انجام می‌شود:

| ایجنت | مسئولیت |
|---|---|
| `team-lead` | مدیریت و تقسیم وظایف بین سایر ایجنت‌ها؛ همیشه اول این را صدا بزن |
| `accounting-domain` | مدل دامنه و قوانین کسب‌وکار کدینگ شناور |
| `database-oracle` | schema **جدید**، migration، ایندکس، Materialized View در Oracle |
| `database-reverse-engineer` | کشف Read-Only دیتابیس Legacy و Scaffold جدول‌ها به Entity + Fluent Mapping |
| `entity-mapper` | ادغام کنترل‌شدهٔ Legacy Entity در Domain و reconcile مفاهیم هم‌پوشان |
| `backend-dotnet` | Commands/Queries/Handlers، API |
| `api-contract` | قرارداد رسمی OpenAPI/DTO/Error و هماهنگی Backend↔Frontend |
| `frontend-react` | UI، فرم صدور سند با فیلدهای تفصیلی داینامیک |
| `qa-tester` | تست و بررسی کیفیت قبل از commit |
| `security-reviewer` | Gate امنیتی: Auth، دسترسی، داده‌های حساس، Audit |
| `performance-reviewer` | Gate عملکرد: Query Plan، ایندکس، N+1، گزارش‌های حجیم |

**قاعدهٔ کار:** برای هر درخواست جدید، ابتدا `team-lead` را صدا بزن؛ او وظیفه را بین سایر ایجنت‌ها تقسیم می‌کند.

## ساختار پوشه‌ها

```
backend/src/Accounting.Domain          # موجودیت‌ها و قوانین دامنه (صفر وابستگی خارجی)
backend/src/Accounting.Domain/Entity   # ۶۵ Entity (namespace: Accounting.Domain.Entity) — POCO خالص
backend/src/Accounting.Application     # CQRS: Commands/Queries/Handlers
backend/src/Accounting.Infrastructure  # EF Core + Oracle، Repository
backend/src/Accounting.Infrastructure/Legacy  # LegacyDbContext + Fluent Mapping + GuidToChar36Converter
backend/src/Accounting.Api             # Controllers
backend/tests/Accounting.Domain.Tests  # تست واحد قوانین دامنه (xUnit)
frontend/src/features/chart-of-accounts
frontend/src/features/vouchers
docs/chart-of-accounts.md              # مستندسازی کامل منطق کدینگ شناور
docs/progress-log.md                   # لاگ روزانهٔ پیشرفت
```

## وضعیت فعلی پروژه

<!-- team-lead این بخش را در پایان هر جلسه به‌روزرسانی می‌کند -->

- [x] راه‌اندازی اولیه solution و پروژه‌های .NET — `backend/Accounting.sln` با ۴ پروژه روی net10.0؛ رفرنس‌ها طبق Clean Architecture (Api → Application+Infrastructure، Infrastructure → Application، Application → Domain، Domain بدون وابستگی)؛ Swagger با Swashbuckle.AspNetCore روی `/swagger` فعال و توسط qa-tester تأیید شد. فعلاً فقط اسکلت + `HealthController` است و هیچ کد دامنه‌ای نوشته نشده.
- [ ] راه‌اندازی اولیه React (Vite) — طبق تصمیم فعلی، فرانت‌اند تا اطلاع ثانوی متوقف است؛ تمرکز روی backend/.
- [x] ~~طراحی نهایی مدل دامنه کدینگ شناور~~ — **حذف شد (۲۰۲۶-۰۸-۱۷)**. rich domain model ساخته و تست شد، سپس به تصمیم دوم کاربر کنار گذاشته و در نهایت **فیزیکاً حذف شد** (۲۲ فایل). در تاریخچهٔ git تا commit `9f760ad` قابل بازیابی است.
- [x] Reverse Engineering دیتابیس Legacy Oracle (schema `CENTRALACCOUNT`) — کشف Read-Only کامل schema (۶۵ جدول، ۷۷۴ ستون، ۸۲ FK، ۲۹ UNIQUE، ۱ sequence، ۲۸ View) و Scaffold همهٔ ۶۵ جدول به Entity + Fluent Mapping. **فقط Entity و Mapping** — هیچ Business Logic یا Repository.
- [x] اجرای تصمیم معماری Legacy-as-Domain (۲۰۲۶-۰۸-۱۷) — هر ۶۵ کلاس Entity از `Accounting.Infrastructure/Legacy/Entities/` به `Accounting.Domain/Legacy/Entities/` **منتقل** شد (نه کپی) و namespace همه به `Accounting.Domain.Legacy` تغییر کرد. `LegacyDbContext.cs` با همهٔ Fluent Mappingها در `Accounting.Infrastructure/Legacy/` باقی ماند و با `using Accounting.Domain.Legacy;` به Entityهای جدید ارجاع می‌دهد. جهت وابستگی Infrastructure → Domain (مجاز)؛ `Accounting.Domain.csproj` همچنان **صفر** وابستگی خارجی دارد. build با ۰ خطا و ۱۶ warning (همگی NU1903 از قبل موجود، بدون هیچ warning نوع CS) و ۳۳/۳۳ تست سبز. ⚠️ **این مسیر/namespace بعداً در ۲۰۲۶-۰۸-۱۸ تغییر کرد** به `Accounting.Domain/Entity/` و `Accounting.Domain.Entity` (رجوع به «تصمیم معماری سوم»)؛ متن بالا وضعیت همان روز را ثبت می‌کند.
- [x] اجرای تصمیم دوم «Legacy جایگزین کامل» (۲۰۲۶-۰۸-۱۷) — در دو مرحله: اول ۱۲ تایپ با `[Obsolete]` علامت خوردند، سپس **به درخواست صریح کاربر فیزیکاً حذف شدند** (۲۲ فایل شامل ۳ فایل Exception یتیم و ۷ فایل تست). جزئیات کامل در «تصمیم معماری دوم» بالا. اکنون `Accounting.Domain` فقط شامل `Legacy/` (۶۵ Entity) + `ValueObjects/` (۴ تایپ باقی‌مانده) + `Common/Guard.cs` + `Exceptions/` (۳ فایل) است. build ۰ خطا / ۱۶ warning پیش‌موجود (هیچ warning نوع CS)، **۱۲/۱۲ تست سبز**.
- [x] حل ابهام «منبع حقیقت تفصیلی مجاز» — `TB_ACCOUNT_LINK_TAFSILGROUP` منبع حقیقت است (FK `FK_TAFSILGOUP_ACCOUNTCODE` به `TB_ACCOUNTCODE` + UNIQUE `UK_ACCOUNTLINKTAFSILGROUP` روی `ACCOUNT_ID, LEVEL_ID, TAFSILGROUP_ID`). زنجیره: `TB_ACCOUNTCODE → TB_ACCOUNT_LINK_TAFSILGROUP → TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI`.
- [x] تبدیل شناسه‌ها از `string` به `System.Guid` (۲۰۲۶-۰۸-۱۸) — **۱۷۷ پراپرتی ID/FK** در **۶۴** از ۶۵ Entity (۱۱۵ `Guid` + ۶۲ `Guid?`). دامنهٔ تبدیل مکانیکی و قابل‌ممیزی تعیین شد: دقیقاً همان ستون‌هایی که در Fluent Mapping با `HasMaxLength(36) + IsFixedLength()` یعنی Oracle `CHAR(36)` نگاشت شده بودند. `TB_YEAR` هیچ ستون `CHAR(36)` ندارد و دست‌نخورده ماند.
  - **پیش‌شرط: verify روی دادهٔ زنده** (توسط `database-reverse-engineer`، کاملاً Read-Only، بدون DDL/DML): **۱۲۴٬۰۹۴** مقدار غیر-null در همهٔ ۱۷۷ ستون full-scan شد → ۱۰۰٪ منطبق بر `^[0-9a-f]{8}-...` یعنی dashed و **lowercase**؛ صفر مقدار خراب، صفر اختلاف طول/padding. صحت FK هم با join حساس‌به‌حروف تأیید شد (`SYSTEM_TYPE→TB_SYSTYPE` ۵۷/۵۷، `PARENTID→ID` ۱۲۲/۱۲۲).
  - **ستون فیزیکی Oracle تغییر نکرد** — همچنان `CHAR(36)`. تبدیل با `GuidToChar36Converter` (یک `ValueConverter<Guid,string>` مشترک) در `Accounting.Infrastructure/Legacy/` انجام می‌شود و صریحاً روی تک‌تک ۱۷۷ پراپرتی اعمال شده (نه Convention سراسری) تا دامنه‌اش قابل‌ممیزی بماند. `Accounting.Domain` همچنان **صفر** وابستگی خارجی دارد (`Guid` جزو BCL است).
  - ستون‌هایی که پسوند `ID` دارند ولی GUID **نیستند** عمداً `string` ماندند: `ADDUSERID`/`CHANGEUSERID`/`USERID` (طول ۱۰)، `DATE_RSID` (طول ۸)، `CONTROLID` (`NUMBER(1)`)، و کدهای کسب‌وکاری مثل `VAHEDCODE`/`ACCOUNTNUMBER`.
  - نکتهٔ ظریف: `TB_VOUCHERSHEAD.SYSTEM_TYPE` با اینکه پسوند `_ID` ندارد، `CHAR(36)` و FK واقعی به `TB_SYSTYPE` است؛ تبدیل شد (در غیر این صورت مدل EF به‌خاطر عدم تطابق نوع دو سر FK اصلاً build نمی‌شد).
  - build ۰ خطا (۱۶ warning پیش‌موجود NU1903، هیچ warning نوع CS)، **۲۲/۲۲ تست سبز** (۱۲ تست قبلی + ۱۰ تست جدید قرارداد round-trip).
- [ ] اتصال به Oracle (مسیر Legacy) و طراحی مدل نوشتن مبتنی بر `Accounting.Domain.Entity`
- [ ] اولین Command/Query روی Entityهای Legacy
- [ ] فرم صدور سند با تفصیلی داینامیک

## تصمیمات باز (برای فاز بعدی)

### ✅ حل‌شده در ۲۰۲۶-۰۸-۱۷

- **مدل نوشتنِ معتبر** — کاربر گزینهٔ «ج» را انتخاب کرد: Legacy جایگزین کامل. رجوع به «تصمیم معماری دوم» بالا.
- **ابهام `TB_ACCOUNT_LINK_TAFSILI`** — حل شد. `TB_ACCOUNT_LINK_TAFSILGROUP` منبع حقیقت است (FK `FK_TAFSILGOUP_ACCOUNTCODE` به `TB_ACCOUNTCODE` + UNIQUE `UK_ACCOUNTLINKTAFSILGROUP` روی `ACCOUNT_ID, LEVEL_ID, TAFSILGROUP_ID`). `TB_ACCOUNT_LINK_TAFSILI` علی‌رغم نامش به `TB_ACCOUNT` (حساب **بانکی**) وصل است و به کدینگ ربطی ندارد — احتمالاً برای پیش‌فرض تفصیلی در تراکنش‌های بانکی/چک. `TB_ACCOUNT_LINK_LEVEL` فقط سطح را فعال می‌کند (ستون `TAFSILGROUP_ID` ندارد) و به‌تنهایی منبع حقیقت نیست.

### تصمیمات باز

- **🔴 «الزامی بودن تفصیلی» در Legacy اصلاً مدل شده یا نه؟** در `TB_ACCOUNT_LINK_TAFSILGROUP`, `TB_TAFSIL_GROUP`, `TB_LEVEL_TAFSIL`, `TB_ACCOUNT_LINK_LEVEL` هیچ ستون معادل `DetailRequirement` (الزامی/اختیاری) پیدا نشد. برای قطعی‌شدن نیاز است: (۱) جست‌وجوی ستون‌هایی با نام `MUST`/`ISREQUIRED`/`OBLIGATORY` در سایر جدول‌ها، (۲) کوئری روی دادهٔ واقعی برای فهمیدن اینکه آیا این قاعده در Application/UI سیستم قدیمی enforce می‌شده. **حدس زده نشد.**
- **🟡 نقش واقعی `TB_ACCOUNT_LINK_LEVEL`** — مشخص نیست فعال است یا artifact قدیمی. نیاز به کوئری روی داده (تعداد ردیف‌های `ISDELETED=0` و هم‌پوشانی با `TB_ACCOUNT_LINK_TAFSILGROUP`).
- **🟡 یکپارچگی ارجاعی تفصیلی در سند** — `TB_VOUCHERDETAIL_LINK_TAFSILI` فقط **یک** FK دارد (به `TB_VOUCHERSDETAIL`)؛ `TAFSILI_ID` و `LEVEL_ID` هیچ FK ای ندارند. یعنی در سطح دیتابیس هیچ تضمینی نیست که تفصیلی ثبت‌شده روی ردیف سند اصلاً وجود داشته باشد یا مجاز باشد. اگر این تضمین لازم است باید صریحاً ساخته شود.
- **🟡 تضمین تراز (Debit == Credit)** — با تصمیم دوم کاربر از سطح کد حذف شد. اگر لازم شد باید در لایهٔ Application یا به‌صورت DB constraint بازسازی شود.
- **~~نگاشت `VoucherLine.DetailPolicySnapshot`~~** — منتفی شد؛ `VoucherLine` منسوخ است و مسیر نوشتن دیگر از آن عبور نمی‌کند.
- **`AccountNature` فعلاً فقط برچسب گزارشی است** و در اعتبارسنجی دخالت ندارد؛ اگر قرار است مانده بر اساس ماهیت محاسبه شود، جای آن سمت Read (View/MV) است.
- **~~حذف فیزیکی کد منسوخ~~** — ✅ انجام شد (۲۰۲۶-۰۸-۱۷، به درخواست صریح کاربر پس از init شدن git). ۲۲ فایل حذف شد؛ در commit `9f760ad` قابل بازیابی است.
- **🔴 DEFAULTهای سمت Oracle که با `Guid` سازگار نیستند** (کشف‌شده ۲۰۲۶-۰۸-۱۸ حین تبدیل string→Guid). ۹ ستون DEFAULT دارند که مقدار تولیدی‌شان GUID معتبر نیست و خواندنش با converter فعلی `FormatException` می‌دهد:
  - `TB_ACCOUNTCODE.PARENTID` با `DEFAULT '0'` — `'0'` یک GUID نیست. احتمالاً در سیستم قدیمی sentinel «بدون والد» برای گره ریشه بوده؛ ولی در دادهٔ فعلی هیچ ردیفی این مقدار را ندارد (ریشه‌ها `NULL` دارند).
  - ۸ جدول با `ID DEFAULT sys_guid()`: `TB_CITY`, `TB_PROVINCE`, `TB_RABET`, `TB_RABET_CLOSING`, `TB_VAHED_INFO`, `TB_VAHED_TYPE`, `TB_WHITEANDBLACKLIST`, `TB_WHITELIST`. تابع `sys_guid()` مقدار `RAW(16)` برمی‌گرداند که داخل `CHAR(36)` به‌صورت **۳۲ کاراکتر بدون dash و UPPERCASE** (فرمت `"N"`) نوشته می‌شود و `Guid.ParseExact(s,"D")` آن را رد می‌کند.
  - **وضعیت فعلی بی‌خطر است** چون اپلیکیشن قدیمی همیشه `ID` را صریح می‌داده و در دادهٔ زنده هیچ نمونه‌ای یافت نشد. خطر برای **کد جدید** ماست: هر `INSERT` که این ستون‌ها را خالی بگذارد، ردیفی می‌سازد که خواندن بعدی‌اش crash می‌کند.
  - **تصمیم آگاهانه:** خواندن سخت‌گیرانه (`ParseExact`) نگه داشته شد تا خطا **بلند** شود؛ جایگزین یعنی `Guid.Parse` سهل‌گیر، مقدار `"N"` را می‌پذیرد و در نوشتن بعدی بی‌صدا به فرمت dashed بازنویسی می‌کند — یعنی تغییر خاموش دادهٔ Legacy، که برای سیستم حسابداری بدتر از crash است.
  - **قبل از اولین Command/Query که این ۹ ستون را لمس کند باید یکی انتخاب شود:** (الف) همیشه ID را در کد Application تولید کنیم و هرگز به DEFAULT تکیه نکنیم، (ب) DEFAULTها با DDL اصلاح شوند، (ج) مسیر خواندن defensive شود.
- **🟡 پوشش تست مدل نوشتن جدید صفر است** — با حذف ۲۱ تست مدل Rich، تنها ۱۲ تست باقی‌مانده مربوط به `AccountCode` و `Money` است. **هیچ تستی روی مدل نوشتن مبتنی بر `Accounting.Domain.Entity` وجود ندارد**، چون هنوز چنین مدلی ساخته نشده. با ساخت اولین Command/Query باید `qa-tester` پوشش را بسازد.

## قوانین کاری تیم

1. هرگز منطق اعتبارسنجی تفصیلی الزامی را فقط در UI ننویس — باید در Domain/Application هم باشد.
2. سمت Read (گزارش‌ها) باید از View/Materialized View مجزا بخواند، نه مستقیماً از مدل نوشتن (اصل CQRS).
3. schema دیتابیس، مدل دامنه، و Contract بک‌اند/فرانت باید همیشه هماهنگ باشند — هماهنگی بین ایجنت‌ها وظیفهٔ `team-lead` است.
4. در پایان هر جلسهٔ کاری: به‌روزرسانی این فایل (بخش «وضعیت فعلی»)، افزودن یک خط به `docs/progress-log.md`، به‌روزرسانی `ROADMAP.md` و GitHub Project board (https://github.com/users/mehdijanfeshar/projects/2)، و `git commit`.
5. **داشبورد همیشه باید آینهٔ وضعیت واقعی پروژه باشد** — به درخواست صریح کاربر (۲۰۲۶-۰۸-۱۷)، هر تغییری در وضعیت پروژه (فاز تمام/شروع شد، تصمیم معماری، ریسک جدید) باید هم‌زمان در `ROADMAP.md` و در Status فیلد GitHub Project منعکس شود؛ صرفاً به‌روز نگه‌داشتن `CLAUDE.md` کافی نیست.

## نحوهٔ ادامهٔ کار در روزهای بعد

1. `git pull` برای گرفتن آخرین تغییرات.
2. Claude Code را در ریشهٔ پروژه اجرا کن — این فایل به‌طور خودکار خوانده می‌شود.
3. به Claude بگو مثلاً: «طبق وضعیت فعلی در CLAUDE.md ادامه بده» یا مستقیماً یک وظیفهٔ جدید بده؛ `team-lead` بر اساس این فایل کار را ادامه می‌دهد.
