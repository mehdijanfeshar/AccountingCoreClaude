# فرصت‌های بهبود — نقد معماری/کارایی پروژهٔ `D:\CentralAccount`

> **تاریخ:** ۲۰۲۶-۰۸-۲۵
> **هدف:** این سند **برای اصلاح `D:\CentralAccount` نیست** (به آن دست نمی‌زنیم). این فهرست backlogی است از ضعف‌هایی که در سیستم قدیمی دیده‌ام، تا هنگام پیاده‌سازی معادلشان در `AccountingCoreClaude` **از همان ابتدا** بهتر طراحی کنیم.
> **همراه این سند:** `docs/centralaccount-business-reference.md` (منطق کسب‌وکار استخراج‌شده).
> **قید:** هر مورد با مسیر فایل + شمارهٔ خط پشتیبانی می‌شود. هرجا مطمئن نبودم، «❓ حدس نزدم» نوشته‌ام.

## راهنمای اولویت

| نشان | معنی |
|---|---|
| 🔴 | ضعف جدی؛ اگر تکرارش کنیم، بعداً گران تمام می‌شود |
| 🟡 | بهبود ارزشمند ولی غیرحیاتی |
| 🟢 | **الگوی خوبِ آن‌ها که باید کپی کنیم** (نقد فقط منفی نیست) |

---

# الف) مسائل کارایی (Performance)

## 🔴 A-1. Sync-over-async گسترده — `.Result` روی Task

**۷۱ مورد** فراخوانی مسدودکنندهٔ `.Result` روی `Task` در `ApplicationUseCases` و `Infrastructure.Persistance.EF`.

نمونه‌های قطعی:

| فایل | خط | کد |
|---|---|---|
| `ApplicationUseCases\Commands\Vouchers\Voucher\Create\AddVoucherCommandHandler.cs` | ۱۰۲ | `var defaultSysId = _unitOfWork.tbSysTypeRepository.GetAllSysType().Result.FirstOrDefault(a => a.Code == "1").Id;` |
| همان | ۱۷۹ | `var levList = _unitOfWork.tbLevelTafsilRepository.GetAllTbLevelTafsilAsync().Result;` |
| `ApplicationUseCases\Commands\Vouchers\VoucherHeads\Delete\DeleteVouchersHeadCommandHandler.cs` | ۱۹ | `var document = _unitOfWork.vouchersHeadRepository.GetByIdAsync(request.Id).Result;` |
| `ApplicationUseCases\Queries\AccountCodes\GetMoinByIdForEdit\GetMoinCodeByIdQueryHandler.cs` | ۲۳ | `... _unitOfWork.accountCodeRepository.GetByIdAsync(entity.ParentId.Value).Result;` |
| `ApplicationUseCases\Commands\PayAndRecive\PayAndreciveDetails\DeleteDetailSingle\DeletePayReciveDetailSingleCommandHandler.cs` | ۲۰ | `... GetByIdAsync(ent.PayRecivHeadId).Result.PayReciveDetails.Count();` |
| `ApplicationUseCases\Common\BusinessUserAcceess\BusinessUserAccess.cs` | ۳۵ | `... GetAllVahedInfoByParentAsync(unitCode).Result;` |

**چرا مهم است:** هر `.Result` یک thread از ThreadPool را تا پایان I/O دیتابیس بلوکه می‌کند. زیر بار همزمان این باعث thread-pool starvation می‌شود — یعنی throughput به‌شدت افت می‌کند بدون اینکه CPU یا DB اشباع باشد. در ASP.NET Core خطر deadlock کمتر است ولی افت مقیاس‌پذیری قطعی است.

**نکتهٔ ظریف‌تر:** بعضی از این‌ها داخل متدهای `private` **غیر‌async** هستند (مثل `CreateVoucherDetail` در خط ۱۷۷ که `levList` را با `.Result` می‌گیرد) — یعنی رفعش نیازمند async کردن زنجیرهٔ فراخوانی است، نه فقط افزودن یک `await`.

**درس برای ما:** ✅ ما این مشکل را **نداریم** (همهٔ مسیرهای ما async خالص‌اند). **قاعدهٔ پیشنهادی:** یک تست/analyzer اضافه کنیم که `.Result` و `.Wait()` را در `Accounting.Application` و `Accounting.Infrastructure` ممنوع کند — درست مثل `HttpVerbConventionTests` و `NoIndependentLinkTableWritePathTests` که قبلاً ساختیم.

---

## 🔴 A-2. `IUnitOfWork` با **۶۳ Repository**، همه **eagerly** در سازنده ساخته می‌شوند

**فایل‌ها:**
- `ApplicationUseCases\Abstractions\IUnitOfWork.cs` — ۱۲۰ خط، **۶۳ پراپرتی `{ get; }`**
- `Infrastructure.Persistance.EF\UnitOfWork.cs` — ۲۲۰ خط

سازندهٔ `UnitOfWork` هر ۶۳ repository را **بدون استثنا** از `IServiceProvider` می‌گیرد (خطوط ~۱۲۰ تا ۱۸۷):

