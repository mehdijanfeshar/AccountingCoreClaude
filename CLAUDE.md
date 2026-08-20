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
- [x] اتصال به Oracle (مسیر Legacy) و طراحی مدل نوشتن مبتنی بر `Accounting.Domain.Entity` — `LegacyDbContext` با `UseOracle(...)` در DI ثبت شد؛ connection string فقط از `IConfiguration`/User Secrets خوانده می‌شود و در `appsettings.json` تنها یک placeholder خالی هست. ⚠️ اتصال واقعی به دیتابیس هنوز **اجرا/تست نشده** (هیچ تست integration روی Oracle واقعی زده نشد — عمداً، برای جلوگیری از side effect روی دیتابیس Legacy).
- [x] اولین Command روی Entityهای Legacy (الگوی پایه) — `CreateAccountCodeCommand` و `CreateVoucherHeadCommand` طبق ترتیب درخواستی صاحب پروژه (UnitOfWork → Interface → Repository → Command Service). **فقط Add؛ هیچ Query/Update/Delete و هیچ Controller ساخته نشد.** جزئیات در «فاز ۵» پایین‌تر.
- [x] Query side (خواندن) روی `Accounting.Domain.Entity` — چهار Query با paging: `GetAccountCodes`/`GetAccountCodeById`/`GetVoucherHeads`/`GetVoucherHeadById` با `PagedResult<T>` و Read Repositoryهای مجزا.
- [x] Controller/Endpoint برای هر ۶ سرویس (۲ Command + ۴ Query) — `AccountCodesController` و `VoucherHeadsController` + `GlobalExceptionHandler` مرکزی. جزئیات در «فاز ۶» پایین‌تر.
- [x] **فاز ۷ — Authentication/Authorization + رفع جعل‌پذیری `ADDUSERID`** (۲۰۲۶-۰۸-۱۹) — هر دو ریسک 🔴 CRITICAL که `security-reviewer` در Gate فاز ۶ کشف کرده بود حل شدند. اتصال inbound به IDP واقعی سازمان (`Tamin.Framework.Common.Security`) + `FallbackPolicy` + `ICurrentUser`. جزئیات در «فاز ۷» پایین‌تر. **۱۷۰/۱۷۰ تست سبز.**
- [x] **فاز ۸ — تکمیل CRUD: Update + Delete (Soft Delete)** (۲۰۲۶-۰۸-۱۹) — ۴ Command جدید و ۴ Endpoint جدید، همگی `POST` با فعل صریح در route (`PUT`/`DELETE` به‌درخواست صریح صاحب پروژه ممنوع شدند — رجوع به «فاز ۸» پایین‌تر). جزئیات در «فاز ۸» پایین‌تر. **۲۵۹/۲۵۹ تست سبز** (پس از یک پاس `/code-review` که ۲ باگ واقعی و ۱ شکاف اعتبارسنجی پیدا و رفع کرد).
- [x] **فاز ۹ — cascade کامل سه‌سطحی حذف نرم سند** (۲۰۲۶-۰۸-۲۰) — ریسک 🔴 «نبود cascade» از فاز ۸ **کاملاً** بسته شد: `TB_VOUCHERSHEAD` → `TB_VOUCHERSDETAIL` → `TB_VOUCHERDETAIL_LINK_TAFSILI`. جزئیات در «فاز ۹» پایین‌تر. **۲۷۲/۲۷۲ تست سبز.**
- [ ] فرم صدور سند با تفصیلی داینامیک

### فاز ۹ — cascade کامل سه‌سطحی حذف نرم سند (۲۰۲۶-۰۸-۲۰، برنچ `changejWT`، commit نشده)

به درخواست صریح صاحب پروژه، در دو مرحله: اول «وقتی سند از هدر حذف بشه، دیتیل‌هاش هم باید حذف بشن»، سپس افزودن سطح سوم (`TB_VOUCHERDETAIL_LINK_TAFSILI`).

**زنجیرهٔ کامل cascade — هر سه سطح در یک تراکنش/یک `SaveChangesAsync`:**
`TB_VOUCHERSHEAD` → `TB_VOUCHERSDETAIL` → `TB_VOUCHERDETAIL_LINK_TAFSILI`

**تصمیم ۱ — محل متد: داخل همان `IVoucherHeadRepository`** (نه repository/interface مستقل برای هیچ‌کدام از دو جدول فرزند).
متد: `Task<int> SoftDeleteDetailTreeAsync(Guid headId, string? changeUserId, DateTime updatedDate, CancellationToken)` — که مجموع ردیف‌های حذف‌شدهٔ هر دو سطح را برمی‌گرداند.
دلیل: اجتناب از premature abstraction — هیچ Command/Query مستقلی برای `TB_VOUCHERSDETAIL` یا `TB_VOUCHERDETAIL_LINK_TAFSILI` وجود ندارد و هر دو فقط به‌عنوان بخشی از aggregate سند قابل دسترسی‌اند؛ همان قضاوتی که قبلاً برای `ITokenManager` هم اعمال شد. سطح سوم عمداً **داخل همان متد** ادغام شد (نه متد دوم) چون IDهای ردیف‌ها از قبل در حافظه هستند (بدون کوئری اضافه) و این کار ساختاراً غیرممکن می‌کند که کسی سطح ۲ را cascade کند ولی سطح ۳ را فراموش کند. نام متد از `SoftDeleteDetailLinesAsync` به `SoftDeleteDetailTreeAsync` تغییر کرد تا دامنهٔ واقعی‌اش را بیان کند. اگر روزی مسیر نوشتن مستقلی ساخته شد، باید بازبینی و استخراج شود (در XML doc ثبت شد).

**تصمیم ۳ — دامنهٔ سطح سوم عمداً وسیع‌تر از «ردیف‌هایی که همین الان حذف شدند» است.**
لینک‌های تفصیلی بر اساس **همهٔ** ردیف‌های سند (صرف‌نظر از `ISDELETED` آن‌ها) فیلتر می‌شوند، نه فقط ردیف‌هایی که در همین فراخوان حذف شدند. دلیل: اگر ردیفی قبلاً به‌تنهایی soft-delete شده بود ولی لینک‌های تفصیلی‌اش فعال مانده بودند، محدودکردن دامنه به ردیف‌های تازه‌حذف‌شده باعث می‌شد آن لینک‌های فعالِ یتیم زیر یک سند حذف‌شده باقی بمانند. این تضمین می‌کند «پس از حذف سند، هیچ چیزی زیر آن فعال نمی‌ماند». برای همین هم فقط **یک** کوئری برای ردیف‌ها زده می‌شود (بدون فیلتر `ISDELETED` در SQL) و تفکیک «نیازمند حذف» از «مجموعهٔ کامل ID» در حافظه انجام می‌شود.

**تصمیم ۲ — load+mutate، نه `ExecuteUpdateAsync`.**
با اینکه EF Core 10 از `ExecuteUpdateAsync` پشتیبانی می‌کند، عمداً استفاده **نشد**: آن متد بلافاصله و خارج از change tracker روی دیتابیس اجرا می‌شود و invariant پروژه («Repository فقط stage می‌کند؛ Handler تنها مالک مرز تراکنش است و یک‌بار `SaveChangesAsync` صدا می‌زند») را می‌شکند. بدون یک `BeginTransactionAsync` صریح، ممکن بود update ردیف‌ها commit شود ولی update سرسند بعداً شکست بخورد — یعنی **cascade نیمه‌کاره**، که برای دادهٔ حسابداری غیرقابل‌قبول است. مقیاس هم کراندار است (ده‌ها ردیف در هر سند) پس load+mutate ارزان است.

