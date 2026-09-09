# لاگ پیشرفت پروژه

**این فایل عمداً کوتاه است و باید کوتاه بماند.** هر ورودی = یک تاریخ + **یک تا دو خط** خلاصه + ارجاع به جایی که جزئیات کامل هست.

> ⚠️ **قاعدهٔ الزامی (از ۲۰۲۶-۰۸-۲۸):** جزئیات کامل هر فاز در `docs/phase-log.md` نوشته می‌شود، ریسک‌ها و تصمیمات باز در `docs/open-decisions.md`، و تصمیمات معماری بنیادین در `CLAUDE.md`. **اینجا فقط اشاره‌گر بگذار.** هدف این است که هزینهٔ توکنِ خواندن این فایل با اضافه‌شدن هر فاز جدید تقریباً ثابت بماند. اگر ورودی جدیدت از دو خط بلندتر شد، یعنی محتوا را اشتباهی اینجا نوشته‌ای.
>
> **ترتیب:** جدیدترین بالاتر.
>
> **تاریخچه:** تا ۲۰۲۶-۰۸-۲۸ هر ورودی یک پاراگراف چندصدکلمه‌ای بود که همان جزئیات `phase-log.md` را تکرار می‌کرد (فایل به ۳۹۷ خط رسیده بود). در آن تاریخ همه فشرده شدند؛ هیچ اطلاعاتی حذف نشد، فقط به سند مالکِ خودش منتقل یا به آن ارجاع داده شد.

---

## ورودی‌ها

- **2026-09-09** (فاز ۱۹، برنچ `EntityCRUD`، commit نشده): اعمال سراسری `VahedCode` سمت سرور با `VahedScopeBehavior` (MediatR pipeline) — نیمهٔ اول ریسک 🔴 #۱ (IDOR) بسته شد: ۳۸ Command + ۲۲ Query روی ۱۹ Entity + ۳ گزارش تراز. `GetById`/`Update`/`Delete` به تصمیم آگاهانهٔ صاحب پروژه باز ماند؛ `PersonAction` منتظر تصمیم کاربر دست‌نخورده ماند. جزئیات: `docs/phase-log.md` بخش «فاز ۱۹»؛ باگ 🔴 جدید `ValidationBehavior` و ۳ ریسک دیگر: `docs/open-decisions.md`.
- **2026-09-09** (پاس `/code-review` مستقل فاز ۱۹، ۸ زاویهٔ موازی): ۳ اصلاح واقعی — `.NotEmpty()` جا‌افتاده در `ElamHead`، کامنت گمراه‌کننده «not dead code» در ۱۵ فایل `Update*Validator`، ادعای نادرست «نمی‌تواند منتقل کند» در `open-decisions.md`. **۲۲۳۴/۲۲۳۴ تست سبز.** جزئیات: `docs/phase-log.md` بخش «فاز ۱۹» §۱۱.

- **2026-09-07** (ادامهٔ فاز ۱۸، پیش از commit): پاس `/code-review` ۲ یافته داد و همان‌جا رفع شد — `JOIN` غیرشرطی به `ak`/`ag` که می‌تونست ردیف معتبر رو از هر سه سطح گزارش بی‌صدا حذف کنه (به `LEFT JOIN` تبدیل شد)، و نقض مستندنشدهٔ قانون تیم #۲ (استثنای View حالا رسماً در `CLAUDE.md` ثبت شد). **۲۱۰۸/۲۱۰۸ همچنان سبز.** جزئیات: `docs/open-decisions.md`.
- **2026-09-07** (ادامهٔ فاز ۱۸، پیش از commit): طبق تصمیم صریح صاحب پروژه، `FirstDebtor`/`FirstCreditor` (مانده اول دوره) یک‌طرفه (`Math.Max`) شدند، نه خام مثل فرمول مرجع. **۲۱۰۸/۲۱۰۸ سبز.** جزئیات: `docs/open-decisions.md`.
- **2026-09-07** (فاز ۱۸، برنچ `EntityCRUD`، commit نشده): اولین گزارش‌های مالی — تراز آزمایشی ۴/۶/۸ ستونه، ۳ Endpoint `GET` روی `api/reports`، با اولین SQL خام پارامتری پروژه (چون `TYPECODE`/`DOCLIFE` با `bool?` قابل بیان نیستند). **۲۱۰۹/۲۱۰۹ سبز** (+۷۰). جزئیات: `docs/phase-log.md` بخش «فاز ۱۸»؛ ابهام باز «مانده اول دوره» و ۴ ریسک دیگر: `docs/open-decisions.md`.

