# Roadmap — سیستم حسابداری

> این فایل خلاصهٔ داشبوردی وضعیت پروژه است. منبع حقیقتِ کامل و روزبه‌روز همیشه `CLAUDE.md` و `docs/progress-log.md` است؛ این فایل فقط برای دید سریع روی روی GitHub نگه داشته می‌شود و باید در پایان هر جلسهٔ کاری (کنار `CLAUDE.md`) به‌روز شود.

## وضعیت کلی

| فاز | وضعیت |
|---|---|
| ۱. اسکلت Backend (.NET Clean Architecture + CQRS) | ✅ انجام شد |
| ۲. Reverse Engineering دیتابیس Legacy Oracle | ✅ انجام شد |
| ۳. تصمیم معماری: Legacy-as-Domain | ✅ انجام شد |
| ۴. تصمیم معماری: Legacy جایگزین کامل مدل Rich | ✅ انجام شد (شامل حذف فیزیکی) |
| ۵. مدل نوشتن روی `Accounting.Domain.Entity` (اتصال Oracle + اولین Command/Query) | ✅ انجام شد — ۲ Command + ۴ Query با paging؛ ریسک `PARENTID DEFAULT '0'` حل شد. ⚠️ اتصال واقعی (integration test روی Oracle) هنوز انجام نشده. |
| ۶. لایهٔ HTTP (Controller/Endpoint + نگاشت مرکزی خطا) | ✅ انجام شد — ۶ Endpoint روی ۲ Controller، خطای UNIQUE → 409، ۱۳۱/۱۳۱ تست سبز. |
| ۷. Authentication/Authorization + رفع جعل `ADDUSERID` | ✅ انجام شد — هر دو ریسک 🔴 CRITICAL بسته شد؛ `ICurrentUser` + `FallbackPolicy`. auth ورودی به **IDP واقعی سازمان** (`Tamin.Framework.Common.Security` 1.0.9) وصل شد و مسیر JWT محلی کاملاً حذف شد. **۱۷۰/۱۷۰ تست سبز.** ⚠️ نگاشت claim برای `ADDUSERID` هنوز با توکن واقعی تأیید نشده. |
| ۸. تکمیل CRUD — Update + Delete (Soft Delete) | ✅ انجام شد — ۴ Endpoint جدید روی هر دو Entity، جایگزینی کامل (نه PATCH)، حذف نرم با `ISDELETED`، 404 مرکزی. **به درخواست صریح صاحب پروژه همگی `POST` اند نه `PUT`/`DELETE`** (این دو متد در محیط سازمان مجاز نیستند). **۲۵۵/۲۵۵ تست سبز.** ⚠️ ۴ ریسک 🔴 جدید کشف شد (IDOR روی نوشتن، نبود cascade، قابل‌تغییر شدن سند Post شده). |
| ۹. cascade کامل سه‌سطحی حذف نرم سند | ✅ انجام شد — یکی از ۴ ریسک 🔴 فاز ۸ **کاملاً** بسته شد. `SoftDeleteDetailTreeAsync` روی همان `IVoucherHeadRepository` (اجتناب از premature abstraction)، با load+mutate تا مرز تراکنش در Handler حفظ شود. هر سه سطح (`head → detail → tafsili-link`) در یک `SaveChangesAsync`. **۲۷۲/۲۷۲ تست سبز** (۱۳ تست جدید، شامل ۱۱ تست واقعی repository روی SQLite in-memory). |
| ۹.۵. مرجع دامنه از پروژهٔ خارجی `Tamin.Core` (تحقیق/مستندسازی، بدون کد) | ✅ انجام شد — ۱۳۰ فایل Entity به‌صورت **Read-Only** تحلیل و به `docs/tamin-core-entity-reference.md` تبدیل شد: **۴۲ مستقل / ۱۳ تعبیه‌شده / ۶ Head-Detail / ۲ مبهم** + ۴۵ enum + ۲۰ View. از این پس قبل از ساخت هر CRUD الزاماً چک می‌شود. صفر تغییر در پروژهٔ مرجع، صفر کد جدید. ⚠️ ۳ تصمیم باز جدید (مرز Aggregate، ۲ Entity مبهم، واگرایی `long` vs `decimal?`). |
| ۱۰. CRUD مستقل ردیف سند (`TB_VOUCHERSDETAIL`) + composite create | ✅ انجام شد — اجرای تصمیم «مرز Aggregate ترکیبی» صاحب پروژه. ۵ Endpoint جدید روی `VoucherDetailsController` + گسترش `CreateVoucherHeadCommand` با `initialDetails` اختیاری (هدر و همهٔ ردیف‌ها در **یک `SaveChangesAsync`**). حذف ردیف به لینک‌های تفصیلی‌اش cascade می‌کند. **۳۷۹/۳۷۹ تست سبز** (۱۰۷ تست جدید). ⚠️ ۳ ریسک 🔴 جدید (تراز در composite create تضمین نشده، نوع مبلغ، نقض FK → 500 خام). |
| ۱۱. فرم صدور سند با تفصیلی داینامیک (Frontend) | ⬜ متوقف |

## Milestone Checklist