**نکتهٔ ظریف NULL — و اینکه چرا در سطح سوم فرق می‌کند:** در `TB_VOUCHERSDETAIL` فیلتر عمداً `d.ISDELETED == null || d.ISDELETED == false` نوشته شد، نه `d.ISDELETED != true`. چون `ISDELETED` آنجا `bool?` است، فرم دوم در منطق سه‌مقداری SQL ردیف‌های `NULL` را حذف می‌کرد — در حالی که Handler سرسند از قبل `null` را «حذف‌نشده» تلقی می‌کند. این با تست واقعی روی SQLite تأیید شد (ترجمهٔ SQL: `"ISDELETED" IS NULL OR NOT ("ISDELETED")`).
اما در `TB_VOUCHERDETAIL_LINK_TAFSILI` ستون `ISDELETED` از نوع `bool` **غیر-nullable** است (تفاوت واقعی schema بین این جدول و دو جدول بالادست)، پس آنجا فیلتر سادهٔ `l.ISDELETED == false` کافی و درست است و شاخهٔ `== null` لازم ندارد.

**Idempotency حفظ شد:** گارد موجود `if (entity.ISDELETED == true) return;` بالای فراخوان cascade ماند، پس سندِ از‌قبل‌حذف‌شده نه سرسند و نه هیچ ردیفی را لمس می‌کند و اصلاً به `SaveChangesAsync` نمی‌رسد. سرسند و همهٔ ردیف‌ها با **یک مقدار زمانی مشترک** (`var now = DateTime.UtcNow` یک‌بار محاسبه) و همان `ICurrentUser.UserId` مهر می‌خورند.

**تست (۱۳ تست جدید، مجموع ۲۷۲/۲۷۲ سبز):**
- ۲ تست Handler در `Accounting.Application.Tests` (mock): فراخوانی cascade با همان user/timestamp سرسند، و تضمین ترتیب cascade **قبل از** `SaveChangesAsync`.
- ۱۱ تست **واقعی repository** در `Accounting.Infrastructure.Tests` روی **SQLite in-memory** (نه InMemory provider، چون آن ترجمهٔ SQL را اثبات نمی‌کند). سطح ۲: N ردیف، صفر ردیف، ردیف‌های از‌قبل‌حذف‌شده با audit قبلی byte-identical، ردیف‌های `ISDELETED = NULL`، ایزولاسیون بین اسناد. سطح ۳: cascade کامل سه‌سطحی، ایزولاسیون لینک‌های سند دیگر، لینک از‌قبل‌حذف‌شده که دوباره لمس نمی‌شود، سند با ردیف ولی بدون لینک، صحت مقدار بازگشتی، و **تست invariant «لینک یتیم»** (ردیفِ از‌قبل‌حذف‌شده با لینک‌های فعال → لینک‌ها باز هم حذف می‌شوند، ولی audit خود ردیف دست‌نخورده می‌ماند).
- همهٔ تست‌ها فاز assert را با یک `LegacyDbContext` تازه انجام می‌دهند، پس **persistence واقعی** اثبات می‌شود نه صرفاً وضعیت change tracker.
- ⚠️ محدودیت: `EnsureCreated()` روی کل `LegacyDbContext` در SQLite شکست می‌خورد (`SQLite does not support sequences` — به‌خاطر `HasSequence("VOUCHERHEAD_SEQ")`)، پس fixture فقط همان یک جدول را با `CREATE TABLE` دستی می‌سازد. این تست‌ها رفتار Oracle-specific را اثبات **نمی‌کنند**.
- پکیج جدید فقط در پروژهٔ تست: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0 (۲ warning جدید NU1903 از وابستگی گذرای `SQLitePCLRaw`، فقط test-only و نه در API منتشرشده → مجموع warning از ۱۶ به ۱۸).

**خارج از دامنه (دست‌نخورده):** `UpdateVoucherHeadCommand` (به ردیف‌ها دست نمی‌زند) و همهٔ مسیرهای `*AccountCode*` — ریسک 🔴 «حذف گرهٔ کدینگ بدون بررسی وابستگی» همچنان کاملاً باز است.

### فاز ۸ — تکمیل CRUD: Update + Delete (۲۰۲۶-۰۸-۱۹، برنچ `changejWT`، commit نشده)

پیش از این فاز فقط Create (فاز ۵) و Read (فاز ۶) وجود داشت. حالا هر دو Entity چرخهٔ کامل CRUD دارند.

**Endpointهای جدید (همگی زیر `SetFallbackPolicy(RequireAuthenticatedUser)`):**

| Verb | Route | موفق | خطاها |
|---|---|---|---|
| `POST` | `/api/account-codes/{id:guid}/update` | 200 + `{id}` | 400, 401, 404, 409, 500 |
| `POST` | `/api/account-codes/{id:guid}/delete` | 200 + `{id}` | 400, 401, 404, 500 |
| `POST` | `/api/voucher-heads/{id:guid}/update` | 200 + `{id}` | 400, 401, 404, 409, 500 |
| `POST` | `/api/voucher-heads/{id:guid}/delete` | 200 + `{id}` | 400, 401, 404, 500 |

> ⚠️ **چرا اینجا REST استاندارد رعایت نشده — این یک محدودیت درخواستی صاحب پروژه است، نه انتخاب معماری داخلی.**
> این ۴ Endpoint ابتدا به‌صورت `PUT /{id}` و `DELETE /{id}` ساخته شدند (و سبز بودند)، ولی **صاحب پروژه صریحاً اعلام کرد که متدهای `PUT` و `DELETE` در این محیط قابل استفاده نیستند و فقط `POST` و `GET` مجازند** (دلیل فنی‌اش را نگفتند؛ احتمالاً محدودیت زیرساخت شبکه/سرور سازمان). پس همگی به `POST` با **فعل صریح در route** تبدیل شدند.
> - **چرا `/{id}/update` و نه `POST /api/account-codes/{id}` خالی:** چون `POST /api/account-codes` (ساخت) از قبل وجود دارد و `POST /api/account-codes/{id}` در کنارش «ساخت زیرمنبع تحت این id» خوانده می‌شود. حالا که از semantics استاندارد خارج شده‌ایم، صراحت بهتر از ابهام است و مسیر برای Endpointهای بعدی (مثل `/{id}/lines`) باز می‌ماند.
> - **چرا 200 با بدنه و نه 204:** (الف) همان محدودیت زیرساختی که `PUT`/`DELETE` را ممنوع کرده نشان می‌دهد لایه‌های میانی شبکه در این محیط رفتار غیرمعمول دارند و پاسخ ۲۰۰ با بدنهٔ کوچک کمتر از ۲۰۴ خالی مستعد ابهام است؛ (ب) با `POST` ساخت که از قبل بدنه برمی‌گرداند هم‌خانواده می‌شود، پس هر سه عملیات نوشتنِ هر Controller یک شکل پاسخ دارند؛ (ج) `id` برگشتی تأیید می‌کند دقیقاً کدام رکورد تحت تأثیر قرار گرفته.
> - **قفل شده با تست:** `HttpVerbConventionTests` با reflection روی کل assembly تأیید می‌کند **هیچ اکشنی در هیچ Controllerی** `[HttpPut]` یا `[HttpDelete]` ندارد — پس بازگشت تصادفی این قاعده تست را قرمز می‌کند.
> - **`GET` (لیست و by-id) و `POST` ساخت دست‌نخورده ماندند** — محدودیت فقط روی مسیر نوشتن/حذف بود.
> - این تغییر **فقط لایهٔ Api را لمس کرد**؛ Command/Handler/Validator/Repository دست‌نخورده ماندند (Controller خودش `id` را از route دارد و بدنهٔ پاسخ را می‌سازد).