- **2026-09-06** (فاز ۱۷، برنچ `EntityCRUD`، commit نشده): پاس دوم خواندن Read-Only پروژهٔ مرجع `D:\CentralAccount` با دانش ۲۵ Entity فازهای ۱۳–۱۶ — **صفر تغییر کد**، فقط مستندسازی. ۸ یافتهٔ اصلی (۱۶ ستون `bool`→enum تأییدشده، ۱۲ گارد وابستگی حذف، ۹ ستون سمت‌سروری، ۴ مرز Aggregate، ماتریس نقش‌های سازمان). جزئیات: `docs/centralaccount-business-reference.md` **بخش ۲۴** و `docs/phase-log.md` بخش «فاز ۱۷»؛ ریسک‌ها: `docs/open-decisions.md`.

- **2026-09-06** (فاز ۱۶، برنچ `EntityCRUD`، commit نشده): CRUD دستهٔ پنجم — دستهٔ عمداً کوچک با ۲ Entity (`PayReciveHead`, `TmpVoucherHead`)، **۱۰ Endpoint روی ۲ Controller**، هر دو فقط Head. **۲۰۳۹/۲۰۳۹ سبز** (+۱۵۴)، ۱۱ تصمیم باز جدید. جزئیات: `docs/phase-log.md` بخش «فاز ۱۶»؛ ریسک‌ها: `docs/open-decisions.md`.

- **2026-09-05** (فاز ۱۵، برنچ `EntityCRUD`، commit نشده): CRUD دستهٔ چهارم — ۸ Entity مستقل، **۴۰ Endpoint روی ۸ Controller**، با سه ایجنت موازی `backend-dotnet`. **اولین دسته بدون استثنای CRU-only** (هر ۸ جدول `ISDELETED` + Audit دارند)؛ `ElamHead` فقط Head. **۱۸۸۵/۱۸۸۵ سبز**، ۹ تصمیم باز جدید. جزئیات: `docs/phase-log.md` بخش «فاز ۱۵»؛ ریسک‌ها: `docs/open-decisions.md`.

- **2026-08-28** (ادامهٔ فاز ۱۴، پیش از commit): پاس `/code-review` با **۸ Agent موازی**؛ ۱ باگ واقعی (`IdentitySubGroup.SubgrpsLen` بدون سقف در Validator → ۵۰۰ خام به‌جای ۴۰۰) رفع شد، تست از ۱۳۱۳ به **۱۳۱۷**. جزئیات کامل: `docs/phase-log.md` بخش «فاز ۱۴».

- **2026-08-28** (فاز ۱۴، برنچ `EntityCRUD`، commit نشده): CRUD دستهٔ سوم — ۸ Entity مستقل، **۳۹ Endpoint روی ۸ Controller**، با سه ایجنت موازی `backend-dotnet`. `VahedInfo` فقط CRU (هیچ ستون Audit ای ندارد). **۱۳۱۳/۱۳۱۳ سبز**، ۸ تصمیم باز جدید. جزئیات: `docs/phase-log.md` بخش «فاز ۱۴»؛ ریسک‌ها: `docs/open-decisions.md`.

- **2026-08-27** (فاز ۱۳، برنچ `EntityCRUD`، commit نشده): CRUD دستهٔ دوم — ۸ Entity مستقل، **۳۹ Endpoint روی ۸ Controller**؛ `PreDescrib` فقط CRU؛ گارد جدید `RepositoryRegistrationTests`. **۸۶۹/۸۶۹ سبز**، ۷ تصمیم باز جدید. جزئیات: `docs/phase-log.md` بخش «فاز ۱۳»؛ ریسک‌ها: `docs/open-decisions.md`.

- **2026-08-26** (ادامهٔ فاز ۱۲، commit `b224db2` push شده): **اولین کوئری Read-Only واقعی روی Oracle زنده** — `TYPEACTIVITY` قطعی حل شد (۱=بدهکار)، `ISAUTOMATIC` تأیید شد، `VAHEDTYPE` ناتمام ماند؛ ریسک جدید: ۳ حساب گروه با `TYPEACTIVITY ∈ {4,5,6}`. همچنین سه فایل ایجنت اصلاح شد (Discovery/Scaffold کامل و بسته است). جزئیات: `docs/phase-log.md` بخش «فاز ۱۲».

