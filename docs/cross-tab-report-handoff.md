# نقطهٔ ادامه — گزارش متقاطع (فاز ۴۳، ناتمام)

> **چرا این فایل هست:** کار در ۲۰۲۶-۰۹-۲۳ به‌درخواست صاحب پروژه (اتمام سهمیهٔ توکن) وسط راه متوقف شد.
> **بک‌اند کامل و سبز است؛ فرانت اصلاً شروع نشده.** این فایل دقیقاً می‌گوید از کجا ادامه بدهی.
> پس از تکمیل، این فایل حذف شود و خلاصه‌اش به `docs/phase-log.md` برود.

## درخواست اصلی

صاحب پروژه ویدیوی «گزارشات ماتریسی» نرم‌افزار ERP برهان سیستم پاسارگاد را فرستاد
(<https://www.aparat.com/v/KhZ4f>) و گفت: **«یه گزارش جدیدش کن، به قبلی دست نزن.»**

ویدیو دانلود و فریم‌هایش رندر و دیده شد. آنچه در آن سیستم هست:

- **شبکه:** ستون‌های ثابت `کد تفصیل۱ | عنوان تفصیل۱`، سپس سرستون‌های گروهی که هرکدام یک **معین**
  است، و زیر هر معین دو زیرستون **بدهکار | بستانکار**.
- **تب «فیلتر پیشرفته»:** دو پنل کاملاً قرینه، **«فیلتر سطر»** و **«فیلتر ستون»**، هرکدام با همان
  مجموعه کنترل (کاربر، گروه، کل، معین، ارز، نوع سند، پیگیری + جدول تفصیل۱/۲/۳ با چک‌باکس «نمایش»).
- در تب «فیلتر عمومی» گزینهٔ **«ستون های نمایشی: بدهکار،بستانکار»**.

یعنی یک **pivot/cross-tab builder**: کاربر خودش انتخاب می‌کند چه چیزی سطر و چه چیزی ستون باشد.

⚠️ **این با «گزارش ماتریسی» موجود ما بنیاداً فرق دارد** و جایگزینش نیست — آن یکی روی *یک* سطح
گروه‌بندی می‌کند و خروجی‌اش لیست است؛ این یکی دو بُعد را متقاطع می‌کند و خروجی‌اش شبکه است. هر دو
باید بمانند. (نقد قبلی صاحب پروژه — «عملا فرقی با تراز ندارد» — ریشه‌اش همین بود.)

## ✅ بک‌اند — کامل، سبز، commit‌نشده

**۲۹۹۰ تست سبز** (۳۷ Domain + ۲۴۳۰ Application + ۱۵۱ Api + ۳۷۲ Infrastructure).

### یافتهٔ کلیدی: قانون کاری #۲ با ساختار برآورده می‌شود

`VW_CONSOLIDATE_REPORT` از قبل یک ردیف کاملاً تخت‌شده است: `GROUPCODE/NAME`, `KOLCODE/NAME`,
`MOINCODE/NAME`, **`TAFSILICODE1..7` / `TAFSILINAME1..7`**, `DEBTOR`, `CREDITOR`, `YEAR`,
`VAHEDCODE`, `ISDELETED`, `DOCLIFE`, `VOUCHERDATE`, `SYSID`.

یعنی **هر بُعدی که گزارش روی محور می‌گذارد از قبل یک ستون همان View است** → کل pivot یک `GROUP BY`
روی read model است، بدون هیچ join و بدون SQL خام. **پس استثنای جدیدی بر قانون #۲ لازم نیست** و دو
استثنای ثبت‌شده (فاز ۱۸ و ۴۱) نباید به‌عنوان مجوز اینجا استناد شوند.

روی اوراکل زنده تست شد: pivot تفصیل۱×معین در **۱۳۸ms**.

### فایل‌های ساخته‌شده

```
backend/src/Accounting.Application/Reports/CrossTab/
  CrossTabDimension.cs                       enum ۱۰ مقداری (گروه/کل/معین/تفصیلی۱..۷)
  CrossTabResultDto.cs                       Column/Cell/Row/Result DTOs
  GetCrossTabReport/GetCrossTabReportQuery.cs         IVahedScopedQuery
  GetCrossTabReport/GetCrossTabReportQueryHandler.cs
  GetCrossTabReport/GetCrossTabReportQueryValidator.cs
backend/src/Accounting.Application/Common/Interfaces/ICrossTabReportReadRepository.cs
backend/src/Accounting.Infrastructure/Repositories/CrossTabReportReadRepository.cs
backend/tests/Accounting.Application.Tests/Reports/CrossTab/  (۲ فایل تست)
backend/tests/Accounting.Infrastructure.Tests/Repositories/CrossTabReportReadRepositoryTests.cs
```

تغییر در فایل‌های موجود (هیچ‌کدام رفتار گزارش قبلی را عوض نمی‌کند):
- `Accounting.Infrastructure/DependencyInjection.cs` — ثبت ریپازیتوری جدید (۲ خط)
- `Accounting.Api/Controllers/TrialBalanceReportsController.cs` — اکشن جدید `GET api/reports/cross-tab`

### قرارداد Endpoint

```
GET /api/reports/cross-tab
    ?year=1404                       (الزامی، ۴ رقم)
    &rowDimension=Tafsili1           (پیش‌فرض)
    &columnDimension=Moin            (پیش‌فرض)
    &fromDate=&toDate=               (YYYYMMDD شمسی)
    &docLife=&systemTypeId=
    &rowCodeFilter=&columnCodeFilter=   («شروع با» روی کد هر محور)
```

پاسخ `CrossTabResultDto`: `Columns[]` (کد، نام، جمع ستون)، `Rows[]` (کد، نام، `Cells[]` فقط
تقاطع‌های پرشده، جمع ردیف)، جمع کل، `TotalColumnCount`، `ColumnsTruncated`.

### تصمیم‌های طراحی که مستند شده‌اند و نباید دوباره کشف شوند

1. **`CrossTabDimension` عمداً enum جداست، نه `MatrixReportLevel`.** آنجا «در چه سطحی تجمیع کنم»
   است و یکی بیشتر نیست؛ اینجا «روی این محور چه بگذارم» است و دو انتخاب مستقل دارد.
2. **سلول‌های خالی فرستاده نمی‌شوند.** cross-tab تقریباً همیشه sparse است (روی دادهٔ زنده ۸ سطر ×
   ۸ ستون فقط ۱۵ سلول پر داشت).
3. **ردیفی که روی هر یک از دو محور کد ندارد حذف می‌شود، نه گروه‌بندی زیر کلید خالی.** با دو محور،
   یک `null` فیلترنشده هم یک سطر خالی می‌سازد و هم یک ستون خالی.
4. **سقف ستون ۱۲۰ تاست** (`MaxColumns`). هنگام بریدن، **پرگردش‌ترین ستون‌ها** نگه داشته می‌شوند نه
   اولین‌ها به ترتیب کد، و بعد دوباره به ترتیب کد چیده می‌شوند.
5. ⚠️ **وقتی بریدن رخ دهد، جمع کل همچنان کل مجموعهٔ فیلترشده را پوشش می‌دهد** — یعنی جمع سطر/ستونِ
   دیده‌شده با جمع کل نمی‌خواند. این عمدی است: rebase کردن جمع روی برش دیده‌شده، گزارشِ بریده را
   کامل جلوه می‌داد. **فرانت موظف است این را روی صفحه بگوید** (`ColumnsTruncated`).
6. **دو محور نمی‌توانند یکی باشند** → ۴۰۰.
7. projection با `Expression` ساخته می‌شود (نه ۹۰ حالت دستی)؛ نام ستون‌ها از switch ثابت روی enum
   می‌آید، **هرگز از ورودی کاربر** → هیچ راهی برای تزریق نیست.
8. `ISDELETED` به‌صورت **عدد** مقایسه می‌شود نه bool — تلهٔ `ORA-00904` فازهای ۳۷/۴۰.

## ⬜ فرانت — شروع نشده

ریپو: `D:\AiProj\AccountCoreAiProj_UI`

کارهای لازم:

1. `src/types/crossTab.ts` — آینهٔ DTOها + enum `CrossTabDimension` با برچسب فارسی.
2. `src/features/reports/cross-tab/api.ts` — فراخوان `GET /api/reports/cross-tab`.
3. `src/features/reports/cross-tab/CrossTabReportPage.tsx`:
   - دو `Select` برای بُعد سطر و بُعد ستون (با جلوگیری از انتخاب یکسان در UI، ضمن اینکه سرور هم ۴۰۰ می‌دهد)
   - فیلترهای سال/تاریخ/وضعیت سند + دو فیلد «شروع با» برای سطر و ستون
   - **جدول با سرستون دوسطحی**: هر ستون = یک بُعد، زیرش دو زیرستون بدهکار/بستانکار
   - ستون‌های ثابت سمت راست (کد و نام سطر) با `position: sticky` — در RTL از `insetInlineStart/End`
     استفاده شود نه `left/right` (تلهٔ `stylis-plugin-rtl`؛ رجوع به `TreeRow.tsx` در
     `features/coding-permissions/`)
   - **هشدار صریح وقتی `columnsTruncated` است**، با ذکر اینکه جمع کل کل داده را پوشش می‌دهد ولی
     ستون‌های دیده‌شده نه
   - `BalanceBar` از `features/reports/_shared/` قابل استفاده است
4. route `/reports/cross-tab` در `src/app/routes.tsx` + آیتم منو در `src/lib/navConfig.tsx`
   (کنار «گزارش ماتریسی»، با نامی که این دو را از هم جدا کند — پیشنهاد: «گزارش متقاطع»)
5. خروجی اکسل/چاپ — ⚠️ چون تعداد ستون‌ها **متغیر** است، `export.ts` و `printStyles.ts` گزارش‌های
   دیگر مستقیماً قابل کپی نیستند.

## کارهای باز دیگر (خارج از این گزارش)

- **`stdoutLogEnabled="true"`** هنوز روی `artifacts/api/web.config` و `D:\AiProj\Publish\web.config`
  روشن است. بعد از اطمینان از پایداری، به `false` برگردد.
- **`D:\AiProj\Publish\web.config` و `appsettings.Development.json`** دستی اصلاح شده‌اند
  (`ASPNETCORE_ENVIRONMENT=Development` + connection string واقعی). **با اولین publish بعدی پاک
  می‌شوند.** راه دائمی: متغیر محیطی روی app pool در IIS.
- **`vite.config.ts` یک IP مشخص ماشین دارد** (`http://172.16.15.65:8090`) که روی مخزن مشترک رفته؛
  بهتر است به `.env.local` منتقل شود.
- مستندات فاز ۴۳ (`CLAUDE.md` / `ROADMAP.md` / `phase-log.md` / `progress-log.md`) هنوز به‌روز
  **نشده‌اند** — عمداً، تا کار تمام شود.
