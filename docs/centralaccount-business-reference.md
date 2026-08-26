# مرجع منطق کسب‌وکار — پروژهٔ خارجی `D:\CentralAccount`

> **تاریخ استخراج:** ۲۰۲۶-۰۸-۲۵ و ۲۰۲۶-۰۸-۲۶ (چهار پاس)
> **ماهیت:** تحلیل **Read-Only** پروژهٔ اصلی صاحب پروژه (`Tamin.BaseTemplate.sln`) که همان «حسابداری متمرکز با کدینگ شناور» روی همان schema اوراکل `CENTRALACCOUNT` است.
> **هیچ فایلی در `D:\CentralAccount` تغییر نکرد. هیچ build/run روی آن پروژه اجرا نشد. هیچ کدی در `backend/` ما تغییر نکرد.**

## ساختار این سند — سه پاس

| پاس | بخش‌ها | دامنه |
|---|---|---|
| **پاس اول** | ۱ تا ۹ | جواب سؤال مسدودکنندهٔ `TYPECODE` + کدینگ حساب + invariantهای سند + مرز Aggregate |
| **پاس دوم** | ۱۰ تا ۱۲ | ممیزی کامل ۴۴ enum و ۴۲ پراپرتی `bool?` ما + فهرست ۴۲ Controller |
| **پاس سوم** | ۱۳ تا ۲۰ | **دو تصحیح مهم پاس اول** + کاتالوگ کامل قوانین کسب‌وکار از ۳۷۲ Handler + ۱۵۶ Command + ۲۲۱ Query + گردش‌کارهای کلان |
| **پاس چهارم** | ۲۱ تا ۲۲ | **مرجع per-Module** (به‌ازای هر Entity: عملیات + منطق + قوانین) + `ReportController` + `CartableController` + ۶ زیرپروژهٔ باقی‌مانده + کشف چندمستأجری و مسیر Kafka |

> ⚠️ **بخش‌های ۱۳ و ۱۴ دو نتیجه‌گیری پاس اول را تصحیح می‌کنند** (تغییرناپذیری سند، و تضمین تراز). اگر فقط پاس اول را خوانده‌اید، حتماً آن دو بخش را هم بخوانید.
>
> 📌 **برای تصمیم «این Entity در پروژهٔ ما چه CRUD/عملیاتی لازم دارد» → مستقیماً بخش ۲۱ را بخوانید.**
>
> **وضعیت نهایی پوشش و آنچه باقی مانده: بخش ۲۲.**

## تفاوت این سند با `docs/tamin-core-entity-reference.md`

| | سند قبلی (فاز ۹.۵) | این سند |
|---|---|---|
| دامنهٔ خواندن | فقط `Tamin.Core\Entities\` | **کل solution** (Domain + Application + Infrastructure + API) |
| نوع شواهد | ساختار Entity و navigation property | **منطق واقعی کسب‌وکار**: Validator، Handler، Repository |
| قدرت استنتاج | سیگنال طراحی | **رفتار اجراشوندهٔ سیستم قدیمی** |

⚠️ **این سند sync خودکار ندارد.** اگر `D:\CentralAccount` تغییر کند، باید دستی بازبینی شود.

⚠️ **قاعدهٔ شواهد در این سند:** هر ادعا با مسیر فایل + شمارهٔ خط پشتیبانی می‌شود. هرجا چیزی را پیدا نکردم یا مبهم بود، صریحاً با «❓ پیدا نشد / نیاز به بررسی بیشتر» علامت زده‌ام و **حدس نزده‌ام**.

---

# بخش ۱ (اولویت فوری) — جواب قطعی چهار ستون `NUMBER(1)` در `TB_ACCOUNTCODE`

## خلاصهٔ اجرایی

هر چهار ستون **enum چندمقداری‌اند و هیچ‌کدام بولین نیستند.** اسکفولد ما (`bool?`) در هر چهار مورد **غلط** است.

در پروژهٔ `CentralAccount` هر چهار ستون یک `enum` صریح در `Tamin.Core\Entities\AccountCodes\` دارند و **EF Core بدون هیچ ValueConverter** آن‌ها را به `int` زیرین نگاشت می‌کند (بررسی شد: در `Infrastructure.Persistance.EF\Contexts\` هیچ `HasConversion` برای این چهار ستون وجود ندارد) — یعنی مقدار ذخیره‌شده در Oracle دقیقاً همان عدد enum است.

| ستون Oracle | نوع فعلی (غلط) در کد ما | نوع درست | مقادیر |
|---|---|---|---|
| `TYPECODE` | `bool?` | `TypeCodes` | **۱=گروه، ۲=کل، ۳=معین** |
| `TYPEACCCODE` | `bool?` | `TypeAccCode?` | **۱=موقت، ۲=دائم** |
| `TYPEACTION` | `bool?` | `TypeAction?` | **۱=کنترل نشود، ۲=اخطار دهد، ۳=ثبت نشود** |
| `TYPEACTIVITY` | `bool?` | `TypeActivity?` | **۱..۷** (نه ۳ مقدار!) — جدول کامل پایین‌تر |

---

## ۱-۱. `TYPECODE` → `TypeCodes` (مسدودکنندهٔ اصلی — قطعی)

**فایل:** `D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeCodes..cs`
(نام فایل واقعاً دو نقطه دارد: `TypeCodes..cs` — تایپوی خودشان)

```csharp
public enum TypeCodes
{
    [Description("گروه")]
    Group = 1,
    [Description("کل")]
    Kol = 2,
    [Description("معین")]
    Moin = 3
}
```

**نگاشت روی ستون:** `D:\CentralAccount\Tamin.Core\Entities\AccountCodes\AccountCode.cs:16-19`

```csharp
[Column("TYPECODE")]
[MaxLength(1)]
[Required(ErrorMessage = "نوع حساب اجباری می باشد")]
public TypeCodes TypeCode { get; private set; }
```

**شواهد تأییدی مستقل — استفادهٔ مستقیم در کوئری‌های EF:**
`D:\CentralAccount\Infrastructure.Persistance.EF\Repositories\AccountCodeRepository.cs`
خطوط ۳۷، ۲۱۷، ۲۲۵، ۲۳۴، ۲۵۴، ۲۸۲، ۲۹۲ — مثلاً:

```csharp
return _dbContext.Where(a => a.ParentId != null && a.TypeCode == TypeCodes.Kol)   // خط 225
```

چون هیچ ValueConverter وجود ندارد، این کوئری در SQL به `TYPECODE = 2` ترجمه می‌شود. **پس مقدار فیزیکی در دیتابیس قطعاً ۱/۲/۳ است.**

**شاهد سوم — Handlerها مقدار را hardcode می‌کنند:**

| فایل | خط | کد |
|---|---|---|
| `ApplicationUseCases\Commands\AccountCodes\Group\Create\AddGroupCodeCommandHandler.cs` | ۲۹ | `account.SetTypeCode(TypeCodes.Group);` |
| `ApplicationUseCases\Commands\AccountCodes\Kol\Create\AddKolCodeCommandHandler.cs` | ۲۹ | `account.SetTypeCode(TypeCodes.Kol);` |
| `ApplicationUseCases\Commands\AccountCodes\Moin\Create\AddMoinCodeCommandHandler.cs` | ۲۹ | `account.SetTypeCode(TypeCodes.Moin);` |

> **نکتهٔ طراحی مهم:** `TYPECODE` **هرگز از ورودی کاربر نمی‌آید.** سه Endpoint مجزا (`AddGroupCodeAsync` / `AddKolCodeAsync` / `AddMoinCodeAsync`) وجود دارد و هرکدام سطح خودش را در Handler ثابت می‌کند. این دقیقاً قرینهٔ الگویی است که ما می‌توانیم به‌کار ببریم.

---

## ۱-۲. `TYPEACCCODE` → `TypeAccCode` (موقت/دائم)

**فایل:** `D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeAccCode.cs`

```csharp
public enum TypeAccCode
{
    [Description("موقت")]
    temporary = 1,
    [Description("دائم")]
    permanent = 2
}
```

**دقیقاً منطبق بر کامنت ستون در schema ما:** «نوع حساب (۱موقت۲دائم)». ✅ هیچ تناقضی نیست.

**نگاشت:** `AccountCode.cs:51-53` — نوع `TypeAccCode?` (**nullable**).

**کجا استفاده می‌شود:**
- **فقط روی سطح گروه ست می‌شود.** `AddGroupCodeCommandHandler.cs:27` → `account.SetTypeAccCode(command.TypeAccCode);`
- در `AddKolCodeCommandHandler` و `AddMoinCodeCommandHandler` **اصلاً ست نمی‌شود**. در `AddMoinCodeCommandHandler.cs:26` این خط عمداً کامنت شده:
  ```csharp
  // account.SetTypeAccCode(command.TypeAccCode);
  ```
- اعتبارسنجی گروه (`Group\Create\AddGroupCodeValidator.cs:31-34`):
  ```csharp
  RuleFor(x => x.TypeAccCode)
  .NotNull().WithMessage("نوع حساب اجباری می باشد")
  .Must(value => (int)value == 1 || (int)value == 2)
  .WithMessage("نوع حساب نامعتبر است؛ فقط مقادیر 1 یا 2 مجاز هستند");
  ```

❓ **پیدا نشد:** هیچ منطق کسب‌وکاری که بر اساس موقت/دائم رفتار متفاوتی داشته باشد (مثلاً بستن حساب‌های موقت در پایان دوره) در `ApplicationUseCases` پیدا نکردم. ستون `MOINFORCLOSE` و پوشهٔ `Commands\RabetClosings` و `Vouchers\ClosingVouchers` وجود دارند ولی **رابطهٔ آن‌ها با `TYPEACCCODE` را بررسی نکردم** — نیاز به بررسی بیشتر.

---

## ۱-۳. `TYPEACTION` → `TypeAction` (رفتار در برابر خلاف ماهیت)

**فایل:** `D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeAction.cs`

```csharp
public enum TypeAction
{
    [Description("کنترل نشود")]
    NotControlled = 1,
    [Description("اخطار دهد")]
    Warning = 2,
    [Description("ثبت نشود")]
    NotAdded = 3
}
```

**ترتیب عددی دقیقاً همان ترتیب کامنت ستون ما است:** «نوع خلاف ماهیت(کنترل نشود-اخطار دهد-ثبت نشود)» → ۱، ۲، ۳. ✅

**نگاشت:** `AccountCode.cs:59-61` — نوع `TypeAction?` (**nullable**).

**کجا ست می‌شود:** **فقط روی معین.**
- `Moin\Create\AddMoinCodeCommandHandler.cs:23` → `account.SetTypeAction(command.TypeAction);`
- `Moin\Update\UpdateMoinCodeHandler.cs:34` → `entity.SetTypeAction(command.TypeAction);`
- در Handlerهای Group و Kol **اصلاً ست نمی‌شود.**

### ⚠️ یافتهٔ کلیدی: `TypeAction` هرگز *مصرف* نمی‌شود

سناریوی کسب‌وکاری موردانتظار این است که هنگام ثبت سند، اگر مبلغ ردیف **خلاف ماهیت حساب** باشد (مثلاً حسابی که ماهیتش بدهکار است بستانکار شود)، بسته به مقدار `TYPEACTION` سیستم یا سکوت کند (۱)، یا اخطار دهد (۲)، یا ثبت را رد کند (۳).

**این منطق در کد پیدا نشد.** جست‌وجوی کامل روی `ApplicationUseCases` نشان می‌دهد `TypeAction` فقط در این نقاط ظاهر می‌شود:

| فایل | نوع استفاده |
|---|---|
| `Commands\AccountCodes\Moin\Create\AddMoinCodeCommand.cs:31` | تعریف پراپرتی |
| `Commands\AccountCodes\Moin\Create\AddMoinCodeCommandHandler.cs:23` | **نوشتن** |
| `Commands\AccountCodes\Moin\Update\UpdateMoinCodeCommand.cs:33` | تعریف پراپرتی |
| `Commands\AccountCodes\Moin\Update\UpdateMoinCodeHandler.cs:34` | **نوشتن** |
| `Queries\AccountCodes\ViewModels\GetMoinCodeByIdDto.cs:19` | **خواندن** (برای فرم ویرایش) |
| `Queries\AccountCodes\GetMoinByIdForEdit\GetMoinCodeByIdQueryHandler.cs:30` | **خواندن** |

**یعنی: ذخیره می‌شود، نمایش داده می‌شود، ولی در هیچ اعتبارسنجی سندی خوانده نمی‌شود.** در `AddVoucherValidator.cs` و `AddVoucherCommandHandler.cs` و `AddVoucherDetailValidator.cs` هیچ ارجاعی به `TypeAction` یا `TypeActivity` نیست.

❓ **نیاز به بررسی بیشتر:** ممکن است این کنترل در **UI** (فرانت‌اند، که خارج از این solution است) یا در **تریگر/پکیج سمت Oracle** پیاده شده باشد. من فقط این solution را خواندم و هیچ‌کدام از آن دو را بررسی نکردم. **حدس نزدم.**

---

## ۱-۴. `TYPEACTIVITY` → `TypeActivity` (ماهیت حساب) — ⚠️ **تناقض با کامنت schema**

**فایل:** `D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeActivity.cs`

```csharp
public enum TypeActivity
{
    [Description("بدهکار")]          Debit      = 1,
    [Description("بستانکار")]        Credit     = 2,
    [Description("بدهکار-بستانکار")] DebitCredit = 3,
    [Description("بدهکار-طی دوره")]   DebitPer   = 4,
    [Description("بستانکار-طی دوره")] CreditPer  = 5,
    [Description("بدهکار-پایان دوره")] DebitFin   = 6,
    [Description("بستانکار-پایان دوره")] CreditFin = 7
}
```

### 🔴 دو یافتهٔ مهم که باید قبل از هر اصلاحی حل شوند

**یافتهٔ اول — تعداد مقادیر ۷ است، نه ۳.**
فرض اولیهٔ ما («۱=بستانکار، ۲=بدهکار، ۳=بد-بس تأییدشده است») **ناقص** بود. چهار مقدار ۴ تا ۷ (طی‌دوره / پایان‌دوره) در کامنت ستون Oracle ما اصلاً ذکر نشده‌اند. این با محدودیت `NUMBER(1)` سازگار است (تک‌رقمی).

**یافتهٔ دوم — ۱ و ۲ نسبت به کامنت ستون ما جابه‌جا هستند.**

| منبع | مقدار ۱ | مقدار ۲ |
|---|---|---|
| کامنت ستون Oracle در schema ما | **بستانکار** | **بدهکار** |
| `enum TypeActivity` در `Tamin.Core` | **بدهکار** (`Debit = 1`) | **بستانکار** (`Credit = 2`) |
| `enum TypeActivityGroup` در `Tamin.Core` | **بدهکار** (`Debit = 1`) | **بستانکار** (`Credit = 2`) |

یعنی **کد اجراشوندهٔ سیستم قدیمی با کامنت خودِ ستون در تناقض است.** دو enum مستقل در همان پروژه هر دو `Debit = 1` می‌گویند، پس احتمال تایپوی enum کم است و احتمال **کهنه/غلط بودن کامنت ستون Oracle** بیشتر — ولی **این را قطعی نمی‌کنم و حدس نمی‌زنم.**

> **⚠️ توصیهٔ صریح: پیش از هر migration یا مقداردهی مجدد، این تناقض باید با کوئری روی دادهٔ زندهٔ `CENTRALACCOUNT` حل شود.**
> یک روش قطعی و Read-Only: چند حساب معین با ماهیت بدیهی (مثلاً «بانک» یا «صندوق» که قطعاً بدهکار است، در برابر «حساب‌های پرداختنی» که قطعاً بستانکار است) را انتخاب و مقدار `TYPEACTIVITY` آن‌ها را بخوانید. اگر بانک `TYPEACTIVITY = 1` داشت، enum درست است و کامنت غلط.
> **این کوئری در این کار اجرا نشد** (دسترسی به دیتابیس زنده در scope این تحقیق نبود). ⚠️ **به‌روزرسانی ۲۰۲۶-۰۸-۲۶:** اجرا شد و تناقض حل شد — رجوع به بخش ۲۳-۱. نتیجه: `TYPEACTIVITY=1` = **بدهکار** (enum `Tamin.Core` درست است، کامنت ستون Oracle نادرست/کهنه است).

### `TypeActivityGroup` — یک enum دوم که فقط زیرمجموعه است

**فایل:** `D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeActivityGroup.cs` — سه مقدار `Debit=1, Credit=2, DebitCredit=3`.

**این enum روی هیچ پراپرتی Entity استفاده نشده** (در `AccountCode.cs` فقط `TypeActivity` هست). ولی محدودیت معادلش **به‌صورت دستی در Validator گروه اعمال شده**:

`ApplicationUseCases\Commands\AccountCodes\Group\Create\AddGroupCodeValidator.cs:26-29`
```csharp
RuleFor(x => x.TypeActivity)
.NotNull().WithMessage("ماهیت حساب اجباری می باشد")
.Must(value => (int)value >= 1 && (int)value <= 3)
.WithMessage("نوع انتخابی باید یکی از گزینه‌های 1 تا 3 باشد");
```

در حالی که برای معین کل بازه مجاز است —
`ApplicationUseCases\Commands\AccountCodes\Moin\Create\AddMoinCodeCommandValidator.cs:39-42`
```csharp
RuleFor(x => x.TypeActivity)
.NotNull().WithMessage("ماهیت حساب اجباری می باشد")
.Must(value => (int)value >= 1 && (int)value <= 7)
.WithMessage("نوع انتخابی باید یکی از گزینه‌های 1 تا 7 باشد");
```

**قاعدهٔ استخراج‌شده:**

| سطح | بازهٔ مجاز `TYPEACTIVITY` | اجباری؟ |
|---|---|---|
| گروه | **۱ تا ۳** | بله |
| کل | — (**اصلاً ست نمی‌شود**) | — |
| معین | **۱ تا ۷** | بله |

### آیا `TypeActivity` در محاسبهٔ مانده یا اعتبارسنجی سند استفاده می‌شود؟

**پاسخ: خیر — تأیید می‌کند که تصمیم فعلی ما درست است.**

- در **هیچ‌کدام** از فایل‌های مسیر ثبت سند (`AddVoucherValidator.cs`, `AddVoucherCommandHandler.cs`, `AddVoucherDetailValidator.cs`, `ChangeStateCommandhandler.cs`) هیچ ارجاعی به `TypeActivity` نیست.
- محاسبهٔ مانده در گزارش‌ها **بدون** توجه به ماهیت و همیشه به‌صورت `Debtor - Creditor` انجام می‌شود:
  - `Queries\Vouchers\Reports\Consolidates\Moein\ConsolidateMoeinQueryHandler.cs:36` → `Balance = g.Sum(x => x.Debtor) - g.Sum(x => x.Creditor)`
  - همین الگو در `Consolidates\Kol\ConsolidatesKolQueryHandler.cs:41` و `Consolidates\Group\ConsolidatesGroupQueryHandler.cs:42` و `ConsolidateReport1\GetConsoliDatereportDataForGridQueryHandler.cs:30,43,56`
- تنها مصرف `TypeActivity` سمت خواندن، **تبدیل به برچسب فارسی برای نمایش** است:
  - `Queries\AccountCodes\GetAllGroup\GetGroupCodeQueryHandler.cs:31` → `TypeActivity = g.TypeActivity.ToDescription()`
  - `Queries\AccountCodes\GetAllMoin\GetMoinCodeQueryHandler.cs:36` → `TypeActivity = g.TypeActivity?.ToDescription()`

> ✅ **نتیجه:** تصمیم فعلی ما در `CLAUDE.md` («`AccountNature` فقط برچسب گزارشی است و در اعتبارسنجی دخالت ندارد») **توسط سیستم قدیمی تأیید می‌شود.**

---

# بخش ۲ — منطق اعتبارسنجی سلسله‌مراتب گروه → کل → معین

## ۲-۱. طول و ساختار `ACCCODE` بر اساس سطح — **enforce می‌شود** ✅

هر سطح Validator مستقل خودش را دارد:

| سطح | فایل Validator | قاعدهٔ regex | معنی |
|---|---|---|---|
| گروه | `Group\Create\AddGroupCodeValidator.cs:19-22` | `^\d{2}$` + `NotEqual("00")` | دقیقاً ۲ رقم، و نمی‌تواند `00` باشد |
| کل | `Kol\Create\AddKolCodeCommandValidator.cs:18-21` | `^(?!00)\d{2}(?!00)\d{2}$` | دقیقاً ۴ رقم، دو رقم اول و دو رقم آخر نمی‌تواند `00` باشد |
| معین | `Moin\Create\AddMoinCodeCommandValidator.cs:25-34` | `^\d{6}$` + بررسی سه بخش | دقیقاً ۶ رقم، **هیچ‌کدام** از سه بخش دورقمی نمی‌تواند `00` باشد |

نقل مستقیم قاعدهٔ معین:
```csharp
RuleFor(x => x.AccCode)
 .NotNull().WithMessage("فیلد کد حساب اجباری می‌باشد")
 .Matches(@"^\d{6}$").WithMessage("کد معین باید دقیقا 6 رقم باشد")
 .Must(x =>
 {
     var part1 = x.Substring(0, 2);
     var part2 = x.Substring(2, 2);
     var part3 = x.Substring(4, 2);
     return part1 != "00" && part2 != "00" && part3 != "00";
 }).WithMessage("هیچ‌کدام از بخش‌های دو رقمی کد معین نمی‌تواند 00 باشد");