- **2026-08-26** (پاس چهارم تحلیل `D:\CentralAccount`، Read-Only، `backend/` دست‌نخورده): مرجع **per-Module** ساخته شد (بخش‌های ۲۱–۲۲؛ سند از ۱۴۰۵ به **۱۷۰۹ خط**) — به‌ازای هر Entity فهرست کامل Command/Query + قوانین. کشف‌ها: چندمستأجری فقط در ۱۲ از ۳۷۲ Handler، ورود سند از Kafka، شکل کامل تراز آزمایشی ۴/۶/۸ ستونه، `Cartable` به‌عنوان façade. جزئیات کامل: `docs/centralaccount-business-reference.md`.

- **2026-08-25/26** (پاس دوم و سوم تحلیل `D:\CentralAccount`): پوشش کل solution (سند از ~۴۰۰ به **۱۴۰۵ خط**). یافتهٔ اصلی: باگ `bool?` روی **۱۹ ستون در ۱۳ جدول**؛ به‌علاوه تصحیح دو نتیجه‌گیری پاس اول (تراز در مسیرهای خودکار enforce می‌شود؛ تغییرناپذیری سند تا حد زیادی enforce می‌شود)، مکانیزم «دورهٔ بسته»، کاتالوگ ۱۸ قانون «قابل حذف نیست»، و «صفر `PUT`» در هر ۴۲ Controller. جزئیات کامل: `docs/centralaccount-business-reference.md`.

- **2026-08-25** (پاس اول تحلیل `D:\CentralAccount`، به‌درخواست صریح صاحب پروژه): برای اولین بار **کل** solution (نه فقط `Entities/`) به‌صورت Read-Only خوانده شد. قطعی شد `TYPECODE` = `Group=1/Kol=2/Moin=3`، «الزامی بودن تفصیلی» از طریق **وجود ردیف** در `TB_ACCOUNT_LINK_LEVEL` مدل شده، الگوی مرجع حذف گرهٔ کدینگ، و مرزهای Aggregate ۶ جفت Head/Detail. جزئیات کامل: `docs/centralaccount-business-reference.md`؛ نقد معماری/کارایی + **backlog اولویت‌دار ۱۰ موردی برای ما**: `docs/centralaccount-improvement-opportunities.md`.

- **2026-08-25** (ادامهٔ همان جلسه، پیش از commit): پاس `/code-review` روی فاز ۱۱؛ ۱ باگ واقعی (عدم sync شدن `VAHEDCODE`/`YEAR` لینک تفصیلیِ مشترک) رفع شد، ۴۱۶ → **۴۱۷**؛ سه یافتهٔ دیگر مستند شد و عمداً رفع نشد. جزئیات: `docs/phase-log.md` بخش «فاز ۱۱».

- **2026-08-25** (فاز ۱۱، برنچ `EntityCRUD`، commit نشده): بستن دو ریسک باز فاز ۱۰ — نگاشت ORA-02291 → **400** در `UnitOfWork`، و مسیر نوشتن تفصیلی ردیف سند (`TafsiliLinks` اختیاری، غیر‌breaking). **۴۱۷/۴۱۷ سبز**؛ دو شکاف باقی‌مانده (سمت خواندن و composite create) حدس زده نشد. جزئیات: `docs/phase-log.md` بخش «فاز ۱۱».

- **2026-08-20** (فاز ۱۰، برنچ `EntityCRUD`، commit نشده): CRUD مستقل `TB_VOUCHERSDETAIL` (۵ Endpoint) + composite create سند با گزینهٔ «الف» (پارامتر اختیاری روی همان `CreateVoucherHeadCommand`). **۳۷۹/۳۷۹ سبز**؛ ۳ ریسک 🔴 جدید (تراز، نوع دادهٔ مبلغ، نقض FK → ۵۰۰ خام). جزئیات: `docs/phase-log.md` بخش «فاز ۱۰».

- **2026-08-20** (ادامهٔ همان جلسه): مستندسازی مرجع دامنه از پروژهٔ خارجی `Tamin.Core` — ۱۳۰ فایل Read-Only → `docs/tamin-core-entity-reference.md` (۶۳ Entity: ۴۲ مستقل / ۱۳ تعبیه‌شده / ۶ Head-Detail / ۲ مبهم). صفر تغییر در `D:\CentralAccount` (با `find -newermt` تأیید شد)؛ هیچ کدی تولید نشد. سه تصمیم باز جدید ثبت شد (`docs/open-decisions.md`).