```csharp
rabetTypeRepository = _serviceProvider.GetRequiredService<IRabetTypeRepository>();
elamDrmdWebService = _serviceProvider.GetRequiredService<IElamDrmdWebService>();
rabetClosingRepository = _serviceProvider.GetRequiredService<IRabetClosingRepository>();
accountExceptionRepository = _serviceProvider.GetRequiredService<IAccountExceptionRepository>();
workShopRepository = _serviceProvider.GetRequiredService<IWorkShopRepository>();
personActionRepository = _serviceProvider.GetRequiredService<IPersonActionRepository>();
IdentityFixItemRepository = _serviceProvider.GetRequiredService<IIdentityFixItemRepository>();
identityDetailRepository = _serviceProvider.GetRequiredService<IIdentityDetailRepository>();
```
*(`Infrastructure.Persistance.EF\UnitOfWork.cs:180-187` — انتهای همان بلوک)*

**سه مشکل هم‌زمان:**

1. **کارایی:** هر درخواست HTTP که به `IUnitOfWork` دست بزند — یعنی **تقریباً همه** — ۶۳ شیء را نمونه‌سازی می‌کند، حتی اگر فقط یکی لازم باشد. `AddGroupCodeCommandHandler` که فقط `accountCodeRepository` می‌خواهد، هزینهٔ کامل ۶۳ تا را می‌پردازد.
2. **Service Locator (ضدالگو):** `_serviceProvider.GetRequiredService<T>()` وابستگی‌ها را از سازنده پنهان می‌کند. کامپایلر دیگر نمی‌تواند وابستگی گمشده را بگیرد و خطا به runtime منتقل می‌شود.
3. **Coupling بیش‌ازحد:** هر Handler که `IUnitOfWork` تزریق می‌کند، از نظر تایپ به **همهٔ** ۶۳ repository وابسته است. تست‌نویسی سخت می‌شود (باید یک mock غول‌پیکر ساخت) و تغییر در هر repository همهٔ Handlerها را بازکامپایل می‌کند.

**درس برای ما:** ✅ **معماری فعلی ما از قبل درست است و باید محافظت شود.** طبق تصمیم فاز ۵ ما: «`IUnitOfWork` عمداً باریک و entity-agnostic است — فقط `SaveChangesAsync` + Transaction. Repositoryها مستقیماً به Handler تزریق می‌شوند.» این سند شاهد عینی می‌دهد که چرا آن تصمیم درست بود.
**⚠️ هشدار برای آینده:** وقتی از ۳ Entity به ۶۵ Entity برسیم، فشار برای «یک `IUnitOfWork` که همه‌چیز دارد» زیاد می‌شود. **مقاومت کنیم.**

---

## 🔴 A-3. `ChangeTracker.Clear()` بعد از هر `SaveChangesAsync`

**فایل:** `Infrastructure.Persistance.EF\UnitOfWork.cs:190-194`

```csharp
public async Task CommitAsync()
{
    await _context.SaveChangesAsync();
    _context.ChangeTracker.Clear();
}
```

**چرا مشکل‌ساز است:** پاک‌کردن change tracker یعنی هر entity که قبلاً load شده بود detach می‌شود. نتیجه:
- کدی که بعد از commit همان entity را می‌خواهد، **مجبور است دوباره از DB بخواند** (رفت‌وبرگشت اضافه).
- الگوی Unit-of-Work عملاً می‌شکند: چند `CommitAsync` پشت‌سرهم دیگر یک واحد کاری منسجم نیستند.
- این احتمالاً **علتِ** مشکل بعدی (A-4) است: چون tracker پاک می‌شود، مجبور شده‌اند مرحله‌به‌مرحله commit کنند.

❓ **حدس نزدم** که آیا این عمدی بوده (برای حل یک باگ tracking) یا صرفاً یک workaround. ولی هر دو حالت، نشانهٔ یک مسئلهٔ طراحی عمیق‌تر است.

**درس برای ما:** ✅ ما `ChangeTracker.Clear()` نداریم. نگهش داریم همین‌طور.

---

## 🔴 A-4. مرز تراکنش شکسته — چند `Commit` تودرتو در یک عملیات

**فایل:** `ApplicationUseCases\Commands\Vouchers\Voucher\Create\AddVoucherCommandHandler.cs`

| خط | کد |
|---|---|
| ۲۴ | `_unitOfWork.BeginTransaction();` |
| ۱۲۱ | `await _unitOfWork.CommitAsync();` ← **داخل** `ProcessVoucherHeadAsync`، بعد از افزودن سرسند |
| ۱۵۶ | `await _unitOfWork.CommitAsync();` ← **داخل** `ProcessVoucherDetailAsync`، بعد از هر ردیف |
| ۵۶ | `_unitOfWork.Commit();` ← تراکنش نهایی |

**چرا مهم است:** ثبت یک سند حسابداری باید **اتمیک** باشد. اینجا سرسند و هر ردیف `SaveChanges` جداگانه می‌خورند. اگرچه یک `BeginTransaction` بیرونی وجود دارد که (در تئوری) rollback را ممکن می‌کند، ولی:
- هر `SaveChangesAsync` یک round-trip جداگانه به Oracle است → برای سندی با N ردیف، **N+1 رفت‌وبرگشت** به‌جای ۱.
- `Commit()` در خط ۵۶ **synchronous** است (`_context.SaveChanges()` + `CurrentTransaction?.Commit()`) — یعنی حتی مسیر commit هم blocking است.
- ترکیب با `ChangeTracker.Clear()` (A-3) وضعیت را شکننده می‌کند.

