# پروژه: سیستم حسابداری (Accounting System)

این فایل در هر سشن و هر ساب‌ایجنت بارگذاری می‌شود — **عمداً کوتاه است. چیزی به آن اضافه نکن** مگر یک خط با ارجاع. جزئیات فازها → `docs/phase-log.md`، ریسک‌ها/تصمیم‌ها → `docs/open-decisions.md`.

## Stack و دستورها

- Backend: .NET 10، Clean Architecture + CQRS (MediatR)، FluentValidation، EF Core + Oracle (schema Legacy `CENTRALACCOUNT`)
- Frontend: **ریپوی جدا** `D:\AiProj\AccountCoreAiProj_UI` (Vite + React 19 + TS + MUI/RTL). پوشهٔ `frontend/` اینجا وجود ندارد و ساخته نمی‌شود.
- Build: `dotnet build backend/Accounting.sln --nologo -v q -clp:ErrorsOnly`
- Test (فقط پروژهٔ لمس‌شده): `dotnet test backend/tests/<Project> --nologo -v q --filter "FullyQualifiedName~<Class>"`
- **کل سوییت (~۳۰۰۰ تست) را اجرا نکن** مگر پیش از Release یا تغییر واقعاً سراسری.

## ساختار

```
backend/src/Accounting.Domain/Entity    # ۶۵ Entity، POCO خالص، namespace Accounting.Domain.Entity
backend/src/Accounting.Application      # Commands/Queries/Handlers/Validators
backend/src/Accounting.Infrastructure/Legacy  # LegacyDbContext + Fluent Mapping + GuidToChar36Converter
backend/src/Accounting.Api              # Controllers
backend/tests/*                         # xUnit؛ repository روی SQLite in-memory
```

## تصمیم‌های معماری تثبیت‌شده (خلافشان نرو)