- **2026-08-20** (ادامهٔ همان جلسه): تکمیل فاز ۹ — افزودن **سطح سوم** cascade (`TB_VOUCHERDETAIL_LINK_TAFSILI`)؛ متد به `SoftDeleteDetailTreeAsync` تغییر نام یافت. **۲۷۲/۲۷۲ سبز**؛ ریسک 🟡 «سند حذف‌شدهٔ پیش از فاز ۹ هرگز تمیز نمی‌شود» ثبت شد. جزئیات: `docs/phase-log.md` بخش «فاز ۹».

- **2026-08-20** (فاز ۹، برنچ `changejWT`، commit نشده): cascade حذف نرم سند → ردیف‌های سند، در همان `IVoucherHeadRepository` و با `load+mutate` (نه `ExecuteUpdateAsync`). **۲۶۶/۲۶۶ سبز** شامل اولین تست‌های واقعی repository روی SQLite in-memory. جزئیات: `docs/phase-log.md` بخش «فاز ۹».

- **2026-08-19** (ادامهٔ همان جلسه، پیش از commit): **حذف کامل `PUT`/`DELETE` از کل API به درخواست صریح صاحب پروژه** (در این محیط فقط `GET`/`POST` مجاز است) — هر ۴ Endpoint نوشتن به `POST /{id}/update` و `POST /{id}/delete` با پاسخ ۲۰۰ + `{id}` تبدیل و با `HttpVerbConventionTests` قفل شدند. **۲۵۵/۲۵۵ سبز**. جزئیات و دلایل: `docs/phase-log.md` بخش «فاز ۸».

- **2026-08-19** (فاز ۸، برنچ `changejWT`، commit نشده): تکمیل CRUD با Update (PUT-معنایی، نه PATCH) + Delete نرم؛ `NotFoundException` مرکزی → ۴۰۴؛ `Id` از route نه بدنه. **۲۵۰/۲۵۰ سبز**؛ ۶ تصمیم باز جدید ثبت و عمداً حل نشد. جزئیات: `docs/phase-log.md` بخش «فاز ۸»؛ ریسک‌ها: `docs/open-decisions.md`.

- **2026-08-19** (ادامهٔ همان جلسه): **جایگزینی کامل JWT محلی با IDP واقعی سازمان** — پکیج `Tamin.Framework.Common.Security` 1.0.9؛ API پکیج با reflection راستی‌آزمایی شد (نه حدس)، shaping خطای 401/403 به‌صورت **زنجیره‌ای** پیاده شد تا delegateهای پکیج نابود نشوند؛ زیرساخت محلی فیزیکاً حذف شد. **۱۷۰/۱۷۰ سبز**؛ تصمیم باز 🔴 «نگاشت claim برای `ADDUSERID`» ثبت شد. جزئیات: `docs/phase-log.md` بخش «فاز ۷ / ۳».

- **2026-08-19** (فاز ۷، برنچ `GetAccountCode`): حل هر دو ریسک 🔴 CRITICAL فاز ۶ — حذف کامل `AddUserId` از Commandها + `ICurrentUser` سمت سرور، و افزودن `UseAuthentication()` که اصلاً در pipeline نبود؛ به‌علاوه پورت `TokenManager` سازمان با **۶ نقص اصلاح‌شده**. جزئیات: `docs/phase-log.md` بخش «فاز ۷».

- **2026-08-18** (فاز ۶، برنچ `GetAccountCode`، commit نشده): اولین لایهٔ HTTP — ۶ Endpoint روی ۲ Controller، `GlobalExceptionHandler` مرکزی، و حل تصمیم باز «خطای UNIQUE خام» (ORA-00001 → ۴۰۹). **۱۳۱/۱۳۱ سبز**؛ Gate امنیتی ۲ یافتهٔ CRITICAL + ۴ ریسک 🟡 برگرداند. جزئیات: `docs/phase-log.md` بخش «فاز ۶».

- **2026-08-18** (همان برنچ `addAccountCode`): حل ریسک 🔴 `PARENTID DEFAULT '0'` — `HasDefaultValueSql` از Fluent Mapping حذف شد تا حساب ریشه واقعاً `NULL` بگیرد؛ DDL اوراکل دست‌نخورده. **۶۳/۶۳ سبز**. commit/push با تأیید صریح کاربر انجام شد. شرح کامل: `docs/open-decisions.md`.