**درس برای ما:** ✅ **معماری ما از قبل بهتر است** — طبق تصمیم فاز ۵: «Repository فقط stage می‌کند؛ Handler تنها مالک مرز تراکنش است و **یک‌بار** `SaveChangesAsync` صدا می‌زند.» فاز ۱۰ (composite create) دقیقاً همین را با تست قفل کرده. **این را به‌عنوان یک invariant غیرقابل‌مذاکره نگه داریم.**

---

## 🔴 A-5. متدهای تراکنش **synchronous** اند

**فایل:** `Infrastructure.Persistance.EF\UnitOfWork.cs:203-219`

```csharp
public void BeginTransaction()
{
    _context.Database.BeginTransaction();
}

public void Commit()
{
    _context.SaveChanges();
    _context.Database.CurrentTransaction?.Commit();
}

public void Rollback()
{
    _context.Database.CurrentTransaction?.Rollback();
}
```

هر سه I/O واقعی به دیتابیس‌اند ولی هیچ‌کدام async نیستند. `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` در EF Core موجودند و استفاده نشده‌اند.

**درس برای ما:** ✅ `IUnitOfWork` ما از قبل `Begin/Commit/RollbackTransactionAsync` دارد. درست است.

---

## 🟡 A-6. متدهای `async` بدون `await` که `IQueryable` برمی‌گردانند (deferred execution فراری)

**فایل:** `Infrastructure.Persistance.EF\Repositories\VouchersDetailRepository.cs:574-621`

```csharp
public virtual async Task<IEnumerable<TafsiliGroupReviewResultDto>> TafsiliGroupReview(List<SearchParam> filter)
{
    ...
    var query = _context.Database.SqlQueryRaw<TafsiliGroupReviewResultDto>(sqlQuery, paramList.ToArray())
                                 .AsNoTracking();
    var script = query.ToQueryString();
    return query;
}
```

**سه مشکل در ۴ خط:**

1. **`async` بدون هیچ `await`** — کامپایلر warning می‌دهد؛ متد در واقع synchronous است و ماشین حالت async بی‌فایده ساخته می‌شود.
2. **`IQueryable` به‌عنوان `IEnumerable` برگردانده می‌شود** — یعنی کوئری هنوز اجرا نشده و **بیرون از repository** اجرا خواهد شد. اگر `DbContext` تا آن لحظه dispose شده باشد → استثنای runtime. ضمناً مرز لایه‌ها می‌شکند: لایهٔ بالاتر می‌تواند ناخواسته کوئری را تغییر دهد یا **دوبار** اجرایش کند.
3. **`var script = query.ToQueryString();`** — یک فراخوانی debug جامانده که در **هر درخواست** رشتهٔ کامل SQL را تولید می‌کند و بلافاصله دور می‌ریزد. کار بی‌فایده روی مسیر داغ گزارش.

همین الگو در `VouchersDetailRepository.cs:671, 726` و احتمالاً موارد مشابه تکرار شده.

**درس برای ما:** repository باید **همیشه** نتیجهٔ materialized برگرداند (`List<T>`/`PagedResult<T>`) یا صریحاً `IQueryable` را در امضایش اعلام کند. ✅ Read Repositoryهای فعلی ما `PagedResult<T>` برمی‌گردانند — درست است.

---

## 🟡 A-7. فایل‌های Repository غول‌پیکر

| فایل | خطوط |
|---|---|
| `Infrastructure.Persistance.EF\Repositories\VouchersDetailRepository.cs` | **۲٬۷۶۶** |
| `Infrastructure.Persistance.EF\Repositories\VouchersHeadRepository.cs` | **۱٬۱۸۹** |
| `Infrastructure.Persistance.EF\Repositories\ChargeAndCostRepository.cs` | ۳۱۶ |
| مجموع پوشهٔ Repositories | ۹٬۳۹۳ |

`VouchersDetailRepository` تنها **۲۹٪ کل کد Infrastructure** است. با ۵۸ فراخوانی `.Include(` در همان فایل.

**علت ریشه‌ای:** منطق **خواندن/گزارش** و منطق **نوشتن** در یک کلاس مخلوط شده‌اند. اکثر آن ۲٬۷۶۶ خط، کوئری‌های گزارشی (`TrialBalanceReport`, `ConsolidateReport`, `TafsiliReview`, …) اند.

**درس برای ما:** ✅ **قانون ۲ در `CLAUDE.md`** («سمت Read باید از View/MV مجزا بخواند، نه مستقیماً از مدل نوشتن») دقیقاً همین را پیشگیری می‌کند. ما از قبل Read Repository و Write Repository را جدا کرده‌ایم. **وقتی گزارش‌ها را ساختیم، حتماً در فایل/کلاس‌های مجزا (مثلاً `Reporting/` جدا از `Repositories/`) نگه داریم.**

