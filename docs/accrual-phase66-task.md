# دستور کار فاز ۶۶ — حسابداری تعهدی (بک‌اند)

> **نحوهٔ اجرا در VS Code:** در Claude Code بنویس:
> `/team-lead فاز ۶۶ را طبق docs/accrual-phase66-task.md اجرا کن`
>
> طرح کامل (برای ابهام، نه برای خواندن کامل): سند «حسابداری تعهدی — نسخه ۲.۰»، بخش‌های ۰ و ۶.
> این فایل تمام چیزی است که برای اجرا لازم است.

## هدف

سه قابلیت تعهدی که امروز نیست، **با کمترین تغییر و با استفاده از کد موجود**:

1. **برگشت خودکار برآورد** — سندی که با الگوی «برگشت خودکار» ثبت شد، روز اول ماه بعد خودکار معکوس شود.
2. **زمان‌بندی پیش‌پرداخت و درآمد انتقالی** — مبلغ روی N ماه تقسیم شود و هر ماه سند سهم همان ماه ساخته شود.
3. **کنترل صورتحساب ماه** — ماهی که سطر زمان‌بندی اجرانشده دارد، صورتحساب نشود.

گزارش ماندهٔ باز و UI در این فاز **نیستند** (فاز ۶۷).

## تصمیم‌های گرفته‌شده (دوباره نپرس)

- **فقط یک جدول جدید:** `TB_AC_SCHEDULE`. هیچ جدول دیگری برای رویداد، بدهی، تسویه، تخصیص یا Outbox ساخته نمی‌شود.
- **قاعدهٔ سند تعهدی = الگوی عملیات حسابیار** (`OperationTemplate`). موتور قاعدهٔ جدید نساز.
- **همهٔ سندها از مسیر سند خودکار موجود** (همان که حسابیار، فروش و خزانه استفاده می‌کنند: `GetNextDocNumAsync`، `IVoucherTafsiliLevelGuard`، `DOCLIFE=Temporary`، `ISAUTOMATIC=1`).
- **برگشت = همان منطق `POST api/voucher-heads/{id}/reverse`** (فاز ۳۹، `PARENTHEAD_ID`). منطق تکراری ننویس.
- سند و تغییر وضعیت سطر زمان‌بندی **در یک تراکنش صریح**، مثل `ExecutePaymentRequestCommandHandler`.
- وضعیت سند تعهدی خودکار: «موقت»، مثل بقیهٔ اسناد خودکار.
- ماژول RBAC: `Accrual`. ثبت‌کردن زمان‌بندی دستی و اجرای دستی فقط برای کاربر واحد.

## گام ۰ — کشف (ارزان، قبل از هر کد)

با Grep/Explore فقط این‌ها را پیدا کن و نام دقیقشان را در گزارش بیاور. **از روی این سند حدس نزن.**

1. سرویس یا کلاسی که سند خودکار می‌نویسد (در `docs/agent-ux.md` به اسم `IVoucherWriter` آمده؛ امضای واقعی؟).
2. Handler معکوس سند (`reverse`) و اینکه منطقش قابل فراخوانی از Command دیگر هست یا داخل Handler است.
3. Handler صورتحساب ماه (فاز ۵۲، `TB_MONTH_CLOSE_LOG`) و جای پیش‌بررسی واحدها.
4. Handler اجرای الگوی حسابیار (Execute) و نقطه‌ای که `VoucherId` بعد از ثبت معلوم می‌شود.
5. یک `BackgroundService`/`IHostedService` موجود در پروژه، اگر هست (الگوی میزبانی روی IIS).
6. نگاشت `TB_OP_TEMPLATE` در Infrastructure (`OperationTemplates`).

اگر مورد ۱ یا ۲ با فرض این سند جور نبود، **متوقف شو و به من گزارش بده.**

## گام ۱ — DDL و DML (`accounting-domain` بررسی، `backend-dotnet` نوشتن)

فایل `backend/db/090_accrual_schedule.sql` با سرآیند و هشدار «DO NOT RUN… WITHOUT EXPLICIT PROJECT-OWNER APPROVAL» مثل `080_sales.sql`. انواع ستون‌ها را با `080` و `067` هماهنگ کن (`VAHEDCODE VARCHAR2(4)`، `ADDUSERID VARCHAR2(10)`، `TIMESTAMP`، `NUMBER(18)`):