```

> ✅ **این دقیقاً همان ساختار ۲/۴/۶ رقمی است که صاحب پروژه در ۲۰۲۶-۰۸-۱۸ تأیید کرده بود** (مثال: گروه `11` → کل `1101` → معین `110101`).
> Validatorهای Update هم عیناً همین قواعد را دارند (`Group\Update\UpdateGroupCodeCommandValidator.cs`, `Kol\Update\UpdateKolCodeCommandValidator.cs:19-22`, `Moin\Update\UpdateMoinCodeValidator.cs:14-24`).

## ۲-۲. رابطهٔ `PARENTID` با `TYPECODE` — ⚠️ **enforce نمی‌شود**

این‌ها را جست‌وجو کردم و **پیدا نکردم**:

| قاعدهٔ موردانتظار | وضعیت در `CentralAccount` |
|---|---|
| والدِ یک «کل» باید `TypeCode == Group` باشد | ❌ **هیچ بررسی‌ای وجود ندارد** |
| والدِ یک «معین» باید `TypeCode == Kol` باشد | ❌ **هیچ بررسی‌ای وجود ندارد** |
| کد فرزند باید با کد والد شروع شود (مثلاً معین `110101` زیر کل `1101`) | ❌ **هیچ بررسی‌ای وجود ندارد** |

**شواهد:**
- `AddKolCodeCommandHandler.cs:30` فقط `account.SetParent(command.ParentId);` را صدا می‌زند — بدون هیچ load کردن والد یا بررسی نوعش.
- `AddMoinCodeCommandHandler.cs:22` همان‌طور.
- جست‌وجوی کامل برای الگوی پیشوند (`AccCode.StartsWith`, `Substring(0, 4)` روی `AccCode`) در `ApplicationUseCases`, `Infrastructure.Persistance.EF`, `Tamin.Core` انجام شد. **تنها نتیجهٔ مرتبط** در سمت **گزارش** است، نه اعتبارسنجی:
  `Queries\Vouchers\Reports\GeneralBalanceSheetReport\GeneralBalanceSheetQueryHandler.cs:57`
  ```csharp
  AccCode = v.DocsDetails.Select(d => d.AccountCode.AccCode.Substring(0, 4)).FirstOrDefault() ?? string.Empty,
  ```
  (یعنی کد کل را با بریدن ۴ رقم اول از کد معین استخراج می‌کند — که خودش **شاهد قوی** بر این است که قاعدهٔ پیشوند در **داده** برقرار است، حتی اگر در **کد** enforce نشود.)

## ۲-۳. اجباری بودن `PARENTID` — نیمه‌کاره (باگ در پروژهٔ خودشان)

در Validatorها بلوک‌های `When(...)` برای اجباری‌کردن والد وجود دارد:

`Moin\Create\AddMoinCodeCommandValidator.cs:70-74`
```csharp
When(a => a.TypeCode.Equals(TypeCodes.Moin), () =>
{
    RuleFor(a => a.AccCode).Length(6).WithMessage("کد معین باید 6 رقم باشد");
    RuleFor(p => p.ParentId).NotNull().NotEmpty().WithMessage("کد کل نمیتواند خالی باشد");
});
```

**اما این بلوک عملاً هرگز اجرا نمی‌شود.** دلیل: پراپرتی `TypeCode` روی خودِ Command از ورودی پر نمی‌شود. Controller آن را ست نمی‌کند —
`Presentaion.Web.API\Controllers\AccountCode\V1\AccountCodeController.cs:191`
```csharp
var ent = new AddMoinCodeCommand(dto.ParentId, dto.AccCode, dto.AccCodeName, dto.TypeActivity, dto.TypeAction, dto.LevelIds);
```
سازندهٔ Command هم `TypeCode` را ست نمی‌کند (`AddMoinCodeCommand.cs:11-21`)، پس مقدارش `default` یعنی `0` می‌ماند و `0 != TypeCodes.Moin (3)` → شرط همیشه false.

> **این یک باگ واقعی در پروژهٔ آن‌هاست، نه یک قاعدهٔ کسب‌وکاری.** ثبتش کردم تا اگر بعداً کسی این کد را کپی کرد، دام را بشناسد.

## ۲-۴. دو Validator که اصلاً wire نمی‌شوند (باگ دوم)

- `Moin\Update\UpdateMoinCodeValidator.cs:10` → `public class UpdateMoinCodeValidator : BaseCommandValidator<AccountCode>`
- `Delete\deleteAccountCodeValidator.cs:12` → `public class DeleteAccountCodeValidator : BaseCommandValidator<AccountCode>`

هر دو روی **`AccountCode` (خود Entity)** تعریف شده‌اند، نه روی Command متناظرشان. یعنی pipeline اعتبارسنجی (که دنبال `IValidator<UpdateMoinCodeCommand>` می‌گردد) هرگز آن‌ها را پیدا نمی‌کند. **کد معین در مسیر Update عملاً بدون اعتبارسنجی ذخیره می‌شود.**

> **درس برای ما:** اگر ساختار سه‌سطحی را بازسازی کردیم، حتماً یک تست بنویسیم که تأیید کند هر Command یک Validator ثبت‌شدهٔ متناظر دارد.

## ۲-۵. حذف گرهٔ کدینگ — **بررسی وابستگی کامل دارد** ✅

این مستقیماً به ریسک باز 🔴 ما («حذف گرهٔ کدینگ هیچ بررسی وابستگی ندارد») مربوط است. سیستم قدیمی این را **کامل** پیاده کرده:

**فایل:** `D:\CentralAccount\ApplicationUseCases\Commands\AccountCodes\Delete\DeleteAccountCodeHandler.cs:24-53`

```csharp
var entity = await _unitOfWork.accountCodeRepository.GetByIdForDeleteAsync(command.Id);
var countTafGroup = await _unitOfWork.tbAccountLinkTafsilGroupRepository.GetByIdAsync(command.Id);

if (entity.TypeCode == TypeCodes.Moin && entity != null)
{
    if (await _unitOfWork.vouchersDetailRepository.GetTuroverByIdAsync(command.Id) == true)
    {
        throw new AccountCodeException("کد حساب انتخابی گردش دارد و قابل حذف نمی باشد");
    }
    if (countTafGroup != null)
    {
        throw new AccountCodeException("کد حساب انتخابی با گروه تفصیلی ارتباط دارد و قابل حذف نمی باشد");
    }
}
if (entity.TypeCode == TypeCodes.Kol && entity != null && entity.Child.Count() > 0)
{
    throw new AccountCodeException("برای کد کل انتخابی کد معین تعریف شده است ابتدا زیرمجموعه های کد کل را حذف نمایید");
}
if (entity.TypeCode == TypeCodes.Group && entity != null && entity.Child.Count() > 0)
{
    throw new AccountCodeException("برای کد گروه انتخابی کد کل تعریف شده است ابتدا زیرمجموعه های کد گروه را حذف نمایید");
}
```

**قواعد استخراج‌شده (سه قاعدهٔ متمایز بر اساس سطح):**

| سطح | شرط منع حذف |
|---|---|
| **معین** | (الف) در ردیف‌های سند **گردش دارد** (`GetTuroverByIdAsync`)، یا (ب) به **گروه تفصیلی** لینک دارد (`TB_ACCOUNT_LINK_TAFSILGROUP`) |
| **کل** | حداقل یک **فرزند** (معین) دارد |
| **گروه** | حداقل یک **فرزند** (کل) دارد |

> **این دقیقاً همان چیزی است که ریسک 🔴 باز ما پیشنهاد می‌کرد** و حالا یک پیاده‌سازی مرجع آزموده برایش داریم.
>
> ⚠️ نکتهٔ ظریف: بررسی «گردش دارد» و «لینک تفصیلی» **فقط برای معین** انجام می‌شود، نه برای کل/گروه — که منطقی است چون فقط معین در ردیف سند به‌کار می‌رود.
>
> ⚠️ نکتهٔ ظریف دوم: قاعدهٔ فرزند برای **معین** بررسی نمی‌شود (معین برگ درخت است) و قاعدهٔ گردش برای **کل/گروه** بررسی نمی‌شود. یعنی می‌توان یک کل بدون فرزند را حذف کرد حتی اگر قبلاً فرزندانی داشته که خودشان گردش داشته‌اند — ولی چون فرزند نداشتنش شرط است، عملاً بی‌خطر است.

---

# بخش ۳ — قوانین اعتبارسنجی سند (invariantهای حسابداری)

این بخش مستقیماً به جدول **Accounting Safety Gate** در `CLAUDE.md` مربوط است.

## ۳-۱. 🔴 تراز بدهکار = بستانکار — **enforce نمی‌شود (کد کامنت شده است)**

**فایل:** `D:\CentralAccount\ApplicationUseCases\Commands\Vouchers\Voucher\Create\AddVoucherValidator.cs:53-69`

کد تراز **نوشته شده ولی کامل کامنت شده**:

```csharp
// RuleFor(x => x.AddVoucher.vouchersDetail)
//.Custom((details, context) =>
//{
//    if (details != null && details.Any())
//    {
//        var totalDebtor = details.Sum(d => d.Debtor);
//        var totalCreditor = details.Sum(d => d.Creditor);
//
//        if (totalDebtor != totalCreditor)
//        {
//            context.AddFailure("VouchersDetail", "جمع تمام فیلدهای بدهکار باید با جمع تمام فیلدهای بستانکار برابر باشد.");
//        }
//    }
//})
//.When(x => x.AddVoucher.vouchersHead.DocLife != DocLife.draft);
```

**دو نکتهٔ بسیار مهم در همین کد کامنت‌شده:**

1. **قصد طراحی روشن است:** تراز فقط وقتی باید بررسی می‌شد که سند **از حالت «یادداشت» (`draft`) خارج شده باشد** — یعنی سند در حالت پیش‌نویس می‌تواند نامتراز باشد. این یک تصمیم طراحی معنادار است، نه صرفاً یک قاعدهٔ ساده.
2. **در عمل غیرفعال است.** جست‌وجوی کامل روی solution هیچ بررسی تراز فعالی در **مسیر نوشتن** پیدا نکرد.

**تراز فقط یک مفهوم سمت خواندن/گزارش است:**
- `Infrastructure.Persistance.EF\Repositories\VouchersDetailRepository.cs:597` و `:642` — رشتهٔ `'تراز'` به‌عنوان **برچسب محاسبه‌شده در SQL گزارش** ظاهر می‌شود (`ELSE 'تراز'`).
- `Queries\Vouchers\VoucherHeadFolder\GetVoucherDetailForEdit\GetVoucherDetailForEditQueryHandler.cs:83` → `Remainder = list.Sum(b => b.Debtor) - list.Sum(a => a.Creditor)` — یعنی **اختلاف تراز به کاربر نمایش داده می‌شود** تا خودش تصمیم بگیرد.

> ✅ **نتیجه برای ما:** تصمیم معماری دوم ما (کنارگذاشتن تضمین تراز از سطح کد) **با رفتار واقعی سیستم قدیمی سازگار است**. اگر بعداً بخواهیم بازسازی‌اش کنیم، نقطهٔ درست از نظر سیستم قدیمی **گذار وضعیت از `draft` به بالاتر** است، نه لحظهٔ ساخت.

## ۳-۲. ✅ یک‌طرفه بودن بدهکار/بستانکار در هر ردیف — **enforce می‌شود**

**فایل:** `D:\CentralAccount\ApplicationUseCases\Commands\Vouchers\VoucherDetails\Create\AddVoucherDetailValidator.cs:39-49`

```csharp
RuleFor(x => x.Debtor)
    .NotNull().WithMessage("فیلد بدهکار اجباری است")
    .Must(x => x >= 0).WithMessage("فیلد بدهکار نمیتواند منفی باشد");

RuleFor(x => x.Creditor)
        .NotNull().WithMessage("فیلد بستانکار اجباری است")
         .Must(x => x >= 0).WithMessage("فیلد بستانکار نمیتواند منفی باشد");

RuleFor(x => x)
     .Custom((voucher, context) =>
     {
         if ((voucher.Debtor <= 0 && voucher.Creditor <= 0) || (voucher.Debtor > 0 && voucher.Creditor > 0))
         {
             context.AddFailure("Debtor and Creditor", "دوتا فیلد بدهکار و بستانکار همزمان نمی‌توانند صفر باشند و حداقل یکی باید بزرگتر از صفر باشد.");
         }
     });
```

**سه قاعدهٔ متمایز:**
1. هیچ‌کدام نمی‌تواند **منفی** باشد.
2. هر دو نمی‌توانند هم‌زمان **صفر** باشند.
3. هر دو نمی‌توانند هم‌زمان **مثبت** باشند (XOR واقعی).

> 🔴 **این مستقیماً یکی از ریسک‌های باز فاز ۱۰ ما را پوشش می‌دهد:** «هیچ چیز مانع نمی‌شود که یک ردیف هم‌زمان `DEBTOR` و `CREDITOR` غیرصفر داشته باشد.» سیستم قدیمی این را **صریحاً منع می‌کند** و یک پیاده‌سازی مرجع سه‌خطی دارد.

## ۳-۳. ✅ الزامی بودن تفصیلی — **حل شد! منبع حقیقت `TB_ACCOUNT_LINK_LEVEL` است**

> 🔴 **این مهم‌ترین یافتهٔ این تحقیق بعد از `TYPECODE` است** و یکی از تصمیمات باز 🔴 ما را می‌بندد:
> «**«الزامی بودن تفصیلی» در Legacy اصلاً مدل شده یا نه؟** ... هیچ ستون معادل `DetailRequirement` (الزامی/اختیاری) پیدا نشد.»
>
> **پاسخ: بله مدل شده — ولی نه با یک ستون، بلکه با «وجود یا نبودِ ردیف».**

**فایل:** `D:\CentralAccount\ApplicationUseCases\Commands\Vouchers\Voucher\Create\AddVoucherCommandHandler.cs:159-176`

```csharp
private void ValidateTafsiliLevels(VoucherDetailViewModel item, List<object> levels)
{
    for (int i = 1; i <= 7; i++)
    {
        var property = typeof(VoucherDetailViewModel).GetProperty($"Tafsili{i}Id");
        var value = property.GetValue(item);

        if (levels.Contains(i.ToString()) && value == null)
        {
            throw new LevelNotProvidedException($"برای کد معین  {item.AccountId}  سطح  {i}  اجباری است");
        }

        if (value != null && !levels.Contains(i.ToString()))
        {
            throw new UnauthorizedLevelException($"سطح {i} کد معین  {item.AccountId}  غیرمجاز پر شده است.");
        }
    }
}
```

فراخوانی‌اش در `AddVoucherCommandHandler.cs:144-145`:
```csharp
var levels = await _unitOfWork.tbAccountLinkLevelRepository.GetLevelIdByMoein(item.AccountId);
ValidateTafsiliLevels(item, levels);
```

و خودِ منبع داده — `D:\CentralAccount\Infrastructure.Persistance.EF\Repositories\TbAccountLinkLevelRepository.cs:73-81`:
```csharp
public virtual async Task<List<object>> GetLevelIdByMoein(Guid moeinId)
{
    return await _dbContext.AsNoTracking()
                           .Include(i => i.AccountCode)
                           .Include(i => i.TbLevelTafsil)
                           .Where(i => i.AccountCode.Id == moeinId)
                           .Select(i => (object)i.TbLevelTafsil.CodeLevel)
                           .ToListAsync();
}
```

### قاعدهٔ استخراج‌شده — دوطرفه و سختگیرانه

برای هر ردیف سند، برای هر سطح تفصیلی `i` از **۱ تا ۷**:

| وضعیت در `TB_ACCOUNT_LINK_LEVEL` | ورودی کاربر | نتیجه |
|---|---|---|
| ردیف **هست** (معین ↔ سطح i) | `null` | ❌ `LevelNotProvidedException` — «سطح i اجباری است» |
| ردیف **هست** | مقدار دارد | ✅ مجاز |
| ردیف **نیست** | `null` | ✅ مجاز |
| ردیف **نیست** | مقدار دارد | ❌ `UnauthorizedLevelException` — «سطح i غیرمجاز پر شده است» |

**یعنی: وجود یک ردیف در `TB_ACCOUNT_LINK_LEVEL` هم‌زمان «مجاز بودن» و «اجباری بودن» را می‌رساند.** هیچ حالت «مجاز ولی اختیاری» وجود ندارد. این توضیح می‌دهد چرا هیچ ستون `MUST`/`ISREQUIRED` در schema پیدا نکردیم — **لازم نبوده**.

### 🟡 این دو تصمیم باز ما را هم تحت تأثیر قرار می‌دهد

1. **«نقش واقعی `TB_ACCOUNT_LINK_LEVEL` — مشخص نیست فعال است یا artifact قدیمی»** → ✅ **کاملاً فعال است.** این جدول قلب اعتبارسنجی تفصیلی سند است.

2. ⚠️ **تنش با تصمیم قبلی ما دربارهٔ «منبع حقیقت تفصیلی».** ما در ۲۰۲۶-۰۸-۱۷ نتیجه گرفتیم `TB_ACCOUNT_LINK_TAFSILGROUP` منبع حقیقت است. این تحقیق نشان می‌دهد **هر دو جدول نقش دارند و نقششان متفاوت است**:

   | جدول | نقش (بر اساس کد `CentralAccount`) |
   |---|---|
   | `TB_ACCOUNT_LINK_LEVEL` | **کدام سطوح تفصیلی برای این معین اجباری/مجازند** (کنترل ساختاری، هنگام ثبت سند) |
   | `TB_ACCOUNT_LINK_TAFSILGROUP` | **کدام گروه تفصیلی به این معین وصل است** (در `DeleteAccountCodeHandler.cs:38-41` به‌عنوان مانع حذف استفاده می‌شود) |

   نتیجه‌گیری قبلی ما **باطل نمی‌شود**، ولی **ناقص بود**. برای مسیر نوشتن سند، `TB_ACCOUNT_LINK_LEVEL` منبع حقیقت است.

3. ✅ **`GetLevelIdByMoein` فیلتر `ISDELETED` صریح ندارد — ولی لازم هم ندارد.** ابتدا این را به‌عنوان باگ مشکوک ثبت کردم، سپس با خواندن `Infrastructure.Persistance.EF\Contexts\ApplicationDbContext.cs:79` مشخص شد یک **Global Query Filter** وجود دارد:
   ```csharp
   modelBuilder.Entity<TbAccountLinkLevel>().HasQueryFilter(a => !a.IsDeleted);
   ```
   یعنی EF Core خودکار `ISDELETED = 0` را به هر کوئری این Entity اضافه می‌کند. **این الگو بهتر از روش فعلی ماست** — رجوع به `docs/centralaccount-improvement-opportunities.md` مورد B-۱.

### حداکثر ۷ سطح تفصیلی

هر دو حلقه (`ValidateTafsiliLevels` خط ۱۶۱ و `CreateVoucherDetail` خط ۱۹۶) دقیقاً `for (int i = 1; i <= 7; i++)` هستند و روی پراپرتی‌های `Tafsili1Id` … `Tafsili7Id` کار می‌کنند. نگاشت شمارهٔ سطح به `LEVEL_ID` واقعی از طریق `TB_LEVEL_TAFSIL.CODELEVEL` انجام می‌شود (`AddVoucherCommandHandler.cs:201`):
```csharp
add.AddVouchersDetailLinkTafsil((Guid)value, levList.SingleOrDefault(a => a.CodeLevel == i.ToString()).Id, userid);
```

## ۳-۴. ❌ تغییرناپذیری سند پس از Post — **enforce نمی‌شود**

**enum وضعیت سند** — `D:\CentralAccount\Tamin.Core\Entities\Vouchers\DocLife.cs`:
```csharp
public enum DocLife
{
    [Description("یادداشت")]   draft      = 1,
    [Description("موقت")]      temporary  = 2,
    [Description("بررسی شده")] reviewed   = 3,
    [Description("تایید دائم")] accepted   = 4,
}
```

> ⚠️ **توجه:** ستون `TB_VOUCHERSHEAD.DOCLIFE` در schema ما `DEFAULT 0` دارد، ولی **`0` در این enum وجود ندارد.** یعنی یا مقدار پیش‌فرض DB نامعتبر است، یا معنایی خارج از enum دارد. ❓ **بررسی نشد و حدس نزدم.**

**فایل تغییر وضعیت:** `ApplicationUseCases\Commands\Vouchers\Voucher\ChangeState\ChangeStateCommandhandler.cs:17-32`

```csharp
var headList = await _unitOfWork.vouchersHeadRepository.GetByIdAsync(command.VoucherHeadIds);
for (var i = 0; i < headList.Count; i++)
{
    headList[i].SetDocLife(command.NewState);
    headList[i].SetChangeUserId(userid);
    headList[i].SetUpdatedDate(DateTime.Now);
}
```

و Validator متناظرش (`ChangeStateValidator.cs:13-16`) **فقط** بررسی می‌کند که مقدار داخل enum باشد:
```csharp
RuleFor(t => t.NewState)
   .IsInEnum().WithMessage("وضعیت سند نامعتبر می باشد");
```

**یعنی هیچ گارد گذار وضعیتی وجود ندارد:** می‌توان سند `accepted` (تایید دائم) را مستقیماً به `draft` برگرداند. هیچ بررسی تراز، هیچ بررسی نقش، و هیچ منع ویرایش سند نهایی‌شده.

> 🔴 این عیناً همان ریسک باز 🔴 ما است («سند Post شده حالا واقعاً قابل تغییر است»). **سیستم قدیمی هم آن را حل نکرده** — پس نمی‌توانیم از آن الگو بگیریم؛ اگر لازم است، باید خودمان طراحی کنیم.

## ۳-۵. ✅ یکتایی شمارهٔ سند — **enforce می‌شود (pre-check)**

`AddVoucherCommandHandler.cs:86-95`:
```csharp
bool exist = await _unitOfWork.vouchersHeadRepository.VoucherNoExistsAsync(
    request.AddVoucher.vouchersHead.DocNumber,
    request.AddVoucher.vouchersHead.VahedCode,
    request.AddVoucher.vouchersHead.Year
);