---

## 🟡 A-8. `.Include()` سنگین و چندلایه روی مسیرهای پرترافیک

**فایل:** `Infrastructure.Persistance.EF\Repositories\VouchersHeadRepository.cs:210-212`

```csharp
.Include(d => d.DocsDetails).ThenInclude(v => v.VouchersDetailLinkTafsilis)
.Include(d => d.DocsDetails).ThenInclude(v => v.Check)
.Include(d => d.DocsDetails).ThenInclude(v => v.Receipt)
```

و `:539` → `.Include(vh => vh.DocsDetails).ThenInclude(dd => dd.AccountCode)`

**چرا مهم است:** سه `Include` موازی روی همان collection (`DocsDetails`) در EF Core به یک **کارتزین explosion** منجر می‌شود مگر اینکه `AsSplitQuery()` استفاده شود. جست‌وجو کردم: **`AsSplitQuery` در هیچ‌کجای این پروژه استفاده نشده.** برای سندی با ۵۰ ردیف که هرکدام ۷ تفصیلی دارد، تعداد ردیف‌های برگشتی از دیتابیس به‌شدت متورم می‌شود.

**درس برای ما:** هرجا بیش از یک collection را با هم `Include` کردیم، `AsSplitQuery()` را آگاهانه بررسی کنیم — یا اصلاً برای خواندن از **Projection** (`.Select(...)` به DTO) استفاده کنیم که هیچ‌وقت این مشکل را ندارد.

---

## 🟡 A-9. بارگذاری کامل جدول‌های مرجع در حلقه، به‌جای cache

**فایل:** `ApplicationUseCases\Commands\Vouchers\Voucher\Create\AddVoucherCommandHandler.cs:179`

```csharp
var levList = _unitOfWork.tbLevelTafsilRepository.GetAllTbLevelTafsilAsync().Result;
```

این داخل `CreateVoucherDetail` است — یعنی **به‌ازای هر ردیف سند**، کل جدول `TB_LEVEL_TAFSIL` دوباره خوانده می‌شود. سپس در حلقهٔ خط ۱۹۶-۲۰۲ برای هر یک از ۷ سطح یک `SingleOrDefault` روی همان لیست اجرا می‌شود.

همین الگو در ۶ فایل دیگر تکرار شده (`AddDrmdElamCommandHandler.cs:97`, `AddElamDetailCommandHandler.cs:25`, `AddOtherElamHeadCommandHandler.cs:100`, `AddElamReciveVahedCommandHandler.cs:140`, `AddPayReciveCommandHandler.cs:136`, `AddVoucherListCommandHandler.cs:147`).

مشابهش برای `SysType` — `AddVoucherCommandHandler.cs:102`:
```csharp
var defaultSysId = _unitOfWork.tbSysTypeRepository.GetAllSysType().Result.FirstOrDefault(a => a.Code == "1").Id;
```
کل جدول خوانده می‌شود تا **یک** ردیف با `Code == "1"` پیدا شود — فیلتر در حافظه به‌جای دیتابیس.

**درس برای ما:** جدول‌های مرجع کوچک و تقریباً ثابت (`TB_LEVEL_TAFSIL`، `TB_SYSTYPE`) کاندیدای عالی **`IMemoryCache`** اند. حداقل باید در طول یک درخواست یک‌بار خوانده شوند، نه به‌ازای هر ردیف.

---

## 🟡 A-10. `.FirstOrDefault(...).Id` بدون بررسی null

`AddVoucherCommandHandler.cs:102` و `AddVoucherListCommandHandler.cs:77`:
```csharp
var defaultSysId = _unitOfWork.tbSysTypeRepository.GetAllSysType().Result.FirstOrDefault(a => a.Code == "1").Id;
```

و `AddVoucherCommandHandler.cs:201`:
```csharp
levList.SingleOrDefault(a => a.CodeLevel == i.ToString()).Id
```

اگر ردیف مرجع وجود نداشته باشد → `NullReferenceException` خام. در `AddVoucherCommandHandler` این استثنا توسط `catch (Exception ex) { ... throw new CustomException(ex.Message); }` (خط ۵۹-۶۳) گرفته و **پیام خامش به کاربر برگردانده می‌شود**.

**درس برای ما:** ✅ `GlobalExceptionHandler` ما از قبل پیام خام را به بیرون نمی‌دهد (با تست اثبات شده). این نکته را نگه داریم.

---

# ب) الگوهای خوب که **باید کپی کنیم** 🟢

## 🟢 B-1. Global Query Filter برای Soft Delete

**فایل:** `Infrastructure.Persistance.EF\Contexts\ApplicationDbContext.cs:76-89`