- [x] راه‌اندازی solution و ۴ پروژهٔ .NET (net10.0) با رفرنس‌های Clean Architecture — Swagger فعال روی `/swagger`
- [x] Reverse Engineering کامل schema `CENTRALACCOUNT` (۶۵ جدول / ۷۷۴ ستون / ۸۲ FK / ۲۹ UNIQUE / ۱ sequence / ۲۸ View)
- [x] Scaffold همهٔ ۶۵ جدول به Entity + Fluent Mapping (فقط Entity/Mapping، بدون Business Logic)
- [x] انتقال ۶۵ Entity به پروژهٔ `Accounting.Domain` (تصمیم Legacy-as-Domain)
- [x] حذف فیزیکی مدل Rich (۲۲ فایل: ۹ Entity + Rules/ValueObjects + Exceptionهای یتیم + ۷ تست) — قابل بازیابی از commit `9f760ad`
- [x] حل ابهام منبع حقیقت تفصیلی مجاز → `TB_ACCOUNT_LINK_TAFSILGROUP`
- [x] تبدیل ID/FK از نوع `string` به `System.Guid` — ۱۷۷ پراپرتی `CHAR(36)` در ۶۴ Entity (۱۱۵ `Guid` + ۶۲ `Guid?`) + `GuidToChar36Converter` در Infrastructure. پیش‌شرط آن verify روی دادهٔ زنده بود: ۱۲۴٬۰۹۴ مقدار، ۱۰۰٪ GUID استاندارد lowercase-dashed. build ۰ خطا، ۲۲/۲۲ تست سبز. (برنچ `feature/legacy-entity-guid-ids`)
- [x] مسطح‌سازی محل Entityها: `Domain/Legacy/Entities/` → `Domain/Entity/` و namespace `Accounting.Domain.Legacy` → `Accounting.Domain.Entity` (۶۵ فایل با `git mv`). سمت Infrastructure (`Legacy/`, `LegacyDbContext`) عمداً بدون تغییر. build ۰ خطا، ۲۲/۲۲ تست سبز. (برنچ `feature/legacy-entity-guid-ids`)
- [x] اتصال به Oracle در سطح DI (`AddDbContext<LegacyDbContext>` + `UseOracle`، connection string فقط از User Secrets؛ `appsettings.json` فقط placeholder خالی). ⚠️ اتصال واقعی هنوز اجرا/تست نشده.
- [x] طراحی مدل نوشتن (Command/Handler) مبتنی بر `Accounting.Domain.Entity` — الگوی پایه: `Command → ValidationBehavior → Handler → Repository (فقط stage) → IUnitOfWork.SaveChangesAsync`. `IUnitOfWork` عمداً باریک است تا افزودن Entity بعدی نیازی به تغییرش نداشته باشد. Interfaceها در `Accounting.Application/Common/Interfaces/`. (برنچ `addAccountCode`)
- [x] پوشش تست مدل نوشتن — `Accounting.Application.Tests` با ۴۱ تست Unit (Mock)؛ مجموع **۶۳/۶۳ سبز**. شامل تأیید صریح ترتیب `AddAsync` قبل از `SaveChangesAsync`.
- [x] Query side (خواندن) — ۴ Query با paging (`GetAccountCodes`/`GetAccountCodeById`/`GetVoucherHeads`/`GetVoucherHeadById`) + `PagedResult<T>` + Read Repositoryهای مجزا. سقف صفحه ۲۰۰ ردیف.
- [x] Controller/Endpoint برای هر ۶ سرویس — `AccountCodesController` (`/api/account-codes`) و `VoucherHeadsController` (`/api/voucher-heads`)، هرکدام POST + GET list + GET by id. Controller کاملاً نازک (فقط `_mediator.Send` و انشعاب `null → 404`). (برنچ `GetAccountCode`)
- [x] نگاشت مرکزی خطا — `GlobalExceptionHandler` (`IExceptionHandler` + `AddProblemDetails`): ValidationException → 400، `DuplicateKeyException` → 409، سایر → 500. هیچ متن Oracle/SQL/stack trace در بدنهٔ پاسخ درز نمی‌کند (با تست روی JSON سریال‌شده اثبات شد).
- [x] پوشش تست لایهٔ HTTP — `Accounting.Api.Tests` با ۲۴ تست؛ مجموع **۱۳۱/۱۳۱ سبز** (۲۲ Domain + ۸۵ Application + ۲۴ Api).
- [x] **Authentication/Authorization** — `ICurrentUser` + `HttpContextCurrentUser` + `SetFallbackPolicy(RequireAuthenticatedUser)` + `app.UseAuthentication()` (که اصلاً در pipeline نبود) + `[AllowAnonymous]` فقط روی `HealthController` + 401/403 به شکل `application/problem+json`.
- [x] **رفع جعل‌پذیری `ADDUSERID`** — `AddUserId` کاملاً از هر دو Command حذف شد؛ مقدار از `ICurrentUser` سمت سرور می‌آید. بازگشت آسیب‌پذیری حالا خطای کامپایل است، نه خطای منطقی.
- [x] **پورت `TokenManager` سازمان** (`Accounting.Infrastructure/Idp/`) با ۶ نقص امنیتی/همزمانی اصلاح‌شده. ⚠️ این مسیر **outbound** است و به‌تنهایی محافظت inbound را حل نمی‌کند.
- [x] **اتصال inbound به IDP واقعی سازمان** — پکیج رسمی `Tamin.Framework.Common.Security` 1.0.9 از feed داخلی؛ `AddTaminJWTToken` با audience از config. رفتار پکیج با reflection راستی‌آزمایی شد (اسکیم `Bearer`، `ValidIssuer=http://idm.tamin.ir`، `JsonWebKey` ثابت، بدون discovery). زیرساخت JWT محلی (`DevAuthController`/`JwtOptions`/`DevAuthOptions`) **فیزیکاً حذف شد**.
- [x] **فقط `GET` و `POST`** — به درخواست صریح صاحب پروژه، `PUT` و `DELETE` در این محیط قابل استفاده نیستند. ۴ Endpoint نوشتن به `POST /{id}/update` و `POST /{id}/delete` تبدیل شدند (فعل صریح در route، چون `POST /{id}` خالی با `POST` ساخت مبهم می‌شد) و موفقیت → **200 + `{id}`** به‌جای 204. قاعده با `HttpVerbConventionTests` (reflection روی کل assembly) قفل شد تا بازگشت تصادفی تست را قرمز کند. **این یک محدودیت درخواستی است، نه انتخاب معماری REST داخلی.**
- [x] **تکمیل CRUD — Update + Delete** — ۴ Command جدید (`UpdateAccountCode`/`DeleteAccountCode`/`UpdateVoucherHead`/`DeleteVoucherHead`) + ۴ Endpoint. **جایگزینی کامل انتخاب شد نه PATCH جزئی** (چون تقریباً همهٔ ستون‌های Legacy nullable اند و «ارسال‌نشده» از «صریحاً null» تفکیک‌پذیر نیست). `Id` از route می‌آید نه بدنه (`UpdateXRequest` پراپرتی `Id` **ندارد**). `CHANGEUSERID`/`UPDATEDDATE` فقط از `ICurrentUser` و ساعت سرور. (برنچ `changejWT`)
- [x] **Soft Delete** — `ISDELETED = true`، هرگز حذف فیزیکی؛ write repository اصلاً متد حذف **ندارد** (تضمین ساختاری، با تست reflection قفل شد). DELETE ایدمپوتنت است (رکورد از قبل حذف‌شده → 204 بدون نوشتن). `ISDELETED == null` هم «حذف‌نشده» تلقی می‌شود.
- [x] **`NotFoundException` → 404 مرکزی** در `GlobalExceptionHandler` (الگو از `DuplicateKeyException` → 409)؛ پیام حاوی id/نام منبع به بدنهٔ پاسخ درز نمی‌کند.
- [x] **اعلام 401 روی هر ۱۰ Endpoint** — یافتهٔ `api-contract`: 401 در هیچ Endpointی (حتی ۶ تای قبلی) اعلام نشده بود با اینکه همگی زیر FallbackPolicy اند. 403 عمداً اعلام نشد (هنوز authorization مبتنی بر نقش وجود ندارد).
- [x] پوشش تست فاز ۸ — **۸۵ تست جدید** (۶۹ Application + ۱۶ Api، شامل `HttpVerbConventionTests`)؛ مجموع **۲۵۵/۲۵۵ سبز** (۲۲ Domain + ۱۵۳ Application + ۶۰ Api + ۲۰ Infrastructure)، صفر رگرسیون.
- [ ] تست pipeline احراز هویت (`WebApplicationFactory`) — عمداً ساخته نشد تا ناخواسته به Oracle زنده وصل نشود
- [ ] تست integration واقعی روی Oracle
- [x] حل ریسک `PARENTID DEFAULT '0'` — حذف `HasDefaultValueSql` از Mapping (۲۰۲۶-۰۸-۱۸)
- [x] **حل کامل ریسک 🔴 «حذف سند بدون cascade»** (۲۰۲۶-۰۸-۲۰، فاز ۹) — هر سه سطح؛ ۱۳ تست جدید، مجموع **۲۷۲/۲۷۲ سبز**
- [x] **اولین تست واقعی repository روی یک provider رابطه‌ای** (SQLite in-memory) — ترجمهٔ SQL فیلتر `ISDELETED` اثبات شد؛ اولین باری که کد repository بدون mock اجرا می‌شود
- [x] cascade سطح سوم: ردیف سند → `TB_VOUCHERDETAIL_LINK_TAFSILI` (۲۰۲۶-۰۸-۲۰)
- [ ] بررسی نیاز به backfill برای اسناد حذف‌شدهٔ پیش از فاز ۹ که هنوز ردیف/لینک فعال دارند
- [x] **مرجع «مستقل vs تعبیه‌شده» از پروژهٔ خارجی `Tamin.Core`** (۲۰۲۶-۰۸-۲۰) — `docs/tamin-core-entity-reference.md`؛ ۱۳۰ فایل Read-Only تحلیل شد، ۴۲ مستقل / ۱۳ تعبیه‌شده شناسایی شد تا CRUD اضافه ساخته نشود
- [x] **گرفتن تصمیم مرز Aggregate برای `TB_VOUCHERSHEAD`/`TB_VOUCHERSDETAIL`** (۲۰۲۶-۰۸-۲۰) — صاحب پروژه **مدل ترکیبی** را انتخاب کرد: ردیف سند مستقل است و CRUD خودش را دارد، **اما** ثبت اولیهٔ سند باید هدر+ردیف‌ها را با هم در یک تراکنش ذخیره کند. cascade فاز ۹ دست‌نخورده ماند.
- [x] **CRUD مستقل ردیف سند** (فاز ۱۰) — ۵ Endpoint روی `VoucherDetailsController` (`POST` ساخت / `GET` لیست با فیلتر `voucherHeadId` / `GET` by-id / `POST {id}/update` / `POST {id}/delete`)، همگی زیر محدودیت «فقط `GET`/`POST`». 409 عمداً اعلام **نشد** (این جدول هیچ UNIQUE constraint ندارد)؛ در عوض `POST` ساخت **404** اعلام می‌کند (سرسند ناموجود/حذف‌شده).
- [x] **composite create سند** (فاز ۱۰) — `CreateVoucherHeadCommand` یک پارامتر پایانیِ اختیاری `InitialDetails` گرفت (گزینهٔ «الف»، نه Command جدا) تا منطق ساخت سرسند دو بار نوشته نشود و مسیر ساخت یکی بماند. کاملاً **غیر‌breaking**: بدون `initialDetails` رفتار دقیقاً مثل قبل است. استثنای صریح و مستندِ قانون «Command فقط primitive» برای record تودرتوی primitive-only ثبت شد.
- [x] **cascade حذف ردیف سند → لینک‌های تفصیلی آن** (فاز ۱۰) — اعمال یک‌سطح‌پایین‌ترِ invariant فاز ۹، در همان تک `SaveChangesAsync`. ۶ تست واقعی repository روی SQLite in-memory.
- [x] **قفل رگرسیون: هیچ جدول `*_LINK_TAFSIL*`/`*_LINK_LEVEL` مسیر نوشتن مستقل ندارد** — تست reflection جدید که قاعدهٔ تیمی سند مرجع را در سطح کد enforce می‌کند. همچنین `HttpVerbConventionTests` حالا تأیید می‌کند مجموعهٔ اسکن‌شده خالی نیست و شامل Controller جدید است.
- [x] **رفع شکاف Swagger** (یافتهٔ `api-contract` در فاز ۱۰) — `Accounting.Application` اصلاً `GenerateDocumentationFile` نداشت، پس XML docهای همهٔ Command/DTO/Queryها در Swagger **نامرئی** بودند. رفع و با fetch واقعی `/swagger/v1/swagger.json` راستی‌آزمایی شد.
- [ ] مسیر نوشتن تفصیلی برای ردیف سند (`TB_VOUCHERDETAIL_LINK_TAFSILI`) — امروز فقط cascade حذف دارد؛ نمی‌توان به ردیف سند تفصیلی نسبت داد
- [ ] راه‌اندازی React (Vite) — متوقف تا اطلاع ثانوی
- [ ] فرم صدور سند با فیلدهای تفصیلی داینامیک