1. **Legacy-as-Domain:** Entityهای جدول‌های Legacy خودِ مدل دامنه‌اند؛ لایهٔ ACL نداریم. `Accounting.Domain` صفر وابستگی خارجی دارد (EF/Oracle فقط در Infrastructure).
2. **مدل Rich حذف شد:** invariantهای تراز بدهکار/بستانکار، سلسله‌مراتب ثابت و… در کد دامنه نیستند. خودسرانه بازسازی‌شان نکن؛ اگر لازم شد از کاربر بپرس (محل درست: Application یا DB constraint).
3. **`PUT`/`DELETE` ممنوع:** الگو `POST {id}/update` و `POST {id}/delete` (حذف نرم با `ISDELETED`).
4. **`vahedCode`** هرگز از بدنه/query گرفته نمی‌شود؛ فقط هدر `X-Vahed-Code`، که `IUnitScopeResolver` در برابر زیردرخت مجاز کاربر اعتبارسنجی می‌کند (غیرمجاز → ۴۰۳؛ بدون هدر → واحد خود کاربر). `year` پارامتر query است.
5. **`NUMBER(1)` چندمقداری = enum** با `.HasConversion<int?>()` (گارد: `LegacyEnumMappingConventionTests`). `CHAR(36)` → `Guid` با `GuidToChar36Converter`.
6. **خطا فقط RFC 7807 ProblemDetails.** Entity لخت در پاسخ API برنمی‌گردد — DTO.
7. **پیش از ساخت CRUD برای هر `TB_XXX`** بخش مربوط در `docs/tamin-core-entity-reference.md` را چک کن (بخش ۲ = تعبیه‌شده، CRUD مستقل نساز؛ بخش ۴ = از کاربر بپرس).
8. **Discovery/Scaffold بسته است** — برای CRUD روی Entity موجود `database-reverse-engineer` را صدا نزن.
9. **سمت Read از View بخواند.** استثناهای ثبت‌شده فقط: تراز آزمایشی (فاز ۱۸)، مرور اسناد و دفتر روزنامه (فاز ۴۱ — Viewهایشان غلط‌اند، ریسک #۲۶). گزارش جدید اول View مناسب را چک کند.
10. Business Rule هرگز فقط در UI (مثلاً تفصیلی الزامی) — باید در Domain/Application هم باشد.

## ⚠️ قاعدهٔ خواندن مستندات (صرفه‌جویی توکن)

فایل‌های `docs/` خیلی بزرگ‌اند (`phase-log` ~۱۶۰k توکن، `centralaccount-business-reference` ~۱۰۰k، `open-decisions` ~۸۰k، `ROADMAP.md` ~۷۴k). **هرگز کامل نخوان.**
روش: `Grep` روی کلمهٔ کلیدی یا عنوان (`^## .*فاز ۳۵`) → فقط همان بخش با `offset`/`limit` (حداکثر ~۱۵۰ خط).
`docs/archive/` و `.claude/agents_old/` تاریخی‌اند — نخوان.

## تیم ایجنت‌ها (`.claude/agents/`)

| ایجنت | کار |
|---|---|
| `backend-dotnet` | Command/Query/Handler/API/Repository |
| `accounting-domain` | ابهام یا قانون کسب‌وکار |
| `api-contract` | DTO/Error Contract/Breaking change |
| `frontend-react` | UI (ریپوی جدا) |
| `qa-tester` | تست و Gate کیفیت |
| `security-reviewer` / `performance-reviewer` | فقط Gate پیش از Release یا Feature حساس |
| `database-reverse-engineer` | فقط SELECT روی اوراکل زنده |

سشن اصلی خودش هماهنگ‌کننده است (ساب‌ایجنت نمی‌تواند ساب‌ایجنت دیگری صدا بزند). برای کار چندلایه، اسکیل `/team-lead` را بزن. کار کوچک یک‌لایه را مستقیم به همان ایجنت بده.

## وضعیت فعلی

آخرین فاز: **۴۵-ز / تکمیل ح-۱..ح-۹** (۲۰۲۶-۱۰-۰۴) — ماژول صورت‌های مالی کامل (قالب تا داشبورد، بستن دوره، یادداشت توضیحی، نسبت‌ها، PDF مرورگر). طراحی و نقشه: `docs/fs-module.md` §۱۳. ⚠️ DDL `061` لازم است؛ انتشار بدون قفل دوره ممکن نیست (V-11). خزانه‌داری (۴۴) کامل است؛ قالب دیسکت بانک هنوز نیامده. DDL `044`–`061` فقط به دست صاحب پروژه. قالب صورت مشترک یا اختصاصی واحد (`docs/fs-module.md` §۸). اسکیمای اوراکل از کانفیگ `Database:Schema` (Development: `AICENTRALACCOUNT`). ⚠️ روی Oracle از `AnyAsync` استفاده نکن (`ORA-00904`). **۲۹۹۰ تست** (تست‌های خزانه نوشته نشده‌اند).
فهرست کامل فازها و جزئیات: `docs/phase-log.md`.

## ریسک‌های باز 🔴 (فقط عنوان — جزئیات در `docs/open-decisions.md`)

۲-الف گارد وابستگی حذف در ۱۲ Entity · ۲-ب نُه ستون ورودی آزاد که باید سمت سرور تولید شوند · ۲-ج CheckBook بدون برگ چک · ۲-د ردیف زندهٔ `TYPEACTION=5` · ۳ تضمین تراز ندارد · ۴ نوع دادهٔ مبلغ (`long` vs `decimal?`) · ۵ گذار وضعیت سند (نیمه‌حل) · ۶ و ۷ حذف کدینگ/`LevelTafsil`/`TafsilGroup` بدون بررسی وابستگی · ۸ `TB_VAHED_INFO` بدون Audit · ۹ و ۱۴ ستون‌های شناسهٔ بدون FK · ۱۰ claim `ADDUSERID` با IDP واقعی تأیید نشده · ۱۱ `sys_guid()` ناسازگار با Guid · ۱۳ دادهٔ Legacy متناقض با Validator مرجع · ۱۵ حذف Head بدون cascade به Detail · ۱۶ `TmpVoucherHead` خالی · ۱۷ معنای «مانده اول دوره» · ۱۹ CORS ندارد · ۲۰ JWT در localStorage · ۲۱ ذخیرهٔ سند فرانت غیراتمیک · ۲۲ SSO روی IDP واقعی اجرا نشده (پورت dev ثابت ۴۲۰۰) · ۲۳ یکتایی `LEVEL_CODE` و سقف ۷ سطح سمت سرور نیست · ۲۴ ستون‌های `byte` بالقوه سرریز · ۲۵ تست‌ها روی SQLite به باگ Oracle نابینا · ۲۶ دو View غلط اوراکل · ۲۷ ماتریس دسترسی کدینگ اعمال نمی‌شود · ۲۸ تنخواه ماژول جدا از `TB_REVOLVING_FUND` · ۲۹ بررسی‌کنندهٔ تنخواه مستقل از RBAC مرکزی، SoD کامل ندارد.

پیش از تصمیم روی هرکدام، **فقط همان بخش** `docs/open-decisions.md` را با Grep پیدا کن و بخوان.

## پایان جلسه (فقط وقتی وضعیت پروژه عوض شد)

- این فایل: فقط خط «آخرین فاز» را عوض کن.
- جزئیات فاز → بالای `docs/phase-log.md` (فقط ۳۰ خط اول را بخوان تا جای درج را پیدا کنی).
- `docs/progress-log.md`: یک خط.
- ریسک جدید → `docs/open-decisions.md` + یک عنوان در بالا.
- GitHub Project board (https://github.com/users/mehdijanfeshar/projects/2) با `gh`؛ `ROADMAP.md` را فقط با Grep روی ردیف همان فاز ویرایش کن، نه خواندن کامل.
- commit با تأیید کاربر؛ **push هرگز بدون درخواست جدا.**