**تصمیم PUT به‌جای PATCH (تصمیم صریح، نه پیش‌فرض):** تقریباً همهٔ ستون‌های Legacy nullable اند (`bool?`, `Guid?`, `string?`)، بنابراین در PATCH جزئی هیچ راهی نیست که «فیلد ارسال‌نشده» از «فیلد صریحاً `null`» تفکیک شود مگر با wrapper نوع `Optional<T>` روی تک‌تک فیلدها — که هم قانون «Command فقط primitive دارد» را می‌شکند و هم ماشین‌آلات نامتناسبی اضافه می‌کند. PUT ضمناً با شکل `CreateXCommand` موجود قرینه است. **پیامد پذیرفته‌شده:** فراخوان باید همیشه بدنهٔ کامل بفرستد؛ فیلد جاافتاده = `null` شدن آن ستون.

**تصمیم‌های تثبیت‌شدهٔ دیگر:**
- **`Id` از route می‌آید، نه از بدنه.** برای PUT یک record جدا در لایهٔ Api تعریف شد (`UpdateAccountCodeRequest`/`UpdateVoucherHeadRequest`) که **اصلاً پراپرتی `Id` ندارد**؛ Controller خودش Command را با id مسیر می‌سازد. یعنی کل کلاسِ باگِ «تناقض id مسیر با id بدنه» **ساختاراً** ناممکن است. (این ضمناً اولین اجرای عملی پیشنهاد باز `api-contract` مبنی بر جداکردن Request از Command است — ولی فقط برای PUT؛ POST همچنان بدنه‌اش خود Command است.)
- **Audit فقط سمت سرور** — `CHANGEUSERID = _currentUser.UserId` و `UPDATEDDATE = DateTime.UtcNow`. دقیقاً همان الگوی فاز ۷؛ هیچ پراپرتی userId در هیچ‌کدام از ۴ Command وجود ندارد، پس بازگشت آسیب‌پذیری جعل Audit **خطای کامپایل** می‌دهد.
- **ستون‌های تغییرناپذیر در Update:** `ID`، `ADDUSERID`، `CREATEDDATE`، `ISDELETED`، و برای VoucherHead ستون‌های `GLOBALNUMBER` (که `ValueGeneratedOnAdd` است) و `ATTACHFILE`. در Command وجود ندارند و Handler لمسشان نمی‌کند (با تست صریح قفل شد).
- **`ISDELETED` عمداً در Update نیست** — اگر بود، Update به در پشتیِ delete/undelete تبدیل می‌شد.
- **404 مرکزی** — `NotFoundException` جدید در `Accounting.Application/Common/Exceptions/` → `GlobalExceptionHandler` → **404** `ProblemDetails` با پیام عمومی. الگو دقیقاً از `DuplicateKeyException` → 409 گرفته شد. پیام خود exception (شامل نام منبع و id) عمداً **به بدنهٔ پاسخ درز نمی‌کند** (با تست اثبات شد).
- **Repository:** یک متد `GetForUpdateAsync(Guid, CancellationToken)` به هر دو write repository اضافه شد که **عمداً tracked است** (بدون `AsNoTracking()`، برخلاف read repository) تا Handler بتواند در جا mutate کند. Repository همچنان **هرگز** `SaveChangesAsync` صدا نمی‌زند؛ مرز تراکنش در Handler ماند. `IUnitOfWork` **هیچ تغییری نکرد** — همان‌طور که در فاز ۵ طراحی شده بود.

**Soft Delete (نه حذف فیزیکی):** Command حذف فقط `ISDELETED = true` + ستون‌های audit را می‌نویسد. هیچ `Remove`/`ExecuteDelete` در کد نیست — و چون interfaceهای write repository اصلاً متد حذف **ندارند**، حذف فیزیکی از این مسیر ساختاراً ناممکن است (این را `qa-tester` با تست reflection روی interface قفل کرد). دلیل: هر دو جدول ستون `ISDELETED` دارند که Query side از قبل با `Where(x => x.ISDELETED != true)` استفاده می‌کند؛ حذف فیزیکی هم یکپارچگی ارجاعی Legacy را می‌شکند و هم برخلاف رفتار سیستم قدیمی است.

**رفتار مرزی (تصمیم‌گرفته‌شده و تست‌شده):**
- Update روی رکورد ناموجود **یا** رکورد `ISDELETED == true` → 404 (رکورد soft-deleted «منطقاً غایب» تلقی می‌شود، هم‌راستا با فیلتر Query side).
- Delete روی رکورد ناموجود → 404.
- Delete روی رکوردی که از قبل حذف شده → **200 + `{id}` بدون هیچ نوشتنی** (idempotent طبق تعریف HTTP؛ status code خودِ ۲۰۰ است، نه ۲۰۴ — طبق تصمیم «۲۰۰ با بدنه» بالا)؛ `SaveChangesAsync` اصلاً صدا زده نمی‌شود و `CHANGEUSERID`/`UPDATEDDATE` قبلی دست‌نخورده می‌ماند.
- `ISDELETED` از نوع `bool?` است: هم `false` و هم **`null`** یعنی «حذف‌نشده» — دقیقاً مثل فیلتر read side. رکورد با `ISDELETED == null` واقعاً soft-delete می‌شود و idempotent تلقی نمی‌شود (ظریف‌ترین حالت مرزی این فاز؛ با تست صریح قفل شد).
- خطای UNIQUE در PUT از همان مسیر موجود عبور می‌کند (`UnitOfWork` → `DuplicateKeyException` → 409)؛ هیچ pre-check دستی اضافه نشد، که برای شرایط رقابتی (race) رفتار درستی است.

**یافتهٔ `api-contract`:** **401 در هیچ‌کدام از Endpointها اعلام نشده بود** — نه در ۴ تای جدید، نه در ۶ تای قبلی — با اینکه همگی زیر FallbackPolicy اند و 401 کاملاً قابل‌تولید است. برای هر ۱۰ Endpoint به‌صورت یکدست اضافه شد. **403 عمداً اعلام نشد** چون هیچ authorization مبتنی بر نقش هنوز وجود ندارد و اعلامش گمانه‌زنی می‌بود.

**تست:** `qa-tester` **۸۰ تست جدید** نوشت (۶۹ Application + ۱۱ Api)، سپس مهاجرت verb (بالا) ۵ تست دیگر اضافه کرد (`HttpVerbConventionTests`) → مجموع ۲۵۵/۲۵۵.

