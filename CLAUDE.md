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
- نقش ایجنت `entity-mapper` از «جلوگیری از نشت Legacy به Domain» به «ادغام کنترل‌شدهٔ Legacy در Domain» تغییر کرد. (خودِ ایجنت در ۲۰۲۶-۰۹-۲۱ حذف شد — رجوع به جدول تیم ایجنت‌ها.)

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
| `database-reverse-engineer` | کوئری Read-Only روی اوراکل زنده (Discovery/Scaffold بسته است) |
| `backend-dotnet` | Commands/Queries/Handlers، API |
| `api-contract` | قرارداد رسمی OpenAPI/DTO/Error و هماهنگی Backend↔Frontend |
| `frontend-react` | UI، فرم صدور سند با فیلدهای تفصیلی داینامیک |
| `qa-tester` | تست و بررسی کیفیت قبل از commit |
| `security-reviewer` | Gate امنیتی: Auth، دسترسی، داده‌های حساس، Audit |
| `performance-reviewer` | Gate عملکرد: Query Plan، ایندکس، Materialized View، N+1، گزارش‌های حجیم |

> **✅ `entity-mapper` حذف شد (۲۰۲۶-۰۹-۲۱، تصمیم صریح صاحب پروژه).** کارش تمام شده بود: هر ۶۵ Entity در ۲۰۲۶-۰۸-۱۷/۱۸ ادغام شدند و با حذف فیزیکی مدل Rich، نقش اصلی‌اش (ACL و reconcile مفاهیم هم‌پوشان) **موضوعاً** منتفی شد؛ از آن تاریخ حتی یک‌بار هم صدا زده نشد. ادغام Entity در حالت نادرِ کشف جدول جدید، حالا کار `database-reverse-engineer` + `backend-dotnet` است.

> **✅ `database-oracle` حذف شد (۲۰۲۶-۰۸-۲۰، تصمیم صریح صاحب پروژه).** طبق «Legacy جایگزین کامل»، این پروژه هرگز schema/جدول جدید نمی‌سازد؛ با این قید، تنها مسئولیت‌های واقعی باقی‌ماندهٔ آن (Index، Materialized View، Execution Plan، گزارش‌های سنگین) از قبل عیناً در `performance-reviewer` هم بودند. برای جلوگیری از ایجنت تکراری/بلااستفاده، حذف شد.
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
docs/chart-of-accounts.md              # مستندسازی کامل منطق کدینگ شناور (SUPERSEDED)
docs/progress-log.md                   # لاگ روزانهٔ پیشرفت
docs/phase-log.md                      # آرشیو کامل جزئیات هر فاز (منتقل‌شده از CLAUDE.md در ۲۰۲۶-۰۸-۲۸)
docs/open-decisions.md                 # ریسک‌رجیستر زنده: تصمیمات باز، ریسک‌ها، موارد حدس‌زده‌نشده
docs/tamin-core-entity-reference.md    # مرجع «مستقل vs تعبیه‌شده» استخراج‌شده از پروژهٔ خارجی Tamin.Core
docs/centralaccount-business-reference.md  # مرجع منطق کسب‌وکار از پروژهٔ مرجع D:\CentralAccount
```

### ⚠️ فرانت‌اند در این ریپو نیست (از ۲۰۲۶-۰۹-۱۰)

پروژهٔ React **بیرون این ریپازیتوری** است: **`D:\AiProj\AccountCoreAiProj_UI`** (تصمیم صریح صاحب پروژه). پوشهٔ `frontend/` داخل این ریپو **وجود ندارد و ساخته نمی‌شود**.

```
D:\AiProj\AccountCoreAiProj_UI\        # Vite 6 + React 19 + TS (ریپوی جدا، git init نشده)
  src/app/                             # Router، Providers، صفحهٔ خانه
  src/components/                      # Layout, PageHeader, DataTable, Pagination, Field, ErrorBanner
  src/lib/api/                         # axios client + ApiError(ProblemDetails) + createResourceApi
  src/lib/auth/                        # tokenStore (JWT از IDP سازمان) + AuthContext
  src/lib/session/                     # SessionContext — سال مالی (+ واحد، فقط نمایشی)
  src/features/chart-of-accounts/      # لیست حساب‌ها ← GET /api/account-codes
  src/features/vouchers/               # لیست اسناد ← GET /api/voucher-heads
  src/features/vouchers/dynamic-tafsili/  # امضای hook آیندهٔ تفصیلی داینامیک (عامدانه throw — ریسک 🔴 #۱۸)
  src/types/                           # DTOهای آینه‌ای بک‌اند + PagedResult + ProblemDetails