- **2026-08-18** (فاز ۵، برنچ `addAccountCode`): اولین مسیر نوشتن CQRS واقعی — `IUnitOfWork` عمداً باریک، مرز تراکنش در Handler، Command فقط primitive. **۶۳/۶۳ سبز**؛ ریسک 🔴 `PARENTID` و دو ریسک 🟡 کشف شد. جزئیات: `docs/phase-log.md` بخش «فاز ۵».

- **2026-08-18** (همان برنچ `feature/legacy-entity-guid-ids`): مسطح‌سازی محل Entityها به `Accounting.Domain/Entity/` + namespace `Accounting.Domain.Entity` (با `git mv`، تاریخچه حفظ شد)؛ سمت Infrastructure عمداً دست‌نخورده. **۲۲/۲۲ سبز**. شرح: `CLAUDE.md` → «تصمیم معماری سوم».

- **2026-08-18** (برنچ `feature/legacy-entity-guid-ids`): تبدیل شناسه‌های Legacy از `string` به `Guid` — **۱۷۷ پراپرتی در ۶۴ Entity**، پس از verify کاملاً Read-Only روی **۱۲۴٬۰۹۴** مقدار زنده؛ ستون فیزیکی اوراکل تغییر نکرد. **۲۲/۲۲ سبز**؛ ریسک 🔴 «۹ ستون DEFAULT ناسازگار با Guid» کشف شد. شرح: `CLAUDE.md` بخش وضعیت + `docs/open-decisions.md`.

- **2026-08-17**: **حذف فیزیکی مدل Rich** به درخواست صریح کاربر (پس از commit اولیهٔ `9f760ad`) — ۲۲ فایل. **۱۲/۱۲ سبز** (از ۳۳). شرح کامل: `CLAUDE.md` → «تصمیم معماری دوم».

- **2026-08-17**: **تصمیم معماری دوم کاربر — «Legacy کاملاً جایگزین شود» (گزینهٔ ج)**، ابتدا با `[Obsolete]`. هم‌زمان ابهام «منبع حقیقت تفصیلی» حل شد (`TB_ACCOUNT_LINK_TAFSILGROUP`) و فایل‌های قانون `team-lead.md`/`accounting-domain.md` با تصمیم جدید هماهنگ شدند. **۳۳/۳۳ سبز**. شرح: `CLAUDE.md` → «تصمیم معماری دوم».

- **2026-08-17**: **تغییر معماری به Legacy-as-Domain** به درخواست صریح صاحب پروژه — ۵ فایل قانون ایجنت اصلاح و هر ۶۵ Entity به `Accounting.Domain` منتقل شد. **مدل Rich در این مرحله ادغام نشد** و تصمیم به کاربر escalate شد. **۳۳/۳۳ سبز**. شرح: `CLAUDE.md` → «تصمیم معماری: Legacy-as-Domain».

- **2026-08-17**: Reverse Engineering دیتابیس Legacy Oracle (schema `CENTRALACCOUNT`) — کشف Read-Only کامل (۶۵ جدول / ۷۷۴ ستون / ۸۲ FK / ۲۹ UNIQUE / ۱ sequence / ۲۸ View) و Scaffold هر ۶۵ جدول به Entity + Fluent Mapping (پکیج‌های `Oracle.EntityFrameworkCore` و `Microsoft.EntityFrameworkCore.Design` فقط به Infrastructure اضافه شد؛ Domain همچنان بدون هیچ پکیج). هیچ DML/DDL، هیچ secret. **۳۳/۳۳ سبز**.

- **2026-08-17**: طراحی و پیاده‌سازی مدل دامنهٔ کدینگ شناور (Entities/ValueObjects/Rules + سلسله‌مراتب Exception، بدون وابستگی خارجی) + تکمیل `docs/chart-of-accounts.md` + ساخت `Accounting.Domain.Tests` با **۳۳ تست** سبز. ⚠️ این مدل بعداً (همان تاریخ) با «تصمیم معماری دوم» کنار گذاشته و حذف شد.

- **2026-08-16**: راه‌اندازی اسکلت solution بک‌اند (Domain/Application/Infrastructure/Api روی net10.0) با رفرنس‌های Clean Architecture و Swagger فعال؛ هر سه endpoint `/swagger/index.html`، `/swagger/v1/swagger.json` و `/api/health` توسط `qa-tester` با کد ۲۰۰ تأیید شدند. مخزن git هنوز init نشده بود.

- **YYYY-MM-DD** (تاریخ ثبت‌نشده — placeholder اصلی فایل، عمداً دست‌نخورده): راه‌اندازی اولیه ریپازیتوری و تیم ایجنت‌ها.
