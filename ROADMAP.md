# Roadmap — سیستم حسابداری

> این فایل خلاصهٔ داشبوردی وضعیت پروژه است. منبع حقیقتِ کامل و روزبه‌روز همیشه `CLAUDE.md` و `docs/progress-log.md` است؛ این فایل فقط برای دید سریع روی روی GitHub نگه داشته می‌شود و باید در پایان هر جلسهٔ کاری (کنار `CLAUDE.md`) به‌روز شود.

## وضعیت کلی

| فاز | وضعیت |
|---|---|
| ۱. اسکلت Backend (.NET Clean Architecture + CQRS) | ✅ انجام شد |
| ۲. Reverse Engineering دیتابیس Legacy Oracle | ✅ انجام شد |
| ۳. تصمیم معماری: Legacy-as-Domain | ✅ انجام شد |
| ۴. تصمیم معماری: Legacy جایگزین کامل مدل Rich | ✅ انجام شد (شامل حذف فیزیکی) |
| ۵. مدل نوشتن روی `Accounting.Domain.Entity` (اتصال Oracle + اولین Command/Query) | ⬜ شروع نشده |
| ۶. فرم صدور سند با تفصیلی داینامیک (Frontend) | ⬜ متوقف — منتظر فاز ۵ |

## Milestone Checklist

- [x] راه‌اندازی solution و ۴ پروژهٔ .NET (net10.0) با رفرنس‌های Clean Architecture — Swagger فعال روی `/swagger`
- [x] Reverse Engineering کامل schema `CENTRALACCOUNT` (۶۵ جدول / ۷۷۴ ستون / ۸۲ FK / ۲۹ UNIQUE / ۱ sequence / ۲۸ View)
- [x] Scaffold همهٔ ۶۵ جدول به Entity + Fluent Mapping (فقط Entity/Mapping، بدون Business Logic)
- [x] انتقال ۶۵ Entity به پروژهٔ `Accounting.Domain` (تصمیم Legacy-as-Domain)
- [x] حذف فیزیکی مدل Rich (۲۲ فایل: ۹ Entity + Rules/ValueObjects + Exceptionهای یتیم + ۷ تست) — قابل بازیابی از commit `9f760ad`
- [x] حل ابهام منبع حقیقت تفصیلی مجاز → `TB_ACCOUNT_LINK_TAFSILGROUP`
- [x] تبدیل ID/FK از نوع `string` به `System.Guid` — ۱۷۷ پراپرتی `CHAR(36)` در ۶۴ Entity (۱۱۵ `Guid` + ۶۲ `Guid?`) + `GuidToChar36Converter` در Infrastructure. پیش‌شرط آن verify روی دادهٔ زنده بود: ۱۲۴٬۰۹۴ مقدار، ۱۰۰٪ GUID استاندارد lowercase-dashed. build ۰ خطا، ۲۲/۲۲ تست سبز. (برنچ `feature/legacy-entity-guid-ids`)
- [x] مسطح‌سازی محل Entityها: `Domain/Legacy/Entities/` → `Domain/Entity/` و namespace `Accounting.Domain.Legacy` → `Accounting.Domain.Entity` (۶۵ فایل با `git mv`). سمت Infrastructure (`Legacy/`, `LegacyDbContext`) عمداً بدون تغییر. build ۰ خطا، ۲۲/۲۲ تست سبز. (برنچ `feature/legacy-entity-guid-ids`)
- [ ] اتصال واقعی به Oracle (connection string، DI برای `LegacyDbContext`)
- [ ] طراحی مدل نوشتن (Command/Handler) مبتنی بر `Accounting.Domain.Entity`
- [ ] اولین Command/Query end-to-end + پوشش تست (الان صفر تست روی مدل نوشتن)
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

جزئیات کامل هر تصمیم (شامل استدلال و پیامدها) در `CLAUDE.md` بخش «معماری کلی» است.

## ریسک‌ها و تصمیمات باز

| اولویت | موضوع | شرح |
|---|---|---|
| 🔴 | الزامی‌بودن تفصیلی در Legacy | هیچ ستون معادل `Requirement`/`ISREQUIRED` در جدول‌های `TB_ACCOUNT_LINK_TAFSILGROUP`, `TB_TAFSIL_GROUP`, `TB_LEVEL_TAFSIL`, `TB_ACCOUNT_LINK_LEVEL` پیدا نشد. نیاز به کوئری روی دادهٔ واقعی. |
| 🟡 | نقش `TB_ACCOUNT_LINK_LEVEL` | مشخص نیست فعال است یا artifact قدیمی — نیاز به کوئری روی داده. |
| 🟡 | یکپارچگی ارجاعی تفصیلی سند | `TB_VOUCHERDETAIL_LINK_TAFSILI` فقط یک FK دارد؛ `TAFSILI_ID`/`LEVEL_ID` بدون FK — هیچ تضمین سطح DB نیست. |
| 🟡 | تضمین تراز بدهکار=بستانکار | از سطح کد حذف شده؛ اگر لازم شود باید در Application یا DB constraint بازسازی شود. |
| 🟡 | پوشش تست مدل نوشتن جدید | صفر — فقط ۱۲ تست روی `AccountCode`/`Money` (+۱۰ تست قرارداد تبدیل Guid). باید با اولین Command/Query ساخته شود. |
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

1. اتصال به Oracle (connection string در `appsettings`، ثبت `LegacyDbContext` در DI).
2. طراحی اولین Command روی `Accounting.Domain.Entity` (مثلاً خواندن/ثبت یک حساب یا سند ساده) با `accounting-domain` + `backend-dotnet`.
3. فعال‌سازی `qa-tester` هم‌زمان با اولین Command تا پوشش تست مدل نوشتن جدید از صفر شروع نشود.