## تصمیمات معماری کلیدی (Timeline)

| تاریخ | تصمیم | جزئیات |
|---|---|---|
| ۲۰۲۶-۰۸-۱۷ | **Legacy-as-Domain** | Entityهای Legacy دیگر پشت Anti-Corruption Layer پنهان نمی‌شوند؛ شهروند درجه‌یک `Accounting.Domain` می‌شوند. `Accounting.Domain` همچنان صفر وابستگی خارجی دارد. |
| ۲۰۲۶-۰۸-۱۷ | **Legacy جایگزین کامل مدل Rich** | ساختار Legacy مبنای نهایی مدل نوشتن شد؛ invariantهای مدل Rich (تراز اجباری سند، immutability پس از Post، الزامی‌بودن تفصیلی، سلسله‌مراتب ثابت گروه/کل/معین) آگاهانه کنار گذاشته شدند. |
| ۲۰۲۶-۰۸-۱۷ | **حذف فیزیکی مدل Rich** | ۱۲ تایپ منسوخ (به‌همراه تست‌ها و Exceptionهای یتیم) فیزیکاً حذف شدند؛ در تاریخچهٔ git قابل بازیابی‌اند. |
| ۲۰۲۶-۰۸-۱۸ | **شناسه‌ها از `string` به `System.Guid`** | ۱۷۷ ستون `CHAR(36)` در ۶۴ Entity به `Guid`/`Guid?` تبدیل شدند. ستون فیزیکی Oracle تغییر **نکرد** (هیچ DDL) — تبدیل با `GuidToChar36Converter` در Infrastructure انجام می‌شود تا `Accounting.Domain` صفر وابستگی بماند. خواندن عمداً سخت‌گیرانه است (`ParseExact("D")`) تا داده‌ای بی‌صدا normalize نشود. |
| ۲۰۲۶-۰۸-۱۸ | **مسطح‌سازی پوشهٔ Entity + namespace جدید** | `Accounting.Domain/Legacy/Entities/` → `Accounting.Domain/Entity/` و `namespace Accounting.Domain.Legacy` → `Accounting.Domain.Entity`. دلیل: پس از «Legacy جایگزین کامل»، این Entityها دیگر مدل کناری نیستند بلکه خودِ مدل دامنه‌اند و نام `Legacy` گمراه‌کننده بود. سمت Infrastructure (پوشهٔ `Legacy/` و کلاس `LegacyDbContext`) عمداً تغییر نکرد. نام تصمیم‌های «Legacy-as-Domain» و «Legacy جایگزین کامل» معتبر می‌ماند (به مفهوم اشاره دارند نه به namespace). |