if (exist)
{
    throw new DuplicateException("این شماره سند از قبل وجود دارد");
}
```

**دامنهٔ یکتایی: (شمارهٔ سند + کد واحد + سال مالی)** — نه یکتایی سراسری.
⚠️ این یک check-then-act است (همان شرایط رقابتی که ما هم داریم). ما این را با نگاشت ORA-00001 → 409 حل کرده‌ایم که **از این بهتر است**.

## ۳-۶. سایر قواعد سند که فقط در سیستم قدیمی هست

از `AddVoucherValidator.cs`:

| قاعده | خط | جزئیات |
|---|---|---|
| شمارهٔ سند فقط عدد، ۵ یا ۶ رقم | ۲۰-۲۳ | `Matches("^[0-9]{1,}$")` + `Length(5, 6)` |
| تاریخ سند ۸ کاراکتر و **تاریخ شمسی معتبر** | ۲۵-۲۹ | با `PersianCalendar` واقعاً parse می‌شود |
| تاریخ سند **نمی‌تواند از امروز بزرگ‌تر باشد** | ۲۸ | `NotBeFutureDate` |
| سال تاریخ سند باید **دقیقاً برابر سال مالی** باشد | ۲۹-۳۱ | `NotBeFromPreviousYear` |
| سال مالی: ۴ رقم عددی | ۴۱-۴۴ | |
| کد واحد: **دقیقاً ۴ رقم عددی** | ۴۶-۴۹ | |
| شرح سند اجباری | ۳۳-۳۴ | |

از `AddVoucherCommandHandler.cs:67-84` — **قاعدهٔ بستن ماه:**
```csharp
if (month != currentMonth)
{
    validDateDoc = await _unitOfWork.vouchersHeadRepository.GetVouchersDocLife(...);
}
if (!validDateDoc)
{
    throw new CustomException("امکان ثبت سند در این ماه بدلیل وجود سند صورتحساب وجود ندارد.");
}
```
> این نزدیک‌ترین چیز به مفهوم **«دورهٔ بسته»** در Accounting Safety Gate ما است: اگر برای ماهی «سند صورتحساب» صادر شده باشد، ثبت سند عادی در آن ماه ممنوع می‌شود. ❓ جزئیات `GetVouchersDocLife` را نخواندم — نیاز به بررسی بیشتر اگر خواستیم این را پیاده کنیم.

## ۳-۷. مرز تراکنش در سیستم قدیمی — ⚠️ **الگوی بد، تقلید نکنیم**

`AddVoucherCommandHandler.cs` سه بار `CommitAsync()` صدا می‌زند **داخل** یک `BeginTransaction`:
- خط ۲۴: `_unitOfWork.BeginTransaction();`
- خط ۱۲۱: `await _unitOfWork.CommitAsync();` (بعد از سرسند)
- خط ۱۵۶: `await _unitOfWork.CommitAsync();` (بعد از هر ردیف)
- خط ۵۶: `_unitOfWork.Commit();` (تراکنش نهایی)

> ✅ **معماری فعلی ما (یک `SaveChangesAsync` در Handler) بهتر و تمیزتر است.** این را فقط برای ثبت تفاوت آوردم.

همچنین `catch (Exception ex) { _unitOfWork.Rollback(); throw new CustomException(ex.Message); }` در خط ۵۹-۶۳ — **پیام خام استثنا را به بیرون می‌دهد**، که یک نشت اطلاعات است. ما این را با `GlobalExceptionHandler` بهتر حل کرده‌ایم.

---

# بخش ۴ — ساختار solution

`D:\CentralAccount\Tamin.BaseTemplate.sln` — ۱۰ پروژه:

| پروژه | مسیر | نقش |
|---|---|---|
| `Domain` | `Tamin.Core\Domain.csproj` | **Domain** — Entityها، enumها، Exceptionها، Services. namespace ریشه: `Domain.*` |
| `ApplicationUseCases` | `ApplicationUseCases\` | **Application/CQRS** — Commands، Queries، Validators، Dtos، Behaviors، Abstractions |
| `Persistance.EF` | `Infrastructure.Persistance.EF\` | **Infrastructure** — `ApplicationDbContext`، Repositories، `UnitOfWork` |
| `Web.API` | `Presentaion.Web.API\` | **Presentation** — Controllers، DtoModels، Filter، Middlewares، Roles |
| `Shared` | `Shared\` | `Result<T>` و تایپ‌های مشترک |
| `IDP` | `IDP\` | احراز هویت / `TokenManager` (مبدأ کدی که ما در فاز ۷ پورت کردیم) |
| `Infrastructure.Logging` | `Infrastructure.Logging\` | لاگ |
| `Infrastructure.Service` | `Infrastructure.Service\` | سرویس‌های جانبی |
| `Accounts.Tests` | `Account.Tests\` | تست |
| — | `Presentation.Worker\`, `Payments\`, `SeriLog\`, `Logging\`, `Utility.Exception\` | پوشه‌هایی که **در فایل `.sln` نیستند** |

**زیرپوشه‌های `Tamin.Core` (Domain):** `Common`, `Entities`, `Exceptions`, `Services`
**زیرپوشه‌های `ApplicationUseCases`:** `Abstractions`, `Behaviors`, `Commands`, `Common`, `Dtos`, `Queries`, `Template`, `Registration.cs`

> ✅ **معماری‌شان دقیقاً همان الگوی ماست** (Clean Architecture + CQRS + MediatR + FluentValidation + Repository/UnitOfWork). این باعث می‌شود انتقال قواعد کسب‌وکار بین دو پروژه کم‌اصطکاک باشد.

## دامنهٔ ماژول‌ها (پوشه‌های `Commands`)

`AccountCodes`, `Accounts`, `Attributes`, `BankCartDetails`, `Banks`, `BillLogs`, `ChargeAndCosts`, `Checks`, `Cities`, `Elms`, `Expenses`, `FinanceYear`, `Identitys`, `PayAndRecive`, `PersonActions`, `PreDescribes`, `Provinces`, `RabetClosings`, `RabetTypes`, `Rabets`, `Receipts`, `RevolvingFunds`, `Sample`, `Tafsilis`, `TbAccountLinkLevels`, `TbAccountLinkTafsilGroups`, `TbLevelTafsils`, `TbTafsilGroups`, `TbTafsilLinkTafsilGroups`, `VahedInfos`, `VahedTypes`, `Vouchers`, `WhiteAndBlackLists`, `WhiteList`, `WorkShops`

## احراز هویت و نقش‌ها

`Presentaion.Web.API\Controllers\AccountCode\V1\AccountCodeController.cs` از `Tamin.Framework.Common.Security` و `RolesAllowedAttribute` استفاده می‌کند — **همان پکیجی که ما در فاز ۷ به کار بردیم.** نقش‌ها در `Presentaion.Web.API\Roles\`:

`FINANCIAL_CORE_SETAD_ADMIN`, `FINANCIAL_CORE_EDK_ADMIN`, `FINANCIAL_CORE_HLT_ADMIN`, `FINANCIAL_CORE_MALI_ADMIN`, `FINANCIAL_CORE_USER`, `FINANCIAL_CORE_IT`

**الگوی تفکیک دسترسی (شاهد مستقیم):**
- **خواندن** → همهٔ ۶ نقش (خطوط ۵۱، ۶۰، ۶۸، ۷۶، ۸۴، ۹۱، …)
- **نوشتن/حذف کدینگ** → **فقط `FINANCIAL_CORE_SETAD_ADMIN`** (خطوط ۱۸۵، ۱۹۵، ۲۰۴، ۲۱۳، ۲۲۲، ۲۳۱، ۲۴۰)

> 🔴 **این مستقیماً به ریسک باز 🔴 IDOR ما مربوط است.** سیستم قدیمی حداقل یک لایهٔ authorization مبتنی بر نقش دارد که ما نداریم. ⚠️ ولی توجه: این **role-based** است نه **record-based** — یعنی حتی سیستم قدیمی هم ایزولاسیون بین `VAHEDCODE`ها را در این Controller اعمال نمی‌کند. ❓ آیا در جای دیگری (مثلاً `businessUserAccessRepository`) اعمال می‌شود؟ **بررسی نکردم.**

**نکتهٔ مثبت دیگر:** تمام Endpointهای نوشتن `[HttpPost]` اند (`AddMoinCodeAsync`, `UpdateMoinCodeAsync`, `DeleteAccountCodeAsync`) — یعنی **محدودیت «فقط GET/POST» صاحب پروژه در فاز ۸ ما با پروژهٔ اصلی خودشان هم‌راستاست.** الگوی نام‌گذاری route هم `api/[controller]/[action]` است.

## هویت کاربر

الگوی ثابت در همهٔ Handlerها:
```csharp
var userid = _unitOfWork.businessUserAccessRepository.UserId;
account.SetAddUserId(userid);
```
> ✅ **دقیقاً همان کاری که ما با `ICurrentUser.UserId` کردیم.** در `AddVoucherCommandHandler.cs:119` حتی می‌بینیم که نسخهٔ قدیمیِ «گرفتن از ورودی» کامنت شده:
> ```csharp
> //  add.SetAddUserId(request.AddVoucher.vouchersHead.AddUserId);
> ```
> یعنی آن‌ها هم همین مسیر اصلاحی را طی کرده‌اند.

---

# بخش ۵ — مرزهای Aggregate: پاسخ به ۵ جفت Head/Detail باز

در `docs/tamin-core-entity-reference.md` بخش ۵، مرز این ۵ جفت **حل‌نشده** مانده بود چون فقط Entity در دسترس بود. حالا با دسترسی به لایهٔ Application، **پوشهٔ Commands شاهد قطعی است**.

## روش استنتاج

اگر یک Detail پوشهٔ `Commands` مستقل با Create/Update/Delete خودش دارد → **Aggregate Root مستقل**.
اگر فقط داخل Handler والدش ساخته می‌شود → **تعبیه‌شده**.

## نتایج

| جفت | مسیر Commands | نتیجه |
|---|---|---|
| **`VoucherHead` / `VouchersDetail`** | `Vouchers\VoucherHeads\{Create,update,Delete}` **و** `Vouchers\VoucherDetails\{Create,CreateDetail,Update,Delete}` **و** `Vouchers\Voucher\Create` (ترکیبی) | ✅ **مدل ترکیبی** |
| **`ElamHead` / `ElamDetail`** | `Elms\OtherElamHead\{Create,Update,Delete,ChangeWebStat}` **و** `Elms\OtherElamDetail\{Create,Update,Delete}` | ✅ **مستقل** |
| **`PayReciveHead` / `PayReciveDetail`** | `PayAndRecive\{Create,Update,Delete}` **و** `PayAndRecive\PayAndreciveDetails\{CreatedetailSingle,UpdateDetailSingle,DeleteDetailSingle}` | ✅ **مستقل** |
| **`ChargeAndCostHead` / `ChargeAndCostDetail`** | `ChargeAndCosts\Head\{Update,Delete,ChangeStatusTo*}` **و** `ChargeAndCosts\Detail\{Create,Update,Delete,DeleteSingle}` **و** `ChargeAndCosts\ChargeAndCost\Create` (ترکیبی) | ✅ **مدل ترکیبی** |
| **`TmpVoucherHead` / `TmpVoucherDetail`** | فقط `Vouchers\TmpVouchers\{CreateTmpHead,CreateVouchers,Delete}` — **هیچ پوشهٔ Detail مستقلی نیست** | ❌ **تعبیه‌شده** |
| **`IdentityHead` / `IdentityDetail`** | `Identitys\IdentityHeads\Create` + `Identitys\IdentityDetails\` که **فقط `Update` دارد** (نه Create نه Delete) | ⚠️ **تعبیه‌شده با روزنهٔ Update** |

### شواهد جزئی

**`TmpVoucherDetail` تعبیه‌شده است** — `Commands\Vouchers\TmpVouchers\CreateTmpHead\AddTmpVoucherHeadCommandHandler.cs`:
```
خط 32:  List<TmpVoucherDetail> details = new List<TmpVoucherDetail>();
خط 33:  foreach (var item in command.Details)
خط 35:      TmpVoucherDetail det = new();
خط 58:  var newEntity = await _unitOfWork.tmpVoucherHeadRepository.AddAsync(head);
```
یک `AddAsync` روی **سرسند**، ردیف‌ها به‌عنوان بخشی از graph ذخیره می‌شوند. **دقیقاً همان الگوی composite create ما.**

**`IdentityDetail` تعبیه‌شده است** — در `AddVoucherCommandHandler.cs:203-209`:
```csharp
if (item.identityDetailDtos != null)
{
    foreach (var det in item.identityDetailDtos)
    {
        add.AddIdentityDetail(det.HeadId, det.SubGroupId, det.value, userid);
    }
}
```
یعنی از طریق **ردیف سند** ساخته می‌شود، نه Command مستقل. فقط `Identitys\IdentityDetails\UpdateIdentityDetailCommand.cs` برای ویرایش وجود دارد.

> ✅ **تأیید تصمیم صاحب پروژه:** الگوی غالب در سیستم قدیمی همان **«مدل ترکیبی»** است که ایشان در ۲۰۲۶-۰۸-۲۰ برای `TB_VOUCHERSDETAIL` انتخاب کردند — ساخت اولیه به‌صورت composite با سرسند، به‌علاوهٔ CRUD مستقل برای ویرایش بعدی. **فاز ۱۰ ما با پروژهٔ اصلی هم‌راستاست.**

## دو Entity مبهم (بخش ۴ سند قبلی)

| Entity | یافته | نتیجه |
|---|---|---|
| **`RabetClosing`** | پوشهٔ کامل `Commands\RabetClosings\{Create,Update,Delete}` دارد | ✅ **Aggregate Root مستقل — CRUD مستقل معقول است** |
| **`ChargeLinkCost`** | **هیچ پوشهٔ Command ندارد.** فقط: `Abstractions\Repositories\ChargeAndCosts\IChargeLinkCostRepository.cs`، `Infrastructure.Persistance.EF\Repositories\ChargeLinkCostRepository.cs`، ثبت در `UnitOfWork.cs` و `Registration.cs` و `ApplicationDbContext.cs` | ⚠️ **هیچ مسیر نوشتن مستقلی ندارد** |

**دربارهٔ `ChargeLinkCost`:** Repository دارد ولی هیچ Command/Controller مصرف‌کننده‌ای ندارد. یعنی یا فقط برای خواندن استفاده می‌شود، یا داخل Handler دیگری مصرف می‌شود.
❓ **محتوای `ChargeLinkCostRepository.cs` و مصرف‌کنندگانش را نخواندم — نیاز به بررسی بیشتر.** طبق قاعدهٔ `CLAUDE.md`، پیش از ساخت CRUD برای `TB_CHARGE_LINK_COST` باید از صاحب پروژه پرسیده شود. **این یافته آن ابهام را کم می‌کند ولی قطعی‌اش نمی‌کند.**

---

# بخش ۶ — تفاوت‌های نوع داده که باید حل شوند

| مفهوم | `CentralAccount` | مدل فعلی ما | وضعیت |
|---|---|---|---|
| `TYPECODE` | `TypeCodes` (enum، ۱-۳) | `bool?` | 🔴 **غلط — باید اصلاح شود** |
| `TYPEACCCODE` | `TypeAccCode?` (enum، ۱-۲) | `bool?` | 🔴 **غلط** |
| `TYPEACTION` | `TypeAction?` (enum، ۱-۳) | `bool?` | 🔴 **غلط** |
| `TYPEACTIVITY` | `TypeActivity?` (enum، ۱-۷) | `bool?` | 🔴 **غلط + تناقض ۱/۲ با کامنت** |
| `DOCLIFE` | `DocLife` (enum، ۱-۴) | ❓ بررسی نکردم | ⚠️ احتمالاً همین مشکل |
| `Debtor` / `Creditor` | `long` غیر-nullable | `decimal?` | 🟡 تصمیم باز موجود ما — **این تحقیق `long` را دوباره تأیید کرد** |
| `RADIF` | `long` (`SetRadif(radif)` با `long radif = 1`) | `int?` | 🟡 تفاوت جزئی |
| `PARENTID` | `Guid?` | `Guid?` | ✅ سازگار |

> ⚠️ **دربارهٔ `NUMBER(1)`:** هر چهار ستون در Oracle `NUMBER(1)` اند و بزرگ‌ترین مقدار enum برابر ۷ است — پس **عرض ستون کافی است** و هیچ تغییر DDL لازم نیست. فقط نگاشت سمت کد باید عوض شود.

---

# بخش ۷ — تأثیر بر تصمیمات باز `CLAUDE.md`

| تصمیم باز در `CLAUDE.md` | تأثیر این تحقیق |
|---|---|
| 🔴 «الزامی بودن تفصیلی در Legacy مدل شده یا نه؟» | ✅ **حل شد** — بله، از طریق وجود ردیف در `TB_ACCOUNT_LINK_LEVEL` (بخش ۳-۳) |
| 🟡 «نقش واقعی `TB_ACCOUNT_LINK_LEVEL`» | ✅ **حل شد** — کاملاً فعال، قلب اعتبارسنجی تفصیلی سند |
| 🟡 «نوع دادهٔ مبلغ: `long` در برابر `decimal?`» | ⬆️ **تقویت شد** — کل solution از `long` استفاده می‌کند |
| 🔴 «حذف گرهٔ کدینگ هیچ بررسی وابستگی ندارد» | ✅ **الگوی مرجع پیدا شد** (بخش ۲-۵) |
| 🔴 «تضمین تراز (Debit == Credit)» | ⚠️ سیستم قدیمی هم enforce نمی‌کند (کد کامنت‌شده)، **ولی قصد طراحی: فقط وقتی `DocLife != draft`** |
| 🔴 «سند Post شده حالا واقعاً قابل تغییر است» | ⚠️ سیستم قدیمی هم حل نکرده — الگویی برای کپی نداریم |
| 🔴 «ردیف می‌تواند هم‌زمان بدهکار و بستانکار باشد» | ✅ **الگوی مرجع پیدا شد** (بخش ۳-۲) |
| 🔴 «IDOR» | ⚠️ سیستم قدیمی **role-based** دارد (فقط `SETAD_ADMIN` می‌نویسد) ولی record-based ندارد |
| 🟡 «سلسله‌مراتب ثابت سه‌سطحی» | ⚠️ **نیمه** — طول کد enforce می‌شود (۲/۴/۶)، ولی رابطهٔ نوعِ والد-فرزند و پیشوند کد **نه** |
| 🟡 «`AccountNature` فقط برچسب گزارشی است» | ✅ **تأیید شد** |
| ✅ مرز Aggregate Head/Detail | ✅ **تأیید شد** — الگوی ترکیبی صاحب پروژه با سیستم قدیمی هم‌راستاست |

---

# بخش ۸ — آنچه بررسی نشد (صریح)

برای شفافیت، این موارد **خوانده نشدند** و هیچ ادعایی دربارهٔ آن‌ها ندارم:

1. **فرانت‌اند** — خارج از این solution. اگر `TypeAction` جایی enforce شود، احتمالاً آنجاست.
2. **تریگرها/پکیج‌های سمت Oracle** — هیچ دسترسی/بررسی‌ای نداشتم.
3. **پوشه‌های خارج از `.sln`**: `Presentation.Worker`, `Payments`, `SeriLog`, `Logging`, `Utility.Exception`.
4. **اکثر ماژول‌های `Commands`** — فقط `AccountCodes` و `Vouchers` را عمیق خواندم. ۳۰+ ماژول دیگر فقط در سطح نام پوشه بررسی شدند.
5. **`Behaviors`، `Abstractions`، `Common` در `ApplicationUseCases`** — pipeline اعتبارسنجی را از روی الگوی `BaseCommandValidator` استنتاج کردم، ولی `Registration.cs` را نخواندم.
6. **`GetVouchersDocLife`** (قاعدهٔ بستن ماه) و **`GetTuroverByIdAsync`** (بررسی گردش) — فقط محل فراخوانی‌شان را دیدم، نه پیاده‌سازی‌شان.
7. **`ChargeLinkCostRepository.cs`** و مصرف‌کنندگانش.
8. **`Account.Tests`** — فقط در فهرست فایل‌ها دیده شد.
9. **دادهٔ زندهٔ Oracle** — هیچ کوئری‌ای اجرا نشد. تناقض `TYPEACTIVITY` (۱/۲) **حل‌نشده باقی است.** ⚠️ **به‌روزرسانی ۲۰۲۶-۰۸-۲۶:** حل شد — رجوع به بخش ۲۳.
10. **`businessUserAccessRepository`** — آیا ایزولاسیون `VAHEDCODE` را اعمال می‌کند؟ بررسی نشد.

---

# بخش ۹ — اقدامات پیشنهادی (تصمیم با صاحب پروژه)

**هیچ کدی در `backend/` تغییر نکرد.** این‌ها صرفاً پیشنهادند:

1. 🔴 **اصلاح چهار پراپرتی `bool?` در `backend/src/Accounting.Domain/Entity/TB_ACCOUNTCODE.cs`.**
   - قبلش باید تصمیم گرفته شود: enum واقعی، یا `byte?`/`short?`؟ (نکته: enum در `Accounting.Domain` مجاز است چون وابستگی خارجی ندارد.)
   - ⚠️ **پیش‌نیاز:** تناقض `TYPEACTIVITY` (۱=بدهکار یا ۱=بستانکار؟) باید اول با کوئری Read-Only روی دادهٔ زنده حل شود.
   - ⚠️ **بررسی داده:** آیا در `TB_ACCOUNTCODE` فعلی مقداری خارج از بازهٔ enum هست؟ (مثلاً `TYPECODE = 0`) — چون `bool?` فعلی هر مقدار غیرصفر را احتمالاً `true` می‌خواند و ما ممکن است تا الان داده را غلط خوانده باشیم.

2. 🟡 **بررسی همین باگ در بقیهٔ ۶۴ Entity** — هر ستون `NUMBER(1)` که به `bool?` اسکفولد شده مشکوک است (`TB_VOUCHERSHEAD.DOCLIFE` قطعاً یکی از آن‌هاست، چون `DocLife` چهار مقدار دارد).

3. 🟢 **قواعد آمادهٔ کپی** (اگر صاحب پروژه بخواهد): طول کد ۲/۴/۶ + منع `00`، XOR بدهکار/بستانکار، بررسی وابستگی حذف کدینگ، اعتبارسنجی تفصیلی از `TB_ACCOUNT_LINK_LEVEL`.

4. ❓ **سؤال باز برای صاحب پروژه:** آیا `TYPEACTION` (کنترل خلاف ماهیت) باید در سیستم جدید واقعاً پیاده شود؟ در سیستم قدیمی ذخیره می‌شود ولی هرگز اعمال نمی‌شود.

---
---

# 🔴 بخش ۱۰ — ممیزی کامل enumها: باگ `bool?` بسیار بزرگ‌تر از `TB_ACCOUNTCODE` است

> **افزوده‌شده در پاس دوم (۲۰۲۶-۰۸-۲۵).** در پاس اول فقط چهار ستون `TB_ACCOUNTCODE` بررسی شد. این بخش نتیجهٔ **ممیزی کامل هر ۴۴ فایل enum** در `Tamin.Core` و تطبیق یک‌به‌یک با **هر ۴۲ پراپرتی `bool?`** در `backend/src/Accounting.Domain/Entity/` ماست.

## ۱۰-۱. روش

1. همهٔ enumهای Domain آن‌ها فهرست شد: `grep -rln "public enum"` روی `D:\CentralAccount\Tamin.Core` → **۴۴ فایل**.
2. همهٔ پراپرتی‌های `bool?` ما فهرست شد: `grep -rn "public bool? "` روی `Accounting.Domain/Entity/` → **۴۲ مورد**.
3. برای هر مورد، نوع ستون و **کامنت واقعی Oracle** از Fluent Mapping ما (`Accounting.Infrastructure/Legacy/LegacyDbContext.cs`) استخراج شد.
4. با Entity متناظر در `Tamin.Core` تطبیق داده شد.

## ۱۰-۲. نتیجه — جدول کامل ۴۲ پراپرتی `bool?` ما

**۱۹ مورد از ۴۲ مورد بولین نیستند.** (۲۱ مورد `ISDELETED` واقعاً بولین‌اند، ۲ مورد تأییداً بولین‌اند.)

| # | ستون ما | نوع Oracle | کامنت Oracle | enum واقعی در `Tamin.Core` | مقادیر | حکم |
|---|---|---|---|---|---|---|
| ۱ | `TB_ACCOUNTCODE.TYPECODE` | `NUMBER(1)` | — | `TypeCodes` | ۱گروه ۲کل ۳معین | 🔴 **غلط** |
| ۲ | `TB_ACCOUNTCODE.TYPEACTIVITY` | `NUMBER(1)` | «۱بستانکار۲بدهکار۳بد-بس» | `TypeActivity` | **۱..۷** | 🔴 **غلط + تناقض — ✅ تناقض با دادهٔ زنده حل شد (بخش ۲۳): ۱=بدهکار، کامنت Oracle نادرست است** |
| ۳ | `TB_ACCOUNTCODE.TYPEACCCODE` | `NUMBER(1)` | «۱موقت۲دائم» | `TypeAccCode` | ۱موقت ۲دائم | 🔴 **غلط** |
| ۴ | `TB_ACCOUNTCODE.TYPEACTION` | `NUMBER(1)` | «کنترل‌نشود-اخطار-ثبت‌نشود» | `TypeAction` | ۱،۲،۳ | 🔴 **غلط** |
| ۵ | **`TB_VOUCHERSHEAD.DOCLIFE`** | `NUMBER(1)` **DEFAULT 0** | «وضعيت سند» | **`DocLife`** | **۱یادداشت ۲موقت ۳بررسی‌شده ۴تایید‌دائم** | 🔴 **غلط — تأیید حدس پاس اول** |
| ۶ | `TB_TAFSILI.VAHEDTYPE` | `NUMBER(1)` | — | `TypeVahed`؟ | **۱..۱۷** | 🔴 **غلط + ناسازگاری عرض ستون — ⚠️ بخش ۲۳: عرض فیزیکی (`DATA_PRECISION=1`) تأیید شد، ولی داده فقط مقادیر {۱,۳} دارد؛ تطبیق با `TypeVahed` هنوز قطعی نیست** |
| ۷ | `TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE` | `NUMBER(1)` | — | `TypeVahed`؟ | **۱..۱۷** | 🔴 **غلط + ناسازگاری عرض — ⚠️ بخش ۲۳: همان وضعیت ردیف ۶** |
| ۸ | `TB_TAFSILI.PERSONTYPE` | `NUMBER(1)` **DEFAULT 0** | — | `PersonTypes` | ۱حقیقی ۲حقوقی ۳سایر | 🔴 **غلط** |
| ۹ | `TB_TAFSIL_GROUP.PERSONTYPE` | `NUMBER(1)` | — | `PersonTypes` | ۱،۲،۳ | 🔴 **غلط** |
| ۱۰ | `TB_TAFSILI.ISACTIVE` | `NUMBER(1)` **DEFAULT 1** | — | **`Active`** | **۱فعال ۲غیرفعال** | 🔴 **غلط — خطرناک‌ترین مورد** |
| ۱۱ | `TB_TAFSILI.OWNER` | `NUMBER(1)` **DEFAULT 1** | **«2=setad 1=vahed»** | **`Owners`** | **۱سراسری ۲داخلی** | 🔴 **غلط + تناقض** |
| ۱۲ | `TB_WHITEANDBLACKLIST.STATE` | `NUMBER(1)` | — | `StateEnum` | ۱مجاز ۲فقط‌سیستمی ۳غیرمجاز | 🔴 **غلط** |
| ۱۳ | `TB_RABET_CLOSING.TYPEACCOUNTCODE` | `NUMBER(1)` | — | **`TypeAccountCode`** | **۲کل ۳معین** (شروع از ۲!) | 🔴 **غلط** |
| ۱۴ | `TB_PAYRECIVHEAD.PAYRECIVTYPE` | `NUMBER(1)` | — | `PayRecivType` | ۱پرداخت ۲دریافت ۳همه | 🔴 **غلط** |
| ۱۵ | `TB_ELAMHEAD.ELAMH_CASE` | `NUMBER(1)` | **«نوع اعلاميه 1بد 2بس»** | `ElamCase` | ۱بدهکار ۲بستانکار | 🔴 **غلط** (ولی کامنت و enum موافق‌اند) |
| ۱۶ | `TB_ELAMHEAD.ELAMHDRAMAD_TYPE` | `NUMBER(1)` | «۱ذي‌حسابي ۲سايردرآمد ۳حق‌بيمه» | `DaramElamhType` | ۱،۲،۳ | 🔴 **غلط + تناقض ترتیب** |
| ۱۷ | `TB_IDENTITYSUBGRP.SUBGRPS_TYPE` | `NUMBER(1)` | «نوع: حروف, اعداد, يا هردو» | `IdentitySubGroupType` | ۱تاریخ ۲فارسی ۳عدد ۴لاتین | 🔴 **غلط + تناقض** |
| ۱۸ | `TB_BANKCARTDETAIL.CHECKRECEIPTTYPE` | `NUMBER(1)` | «نوع مدرك بانكي (فيش يا حواله)» | `CheckReceiptType` | ۱صوری ۲واقعی ۳فیش ۴حواله | 🔴 **غلط + تناقض** |
| ۱۹ | `TB_CHECKBOOK.CHECKBOOK_TYPE` | `NUMBER(1)` | — | `CheckType` | ۱صوری ۲واقعی | 🔴 **غلط** |
| ۲۰ | `TB_ATTRIBFORACCOUNTCODE.CONTROLID` | `NUMBER(1)` **NOT NULL** | — | `ControlEnum`؟ | ۱غیرصفر ۲تاریخ | 🟡 **مشکوک** |
| ۲۱ | `TB_VOUCHERSHEAD.ISAUTOMATIC` | `NUMBER(1)` | **«0دستي و 1 مکانيزه»** | `IsAutomatic` | ۰،۱ | ✅ **`bool?` قابل‌قبول — ⚠️ بخش ۲۳: باگ محتمل ۱۰-۷ با دادهٔ زنده تأیید شد (همبستگی قوی با `SYSTEM_TYPE`)** |
| ۲۲ | `TB_PREDESCRIB.FLAGVOUCHER` | `NUMBER(1)` | **«head=0 Detail=1»** | `Flag` | ۰،۱ | ✅ **`bool?` قابل‌قبول** |
| ۲۳ | `TB_PERSON_ACTION.STATUS` | `NUMBER(1)` | — | **در خودشان هم `bool?` است** | — | ✅ **درست** |
| ۲۴ | `TB_YEAR.ISCURRENT` | `NUMBER(1)` DEFAULT 0 | — | ❓ معادل پیدا نشد | — | 🟡 **احتمالاً درست** |
| ۲۵-۴۲ | ۲۱ مورد `ISDELETED` | `NUMBER(1)` | — | در خودشان `bool` است | ۰،۱ | ✅ **درست** |

> **شاهد `PersonAction`:** `Tamin.Core\Entities\PersonActions\PersonAction.cs:33` → `public bool? Status { get; private set; }`. **تنها موردی که مثبت تأیید شد.** (`OperatorRole` یک پراپرتی **جداگانه** روی همان Entity است، خط ۳۷ — اشتباه گرفته نشود.)
> **شاهد `CheckBook`:** `Tamin.Core\Entities\Checks\CheckBook.cs:38` → `public CheckType CheckBookType { get; private set; }`.

## ۱۰-۳. 🔴 خطرناک‌ترین مورد: `TB_TAFSILI.ISACTIVE`

`enum Active { IsActive = 1 (فعال), DeActive = 2 (غیرفعال) }` روی ستون `NUMBER(1) DEFAULT 1`.

**چرا بدترین مورد فهرست است:** نام ستون (`ISACTIVE`) و مقدار پیش‌فرض (`1`) هر دو **دقیقاً شبیه یک بولین معمولی به‌نظر می‌رسند**. هر خواننده‌ای — و خودِ EF Scaffold — فرض می‌کند `1=true=فعال` و `0=false=غیرفعال`. **ولی `0` اصلاً در این enum وجود ندارد؛ «غیرفعال» با `2` بیان می‌شود.**

**پیامد:** اگر کد ما بنویسد `ISACTIVE = false` (یعنی `0`)، مقداری تولید می‌شود که در دامنهٔ سیستم قدیمی بی‌معنی است. و هنگام خواندن، تفصیلیِ **غیرفعال** (مقدار `2`) توسط تبدیل `bool` احتمالاً `true` خوانده می‌شود — یعنی **یک تفصیلی غیرفعال را فعال گزارش می‌کنیم.** باگ خاموش با پیامد مستقیم روی صحت داده.

## ۱۰-۴. 🔴 الگوی سیستماتیک: کامنت ستون Oracle در چند مورد با enum در تناقض است

| ستون | کامنت Oracle | enum `Tamin.Core` | وضعیت |
|---|---|---|---|
| `TB_ACCOUNTCODE.TYPEACTIVITY` | ۱=**بستانکار**، ۲=**بدهکار** | ۱=**Debit**، ۲=**Credit** | 🔴 **معکوس** |
| `TB_TAFSILI.OWNER` | «2=setad **1=vahed**» | ۱=**Global(سراسری)**، ۲=**Unit(داخلی)** | 🔴 **معکوس** |
| `TB_ELAMHEAD.ELAMHDRAMAD_TYPE` | ۱ذی‌حسابی ۲سایردرآمد ۳حق‌بیمه | ۱حق‌بیمه ۲ذی‌حسابی ۳سایر | 🔴 **جابه‌جا** |
| `TB_IDENTITYSUBGRP.SUBGRPS_TYPE` | «حروف, اعداد, يا هردو» | ۱تاریخ ۲فارسی ۳عدد ۴لاتین | 🔴 **کامنت منطبق نیست** |
| `TB_BANKCARTDETAIL.CHECKRECEIPTTYPE` | «(فيش يا حواله)» — دو مقدار | ۴ مقدار | 🔴 **کامنت ناقص** |
| `TB_ELAMHEAD.ELAMH_CASE` | ۱بد ۲بس | ۱Debtor ۲Creditor | ✅ **موافق** |
| `TB_VOUCHERSHEAD.ISAUTOMATIC` | ۰دستي ۱مکانيزه | auto=0, manual=1 | ✅ موافق (به‌جز نام‌ها) |
| `TB_PREDESCRIB.FLAGVOUCHER` | head=0 Detail=1 | ForVoucherHead=0, ForVoucherDetail=1 | ✅ **موافق** |

> **نتیجهٔ روش‌شناختی مهم:** **کامنت‌های ستون Oracle در این schema منبع قابل‌اعتمادی نیستند.** حداقل ۵ مورد با کد اجراشونده در تناقض یا ناقص‌اند.

### به‌روزرسانی تناقض `TYPEACTIVITY`

نتیجه‌گیری پاس اول («حدس نمی‌زنم») برقرار است، ولی وزن شواهد سنگین‌تر شد. **چهار شاهد مستقل** می‌گویند **بدهکار = ۱**: `TypeActivity.Debit=1`، `TypeActivityGroup.Debit=1`، `DebCredType.Debtor=1`، `ElamCase.Debtor=1`.

و مهم‌تر: **خودِ schema اوراکل ما در ستون `TB_ELAMHEAD.ELAMH_CASE` کامنت «نوع اعلاميه 1بد 2بس» دارد** — یعنی درون همان schema دو ستون با قرارداد متضاد کامنت خورده‌اند، و کامنت `TYPEACTIVITY` تنها منبع مخالف است.

⚠️ **همچنان حدس نزدم:** پیش از migration، مقدار `TYPEACTIVITY` چند حساب با ماهیت بدیهی روی دادهٔ زنده خوانده شود. هزینهٔ اشتباه = **وارونه‌شدن ماهیت همهٔ حساب‌ها**.

> ✅ **حل شد (۲۰۲۶-۰۸-۲۶، بخش ۲۳):** با کوئری روی دادهٔ زندهٔ `CENTRALACCOUNT`، مشخص شد **enum `Tamin.Core` درست است و کامنت ستون Oracle نادرست/کهنه است** — `TYPEACTIVITY=1` یعنی **بدهکار**، `TYPEACTIVITY=2` یعنی **بستانکار**. قوی‌ترین شاهد: حساب‌های سطح گروه با نام بدیهی حسابداری (`دارايي هاي جاري`, `هزينه ها` = `TYPEACTIVITY=1`؛ `بدهي هاي جاري`, `بدهي هاي غيرجاري` = `TYPEACTIVITY=2`) دقیقاً با enum هم‌راستایند، نه با کامنت. جزئیات کامل در بخش ۲۳.

## ۱۰-۵. 🔴 ناسازگاری عرض ستون: `VAHEDTYPE`

`TB_TAFSILI.VAHEDTYPE` و `TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE` هر دو `NUMBER(1)` (حداکثر یک رقم)، ولی `TypeVahed` **۱۷ مقدار** دارد.

سه احتمال، **هیچ‌کدام حدس زده نشد:** (۱) این ستون‌ها از `TypeVahed` استفاده نمی‌کنند؛ (۲) عرض واقعی ستون بزرگ‌تر است؛ (۳) فقط ۱..۹ استفاده می‌شوند.
❓ نیاز به `SELECT DISTINCT VAHEDTYPE FROM TB_TAFSILI` + بررسی DDL. **قبل از هر CRUD روی `TB_TAFSILI` باید روشن شود.**

> ⚠️ **تا حدی حل شد (۲۰۲۶-۰۸-۲۶، بخش ۲۳):** DDL تأیید کرد عرض فیزیکی واقعاً `NUMBER(1)` با `DATA_PRECISION=1` است (احتمال ۲ رد شد؛ Fluent Mapping فعلی ما از نظر عرض درست است). دادهٔ زنده نشان می‌دهد مقدار واقعی فقط در `{1, 3}` است (نه ۱..۹ کامل) — یعنی احتمال ۳ هم به‌طور دقیق تأیید نشد (باریک‌تر از پیش‌بینی). **هنوز حدس نزدم:** با این حجم کم داده (۲۶ ردیف غیر-NULL در `TB_TAFSILI`، محیط توسعه) نمی‌توان قطعی گفت این ستون واقعاً زیرمجموعه‌ای از `TypeVahed` (۱۷‌مقداری) است یا یک enum کاملاً متفاوت و باریک‌تر — چون معنای دقیق مقادیر ۱ و ۳ در `TypeVahed` در دامنهٔ این تحقیق (فقط DB) بررسی نشد. جزئیات در بخش ۲۳.

⚠️ دام نام‌گذاری: `TB_VAHED_TYPE.TYPECODE` (با UNIQUE `UK_VAHEDTYPE`) **با `TB_ACCOUNTCODE.TYPECODE` هم‌نام ولی کاملاً بی‌ربط است**.

## ۱۰-۶. 🟡 مقادیر DEFAULT خارج از دامنهٔ enum

| ستون | DEFAULT | دامنهٔ enum | مشکل |
|---|---|---|---|
| `TB_VOUCHERSHEAD.DOCLIFE` | `0` | ۱..۴ | ۰ در enum نیست |
| `TB_TAFSILI.PERSONTYPE` | `0` | ۱..۳ | ۰ در enum نیست |
| `TB_ATTRIBFORACCOUNTCODE.CONTROLID` | `null` ولی ستون **NOT NULL** | ۱،۲ | DEFAULT با NOT NULL متناقض |

هم‌خانوادهٔ ریسک باز 🔴 موجود ما دربارهٔ «DEFAULTهای Oracle ناسازگار با `Guid`» — همان کلاس مسئله روی ستون‌های enum.

## ۱۰-۷. 🐛 باگ محتمل در سیستم قدیمی: نام‌های `IsAutomatic` وارونه‌اند

```csharp
public enum IsAutomatic {
    [Description("دستی")]      auto   = 0,
    [Description("اتوماتیک")]  manual = 1
}
```
نام عضو `auto` توضیحش «دستی» و نام عضو `manual` توضیحش «اتوماتیک» — **نام‌ها دقیقاً برعکس معنایشان‌اند.** کامنت Oracle («0دستي و 1 مکانيزه») با Descriptionها موافق است، نه با نام‌ها.

**پیامد:** `AddVoucherCommandHandler.cs:113` → `add.SetIsAutomatic(IsAutomatic.manual);` در Endpoint **ثبت دستی** فراخوانی می‌شود، ولی مقدارش **`1`** یعنی «مکانيزه». یعنی احتمالاً **هر سند دستی به‌اشتباه «اتوماتیک» علامت خورده است.**
❓ **قطعی نیست، حدس نزدم** — اگر این ستون را مصرف کردیم باید روی دادهٔ زنده راستی‌آزمایی شود.

> ✅ **راستی‌آزمایی شد (۲۰۲۶-۰۸-۲۶، بخش ۲۳):** فرضیهٔ باگ با دادهٔ زنده **تأیید** شد. ۹۴٫۷٪ اسناد (`۵۴` از `۵۷`) مقدار `ISAUTOMATIC=1` دارند و این دقیقاً و به‌طور کامل با اسنادی هم‌بسته است که از `SYSTEM_TYPE` عمومی «حسابداري» (`SYS_COD=1`، ماژول کلی که منطقاً محل ثبت دستی است) می‌آیند؛ در مقابل، فقط اسناد زیرسیستم تخصصی «دريافت و پرداخت» (`SYS_COD=4`) مقدار `ISAUTOMATIC=0` دارند. این دقیقاً برعکسِ انتظار منطقی است (ماژول عمومی/دستی باید ۰ و زیرسیستم خودکار باید ۱ باشد) و با معیار تفسیر خودِ این کار («اگر تقریباً ۱۰۰٪ مقدار ۱ باشد، باگ تأیید می‌شود») همخوانی دارد. جزئیات در بخش ۲۳.

## ۱۰-۸. فهرست کامل ۴۴ enum

**کدینگ:** `TypeCodes`(۱-۳)، `TypeAccCode`(۱-۲)، `TypeAction`(۱-۳)، `TypeActivity`(۱-۷)، `TypeActivityGroup`(۱-۳)
**سند:** `DocLife`(۱-۴)، `IsAutomatic`(۰-۱)، `TafsiliNo`(۱-۷)، `SortType`(۱-۲)، `TmpImportType`(۱-۲)، `ConsolidateReportType`(۰-۱۰)، `PersianMonths`(۱-۱۲)
**تفصیلی:** `Active`(۱-۲)، `Owners`(۱-۲)، `PersonTypes`(۱-۳)
**عمومی:** `TypeInfos`(۱-۴)، `TypeKoli`(۱-۳)، `TypeVahed`(۱-۱۷)، `TrialBalanceGridShowType`(۱-۴)، `SearchOperator`(EQ/NEQ/GT/LT/GTE/LTE/LIKE/IN)
**پرداخت/چک:** `ChargeAndCostType`(۱-۲)، `Status`(۰-۲)، `CheckStatus`(۱-۳)، `CheckType`(۱-۲)، `CheckPrintStatus`(۱-۲)، `CheckShowGridType`(۱-۶)، `CheckStatus`(ns `Enum`، ۱-۲)، `CheckReceiptType`(۱-۴)، `ReceiptType`(۱-۲)، `DebCredType`(۱-۲)، `PayRecivType`(۱-۳)
**اعلامیه:** `DaramElamhType`(۱-۳)، `ElamCase`(۱-۲)، `ElamType`(۱-۳)، `WebStat`(۱-۱۰)
**شناسه/صفت:** `IdentitySubGroupKind`(۱-۲)، `IdentitySubGroupType`(۱-۴)، `AttribSumEnum`(۱-۲)، `ControlEnum`(۱-۲)، `FlagEnum`(۱-۲)
**سایر:** `Flag`(۰-۱)، `TypeAccountCode`(**۲-۳**)، `InterfaceType`(۱-۲)، `OperatorRole`(۱-۴)، `StateEnum`(۱-۳)

> ⚠️ **دو دام:** (الف) **دو enum متفاوت به نام `CheckStatus`** در دو namespace. (ب) `TypeAccountCode` **از ۲ شروع می‌شود** (`KolCode=2, Moincode=3`) — عمداً با `TypeCodes` هم‌تراز شده؛ **شاهد پنجم مستقل برای نگاشت `TYPECODE`**.

## ۱۰-۹. جمع‌بندی اقدام

🔴 **این «باگ `TB_ACCOUNTCODE`» نیست؛ یک باگ سیستماتیک scaffold روی ۱۹ ستون در ۱۳ جدول است:**
`TB_ACCOUNTCODE`, `TB_VOUCHERSHEAD`, `TB_TAFSILI`, `TB_TAFSIL_GROUP`, `TB_TAFSIL_LINK_TAFSILGROUP`, `TB_WHITEANDBLACKLIST`, `TB_RABET_CLOSING`, `TB_PAYRECIVHEAD`, `TB_ELAMHEAD`, `TB_IDENTITYSUBGRP`, `TB_BANKCARTDETAIL`, `TB_CHECKBOOK`, `TB_ATTRIBFORACCOUNTCODE`

⚠️ **دو تا همین حالا در مسیر نوشتن فعال ما هستند:** `TB_ACCOUNTCODE` و `TB_VOUCHERSHEAD`. یعنی Commandهای ما همین الان `DOCLIFE` را `bool?` می‌پذیرند و می‌نویسند — ناسازگار با دامنهٔ واقعی ۱..۴.

> این مستقیماً ریسک باز 🔴 ما را تشدید می‌کند: «سند Post شده قابل تغییر است». نه‌تنها گاردی نیست، بلکه **نوع دادهٔ `DOCLIFE` هم غلط است** و اصلاً نمی‌تواند چهار وضعیت واقعی سند را بیان کند.

---
---

# بخش ۱۳ — 🔴 تصحیح مهم بخش ۳: تغییرناپذیری سند **تا حد زیادی enforce می‌شود**

> **این بخش یک نتیجه‌گیری غلط در پاس اول را تصحیح می‌کند.**

در بخش ۳-۴ نوشتم «تغییرناپذیری سند پس از Post ❌ enforce نمی‌شود» — آن نتیجه‌گیری **فقط بر پایهٔ `ChangeStateCommandhandler`** بود که واقعاً هیچ گاردی ندارد. اما در پاس سوم که **همهٔ ۳۷۲ Handler** اسکن شدند، معلوم شد گاردهای وضعیت سند در **خودِ عملیات‌ها** پیاده شده‌اند، نه در تغییر وضعیت.

## ۱۳-۱. جدول واقعی گاردهای `DocLife`

| عملیات | فایل | خط | گارد |
|---|---|---|---|
| **حذف سرسند** | `Commands\Vouchers\VoucherHeads\Delete\DeleteVouchersHeadCommandHandler.cs` | ۲۶ | `reviewed` **یا** `accepted` → «سند در وضعیت بررسی شده یا تایید دائم قابل حذف نمی باشد» |
| **ویرایش سرسند** | `Commands\Vouchers\VoucherHeads\update\UpdateVoucherHeadAcommandHandler.cs` | ۴۳ | فقط `accepted` → «سند در وضعیت تایید دائم قابل ویرایش نمی باشد» |
| **ادغام اسناد** | `Commands\Vouchers\Voucher\MergVoucher\MergVoucherCommandHandler.cs` | ۳۱-۳۲ | `reviewed` و `accepted` هر دو ممنوع (دو پیام مجزا) |
| **مرتب‌سازی اسناد** | `Commands\Vouchers\Voucher\SortVoucher\SortVoucherCommandHandler.cs` | ۳۹ | **همهٔ** اسناد بازه باید `temporary` باشند |
| **تغییر وضعیت** | `Commands\Vouchers\Voucher\ChangeState\ChangeStateCommandhandler.cs` | — | ❌ **هیچ گاردی ندارد** |

**نقل مستقیم گارد حذف:**
```csharp
if (document.DocLife == DocLife.reviewed || document.DocLife == DocLife.accepted)
{
    throw new CustomException("سند در وضعیت بررسی شده یا تایید دائم قابل حذف نمی باشد");
}
```

**نقل مستقیم گارد ویرایش:**
```csharp
if (entity.DocLife == DocLife.accepted)
{
    throw new CustomException("سند در وضعیت تایید دائم قابل ویرایش نمی باشد");
}
```

## ۱۳-۲. 🔴 دو یافتهٔ بسیار مهم برای مدل ما

**یافتهٔ اول — عدم‌تقارن عمدی حذف در برابر ویرایش.** حذف در `reviewed` ممنوع است ولی ویرایش در `reviewed` **مجاز** است. یعنی سند «بررسی‌شده» هنوز قابل اصلاح است ولی دیگر قابل حذف نیست. این یک تصمیم کسب‌وکاری آگاهانه است، نه فراموشی (دو Handler مختلف، دو شرط متفاوت، دو پیام متفاوت).

**یافتهٔ دوم — 🔴 `UpdateVoucherHeadAcommandHandler` اصلاً `DOCLIFE` را تغییر نمی‌دهد.** فقط سه فیلد قابل ویرایش‌اند:
```csharp
entity.SetDateDoc(command.DateDoc);
entity.SetHeadDesc(command.HeadDesc);
entity.SetNoDoc(command.DocNumber.PadLeft(6, '0'));
```
**تغییر وضعیت فقط از مسیر اختصاصی `ChangeStateCommand` ممکن است.**

> 🔴 **این مستقیماً یکی از ریسک‌های باز 🔴 ما را نشانه می‌گیرد.**
> `UpdateVoucherHeadCommand` **ما** اجازه می‌دهد `DOCLIFE` آزادانه از بدنهٔ PUT عوض شود. سیستم قدیمی این را **عمداً** جدا کرده: ویرایش محتوا یک عملیات است، تغییر وضعیت عملیاتی دیگر با Endpoint و مجوز خودش.
> ترکیب این با یافتهٔ بخش ۱۰ (که `DOCLIFE` در مدل ما اصلاً `bool?` است و نمی‌تواند ۴ وضعیت را بیان کند) یعنی **مسیر ویرایش سرسند ما دو نقص هم‌زمان دارد**: نوع دادهٔ غلط، و عملیات نامتمایز.

## ۱۳-۳. ماشین وضعیت واقعی سند

```
draft(۱ یادداشت) → temporary(۲ موقت) → reviewed(۳ بررسی‌شده) → accepted(۴ تایید دائم)
```

| وضعیت | حذف | ویرایش محتوا | ادغام | مرتب‌سازی |
|---|---|---|---|---|
| `draft` ۱ | ✅ | ✅ | ✅ | ❌ (فقط `temporary`) |
| `temporary` ۲ | ✅ | ✅ | ✅ | ✅ |
| `reviewed` ۳ | ❌ | ✅ | ❌ | ❌ |
| `accepted` ۴ | ❌ | ❌ | ❌ | ❌ |

⚠️ **ولی `ChangeStateCommand` هیچ گارد گذاری ندارد** — می‌توان `accepted` را مستقیماً به `draft` برگرداند و سپس حذف کرد. **یعنی تغییرناپذیری از این در پشتی قابل دور زدن است.** این یک شکاف واقعی در سیستم قدیمی است (نه چیزی که ما باید کپی کنیم).

---

# بخش ۱۴ — 🔴 تصحیح مهم بخش ۳-۱: تراز **در یک مسیر واقعاً enforce می‌شود**

> **تصحیح دوم پاس اول.** گفتم «هیچ بررسی تراز فعالی در مسیر نوشتن وجود ندارد». این برای مسیر **دستی** درست است، ولی یک مسیر **خودکار** پیدا شد که تراز را واقعاً اجبار می‌کند.

**فایل:** `ApplicationUseCases\Commands\PayAndRecive\SendToVouchers\AddVoucherByPayAndReciveCommandHandler.cs:23-31`

```csharp
var headent = await _unitOfWork.payReciveHeadRepository.GetByIdAsync(command.PayReciveId);
if (headent.PayReciveDetails.Count() == 0)
{
    throw new CustomException("ردیف انتخابی جزئیات دریافت و پرداخت ندارد");
}
if (headent.PayReciveDetails.Sum(d => d.Debtor) != headent.PayReciveDetails.Sum(c => c.Creditor))
{
    throw new VouchersException("ردیف انتخابی تراز نمی باشد.");
}
```

**یعنی تضمین «بدهکار = بستانکار» در سیستم قدیمی وجود دارد — ولی فقط هنگام تبدیل یک سند دریافت/پرداخت به سند حسابداری.**

## ۱۴-۱. تصویر کامل و اصلاح‌شدهٔ وضعیت تراز

| مسیر ساخت سند | بررسی تراز؟ | شاهد |
|---|---|---|
| `AddVoucherCommand` (دستی، تک‌ردیفی) | ❌ کد کامنت شده | `AddVoucherValidator.cs:53-69` |
| `AddVoucherListCommand` (دستی، چندردیفی) | ❌ | بررسی شد، وجود ندارد |
| **`AddVoucherByPayAndReciveCommand`** (خودکار از دریافت/پرداخت) | ✅ **بله** | `...Handler.cs:28-31` |
| `AddReversVoucherCommand` (سند برگشتی) | ✅ **ذاتاً** (کپی معکوس سند متراز) | `AddReversVoucherCommandHabdler.cs:56-57` |
| `AddOpeningVoucherCommand` (افتتاحیه) | ⚠️ ذاتاً از مانده‌ها ساخته می‌شود | `AddOpeningVoucherCommandHandler.cs` |
| `AddClosingVoucherCommand` (اختتامیه) | ✅ **ذاتاً** (ردیف رابط تراز را می‌بندد) | `AddClosingVoucherCommandHandler.cs:139-157` |

> ✅ **نتیجهٔ اصلاح‌شده:** فرض قبلی ما («تراز هرگز enforce نمی‌شود») **نادرست** بود. الگوی واقعی این است: **مسیرهای خودکار متراز بودن را تضمین می‌کنند؛ مسیر ورود دستی به کاربر اعتماد می‌کند** (و در عوض اختلاف تراز را در UI نمایش می‌دهد — `GetVoucherDetailForEditQueryHandler.cs:83` → `Remainder = Sum(Debtor) - Sum(Creditor)`).
>
> 🔴 **برای ریسک باز 🔴 ما («composite create تنها نقطه‌ای است که می‌توانست تراز را تضمین کند»):** حالا یک الگوی مرجع سه‌خطی داریم و می‌دانیم سیستم قدیمی آن را در مسیرهای خودکار **واقعاً** به‌کار برده.

## ۱۴-۲. 🔴 قاعدهٔ معکوس‌سازی بدهکار/بستانکار (contra entry)

**دو جا کشف شد و در هر دو عمدی است:**

**۱. تبدیل دریافت/پرداخت به سند** (`AddVoucherByPayAndReciveCommandHandler.cs:62-63`):
```csharp
add.SetDebtor(detail.Creditor);
add.SetCreditorr(detail.Debtor);
```

**۲. سند برگشتی** (`AddReversVoucherCommandHabdler.cs:56-57`):
```csharp
add.SetDebtor(detail.Creditor);
add.SetCreditorr(detail.Debtor);
```

سند برگشتی ضمناً `head.SetDocLife(DocLife.draft)` و `head.SetParentHeadId(voucher.Id)` می‌زند — یعنی **سند برگشتی همیشه به‌صورت پیش‌نویس ساخته می‌شود و به سند اصلی زنجیر می‌شود.**

> ⚠️ ستون `PARENTHEAD_ID` در `TB_VOUCHERSHEAD` (که ما به‌عنوان `ParentHeadId` داریم و در Validator فاز ۸ فقط چک می‌کنیم که با `Id` خودش برابر نباشد) در واقع **دو معنای متمایز** دارد: (الف) زنجیرهٔ سند برگشتی، (ب) ارجاع به سند دریافت/پرداخت مبدأ. ❓ **حدس نزدم که آیا معنای سومی هم دارد.**

---

# بخش ۱۵ — فهرست کامل ۱۵۶ Command و ۲۲۱ Query

استخراج کامل با `find`. این فهرست **نقشهٔ راه دامنه** است: هر مورد یک قابلیت کسب‌وکاری واقعی است که سیستم قدیمی دارد و ما نداریم.

## ۱۵-۱. Commands بر حسب ماژول (۱۵۶ مورد در ۳۵ ماژول)

| ماژول | Commandها |
|---|---|
| **AccountCodes** (۷) | `DeleteAccountCode`; Group: `Add`/`Update`; Kol: `Add`/`Update`; Moin: `Add`/`Update` |
| **Vouchers** (۲۲) | `AddClosingVoucher`, `AddOpeningVoucher`; TmpVouchers: `AddTmpVoucherHead`/`AddVochersImportTemp`/`Delete`; Voucher: `ChangeState`/`AddVoucher`/`AddReversVoucher`/`AddVoucherList`/`InvoiceConfirmation`/`InvoiceReturn`/`MergVoucher`/`SortVoucher`/`UploadExcel`; VoucherDetails: `AddVouchersDetail`/`AddSingleVoucherDetail`/`Delete`/`Update`; VoucherHeads: `Add`/`Delete`/`update` + Attachments `Add`/`Delete` |
| **ChargeAndCosts** (۱۰) | `AddChargeAndCost` (ترکیبی); Detail: `Add`/`Delete`/`DeleteSingle`/`Update`; Head: `ChangeStatusToAccept`/`ChangeStatusToReviewed`/`ChangeStatusToTemporary`/`Delete`/`Update` |
| **PayAndRecive** (۹) | `Add`/`Update`/`Delete`; Details: `AddSingle`/`UpdateSingle`/`DeleteSingle`; `AddVoucherByPayAndRecive`; Attachments: `Add`/`Delete` |
| **Elms** (۱۴) | `CreateRcvElamVoucher`, `SendElamVoucher`; DrmdElams: `Add`/`Delete`/`Send`/`Update`; OtherElamDetail: `Add`/`Delete`/`Update`; OtherElamHead: `ChangeWebStat`/`Add`/`Delete`/`Update`; `AddElamReciveVahed` |
| **Checks** (۷) | CheckInCurrents: `Add`; CheckTypes: `Add`/`Delete`/`Update`; CheckBook: `Add`/`Delete`/`Update` |
| **Identitys** (۹) | `UpdateIdentityDetail`, `UpdateIdentityFixItem`; IdentityGroup: `Add`/`Delete`/`Update`; `AddIdentityHead`; IdentitySubGroup: `Add`/`Delete`/`Update` |
| **Tafsilis** (۵) | `Add`/`Update`/`Delete`; Attachments: `Add`/`Delete` |
| **Attributes** (۷) | AttribForAccountCodes: `Add`/`Delete`/`Update`; AttribInVouchers: `Add`/`AddWithAccountId`/`Delete`/`Update` |
| **BankCartDetails** (۵) | `Add`/`Update`/`Delete`, `ImportDisket` (ورود دیسکت بانک رفاه)، `Reconciliation` (مغایرت‌گیری) |
| **WhiteAndBlackLists** (۵) | `Add`/`Update`/`Delete`، `ChangeStateToBlackListed`، `ReActive` |
| **RabetClosings** (۳) / **Rabets** (۳) / **RabetTypes** (۱) | `Add`/`Update`/`Delete` هرکدام |
| **Expenses** (۶) | ExpenseGroups: `Add`/`Delete`/`Update`; Expenses: `Add`/`Delete`/`Update` |
| **TbAccountLinkLevels** (۳) | `Add`/`Update`/`Delete` |
| **TbAccountLinkTafsilGroups** (۴) | `Add`/`Update`/`Delete`/`DeleteByTafsilGroupId` |
| **TbTafsilGroups** (۳) / **TbTafsilLinkTafsilGroups** (۳) / **TbLevelTafsils** (۳) | `Add`/`Update`/`Delete` هرکدام |
| **RevolvingFunds** (۳) | `Add`/`Update`/`Delete` — تنخواه گردان |
| **Accounts** (۴) | `Add`/`Update`/`Delete` + `AddAccountType` — حساب **بانکی** |
| **PersonActions** (۳) | `Add`/`Update`/`Deactivate` |
| **FinanceYear** (۳) | `AddFinancialYear`/`Delete`/`AddUnitFinancialYear` |
| **WorkShops** (۳) | `Add`/`Update`/`ChangeState` |
| **PreDescribes** (۲) | `Add`/`Delete` — شرح آماده |
| **Cities/Provinces/Banks/VahedInfos/VahedTypes/WhiteList/Receipts/BillLogs/Sample** (۹) | هرکدام `Add` |

## ۱۵-۲. Queries بر حسب ماژول (۲۲۱ مورد)

| ماژول | تعداد | نمونه‌های شاخص |
|---|---|---|
| **Vouchers/Reports** | **۳۱** | `TrialBalance` در سه نوع (Columns4/6/8) هرکدام با `DrillDown` و `Wrapper`؛ `AccJournalReport`، `LedgerReport`، `BalanceSheetReport`، `GeneralBalanceSheetReport`، `ConsolidateReport1` (+Filter+Wrapper)، `Consolidates` (Group/Kol/Moein)، `TafsiliReviewReport` (Tafsili/TafsiliGroup/TafsiliVoucher)، `VoucherReviewReport`، `AgePyramidReport`، `AttribMisMatchReport`، `RelationGroupMoeinReport`، `BillLogReport` |
| **AccountCodes** | ۲۲ | `GetAllGroup`/`Kol`/`Moin`، `GetKolsByGroupIds`، `GetMoinsByKolIds`، `GetAccounCodelByParentId`، `SearchOnMoein`، `ConsolidateReportFilter` (Group/Kol/Moin)، `GetAllMoinsWithIdentifer`، `GetMoinWithOutCheckList` |
| **Vouchers/VoucherHead** | ۲۰ | `GetAllByVahedCodeAndYear` (۳ نسخه)، `GetVoucherByIdForEdit`، `GetVoucherDetailForEdit`، `GetAtfNo`، `GetVoucherNo`، `GetDocLife`، `GetExistVoucher`، `GetUserByVoucherId` |
| **VahedsInfo + VahedTypes** | ۱۵ | درخت واحدهای سازمانی، `GetByTypeDarmanBimeh`، `GetVahedTypeTree` |
| **TbAccountLinkTafsilGroups** | ۶ | `GetAccountLinkMoinTree`، `GetAccountLinkTafsilGroupTree`، `GetListTafsiliByAccountLevelid` |
| **ChargeAndCosts** | ۱۳ | `Cartable/Search`، `GetMaxCode`، `GetDetailsForCreatePay` |
| **Checks** | ۱۰ | `CheckBookCartable`، `Print`، `PrintHtml` |
| **WhiteList** | ۷ | بر حسب `AccountCode`/`City`/`Province`/`VahedCode`/`VahedType` |
| **Identity** | ۱۱ | `GetFixedItemByGroupId`، `GetNotFixedItemByGroupId`، `GetMaxSubGroupCode` |
| **Tafsilies** | ۶ | `GetTafsiliByIdForEdit`، `GetTafsiliFiles`، `ConsolidateReportFilter` |
| **PayReciv** | ۶ | `GetLastCode`، `GetPayReciveInfoByHeadId` |
| بقیه | ~۷۴ | Banks، Cities، Expenses، RevolvingFunds، PersonActions، PreDescribes، Rabet، Enum، CurrentUser، … |

> **مشاهده:** `Vouchers/Reports` با ۳۱ Query بزرگ‌ترین بخش سمت خواندن است. سه نوع **تراز آزمایشی** (۴/۶/۸ ستونه) با drill-down کامل — این استاندارد گزارش‌های حسابداری ایران است و ما هنوز هیچ‌کدام را نداریم.

---

# بخش ۱۶ — کاتالوگ کامل قوانین کسب‌وکار enforce‌شده (استخراج از ۳۷۲ Handler)

روش: `grep -rn "throw new" --include=*Handler.cs` روی کل `ApplicationUseCases`. هر استثنا = یک قانون کسب‌وکاری واقعی.

## ۱۶-۱. قوانین «قابل حذف نیست» (حفاظت از یکپارچگی)

| موجودیت | شرط منع | فایل:خط |
|---|---|---|
| کد معین | گردش دارد | `AccountCodes\Delete\...:32` |
| کد معین | به گروه تفصیلی لینک دارد | `...:36` |
| کد کل | فرزند (معین) دارد | `...:45` |
| کد گروه | فرزند (کل) دارد | `...:49` |
| سرسند | `reviewed` یا `accepted` است | `VoucherHeads\Delete\...:26` |
| سرسند | اوراق بانکی مغایرت‌گیری شده دارد | `VoucherHeads\Delete\...:30` |
| ردیف سند | اوراق بانکی مغایرت‌گیری شده | `VoucherDetails\Delete\...:31` |
| دریافت/پرداخت | سند صادر شده | `PayAndRecive\Delete\...:22` |
| ردیف دریافت/پرداخت | مغایرت‌گیری شده | `...DeleteDetailSingle\...:23` |
| دسته چک | اوراقش استفاده شده | `Checks\DeleteCheckBook\...:36` |
| صفت حساب | در اسناد استفاده شده | `Attributes\...Delete\...:33` |
| گروه شناسه | ویژگی تعریف‌شده دارد | `Identitys\IdentityGroup\Delete\...:21` |
| زیرگروه شناسه | فیلد ثابت/متغیر دارد | `IdentitySubGroup\Delete\...:21` |
| تنخواه گردان | در شارژ تنخواه استفاده شده | `RevolvingFunds\Delete\...:25` |
| رابط | گردش مالی دارد | `Rabets\Delete\...:39` |
| رابط اختتامیه | گردش مالی دارد | `RabetClosings\Delete\...:43` |
| اعلامیه | ارسال شده | `Elms\...\Delete\...:29` و `:23` |
| ردیف بانکی | مغایرت‌گیری شده | `BankCartDetails\Delete\...:21` |

> 🔴 **این کاتالوگ مستقیماً ریسک باز 🔴 ما را پوشش می‌دهد.** الگوی ثابت: **هیچ چیزی که «گردش مالی» یا «مصرف پایین‌دستی» دارد حذف نمی‌شود.** ما فعلاً هیچ‌کدام از این ۱۸ بررسی را نداریم.

## ۱۶-۲. قوانین «قابل ویرایش نیست»

| موجودیت | شرط | فایل:خط |
|---|---|---|
| سرسند | `accepted` است | `VoucherHeads\update\...:43` |
| سرسند | ماه صورتحساب شده | `VoucherHeads\update\...:39` |
| اعلامیه | ارسال شده | `Elms\...\Update\...:30`, `:25` |
| رابط / رابط اختتامیه | گردش مالی دارد | `Rabets\Update\...:31`, `RabetClosings\Update\...:42` |
| دسته چک | اوراقش ایجاد/استفاده شده | `Checks\UpdateCheckBook\...:45,49` |

## ۱۶-۳. قوانین یکتایی

| قانون | فایل:خط |
|---|---|
| شمارهٔ سند در (واحد + سال) | `AddVoucherCommandHandler.cs:94`، `AddVoucherListCommandHandler.cs:71`، `AddVouchersHeadCommandHandler.cs:55`، `UpdateVoucherHeadAcommandHandler.cs:57` |
| کد تفصیلی | `Tafsilis\Create\AddTafsiliHandler.cs:30` |
| کد درخواست شارژ/هزینه | `ChargeAndCosts\...\Create\...:38` |
| شمارهٔ دریافت/پرداخت | `PayAndRecive\Create\...:52`، `Update\...:26` |
| شمارهٔ فیش/حواله | `Receipts\Create\...:39` |
| رابط تکراری | `Rabets\Create\...:42` |
| سال مالی تکراری | `FinanceYear\Create\...:59` |

## ۱۶-۴. قوانین تفصیلی (تکرارشده در ۶ ماژول)

جفت `LevelNotProvidedException` / `UnauthorizedLevelException` در این Handlerها تکرار شده:
`AddVoucherCommandHandler.cs:168,173`؛ `AddSingleVoucherDetailCommandHandler.cs:115,120`؛ `AddElamDetailCommandHandler.cs:76,81`؛ `AddOtherElamHeadCommandHandler.cs:90,94`؛ `AddElamReciveVahedCommandHandler.cs:130,134`؛ `AddPayReciveCommandHandler.cs:125,129`؛ `AddPayReciveDetailSingleCommandHandler.cs:110,115`

> ✅ **تأیید مجدد بخش ۳-۳:** قاعدهٔ «وجود ردیف در `TB_ACCOUNT_LINK_LEVEL` = هم مجاز هم اجباری» یک قاعدهٔ **سراسری دامنه** است، نه مختص سند. هر جا ردیفی با کد معین ساخته می‌شود این بررسی تکرار شده.
> ⚠️ ولی در `UpdateElamDetailCommandHandler.cs:43` و `UpdatePayReciveDetailSingleCommandHandler.cs:54` نسخهٔ ساده‌شده‌ای هست: `throw new Exception("تفصیلی تمام سطوح پر نشده است")` — یعنی **در مسیر Update قاعده سست‌تر و ناسازگار است.**

## ۱۶-۵. قوانین سال مالی و دوره

`Commands\FinanceYear\Create\AddFinancialYearCommandHandler.cs`:
- خط ۴۶ → «هیچ سال مالی فعالی یافت نشد»
- خط ۵۱ → «ایجاد سال مالی در این زمان ممکن نیست» (کنترل زمانی بر پایهٔ `Substring(0,4)` تاریخ شمسی جاری)
- خط ۵۹ → «سال مالی {newYear} قبلاً ایجاد شده است»

**قاعدهٔ بستن ماه** (`AddVoucherCommandHandler.cs:81-84`, `AddVoucherListCommandHandler.cs:61`, `UpdateVoucherHeadAcommandHandler.cs:39`):
```csharp
if (month != currentMonth)
    validDateDoc = await _unitOfWork.vouchersHeadRepository.GetVouchersDocLife(month, vahedCode, year);
