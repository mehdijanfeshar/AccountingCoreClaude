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
| ۹. فرم صدور سند با تفصیلی داینامیک (Frontend) | ⬜ متوقف |

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
| 🔴 | الزامی‌بودن تفصیلی در Legacy | هیچ ستون معادل `Requirement`/`ISREQUIRED` در جدول‌های `TB_ACCOUNT_LINK_TAFSILGROUP`, `TB_TAFSIL_GROUP`, `TB_LEVEL_TAFSIL`, `TB_ACCOUNT_LINK_LEVEL` پیدا نشد. نیاز به کوئری روی دادهٔ واقعی. |
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
| 🔴 | حذف سند بدون cascade (فاز ۸) | `DeleteVoucherHeadCommand` فقط سرسند را `ISDELETED=true` می‌کند و به `TB_VOUCHERSDETAIL` و `TB_VOUCHERDETAIL_LINK_TAFSILI` دست نمی‌زند؛ ردیف‌های سند حذف‌شده همچنان فعال می‌مانند. باید cascade در Application یا فیلتر join در Read یا constraint در DB تصمیم‌گیری شود. |
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
| `database-oracle` | Schema جدید، Migration، Index، MV |
| `database-reverse-engineer` | کشف Read-Only Legacy + Scaffold |
| `entity-mapper` | ادغام کنترل‌شدهٔ Legacy در Domain |
| `backend-dotnet` | Commands/Queries/Handlers، API |
| `api-contract` | OpenAPI/DTO/Error Contract |
| `frontend-react` | UI و فرم‌ها |
| `qa-tester` | تست و کیفیت |
| `security-reviewer` / `performance-reviewer` | Gateهای امنیت و عملکرد |

## گام بعدی پیشنهادی

1. **🔴 Authorization در سطح رکورد (IDOR) + اعمال سمت‌سروری `VAHEDCODE`** — با آمدن `PUT`/`DELETE` در فاز ۸ این دیگر فقط مسئلهٔ افشای اطلاعات نیست؛ الان **تغییر و حذف غیرمجاز دادهٔ حسابداری** ممکن است. مسدودکنندهٔ استقرار. (پکیج سازمان `RolesAllowedAttribute`/`ClaimRequirementFilter` دارد و آماده است.)
2. **🔴 تصمیم دربارهٔ cascade حذف** — سرسند vs ردیف‌های سند، و گرهٔ کدینگ vs فرزندان/ارجاعات. الان هر دو بی‌صدا ناسازگاری تولید می‌کنند.
3. **🔴 تصمیم دربارهٔ تغییرناپذیری سند Post شده** — آیا `DOCLIFE` باید در `PUT` قفل شود؟
4. اولین تست integration واقعی روی Oracle (با احتیاط و روی دادهٔ یک‌بارمصرف) تا صحت Fluent Mapping اثبات شود — شامل ساخت حساب ریشه (`ParentId = null`)، تأیید عملی نگاشت ORA-00001 → 409، و صحت `UPDATE` مسیرهای جدید فاز ۸.
5. تصمیم دربارهٔ کنترل همزمانی (last-write-wins فعلی) برای مسیرهای `PUT`.
6. جداسازی `CreateXRequest` از MediatR Command (در فاز ۸ فقط برای PUT انجام شد) + نسخه‌بندی API، پیش از آنکه مصرف‌کنندهٔ واقعی (فرانت‌اند) شروع کند.