```sql
CREATE TABLE TB_AC_SCHEDULE
(
    ID                  CHAR(36)        NOT NULL,
    VAHEDCODE           VARCHAR2(4)     NOT NULL,
    YEAR                VARCHAR2(4)     NOT NULL,
    SOURCE_VOUCHER_ID   CHAR(36)        NOT NULL,   -- TB_VOUCHERSHEAD.ID سند اصلی
    EXECUTION_ID        CHAR(36),                   -- TB_OP_EXECUTION.ID اگر از حسابیار آمده
    KIND                NUMBER(1)       NOT NULL,   -- 1 برگشت برآورد، 2 پیش‌پرداخت، 3 درآمد انتقالی
    LINE_NO             NUMBER(3)       NOT NULL,
    DUE_DATE            VARCHAR2(8)     NOT NULL,   -- شمسی yyyyMMdd
    AMOUNT              NUMBER(18)      NOT NULL,
    DEBIT_ACCOUNT_ID    CHAR(36),                   -- برای 2 و 3
    CREDIT_ACCOUNT_ID   CHAR(36),
    TAFSILI_JSON        CLOB,                       -- [{level, tafsiliId}] برای هر طرف
    DESCRIPTION         VARCHAR2(500),
    STATE               NUMBER(1)       DEFAULT 1 NOT NULL,  -- 1 باز، 2 اجراشده، 3 لغو
    RESULT_VOUCHER_ID   CHAR(36),
    ISDELETED           NUMBER(1)       DEFAULT 0 NOT NULL,
    ADDUSERID           VARCHAR2(10)    NOT NULL,
    CREATEDDATE         TIMESTAMP       NOT NULL,
    CHANGEUSERID        VARCHAR2(10),
    UPDATEDDATE         TIMESTAMP,
    CONSTRAINT PK_AC_SCHEDULE PRIMARY KEY (ID),
    CONSTRAINT UK_AC_SCHEDULE_LINE UNIQUE (SOURCE_VOUCHER_ID, KIND, LINE_NO),
    CONSTRAINT CK_AC_SCHEDULE_KIND CHECK (KIND IN (1, 2, 3)),
    CONSTRAINT CK_AC_SCHEDULE_STATE CHECK (STATE IN (1, 2, 3)),
    CONSTRAINT CK_AC_SCHEDULE_AMT CHECK (AMOUNT > 0)
);
CREATE INDEX IX_AC_SCHEDULE_DUE ON TB_AC_SCHEDULE (VAHEDCODE, STATE, DUE_DATE);

ALTER TABLE TB_OP_TEMPLATE ADD (
    AUTO_REVERSE    NUMBER(1) DEFAULT 0 NOT NULL,   -- سند این الگو اول ماه بعد برگردد
    SCHEDULE_KIND   NUMBER(1),                      -- null | 2 پیش‌پرداخت | 3 انتقالی
    CONSTRAINT CK_OP_TEMPLATE_SCHED CHECK (SCHEDULE_KIND IS NULL OR SCHEDULE_KIND IN (2, 3))
);
```

فایل `backend/db/091_accrual_seed.sql` (DML): مقدار جدید منوی نقش/ماژول `Accrual` به همان شکلی که `075_role_menu_access.sql` انجام داده. **معین‌های کدینگ را در این اسکریپت نساز** — کدینگ را صاحب پروژه از صفحهٔ کدینگ تعریف می‌کند.

## گام ۲ — Domain

- `Accounting.Domain/Accrual/AccrualSchedule.cs` (POCO، مثل `Sales/SalesEntities.cs`) + enumهای `AccrualScheduleKind { Reversal=1, Prepaid=2, Deferred=3 }` و `AccrualScheduleState { Open=1, Done=2, Cancelled=3 }`.
- `OperationTemplate`: دو property `AutoReverse` (bool) و `ScheduleKind` (enum nullable).
- `TemplateCategory`: مقدار `Accrual = 7`.
- قاعدهٔ تقسیم مبلغ (ریال صحیح، باقیماندهٔ گرد روی **آخرین** سطر، جمع سطرها = مبلغ اصلی) در یک کلاس خالص و قابل تست، مثل `AccrualScheduleSplitter`.

## گام ۳ — Application و Infrastructure