if (!validDateDoc)
    throw new CustomException("امکان ثبت سند در این ماه بدلیل وجود سند صورتحساب وجود ندارد.");
```
> **این نزدیک‌ترین معادل «دورهٔ بسته» است** و در **هر سه** مسیر نوشتن سند تکرار شده (ثبت تکی، ثبت لیستی، ویرایش سرسند).

## ۱۶-۶. قوانین ورود دیسکت بانک (`ImportDisketCommandHandler`)

اعتبارسنجی فایل بانک رفاه — `BankCartDetails\ImportDisk\RefahJari\...`:
- خط ۹۹ → فایل ارسال نشده
- خط ۱۰۲ → **نام فایل باید دقیقاً `STM001` باشد**
- خط ۱۰۷ → **طول هر ردیف باید دقیقاً ۱۳۹ کاراکتر باشد**
- خط ۱۱۰ → دیسکت مربوط به ماه انتخابی نیست
- خط ۴۵ → شماره حساب دیسکت با حساب انتخابی مطابقت ندارد
- خط ۶۹ → حساب جاری پیدا نشد

> یک فرمت فایل ثابت‌عرض (fixed-width) بانکی. اگر روزی این قابلیت را خواستیم، مشخصات دقیقش اینجاست.

---

# بخش ۱۷ — گردش‌کارهای کلان که ما هیچ معادلی برایشان نداریم

## ۱۷-۱. سند افتتاحیه (`AddOpeningVoucherCommand`)

`Commands\Vouchers\OpeningVouchers\AddOpeningVoucherCommandHandler.cs`

منطق: مانده‌های سال قبل → گروه‌بندی → ردیف سند در سال جدید.
- **حساب‌های مستثنا حذف می‌شوند:** `accountExceptionRepository.GetAllAsync()` (جدول `TB_ACCOUNT_EXCEPTION` که `ACCOUNTCOE_ID` + `VAHEDTYPE_ID` دارد) — یعنی **برخی حساب‌ها عمداً به سال بعد منتقل نمی‌شوند**.
- گروه با مجموع صفر رد می‌شود: `if (totalDebtor == 0 && totalCreditor == 0) continue;`
- تخصیص یک‌طرفه: `Debtor = totalDebtor > 0 ? totalDebtor : 0; Creditor = totalDebtor <= 0 && totalCreditor > 0 ? totalCreditor : 0;`
- برای هر گروه یک سند مجزا از طریق `AddVoucherListCommand` صادر می‌شود (**فراخوانی MediatR تودرتو**).

## ۱۷-۲. سند اختتامیه (`AddClosingVoucherCommand`)

`Commands\Vouchers\ClosingVouchers\AddClosingVoucherCommandHandler.cs`

- بر پایهٔ `RabetClosing` گروه‌بندی می‌شود (`GroupBy(i => i.RabetClosingId)`).
- خط ۹۰ → `if (rabetClosing is null || rabetClosing.Year != lastYear)` — رابط باید متعلق به **سال قبل** باشد.
- خط ۹۶ → `if (!voucherDetails.Any() || voucherDetails.Sum(x => x.Debtor - x.Creditor) == 0) return null;` — حساب متراز اصلاً بسته نمی‌شود.
- خط ۱۱۸-۱۲۸ → برای هر حساب: `balance = Debtor - Creditor` و سپس **ردیف معکوس** ساخته می‌شود (`Creditor = balance > 0 ? balance : 0`).
- خط ۱۳۹-۱۵۷ → در پایان یک ردیف روی **حساب رابط** (`rabetClosing.AccountCodeRabetId`) با مجموع کل، که سند را متراز می‌کند.

> **این معنای واقعی `TB_RABET_CLOSING` است** (که در سند قبلی به‌عنوان «مبهم» ثبت شده بود): **حساب واسطی که در بستن سال، مانده‌ها به آن منتقل می‌شوند.** ✅ ابهام بخش ۴ سند `tamin-core-entity-reference.md` **حل شد**.

## ۱۷-۳. صورتحساب ماهانه (`InvoiceConfirmationCommand`) — مکانیزم «دورهٔ بسته»

`Commands\Vouchers\Voucher\InvoiceConfirmation\InvoiceConfirmationCommandHandler.cs`

- بازهٔ ماه با `PersianMonths.ToPersianMonth()` / `ToTwoDigitString()` نرمال می‌شود.
- خط ۶۲ → هیچ سندی در بازه نیست → خطا.
- خط ۶۶ → `if (headList.All(v => v.DocLife == DocLife.accepted))` → «این ماه قبلا صورتحساب شده است».
- گروه‌بندی بر حسب `VahedCode`؛ **واحدهای «سالم»** آن‌هایی که حداقل یک سند `reviewed` دارند → همه به `accepted` می‌روند.
- **واحدهای «مشکل‌دار»** (اسناد `draft`/`temporary`) → برای هرکدام یک ردیف در `TB_BILL_LOG` ثبت می‌شود با شرح `"سند تأیید نشده (DocLife = ...)"`، سپس کل عملیات با خطا برمی‌گردد.
- `InvoiceReturnCommand` عملیات معکوس است (برگشت صورتحساب).

> **این حلقهٔ بستهٔ «دورهٔ مالی» است:** صورتحساب ماه → اسناد `accepted` می‌شوند → `GetVouchersDocLife` از آن پس ثبت/ویرایش سند در آن ماه را مسدود می‌کند. **ما هیچ معادلی نداریم** و Accounting Safety Gate ما این را «⚠️ هرگز در دامنه پیاده نشده بود» ثبت کرده است.

## ۱۷-۴. ادغام و مرتب‌سازی اسناد

- **`MergVoucherCommand`**: چند سند را در یک سند ادغام می‌کند؛ اسناد `reviewed`/`accepted` ممنوع (خط ۳۱-۳۲).
- **`SortVoucherCommand`**: شماره‌گذاری مجدد اسناد بر اساس `SortType` (۱=شماره سند، ۲=تاریخ)؛ خط ۳۳ اگر نوع مرتب‌سازی انتخاب نشده باشد خطا، خط ۳۹ **همهٔ اسناد بازه باید `temporary` باشند**.

> این دو عملیات در حسابداری ایران استاندارد‌اند (شماره‌گذاری پیوستهٔ اسناد پیش از نهایی‌شدن). ما هیچ‌کدام را نداریم — و ریسک باز 🟡 ما دربارهٔ «`RADIF` هیچ مدیریتی ندارد» دقیقاً از همین جنس است.

## ۱۷-۵. زنجیرهٔ اعلامیه (Elam) — گردش‌کار بین‌واحدی

`WebStat` یک ماشین وضعیت ۱۰ مرحله‌ای است (`Tamin.Core\Entities\Elam\WebStat.cs`): تهیه → صدور سند → تایید اولیه → تایید نهایی و ارسال، برای هر یک از دو نوع اعلامیه، به‌علاوهٔ دو وضعیت دریافت.

`ChangeWebStatCommandHandler.cs:32` → **«ابتدا برای اعلامیه انتخابی سند صادر نمایید»** — یعنی نمی‌توان وضعیت را جلو برد مگر اینکه سند حسابداری‌اش صادر شده باشد.
خط ۵۹ → «اعلامیه آماده ارسال می باشد».

`SendElmDrmdCommandHandler.cs:140-160` مجموعه‌ای از اعتبارسنجی‌های پیش از ارسال به سرویس بیرونی: کد کارگاه دقیقاً ۱۰ کاراکتر، شماره بدهی، نام کارگاه، تاریخ بدهی، کد واحد اسناد پزشکی، کد واحد شعبه.

> این یک **یکپارچگی با سرویس بیرونی** (`IElamDrmdWebService`) است. ❓ جزئیات آن سرویس را نخواندم.

## ۱۷-۶. شارژ/هزینهٔ تنخواه گردان (ChargeAndCost)

ماشین وضعیت مستقل با `Status` (۰=موقت، ۱=بررسی‌شده، ۲=تایید دائم) و سه Command اختصاصی: `ChangeStatusToTemporary`/`ChangeStatusToReviewed`/`ChangeStatusToAccept`.
⚠️ **توجه: `Status` از ۰ شروع می‌شود، برخلاف `DocLife` که از ۱ شروع می‌شود** — دو ماشین وضعیت مشابه با مبدأ متفاوت. دام واقعی.

## ۱۷-۷. مفاهیمی که کاملاً برای ما جدیدند

| مفهوم | جدول | نقش |
|---|---|---|
| **`Rabet`** | `TB_RABET` | نگاشت «نوع رابط → کد حساب». حساب‌های سیستمی قابل‌پیکربندی (مثلاً «حساب اعلامیه صادره») به‌جای hardcode. شاهد: `GetAllRabetAsync().Where(a => a.RabetType.RabetCode == "1").Select(x => x.AccountCode.AccCode)` |
| **`RabetClosing`** | `TB_RABET_CLOSING` | حساب واسط اختتامیه (بخش ۱۷-۲) |
| **`AccountException`** | `TB_ACCOUNT_EXCEPTION` | حساب‌هایی که به سال بعد منتقل **نمی‌شوند** |
| **`WhiteAndBlackList`** | `TB_WHITEANDBLACKLIST` | ماتریس مجوز «کد حساب × نوع واحد» با بازهٔ تاریخ مجاز و بازهٔ محدودیت + `StateEnum` |
| **`AttribForAccountCode` / `AttribInVoucher`** | `TB_ATTRIBFORACCOUNTCODE` | صفت‌های سفارشی قابل‌تعریف روی معین، که هنگام ثبت سند مقدار می‌گیرند |
| **`Identity*`** | `TB_IDENTITY*` | «شناسه» — ساختار فرا-داده‌ای گروه/زیرگروه با فیلدهای ثابت و متغیر روی ردیف سند |
| **`PreDescrib`** | `TB_PREDESCRIB` | شرح آمادهٔ سند (سرسند یا ردیف، طبق `Flag`) |
| **`BillLog`** | `TB_BILL_LOG` | لاگ شکست صورتحساب ماهانه |
| **`CheckBook` / `Check`** | `TB_CHECKBOOK` | دستهٔ چک با بازهٔ سریال؛ قانون «طول اولین برگ = طول آخرین برگ» |
| **`BankCartDetail`** | `TB_BANKCARTDETAIL` | صورت‌حساب بانکی + مغایرت‌گیری (`Reconciliation`) |

## ۱۷-۸. 🔴 دو کنترل که ذخیره می‌شوند ولی **هرگز اعمال نمی‌شوند**

| کنترل | وضعیت | شاهد |
|---|---|---|
| **`TYPEACTION`** (خلاف ماهیت) | ❌ هرگز خوانده نمی‌شود در اعتبارسنجی | بخش ۱-۳ |
| **`WhiteAndBlackList`** | ❌ **هرگز اعمال نمی‌شود** | جست‌وجوی `whiteListRepository`/`whiteAndBlackListRepository` در کل `ApplicationUseCases\Commands` **صفر نتیجه** خارج از ماژول خودش داد |

> این یک الگوی هشداردهنده است: **دو مکانیزم کنترلی کامل ساخته شده‌اند (Entity + CRUD + Controller + Query) ولی هیچ‌کدام به مسیر نوشتن سند وصل نیستند.** اگر ما این‌ها را از روی سیستم قدیمی کپی کنیم، ممکن است ماشین‌آلاتی بسازیم که هیچ اثری ندارد.
> ❓ **حدس نزدم** که آیا در UI یا سطح دیتابیس اعمال می‌شوند.

---

# بخش ۱۸ — محتوای ۴۲ Controller

الگوی سراسری: `[Route("api/[controller]/[action]")]` + `[ApiController]` + `[Authorize]` + `[RolesAllowed([...])]` روی هر اکشن. **صفر `PUT`**، ۶ `DELETE` (بخش ۱۱-۱).

| # | Controller | GET/POST | مسئولیت |
|---|---|---|---|
| ۱ | `AccountCodeController` | ۱۸/۹ | کدینگ سه‌سطحی؛ Add/Update برای Group/Kol/Moin مجزا + Delete واحد + `SearchOnMoein` |
| ۲ | `VoucherController` | ۱۰/۹ (+۲DEL) | ثبت/ویرایش/حذف سند، برگشتی، ادغام، مرتب‌سازی، پیوست |
| ۳ | `CartableController` | ۹/۱۱ | گردش کار تأیید سند، صورتحساب، برگشت صورتحساب |
| ۴ | `ReportController` | **۲۷/۱۴** | تراز ۴/۶/۸ ستونه، دفتر کل، روزنامه، ترازنامه، تجمیعی، بررسی تفصیلی |
| ۵ | `TmpVouchersController` | ۲/۴ | سند موقت، ورود از اکسل |
| ۶ | `TafsiliController` | ۶/۴ (+۱DEL) | تفصیلی + پیوست |
| ۷ | `TbTafsilGroupController` | ۴/۳ | گروه تفصیلی |
| ۸ | `TbTafsilLinkTafsilGroupController` | ۴/۳ | ارتباط تفصیلی↔گروه |
| ۹ | `TbLevelTafsilController` | ۲/۲ (+۱DEL) | سطوح تفصیلی (۱..۷) |
| ۱۰ | `TbAccountLinkLevelController` | ۳/۳ | **سطوح مجاز/اجباری هر معین** |
| ۱۱ | `AttributeController` | ۵/۶ | صفت حساب + صفت در سند |
| ۱۲ | `IdentityGroupController` | ۲/۳ | گروه شناسه |
| ۱۳ | `IdentitySubGroupController` | ۶/۳ | زیرگروه شناسه |
| ۱۴ | `IdentityHeadController` | ۱/۱ | سرشناسه |
| ۱۵ | `IdentityDetailController` | ۱/۱ | جزئیات شناسه |
| ۱۶ | `IdentityFixItemController` | ۰/۱ | اقلام ثابت |
| ۱۷ | `ElamsController` | ۳/۱۰ | اعلامیه صادره/رسیده، تغییر وضعیت، ارسال |
| ۱۸ | `RabetController` | ۲/۳ | رابط |
| ۱۹ | `RabetTypeController` | ۱/۱ | نوع رابط |
| ۲۰ | `RabetClosingController` | ۲/۳ | رابط اختتامیه |
| ۲۱ | `AccountController` | ۳/۴ | حساب **بانکی** |
| ۲۲ | `BankController` | ۴/۱ | بانک و شعبه |
| ۲۳ | `CheckController` | ۶/۷ | چک، دسته چک، چاپ |
| ۲۴ | `BankCartDetailController` | ۲/۵ | صورت‌حساب بانکی، ورود دیسکت، مغایرت‌گیری |
| ۲۵ | `ReceiptController` | ۰/۱ | فیش/حواله |
| ۲۶ | `PayAndReciveController` | ۶/۸ (+۱DEL) | دریافت/پرداخت + تبدیل به سند |
| ۲۷ | `ChargeAndCostController` | ۱۲/۱۱ | شارژ/هزینهٔ تنخواه + ماشین وضعیت |
| ۲۸ | `RevolvingFundController` | ۳/۳ | تنخواه گردان |
| ۲۹ | `ExpenseController` | ۴/۳ | هزینه |
| ۳۰ | `ExpenseGroupController` | ۲/۳ | گروه هزینه |
| ۳۱ | `TbYearController` | ۱/۳ | سال مالی |
| ۳۲ | `VahedInfoController` | ۷/۱ | واحد سازمانی |
| ۳۳ | `VahedTypeController` | ۶/۱ | نوع واحد + درخت |
| ۳۴ | `ProvinceController` | ۶/۱ | استان + درخت |
| ۳۵ | `CityController` | ۴/۱ | شهر |
| ۳۶ | `WorkShopController` | ۱/۴ | کارگاه |
| ۳۷ | `PersonActionController` | ۲/۳ | نقش اشخاص (`OperatorRole`) |
| ۳۸ | `PreDescribController` | ۴/۱ (+۱DEL) | شرح آماده |
| ۳۹ | `WhiteListController` | ۷/۱ | لیست سفید |
| ۴۰ | `WhiteAndBlackListController` | ۳/۳ | ماتریس مجوز |
| ۴۱ | `CurrentUserController` | ۱/۰ | کاربر جاری |
| ۴۲ | `CommonController` | ۱/۰ | `GetEnumQuery` — enumها به فرانت |

> 🟢 **الگوی خوب `CommonController`:** یک Endpoint واحد که enumها را با `[Description]`هایشان به فرانت می‌دهد (`Queries\Enum\GetEnumQuery.cs`). یعنی برچسب فارسی هر enum **یک منبع حقیقت** دارد و در فرانت تکرار نمی‌شود. اگر ما enumها را (طبق بخش ۱۰) اضافه کردیم، این الگو ارزش کپی دارد.

---

# بخش ۱۹ — به‌روزرسانی جدول تأثیر بر تصمیمات باز `CLAUDE.md`

| تصمیم باز | وضعیت پس از پاس سوم |
|---|---|
| 🔴 «تضمین تراز» | ⚠️ **تصحیح شد** — در مسیرهای **خودکار** enforce می‌شود (بخش ۱۴)، در مسیر دستی نه. الگوی مرجع موجود است. |
| 🔴 «سند Post شده قابل تغییر است» | ⚠️ **تصحیح شد** — گاردها وجود دارند ولی در **عملیات‌ها** نه در تغییر وضعیت (بخش ۱۳). ضمناً سیستم قدیمی `DOCLIFE` را از مسیر ویرایش **جدا** کرده؛ ما نکرده‌ایم. |
| 🔴 «حذف گرهٔ کدینگ» | ✅ الگوی مرجع + **۱۸ قانون مشابه** برای بقیهٔ موجودیت‌ها (بخش ۱۶-۱) |
| 🔴 «الزامی بودن تفصیلی» | ✅ حل شد + تأیید شد که قاعده‌ای **سراسری** است (۷ Handler) |
| 🟡 «`RADIF` مدیریت ندارد» | ⚠️ سیستم قدیمی `SortVoucherCommand` دارد (بخش ۱۷-۴) |
| 🟡 «Period بسته» | ✅ **مکانیزم کامل پیدا شد** — `InvoiceConfirmation` + `GetVouchersDocLife` (بخش ۱۷-۳) |
| 🟡 «`TB_RABET_CLOSING` مبهم» | ✅ **حل شد** — حساب واسط اختتامیه (بخش ۱۷-۲) |
| 🟡 «نوع دادهٔ مبلغ `long`» | ⬆️ تأیید مجدد — `CalculateGroupTotals` هم `(long, long)` برمی‌گرداند |
| 🔴 **جدید:** `bool?` روی ۱۹ ستون enum | 🔴 بخش ۱۰ — **مسدودکنندهٔ CRUD روی ۱۳ جدول** |
| 🔴 **جدید:** `DOCLIFE` در Command ما | 🔴 نوع غلط + عملیات نامتمایز (بخش ۱۳-۲) |

---

# بخش ۲۰ — وضعیت نهایی پوشش (صریح)

## ✅ پوشش کامل (۱۰۰٪)
- هر ۴۴ فایل enum در `Tamin.Core` + تطبیق با هر ۴۲ پراپرتی `bool?` ما.
- فهرست کامل هر ۱۵۶ Command و ۲۲۱ Query (نام + ماژول).
- فهرست کامل هر ۴۲ Controller با شمارش verb و مسئولیت.
- **کاتالوگ کامل قوانین کسب‌وکار** — هر `throw new` در هر ۳۷۲ Handler اسکن شد.
- ماشین وضعیت سند و گاردهایش.

## ⚠️ پوشش جزئی
- **بدنهٔ Handlerها:** ~۲۵ Handler عمیق خوانده شد از ۳۷۲. بقیه فقط از طریق استثناهایشان شناخته شدند.
- **Repositoryها:** ۵ فایل از ۴۰+ (ولی دو تای بزرگ‌ترین جزئی خوانده شد).
- **Controllerها:** فقط `AccountCodeController` خط‌به‌خط خوانده شد؛ بقیه از طریق متادیتا.

## ❌ پوشش داده **نشده** (حدس زده نشد)
1. **بدنهٔ ۴۱ Endpoint `ReportController`** و ۳۱ Query گزارشی — منطق تراز ۴/۶/۸ ستونه باز نشد.
2. **`CartableController` (۲۰ Endpoint)** — گردش کار تأیید، فقط از طریق Commandهایش شناخته شد.
3. **`Shared`, `IDP`, `Infrastructure.Logging`, `Infrastructure.Service`, `Presentation.Worker`, `Payments`, `Utility.Exception`, `SeriLog`, `Logging`** — هیچ‌کدام باز نشدند.
4. **`ApplicationUseCases\Behaviors\`, `Abstractions\`, `Common\`, `Template\`, `Registration.cs`** — pipeline اعتبارسنجی/رفتارها.
5. **`Tamin.Core\Services\`, `Common\`, `Exceptions\`** — فقط `Exceptions` غیرمستقیم شناخته شد.
6. **`Account.Tests` / `Application.Test`** — ارزیابی نشد.
7. **DTOها و ViewModelها** (~۲۰۰ فایل) — شکل دقیق قرارداد API استخراج نشد.
8. **`OracleExpressionToSqlConverter`** — امنیت فیلتر پویا.
9. **`IElamDrmdWebService`** و سرویس‌های بیرونی (`NationalCodeService`, `CompanyService` که کامنت‌اند).
10. **دادهٔ زندهٔ Oracle** — هیچ کوئری‌ای اجرا نشد. پس **همچنان حل‌نشده:** تناقض `TYPEACTIVITY` (۱=بدهکار؟)، عرض `VAHEDTYPE`، باگ محتمل `IsAutomatic`، و اینکه آیا مقادیر خارج از دامنهٔ enum در داده وجود دارند. ⚠️ **به‌روزرسانی ۲۰۲۶-۰۸-۲۶:** اولین اتصال واقعی به Oracle زنده انجام شد — رجوع به بخش ۲۳ (`TYPEACTIVITY` و `IsAutomatic` حل شدند؛ `VAHEDTYPE` تا حدی).

---
---

# بخش ۲۱ — مرجع per-Module: عملیات، منطق و قوانین هر Entity

> **افزوده‌شده در پاس چهارم (۲۰۲۶-۰۸-۲۶).** هدف: به‌ازای هر Entity مهم بدانیم **چه عملیاتی روی آن انجام شده** و **چه قوانینی enforce می‌شود** — ورودی مستقیم برای تصمیم «این Entity در پروژهٔ ما چه CRUD/عملیاتی لازم دارد».
>
> **روش:** استخراج مکانیکی از هر ۳۷۲ Handler (فراخوانی‌های repository + هر `throw new`)، به‌علاوهٔ خواندن عمیق ~۴۰ Handler کلیدی. هرجا فقط استخراج مکانیکی داشتم و بدنه را نخواندم، با ⚙️ علامت زده‌ام.

## ۲۱-۱. 🔴 کشف مهم پاس چهارم: مکانیزم چندمستأجری (`HaveAccessToUnit`) **وجود دارد ولی ناقص اعمال شده**

> این مستقیماً به دو تصمیم باز 🟡/🔴 ما می‌خورد: «`VahedCode` سمت سرور اعمال نمی‌شود» و «IDOR».

**فایل:** `ApplicationUseCases\Common\BusinessUserAcceess\BusinessUserAccess.cs`

```csharp
public bool HaveAccessToUnit(string targetUnitCode)
{
    string unitCode = CurrentUserDto.data.organization.code;
    var userVahedList = _unitOfWork.vahedInfoRepository.GetAllVahedInfoByParentAsync(unitCode).Result;
    if (userVahedList.Any(v => v.VahedCode == targetUnitCode))
        return true;
    else
        throw new UserNotAccessToUnitException(
            string.Format("کاربر مورد نظر دسترسی لازم به اطلاعات واحد {0} را ندارد ", targetUnitCode));
}
```

**مدل دسترسی: سلسله‌مراتبی.** واحد کاربر از **توکن IDP** خوانده می‌شود (`CurrentUserDto.data.organization.code`)، سپس **همهٔ زیرواحدهای آن** واکشی می‌شوند؛ کاربر فقط به واحد خودش و زیرمجموعه‌هایش دسترسی دارد. تخلف → استثنا (نه فیلتر خاموش).

**🔴 ولی فقط در ۱۲ Handler از ۳۷۲ فراخوانی شده:**

| اعمال می‌شود ✅ | اعمال **نمی‌شود** ❌ |
|---|---|
| هر ۴ مسیر نوشتن `BankCartDetails` (Create/Update/Import/Reconciliation) | **کل `AccountCodes`** (Create/Update/Delete) |
| `GetAllVoucherHeadByVahedYear` (۳ نسخه) | **کل مسیر نوشتن سند** (Create/Update/Delete/ChangeState) |
| `GetVouchersInfo` | **کل `Tafsili`**، `PayAndRecive`، `ChargeAndCost`، `Elms` |
| `GetAllMoin` / `GetMoinWithOutCheckList` | همهٔ `GetById`ها |
| `Accounts/GetAll`، `BankCartDetails/GetAllByAccountNumber` | بقیهٔ ~۳۶۰ Handler |

> ⚠️ `HaveAccessToProvince` و `HaveAccessToUnitType` هر دو `throw new NotImplementedException()` اند — یعنی طراحی شده ولی ساخته نشده.
>
> **درس برای ما:** الگوی مرجع خوبی است (استخراج واحد از توکن + سلسله‌مراتب + fail-loud)، ولی **اعمال per-handler دقیقاً همان چیزی است که باعث فراموشی می‌شود**. اگر ما این را پیاده کردیم، باید از طریق یک `Behavior` سراسری یا Global Query Filter باشد، نه فراخوانی دستی در هر Handler.

## ۲۱-۲. کدینگ حساب — `TB_ACCOUNTCODE`

| عملیات | Command/Query | منطق و قوانین |
|---|---|---|
| ساخت گروه | `AddGroupCodeCommand` | `TypeCode=Group` ثابت در Handler؛ کد ۲ رقم ≠`00`؛ `TypeActivity` ۱-۳ اجباری؛ `TypeAccCode` ۱\|۲ اجباری؛ `ParentId` ست نمی‌شود |
| ساخت کل | `AddKolCodeCommand` | `TypeCode=Kol`؛ کد ۴ رقم، هیچ نیم‌بخشی `00`؛ نام فقط حروف فارسی (`^[؀-ۿ\s]+$`)؛ `TypeActivity`/`TypeAccCode` **ست نمی‌شوند** |
| ساخت معین | `AddMoinCodeCommand` | `TypeCode=Moin`؛ کد ۶ رقم، هیچ‌کدام از ۳ بخش `00`؛ `TypeActivity` ۱-۷؛ `TypeAction` ست می‌شود؛ `LevelIds` → `TbAccountLinkLevel` |
| ویرایش ×۳ | `UpdateGroup/Kol/MoinCodeCommand` | همان قواعد طول کد. ⚠️ `UpdateMoinCodeValidator` روی تایپ اشتباه است → **عملاً اجرا نمی‌شود** |
| حذف | `DeleteAccountCodeCommand` | **۴ گارد** (بخش ۲-۵): معین→گردش/لینک تفصیلی؛ کل→فرزند؛ گروه→فرزند |
| خواندن | ۲۲ Query | `GetAllGroup/Kol/Moin`، `GetKolsByGroupIds`، `GetMoinsByKolIds`، `GetAccounCodelByParentId`، `SearchOnMoein`، `GetAllMoinsWithIdentifer`، `GetMoinWithOutCheckList`، ۳ فیلتر گزارش تجمیعی |

**نکتهٔ ساختاری:** سه Endpoint ساخت مجزا (نه یک Endpoint با پارامتر سطح) — `TYPECODE` هرگز ورودی کاربر نیست.

## ۲۱-۳. سند — `TB_VOUCHERSHEAD` / `TB_VOUCHERSDETAIL`

### Commandها (۲۲ مورد)

| Command | منطق |
|---|---|
| `AddVoucherCommand` | مسیر دستی. ⚠️ حلقهٔ ردیف‌ها کامنت شده → **فقط یک ردیف**. `DocLife=draft`، `Atf_Num` از `CreateNewVoucherAtfNumAsync`، `SysType.Code=="1"`، `IsAutomatic.manual`(=۱). گاردها: ماه صورتحساب‌شده، شمارهٔ تکراری، `ValidateTafsiliLevels` |
| `AddVoucherListCommand` | نسخهٔ چندردیفی. همان گاردها (ماه/تکراری) ولی **`ValidateTafsiliLevels` در آن کامنت است** (خطوط ۱۳۶-۱۴۱) → ناسازگاری واقعی |
| `AddVouchersHeadCommand` | فقط سرسند. گارد شمارهٔ تکراری |
| `UpdateVoucherHeadAcommand` | فقط `DateDoc`/`HeadDesc`/`DocNumber`. **`DOCLIFE` را لمس نمی‌کند.** گارد `accepted` + ماه صورتحساب‌شده |
| `DeleteVouchersHeadCommand` | گارد `reviewed`\|`accepted` + مغایرت بانکی |
| `AddVouchersDetailCommand` / `AddSingleVoucherDetailCommand` | افزودن ردیف؛ `ValidateTafsiliLevels`؛ صفت اختیاری |
| `UpdateVoucherDetailCommand` ⚙️ | ویرایش ردیف |
| `DeleteVouchersDetailCommand` | گارد: ردیف یافت نشد / سند اصلی یافت نشد / مغایرت بانکی |
| `ChangeStateCommand` | تغییر `DocLife` گروهی. **هیچ گارد گذاری ندارد** |
| `AddReversVoucherCommand` | سند برگشتی: `Debtor↔Creditor` معکوس، `DocLife=draft`، `ParentHeadId=` سند اصلی |
| `MergVoucherCommand` | ادغام؛ `reviewed`/`accepted` ممنوع |
| `SortVoucherCommand` | شماره‌گذاری مجدد بر `SortType` (۱ شماره، ۲ تاریخ)؛ **همه باید `temporary` باشند** |
| `InvoiceConfirmationCommand` | صورتحساب ماهانه (بخش ۱۷-۳) |
| `InvoiceReturnCommand` | برگشت صورتحساب |
| `AddOpeningVoucherCommand` | افتتاحیه (بخش ۱۷-۱) |
| `AddClosingVoucherCommand` | اختتامیه (بخش ۱۷-۲) |
| `UploadExcelCommand` ⚙️ | ورود سند از اکسل |
| `AddTmpVoucherHeadCommand` | سند موقت؛ ردیف‌ها **تعبیه‌شده** |
| `AddVochersImportTempCommand` | تبدیل سند موقت به سند اصلی؛ `ValidateMoinCode`/`ValidateTafCode` (تبدیل **کد** به **Id**) |
| `DeleteTmpVoucherHeadCommand` ⚙️ | |
| `AddAttachmentCommand`/`DeleteAttachFileCommand` | پیوست؛ `GetRadif` برای شمارهٔ ردیف پیوست |

### Queryها (۲۰ + ۳۱ گزارشی)

`GetAllByVahedCodeAndYear` (۳ نسخه، با `HaveAccessToUnit`)، `GetVoucherByIdForEdit`، `GetVoucherHeadForEdit`، `GetVoucherDetailForEdit` (شامل `Remainder = ΣDebtor - ΣCreditor`)، `GetDetailForShow`، `GetVoucherDetailByHeadIds` (کپی)، `GetAtfNo`، `GetVoucherNo`، `GetDocLife` (⚠️ `throw new NotImplementedException()`)، `GetExistVoucher`، `GetUserByVoucherId`، `GetLastRadifAttachment`، `GetTurnoverByAccountCodeId`، `GetVouchersHeadByVahedTypeId`، `GetAllElmVoucher`.

## ۲۱-۴. تفصیلی — `TB_TAFSILI` و جدول‌های مرتبط

| عملیات | منطق و قوانین |
|---|---|
| `AddTafsiliCommand` | **کد تفصیلی یکتا** (`TafsiliException("تفصیلی تکراری می باشد")`). `PersonType=Other` → کد با پیشوند `VahedCode`. `Owner=Global` → `VahedCode=""` (تفصیلی سراسری). لینک به گروه‌های تفصیلی (`AddTafsiliLinkTafsilGroup`) + `tafsiliUnitAccessRepository`. ⚠️ فراخوانی سرویس‌های بیرونی **کد ملی** و **ثبت شرکت‌ها** کامنت شده |
| `UpdateTafsiliCommand` | `GetByIdIcludVoucherAsync` → اگر در سند استفاده شده محدود می‌شود |
| `DeleteTafsiliCommand` | `GetByIdIcludVoucherAsync` → «تفصیلی قابل حذف نمی باشد»؛ `tafsiliUnitAccess` هم حذف می‌شود |
| پیوست | حداکثر **۲ فایل** به ازای هر تفصیلی |
| `TB_TAFSIL_GROUP` | `Add`/`Update`/`Delete` ⚙️ |
| `TB_TAFSIL_LINK_TAFSILGROUP` | `Add`/`Update`/`Delete` — **CRUD مستقل دارد** |
| `TB_LEVEL_TAFSIL` | `Add`/`Update`/`Delete` — سطوح ۱..۷ |
| **`TB_ACCOUNT_LINK_LEVEL`** | `AddRange`/`Update`/`Delete` — **CRUD مستقل دارد** (منبع حقیقت اجباری‌بودن تفصیلی) |
| `TB_ACCOUNT_LINK_TAFSILGROUP` | `AddRange`/`Update`/`Delete`/`DeleteByTafGroupId`؛ گارد `GetTuroverByTafGroupIdAsync` (گردش) |

> 🔴 **تکرار هشدار بخش ۱۱-۲:** دو جدول `_LINK_*` در سیستم قدیمی CRUD مستقل دارند، برخلاف قاعدهٔ تیمی ما. این‌ها **جدول پیکربندی** اند نه لینک تراکنشی.

## ۲۱-۵. چک و دسته‌چک — `TB_CHECK` / `TB_CHECKBOOK`

| عملیات | قوانین |
|---|---|
| `AddCheckBookCommand` | **«طول فیلد اولین برگ چک با فیلد آخرین برگ چک برابر نمی باشد»** — طول رشتهٔ سریال اول و آخر باید یکی باشد |
| `UpdateCheckBookCommand` | همان + «اوراق ایجاد شده» → تغییر بازه ممنوع + «اوراق استفاده شده» → ممنوع |
| `DeleteCheckBookCommand` | «اوراق دسته چک انتخابی استفاده شده و قابل حذف نمی باشد» |
| `AddCheckTypeCommand`/`Update`/`Delete` | حذف نوع چک → `checkBookRepository.updateRangeCheckBookAsync` (cascade) |
| `AddChechInCurrentCommand` ⚙️ | چک در جاری |
| Query | `GetAllCheckBook`، `GetCheckBookByAccountId`، `CheckBookCartable`، `PrintQuery`/`PrintHtmlQuery` (چاپ چک) |

## ۲۱-۶. دریافت/پرداخت — `TB_PAYRECIVHEAD` / `TB_PAYRECIVDETAIL`

| عملیات | منطق |
|---|---|
| `AddPayReciveCommand` | ترکیبی (هدر+ردیف‌ها). گاردها: شمارهٔ تکراری، `ValidateTafsiliLevels`، فیش/حواله تکراری (⚠️ کامنت شده). ایجاد `Receipt` و به‌روزرسانی `Check` |
| `UpdatePayReciveCommand` | گارد شمارهٔ تکراری |
| `DeletePayReciveCommand` | «برای ردیف انتخابی سند صادر شده و قابل حذف نمی باشد» |
| `AddPayReciveDetailSingleCommand` | ردیف مستقل؛ `GetMaxRadifAsync` برای شمارهٔ ردیف |
| `UpdatePayReciveDetailSingleCommand` | ⚠️ نسخهٔ ضعیف اعتبارسنجی تفصیلی |
| `DeletePayReciveDetailSingleCommand` | گارد مغایرت بانکی؛ اگر آخرین ردیف باشد **سرسند هم حذف می‌شود** |
| **`AddVoucherByPayAndReciveCommand`** | 🔴 **تنها جای enforce تراز** + معکوس‌سازی بدهکار/بستانکار (بخش ۱۴) |
| پیوست | `Add`/`Delete` |

## ۲۱-۷. اعلامیه — `TB_ELAMHEAD` / `TB_ELAMDETAIL`

سه نوع (`ElamType`): صادره، رسیده، صادرهٔ درآمد. ماشین وضعیت `WebStat` ۱۰ مرحله‌ای.

| عملیات | منطق |
|---|---|
| `AddOtherElamCommand` | سرسند+ردیف ترکیبی؛ `GetNewElamSerialNoAsync`؛ `GetRabetMoinByElamTypeAsync` (**حساب از `Rabet` می‌آید نه hardcode**)؛ `ValidateTafsiliLevels` |
| `AddDrmdElamCommand` | اعلامیهٔ درآمد (حق بیمهٔ کارکنان) |
| `AddElamReciveVahedCommand` | اعلامیهٔ رسیده |
| `Update`/`Delete` (هر سه نوع) | **«اعلامیه ارسال شده قابل ویرایش/حذف نمی باشد»** |
| `ChangeWebStatCommand` | **«ابتدا برای اعلامیه انتخابی سند صادر نمایید»** + «اعلامیه آماده ارسال می باشد» |
| `SendElmDrmdCommand` | ۶ اعتبارسنجی پیش از ارسال SOAP (کد کارگاه ۱۰ کاراکتر، شماره/تاریخ بدهی، نام کارگاه، دو کد واحد) |
| `SendElamVoucherCommand` / `CreateRcvElamVoucherCommand` | صدور سند حسابداری از اعلامیه |
| `AddElamDetailCommand`/`Update`/`Delete` | CRUD مستقل ردیف؛ حذف آخرین ردیف → حذف سرسند |

## ۲۱-۸. شارژ/هزینهٔ تنخواه — `TB_CHARGEANDCOST*`

ماشین وضعیت مستقل `Status` (**۰**=موقت، ۱=بررسی‌شده، ۲=تایید دائم) — ⚠️ **از ۰ شروع می‌شود برخلاف `DocLife`**.

`AddChargeAndCostCommand` (ترکیبی، گارد کد درخواست تکراری)؛ `Detail`: `Add`/`Update`/`Delete`/`DeleteSingle`؛ `Head`: `Update`/`Delete` + سه Command تغییر وضعیت مجزا (`ChangeStatusToTemporary`/`Reviewed`/`Accept`).
Query: `Cartable/Search`، `GetMaxCode`، `GetExistCode`، `GetAllAccepted`، `GetDetailsForCreatePay`، `GetCostByHeadId`.

## ۲۱-۹. شناسه (Identity) — فرا-دادهٔ ردیف سند

`IdentityGroup` → `IdentitySubGroup` → `IdentityDetail`/`IdentityFixItem`.

| عملیات | قوانین |
|---|---|
| `AddIdentityGroupCommand`/`Update`/`Delete` | حذف: «برای گروه انتخابی ویژگی تعریف شده» |
| `AddIdentitySubGroupCommand`/`Update`/`Delete` | حذف: «برای ویژگی انتخابی فیلد متغیر یا ثابت تعریف شده»؛ `SubGrpsType` = `IdentitySubGroupType` (تاریخ/فارسی/عدد/لاتین)؛ `Kind` = ثابت\|متغیر |
| `AddIdentityHeadCommand` | `GetMaxSerialAsync` — تولید سریال |
| `UpdateIdentityDetailCommand` | فقط Update (ساخت از طریق ردیف سند) |
| `UpdateIdentityFixItemCommand` | اقلام ثابت |

## ۲۱-۱۰. رابط — `TB_RABET` / `TB_RABET_TYPE` / `TB_RABET_CLOSING`

**`Rabet` = نگاشت «نوع رابط → کد حساب»** یعنی حساب‌های سیستمی قابل‌پیکربندی. مصرف واقعی: `GetRabetMoinByElamTypeAsync` و `GetRabetMoinByRabetCodeAsync` در Elam.

| عملیات | گارد |
|---|---|
| `AddRabetCommand` | «هیچ نوع رابطی موجود نیست»؛ «این رابط قبلاً تعریف شده است» |
| `Update`/`Delete` | **`GetTuroverByIdAsync`** → «به دلیل گردش مالی امکان … نیست» |
| `AddRabetClosingCommand` | «کد رابط نمی‌تواند خالی باشد»؛ **«کد حساب و کد رابط نمی‌توانند یکسان باشند»**؛ «کد حساب نامعتبر»؛ «هیچ واحد سازمانی برای کد نوع والد یافت نشد»؛ `AddRangeAsync` (ضرب دکارتی در واحدها) |
| `RabetClosing` Update/Delete | همان گارد گردش مالی |

## ۲۱-۱۱. صفت (Attrib) — `TB_ATTRIBFORACCOUNTCODE` / `TB_ATTRIBINVOUCHER`

تعریف صفت سفارشی روی معین (`FlagEnum` عدد\|تاریخ، `ControlEnum` غیرصفر\|تاریخ، `AttribSumEnum` جمع‌پذیر\|جمع‌ناپذیر)، سپس مقداردهی هنگام ثبت سند.

`AddAttribForAccountCodeCommand`/`Update`/`Delete` (گارد: «برای ردیف انتخابی شناسه ای در اسناد ثبت شده»)؛ `AddAttribInVoucherCommand` + نسخهٔ `CreateWithAccountId` (که خودش `AttribForAccountCodeId` را از `AccountId` استخراج می‌کند)/`Update`/`Delete`.
گزارش‌ها: `AttributeMoinsReport`، `AttributeValueForMoinsReport`، **`AttributeMisMatchReport`** (مغایرت صفت).

## ۲۱-۱۲. بانک — `TB_ACCOUNT` / `TB_BANKCARTDETAIL`

⚠️ `TB_ACCOUNT` = **حساب بانکی** (نه کد حساب).

`AddAccountCommand`/`Update`/`Delete`؛ `AddAccountTypeCommand`.
`BankCartDetail` (صورت‌حساب بانکی): `Add`/`Update`/`Delete` (گارد مغایرت‌گیری‌شده)، **`ImportDisketCommand`** (فایل `STM001`، ردیف ۱۳۹ کاراکتر، گارد ماه و شماره‌حساب، `CheqNoExists`)، **`ReconciliationCommand`** (تطبیق با `CheckOrElmSoriExist` و `ReceiptExist`، به‌روزرسانی گروهی چک/فیش/ردیف بانکی).
🟢 **هر ۴ مسیر نوشتن `HaveAccessToUnit` دارند** — تنها ماژولی که چندمستأجری را کامل رعایت می‌کند.

## ۲۱-۱۳. سال مالی — `TB_YEAR`

`AddFinancialYearCommand`: `GetLastActiveYearAsync` → «هیچ سال مالی فعالی یافت نشد»؛ کنترل زمانی → «ایجاد سال مالی در این زمان ممکن نیست»؛ «سال مالی {x} قبلاً ایجاد شده است».
**`AddUnitFinancialYearCommand`**: افتتاح سال برای یک واحد — انتقال حساب‌های بانکی (`GetByVahedCodesAsync`+`UpdateAsync`)، ایجاد دسته‌چک، و `GetFirstAmountAsync` (مانده اول دوره).
`DeleteFinancialYearCommand` ⚙️.

## ۲۱-۱۴. سایر ماژول‌ها

| ماژول | عملیات و قوانین |
|---|---|
| **Expenses** | `ExpenseGroup` Add/Update/Delete (حذف: `expenseRepository.AnyAsync` → گروه دارای هزینه حذف نمی‌شود)؛ `Expense` Add/Update/Delete + `GetMaxExpenseCode` |
| **RevolvingFund** (تنخواه) | Add/Update/Delete؛ حذف: «در قسمت شارژ تنخواه استفاده شده» |
| **PersonAction** | Add/Update/Deactivate؛ گاردهای `HasActiveRoleAsync`/`HasActiveUserAsync`/`HasActiveRoleExceptAsync` → **یک نقش فعال به‌ازای هر شخص** |
| **WorkShop** | Add/Update/ChangeState |
| **PreDescrib** (شرح آماده) | Add (گارد «کد معین نامعتبر است»)/Delete؛ `Flag` = سرسند(۰)\|ردیف(۱) |
| **Receipt** (فیش/حواله) | Add؛ «شماره فیش /حواله تکرای می باشد» |
| **WhiteAndBlackList** | Add (ضرب دکارتی حساب×نوع‌واحد)/Update/Delete/`ChangeStateToBlackListed`/`ReActive`. 🔴 **هرگز در مسیر نوشتن اعمال نمی‌شود** |
| **WhiteList** | فقط `Add` + ۷ Query |
| **VahedInfo/VahedType/Province/City/Bank** | فقط `Add` + Queryهای درختی |
| **BillLog** | فقط `Add` (از `InvoiceConfirmation`) + `BillLogReport` |

## ۲۱-۱۵. گزارش‌ها — `ReportController` (۴۱ Endpoint)

### تراز آزمایشی (Trial Balance) — مهم‌ترین بخش

سه نوع، هرکدام با `TrialBalanceGridShowType` (۱گروه ۲کل ۳معین ۴تفصیلی) و drill-down:

| نوع | ستون‌های عددی |
|---|---|
| **۴ ستونه** (`TrialBalanceReportviewModel`) | `Debtor`, `Creditor`, `DebtorBalance`, `CreditorBalance` |
| **۶ ستونه** (`TrialBalance6ReportviewModel`) | + `FirstDebtor`, `FirstCreditor` (مانده اول دوره) |
| **۸ ستونه** (`TrialBalance8ReportviewModel`) | + `TotDebtor`, `TotCreditor` (گردش تجمعی) |

همه به‌علاوهٔ `Code` + `Description`.

**فرمول‌ها (از کامنت‌های خودِ DTO):**
```
DebtorBalance  = TotDebtor > TotCreditor ? TotDebtor - TotCreditor : 0
CreditorBalance= TotCreditor > TotDebtor ? TotCreditor - TotDebtor : 0
FirstDebtor    = TotDebtor - CurDebtor
```
در SQL با `greatest(nvl(sum(debtor),0) - nvl(sum(creditor),0), 0)` پیاده شده.

**منبع داده: دو View اوراکل** — `VWTRIALREPORTWRAPPER` (دارای `groupCode/groupName/kolcode/kolname/moinCode/moinName/debtor/creditor/year/vahedcode`) و `VWTRIALREPORTWRAPPERTAFSILI`. تنها تفاوت چهار سطح، **ستون `GROUP BY`** است.

**`TrialBalanceFilterDto` (۱۸ فیلد فیلتر):** `FromVoucherNo`/`To`, `FromAtfNum`/`To`, `FromYear`/`To`, `VahedCode`, `VahedsList`, `FromDate`/`To`, `FromCodeKol`/`To`, `FromMoein`/`To`, `CreatedUser`, `VahedType`, `DocLife`, `SystemType`.

### بقیهٔ گزارش‌ها

`AccJournalReport` (دفتر روزنامه)، `LedgerReport` (دفتر کل)، `BalanceSheetReport` + `GeneralBalanceSheetReport` (ترازنامه)، `ConsolidatesGroup/Kol/Moein` (تجمیعی)، `ConsolidateReport1` + Filter + Wrapper + `GetMatrixReport` (ماتریسی با `ConsolidateReportType` ۰..۱۰)، `VoucherReviewReport`، `TafsiliReview`/`TafsiliGroupReview`/`TafsiliVoucherReview`، `RelationGroupWithMoeinReport`، `ReportForOpeningVoucher`/`ReportForClosingVoucher`، `AgePyramidReport` (هرم سنی بدهی)، `BillLogReport`، `GetFirstAndLast`، `GenerateReportHtml`/`PrintCheque` (چاپ چک).

## ۲۱-۱۶. کارتابل — `CartableController`

🔴 **کشف ساختاری: `Cartable` یک Entity نیست — یک façade است.**

همان Commandهای `VoucherController` را دوباره expose می‌کند (`ChangeState`, `MergVoucher`, `SortVoucher`, `AddReversVoucher`, `AddSingleVoucherDetail`, `UpdateVoucherHeadAcommand`, `UpdateVoucherDetailCommand`, `DeleteVouchersHeadCommand`, `DeleteVouchersDetailCommand`) + Queryهای مخصوص میز کار: `GetCartable`, `GetVoucherByIdForEdit`, `GetVoucherHeadForEdit`, `GetVoucherDetailForEdit`, `GetDetailForShow`, `GetVouchersInfo`, `GetVoucherDetailByHeadIds` (کپی سند), `GetUserByVoucherId`.

منبع خواندن: View اوراکل **`VWCARTABLE`** (`VwCartable.cs` با `SumDebtor`/`SumCreditor`/`DETAILCOUNT`).

> **دو Controller روی یک مجموعه Command** — یکی برای ورود داده، یکی برای بازبینی/تأیید. الگوی قابل‌توجه برای فاز UI ما.

## ۲۱-۱۷. زیرپروژه‌های باقی‌مانده

| پروژه | محتوا |
|---|---|
| **`Shared`** | `DateHelper` (تاریخ شمسی)، `EnumExtensions.ToDescription()`، `DictionaryConverter`، **`ExportToExcelService`/`ExportToPdfService`**، و **`NationalCodeService` + `CompanyService`** (استعلام کد ملی و ثبت شرکت‌ها — در `AddTafsiliHandler` کامنت شده‌اند) |
| **`IDP`** | `TokenManager` + `ITokenManager` + `TokenManagerConfiguration` + `ClaimsPrincipalExtensions` — **مبدأ کدی که ما در فاز ۷ پورت کردیم** |
| **`Infrastructure.Logging`** | Serilog + سه Enricher (`IdentityUser`, `Referrer`, `XForwardedFor`) + دو Formatter |
| **`Infrastructure.Service`** | **`ElamDrmdWebService`** (SOAP، `Connected Services/elmDrmd/Reference.cs`) و **`KafkaVoucherConsumerService`** (پایین) |
| **`Utility.Exception`** | سلسله‌مراتب استثنا: `BaseException`, `DomainBaseException`, و ۹ کلاس `ACL*` (`ACLBadRequest`, `ACLConflict`, `AclForbidden`, `ACLNotFound`, `ACLUnauthorized`, `ACLUnprocessableAction`, `AclUnexpectedResponse`, `AclAggregatorHandover`) |
| **`Presentation.Worker`** / **`Payments`** | ⚠️ **کاملاً خالی** — هیچ فایل `.cs` ندارند |

### 🔴 مسیر ورود سند از Kafka (کشف پاس چهارم)

`Infrastructure.Service\Messaging\Kafka\KafkaVoucherConsumerService.cs` (۲۳۵ خط) — یک `BackgroundService` که:
- به `Kafka:Topic` گوش می‌دهد؛ `Kafka:DLQTopic` و `Kafka:ProcessedTopic` (پیش‌فرض `{topic}-DLQ` / `{topic}-Processed`).
- پیام را به `VoucherMessageDto` دیسریالایز می‌کند و **`AddTmpVoucherHeadCommand`** می‌سازد (یعنی به **سند موقت** می‌رود، نه مستقیم سند اصلی).
- خطا → DLQ؛ موفق → Processed topic.

**`VoucherMessageDto`:** `DateDoc`, `HeadDesc`, `DocType`, `AddUserId`, `VahedCode`, `Year`, `SourceId`, `List<VoucherDetailDto>`.
**`VoucherDetailDto` (Kafka):** `MoinCode`, `TafsiliCode1..7`, `Description`, `Radif`, `Debtor` (`long`), `Creditor` (`long`), `AttribValue`, `CheckDate`.

> ⚠️ **نکتهٔ مهم:** این DTO با **کد** کار می‌کند (`MoinCode`, `TafsiliCode1..7`) نه `Guid` — یک لایهٔ resolution لازم دارد (`ValidateMoinCode`/`ValidateTafCode` در `AddVochersImportTempCommandHandler`).
> ⚠️ `AddUserId` **در پیام Kafka می‌آید** — یعنی در این مسیر هویت از بیرون گرفته می‌شود، برخلاف مسیر HTTP که از توکن می‌آید. ❓ **حدس نزدم** که آیا اعتبارسنجی می‌شود.

---

# بخش ۲۲ — وضعیت نهایی پوشش پس از پاس چهارم

## ✅ پوشش کامل (۱۰۰٪)
- هر ۴۴ enum + تطبیق با ۴۲ پراپرتی `bool?` ما (بخش ۱۰).
- هر ۱۵۶ Command و ۲۲۱ Query — نام، ماژول، و **منطق/قوانین** (بخش ۲۱).
- هر ۳۷۲ Handler — استخراج مکانیکی فراخوانی‌های repository + هر `throw new`.
- هر ۴۲ Controller — verbها، مسئولیت، و برای `ReportController`/`CartableController`/`AccountCodeController` فهرست کامل Endpointها.
- هر ۱۰ پروژهٔ solution (شامل تأیید خالی‌بودن `Presentation.Worker` و `Payments`).
- شکل کامل گزارش‌های تراز ۴/۶/۸ ستونه + فرمول‌ها + منبع داده.
- مکانیزم چندمستأجری و دامنهٔ دقیق اعمالش.

## ⚠️ پوشش جزئی
- **بدنهٔ Handlerها:** ~۴۰ از ۳۷۲ خط‌به‌خط خوانده شد. بقیه از طریق استخراج مکانیکی (با ⚙️ علامت خورده‌اند).
- **DTOها:** ~۱۵ از ~۲۰۰ خوانده شد (تراز، Kafka، فیلترها). بقیه نه.
- **Repositoryها:** ۶ از ۴۰+.

## ❌ همچنان پوشش داده نشد
1. **SQL کامل ~۲۵ گزارش** در `VouchersDetailRepository.cs` (۲٬۷۶۶ خط) — شکل خروجی و منبع View مشخص است، ولی منطق داخلی هر کوئری نه.
2. **~۱۸۵ فایل DTO باقی‌مانده** — شکل دقیق ورودی/خروجی اکثر Endpointها.
3. **`ApplicationUseCases\Behaviors\`, `Abstractions\`, `Registration.cs`** — pipeline اعتبارسنجی.
4. **`Account.Tests` / `Application.Test`**.
5. **`Tamin.Core\Services\` و `Common\`**.
6. **`OracleExpressionToSqlConverter`** — امنیت فیلتر پویا.
7. **`ElamDrmdWebService`** (SOAP) و بدنهٔ کامل Kafka consumer.
8. **دادهٔ زندهٔ Oracle** — پس **همچنان حل‌نشده:** تناقض `TYPEACTIVITY`، عرض `VAHEDTYPE`، باگ محتمل `IsAutomatic`، و وجود مقادیر خارج از دامنهٔ enum. ⚠️ **به‌روزرسانی ۲۰۲۶-۰۸-۲۶:** اولین اتصال واقعی به Oracle زنده انجام شد — رجوع به بخش ۲۳ (`TYPEACTIVITY` و `IsAutomatic` حل شدند؛ `VAHEDTYPE` تا حدی؛ ضمناً یک یافتهٔ جدید دربارهٔ مقادیر خارج از دامنهٔ enum کشف شد).

---
---

# بخش ۲۳ — تأیید با دادهٔ زندهٔ Oracle ما (۲۰۲۶-۰۸-۲۶)

> **این اولین اتصال واقعی پروژهٔ ما (نه پروژهٔ مرجع `D:\CentralAccount`) به دیتابیس زندهٔ `CENTRALACCOUNT` است.**
> **فقط `SELECT` اجرا شد.** هیچ `INSERT`/`UPDATE`/`DELETE`/`MERGE`/DDL/`ALTER SESSION`ای اجرا نشد. کوئری‌ها با یک پروژهٔ کنسول یک‌بارمصرف (`Oracle.ManagedDataAccess.Core` 23.26.300، خروجی `UTF-8`) در scratchpad اجرا شدند تا مشکل شناخته‌شدهٔ خرابی encoding فارسی در `sqlplus` روی ویندوز دور زده شود؛ متن فارسی صحیح خوانده شد (تأیید بصری در خروجی خام پایین). پروژهٔ کنسول و خروجی خام پس از این کار از scratchpad پاک شدند؛ **connection string در هیچ فایلی نوشته نشد** (فقط از طریق متغیر محیطی shell که خودش هرگز echo/log نشد).
> Connection: `Data Source=db3rh:1521/setadidevdb.tamin.org` (بدون کاربر/رمز در این سند). `ServerVersion=19.4.0.0.0`.
> نام ستون FK در `TB_VOUCHERSDETAIL` قبل از هر کوئری از Entity (`Accounting.Domain/Entity/TB_VOUCHERSDETAIL.cs`) تأیید شد: `ACCOUNT_ID` (نه `ACCOUNTCODE_ID`). عنوان `TB_SYSTYPE` هم از Entity تأیید شد: `SYS_NAME`.

## ۲۳-۱. ✅ ابهام ۱ — `TB_ACCOUNTCODE.TYPEACTIVITY` — **حل شد قطعی: enum `Tamin.Core` درست است، کامنت ستون Oracle نادرست/کهنه است**

**نتیجه‌گیری:** `TYPEACTIVITY = 1` یعنی **بدهکار**، `TYPEACTIVITY = 2` یعنی **بستانکار** — دقیقاً مطابق enum `TypeActivity` در `Tamin.Core` (`Debit=1, Credit=2`)، و **برخلاف** کامنت ستون Oracle در schema ما («۱بستانکار۲بدهکار»).

**شاهد قطعی‌کننده (روش ب، سطح گروه):** قوی‌ترین و تمیزترین شاهد از حساب‌های **سطح گروه** (`TYPECODE=1`) با نام‌های بدیهی حسابداری آمد — این‌ها تنها ردیف‌هایی بودند که هم نام فارسی روشن داشتند و هم مستقیماً enum را آزمایش می‌کردند (بدون واسطهٔ گردش سند):

```sql
SELECT ACCCODE, ACCCODENAME, TYPEACTIVITY FROM TB_ACCOUNTCODE
WHERE TYPECODE=1 AND TYPEACTIVITY IN (1,2) ORDER BY TYPEACTIVITY, ACCCODE;
```

| `TYPEACTIVITY` | نام حساب | ماهیت واقعی حسابداری |
|---|---|---|
| **۱** | `دارايي هاي جاري` (دارایی جاری) | **بدهکار** ✅ |
| **۱** | `داراريي هاي غير جاري` (دارایی غیرجاری) | **بدهکار** ✅ |
| **۱** | `دارايي هاي غيرجاري` (دارایی غیرجاری) | **بدهکار** ✅ |
| **۱** | `هزينه ها` (هزینه‌ها) | **بدهکار** ✅ |
| **۲** | `بدهي هاي جاري` (بدهی جاری) — دو ردیف | **بستانکار** ✅ |
| **۲** | `بدهي هاي غيرجاري` (بدهی غیرجاری) | **بستانکار** ✅ |

هر ۶ حساب معنادار (غیرتستی) در این کوئری **۱۰۰٪** با enum سازگارند: دارایی/هزینه (که در حسابداری متعارف قطعاً بدهکارند) دقیقاً `TYPEACTIVITY=1` دارند، و بدهی (که قطعاً بستانکار است) دقیقاً `TYPEACTIVITY=2` دارد. مابقی ردیف‌های همان کوئری نام‌های تستی/بی‌معنی داشتند (`test1`, `تست`, `hdsgd`, ...) و در نتیجه‌گیری لحاظ نشدند.

**شاهد تأییدی دوم (روش الف، رفتار گردش سند):**
```sql
SELECT a.ACCCODE, a.ACCCODENAME, a.TYPEACTIVITY,
       SUM(NVL(d.DEBTOR,0)) AS SUM_DEBTOR, SUM(NVL(d.CREDITOR,0)) AS SUM_CREDITOR, COUNT(*) AS LINES