```csharp
modelBuilder.Entity<TbLevelTafsil>().HasQueryFilter(t => !t.IsDeleted);
modelBuilder.Entity<Tafsili>().HasQueryFilter(t => !t.IsDeleted);
modelBuilder.Entity<AccountCode>().HasQueryFilter(a => !a.IsDeleted);
modelBuilder.Entity<TbAccountLinkLevel>().HasQueryFilter(a => !a.IsDeleted);
modelBuilder.Entity<TbAccountLinkTafsilGroup>().HasQueryFilter(a => !a.IsDeleted);
modelBuilder.Entity<TbTafsilLinkTafsilGroup>().HasQueryFilter(a => !a.IsDeleted);
modelBuilder.Entity<TbTafsilGroup>().HasQueryFilter(a => !a.IsDeleted);
modelBuilder.Entity<VouchersHead>().HasQueryFilter(c => !c.IsDeleted);
modelBuilder.Entity<VouchersDetail>().HasQueryFilter(c => !c.IsDeleted);
...
```

**۱۴ Entity** با فیلتر سراسری soft-delete.

**چرا این بهتر از روش فعلی ماست:** ما در حال حاضر `Where(x => x.ISDELETED != true)` را **دستی در هر Query** تکرار می‌کنیم. با رشد تعداد Query، احتمال اینکه یکی فراموش شود و رکورد حذف‌شده به کاربر نشت کند بالا می‌رود — و چون این یک **باگ خاموش** است، تست هم لزوماً نمی‌گیردش.

**⚠️ ولی یک تفاوت schema حیاتی هست که باید حل شود:** در پروژهٔ آن‌ها `IsDeleted` از نوع `bool` غیر-nullable است، پس `!t.IsDeleted` کافی است. در **ما** ستون `ISDELETED` در بیشتر جدول‌ها `bool?` است و **`null` هم به معنی «حذف‌نشده»** تلقی می‌شود (این را در فاز ۹ با تست SQLite اثبات کردیم). پس فیلتر معادل ما باید `x => x.ISDELETED == null || x.ISDELETED == false` باشد، **نه** `!x.ISDELETED` و **نه** `x.ISDELETED != true` (که در منطق سه‌مقداری SQL ردیف‌های `NULL` را حذف می‌کند).

**📌 پیشنهاد مشخص برای backlog ما:** افزودن Global Query Filter به `LegacyDbContext` برای جدول‌های دارای `ISDELETED`، به‌همراه:
- یک تست که تأیید کند فیلتر شاخهٔ `NULL` را درست مدیریت می‌کند.
- توجه به اینکه `GetForUpdateAsync` و مسیرهای soft-delete **باید** بتوانند رکورد حذف‌شده را ببینند (برای idempotency فاز ۸) → یعنی آنجا `IgnoreQueryFilters()` لازم می‌شود. **این یک تغییر ظریف است و باید با `qa-tester` بررسی شود، نه مکانیکی اعمال شود.**

---

## 🟢 B-2. گزارش‌ها از View های Oracle خوانده می‌شوند، نه از مدل نوشتن