**پاس `/code-review` نهایی (۲۰۲۶-۰۸-۲۰، قبل از commit، به درخواست صریح کاربر):** ۲ باگ واقعی + ۱ شکاف اعتبارسنجی پیدا شد و همان‌جا رفع شد:
- **باگ در تست محافظ verb:** `HttpVerbConventionTests.AllControllerActions()` به‌جای reflect کردن روی assembly واقعی `Accounting.Api`، روی `typeof(ControllerBase).Assembly` (یعنی خودِ فریم‌ورک ASP.NET Core) reflect می‌کرد — یعنی این تست **هرگز** Controllerهای این پروژه را بررسی نمی‌کرد و اگر کسی بعداً `[HttpPut]`/`[HttpDelete]` اضافه می‌کرد، بازهم سبز می‌ماند. اصلاح شد به `typeof(AccountCodesController).Assembly`.
- **تناقض مستندسازی:** این فایل (خط مربوط به رفتار مرزی Delete) هنوز می‌گفت idempotent-delete «۲۰۴» برمی‌گرداند، درحالی‌که تصمیم نهایی (بالا) و کد واقعی «۲۰۰ + بدنه» است. اصلاح شد.
- **شکاف اعتبارسنجی خودارجاعی:** نه `UpdateAccountCodeCommandValidator` و نه `UpdateVoucherHeadCommandValidator` بررسی نمی‌کردند که `ParentId`/`ParentHeadId` برابر `Id` خودِ رکورد باشد. چون Update برخلاف Create می‌تواند `Id` را از قبل بداند (از route می‌آید)، این مسیر یک گرهٔ خودارجاعی (حلقهٔ بی‌نهایت در سلسله‌مراتب) می‌ساخت که Create اصلاً نمی‌توانست تولید کند. یک قانون `Must(x => x.ParentId != x.Id)` به هر دو Validator اضافه شد + ۴ تست جدید (۲ به‌ازای هر Entity).
- **یافتهٔ چهارم (IDOR روی نوشتن/حذف) دوباره تأیید شد ولی عمداً حل نشد** — طبق تصمیم صریح کاربر، مثل بقیهٔ ریسک‌های امنیتی این فاز، فقط مستند می‌ماند (رجوع به «تصمیمات باز» پایین‌تر).

مجموع نهایی بعد از این سه اصلاح: **۲۵۹/۲۵۹ سبز** (۲۲ Domain + ۱۵۷ Application + ۶۰ Api + ۲۰ Infrastructure)، **صفر رگرسیون**. build ۰ خطا / ۱۶ warning پیش‌موجود NU1903 (هیچ CS). همگی Unit/Mock — **هیچ اتصالی به Oracle زنده**.

### فاز ۷ — Authentication/Authorization + رفع جعل‌پذیری Audit (۲۰۲۶-۰۸-۱۹، برنچ `changejWT`، commit `86e6526`، push شده)

هر دو ریسک 🔴 CRITICAL فاز ۶ حل شدند. طراحی توسط `security-reviewer`، پیاده‌سازی توسط `backend-dotnet`، تأیید توسط `qa-tester`.

#### ۱. جعل‌پذیری `ADDUSERID` — حل شد ✅

- پارامتر `AddUserId` **کاملاً حذف شد** از `CreateAccountCodeCommand` و `CreateVoucherHeadCommand` (و از Validatorهایشان). یعنی دیگر فقط «بی‌اعتماد» نیست — اصلاً بخشی از قرارداد ورودی نیست.
- هر دو Handler حالا `ADDUSERID = _currentUser.UserId` می‌نویسند؛ دقیقاً مثل `CREATEDDATE`/`ISDELETED` که از قبل درست سمت سرور ست می‌شدند.
- **این تنها تضمین ✅ باقی‌مانده در جدول Accounting Safety Gate (یعنی Audit Trail) بود که عملاً باطل شده بود؛ حالا واقعاً برقرار است.**
- ⚠️ **Breaking change در قرارداد API:** فیلد `addUserId` از بدنهٔ هر دو POST حذف شد. چون هنوز هیچ مصرف‌کنندهٔ خارجی وجود ندارد، پذیرفته شد.
- سمت **خواندن** (`AccountCodeDto`/`VoucherHeadDto`) عمداً `AddUserId` را همچنان برمی‌گرداند — آن جهت خواندن است و خارج از دامنهٔ این تغییر.

#### ۲. `ICurrentUser`

- قرارداد در `Accounting.Application/Common/Interfaces/ICurrentUser.cs` (طبق الگوی موجود `IUnitOfWork` — نه در Domain، تا `Accounting.Domain` صفر وابستگی بماند). پیاده‌سازی `HttpContextCurrentUser` در `Accounting.Api/Security/` بر پایهٔ `IHttpContextAccessor` و Claimها.
- اعضا: `IsAuthenticated`, `UserId`, `VahedCode`, `IsInRole(role)`.
- **`UserId` عمداً پرتاب می‌کند (throw) و هرگز truncate نمی‌کند** اگر کاربر احراز نشده باشد، Claim `NameIdentifier` نباشد، یا مقدار بیش از ۱۰ کاراکتر باشد (عرض ستون Legacy `ADDUSERID`). فلسفه: نوشتن یک هویت بریده‌شده در ستون Audit یک سیستم حسابداری، بدتر از خطای بلند است. (همان منطق تصمیم `ParseExact` در `GuidToChar36Converter`.)
- `VahedCode` صرفاً **در دسترس** قرار گرفت ولی **به هیچ فیلتر Query وصل نشد** — عمداً؛ رجوع به تصمیم باز پایین.

#### ۳. اسکیم احراز هویت **ورودی (inbound)**: IDP واقعی سازمان ✅

> **تاریخچه:** ابتدا (۲۰۲۶-۰۸-۱۹، صبح) یک JWT محلی‌امضا به‌عنوان راه‌حل موقت پیاده شد (گزینهٔ B از `security-reviewer`). سپس **در همان روز** صاحب پروژه پکیج رسمی سازمان را در اختیار گذاشت و مسیر محلی **کاملاً حذف و جایگزین شد**. متن زیر وضعیت نهایی است.

- پکیج `Tamin.Framework.Common.Security` **1.0.9** از feed داخلی سازمان (`https://nexus.tamin.ir/repository/nuget-v2-group/`، تعریف‌شده در `backend/NuGet.Config`).
- فراخوانی: `builder.Services.AddTaminJWTToken(validAudience, environment)` در `Program.cs`.
  - `validAudience` از کلید config `Tamin:Idp:Audience` خوانده می‌شود و در نبودش به مقدار پیش‌فرض برمی‌گردد. ⚠️ **مقدار فعلی به‌صراحت درخواست صاحب پروژه با پروژهٔ `Financial_Account` مشترک است** تا وقتی audience اختصاصی این سرویس در IDP سازمان ثبت شود.
  - `environment` مشروط است: `IsProduction() ? Environments.Production : Environments.Test` (این enum فقط همین دو مقدار را دارد).
- **رفتار واقعی پکیج که با reflection و اجرای واقعی تأیید شد (نه حدس):**
  - اسکیم استاندارد `Bearer` را با `JwtBearerHandler` ثبت می‌کند و `DefaultScheme` را هم ست می‌کند → `SetFallbackPolicy(RequireAuthenticatedUser)` واقعاً scheme ای برای authenticate/challenge دارد. **نباید `AddAuthentication(...)` دوم اضافه شود** چون با اسکیم پکیج رقابت می‌کند.
  - `ValidIssuer = http://idm.tamin.ir`، `IssuerSigningKey` یک **`JsonWebKey` ثابتِ جاسازی‌شده** است — **هیچ Authority/discovery/JWKS fetch ای در startup یا per-request انجام نمی‌شود**.
  - `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime`/`ValidateIssuerSigningKey` همگی `true`.
  - `NameClaimType = .../identity/claims/name` و `RoleClaimType = .../identity/claims/role`.
- **حذف‌شده (فیزیکی، طبق قانون پروژه — نه `[Obsolete]`):** `DevAuthController` + تست‌هایش، `JwtOptions`، `DevAuthOptions`، و بخش‌های `Jwt`/`DevAuth` از `appsettings.json`. دیگر هیچ توکن محلی‌امضایی در پروژه صادر نمی‌شود.
- **`RolesAllowedAttribute` و `ClaimRequirementFilter`** هم در همین پکیج هستند و برای فاز بعدی (authorization سطح نقش/رکورد — تصمیم باز IDOR) در دسترس‌اند.