| ۲۰۲۶-۰۸-۱۹ | **Auth: هویت سمت سرور، نه سمت فراخوان** | `AddUserId` از Commandها **حذف** شد (نه صرفاً بی‌اعتماد) و `ICurrentUser` جای آن را گرفت؛ `UserId` عمداً **throw می‌کند و truncate نمی‌کند** (عرض ۱۰ کاراکتری `ADDUSERID`) — همان فلسفهٔ `ParseExact` در تصمیم Guid. اسکیم inbound فعلاً **JWT محلی‌امضا و موقت** است. یافتهٔ تعیین‌کننده: **هیچ ستون credential در کل ۶۵ جدول Legacy وجود ندارد**، پس schema Legacy اصلاً قادر به احراز هویت نیست و اسکیم جدید اجتناب‌ناپذیر بود. `TokenManager` سازمان پورت شد ولی **outbound** است و مسیر inbound را حل نمی‌کند. |

جزئیات کامل هر تصمیم (شامل استدلال و پیامدها) در `CLAUDE.md` بخش «معماری کلی» است.

## ریسک‌ها و تصمیمات باز

| اولویت | موضوع | شرح |
|---|---|---|
| ✅ | ~~**مرز Aggregate برای Head/Detail**~~ | **حل و پیاده شد (۲۰۲۶-۰۸-۲۰، فاز ۱۰).** صاحب پروژه **مدل ترکیبی** را انتخاب کرد — نه مدل `Tamin.Core` (مستقل کامل) و نه مدل cascade فاز ۹ (تابع کامل): ردیف سند CRUD مستقل دارد، **اما** ثبت اولیهٔ سند هدر+ردیف‌ها را در یک تراکنش ذخیره می‌کند. cascade فاز ۹ دست‌نخورده ماند. |
| 🔴 | **تراز (بدهکار=بستانکار) در composite create تضمین نمی‌شود** (فاز ۱۰) | تا این فاز، هدر و ردیف‌ها هرگز در یک درخواست ساخته نمی‌شدند پس تراز عملاً غیرقابل‌بررسی بود. حالا **کل سند در یک تراکنش** می‌آید — یعنی تنها نقطهٔ طبیعیِ چنین تضمینی وجود دارد و طبق «تصمیم معماری دوم» عمداً خالی گذاشته شد. ضمناً یک ردیف می‌تواند هم‌زمان `DEBTOR` و `CREDITOR` غیرصفر داشته باشد. |
| 🔴 | **نوع دادهٔ مبلغ حالا در مسیر نوشتن است** (تشدید‌شده در فاز ۱۰) | تصمیم باز `long` vs `decimal?` تا این فاز نظری بود؛ حالا `DEBTOR`/`CREDITOR` واقعاً از API پذیرفته و نوشته می‌شوند. **قبل از نوشتن هر دادهٔ واقعی روی Oracle باید روشن شود.** |
| 🔴 | **نقض FK به‌جز UNIQUE → 500 خام** (فاز ۱۰) | `UnitOfWork` فقط ORA-00001 را می‌شناسد. یک `AccountId` نامعتبر روی ردیف سند یک **ORA-02291** خام می‌دهد → 500. سرسند pre-check دارد، `ACCOUNT_ID` عمداً نه (تا قانون کسب‌وکاری اختراع نشود). گزینه‌ها: نگاشت ORA-02291 → 400/409، یا pre-check صریح. |
| 🟡 | نبود مسیر نوشتن تفصیلی برای ردیف سند (فاز ۱۰) | `TB_VOUCHERDETAIL_LINK_TAFSILI` طبق قاعدهٔ تیمی تعبیه‌شده است و فقط **حذف** می‌شود. یعنی امروز می‌توان ردیف سند ساخت ولی **نمی‌توان به آن تفصیلی نسبت داد** — شکاف کارکردی واقعی برای «فرم صدور سند با تفصیلی داینامیک». |
| 🟡 | عدم تقارن `VahedCode`/`Year` بین دو مسیر ساخت (فاز ۱۰) | در composite create از سرسند مشتق می‌شوند؛ در ساخت مستقل، فراخوان می‌دهد و **هیچ اعتبارسنجی متقاطعی با سرسند نیست** → می‌توان ردیفی با `YEAR` متفاوت از سند والدش ساخت. |
| 🟡 | `RADIF` هیچ مدیریتی ندارد (فاز ۱۰) | نه تولید خودکار، نه یکتایی در محدودهٔ سند، نه پیوستگی — کاملاً ورودی فراخوان. چون `int?` است، `NULL`ها در مرتب‌سازی Oracle آخر می‌آیند. |
| 🟡 | تکرار منطق cascade در دو repository (فاز ۱۰) | `SoftDeleteDetailTreeAsync` (سطح ۲+۳) و `SoftDeleteTafsiliLinksAsync` (سطح ۳) هم‌پوشانی دارند. فاز ۹ به دستور صریح کاربر دست‌نخورده ماند، ولی XML doc خودش گفته بود در این حالت باید استخراج شود. **follow-up ثبت‌شده.** |
| 🟡 | دو Entity مبهم در `Tamin.Core` | `TB_RABET_CLOSING` و `TB_CHARGE_LINK_COST` سیگنال کپسولگی نیمه‌کاره/متروک دارند (کد مربوطه کامنت شده). قبل از CRUD از کاربر بپرس — بخش ۴ سند مرجع. |
| 🟡 | واگرایی نوع دادهٔ مبلغ | در سراسر `Tamin.Core` مبالغ `long` غیرnullable‌اند، ما `decimal?` مدل کرده‌ایم. یا ریال بدون اعشار ذخیره می‌شده یا ما دقت اضافه می‌کنیم. قبل از Command روی سند باید روشن شود. |
| 🔴 | الزامی‌بودن تفصیلی در Legacy | هیچ ستون معادل `Requirement`/`ISREQUIRED` در جدول‌های `TB_ACCOUNT_LINK_TAFSILGROUP`, `TB_TAFSIL_GROUP`, `TB_LEVEL_TAFSIL`, `TB_ACCOUNT_LINK_LEVEL` پیدا نشد. نیاز به کوئری روی دادهٔ واقعی. **سرنخ جدید (۲۰۲۶-۰۸-۲۰):** در `Tamin.Core` هم هیچ Aggregate Root ای `TbAccountLinkTafsilGroup` را کپسوله نمی‌کند — سیگنال ضعیف به نفع اینکه این قاعده در لایهٔ Application enforce می‌شده، نه Entity/DB. **اثبات نشد.** |
| 🟡 | نقش `TB_ACCOUNT_LINK_LEVEL` | مشخص نیست فعال است یا artifact قدیمی — نیاز به کوئری روی داده. |
| 🟡 | یکپارچگی ارجاعی تفصیلی سند | `TB_VOUCHERDETAIL_LINK_TAFSILI` فقط یک FK دارد؛ `TAFSILI_ID`/`LEVEL_ID` بدون FK — هیچ تضمین سطح DB نیست. |
| 🟡 | تضمین تراز بدهکار=بستانکار | از سطح کد حذف شده؛ اگر لازم شود باید در Application یا DB constraint بازسازی شود. |
| ✅ | ~~پوشش تست مدل نوشتن جدید~~ | حل شد (۲۰۲۶-۰۸-۱۸): ۴۱ تست Application، مجموع ۶۳. ولی همگی Unit با Mock‌اند — هیچ تست integration روی Oracle واقعی نیست. |
| ✅ | ~~`PARENTID DEFAULT '0'` مسیر حساب ریشه را می‌شکند~~ | حل شد (۲۰۲۶-۰۸-۱۸): `HasDefaultValueSql("'0'")` از Fluent Mapping `TB_ACCOUNTCODE.PARENTID` حذف شد. تأیید کاربر: طبق قاعدهٔ کدینگ (گروه بدون والد، کل←گروه، معین←کل؛ مثال `11`→`1101`→`110101`) این ستون باید بتواند واقعاً `NULL` باشد نه `'0'`. حالا EF همیشه مقدار CLR واقعی (شامل `null` برای حساب ریشه) را صریح می‌فرستد. DDL خود Oracle دست‌نخورده ماند. build ۰ خطا، ۶۳/۶۳ تست سبز. |
| ✅ | ~~خطای UNIQUE بدون نگاشت~~ | حل شد (۲۰۲۶-۰۸-۱۸، فاز ۶): `UnitOfWork` تنها جایی است که `OracleException` را می‌شناسد و ORA-00001 را به `DuplicateKeyException` ترجمه می‌کند؛ `GlobalExceptionHandler` آن را به **409 Conflict** با پیام عمومی نگاشت می‌کند. ⚠️ فقط Unit Test دارد، روی Oracle واقعی اجرا نشده. |
| ✅ | ~~**هیچ Authentication/Authorization ای وجود ندارد**~~ | حل شد (۲۰۲۶-۰۸-۱۹، فاز ۷): JWT Bearer + `SetFallbackPolicy(RequireAuthenticatedUser)` + `app.UseAuthentication()` (که اصلاً در pipeline نبود و بدون آن policy بی‌صدا بی‌اثر می‌ماند) + `[AllowAnonymous]` فقط روی `HealthController` + 401/403 به شکل `application/problem+json`. auth ورودی به **IDP واقعی سازمان** وصل است (مسیر JWT محلی حذف شد). |
| ✅ | ~~**`ADDUSERID` قابل جعل توسط فراخوان**~~ | حل شد (۲۰۲۶-۰۸-۱۹، فاز ۷): `AddUserId` **کاملاً حذف شد** از هر دو Command/Validator؛ Handlerها `ADDUSERID = _currentUser.UserId` می‌نویسند. چون پراپرتی دیگر وجود ندارد، بازگشت آسیب‌پذیری خطای کامپایل می‌دهد نه خطای منطقی. |
| ✅ | ~~**اتصال inbound به IDP واقعی سازمان**~~ | حل شد (۲۰۲۶-۰۸-۱۹): پکیج رسمی `Tamin.Framework.Common.Security` 1.0.9 از feed داخلی سازمان. سؤال‌های قبلی منتفی شدند — پکیج خودش `ValidIssuer=http://idm.tamin.ir` و یک `JsonWebKey` **ثابتِ جاسازی‌شده** دارد، پس نه discovery URL لازم است نه JWKS fetch. پکیج `IDP` روی feed **پیدا نشد** → پروژهٔ لوکال solution اصلی کاربر بوده، پس پورت دستی `TokenManager` درست بوده. |
| 🔴 | **نگاشت claim برای `ADDUSERID` تأییدنشده** | معلوم نیست کدام claim توکن واقعی IDP یک شناسهٔ **≤۱۰ کاراکتری** قابل‌نگاشت به `ADDUSERID` دارد (اگر `sub` یک GUID باشد جا نمی‌شود). پکیج `NameClaimType` را روی `.../claims/name` ست می‌کند نه `nameidentifier`. نگاشت بین کدهای کاربری قدیمی Legacy و هویت‌های IDP جدید نامشخص است. **هرگز با توکن واقعی تست نشده.** بی‌خطر است چون `ICurrentUser.UserId` عمداً **fail-loud** است (throw به‌جای truncate) — خطای بلند، نه خرابی خاموش دادهٔ Audit. |
| 🟡 | audience مشترک با `Financial_Account` | به‌صراحت درخواست صاحب پروژه، فعلاً audience پروژهٔ `Financial_Account` استفاده می‌شود تا audience اختصاصی این سرویس در IDP سازمان ثبت شود. از طریق `Tamin:Idp:Audience` قابل override است. |
| 🔴 | **IDOR حالا روی نوشتن هم هست** (تشدید‌شده در فاز ۸) | تا فاز ۷ نبودِ authorization سطح رکورد فقط روی `GetById` یعنی **خواندن** بود. با افزودن `PUT`/`DELETE` در فاز ۸، هر کاربر احراز‌شده می‌تواند **هر** حساب یا سندی را با دانستن `ID` **ویرایش یا حذف کند** — از جمله رکوردهای واحدهای دیگر (چون `VAHEDCODE` سمت سرور اعمال نمی‌شود). شدت از «افشای اطلاعات» به **«تغییر/حذف غیرمجاز دادهٔ حسابداری»** ارتقا یافت. فاز ۸ عمداً قانون جدیدی اختراع نکرد. **مسدودکنندهٔ استقرار.** |
| ✅ | ~~حذف سند بدون cascade~~ (فاز ۸ → **حل‌شده در فاز ۹**) | **کاملاً حل شد (۲۰۲۶-۰۸-۲۰).** cascade **سه‌سطحی**: `TB_VOUCHERSHEAD` → `TB_VOUCHERSDETAIL` → `TB_VOUCHERDETAIL_LINK_TAFSILI`، هر سه در **یک تراکنش/یک `SaveChangesAsync`** (load+mutate، نه `ExecuteUpdateAsync`). **۲۷۲/۲۷۲ تست سبز.** |
| 🟡 | سند از‌قبل‌حذف‌شده با زیرمجموعهٔ فعال هرگز تمیز نمی‌شود (کشف‌شده در فاز ۹) | پیامد گارد idempotency: چون سرسندِ از‌قبل‌حذف‌شده زودتر `return` می‌کند، اسنادی که **پیش از** فاز ۹ حذف شده‌اند ردیف‌ها/لینک‌های فعالشان را نگه داشته‌اند و فراخوانی دوبارهٔ Delete هم تمیزشان نمی‌کند. اگر دادهٔ Legacy چنین اسنادی دارد، اسکریپت backfill یک‌بارمصرف لازم است — **بررسی نشد، حدس زده نشد** (نیازمند کوئری روی دادهٔ زنده). |
| 🔴 | حذف گرهٔ کدینگ بدون بررسی وابستگی (فاز ۸) | `DeleteAccountCodeCommand` بررسی نمی‌کند که گره فرزند دارد (`PARENTID` خودارجاع) یا در ردیف‌های سند استفاده شده. چون حذف نرم است، FK دیتابیس شکایت نمی‌کند — ناسازگاری کاملاً بی‌صدا می‌ماند. |
| 🔴 | سند Post شده حالا واقعاً قابل تغییر است (فاز ۸) | `UpdateVoucherHeadCommand` اجازه می‌دهد `DOCLIFE` آزادانه عوض شود. invariant تغییرناپذیری در تصمیم دوم آگاهانه حذف شده بود، ولی تا فاز ۷ مسیر نوشتن فقط `INSERT` داشت پس قابل بهره‌برداری نبود؛ حالا مسیر واقعی ویرایش/برگرداندن سند نهایی‌شده وجود دارد. |
| 🟡 | نبود کنترل همزمانی (فاز ۸) | schema Legacy ستون `rowversion` ندارد و PUTها توکن همزمانی نمی‌گیرند → **last-write-wins**؛ دو ویرایش هم‌زمان یکی را بی‌صدا از بین می‌برد. گزینه‌ها: `UPDATEDDATE` به‌عنوان توکن، یا `If-Match`. |
| 🟡 | نبود مسیر undelete/restore (فاز ۸) | Update روی رکورد soft-deleted عمداً 404 می‌دهد و `ISDELETED` در Update نیست، پس رکورد حذف‌شده از طریق API قابل بازگردانی نیست. نیازمند `RestoreXCommand` صریح در صورت لزوم. |
| 🟡 | نبود تست pipeline احراز هویت | تست‌های Api همگی unit اند (بدون `WebApplicationFactory`)، پس اعمال واقعی `FallbackPolicy` و ۴۰۱ شدن درخواست بدون توکن **در هیچ تستی اثبات نشده**. عمداً بسته نشد چون `AddInfrastructure` هنگام ساخت host یک `UseOracle` ثبت می‌کند و تست ساده‌لوحانه ممکن بود به دیتابیس **زنده** وصل شود. |
| 🟡 | محدودکردن Kestrel به `localhost` | توصیهٔ `security-reviewer` به‌عنوان کاهش ریسک فوری و بدون کد تا استقرار auth نهایی — تصمیم عملیاتی با صاحب پروژه. |
| 🟡 | `VAHEDCODE` مرز ایزولاسیون نیست | `vahedCode` پارامتر اختیاری query string است که فراخوان می‌دهد، نه فیلتر سمت‌سروری برخاسته از هویت؛ مرور اسناد واحدهای دیگر آزاد است. `ICurrentUser.VahedCode` در فاز ۷ ساخته شد ولی **عمداً به هیچ فیلتر Query وصل نشد**. باید یا اجباری/سمت‌سروری شود یا صریحاً مستند شود که عمدی است. |
| 🟡 | فیلدهای Audit در DTO عمومی‌اند | `AddUserId`/`ChangeUserId`/`IsDeleted` بی‌قید برگردانده می‌شوند؛ پس از افزودن نقش‌ها باید تصمیم گرفته شود. |
| 🟡 | بدون Rate Limiting | سقف ۲۰۰ ردیف در هر صفحه کار می‌کند ولی تعداد درخواست محدود نیست. |
| 🟡 | بدنهٔ POST همان MediatR Command است | هر فیلد جدید در Command بی‌صدا وارد قرارداد عمومی API می‌شود. پیشنهاد: `CreateXRequest` نازک در لایهٔ Api. |
| 🟡 | بدون نسخه‌بندی API | مسیرها `/api/...` بدون `/v1`؛ افزودن بعدی خودش breaking change است. |
| 🟡 | دو UserSecrets store رقیب | هم `Accounting.Api.csproj` و هم `Accounting.Infrastructure.csproj` هرکدام `UserSecretsId` جدا دارند و هر دو کلید `ConnectionStrings:DefaultConnection` را نگه می‌دارند؛ به‌خاطر ترتیب بارگذاری در `Program.cs`، مقدار Infrastructure برنده است. الان مقادیر یکسان‌اند ولی منبع خطای بی‌صدا در آینده است. |
| 🔴 | DEFAULTهای Oracle ناسازگار با `Guid` | ۹ ستون DEFAULT سمت DB دارند که مقدارشان GUID معتبر **نیست** و خواندنشان `FormatException` می‌دهد: `TB_ACCOUNTCODE.PARENTID` با `DEFAULT '0'`، و ۸ جدول با `ID DEFAULT sys_guid()` (`TB_CITY`, `TB_PROVINCE`, `TB_RABET`, `TB_RABET_CLOSING`, `TB_VAHED_INFO`, `TB_VAHED_TYPE`, `TB_WHITEANDBLACKLIST`, `TB_WHITELIST`) — چون `sys_guid()` مقدار `RAW(16)` را ۳۲ کاراکتر **بدون dash و UPPERCASE** می‌نویسد. در دادهٔ فعلی هیچ ردیفی این حالت را ندارد (اپلیکیشن قدیمی همیشه ID را صریح می‌داده)، ولی اگر INSERT جدیدی ستون را خالی بگذارد، خواندن بعدی crash می‌کند. **قبل از اولین Command/Query که این ۹ ستون را لمس کند باید تصمیم‌گیری شود.** |