نمونه‌ها:
- `Infrastructure.Persistance.EF\Repositories\VouchersDetailRepository.cs:600` → `FROM VW_TAFSILIREPORT vh`
- `Infrastructure.Persistance.EF\Repositories\VouchersHeadRepository.cs:1081` → `... from vwcartable`
- Entityهای View در `Tamin.Core\Entities\ViewEntity\` (مثلاً `ViewEntity\Voucher\VwCartable.cs`)

با aggregation سمت دیتابیس (`SUM(vh.Debtor)`, `GREATEST(...)`, `GROUP BY`) به‌جای بارگذاری ردیف‌ها در حافظه.

> ✅ **این دقیقاً قانون ۲ در `CLAUDE.md` ماست** و یک تأیید عملی برایش. وقتی به فاز گزارش‌ها رسیدیم، مستقیماً از همین View های موجود (`VW_TAFSILIREPORT`, `VWCARTABLE` و ۲۶ View دیگری که در reverse engineering کشف کردیم) استفاده کنیم.

---

## 🟢 B-3. SQL خام **پارامتری‌شده** (نه الحاق رشته)

`VouchersDetailRepository.cs:606-617`:
```csharp
whereClause = OracleExpressionToSqlConverter.ConvertToSql(wrapper, parameters);
sqlQuery += " WHERE " + whereClause;
...
var oracleParam = new OracleParameter(param.Key, OracleDbType.Varchar2)
{
    Value = param.Value ?? (object)DBNull.Value,
    Direction = ParameterDirection.Input
};
```

با اینکه SQL به‌صورت رشته ساخته می‌شود، **مقادیر** از طریق `OracleParameter` می‌روند نه الحاق مستقیم. یعنی SQL Injection از این مسیر بسته است، و ضمناً Oracle می‌تواند plan را cache کند.

⚠️ **ولی:** امنیت این کاملاً به `OracleExpressionToSqlConverter` وابسته است. اگر نام ستون‌ها از ورودی کاربر (`List<SearchParam> filter`) بدون whitelist وارد `whereClause` شوند، injection از طریق **نام ستون** ممکن است.
❓ **`OracleExpressionToSqlConverter` را نخواندم — حدس نزدم.** اگر روزی خواستیم فیلتر پویا بسازیم، این نقطه باید با `security-reviewer` بررسی شود.

---

## 🟢 B-4. Authorization مبتنی بر نقش با تفکیک خواندن/نوشتن

`Presentaion.Web.API\Controllers\AccountCode\V1\AccountCodeController.cs`:
- **خواندن** (خطوط ۵۱، ۶۰، ۶۸، ۷۶، ۸۴، ۹۱، ۱۰۰، …) → هر ۶ نقش
- **نوشتن/حذف** (خطوط ۱۸۵، ۱۹۵، ۲۰۴، ۲۱۳، ۲۲۲، ۲۳۱، ۲۴۰) → **فقط `FINANCIAL_CORE_SETAD_ADMIN`**

> 🔴 **این مستقیماً ریسک باز 🔴 IDOR ما را نشانه می‌گیرد.** ما هیچ authorization مبتنی بر نقش نداریم. سیستم قدیمی حداقل یک لایه دارد، و از **همان پکیج `Tamin.Framework.Common.Security`** استفاده می‌کند که ما در فاز ۷ به‌کار بردیم — یعنی `RolesAllowedAttribute` از قبل در دسترس ماست و فقط باید فهرست نقش‌ها تعریف شود.
>
> ⚠️ ولی توجه: این **role-based** است نه **record-based**. حتی سیستم قدیمی هم ایزولاسیون بین `VAHEDCODE`ها را در این Controller اعمال نمی‌کند. ❓ آیا `businessUserAccessRepository` این کار را می‌کند؟ **بررسی نکردم.**

---

## 🟢 B-5. `AddAsync` روی graph کامل به‌جای درج جداگانه

`Commands\Vouchers\TmpVouchers\CreateTmpHead\AddTmpVoucherHeadCommandHandler.cs`:
```
خط 32:  List<TmpVoucherDetail> details = new List<TmpVoucherDetail>();
خط 33:  foreach (var item in command.Details) { ... }
خط 58:  var newEntity = await _unitOfWork.tmpVoucherHeadRepository.AddAsync(head);
```
یک `AddAsync` روی سرسند، ردیف‌ها به‌عنوان بخشی از graph.

> ✅ **دقیقاً همان الگوی composite create فاز ۱۰ ما.** جالب اینکه در مسیر **سند اصلی** (`AddVoucherCommandHandler`) این الگو رعایت **نشده** (رجوع به A-4) — یعنی خودشان هم ناسازگارند و ما نسخهٔ بهترشان را برداشته‌ایم.

---

# ج) مسائل معماری و کیفیت کد

## 🔴 C-1. Domain آلوده به attributeهای Persistence

`Tamin.Core\Entities\AccountCodes\AccountCode.cs:8-9`:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
```
و سپس روی هر پراپرتی: `[Column("TYPECODE")]`, `[MaxLength(1)]`, `[ForeignKey("Parent")]`, `[Required(ErrorMessage = "...")]`.

یعنی لایهٔ Domain آن‌ها هم‌زمان حامل **نگاشت دیتابیس** و **پیام‌های اعتبارسنجی فارسیِ سطح UI** است.

> ✅ **قید غیرقابل‌مذاکرهٔ ما («`Accounting.Domain` صفر وابستگی خارجی؛ فقط POCO خالص؛ همهٔ Fluent Mapping در Infrastructure») دقیقاً همین را پیشگیری می‌کند.** این سند شاهد عینی می‌دهد که چرا آن قید ارزش دارد.

## 🔴 C-2. Validatorهایی که هرگز اجرا نمی‌شوند (دو باگ واقعی)

1. **نوع اشتباه در generic:**
   - `Commands\AccountCodes\Moin\Update\UpdateMoinCodeValidator.cs:10` → `class UpdateMoinCodeValidator : BaseCommandValidator<AccountCode>` (باید `UpdateMoinCodeCommand` می‌بود)
   - `Commands\AccountCodes\Delete\deleteAccountCodeValidator.cs:12` → `class DeleteAccountCodeValidator : BaseCommandValidator<AccountCode>`

   **نتیجه: مسیر ویرایش و حذف کد معین عملاً بدون اعتبارسنجی است.**

2. **شرط `When` که همیشه false است:**
   `Commands\AccountCodes\Moin\Create\AddMoinCodeCommandValidator.cs:70-74`:
   ```csharp
   When(a => a.TypeCode.Equals(TypeCodes.Moin), () =>
   {
       RuleFor(a => a.AccCode).Length(6)...
       RuleFor(p => p.ParentId).NotNull().NotEmpty().WithMessage("کد کل نمیتواند خالی باشد");
   });
   ```
   `TypeCode` روی Command هرگز مقداردهی نمی‌شود (Controller خط ۱۹۱ آن را پاس نمی‌دهد و سازنده هم ست نمی‌کند) → مقدارش `0` می‌ماند → شرط همیشه false → **`ParentId` عملاً اجباری نیست.**

**📌 پیشنهاد مشخص برای backlog ما:** یک تست reflection بنویسیم که تأیید کند **هر** `IRequest` در `Accounting.Application` یک `IValidator<T>` با تایپ **دقیقاً منطبق** دارد. این کلاس باگ را ساختاراً می‌بندد. (هم‌خانواده با `HttpVerbConventionTests` و `NoIndependentLinkTableWritePathTests` موجود.)

## 🟡 C-3. تکرار کد بین Handlerها