- **یافتهٔ تعیین‌کننده:** در **هیچ‌کدام از ۶۵ جدول Legacy هیچ ستون password/hash/salt/token/credential وجود ندارد** → **schema Legacy اصلاً قادر به احراز هویت کسی نیست.** پس تکیه بر Legacy برای authentication ممکن نبود و تکیه بر IDP بیرونی سازمان اجتناب‌ناپذیر بود.

#### ۴. Authorization و خطاها

- `AddAuthorizationBuilder().SetFallbackPolicy(RequireAuthenticatedUser)` — عمداً به‌جای `[Authorize]` روی تک‌تک Controllerها، تا Controller بعدی که کسی یادش برود، دوباره دیتابیس را باز نکند.
- **`app.UseAuthentication()` اصلاً در pipeline وجود نداشت** (خلأیی که `security-reviewer` کشف کرد) — اضافه شد، **قبل از** `app.UseAuthorization()`. بدون آن `HttpContext.User` هرگز پر نمی‌شد و policy بی‌صدا بی‌اثر می‌ماند.
- تنها استثنا: `HealthController` با `[AllowAnonymous]` (probeها نمی‌توانند bearer token حمل کنند).
- 401/403 توسط middleware تولید می‌شوند و **هرگز به `GlobalExceptionHandler` نمی‌رسند** (استثنا نیستند)؛ پس صریحاً به شکل `application/problem+json` با `traceId` و پیام عمومی درآمدند. جزئیات خطای اعتبارسنجی توکن فقط در Development برمی‌گردد.
- ⚠️ **این shaping با `Configure<JwtBearerOptions>` و به‌صورت زنجیره‌ای (chain) انجام می‌شود، نه جایگزینی.** دلیل: `AddTaminJWTToken` خودش `OnMessageReceived` و `OnAuthenticationFailed` را wire می‌کند؛ اگر `options.Events = new JwtBearerEvents{...}` می‌نوشتیم، **کل آن‌ها بی‌صدا نابود می‌شدند**. پس delegate اصلی اول `await` می‌شود و فقط اگر `!Response.HasStarted && !context.Handled` بود، ProblemDetails ما نوشته می‌شود. `OnAuthenticationFailed`/`OnTokenValidated`/`OnMessageReceived` کاملاً دست‌نخورده‌اند.

#### ۵. `TokenManager` — مسیر **خروجی (outbound)** به IDP واقعی سازمان

صاحب پروژه الگوی `IDP.Services.TokenManager` را از پروژهٔ اصلی خودش (همان سازمان `tamin.org`) فرستاد و خواست «مثل همین» ساخته شود. پورت شد به `Accounting.Infrastructure/Idp/` با namespace `Accounting.Infrastructure.Idp`.

**نکتهٔ کلیدی معماری که باید بدانید:** این `TokenManager` یک **acquirer از نوع outbound `client_credentials`** است — یعنی این API خودش به‌عنوان *client* از IDP توکن می‌گیرد تا به *سرویس‌های دیگر* سازمان زنگ بزند. **این به‌تنهایی CRITICAL #1 (محافظت از Controllerهای خودمان در برابر فراخوان‌های ورودی) را حل نمی‌کند** — آن مسیر جداگانه‌ای است (بخش ۳ بالا).

- `ITokenManager` عمداً **داخلی به Infrastructure** ماند و به `Accounting.Application/Common/Interfaces/` اضافه **نشد**. دلیل: هیچ use case ای در Application امروز مصرف‌کنندهٔ آن نیست؛ وقتی شد، باید به یک interface هدف‌محور (مثلاً `IPersonDirectoryClient`) وابسته شود نه به این plumbing سطح‌پایین. (اجتناب از premature abstraction.)
- **۶ نقص امنیتی/همزمانی الگوی اصلی حین پورت اصلاح شد** (نه کورکورانه کپی):
  1. `HttpRequestException` بلعیده‌شده + `Console.WriteLine` → `ILogger` ساختاریافته و **انتشار خطا** با `TokenAcquisitionException`. دیگر هرگز توکن کهنه/`null` را بی‌صدا برنمی‌گرداند.
  2. `new HttpClient()` در هر refresh (با وجود تزریق بلااستفادهٔ `IHttpClientFactory`) → استفادهٔ واقعی از factory با named client (رفع socket exhaustion / کهنگی DNS).
  3. `static SemaphoreSlim` روی یک singleton → فیلد نمونه‌ای.
  4. `Dictionary` با نوشتن بدون محافظت از مسیر خواندن → `ConcurrentDictionary` (باگ واقعی خرابی متناوب زیر بار، نه آرایشی).
  5. `DateTime.Now` → `DateTime.UtcNow` در همهٔ محاسبات انقضا (drift منطقه‌زمانی/DST می‌توانست توکن منقضی را معتبر بشمارد).
  6. افشای راز در لاگ → هیچ لاگ/پیام استثنا/`ToString` هرگز `ClientSecret` یا `access_token` خام را شامل نمی‌شود (اعتبارسنجی فقط **نام** پراپرتی‌های ناقص را فهرست می‌کند).
- **نقص ۷ عمداً اصلاح نشد:** `Resources`/`External_Resources` پیکربندی می‌شود ولی در درخواست توکن **ارسال نمی‌شود** — دقیقاً مثل الگوی اصلی. حدس زده نشد؛ سؤالش برای صاحب پروژه باز است (پایین).
- **اعتبارسنجی config تنبل (lazy) است، نه در startup** — برخلاف `Jwt:SigningKey` که fail-fast است. دلیل: هنوز هیچ‌چیز در این کدبیس به سرویس دیگری زنگ نمی‌زند، پس نبودِ `Idp:*` نباید بالاآمدن API را برای کار محلی خراب کند. خطای روشن در اولین `GetAccessTokenAsync` پرتاب می‌شود.
- config با همان الگوی placeholder خالی در `appsettings.json` (`Idp:tamin:*`)؛ مقادیر واقعی فقط در User Secrets.

تست: **۱۷۰/۱۷۰ سبز** (۲۲ Domain + ۸۴ Application + ۴۴ Api + ۲۰ Infrastructure) — `qa-tester` مستقلاً همین عدد را تأیید کرد. build ۰ خطا، بدون هیچ warning نوع CS (۱۶ warning پیش‌موجود NU1903 دست‌نخورده، بدون NU1902 — نسخهٔ transitive `Microsoft.IdentityModel.*` توسط resolver NuGet به ۸.۱۴.۰/۸.۰.۱ ارتقا یافت، فراتر از بازهٔ آسیب‌پذیر). Application از ۸۵ به ۸۴ رسید (حذف تست Validator مربوط به `AddUserId`)، Api از ۲۴ به ۴۴ (تست‌های `HttpContextCurrentUser`، `TaminJwtWiringTests` و مسیر Audit؛ `DevAuthController` و تست‌هایش پس از سوییچ به IDP واقعی فیزیکاً حذف شدند)، و پروژهٔ جدید `Accounting.Infrastructure.Tests` با ۲۰ تست (cache/انقضا/انتشار خطا/عدم افشای راز، همگی با `HttpMessageHandler` mock — **هیچ فراخوان شبکه‌ای واقعی به IDP انجام نشد**). همهٔ تست‌ها همچنان **Unit/Mock**؛ هیچ تست integration روی Oracle یا IDP واقعی اجرا نشد.

