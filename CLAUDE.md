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
| `database-reverse-engineer` | کشف Read-Only دیتابیس Legacy و Scaffold جدول‌ها به Entity + Fluent Mapping |
| `entity-mapper` | ادغام کنترل‌شدهٔ Legacy Entity در Domain و reconcile مفاهیم هم‌پوشان |
| `backend-dotnet` | Commands/Queries/Handlers، API |
| `api-contract` | قرارداد رسمی OpenAPI/DTO/Error و هماهنگی Backend↔Frontend |
| `frontend-react` | UI، فرم صدور سند با فیلدهای تفصیلی داینامیک |
| `qa-tester` | تست و بررسی کیفیت قبل از commit |
| `security-reviewer` | Gate امنیتی: Auth، دسترسی، داده‌های حساس، Audit |
| `performance-reviewer` | Gate عملکرد: Query Plan، ایندکس، Materialized View، N+1، گزارش‌های حجیم |

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
frontend/src/features/chart-of-accounts
frontend/src/features/vouchers
docs/chart-of-accounts.md              # مستندسازی کامل منطق کدینگ شناور (SUPERSEDED)
docs/progress-log.md                   # لاگ روزانهٔ پیشرفت
docs/phase-log.md                      # آرشیو کامل جزئیات هر فاز (منتقل‌شده از CLAUDE.md در ۲۰۲۶-۰۸-۲۸)
docs/open-decisions.md                 # ریسک‌رجیستر زنده: تصمیمات باز، ریسک‌ها، موارد حدس‌زده‌نشده
docs/tamin-core-entity-reference.md    # مرجع «مستقل vs تعبیه‌شده» استخراج‌شده از پروژهٔ خارجی Tamin.Core
docs/centralaccount-business-reference.md  # مرجع منطق کسب‌وکار از پروژهٔ مرجع D:\CentralAccount
```

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
- [ ] راه‌اندازی اولیه React (Vite) — طبق تصمیم فعلی، فرانت‌اند تا اطلاع ثانوی متوقف است؛ تمرکز روی backend.
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
- [ ] رفع باگ‌های `bool?`→enum — فهرست انباشته حالا **~۱۸ ستون در مسیر نوشتن فعال** است (از فازهای ۱۲/۱۳/۱۴/۱۵/۱۶). جزئیات: `docs/open-decisions.md`.
- [ ] فرم صدور سند با تفصیلی داینامیک.

## ریسک‌های باز 🔴 (خلاصه)

⚠️ این فقط **فهرست عنوان‌ها**ست تا هر ایجنت پیش از شروع کار جدید آن را ببیند. **جزئیات، دلایل، موارد 🟡، و موارد ✅ حل‌شده در `docs/open-decisions.md` است** — پیش از تصمیم‌گیری روی هرکدام، حتماً آنجا را بخوان.

| # | ریسک 🔴 | کشف‌شده در |
|---|---|---|
| ۱ | **IDOR — هیچ authorization در سطح رکورد وجود ندارد.** هر کاربر احراز‌شده می‌تواند هر رکوردی از هر واحد سازمانی را بخواند/بسازد/ویرایش/حذف کند؛ `VAHEDCODE` سمت سرور اعمال نمی‌شود. با هر فاز سطح حمله بزرگ‌تر شده (حالا همهٔ Endpointهای فازهای ۷–۱۶، شامل **شماره حساب/شبا/کارت بانکی**، گردش بانکی، و **صف staging ورودی سند**). **مسدودکنندهٔ استقرار.** | فاز ۷/۸/۱۰/۱۳/۱۴/۱۵/۱۶ |
| ۲ | **باگ `bool?`→enum روی ستون‌های چندمقداری `NUMBER(1)`** — ~۱۸ ستون در مسیر نوشتن فعال، شامل موارد تأییدشدهٔ `TB_ACCOUNTCODE.TYPECODE`، `TB_VOUCHERSHEAD.DOCLIFE`، `TB_WHITEANDBLACKLIST.STATE`، `TB_ELAMHEAD.ELAMHDRAMAD_TYPE` و `TB_PAYRECIVHEAD.PAYRECIVTYPE` (سه‌مقداری: ۱پرداخت/۲دریافت/۳همه؛ تنها موردی که تست پین‌کننده دارد). | فاز ۱۲/۱۳/۱۴/۱۵/۱۶ |
| ۳ | **تضمین تراز (بدهکار = بستانکار) وجود ندارد** — با composite create فاز ۱۰ حالا نقطهٔ طبیعی اعمالش وجود دارد، ولی طبق «تصمیم معماری دوم» عمداً پیاده نشده. | فاز ۱۰ |
| ۴ | **نوع دادهٔ مبلغ حل‌نشده و در مسیر نوشتن فعال است** (`long` در برابر `decimal?`). | فاز ۱۰ |
| ۵ | **سند Post شده واقعاً قابل تغییر است** — `UpdateVoucherHeadCommand` اجازه می‌دهد `DOCLIFE` آزادانه عوض شود. | فاز ۸ |
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
| ۱۶ | **`TmpVoucherHead` فقط سند موقتِ *خالی* می‌سازد** — در پروژهٔ مرجع `TmpVoucherDetail` تعبیه‌شده است و head+details یک‌جا ساخته می‌شوند؛ API ما هیچ راهی برای افزودن ردیف ندارد، پس مسیر «ارتقا به سند اصلی» در دسترس نیست. | فاز ۱۶ |

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