`ValidateTafsiliLevels` / حلقهٔ `for (int i = 1; i <= 7; i++)` روی `Tafsili{i}Id` با reflection، در حداقل ۴ Handler تکرار شده:
- `Commands\Vouchers\Voucher\Create\AddVoucherCommandHandler.cs:159-176` و `196-202`
- `Commands\Vouchers\Voucher\CreateList\AddVoucherListCommandHandler.cs:147`
- `Commands\Vouchers\TmpVouchers\CreateVouchers\AddVochersImportTempCommandHandler.cs:94`
- `Commands\Elms\OtherElamHead\Create\AddOtherElamHeadCommandHandler.cs:100`

**دو مشکل:** (۱) هر اصلاح باید در ۴ جا انجام شود → drift. (۲) استفاده از **reflection** (`typeof(...).GetProperty($"Tafsili{i}Id")`) به‌جای یک آرایه/لیست — هم کند است، هم در زمان کامپایل بررسی نمی‌شود، و تغییر نام پراپرتی به یک خطای runtime بی‌صدا تبدیل می‌شود.

**درس برای ما:** ✅ ما در فاز ۱۱ به‌جای هفت پراپرتی `Tafsili1Id..Tafsili7Id`، یک **لیست** `IReadOnlyList<VoucherDetailTafsiliLinkInput>` طراحی کردیم — که هم reflection لازم ندارد، هم قابل‌گسترش است. **این تصمیم درست بوده.**
⚠️ ولی توجه: مدل ما تعداد سطوح را محدود نمی‌کند، در حالی که سیستم قدیمی **دقیقاً ۷ سطح** دارد. اگر قرار است سازگار بمانیم، این محدودیت باید جایی اعمال شود. ❓ **تصمیمش با صاحب پروژه است — حدس نزدم.**

## 🟡 C-4. نشت پیام خام استثنا به پاسخ API

`Commands\Vouchers\Voucher\Create\AddVoucherCommandHandler.cs:59-63`:
```csharp
catch (Exception ex)
{
    _unitOfWork.Rollback();
    throw new CustomException(ex.Message);
}
```

`ex.Message` می‌تواند شامل متن خام خطای Oracle (نام جدول، نام constraint، schema) یا `NullReferenceException` باشد.

> ✅ `GlobalExceptionHandler` ما این را حل کرده و **با تست اثبات شده** که `ORA-*` و نام constraint به بدنهٔ پاسخ درز نمی‌کند.

## 🟡 C-5. ناسازگاری نام‌گذاری و تایپو در سراسر پروژه