### فاز ۶ — لایهٔ HTTP (۲۰۲۶-۰۸-۱۸، برنچ `GetAccountCode`، commit نشده)

اولین سطح HTTP واقعی پروژه. ۶ Endpoint روی ۲ Controller:

| Method | Route | موفق | خطا |
|---|---|---|---|
| POST | `/api/account-codes` | 201 + `Location` + `{id}` | 400 / 409 / 500 |
| GET | `/api/account-codes?pageNumber=&pageSize=` | 200 `PagedResult<AccountCodeDto>` | 400 / 500 |
| GET | `/api/account-codes/{id:guid}` | 200 `AccountCodeDto` | 400 / 404 / 500 |
| POST | `/api/voucher-heads` | 201 + `Location` + `{id}` | 400 / 409 / 500 |
| GET | `/api/voucher-heads?pageNumber=&pageSize=&year=&vahedCode=` | 200 `PagedResult<VoucherHeadDto>` | 400 / 500 |
| GET | `/api/voucher-heads/{id:guid}` | 200 `VoucherHeadDto` | 400 / 404 / 500 |

paging: پیش‌فرض `pageNumber=1`, `pageSize=20`؛ سقف `MaxPageSize=200` و `MaxPageNumber=int.MaxValue/200` (برای جلوگیری از overflow در `Skip`). این سقف در Validator است و از طریق `ValidationBehavior` قبل از رسیدن به DB اعمال می‌شود.

تصمیم‌های تثبیت‌شده:
- **Controller کاملاً نازک است** — فقط `_mediator.Send(...)` و تنها انشعاب مجاز `null → 404`. هیچ business logic ای در Controller نیست.
- **نگاشت خطا مرکزی است** (`Accounting.Api/GlobalExceptionHandler.cs` با `IExceptionHandler` + `AddProblemDetails`)؛ هیچ Controller ای `try/catch` ندارد.
- **✅ نگاشت خطای UNIQUE حل شد:** `UnitOfWork.SaveChangesAsync` تنها جایی است که `OracleException` را می‌شناسد؛ ORA-00001 را به `DuplicateKeyException` (سطح Application) ترجمه می‌کند و `GlobalExceptionHandler` آن را به **409 Conflict** با پیام عمومی نگاشت می‌کند. **`Accounting.Api` هیچ ارجاعی به تایپ‌های Oracle ندارد** — فقط در XML doc نام Oracle آمده.
- **هیچ متن خام Oracle/SQL/stack trace در بدنهٔ پاسخ درز نمی‌کند** (با تست صریح روی JSON سریال‌شده اثبات شد، در هر دو محیط Development و Production یکسان).
- XML doc پروژهٔ Api حالا وارد Swagger می‌شود (`GenerateDocumentationFile` + `IncludeXmlComments`).

تست: پروژهٔ جدید `backend/tests/Accounting.Api.Tests` با **۲۴ تست**. مجموع پروژه: **۱۳۱/۱۳۱ سبز** (۲۲ Domain + ۸۵ Application + ۲۴ Api). build ۰ خطا / ۱۶ warning پیش‌موجود NU1903.

### فاز ۵ — اولین مسیر نوشتن CQRS (۲۰۲۶-۰۸-۱۸، برنچ `addAccountCode`، commit نشده)

الگوی پایه‌ای که همهٔ Commandهای بعدی از روی آن ساخته می‌شوند:

`Command (فقط primitive) → ValidationBehavior (FluentValidation) → Handler (ساخت Entity + تولید Guid) → Repository (فقط stage) → IUnitOfWork.SaveChangesAsync (یک‌بار، توسط Handler)`

تصمیم‌های تثبیت‌شده:
- **محل Interfaceها: `Accounting.Application/Common/Interfaces/`** (نه Domain) — چون `Accounting.Domain` باید صفر وابستگی بماند. Implementation در `Accounting.Infrastructure`.
- **`IUnitOfWork` عمداً باریک و entity-agnostic است** — فقط `SaveChangesAsync` + `Begin/Commit/RollbackTransactionAsync`. هیچ پراپرتی per-entity ندارد. افزودن Entity بعدی **هیچ تغییری** در `IUnitOfWork`/`UnitOfWork` لازم ندارد؛ Repositoryها مستقیماً به Handler تزریق می‌شوند.
- **مرز تراکنش در Handler است** — Repository فقط `DbSet.AddAsync` می‌زند و هرگز `SaveChangesAsync` صدا نمی‌زند.
- **Command هرگز Entity نیست** — Command فقط primitive دارد و Handler خودش Entity می‌سازد.
- **ID سمت Application تولید می‌شود** (`Guid.NewGuid()`)؛ تأیید شد که `ID` هیچ‌کدام از دو جدول DB-generated نیست.
- **اعتبارسنجی عمداً فقط سطحی است** (NotEmpty/MaximumLength منطبق با Fluent Mapping) — طبق تصمیم «Legacy جایگزین کامل»، هیچ invariant حسابداری بازسازی نشد.

پکیج‌های جدید در `Accounting.Application`: `MediatR` 14.2.0، `FluentValidation` 12.1.1، `FluentValidation.DependencyInjectionExtensions` 12.1.1. در تست: `Moq` 4.20.72.

تست: پروژهٔ جدید `backend/tests/Accounting.Application.Tests` با **۴۱ تست** (شامل تأیید صریح ترتیب `AddAsync` قبل از `SaveChangesAsync`). مجموع پروژه: **۶۳/۶۳ سبز** (۲۲ Domain + ۴۱ Application). build ۰ خطا / ۱۶ warning پیش‌موجود NU1903.

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
- **✅ ~~پوشش تست مدل نوشتن جدید صفر است~~** — حل شد در ۲۰۲۶-۰۸-۱۸. با فاز ۵، پروژهٔ `Accounting.Application.Tests` با ۴۱ تست ساخته شد (مجموع ۶۳). ⚠️ ولی این تست‌ها **همگی Unit با Mock** هستند؛ هیچ تست integration روی Oracle واقعی وجود ندارد، پس صحت Fluent Mapping و رفتار واقعی INSERT هنوز اثبات‌نشده است.

- **✅ ~~`TB_ACCOUNTCODE.PARENTID` با `DEFAULT '0'` مسیر ساخت حساب ریشه را خراب می‌کند~~** — حل شد (۲۰۲۶-۰۸-۱۸). `HasDefaultValueSql("'0'")` از Fluent Mapping `PARENTID` حذف شد؛ فقط `HasConversion(GuidToChar36Converter)` باقی ماند. تأیید کاربر بر اساس قاعدهٔ کدینگ Legacy: گروه بدون والد (`PARENTID = null`)، کل←گروه، معین←کل (مثال عددی: گروه `11` → کل `1101` → معین `110101`)، پس این ستون باید بتواند واقعاً `NULL` باشد. حالا که `HasDefaultValueSql` حذف شده، EF Core دیگر `PARENTID` را `ValueGenerated.OnAdd` نمی‌داند و همیشه مقدار CLR واقعی (شامل `null` برای حساب ریشه) را صریح در INSERT می‌فرستد — DDL خود Oracle دست‌نخورده ماند. build ۰ خطا، ۶۳/۶۳ تست سبز. توجه: `TB_VOUCHERSHEAD.DOCLIFE` (`DEFAULT 0`) و `HEAD_DESC` (`DEFAULT '-'`) همچنان دست‌نخورده‌اند چون بی‌خطرند (مقدارشان با نوع ستون سازگار است). ۸ جدول دیگر با `sys_guid()` (خط بالا) هنوز حل‌نشده باقی‌اند — هنوز هیچ Command به آن‌ها دست نزده.