```

**سه قاعدهٔ الزامی برای هر کار فرانت** (در کد هم کامنت شده‌اند):
1. **هرگز `PUT`/`DELETE` نزن** — الگوی بک‌اند `POST {id}/update` و `POST {id}/delete` است. همهٔ URLها فقط از `createResourceApi` ساخته شوند.
2. **هرگز `vahedCode` از کلاینت نفرست** — سمت سرور از توکن تحمیل می‌شود (فاز ۱۹). ولی `year` واقعاً پارامتر query است.
3. **شکل خطا فقط RFC 7807 ProblemDetails است** — بک‌اند ما envelope `{succeeded, code, messages, data}` پروژهٔ Angular قدیمی را **ندارد**؛ آن الگو را بازنساز.

> 📄 **این فایل عمداً کوتاه نگه داشته می‌شود** (بازآرایی ۲۰۲۶-۰۸-۲۸): جزئیات فازها در `docs/phase-log.md` و ریسک‌ها/تصمیمات باز در `docs/open-decisions.md` هستند. **جزئیات فاز جدید را اینجا اضافه نکن** — در `docs/phase-log.md` بنویس و اینجا فقط یک خط خلاصه با ارجاع بگذار.

## 📌 مرجع دامنه — قبل از ساخت هر CRUD این را بخوان

`docs/tamin-core-entity-reference.md` (ایجادشده ۲۰۲۶-۰۸-۲۰) نگاشت کامل ۱۳۰ فایل Entity پروژهٔ خارجی `D:\CentralAccount\Tamin.Core\Entities` به ۶۵ Entity `TB_XXX` ماست. آن پروژه **روی همان schema اوراکل `CENTRALACCOUNT`** کار می‌کند، پس تصمیم‌های aggregate آن یک سیگنال طراحی معتبر و آزموده است.

**قاعدهٔ الزامی:** `backend-dotnet` (و هر ایجنتی که CRUD می‌سازد) باید **پیش از ساخت Command/Query برای هر `TB_XXX`** این سند را چک کند:
1. اگر Entity در **بخش ۲** سند است (۱۳ مورد تعبیه‌شده) → **CRUD مستقل نساز**؛ عملیات را از طریق Aggregate Root پیاده کن.
2. اگر در **بخش ۴** است (`RabetClosing`, `ChargeLinkCost` — مبهم) → قبل از تصمیم از کاربر بپرس.
3. اگر در **بخش ۵** است (الگوی Head/Detail) → مرز Aggregate؛ رجوع به `docs/open-decisions.md` (این مورد در ۲۰۲۶-۰۸-۲۰ با «مدل ترکیبی» حل شد — جزئیات همان‌جا).
4. بقیه (بخش ۳، ۴۲ مورد) → CRUD مستقل معقول است.

⚠️ **این سند sync خودکار ندارد.** از یک پروژهٔ خارجی READ-ONLY استخراج شده و scope خواندنش عمداً فقط به پوشهٔ `Entities/` محدود بوده (نه `ApplicationUseCases`/`Domain.Common`). اگر آن پروژه تغییر کرد یا به بخش‌های دیگرش دسترسی داده شد، سند باید **دستی** بازبینی شود. بخش ۱۰ سند فهرست مواردی است که بدون خروج از scope قطعی نشد.

## وضعیت فعلی پروژه

<!-- team-lead این بخش را در پایان هر جلسه به‌روزرسانی می‌کند -->
<!-- ⚠️ هر آیتم باید یک/دوخطی بماند. جزئیات کامل هر فاز در docs/phase-log.md است — آنجا اضافه کن، نه اینجا. -->

- [x] راه‌اندازی solution و ۴ پروژهٔ .NET روی net10.0 طبق Clean Architecture + Swagger + `HealthController`.
- [x] **راه‌اندازی اولیه React (Vite + TS)** — ⚠️ **در ریپوی جدا: `D:\AiProj\AccountCoreAiProj_UI`** (نه `frontend/` داخل این ریپو؛ تصمیم صریح صاحب پروژه، ۲۰۲۶-۰۹-۱۰). توقف فرانت برداشته شد. جزئیات: `docs/phase-log.md` بخش «فاز ۲۰».
- [x] ~~طراحی مدل دامنه کدینگ شناور (مدل Rich)~~ — **فیزیکاً حذف شد** (۲۲ فایل، ۲۰۲۶-۰۸-۱۷)؛ قابل بازیابی تا commit `9f760ad`. رجوع به «تصمیم معماری دوم» بالا.
- [x] Reverse Engineering کامل Oracle Legacy (`CENTRALACCOUNT`) — ۶۵ جدول، ۷۷۴ ستون، ۸۲ FK، ۲۹ UNIQUE، ۲۸ View؛ همه Scaffold شده به Entity + Fluent Mapping. ⚠️ **Discovery/Scaffold بسته است** — برای CRUD روی Entity موجود `database-reverse-engineer` را صدا نزن.
- [x] اجرای هر سه تصمیم معماری — Legacy-as-Domain + «Legacy جایگزین کامل» (۲۰۲۶-۰۸-۱۷) + مسطح‌سازی به `Accounting.Domain/Entity/` (۲۰۲۶-۰۸-۱۸). `Accounting.Domain` همچنان **صفر** وابستگی خارجی دارد.
- [x] حل ابهام «منبع حقیقت تفصیلی مجاز» — `TB_ACCOUNTCODE → TB_ACCOUNT_LINK_TAFSILGROUP → TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI`.
- [x] تبدیل ۱۷۷ شناسه از `string` به `Guid` در ۶۴ Entity با `GuidToChar36Converter` (۲۰۲۶-۰۸-۱۸؛ ستون فیزیکی Oracle همچنان `CHAR(36)`) — جزئیات و verify روی دادهٔ زنده: `docs/phase-log.md` بخش آرشیو چک‌لیست.
- [x] اتصال Oracle در DI (`UseOracle`)؛ connection string فقط از User Secrets. ⚠️ **اتصال واقعی هرگز در تست اجرا نشده.**
- [x] **فاز ۵** — اولین مسیر نوشتن CQRS (الگوی پایهٔ همهٔ Commandهای بعدی). جزئیات: `docs/phase-log.md` بخش «فاز ۵».
- [x] **فاز ۶** — Query side + ۶ Endpoint + `GlobalExceptionHandler` مرکزی. **۱۳۱ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۶».
- [x] **فاز ۷** — Authentication/Authorization (IDP سازمان) + رفع جعل‌پذیری `ADDUSERID`. **۱۷۰ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۷».
- [x] **فاز ۸** — تکمیل CRUD: Update + Soft Delete (همگی `POST`؛ `PUT`/`DELETE` به‌درخواست صاحب پروژه ممنوع‌اند). **۲۵۹ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۸».
- [x] **فاز ۹** — cascade کامل سه‌سطحی حذف نرم سند. **۲۷۲ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۹».
- [x] **فاز ۱۰** — CRUD مستقل `TB_VOUCHERSDETAIL` + composite create سند. **۳۷۹ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۰».
- [x] **فاز ۱۱** — نگاشت ORA-02291 → 400 + مسیر نوشتن تفصیلی ردیف سند. **۴۱۷ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۱».
- [x] **فاز ۱۲** — تحلیل Read-Only پروژهٔ مرجع `D:\CentralAccount` + اولین کوئری Oracle زنده. جزئیات: `docs/phase-log.md` بخش «فاز ۱۲».
- [x] **فاز ۱۳** — CRUD دستهٔ دوم: ۸ Entity، ۳۹ Endpoint. **۸۶۹ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۳».
- [x] **فاز ۱۴** — CRUD دستهٔ سوم: ۸ Entity، ۳۹ Endpoint. **۱۳۱۷ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۴».
- [x] **فاز ۱۵** — CRUD دستهٔ چهارم: ۸ Entity، ۴۰ Endpoint (اولین دسته بدون استثنای CRU-only؛ `ElamHead` فقط Head). **۱۸۸۵ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۵».
- [x] **فاز ۱۶** — CRUD دستهٔ پنجم: ۲ Entity، ۱۰ Endpoint (دستهٔ عمداً کوچک؛ `PayReciveHead` و `TmpVoucherHead` هر دو فقط Head). **۲۰۳۹ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۶».
- [x] **فاز ۱۷** — پاس دوم خواندن Read-Only پروژهٔ مرجع `D:\CentralAccount` با دانش ۲۵ Entity فازهای ۱۳–۱۶. **صفر تغییر کد**؛ ۸ یافتهٔ اصلی + تصحیح سه ریسک ثبت‌شده. جزئیات: `docs/centralaccount-business-reference.md` **بخش ۲۴** و `docs/phase-log.md` بخش «فاز ۱۷».
- [x] **فاز ۱۸** — اولین گزارش‌های مالی: تراز آزمایشی ۴/۶/۸ ستونه (۳ Endpoint `GET` روی `api/reports`). اولین SQL خام پروژه — چون `TYPECODE`/`DOCLIFE` با `bool?` قابل بیان نیستند. `FirstDebtor`/`FirstCreditor` (مانده اول دوره) یک‌طرفه‌سازی شدند (تصمیم صریح صاحب پروژه، ۲۰۲۶-۰۹-۰۷). **۲۱۰۸ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۸».
- [x] **فاز ۱۹** — اعمال سراسری `VahedCode` سمت سرور (`VahedScopeBehavior`): نیمهٔ اول ریسک 🔴 #۱ (IDOR) بسته شد — ۳۸ Command + ۲۲ Query. `GetById`/`Update`/`Delete` به تصمیم آگاهانهٔ صاحب پروژه باز ماند؛ `PersonAction` منتظر تصمیم کاربر. پاس `/code-review` ۸-زاویه‌ای مستقل + ۳ اصلاح (کامنت گمراه‌کننده در ۱۵ فایل، `.NotEmpty()` جا‌افتاده در `ElamHead`، ادعای نادرست در `open-decisions.md`). **۲۲۳۴ تست.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۹».
- [x] **فاز ۲۰** — آغاز فرانت‌اند: بررسی READ-ONLY پروژهٔ Angular قدیمی (`D:\WorkSpace\projects\financial-account`) + اسکافولد React/Vite در ریپوی جدا با ۲ صفحهٔ متصل به Endpoint واقعی. **صفر تغییر بک‌اند؛ تست‌ها همچنان ۲۲۳۴.** جزئیات: `docs/phase-log.md` بخش «فاز ۲۰».
- [x] **فاز ۲۱** — دو Query تفصیلی داینامیک (فقط `GET`، روی `AccountCodesController` موجود): ریسک 🔴 #۱۸ / Issue #35 بسته شد و فرم صدور سند از انسداد درآمد. کشف کلیدی: فیلتر visibility تفصیلی یک تساویِ ساده نیست (قاعدهٔ B). **۲۳۰۰ تست** + اولین تست واقعی repository روی SQLite. جزئیات: `docs/phase-log.md` بخش «فاز ۲۱».
- [x] **فاز ۲۲** — پوستهٔ بصری MUI/RTL + اولین فرم‌های واقعی: کدینگ حساب (Create/Edit) و **صدور سند با تفصیلی داینامیک**. صفر تغییر بک‌اند؛ `tsc`/`build` تمیز. جزئیات: `docs/phase-log.md` بخش «فاز ۲۲».
- [x] ~~رفع باگ‌های `bool?`→enum~~ — **کاملاً تمام شد در فاز ۲۸ (۲۰۲۶-۰۹-۲۰).** مسیر: `TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE` (فاز ۲۱) → ۴ ستون `TB_ACCOUNTCODE` (فاز ۲۵) → ۱۴ ستون روی ۸ Entity (فاز ۲۷) → **۹ ستون آخر (فاز ۲۸)**. جدول مرجع: `docs/centralaccount-business-reference.md` §۲۴-۱ (⚠️ یک خطای تأییدشده دارد — `TB_CHECK.EBTAL` — که **تصحیح نشده**؛ رجوع به `open-decisions.md`).
- [x] ~~فرم صدور سند با تفصیلی داینامیک~~ — **ساخته شد در فاز ۲۲** (ریپوی فرانت). ⚠️ ذخیره‌اش دومرحله‌ای و **غیراتمیک** است، چون `CreateVoucherHeadDetailInput` فیلد `tafsiliLinks` ندارد — ریسک 🔴 #۲۱ پایین.
- [x] **فاز ۲۳** — CRUD مستقل `Tafsili` (`TB_TAFSILI`, `api/tafsilis`، Vahed-scoped) + ۴ Endpoint جدید «ارتباط معین با گروه تفصیلی» (`TB_ACCOUNT_LINK_TAFSILGROUP`) به‌صورت parent-scoped روی `AccountCodesController` موجود (نه Controller مستقل، طبق قاعدهٔ `*_LINK_TAFSIL*` تعبیه‌شده — `NoIndependentLinkTableWritePathTests` گسترش یافت). لینک تفصیلی↔گروه‌تفصیلی (`TB_TAFSIL_LINK_TAFSILGROUP`) هم embedded ماند، داخل خودِ `Create/UpdateTafsiliCommand`. **۲۳۷۷ تست سبز** (۲۰۱۴ Application + ۳۷ Domain + ۱۴۶ Api + ۱۸۰ Infrastructure). جزئیات: `docs/phase-log.md` بخش «فاز ۲۳».
- [x] **فاز ۲۴** — رفع باگ دیده‌نشدن لینک‌های تفصیلی↔گروه‌تفصیلی در فرم صدور سند: فیلد صریح `TafsilGroupLinkVahedType` (enum `VahedCategory`) به `Create/UpdateTafsiliCommand` اضافه شد تا `VAHEDTYPE` لینک دیگر همیشه `null` (=فقط واحد سازنده) نباشد. سمت خواندن دست‌نخورد. جزئیات: `docs/phase-log.md` بخش «فاز ۲۴».
- [x] **فاز ۲۵** — اصلاح ۴ ستون `bool?`→enum روی `TB_ACCOUNTCODE` (`TYPECODE`/`TYPEACCCODE`/`TYPEACTION`/`TYPEACTIVITY`): بخش `TB_ACCOUNTCODE` از ریسک 🔴 #۲ بسته شد. کشف تجربی روی Oracle زنده: `.HasConversion<int?>()` اجباری است (قرارداد «`NUMBER(1)`⇒`bool`» درایور Oracle بر اساس store type تصمیم می‌گیرد نه نوع CLR). **۲۴۳۳ تست.** ⚠️ **تغییر شکستهٔ قرارداد API** — این چهار فیلد حالا عدد صحیح‌اند نه بولین؛ فرانت هنوز هماهنگ نشده. جزئیات: `docs/phase-log.md` بخش «فاز ۲۵».
- [x] **فاز ۲۶** — هماهنگ‌سازی فرانت با enumهای `TB_ACCOUNTCODE` (ریپوی `AccountCoreAiProj_UI`): `accountCodeEnums.ts` به‌عنوان منبع واحد مقدار↔برچسب، `Select` به‌جای `TriStateToggle` بولی، Zod محدودشده. شکاف قرارداد فاز ۲۵ بسته شد. جزئیات: `docs/phase-log.md` بخش «فاز ۲۶».
- [x] **فاز ۲۷** — تعمیم `bool`→enum به **۱۴ ستون روی ۸ Entity** (`Tafsili`, `TafsilGroup`, `CheckBook`, `PayReciveHead`, `AttribForAccountCode`, `AccountCodeInterface`, `PersonAction`, `WhiteAndBlackList`) + هماهنگ‌سازی کامل فرانت. **۱۱ enum جدید**؛ `Accounting.Domain` همچنان صفر وابستگی. **۲۵۰۱ تست.** ⚠️ **دسته‌های ۴ و ۵ به تصمیم صریح صاحب پروژه انجام نشدند — ۸ ستون هنوز `bool` غلط‌اند** (فهرست در `docs/open-decisions.md`). جزئیات: `docs/phase-log.md` بخش «فاز ۲۷».
- [x] **فاز ۲۸** — دو کار مستقل: (الف) **۹ ستون آخر `bool`→enum — ریسک 🔴 #۲ کاملاً بسته شد** (دسته‌های ۴ و ۵ + `TB_VOUCHERSHEAD.DOCLIFE`، که بدترین مورد بود: چهار وضعیت *ترتیبی*، دو تا دست‌نیافتنی). `LegacyEnumMappingConventionTests` یافتهٔ فاز ۲۵ را به گارد خودکار تبدیل کرد. **۲۵۳۳ تست.** (ب) **جریان SSO واقعی در فرانت** جایگزین نوار دستی توکن شد (ریپوی جدا، `4c03082`) — ⚠️ **هرگز روی IDP واقعی اجرا نشده**؛ پورت dev حالا عمداً ۴۲۰۰ است. جزئیات: `docs/phase-log.md` بخش «فاز ۲۸».
- [x] **فاز ۲۹** — دو کار مرتبط: (الف) تکمیل **«حساب‌های شناسه‌دار»** (`TB_ATTRIBFORACCOUNTCODE`) — ستون معین در لیست + ۵ فیلتر سمت سرور + تغییر نام از «ویژگی» (که اصلاً پیدا نمی‌شد)؛ (ب) **ساخت کامل «شناسنامه»** (`TB_IDENTITY*`) از صفر در بک‌اند و فرانت، با فرم فیلد-داینامیک و `FixItem` تعبیه‌شده در Aggregate. **۲۶۰۱ تست.** کشف مهم: «تعریف ویژگی» پروژهٔ مرجع همین شناسنامه است، نه `ATTRIBFOR…` — این دو مکانیزم جدا با نام‌های گمراه‌کننده‌اند (سند مرجع **بخش ۲۵**). ⚠️ `TB_IDENTITYDETAIL` (مقادیر متغیر، روی ردیف سند) عمداً ساخته نشد. جزئیات: `docs/phase-log.md` بخش «فاز ۲۹».
- [x] **فاز ۳۰** — عملیات مستقل «انتقال وضعیت سند» (`POST /api/voucher-heads/change-state`، دسته‌ای و همه‌یا-هیچ) + فیلتر وضعیت در لیست، برای کارتابل. نیمی از ریسک 🔴 #۵ را بست: تغییر وضعیت دیگر یک نوشتن بی‌نام روی `DOCLIFE` در مسیر update نیست. ⚠️ نیمهٔ دیگر عمداً باز است (هیچ گذاری ممنوع نیست؛ پروژهٔ مرجع هم قاعده‌ای ندارد که پورت شود). commit `a3e258b`.
- [x] **فاز ۳۱** — **رفع ریسک 🔴 #۱-الف: `ValidationBehavior` برای ۲۲ فاز هیچ‌وقت اجرا نشده بود.** قید جنریک به `notnull` تغییر کرد و `BehaviorPipelineConstraintTests` (reflection روی همهٔ `IPipelineBehavior<,>` + پروب کانتینر واقعی MediatR) جلوی بازگشتش را می‌گیرد؛ صحت گارد با برگرداندن موقت باگ تأیید شد. ۱۸ فایل که مستند می‌کردند «این قواعد اجرا نمی‌شوند» اصلاح شدند. **۲۶۱۲ تست سبز** (۲۲۱۳ Application + ۲۱۶ Infrastructure + ۱۴۶ Api + ۳۷ Domain). جزئیات: `docs/phase-log.md` بخش «فاز ۳۱».
- [x] **فاز ۳۲** — **بستن کامل ریسک 🔴 #۱ (IDOR) و #۱-ج.** نیمهٔ دوم که از فاز ۱۹ آگاهانه باز مانده بود: `GetById`/`Update`/`Delete` حالا مالکیت رکورد را بررسی می‌کنند. `vahedCode` پارامتر **اجباری** امضای ریپازیتوری شد (فراموش‌کردنش = خطای کامپایل)، رکورد واحد دیگر → **۴۰۳**، و ردیف با `VAHEDCODE` خالی عمداً سراسری ماند. تست‌های convention فاز ۱۹ معکوس و بی‌قیدوشرط شدند. ریسک #۱-ج را خودِ کامپایلر بیرون کشید. **۲۶۱۸ تست.** ⚠️ ۲۱ مورد دائماً خارج‌اند (۸ جدول بدون ستون + `VahedInfo` + `PersonAction`=ریسک #۱-ب). جزئیات: `docs/phase-log.md` بخش «فاز ۳۲».

## ریسک‌های باز 🔴 (خلاصه)

⚠️ این فقط **فهرست عنوان‌ها**ست تا هر ایجنت پیش از شروع کار جدید آن را ببیند. **جزئیات، دلایل، موارد 🟡، و موارد ✅ حل‌شده در `docs/open-decisions.md` است** — پیش از تصمیم‌گیری روی هرکدام، حتماً آنجا را بخوان.

| # | ریسک 🔴 | کشف‌شده در |
|---|---|---|
| ~~۱~~ | ✅ **حل شد کامل در فاز ۳۲ (۲۰۲۶-۰۹-۲۱).** نیمهٔ اول در فاز ۱۹ (لیست/جستجو/ساخت)، نیمهٔ دوم امروز: هر `GetById`/`Update`/`Delete` حالا مالکیت رکورد را بررسی می‌کند. مکانیزم: `vahedCode` **پارامتر اجباری** امضای `GetForUpdateAsync`/`GetByIdAsync` شد (کامپایلر هر call site را مجبور می‌کند)، و `VahedOwnership` تنها جای تصمیم است. رکورد واحد دیگر → **۴۰۳** (`UnitAccessDeniedException`؛ تصمیم صریح صاحب پروژه، ۴۰۴ رد شد). مدل دسترسی **تساوی دقیق** است نه سلسله‌مراتبی مرجع (تصمیم صاحب پروژه). ردیف با `VAHEDCODE` خالی = **رکورد سراسری** و برای همه مجاز (قاعده‌ای از پروژهٔ مرجع: `Owner=Global → VahedCode=""`؛ بدون این، همهٔ تفصیلی‌های سراسری از دسترس خارج می‌شدند). تست‌های convention فاز ۱۹ **معکوس شدند**: حالا هر `Get…ByIdQuery` و هر `Delete…Command` *باید* `IVahedScoped` باشد — بی‌قیدوشرط، بدون لیست معافیت موقت. ⚠️ **۲۱ مورد دائماً بیرون‌اند** (۸ جدول بدون ستون `VAHEDCODE` + `VahedInfo` + `PersonAction` — این آخری ریسک #۱-ب است). | فاز ۷–۱۶؛ نیمه‌حل در ۱۹؛ حل کامل در ۳۲ |
| ۱-ب | **`PersonAction` کاملاً بی‌محافظ مانده — بمب ساعتی ارتقای سطح دسترسی.** Create/Update/لیست هر سه unscoped؛ هر کاربری می‌تواند ردیف‌های همهٔ واحدها را ببیند (شامل `USERID` = **کد ملی**) و ردیفی با `VahedCode` دلخواه بسازد. امروز بی‌اثر است (RBAC وجود ندارد)، ولی لحظهٔ ساخت RBAC روی این جدول، مجوزهای جعلیِ از قبل کاشته‌شده فعال می‌شوند. **باید اولین کار فاز RBAC باشد.** | فاز ۱۹ (Gate امنیتی) |
| ~~۱-ج~~ | ✅ **حل شد در فاز ۳۲.** جست‌وجوی سرسند والد در `CreateVoucherDetail` حالا `VahedCode` فراخوان را می‌گیرد. جالب اینکه خودِ کامپایلر پیدایش کرد: وقتی امضای `GetForUpdateAsync` اجباری شد، این call site شکست — یعنی همان ساختاری که برای بستن ریسک #۱ ساخته شد، ریسک #۱-ج را هم به‌صورت جانبی بیرون کشید. | فاز ۱۹ → حل در ۳۲ |
| ~~۱-الف~~ | ✅ **حل شد در فاز ۳۱ (۲۰۲۶-۰۹-۲۱).** قید `where TRequest : IRequest<TResponse>` با `IRequest` غیرجنریک MediatR 14 برآورده نمی‌شد و DI بی‌صدا ردش می‌کرد، پس از فاز ۸ تا ۳۰ FluentValidation روی هیچ Update/Delete اجرا نشده بود. قید به `notnull` تغییر کرد و `BehaviorPipelineConstraintTests` دیگر اجازهٔ بازگشتش را نمی‌دهد (هم برای این دو Behavior، هم هر Behavior آینده). ⚠️ **پیامد زنده:** ۳۷ `IsInEnum()` روی Commandهای Update حالا واقعاً اجرا می‌شوند، پس ریسک‌های #۲-د و #۱۳ از تئوری به عملی تبدیل شدند — ردیف‌های زنده‌ای که از قبل خارج از enum بودند خوانده می‌شوند ولی ویرایششان ۴۰۰ می‌گیرد. | فاز ۱۹ → حل در ۳۱ |
| ~~۲~~ | ✅ **حل شد کامل در فاز ۲۸ (۲۰۲۶-۰۹-۲۰).** هر ۱۶ ستون تأییدشده به‌علاوهٔ `TYPECODE` و `DOCLIFE` حالا enum واقعی‌اند (مسیر: فاز ۲۱ → ۲۵ → ۲۷ → ۲۸). قاعدهٔ نگاشتِ فاز ۲۵ دیگر چیزی نیست که یادت بماند — `LegacyEnumMappingConventionTests` خودکار اجبارش می‌کند. ⚠️ **دو مورد کوچک باز ماند** (هر دو در `docs/open-decisions.md` بخش «فاز ۲۸»): (۱) وجود ردیف زنده با مقدار `0` روی `DOCLIFE`/`PERSONTYPE` راستی‌آزمایی نشده — چنین ردیفی خوانده می‌شود ولی ویرایشش ۴۰۰ می‌گیرد؛ (۲) خودِ §۲۴-۱ سند مرجع هنوز خطای `TB_CHECK.EBTAL` را دارد. | فاز ۱۲–۱۷ → حل در ۲۸ |
| ۲-د | **یک ردیف زنده با `TYPEACTION = 5` خارج از enum سه‌مقداری** (`ACCCODE=006000`, حذف‌نشده, آشکارا دادهٔ تستی). خواندنش سالم است، ولی `IsInEnum()` اجازهٔ ویرایش آن از طریق API را نمی‌دهد. هم‌الگوی ریسک #۱۳. | فاز ۲۵ |
| ۲-الف | **گارد وابستگی هنگام حذف در ۱۲ Entity وجود ندارد** — پروژهٔ مرجع تقریباً همه‌جا حذف **فیزیکی** می‌کند و برای همین ۱۲ گارد صریح دارد؛ ما حذف نرم می‌کنیم پس هیچ FK ای شکایت نمی‌کند و همان نقض‌ها **بی‌صدا** اتفاق می‌افتند. تعمیم ریسک‌های «حذف گرهٔ کدینگ» (فاز ۸) و «حذف `LevelTafsil`/`TafsilGroup`» (فاز ۱۴). | فاز ۱۷ |
| ۲-ب | **۹ ستون که ما ورودی آزاد فراخوان گرفته‌ایم و باید سمت سرور تولید/ثابت شوند** (`ELAMH_SERIALNO`, `ELAMH_CODE`, `WEB_STAT`, `CHECKBOOK_TYPE`, `WORKSHOP.ISACTIVE`, `PERSON_ACTION.STATUS`/`USERNAME`, `RADIF`, `PAYRECIVCODE`). ضمناً `TB_PERSON_ACTION.USERID` **کد ملی** است نه شناسهٔ کاربری. | فاز ۱۷ |
| ۲-ج | **`CheckBook` بدون برگ چک** — در پروژهٔ مرجع Create دسته‌چک به‌ازای هر شماره در بازه یک ردیف `TB_CHECK` می‌سازد؛ `POST /api/check-books` ما دسته‌چکی تولید می‌کند که از دید بقیهٔ سیستم **خالی** است. هم‌خانوادهٔ ریسک‌های مرز Aggregate فاز ۱۵/۱۶. | فاز ۱۷ |
| ۳ | **تضمین تراز (بدهکار = بستانکار) وجود ندارد** — با composite create فاز ۱۰ حالا نقطهٔ طبیعی اعمالش وجود دارد، ولی طبق «تصمیم معماری دوم» عمداً پیاده نشده. | فاز ۱۰ |
| ۴ | **نوع دادهٔ مبلغ حل‌نشده و در مسیر نوشتن فعال است** (`long` در برابر `decimal?`). | فاز ۱۰ |
| ۵ | **سند Post شده واقعاً قابل تغییر است** — `UpdateVoucherHeadCommand` اجازه می‌دهد `DOCLIFE` آزادانه عوض شود. ⚠️ فاز ۲۸ فقط **نوع دادهٔ** غلط را اصلاح کرد (حالا enum چهارحالتهٔ `DocLife` است، نه `bool?`)؛ **عملیات نامتمایز سر جایش است**. پروژهٔ مرجع تغییر وضعیت را در `ChangeStateCommandHandler` جدا کرده و مسیر update آن اصلاً این ستون را لمس نمی‌کند — این یک تغییر طراحی است، نه تایپ. | فاز ۸ |
| ۶ | **حذف گرهٔ کدینگ هیچ بررسی وابستگی ندارد** — فرزندان یتیم و ارجاع‌های فعال، کاملاً بی‌صدا. | فاز ۸ |
| ۷ | **حذف `LevelTafsil`/`TafsilGroup` هم بررسی وابستگی ندارد** — از مورد ۶ حساس‌تر است (منبع حقیقت تفصیلی به آن‌ها FK می‌زند). | فاز ۱۴ |
| ۸ | **نوشتن روی `TB_VAHED_INFO` هیچ ردّ Audit ای ندارد** — این جدول هیچ ستون Audit ای ندارد، در حالی که ریشهٔ چندمستأجری آینده است. | فاز ۱۴ |
| ۹ | **`CITY_ID`/`PARENT_ID` در `TB_VAHED_INFO` هیچ FK ندارند** → مقدار نامعتبر بی‌صدا نوشته می‌شود (نگاشت مرکزی ORA-02291 کمکی نمی‌کند). هم‌الگوی `TB_VOUCHERDETAIL_LINK_TAFSILI.TAFSILI_ID`. | فاز ۱۱/۱۴ |
| ۱۰ | **نگاشت claim برای `ADDUSERID` هرگز با توکن واقعی IDP تأیید نشده** — طراحی عمداً fail-loud است. | فاز ۷ |
| ۱۱ | **DEFAULTهای سمت Oracle ناسازگار با `Guid`** — ۸ جدول با `ID DEFAULT sys_guid()` که خواندنشان `FormatException` می‌دهد. | ۲۰۲۶-۰۸-۱۸ |
| ۱۲ | **«الزامی بودن تفصیلی» در کد ما پیاده نشده** — مکانیزمش (وجود/نبود ردیف در `TB_ACCOUNT_LINK_LEVEL`) از فاز ۱۲ شناخته‌شده است، ولی بازسازی نشده. | باز از ابتدا |
| ۱۳ | **دادهٔ Legacy موجود از قبل با یک قاعدهٔ محتمل در تناقض است** — ۳ حساب سطح گروه با `TYPEACTIVITY ∈ {4,5,6}` برخلاف Validator پروژهٔ مرجع. | فاز ۱۲ |
| ۱۴ | **۶ ستون شناسه در فاز ۱۵ هیچ FK ندارند** → مقدار نامعتبر بی‌صدا نوشته می‌شود (`TB_ACCOUNT.ACCOUNTTYPE_ID`، سه ستون `TB_BANKCARTDETAIL`، `TB_CHEQUES_INCORRENT.CHECK_ID`، دو ستون `TB_ELAMHEAD`). هم‌الگوی موارد ۹ و فاز ۱۱. | فاز ۱۵ |
| ۱۵ | **حذف `ElamHead`/`CheckBook`/`PayReciveHead`/`TmpVoucherHead` هیچ cascade ای به فرزندانشان ندارد** (`TB_ELAMDETAIL`, `TB_CHECK`, `TB_PAYRECIVDETAIL`, `TB_TMP_VOUCHERSDETAIL`) — همان وضعیت سند پیش از فاز ۹. مرز Aggregate هر سه جفت Head/Detail هنوز تصمیم‌گیری نشده. | فاز ۱۵/۱۶ |
| ۱۷ | **معنای «مانده اول دوره» در تراز آزمایشی قطعی نیست** — فرمول مرجع (`TotDebtor - CurDebtor`) گردش خام می‌دهد ولی برچسب فارسی ماندهٔ خالص یک‌طرفه را القا می‌کند. وفادار به فرمول پیاده شد؛ اگر معنا «خالص» باشد **مقادیر گزارش عوض می‌شوند**. ⬅️ نیازمند تصمیم صاحب پروژه. | فاز ۱۸ |
| ~~۱۸~~ | ✅ **حل شد در فاز ۲۱** — هر دو Endpoint ساخته شدند (`GET /api/account-codes/{id}/tafsili-levels` و `.../{levelId}/items`). فرم صدور سند دیگر مسدود نیست. Issue #35 بسته شد. جزئیات: `docs/phase-log.md` بخش «فاز ۲۱». | فاز ۲۰ → ۲۱ |
| ۱۹ | **بک‌اند هیچ CORS ای ندارد** — در فاز ۲۰ با Vite dev proxy دور زده شد؛ لحظهٔ استقرار جدای فرانت، همهٔ فراخوان‌ها می‌شکنند. نیازمند سیاست CORS صریح (whitelist، نه `*`) یا سرو از همان origin. | فاز ۲۰ |
| ۲۰ | **توکن JWT در `localStorage` فرانت** — در معرض XSS، با بستن تب پاک نمی‌شود. برای اسکلت dev پذیرفته شد؛ پیش از استقرار باید تصمیم‌گیری شود. ⚠️ فاز ۲۸ این را عوض نکرد (پکیج مرجع `TaminStorageService` هم همین کار را می‌کند، پس پسرفت نیست) — ولی حالا توکن از جریان واقعی SSO می‌آید نه paste دستی، یعنی ریسک فرضی نیست. | فاز ۲۰ |
| ۲۲ | **جریان SSO فرانت هرگز روی IDP واقعی اجرا نشده** — همه‌چیز از `TaminSecurityService` نصب‌شده پورت شد (نه حدس) و `tsc`/`build` تمیزند، ولی هیچ لاگین واقعی انجام نشده. دو مورد صریحاً UNVERIFIED: شاخهٔ `code`+PKCE (مسیر production؛ dev از `token` implicit استفاده می‌کند) و نام claim نام کاربر. ⚠️ پورت dev حالا **عمداً روی ۴۲۰۰ ثابت** است — بدون ثبت URL جدید در IDP عوضش نکن. | فاز ۲۸ |
| ۲۱ | **ذخیرهٔ سند از فرم فرانت اتمیک نیست** — `CreateVoucherHeadDetailInput` (آیتم‌های `initialDetails`) فیلد `tafsiliLinks` **ندارد**، پس فرم فاز ۲۲ اجباراً اول سرسند و سپس تک‌تک ردیف‌ها را می‌فرستد. خطای میانی ⇒ **سند ناقص در دیتابیس** و بک‌اند هیچ endpoint ای برای rollback ندارد. سمت UI با retry فقط-ردیف‌های-ناموفق مهار شده، ولی راه‌حل واقعی یک composite endpoint در بک‌اند است. ⬅️ نیازمند تصمیم صاحب پروژه. | فاز ۲۲ |
| ۱۶ | **`TmpVoucherHead` فقط سند موقتِ *خالی* می‌سازد** — در پروژهٔ مرجع `TmpVoucherDetail` تعبیه‌شده است و head+details یک‌جا ساخته می‌شوند؛ API ما هیچ راهی برای افزودن ردیف ندارد، پس مسیر «ارتقا به سند اصلی» در دسترس نیست. | فاز ۱۶ |

## قوانین کاری تیم

1. هرگز منطق اعتبارسنجی تفصیلی الزامی را فقط در UI ننویس — باید در Domain/Application هم باشد.
2. سمت Read (گزارش‌ها) باید از View/Materialized View مجزا بخواند، نه مستقیماً از مدل نوشتن (اصل CQRS). **⚠️ استثنای ثبت‌شده (فاز ۱۸):** گزارش‌های تراز آزمایشی پایه (۴/۶/۸ ستونه) مستقیماً از `TB_VOUCHERSDETAIL`/`TB_VOUCHERSHEAD`/`TB_ACCOUNTCODE` با SQL خام می‌خوانند، نه از View — چون (۱) هیچ View معادلی روی schema ما Scaffold نشده (رجوع `docs/open-decisions.md`)، و (۲) خودِ پروژهٔ مرجع هم برای این خانوادهٔ خاص از گزارش («پایه»، نه Drill-down) دقیقاً همین کار را می‌کند. این استثنا **فقط برای این سه گزارش** است، نه یک الگوی عمومی — گزارش‌های بعدی (Drill-down، دفتر کل، ترازنامه) باید ابتدا وجود View مناسب را چک کنند و این قانون را دور نزنند مگر با همین سطح توجیه صریح.
3. schema دیتابیس، مدل دامنه، و Contract بک‌اند/فرانت باید همیشه هماهنگ باشند — هماهنگی بین ایجنت‌ها وظیفهٔ `team-lead` است.
4. در پایان هر جلسهٔ کاری: به‌روزرسانی این فایل (بخش «وضعیت فعلی»)، افزودن یک خط به `docs/progress-log.md`، به‌روزرسانی `ROADMAP.md` و GitHub Project board (https://github.com/users/mehdijanfeshar/projects/2)، و `git commit`.
5. **داشبورد همیشه باید آینهٔ وضعیت واقعی پروژه باشد** — به درخواست صریح کاربر (۲۰۲۶-۰۸-۱۷)، هر تغییری در وضعیت پروژه (فاز تمام/شروع شد، تصمیم معماری، ریسک جدید) باید هم‌زمان در `ROADMAP.md` و در Status فیلد GitHub Project منعکس شود؛ صرفاً به‌روز نگه‌داشتن `CLAUDE.md` کافی نیست.

## نحوهٔ ادامهٔ کار در روزهای بعد

1. `git pull` برای گرفتن آخرین تغییرات.
2. Claude Code را در ریشهٔ پروژه اجرا کن — این فایل به‌طور خودکار خوانده می‌شود.
3. به Claude بگو مثلاً: «طبق وضعیت فعلی در CLAUDE.md ادامه بده» یا مستقیماً یک وظیفهٔ جدید بده؛ `team-lead` بر اساس این فایل کار را ادامه می‌دهد.