1. **سرویس مشترک معکوس:** منطق Handler معکوس فعلی را به سرویسی مثل `IVoucherReverser.ReverseAsync(sourceId, docDate)` استخراج کن. Endpoint فعلی همین سرویس را صدا بزند و **رفتارش عوض نشود** (تست‌های فعلی فاز ۳۹ باید سبز بمانند). تفاوت تنها: وقتی از زمان‌بندی صدا زده می‌شود، تاریخ سند = `DUE_DATE`، نه امروز.
2. **ثبت زمان‌بندی هنگام اجرای الگو:** در Execute حسابیار، اگر الگو `AutoReverse` دارد ⇒ یک سطر `KIND=1` با `DUE_DATE` = روز اول ماه بعدِ تاریخ سند. اگر `ScheduleKind` دارد ⇒ N سطر (N و تاریخ شروع از پارامترهای الگو؛ نام پارامترها را با `accounting-domain` تعیین کن). **در همان تراکنش ثبت سند.**
3. **`RunAccrualScheduleLineCommand(scheduleId)`:** تراکنش صریح ⇒ قفل سطر (`FOR UPDATE`) ⇒ اگر `STATE != Open` همان `RESULT_VOUCHER_ID` را برگردان (idempotent) ⇒ برای `KIND=1` سرویس معکوس، برای ۲ و ۳ ساخت سند از طریق نویسندهٔ سند خودکار ⇒ `STATE=Done` و `RESULT_VOUCHER_ID` ⇒ commit.
4. **`CancelAccrualScheduleCommand`:** فقط سطر باز؛ برای فسخ قرارداد پیش‌پرداخت.
5. **Job روزانه:** `BackgroundService` که سطرهای `STATE=1 AND DUE_DATE <= امروز` را با `FOR UPDATE SKIP LOCKED` در دسته‌های کوچک برمی‌دارد و برای هر کدام Command بالا را اجرا می‌کند. خطای یک سطر نباید بقیه را متوقف کند؛ لاگ شود. قابل خاموش‌شدن با کانفیگ `Accrual:Job:Enabled`.
6. **Queries:** فهرست زمان‌بندی‌ها (فیلتر وضعیت/نوع/بازهٔ سررسید)، جزئیات یک سند اصلی با سطرهایش.
7. **صورتحساب ماه:** در پیش‌بررسی هر واحد، اگر سطر باز با `DUE_DATE` داخل همان ماه یا قبل از پایانش هست ⇒ واحد «مشکل‌دار» با `REASON` خوانا («۳ برگشت برآورد اجرانشده»).

⚠️ روی Oracle از `AnyAsync` استفاده نکن (`ORA-00904`). از `CountAsync() > 0` یا الگوی موجود پروژه استفاده کن.

## گام ۴ — API

`AccrualController` با `api/accrual`، همه `IVahedScoped`، فقط `GET`/`POST`:

```
GET  schedules?state=&kind=&from=&to=
GET  schedules/by-voucher/{voucherId}
POST schedules/{id}/run
POST schedules/{id}/cancel     { reason }
```

خطاها ProblemDetails با پیام فارسی قابل اقدام. `api-contract` را برای DTOها خبر کن.

## گام ۵ — تست (`qa-tester`، فقط پروژه‌های لمس‌شده)

- `AccrualScheduleSplitter`: ۱۲۰٬۰۰۰٬۰۰۰ روی ۱۲ ماه؛ ۱۰۰٬۰۰۰٬۰۰۱ روی ۳ ماه (باقیمانده روی آخرین)؛ جمع همیشه = اصل.
- Run: اجرای دوباره همان سطر سند دوم نمی‌سازد؛ سطر لغوشده اجرا نمی‌شود؛ سند برگشت `PARENTHEAD_ID` به سند اصلی دارد و تاریخش `DUE_DATE` است.
- Execute الگو با `AutoReverse`: دقیقاً یک سطر `KIND=1` با تاریخ اول ماه بعد؛ الگوی بدون آن هیچ سطری نمی‌سازد.
- معکوس: تست‌های فعلی فاز ۳۹ بدون تغییر سبز.
- صورتحساب ماه: واحد با سطر باز رد می‌شود و `REASON` دارد.
- ⚠️ تست‌ها روی SQLite‌اند (ریسک #۲۵) و قفل را نمی‌بینند. در گزارش پایانی یک چک‌لیست کوتاه تست دستی روی Oracle بگذار (دو اجرای هم‌زمان Job).

## ممنوعیت‌ها

- جدول جدید غیر از `TB_AC_SCHEDULE` نساز.
- `PUT`/`DELETE` نساز. `vahedCode` را از بدنه یا query نگیر.
- رفتار endpoint معکوس فعلی را عوض نکن.
- DDL را روی هیچ دیتابیسی اجرا نکن.
- کل سوییت (~۴۵۰۰ تست) را اجرا نکن.
- `docs/` را کامل نخوان؛ فقط Grep.

## Done

Build موفق + تست‌های بالا سبز + گزارش ≤۳۰ خط شامل: نام‌های واقعی گام ۰، فایل‌های تغییرکرده، DDLها، چک‌لیست تست دستی Oracle، سؤال‌های باز. سپس «پایان جلسه» طبق `CLAUDE.md` (خط «آخرین فاز»، `docs/phase-log.md`، `docs/progress-log.md`). **commit فقط با تأیید من، push هرگز.**

Accounting Safety Gate مرتبط برای یادآوری در یک خط: تراز سند در کد نیست (ریسک #۳)؛ سند برگشت/سهم ماهانه را پیش از ذخیره خودت تراز چک کن.