FROM TB_ACCOUNTCODE a JOIN TB_VOUCHERSDETAIL d ON d.ACCOUNT_ID = a.ID
WHERE a.TYPEACTIVITY IN (1,2) GROUP BY a.ACCCODE, a.ACCCODENAME, a.TYPEACTIVITY;
```
تنها ردیف با گردش سند واقعی: `بانک ملت` (حساب معین بانکی — دارایی، قطعاً بدهکار) با `TYPEACTIVITY=1` و `SUM_DEBTOR=10, SUM_CREDITOR=0`. جهت گردش ۱۰۰٪ بدهکار است — سازگار با enum. (دادهٔ محیط توسعه محدود است؛ این تنها یک نمونه است، ولی جهت‌دار و بدون تناقض.)

**شاهد تأییدی سوم (روش ج، ساختار سلسله‌مراتبی):**
```
TYPECODE=2 (کل) → TYPEACTIVITY همیشه NULL   (۶۲/۶۲ ردیف)   ✅ مطابق قاعدهٔ استخراج‌شده از Validator مرجع
TYPECODE=1 (گروه) → TYPEACTIVITY در {۱..۶} یافت شد
TYPECODE=3 (معین) → TYPEACTIVITY در {۱..۵} یافت شد
```
تأیید مستقل پنجم برای نگاشت `TYPECODE` (۱گروه/۲کل/۳معین؛ رجوع به بخش ۱-۱) — کل هیچ‌وقت `TYPEACTIVITY` ندارد، دقیقاً همان‌طور که مرجع می‌گفت.

### خروجی خام (کامل، بدون ویرایش)

```
TYPEACTIVITY | COUNT(*)
1 | 16
2 | 13
3 | 54
4 | 2
5 | 2
6 | 1
NULL | 62
(7 rows)