- **🟡 دو UserSecrets store رقیب با همان کلید** — `Accounting.Api.csproj` دارای `UserSecretsId = f5497a90-...` و `Accounting.Infrastructure.csproj` دارای `99fb4522-...` است و **هر دو** کلید `ConnectionStrings:DefaultConnection` را دارند. `Program.cs` علاوه بر بارگذاری خودکار store پروژهٔ Api، صراحتاً `AddUserSecrets<Accounting.Infrastructure.AssemblyMarker>()` را هم صدا می‌زند؛ چون این provider **بعداً** اضافه می‌شود، مقدار store مربوط به Infrastructure برنده است. الان مقدار هر دو یکسان است پس مشکلی بروز نمی‌کند، ولی اگر کسی فقط یکی را عوض کند، تغییرش ممکن است بی‌صدا نادیده گرفته شود. پیشنهاد: یکی از دو store حذف شود.

- **✅ ~~خطای UNIQUE به‌صورت خام به بیرون درز می‌کند~~** — حل شد در ۲۰۲۶-۰۸-۱۸ (فاز ۶). `UK_ACCOUNTCODE` و `UK_VOUCHERHEAD_NUMBER` حالا از طریق `UnitOfWork.SaveChangesAsync` (تشخیص `OracleException { Number: 1 }`) به `DuplicateKeyException` و سپس به **409 Conflict** با پیام عمومی نگاشت می‌شوند. ⚠️ این نگاشت فقط با Unit Test (ساخت `OracleException` واقعی از طریق reflection روی constructor داخلی درایور) اثبات شده؛ **روی Oracle واقعی هرگز اجرا نشده**. ضمناً بررسی پیش‌دستانه (pre-check) وجود ندارد — اتکا کاملاً به constraint سطح DB است، که برای شرایط رقابتی (race) در واقع رفتار درستی است.

### 🔴 تصمیمات باز جدید — امنیت (کشف‌شده ۲۰۲۶-۰۸-۱۸ توسط `security-reviewer` در Gate فاز ۶)

با ساخته‌شدن اولین سطح HTTP، این موارد از حالت نظری خارج شدند و **قبل از هرگونه میزبانی خارج از localhost باید تصمیم‌گیری شوند**:

- **✅ ~~🔴 CRITICAL — هیچ Authentication/Authorization ای وجود ندارد.~~** — **حل شد در ۲۰۲۶-۰۸-۱۹ (فاز ۷).** IDP واقعی سازمان (`Tamin.Framework.Common.Security`) + `app.UseAuthentication()` (که اصلاً در pipeline نبود) + `SetFallbackPolicy(RequireAuthenticatedUser)` + `[AllowAnonymous]` فقط روی `HealthController`. جزئیات کامل و تصمیم باز باقی‌مانده (نگاشت claim) پایین‌تر.
- **✅ ~~🔴 CRITICAL — `ADDUSERID` توسط خود فراخوان قابل جعل است.~~** — **حل شد در ۲۰۲۶-۰۸-۱۹ (فاز ۷).** `AddUserId` کاملاً از هر دو Command حذف شد و مقدار از `ICurrentUser.UserId` سمت سرور می‌آید. چون پراپرتی دیگر **اصلاً وجود ندارد**، بازگشت این آسیب‌پذیری خطای کامپایل می‌دهد نه خطای منطقی (تأیید `qa-tester`).

### ✅ ~~تصمیم باز — اتصال inbound به IDP واقعی سازمان~~ — حل شد (۲۰۲۶-۰۸-۱۹)

صاحب پروژه پکیج رسمی `Tamin.Framework.Common.Security` 1.0.9 را در اختیار گذاشت و مسیر inbound به IDP واقعی سازمان وصل شد (فاز ۷ بخش ۳). سؤال‌های قبلی به‌این‌ترتیب منتفی شدند: پکیج خودش `ValidIssuer = http://idm.tamin.ir` و یک `JsonWebKey` ثابتِ جاسازی‌شده دارد، پس **نه discovery URL لازم است نه JWKS fetch**. audience هم به‌صراحت درخواست کاربر فعلاً با `Financial_Account` مشترک است.

پکیج `IDP` (که در کد outbound `TokenManager` استفاده شده بود) روی feed سازمان **پیدا نشد** → یعنی یک پروژهٔ لوکال داخل solution اصلی کاربر بوده، نه پکیج منتشرشده. پس پورت دستی `TokenManager` به `Accounting.Infrastructure/Idp/` تصمیم درستی بوده و باقی می‌ماند.

### 🔴 تصمیم باز جدید — نگاشت claim برای `ADDUSERID` (۲۰۲۶-۰۸-۱۹)

**این تنها سؤال باقی‌مانده از بستهٔ auth است و عمداً حدس زده نشد.**

`HttpContextCurrentUser.UserId` فعلاً `ClaimTypes.NameIdentifier` را می‌خواند و اگر غایب یا **بیش از ۱۰ کاراکتر** باشد throw می‌کند (عرض ستون Legacy `ADDUSERID`). اما:

- **معلوم نیست کدام claim در توکن واقعی IDP سازمان یک شناسهٔ ≤۱۰ کاراکتری قابل‌نگاشت به `ADDUSERID` دارد.** اگر `sub` یک GUID باشد، قطعاً جا نمی‌شود.
- نکتهٔ مرتبط: پکیج `NameClaimType` را روی `.../identity/claims/name` ست می‌کند (نه `nameidentifier`)، پس `User.Identity.Name` به claim دیگری اشاره می‌کند.
- معلوم نیست بین کدهای کاربری قدیمی Legacy (که ستون `ADDUSERID` را پر کرده‌اند) و هویت‌های IDP جدید اصلاً نگاشتی وجود دارد یا نه.

**⚠️ این هرگز با یک توکن واقعی IDP تست/تأیید نشده** (هیچ فراخوان شبکه‌ای به IDP انجام نشد).

**چرا فعلاً بی‌خطر است:** طراحی `ICurrentUser` عمداً **fail-loud** است — هر ناسازگاری بلافاصله خطا می‌دهد، نه اینکه بی‌صدا یک مقدار غلط/بریده در ستون Audit یک سیستم حسابداری بنویسد. این رفتار **درست است و باید حفظ شود**؛ فقط باید با اولین توکن واقعی راستی‌آزمایی شود.

### 🟡 تصمیمات باز دیگر که در فاز ۷ عمداً حل نشدند