فهرست کامل و به‌روز همیشه در `CLAUDE.md` بخش «تصمیمات باز» است.

## تیم ایجنت‌ها

| ایجنت | مسئولیت |
|---|---|
| `team-lead` | Orchestrator — همیشه اولین تماس برای هر Task جدید |
| `accounting-domain` | مدل دامنه و قوانین کسب‌وکار |
| `database-reverse-engineer` | کشف Read-Only Legacy + Scaffold |
| `entity-mapper` | ادغام کنترل‌شدهٔ Legacy در Domain |
| `backend-dotnet` | Commands/Queries/Handlers، API |
| `api-contract` | OpenAPI/DTO/Error Contract |
| `frontend-react` | UI و فرم‌ها |
| `qa-tester` | تست و کیفیت |
| `security-reviewer` / `performance-reviewer` | Gateهای امنیت و عملکرد |

## گام بعدی پیشنهادی

1. **🔴 Authorization در سطح رکورد (IDOR) + اعمال سمت‌سروری `VAHEDCODE`** — با آمدن مسیرهای نوشتن در فاز ۸ این دیگر فقط مسئلهٔ افشای اطلاعات نیست؛ الان **تغییر و حذف غیرمجاز دادهٔ حسابداری** ممکن است. فاز ۱۰ سطح حمله را بزرگ‌تر کرد (۵ Endpoint جدید روی ردیف سند، همگی بدون authorization سطح رکورد). مسدودکنندهٔ استقرار. (پکیج سازمان `RolesAllowedAttribute`/`ClaimRequirementFilter` دارد و آماده است.)
2. **🔴 تصمیم دربارهٔ تراز سند** — composite create فاز ۱۰ اولین و طبیعی‌ترین نقطه برای تضمین «بدهکار = بستانکار» است و امروز خالی است. یا آگاهانه پذیرفته شود، یا در `CreateVoucherHeadCommandValidator` بازسازی شود.
3. **🔴 روشن‌کردن نوع دادهٔ مبلغ (`long` vs `decimal?`)** — دیگر نظری نیست؛ `DEBTOR`/`CREDITOR` حالا از API نوشته می‌شوند.
4. **🔴 حذف گرهٔ کدینگ vs فرزندان/ارجاعات** — تنها بخش باقی‌ماندهٔ ریسک «یکپارچگی حذف». ✅ سمت سند در فاز ۹/۱۰ کاملاً حل شد (هر سه سطح + حذف مستقل ردیف)، ولی `DeleteAccountCodeCommand` همچنان بررسی نمی‌کند که گره فرزند دارد یا در ردیف‌های سند استفاده شده — بی‌صدا ناسازگاری تولید می‌کند.
5. **🔴 تصمیم دربارهٔ تغییرناپذیری سند Post شده** — آیا `DOCLIFE` باید در مسیر update قفل شود؟
6. **🟡 مسیر نوشتن تفصیلی برای ردیف سند** — بدون آن، «فرم صدور سند با تفصیلی داینامیک» قابل ساخت نیست.
7. اولین تست integration واقعی روی Oracle (با احتیاط و روی دادهٔ یک‌بارمصرف) تا صحت Fluent Mapping اثبات شود — شامل ساخت حساب ریشه (`ParentId = null`)، تأیید عملی نگاشت ORA-00001 → 409، و صحت `INSERT`/`UPDATE` مسیرهای جدید فاز ۸/۱۰.
8. تصمیم دربارهٔ کنترل همزمانی (last-write-wins فعلی) برای مسیرهای update.
9. جداسازی `CreateXRequest` از MediatR Command (در فاز ۸ فقط برای update انجام شد) + نسخه‌بندی API، پیش از آنکه مصرف‌کنندهٔ واقعی (فرانت‌اند) شروع کند.