| نمونه | فایل |
|---|---|
| `Presentaion.Web.API` (املای غلط Presentation) | نام پوشه/پروژه |
| `TypeCodes..cs` (دو نقطه) | `Tamin.Core\Entities\AccountCodes\` |
| `Infrastructure.Persistance.EF` (املای غلط Persistence) | نام پروژه |
| `deleteAccountCodeValidator.cs` (حرف کوچک اول) | `Commands\AccountCodes\Delete\` |
| `UodateMoinCodeDto.cs`, `UodateAccountCodeDto.cs` (Uodate) | `Controllers\AccountCode\V1\Dto\` |
| `AddReversVoucherCommandHabdler.cs` (Habdler) | `Commands\Vouchers\Voucher\Create\ReversedVoucher\` |
| `Seraches` (Searches), `Elms` (Elams?) | پوشه‌های `Queries`/`Commands` |
| `SetCreditorr` (دو تا r) | `Tamin.Core\Entities\Vouchers\VouchersDetail.cs` |
| namespaceهای ناسازگار: `ApplicationUseCase.*` و `ApplicationUseCases.*` و `ApplictionUseCases.*` | `UnitOfWork.cs:24-30` |

مورد آخر واقعاً مشکل‌ساز است — سه املای متفاوت از یک namespace در **همان فایل** کنار هم `using` شده‌اند.

**درس برای ما:** یک قرارداد نام‌گذاری صریح و یک تست/analyzer برایش. ارزان است و از انباشت این نوع بدهی جلوگیری می‌کند.

## 🟡 C-6. `DateTime.Now` به‌جای `DateTime.UtcNow`

در سراسر Handlerها: `AddGroupCodeCommandHandler.cs:30`, `AddKolCodeCommandHandler.cs:31`, `AddMoinCodeCommandHandler.cs:31`, `AddVoucherCommandHandler.cs:117,195`, `ChangeStateCommandhandler.cs:27`, `AccountCode.cs:92,104` و …

> ✅ ما این را از قبل حل کرده‌ایم (`DateTime.UtcNow` در همه‌جا) — و جالب اینکه **دقیقاً همین اشکال را در فاز ۷ هنگام پورت `TokenManager` اصلاح کردیم** (نقص شمارهٔ ۵). این نشان می‌دهد الگو در کل کدبیس آن‌ها هست، نه فقط در `TokenManager`.

## 🟡 C-7. کد کامنت‌شدهٔ انبوه در مسیرهای حیاتی

نمونهٔ خطرناک‌ترین: قاعدهٔ **تراز بدهکار/بستانکار** به‌طور کامل کامنت شده — `Commands\Vouchers\Voucher\Create\AddVoucherValidator.cs:53-69`.

موارد دیگر: `AddMoinCodeCommandValidator.cs` (خطوط ۱۴-۲۲، ۴۴-۶۸ تقریباً همه کامنت)، `AddMoinCodeCommandHandler.cs:24-27`، `AddVoucherCommandHandler.cs:134-139` (حلقهٔ `foreach` روی ردیف‌ها کامنت شده و به یک ردیف تکی تقلیل یافته).

**نکتهٔ ظریف و مهم دربارهٔ خط ۱۳۴-۱۳۹:**
```csharp
long radif = 1;
//foreach (var item in request.AddVoucher.vouchersDetail)
//{
var res = await ProcessVoucherDetailAsync(request.AddVoucher.vouchersDetail, response, radif);
return res;
// radif++;
// }
```
یعنی `AddVoucherCommand` در وضعیت فعلی **فقط یک ردیف** می‌پذیرد، نه لیست. این توضیح می‌دهد چرا بررسی تراز کامنت شده — **با یک ردیف، تراز اصلاً معنا ندارد.** (کد کامنت‌شدهٔ تراز از `details.Sum(...)` استفاده می‌کند که با ورودی تک‌ردیفی سازگار نیست.)

> این یک بینش مهم است: **سیستم قدیمی سند را ردیف‌به‌ردیف می‌سازد**، نه یکجا. که دقیقاً همان چیزی است که مدل ترکیبی فاز ۱۰ ما را توجیه می‌کند — ولی ما نسخهٔ بهترش را داریم (composite create واقعی با لیست).

## 🟡 C-8. ۴۲ Controller با route `api/[controller]/[action]` و بدون versioning واقعی

پوشه‌بندی `Controllers\AccountCode\V1\` نشان می‌دهد قصد versioning بوده، ولی route واقعی `[Route("api/[controller]/[action]")]` است (`AccountCodeController.cs:39`) — یعنی **`V1` در URL ظاهر نمی‌شود**. پوشه صرفاً سازمان‌دهی فایل است.

> ⚠️ ما هم همین مشکل را داریم (تصمیم باز 🟡 «نسخه‌بندی API وجود ندارد»). این تأیید می‌کند که تعویقش عواقب دارد.

---

# د) خلاصهٔ backlog پیشنهادی برای `AccountingCoreClaude`

| # | اقدام | مبنا | اولویت |
|---|---|---|---|
| ۱ | Global Query Filter برای `ISDELETED` در `LegacyDbContext` (با مدیریت درست `NULL` + `IgnoreQueryFilters` در مسیرهای soft-delete) | B-1 | 🔴 |
| ۲ | تست reflection: هر `IRequest` باید `IValidator<T>` منطبق داشته باشد | C-2 | 🔴 |
| ۳ | Authorization مبتنی بر نقش با `RolesAllowedAttribute` (خواندن vs نوشتن) | B-4 | 🔴 |
| ۴ | تست/analyzer ممنوعیت `.Result`/`.Wait()` | A-1 | 🟡 |
| ۵ | محافظت از باریک ماندن `IUnitOfWork` (تست عدم رشد) | A-2 | 🟡 |
| ۶ | Cache برای جدول‌های مرجع (`TB_LEVEL_TAFSIL`, `TB_SYSTYPE`) | A-9 | 🟡 |
| ۷ | گزارش‌ها از View های Oracle، در پوشه/کلاس مجزا از Write Repository | B-2, A-7 | 🟡 |
| ۸ | بررسی آگاهانهٔ `AsSplitQuery()` هرجا چند collection را `Include` کردیم | A-8 | 🟡 |
| ۹ | Repository همیشه نتیجهٔ materialized برگرداند، نه `IQueryable` پنهان | A-6 | 🟡 |
| ۱۰ | قرارداد نام‌گذاری + versioning API (`/v1`) قبل از اولین مصرف‌کنندهٔ خارجی | C-5, C-8 | 🟡 |

---

# ه) آنچه بررسی نشد (صریح)

1. `OracleExpressionToSqlConverter` — امنیت فیلتر پویا (B-3).
2. `Infrastructure.Persistance.EF\Repositories\` — فقط ۵ فایل از ۴۰+ فایل را عمیق خواندم.
3. `Presentaion.Web.API\Program.cs`, `Middlewares\`, `Filter\` — نخواندم.
4. `ApplicationUseCases\Behaviors\` — فقط نام `ValidatorBehavior.cs` را دیدم.
5. `Account.Tests` — پوشش تست‌ها ارزیابی نشد.
6. **ایندکس‌های واقعی دیتابیس** — فقط دو `HasIndex` در `ApplicationDbContext.cs:55,106` دیدم. ارزیابی «عدم ایندکس‌گذاری درست» **نیازمند دسترسی به execution plan روی Oracle زنده است که انجام نشد. حدس نزدم.**
7. `businessUserAccessRepository` — آیا ایزولاسیون `VAHEDCODE` را اعمال می‌کند؟
8. `.gitlab-ci.yml` / `Dockerfile` — pipeline و استقرار بررسی نشد.