- **`VahedCode` سمت سرور اعمال نمی‌شود.** `ICurrentUser.VahedCode` ساخته شد ولی **به هیچ فیلتر Query وصل نشد**. یعنی `?vahedCode=` همچنان یک پارامتر اختیاری و غیرقابل‌اعتماد است و هر کاربر احراز‌شده می‌تواند اسناد هر واحد سازمانی دیگری را ببیند. باید تصمیم گرفته شود: اجباری و سمت‌سروری شود، یا دید بین‌واحدی صریحاً عمدی اعلام شود.
- **IDOR روی `GetById`** — فاز ۷ فقط authentication و جعل Audit را حل کرد، **نه authorization در سطح رکورد**. هر کاربر احراز‌شده می‌تواند هر `ID` ای را بخواند. باید تأیید شود که این برای این مرحله قابل‌قبول است.
- **محدودکردن Kestrel به `localhost`** — توصیهٔ `security-reviewer` به‌عنوان کاهش‌دهندهٔ ریسک فوری و بدون کد، تا وقتی auth نهایی مستقر نشده. تصمیم عملیاتی با صاحب پروژه.
- **پوشش تست pipeline احراز هویت** — تست‌های `Accounting.Api.Tests` همگی unit اند (بدون `WebApplicationFactory`)، پس این‌که واقعاً یک درخواست بدون توکن ۴۰۱ می‌گیرد و `FallbackPolicy` عملاً اعمال می‌شود **در هیچ تستی اثبات نشده**. `qa-tester` این را به‌عنوان بزرگ‌ترین شکاف پوشش اعلام کرد و عمداً نبست، چون `AddInfrastructure` هنگام ساخت host یک `UseOracle` ثبت می‌کند و یک تست ساده‌لوحانه ممکن بود ناخواسته به دیتابیس **زندهٔ** واقعی وصل شود.
- **🟡 چندمستأجری (`VAHEDCODE`) مرز ایزولاسیون نیست.** `vahedCode` یک پارامتر **اختیاری query string** است که فراخوان خودش می‌دهد، نه فیلتری که سرور از هویت استخراج کند؛ یعنی هر کسی می‌تواند اسناد هر واحد سازمانی دیگری را مرور کند. باید صریحاً تصمیم گرفته شود: یا اجباری و سمت‌سروری شود، یا مستند شود که دید بین‌واحدی عمدی است.
- **🟡 فیلدهای Audit در DTO برای همه قابل خواندن‌اند** — `AddUserId`/`ChangeUserId`/`IsDeleted` در هر دو DTO بی‌قید برگردانده می‌شوند. پس از افزودن نقش‌ها باید تصمیم گرفته شود که آیا این‌ها نیاز به نقش ممتاز دارند.
- **🟡 هیچ Rate Limiting ای وجود ندارد** — سقف ۲۰۰ ردیف در هر صفحه کار می‌کند، ولی هیچ محدودیتی روی تعداد درخواست نیست، پس پیمایش کامل جدول‌ها با اسکریپت ممکن است.
- **🟡 بدنهٔ POST همان MediatR Command است** — هر فیلدی که بعداً به Command اضافه شود، بی‌صدا بخشی از قرارداد عمومی API می‌شود. پیشنهاد `api-contract`: افزودن `CreateXRequest` نازک در لایهٔ Api. (اجرا نشد.)
- **🟡 نسخه‌بندی API وجود ندارد** (`/api/...` بدون `/v1`). افزودن آن بعداً خودش یک breaking change است.

### 🔴 تصمیمات باز جدید — کشف‌شده در فاز ۸ (۲۰۲۶-۰۸-۱۹)

این‌ها **عمداً حل نشدند** چون هرکدام یک تصمیم کسب‌وکاری‌اند، نه یک نقص پیاده‌سازی. طبق Accounting Safety Gate بی‌صدا رد نشدند:

- **🔴 IDOR حالا به مسیر نوشتن هم رسید — تشدید ریسک، نه صرفاً انتقال آن.** تا پیش از این فاز، نبودِ authorization در سطح رکورد فقط روی `GetById` بود یعنی **فقط خواندن**. حالا هر کاربر احراز‌شده می‌تواند **هر** `TB_ACCOUNTCODE` یا `TB_VOUCHERSHEAD` را با دانستن `ID` آن **ویرایش یا حذف کند** — از جمله رکوردهای واحدهای سازمانی دیگر، چون `VAHEDCODE` هنوز سمت سرور اعمال نمی‌شود. این فاز عمداً هیچ قانون IDOR جدیدی اختراع نکرد (طبق دستور صریح)، ولی شدت ریسک موجود از «افشای اطلاعات» به **«تغییر/حذف غیرمجاز دادهٔ حسابداری»** ارتقا یافت. **پیش از هر استقراری باید تصمیم‌گیری شود.**
- **✅ ~~حذف سند هیچ cascade ای ندارد~~** — **کاملاً حل شد (۲۰۲۶-۰۸-۲۰، فاز ۹).** `DeleteVoucherHeadCommand` حالا cascade **کامل سه‌سطحی** دارد: `TB_VOUCHERSHEAD` → `TB_VOUCHERSDETAIL` → `TB_VOUCHERDETAIL_LINK_TAFSILI`، هر سه سطح در **یک تراکنش/یک `SaveChangesAsync`**. رجوع به «فاز ۹» پایین‌تر.
- **🟡 سندی که از قبل حذف شده ولی زیرمجموعهٔ فعال دارد، هرگز تمیز نمی‌شود.** پیامد ظریفِ گارد idempotency: چون `if (entity.ISDELETED == true) return;` بالای cascade است، اگر سندی **پیش از فاز ۹** حذف شده باشد (یعنی وقتی هنوز cascade وجود نداشت)، ردیف‌ها و لینک‌های تفصیلی‌اش همچنان `ISDELETED = false` مانده‌اند و فراخوانی دوبارهٔ Delete هم آن‌ها را تمیز **نمی‌کند**. اگر دادهٔ Legacy چنین اسنادی دارد، به یک اسکریپت backfill یک‌بارمصرف نیاز است — **بررسی نشد و حدس زده نشد** (نیازمند کوئری روی دادهٔ زنده).
- **🔴 حذف گرهٔ کدینگ هیچ بررسی وابستگی ندارد.** `DeleteAccountCodeCommand` یک گره را soft-delete می‌کند بدون اینکه بررسی کند (الف) آیا فرزندی دارد (`PARENTID` خودارجاع، `FK_SELFREFRENCE`) یا (ب) آیا در ردیف‌های سند موجود استفاده شده. پس می‌توان یک گروه را حذف کرد و فرزندان یتیم ولی فعال باقی بمانند. حذف فیزیکی نیست پس FK دیتابیس شکایتی نمی‌کند — یعنی این ناسازگاری **کاملاً بی‌صدا** است.
- **🔴 سند Post شده حالا واقعاً قابل تغییر است.** `UpdateVoucherHeadCommand` اجازه می‌دهد `DOCLIFE` (وضعیت سند) آزادانه عوض شود. invariant «تغییرناپذیری سند پس از Post» در تصمیم دوم آگاهانه حذف شده بود، ولی تا پیش از این فاز مسیر نوشتن فقط `INSERT` داشت پس عملاً قابل بهره‌برداری نبود. **حالا یک مسیر واقعی و در دسترس برای ویرایش/برگرداندن سند نهایی‌شده وجود دارد.** اگر این تضمین لازم است، باید صریحاً در Application یا به‌صورت DB constraint بازسازی شود.
- **🟡 هیچ کنترل همزمانی (optimistic concurrency) وجود ندارد.** schema Legacy ستون `rowversion`/`ETag` ندارد و هیچ‌کدام از دو PUT جدید توکن همزمانی نمی‌گیرند، پس رفتار **last-write-wins** است: دو ویرایش هم‌زمان روی یک سند، یکی را بی‌صدا از بین می‌برد. برای سیستم حسابداری باید آگاهانه پذیرفته یا حل شود (مثلاً مقایسهٔ `UPDATEDDATE` به‌عنوان توکن، یا `If-Match`).
- **🟡 هیچ مسیر undelete/restore وجود ندارد.** چون Update رکورد soft-deleted را 404 می‌دهد و `ISDELETED` در Update نیست، رکورد حذف‌شده از طریق API **قابل بازگردانی نیست**. اگر بازگردانی لازم است، به یک Command صریح (`RestoreXCommand`) نیاز دارد — عمداً در این فاز ساخته نشد.

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