TYPECODE | TYPEACTIVITY | COUNT(*)
1 | 1 | 10
1 | 2 | 10
1 | 3 | 5
1 | 4 | 1
1 | 5 | 1
1 | 6 | 1
2 | NULL | 62
3 | 1 | 6
3 | 2 | 3
3 | 3 | 49
3 | 4 | 1
3 | 5 | 1
(12 rows)
```

⚠️ رجوع به «۲۳-۴ یافتهٔ جدید» پایین‌تر — همین جدول یک ناسازگاری غیرمنتظره با قاعدهٔ Validator گروه (۱..۳) نشان می‌دهد.

## ۲۳-۲. ⚠️ ابهام ۲ — عرض `VAHEDTYPE` — **تا حدی حل شد**

**آنچه قطعی شد:** عرض فیزیکی ستون واقعاً `NUMBER(1)` با `DATA_PRECISION=1` است — Fluent Mapping فعلی ما از نظر عرض **درست** است (احتمال «۲: عرض واقعی بزرگ‌تر است» رد شد).

```sql
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, DATA_PRECISION, DATA_SCALE, DATA_LENGTH, NULLABLE
FROM ALL_TAB_COLUMNS WHERE OWNER='CENTRALACCOUNT' AND COLUMN_NAME LIKE '%VAHEDTYPE%' ORDER BY TABLE_NAME;
```
```
TABLE_NAME                  | COLUMN_NAME  | DATA_TYPE | DATA_PRECISION | DATA_SCALE | DATA_LENGTH | NULLABLE
TB_ACCOUNTEXCEPTION         | VAHEDTYPE_ID | CHAR      | NULL           | NULL       | 36          | N
TB_RABET_CLOSING            | VAHEDTYPE_ID | CHAR      | NULL           | NULL       | 36          | N
TB_TAFSILI                  | VAHEDTYPE    | NUMBER    | 1              | 0          | 22          | Y
TB_TAFSIL_LINK_TAFSILGROUP  | VAHEDTYPE    | NUMBER    | 1              | 0          | 22          | Y
TB_VAHED_INFO               | VAHEDTYPE_ID | CHAR      | NULL           | NULL       | 36          | N
TB_WHITEANDBLACKLIST        | VAHEDTYPE_ID | CHAR      | NULL           | NULL       | 36          | Y
TB_WHITELIST                | VAHEDTYPE_ID | CHAR      | NULL           | NULL       | 36          | Y
VWTAFSILILIST                | VAHEDTYPE    | NUMBER    | 1              | 0          | 22          | Y
(8 rows)
```

نکتهٔ جانبی مهم: پنج ستون دیگر با نام مشابه (`VAHEDTYPE_ID`) اصلاً همین ستون نیستند — `CHAR(36)` یعنی FK از نوع GUID به یک جدول دیگر، کاملاً بی‌ربط به این ابهام. فقط `TB_TAFSILI.VAHEDTYPE` و `TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE` (به‌علاوهٔ View `VWTAFSILILIST`) واقعاً `NUMBER(1)` هستند.

**آنچه همچنان حل‌نشده — حدس زده نشد:** توزیع مقدار واقعی:
```sql
SELECT VAHEDTYPE, COUNT(*) FROM TB_TAFSILI GROUP BY VAHEDTYPE ORDER BY VAHEDTYPE;
```
```
VAHEDTYPE | COUNT(*)
1 | 1
3 | 25
NULL | 4
(3 rows)          -- TB_TAFSIL_LINK_TAFSILGROUP تقریباً یکسان: 1→1, 3→27, NULL→4
```
`MIN=1, MAX=3`. یعنی دادهٔ زنده (۲۶ ردیف غیر-NULL، یک محیط توسعه با حجم کم) فقط دو مقدار `{1, 3}` را نشان می‌دهد — نه بازهٔ کامل ۱..۱۷ که `TypeVahed` دارد، و نه حتی ۱..۹ که احتمال سوم پیش‌بینی کرده بود. **این را حدس نمی‌زنم که ستون واقعاً همان `TypeVahed` باریک‌شده است یا یک enum کاملاً متفاوت** — چون تعیین اینکه مقادیر ۱ و ۳ در `TypeVahed` به چه معنایی‌اند نیازمند خواندن فایل enum در `D:\CentralAccount` است که خارج از scope این کار (فقط دادهٔ زنده) بود. **پیشنهاد صریح برای Task بعدی:** اگر `entity-mapper`/`backend-dotnet` بخواهد این ستون را enum کند، باید یا (الف) این خواندن تکمیلی روی enum مرجع انجام شود، یا (ب) به‌جای enum کامل `TypeVahed`، فقط یک enum باریک با دو مقدار مشاهده‌شده تعریف شود — با آگاهی از اینکه ممکن است دادهٔ Production مقادیر بیشتری داشته باشد.

## ۲۳-۳. ✅ ابهام ۳ — `TB_VOUCHERSHEAD.ISAUTOMATIC` — **باگ محتمل تأیید شد**

```sql
SELECT ISAUTOMATIC, COUNT(*) FROM TB_VOUCHERSHEAD GROUP BY ISAUTOMATIC ORDER BY ISAUTOMATIC;
```
```
ISAUTOMATIC | COUNT(*)
0 | 3
1 | 54
(2 rows)
```
۹۴٫۷٪ (۵۴ از ۵۷) اسناد `ISAUTOMATIC=1` دارند — طبق معیار تفسیر همین تحقیق («اگر تقریباً ۱۰۰٪ مقدار ۱ باشد، فرضیهٔ باگ تأیید می‌شود»)، **این معیار برآورده شده است.**

**آزمون متقاطع (قوی‌تر):**
```sql
SELECT h.ISAUTOMATIC, h.SYSTEM_TYPE, s.SYS_COD, s.SYS_NAME, COUNT(*)
FROM TB_VOUCHERSHEAD h LEFT JOIN TB_SYSTYPE s ON s.ID = h.SYSTEM_TYPE
GROUP BY h.ISAUTOMATIC, h.SYSTEM_TYPE, s.SYS_COD, s.SYS_NAME ORDER BY 1,2;
```
```
ISAUTOMATIC | SYSTEM_TYPE                          | SYS_COD | SYS_NAME          | COUNT(*)
0           | 2675b98a-f9fa-2a68-e063-0100007fe971 | 4       | دريافت و پرداخت   | 3
1           | f4968f6b-3e7d-4976-88cd-f1ce0b023482 | 1       | حسابداري          | 54
(2 rows)
```

**تفسیر:** همبستگی **کامل و یک‌به‌یک** بین `ISAUTOMATIC` و `SYSTEM_TYPE` است — هیچ همپوشانی‌ای نیست. اسناد ماژول عمومی «حسابداري» (`SYS_COD=1`، ماژولی که منطقاً محل ثبت **دستی** سند است) همگی `ISAUTOMATIC=1` دارند؛ اسناد زیرسیستم تخصصی «دريافت و پرداخت» (`SYS_COD=4`، که منطقاً یک زیرسیستم **خودکارتر/تخصصی‌تر** برای تولید سند از تراکنش مالی است) همگی `ISAUTOMATIC=0` دارند. این دقیقاً **برعکس** انتظار ساده (ماژول عمومی/دستی = ۰، زیرسیستم تخصصی = ۱) است و مستقیماً با فرضیهٔ بخش ۱۰-۷ («نام‌های `IsAutomatic` وارونه‌اند و `AddVoucherCommandHandler` در مسیر ثبت دستی به‌اشتباه مقدار ۱ می‌نویسد») همخوانی دارد.

⚠️ **صادقانه، این شاهد هم‌بستگی است نه اثبات مستقیم کد.** من مستقیماً کد `AddVoucherCommandHandler` یا کاربری که این ۵۷ سند را در محیط توسعه ثبت کرده را ندیدم (خارج از scope این کار، که فقط دیتابیس بود). ولی الگوی داده دقیقاً همان چیزی است که فرضیهٔ باگ پیش‌بینی می‌کرد، و هیچ تفسیر جایگزین محکمی (مثلاً توزیع متعادل و صرفاً هم‌بسته با ماژول بدون وارونگی) دیده نشد. **نتیجه‌گیری: فرضیهٔ باگ با دادهٔ زنده تأیید شد، نه رد.**

## ۲۳-۴. 🆕 یافتهٔ جدید و پیش‌بینی‌نشده — تناقض دادهٔ زنده با قاعدهٔ Validator سطح گروه

بخش ۱۰-۴ (بر پایهٔ خواندن Validator مرجع) قاعده‌ای استخراج کرده بود: «سطح **گروه** فقط `TYPEACTIVITY` در بازهٔ ۱ تا ۳ را می‌پذیرد» (`AddGroupCodeValidator.cs:26-29` → `.Must(value => (int)value >= 1 && (int)value <= 3)`).

ولی دادهٔ زندهٔ ما نشان می‌دهد **سه حساب سطح گروه واقعی** (`TYPECODE=1`) مقادیر **۴، ۵، ۶** دارند (هرکدام دقیقاً یک ردیف — رجوع به جدول خام در ۲۳-۱، ردیف‌های `TYPECODE=1`):
```
TYPECODE | TYPEACTIVITY | COUNT(*)
1 | 4 | 1
1 | 5 | 1
1 | 6 | 1
```

**این را حدس نمی‌زنم که چرا.** چند تفسیر ممکن، هیچ‌کدام تأیید نشد:
- این ردیف‌ها ممکن است از **قبل از استقرار** برنامهٔ وب `Tamin.Core` در دیتابیس ثبت شده باشند (یعنی از یک سیستم قدیمی‌تر که این Validator را نداشته).
- ممکن است این قاعدهٔ Validator به‌مرور **اضافه شده باشد** و این سه ردیف از دوره‌ای قبل از آن باقی مانده‌اند.
- ممکن است این‌ها دادهٔ تستی/خرابِ محیط توسعه باشند (بسیاری از ردیف‌های همین محیط نام‌های آشکارا تستی دارند مثل `test1`, `تست`).

**پیامد برای پروژهٔ ما:** اگر بخواهیم این Validator (بازهٔ ۱..۳ برای گروه) را در `Accounting.Application` بازسازی کنیم، باید توجه داشت که **دادهٔ Legacy موجود ممکن است از قبل این قاعده را نقض کند** — پس اعمال این Validator روی داده‌های موجود (نه فقط ورودی جدید) می‌تواند رکوردهای معتبر تاریخی را نامعتبر نشان دهد. این یک تصمیم باز جدید است، نه یک اشتباه در کار فعلی.

## ۲۳-۵. محدودیت‌های این کار

- تمام دادهٔ بررسی‌شده از یک **دیتابیس محیط توسعه** (`setadidevdb.tamin.org`) آمد که حجم کم و ردیف‌های آشکارا تستی (`test1`, `تست`, `hdsgd`, ...) فراوان دارد. نتیجه‌گیری‌های بالا بر پایهٔ **جهت‌داری الگو** است، نه حجم آماری بزرگ. برای `TYPEACTIVITY` این مشکلی ایجاد نکرد چون شواهد سطح گروه ۱۰۰٪ بدون استثنا بودند؛ برای `VAHEDTYPE` دقیقاً همین محدودیت باعث شد نتیجه‌گیری «تا حدی» باقی بماند.
- فقط سه ابهام مشخص‌شده در این Task بررسی شد؛ بقیهٔ enumهای فهرست‌شده در بخش ۱۰-۲ (`DOCLIFE`, `PERSONTYPE`, `ISACTIVE`, `OWNER`, ...) **در این کار لمس نشدند** و همچنان بر پایهٔ تحلیل کد مرجع (نه دادهٔ زنده) هستند.
- کوئری‌ها روی یک اسکیمای تک‌مستأجر اجرا شدند (بدون فیلتر `VAHEDCODE`)؛ چون این کار صرفاً برای رفع ابهام نوع/enum بود، نه بررسی ایزولاسیون داده.
