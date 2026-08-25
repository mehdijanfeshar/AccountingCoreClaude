# مرجع Entityهای پروژهٔ خارجی Tamin.Core — مستقل vs تعبیه‌شده

> **بنر — منشأ و محدودیت این سند**
> این سند در تاریخ **۲۰۲۶-۰۸-۲۰** با خواندن **Read-Only** پوشهٔ `D:\CentralAccount\Tamin.Core\Entities\` (و **فقط** همین پوشه — نه `ApplicationUseCases`، نه `Infrastructure.Persistance.EF`، نه `Presentaion.Web.API`، نه `IDP`، نه کلاس‌های پایهٔ `Domain.Common`) استخراج شده است.
> **هیچ sync خودکاری با آن پروژه وجود ندارد.** اگر `Tamin.Core` بعداً تغییر کند، این سند باید **دستی** بازبینی شود.
> این سند صرفاً **راهنمای تصمیم معماری** است، نه منبع قانون کسب‌وکار. طبق قانون تیم: `accounting-domain` مالک Business Meaning پروژهٔ ماست؛ ساختار پروژهٔ مرجع فقط سیگنال طراحی می‌دهد.
> پروژهٔ ما و `Tamin.Core` روی همان schema اوراکل `CENTRALACCOUNT` کار می‌کنند؛ Entityهای ما در `backend/src/Accounting.Domain/Entity/` (namespace `Accounting.Domain.Entity`) هستند.

---

## ۰. آمار نهایی (شمارش دقیق، نه تخمین)

با `grep`/`find` مستقیم روی ۱۳۰ فایل `.cs`:

| دسته | تعداد فایل | توضیح |
|---|---|---|
| Entity واقعی (کاندید CRUD) | **۶۳** | کلاس‌های `BaseEntity<Guid>` / `BaseEntityG<Guid>` / `BaseAuditableEntity<Guid>` یا معادل با جدول واقعی |
| View / Read-model (`ViewEntity/`) | **۲۰** | ⚠️ تصحیح نسبت به تخمین اولیهٔ «۲۱» در ورودی کار — شمارش دقیق `grep` روی خودِ پوشه ۲۰ فایل داد |
| Enum | **۴۵** | ۴۵ فایل، هرکدام دقیقاً یک `public enum`. همگی مقدار عددی/کاراکتری روی ستون همان جدول؛ **هرگز CRUD مستقل نمی‌گیرند**. ⚠️ یکی از این ۴۵ فایل (`SearchOperator.cs`) علاوه بر enum یک `class SearchParam` (DTO جستجوی پویا، بدون جدول) هم دارد — در این جدول فقط **یک‌بار** شمرده شده تا جمع درست بماند |
| Exception | **۱** | `RabetExceptions.cs` — کلاس Exception، نه Entity |
| Sample/Demo | **۱** | `SampleEntity.cs` — `BaseAuditableEntity<int>` (نه `Guid`!)، بدون جدول واقعی، صرفاً نمونهٔ آموزشی |
| **جمع** | **۱۳۰** | ✅ منطبق با شمارش فایل واقعی: `63 + 20 + 45 + 1 + 1 = 130` (تأییدشده با `find`/`grep` توسط `team-lead`) |

توزیع ۶۳ Entity واقعی:

| زیر‌دسته | تعداد | توضیح |
|---|---|---|
| **تعبیه‌شده — سیگنال قوی** (کلکسیون کپسوله‌شده + متد `AddX`/`DeleteX`) | **۱۳** | جدول کامل در بخش ۲ |
| **مبهم / ناقص** (سیگنال کپسولگی نیمه‌کاره یا متروک) | **۲** | `RabetClosing`, `ChargeLinkCost` — نیاز به بررسی بیشتر، **قطعی طبقه‌بندی نشدند** |
| **جزئیات بدون کپسولگی** (مفهوماً «ردیف زیرِ یک سند» ولی در Tamin.Core محافظت‌نشده — یک الگوی **تکراری و سیستماتیک**) | **۶** | `VouchersDetail`, `ElamDetail`, `PayReciveDetail`, `TmpVoucherDetail`, `Attach`, `AttribsInVoucher` — رجوع به بخش ۵ |
| **مستقل** | **۴۲** | جدول کامل در بخش ۱ |

نگاشت `TB_XXX`: هر ۶۳ Entity واقعی **دقیقاً یک** معادل `TB_XXX` در پروژهٔ ما دارد (صفر بدون‌معادل از آن‌طرف). در جهت عکس، **۲ جدول** از ۶۵ `TB_XXX` ما در `Tamin.Core` معادلی ندارند: `TB_AUDITLOG`, `TB_WORKSHOP_LINK_TAFSILI` (بخش ۷).

---

## ۱. اولویت تحلیل — طبق دستور صاحب پروژه

**تحلیل عمیق** (خوانده‌شده کامل، خط‌به‌خط) روی Entityهای هم‌پوشان با کار فعلی ما:
`AccountCode`, `VouchersHead`, `VouchersDetail`, `VouchersDetailLinkTafsili`, `Tafsili`, `TbTafsilGroup`, `TbTafsilLinkTafsilGroup`, `TbLevelTafsil` (فایل `TBLEVELTAFSIL.cs`), `TbAccountLinkTafsilGroup`, `TbAccountLinkLevel`.

**تحلیل سطحی** (نام + کلاس/enum + کلاس پایه + navigation، بدون بررسی خط‌به‌خط منطق) روی بقیهٔ ۱۲۰ فایل. در جدول‌های زیر Entityهای عمیق با 🔎 علامت خورده‌اند.

---

## ۲. جدول Entityهای تعبیه‌شده/Child — سیگنال قوی (۱۳ مورد)

سیگنال: `private List<X> _x = new();` + `public ICollection<X> X { get; }` (فقط getter) + متد `AddX(...)`/`DeleteX(...)` روی والد.

⛔ **هشدار صریح: برای هیچ‌کدام از این ۱۳ مورد CRUD مستقل نساز.** این‌ها فقط از طریق aggregate root خودشان باید ساخته/حذف شوند.

| Entity | معادل `TB_XXX` | والد (Aggregate Root) | متد سازنده روی والد | یادداشت |
|---|---|---|---|---|
| `AccountLinkTafsili` 🔎(نام‌گذاری) | `TB_ACCOUNT_LINK_TAFSILI` | `Account` (حساب **بانکی**، نه AccountCode!) | `AddAccountLinkTafsil` | تلهٔ نام‌گذاری تأییدشدهٔ مستقل — رجوع بخش ۶ |
| `TbAccountLinkLevel` 🔎 | `TB_ACCOUNT_LINK_LEVEL` | `AccountCode` | `AddAccountlinkLevel` (۲ overload) / `DeleteAccountLinkAsync` | — |
| `Check` | `TB_CHECK` | `CheckBook` | `AddCheckPapers` | تولید انبوه چک از بازهٔ شماره |
| `ElamDetailLinkTafsili` | `TB_ELAMDETAIL_LINK_TAFSILI` | `ElamDetail` | `AddElamDetailLinkTafsil` / `DeleteElamDetailLinkTafsilByTafID` | — |
| `ExpenceLinkTafsili` | `TB_EXPENCE_LINK_TAFSILI` | `Expense` | `AddExpenseLinkTafsil` / ۳ متد Delete | یکی از Deleteها try/catch با پیام فارسی «امکان حذف وجود ندارد» دارد |
| `IdentityDetail` | `TB_IDENTITYDETAIL` | **`VouchersDetail`** (نه `IdentityHead`!) | `AddIdentityDetail` | ⚠️ FK به `IdentityHead`/`IdentitySubGrp` هم دارد ولی والدِ **سازنده** طبق کد `VouchersDetail` است — رجوع بخش ۵ |
| `IdentityFixItem` | `TB_IDENTITYFIXITEM` | `IdentityHead` | `AddIdentityFixItems` | — |
| `PayAndReciveLinkTafsili` | `TB_PAYRECIVDETAIL_LINK_TAFSILI` | `PayReciveDetail` | `AddPayAndReciveLinkTafsil` / ۳ متد Delete | — |
| `RevolvingLinkTafsili` | `TB_REVOLVINGFUND_LINK_TAFSILI` | `RevolvingFund` | `AddRevolvingLinkTafsil` / ۳ متد Delete | — |
| `TbTafsilLinkTafsilGroup` 🔎 | `TB_TAFSIL_LINK_TAFSILGROUP` | `Tafsili` | `AddTafsiliLinkTafsilGroup` (۴ overload!) / `DeleteTafsiliLinkTafsilGroup` | تعداد overload غیرعادی زیاد — کد کیفیت پایین (احتمال iteration های مختلف بدون پاک‌سازی) |
| `TafsiliUnitAccess` | `TB_TAFSILI_UNITACCESS` | `Tafsili` | `AddTafsiliUnitAccess` | — |
| `VouchersDetailLinkTafsili` 🔎 | `TB_VOUCHERDETAIL_LINK_TAFSILI` | `VouchersDetail` | `AddVouchersDetailLinkTafsil` + ۳ متد Delete | رجوع بند (و) بخش ۶ برای FK EF-only |
| `ChargeAndCostDetail` (سیگنال ضعیف‌تر) | `TB_CHARGEANDCOST_DETAIL` | `ChargeAndCostHead` | **ندارد** — فقط `{ get; private set; }` روی کلکسیون، بدون متد `AddX` آشکار در همین فایل؛ کد مرتبط با `ChargeLinkCost` هم کامنت شده | سیگنال کپسولگی ضعیف‌تر از ۱۲ مورد بالا — private setter هست ولی راه ساخت از داخل کلاس دیده نشد (شاید در `ApplicationUseCases`، خارج از scope) |

---

## ۳. جدول Entityهای مستقل (کاندید CRUD مستقل) — ۴۲ مورد

سیگنال استقلال: کلکسیون‌های navigation روی این Entityها همه `{ get; set; }` **بدون** backing field خصوصی و **بدون** متد `AddX` هستند (یا اصلاً هیچ کلکسیونی روی هیچ والدی به آن‌ها اشاره ندارد) → یعنی از بیرونِ کلاس هم قابل درج/حذف مستقیم است.

| Entity | معادل `TB_XXX` | دلیل استقلال |
|---|---|---|
| `AccountCodeInterface` | `TB_ACCOUNTCODE_INTERFACE` | فقط FK به `AccountCode`، هیچ کلکسیون کپسوله‌شده‌ای اشاره‌گر به آن نیست |
| `AccountCode` 🔎 | `TB_ACCOUNTCODE` | خودش Aggregate Root است (والد `TbAccountLinkLevel` و مبهماً `RabetClosing`) |
| `AccountException` | `TB_ACCOUNTEXCEPTION` | FK به `AccountCode`+`VahedType`، هیچ‌کدام کپسوله‌اش نمی‌کنند |
| `Account` (بانکی) | `TB_ACCOUNT` | Aggregate Root (والد `AccountLinkTafsili`) |
| `AccountType` | `TB_ACCOUNT_TYPE` | Lookup ساده (`BaseEntity<Guid>`) |
| `AttribForAccountCode` | `TB_ATTRIBFORACCOUNTCODE` | FK به `AccountCode` بدون کپسولگی |
| `BankCartDetail` | `TB_BANKCARTDETAIL` | مستقل، به `Check`/`Receipt` اشاره دارد بدون کپسولگی از هیچ‌کدام |
| `Bank` | `TB_BANK_LIST` | Lookup |
| `Branch` | `TB_BANKBRANCH_LIST` | مفهوماً زیرمجموعهٔ `Bank` ولی `Bank.Branches` کپسوله نیست |
| `BillLog` | `TB_BILL_LOG` | هیچ FK ای اصلاً ندارد؛ کاملاً مستقل |
| `ChargeAndCostHead` | `TB_CHARGEANDCOST_HEAD` | Root (والد نیمه‌کپسولهٔ `ChargeAndCostDetail`) |
| `ChechIncurrent` | `TB_CHEQUES_INCORRENT` | FK به `Check` بدون کپسولگی |
| `CheckBook` | `TB_CHECKBOOK` | Aggregate Root (والد `Check`) |
| `ChequeType` | `TB_CHECK_TYPE` | Lookup تنظیمات چاپ چک (~۴۰ فیلد پیکربندی چاپگر) |
| `City` | `TB_CITY` | Lookup |
| `ElamHead` | `TB_ELAMHEAD` | Root/Head؛ کلکسیون `ElamDetails` کپسوله نیست (بخش ۵) |
| `Expense` | `TB_EXPENCE` | Aggregate Root (والد `ExpenceLinkTafsili`) |
| `ExpenseGroup` | `TB_EXPENCEGROUP` | Lookup |
| `IdentityGroups` | `TB_IDENTITYGROUP` | Root lookup؛ `IdentityHeads`/`identitySubGrps` کپسوله نیستند |
| `IdentityHead` | `TB_IDENTITYHEAD` | Root (والد `IdentityFixItem`، ولی `IdentityDetails` کپسوله نیست) |
| `IdentitySubGrp` | `TB_IDENTITYSUBGRP` | Lookup زیرمجموعهٔ `IdentityGroups`، بدون کپسولگی |
| `PayReciveHead` | `TB_PAYRECIVHEAD` | Root/Head؛ `PayReciveDetails` کپسوله نیست (بخش ۵) |
| `PersonAction` | `TB_PERSON_ACTION` | مستقل، بدون هیچ FK ای که به آن اشاره کند |
| `PreDescrib` | `TB_PREDESCRIB` | Lookup اختیاری به `AccountCode` |
| `Province` | `TB_PROVINCE` | Lookup (والد `City`، بدون کپسولگی) |
| `Rabet` | `TB_RABET` | لینک `AccountCode`↔`RabetType`، بدون کپسولگی از هیچ‌کدام |
| `RabetType` | `TB_RABET_TYPE` | Lookup |
| `Receipt` | `TB_RECEIP` | مستقل، از `PayReciveDetail`/`BankCartDetail`/`VouchersDetail` ارجاع می‌شود بدون کپسولگی |
| `RevolvingFund` | `TB_REVOLVING_FUND` | Aggregate Root (والد `RevolvingLinkTafsili`) |
| `TbLevelTafsil` 🔎 | `TB_LEVEL_TAFSIL` | Lookup سطح تفصیلی؛ سه Entity دیگر به آن FK می‌زنند ولی خودش هیچ‌کدام را کپسوله نمی‌کند |
| `Tafsili` 🔎 | `TB_TAFSILI` | Aggregate Root (والد `TbTafsilLinkTafsilGroup` + `TafsiliUnitAccess`) |
| `TbAccountLinkTafsilGroup` 🔎 | `TB_ACCOUNT_LINK_TAFSILGROUP` | ⚠️ **یافتهٔ مهم:** با اینکه در پروژهٔ ما این جدول «منبع حقیقت تفصیلی مجاز» است، در `Tamin.Core` **هیچ** والدی (نه `AccountCode`، نه `TbTafsilGroup`، نه `TbLevelTafsil`) آن را کپسوله نمی‌کند — `AccountCode.TbAccountLinkTafsilGroups` صرفاً `{ get; set; }` است. رجوع بخش ۶ |
| `TbSysType` | `TB_SYSTYPE` | Lookup |
| `TbTafsilGroup` 🔎 | `TB_TAFSIL_GROUP` | Lookup؛ `TafsilLinks`/`TbAccountLinkTafsilGroups` کپسوله نیستند |
| `TbYear` | `TB_YEAR` | ⚠️ آنومالی: **بدون** هیچ کلاس پایه (نه `BaseEntity`، نه `BaseEntityG`)؛ کلید `WorkingYear` از نوع `int` با `[Key]` صریح، نه `Guid` — تنها Entity با این الگو در کل پروژه |
| `VahedInfo` | `TB_VAHED_INFO` | Root سلسله‌مراتب واحد سازمانی؛ `Childs` self-reference کپسوله نیست |
| `VahedType` | `TB_VAHED_TYPE` | Lookup |
| `TmpVoucherHead` | `TB_TMP_VOUCHERHEAD` | Root/Head؛ `TmpVoucherDetails` کپسوله نیست (بخش ۵) |
| `VouchersHead` 🔎 | `TB_VOUCHERSHEAD` | Root/Head؛ `DocsDetails` کپسوله نیست (بخش ۵ — یافتهٔ اصلی د) |
| `WhiteAndBlackList` | `TB_WHITEANDBLACKLIST` | مستقل |
| `WhiteList` | `TB_WHITELIST` | مستقل |
| `WorkShop` | `TB_WORKSHOP` | Root؛ `ElamHeads` کپسوله نیست |

---

## ۴. موارد مبهم / ناقص — **قطعی طبقه‌بندی نشدند** (۲ مورد)

### `RabetClosing` (`TB_RABET_CLOSING`)

در `AccountCode.cs`:
```csharp
public AccountCode()
{
    AccountLinks = _linkLevels;
    RabetClosings = _rabetClosing;   // پرشده از backing field خصوصی...
}
private List<RabetClosing> _rabetClosing = new();
...
public virtual ICollection<RabetClosing> RabetClosings { get; set; }   // ...ولی پراپرتی public setter دارد!
```
برخلاف `TbAccountLinkLevel` (که `{ get; }` فقط-خواندنی + `AddAccountlinkLevel`/`DeleteAccountLinkAsync` دارد)، برای `RabetClosing` **هیچ متد `AddX`/`DeleteX`ای در همین فایل وجود ندارد** و پراپرتی قابل جایگزینی از بیرون است. این یا (الف) کپسولگی نیمه‌کاره/فراموش‌شده است، یا (ب) عمداً باز گذاشته شده چون `RabetClosing` منطقاً می‌تواند مستقل هم مدیریت شود (خودش FK به دو `AccountCode` مجزا دارد: `AccountCodeId` و `AccountCodeRabetId`، یعنی رابطهٔ many-to-many-like بین دو حساب). **تصمیم طبقه‌بندی به `team-lead`/کاربر واگذار می‌شود.**

### `ChargeLinkCost` (`TB_CHARGE_LINK_COST`)

در `ChargeAndCostDetail.cs` کد زیر **کامنت شده**:
```csharp
//public virtual ICollection<ChargeLinkCost> ChargeLinkCost { get; set; }
//public virtual ICollection<ChargeLinkCost> ChargeLinkCost1 { get; set; }
//private List<ChargeLinkCost> _lists = new();
//private List<ChargeLinkCost> _lists1 = new();
//public ChargeAndCostDetail() { ChargeLinkCost = _lists; ChargeLinkCost1 = _lists1; }
```
یعنی نویسندهٔ پروژهٔ مرجع **قصد کپسولگی داشته و آن را متروک کرده**. خودِ `ChargeLinkCost` هیچ `[ForeignKey]` روی `ChargeId`/`CostId` هم ندارد (برخلاف همهٔ Link entityهای دیگر). **وضعیت فعلی این Entity در Tamin.Core عملاً یتیم است.** توصیه: تا تصمیم صریح کاربر، نه مستقل و نه child در نظر گرفته نشود؛ اگر لازم شد از صفر بر اساس قوانین خودمان طراحی شود.

---

## ۵. یافتهٔ سیستماتیک مهم: الگوی «Head هرگز Detail را کپسوله نمی‌کند»

این نکته فراتر از یافتهٔ اولیهٔ team-lead دربارهٔ `VouchersHead.DocsDetails` است — با بررسی کامل مشخص شد **یک الگوی تکرارشونده در کل `Tamin.Core`** است، نه یک استثنا:

| Head (Root) | Detail (فرزندِ مفهومی) | کپسوله شده؟ |
|---|---|---|
| `VouchersHead` | `VouchersDetail` | ❌ `{ get; set; }` ساده |
| `ElamHead` | `ElamDetail` | ❌ `{ get; set; }` ساده |
| `PayReciveHead` | `PayReciveDetail` | ❌ `{ get; set; }` ساده |
| `TmpVoucherHead` | `TmpVoucherDetail` | ❌ `{ get; set; }` ساده |
| `IdentityHead` | `IdentityDetail` | ❌ `{ get; set; }` ساده (و جالب اینکه `IdentityDetail` واقعاً از طریق `VouchersDetail.AddIdentityDetail` ساخته می‌شود، نه از `IdentityHead`!) |
| `ChargeAndCostHead` | `ChargeAndCostDetail` | ⚠️ نیمه (`private set`، بدون `AddX` قابل مشاهده) |

در مقابل، سطح **«Detail → Link-Tafsili»** (`VouchersDetail→VouchersDetailLinkTafsili`, `ElamDetail→ElamDetailLinkTafsili`, `PayReciveDetail→PayAndReciveLinkTafsili`, `Expense→ExpenceLinkTafsili`, `RevolvingFund→RevolvingLinkTafsili`, `Tafsili→TbTafsilLinkTafsilGroup`) **همیشه** کپسوله است.

**نتیجه‌گیری برای تصمیم معماری ما (این تصمیم را خودم نمی‌گیرم، فقط سیگنال می‌دهم):** اگر قرار است از مرزهای Aggregate این پروژه الگو بگیریم، مرز طبیعی این نیست که «`VouchersHead`+`VouchersDetail` یک Aggregate باشند»؛ بلکه دو Aggregate جدا به‌نظر می‌رسد:
1. `VouchersHead` به‌تنهایی (یا حتی به‌عنوان یک Entity کاملاً مستقل با CRUD خودش)،
2. `VouchersDetail` + `VouchersDetailLinkTafsili` + `IdentityDetail` به‌عنوان یک Aggregate کوچک‌تر.

این دقیقاً همان تنشی است که در ورودی کار («بند د») صاحب پروژه خواسته بود **خودسرانه حل نشود** — به‌ویژه چون در دیتابیس ما cascade soft-delete `TB_VOUCHERSHEAD → TB_VOUCHERSDETAIL → TB_VOUCHERDETAIL_LINK_TAFSILI` (کشف فاز ۹) وجود دارد که به نفع «یک Aggregate واحد» است — مستقیماً در تضاد با مدل‌سازی `Tamin.Core`.

**✅ حل شد (۲۰۲۶-۰۸-۲۰، تصمیم صریح صاحب پروژه، نه استنتاج ما):** `VouchersDetail` **مستقل** است — یعنی CRUD خودش را می‌گیرد (قابل‌فراخوانی مستقل، برای مثال افزودن یک ردیف دیتیل جدید به سندی که از قبل وجود دارد، یا ویرایش سند). **اما** موقع ثبت اولیهٔ یک سند، هدر و دیتیل(ها) باید **با هم، در یک عملیات** ذخیره شوند (composite create). یعنی مرز Aggregate دقیقاً همان چیزی نیست که `Tamin.Core` مدل کرده (که `VouchersDetail` را کاملاً مستقل و بی‌ربط به لحظهٔ ساخت سند می‌داند) و نه کاملاً همان چیزی که cascade فاز ۹ ما پیشنهاد می‌داد (که `VouchersDetail` را کاملاً تابع `VouchersHead` می‌دانست) — بلکه یک مدل ترکیبی: **دو مسیر نوشتن برای یک Entity**: (۱) از طریق `CreateVoucherHeadCommand` گسترش‌یافته که می‌تواند دیتیل‌های اولیه را هم‌زمان با هدر در یک تراکنش ثبت کند، و (۲) یک `CreateVoucherDetailCommand`/`UpdateVoucherDetailCommand`/`DeleteVoucherDetailCommand` کاملاً مستقل برای افزودن/ویرایش/حذف بعدی. **cascade soft-delete فاز ۹ (حذف سند → حذف دیتیل‌ها → حذف لینک‌های تفصیلی) دست‌نخورده و همچنان معتبر می‌ماند** — این تصمیم فقط دربارهٔ Create/Update مستقل است، نه دربارهٔ Delete.

**همچنین تأیید شد:** جدول‌هایی که نامشان با `_LINK_TAFSIL` یا `_LINK_LEVEL` تمام می‌شود (هر ۱۰ مورد: `TB_ACCOUNT_LINK_LEVEL`, `TB_ACCOUNT_LINK_TAFSILGROUP`, `TB_ACCOUNT_LINK_TAFSILI`, `TB_ELAMDETAIL_LINK_TAFSILI`, `TB_EXPENCE_LINK_TAFSILI`, `TB_PAYRECIVDETAIL_LINK_TAFSILI`, `TB_REVOLVINGFUND_LINK_TAFSILI`, `TB_TAFSIL_LINK_TAFSILGROUP`, `TB_VOUCHERDETAIL_LINK_TAFSILI`, `TB_WORKSHOP_LINK_TAFSILI`) **همیشه تعبیه‌شده می‌مانند** — این با یافتهٔ Tamin.Core («سطح Detail→Link-Tafsili همیشه کپسوله است»، بالاتر در همین بخش) هم‌راستاست و صراحتاً به‌عنوان قاعدهٔ کلی تیم تثبیت شد، نه فقط برای `VouchersDetailLinkTafsili`.

دو مورد باقی‌مانده در همین گروه که سیگنال کپسولگی ندارند: `Attach` (پیوست فایل به سند/تفصیلی/پرداخت، بدون هیچ کپسولگی) و `AttribsInVoucher` (مقدار ویژگی هر ردیف سند؛ نکتهٔ جانبی: nav property‌اش روی `VouchersDetail` **مفرد** است — `AttribsInVoucher Attribs`، نه کلکسیون — که با ماهیت «چند attribute در هر ردیف» ناسازگار به‌نظر می‌رسد؛ احتمالاً نقص مدل‌سازی در خودِ Tamin.Core).

---

## ۶. جدول Enumها (۴۵ مورد) — **enumها هرگز CRUD مستقل نمی‌گیرند**

طبق قانون تیم ما، مقدار enum در پروژهٔ مرجع دقیقاً همان چیزی است که در پروژهٔ ما به‌صورت ستون عددی/کاراکتری (`NUMBER(1)`/`VARCHAR2(1)`) روی خودِ جدول ذخیره شده — نه جدول جداگانه، نه CRUD جداگانه.

| Enum | پوشه/namespace | فیلد/ستون مصرف‌کننده (در scope مجاز) | معادل `TB_XXX.COLUMN` |
|---|---|---|---|
| `InterfaceType` | `AccountCodeIntefaces` | `AccountCodeInterface.Type` | `TB_ACCOUNTCODE_INTERFACE.TYPE` |
| `TypeAccCode` | `AccountCodes` | `AccountCode.TypeAccCode` | `TB_ACCOUNTCODE.TYPEACCCODE` |
| `TypeAction` | `AccountCodes` | `AccountCode.TypeAction` | `TB_ACCOUNTCODE.TYPEACTION` |
| `TypeActivity` | `AccountCodes` | `AccountCode.TypeActivity` | `TB_ACCOUNTCODE.TYPEACTIVITY` |
| `TypeActivityGroup` | `AccountCodes` | یافت نشد در scope | — (احتمالاً در `ApplicationUseCases`) |
| `TypeCodes` | `AccountCodes` | `AccountCode.TypeCode` | `TB_ACCOUNTCODE.TYPECODE` (گروه/کل/معین) |
| `AttribSumEnum` | `Attribs` | `AttribForAccountCode.AttribSum` | `TB_ATTRIBFORACCOUNTCODE.ATTRIBSUM` |
| `ControlEnum` | `Attribs` | `AttribForAccountCode.ControlId` | `TB_ATTRIBFORACCOUNTCODE.CONTROLID` |
| `FlagEnum` | `Attribs` | `AttribForAccountCode.Flag` | `TB_ATTRIBFORACCOUNTCODE.FLAG` |
| `CheckReceiptType` | `BankCartDetails` | `BankCartDetail.CheckReceiptType` | `TB_BANKCARTDETAIL.CHECKRECEIPTTYPE` |
| `ChargeAndCostType` | `ChargeAndCost` | `ChargeAndCostHead.ChargeAndCostType` | `TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_TYPE` |
| `Status` (ChargeAndCost) | `ChargeAndCost` | `ChargeAndCostHead.Status` | `TB_CHARGEANDCOST_HEAD.STATUS` |
| `CheckStatus` (`Domain.Entities.Checks`) | `Checks` | یافت نشد استفاده در scope (مقادیر Pay/UnPay/Cancle) | ⚠️ نام تکراری با ردیف بعدی — رجوع یادداشت پایین جدول |
| `CheckType` (`Domain.Entities.Checks`) | `Checks` | `CheckBook.CheckBookType` | `TB_CHECKBOOK.CHECKBOOK_TYPE` |
| `CheckPrintStatus` | `Checks.Enum` | `Check.IsPrint` | `TB_CHECK.PRINT` |
| `CheckShowGridType` | `Checks.Enum` | یافت نشد در scope (فیلتر UI) | — |
| `CheckStatus` (`Domain.Entities.Checks.Enum`) | `Checks.Enum` | `Check.IsEbtal` (مقادیر canceled/notCanceled) | `TB_CHECK.EBTAL` |
| `DaramElamhType` | `Elam` | `ElamHead.ElamhDaramadType` | `TB_ELAMHEAD.ELAMHDRAMAD_TYPE` |
| `ElamCase` | `Elam` | `ElamHead.ElamhCase` | `TB_ELAMHEAD.ELAMH_CASE` |
| `ElamType` | `Elam` | یافت نشد در scope | — |
| `WebStat` | `Elam` | `ElamHead.WebStat` | `TB_ELAMHEAD.WEB_STAT` |
| `TrialBalanceGridShowType` | `Enum` | یافت نشد در scope (پارامتر گزارش) | — |
| `TypeInfos` | `Enum` | یافت نشد در scope | — |
| `TypeKoli` | `Enum` | `Tafsili.VahedType`, `TbTafsilLinkTafsilGroup.VahedType` | `TB_TAFSILI.VAHEDTYPE`, `TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE` |
| `TypeVahed` | `Enum` | یافت نشد در scope | — |
| `IdentitySubGroupKind` | `Identity` | `IdentitySubGrp.Fixed` | `TB_IDENTITYSUBGRP.FIXED` |
| `IdentitySubGroupType` | `Identity` | `IdentitySubGrp.SubgrpsType` | `TB_IDENTITYSUBGRP.SUBGRPS_TYPE` |
| `DebCredType` | `PayAndReciv` | یافت نشد در scope | — |
| `PayRecivType` | `PayAndReciv` | `PayReciveHead.PayRecivType` | `TB_PAYRECIVHEAD.PAYRECIVTYPE` |
| `OperatorRole` | `PersonActions` | `PersonAction.OperatorRole` | `TB_PERSON_ACTION.OPERATORROLE` |
| `Flag` (PreDescribs) | `PreDescribs` | `PreDescrib.FlagVoucher` | `TB_PREDESCRIB.FLAGVOUCHER` |
| `TypeAccountCode` | `Rabetss` | `RabetClosing.TypeAccountCode` | `TB_RABET_CLOSING.TYPEACCOUNTCODE` |
| `ReceiptType` | `Receipts` | `Receipt.ReceiptKind` | `TB_RECEIP.RECEIPT_KIND` |
| `Active` | `Tafsiliies` | `Tafsili.IsActive` | `TB_TAFSILI.ISACTIVE` |
| `Owners` | `Tafsiliies` | `Tafsili.Owner` | `TB_TAFSILI.OWNER` |
| `PersonTypes` | `Tafsiliies` | `Tafsili.PersonType`, `TbTafsilGroup.PersonType` | `TB_TAFSILI.PERSONTYPE`, `TB_TAFSIL_GROUP.PERSONTYPE` |
| `ConsolidateReportType` | `Vouchers` | یافت نشد در scope (پارامتر گزارش) | — |
| `DocLife` | `Vouchers` | `VouchersHead.DocLife` | `TB_VOUCHERSHEAD.DOCLIFE` |
| `IsAutomatic` | `Vouchers` | `VouchersHead.IsAutomatic` | `TB_VOUCHERSHEAD.ISAUTOMATIC` |
| `PersianMonths` | `Vouchers` | یافت نشد در scope | — |
| `SortType` | `Vouchers` | یافت نشد در scope | — |
| `TafsiliNo` | `Vouchers` | یافت نشد در scope (مفهوماً معادل taf1..taf7 در `TmpVoucherDetail` است ولی آن‌جا ستون‌های جدا هستند نه این enum) | — |
| `TmpImportType` | `Vouchers` | یافت نشد روی `TmpVoucherHead`/`TmpVoucherDetail` در scope | — |
| `StateEnum` | `WhiteAndBlackLists` | `WhiteAndBlackList.State` | `TB_WHITEANDBLACKLIST.STATE` |
| `SearchOperator` | ریشه (`Domain.Entities`) | DTO عمومی جستجو (`SearchParam.Operator`)، به هیچ جدولی وصل نیست | — |

⚠️ **تناقض نام‌گذاری کشف‌شده:** دو enum به نام `CheckStatus` در دو namespace متفاوت با معنای کاملاً متفاوت وجود دارد — `Domain.Entities.Checks.CheckStatus` (مقادیر: پرداخت‌شده/پرداخت‌نشده/باطل) و `Domain.Entities.Checks.Enum.CheckStatus` (مقادیر: ابطال‌شده/ابطال‌نشده). فیلد `Check.IsEbtal` صریحاً از namespace کامل (`Domain.Entities.Checks.Enum.CheckStatus`) استفاده می‌کند تا ابهام را در کامپایل حل کند — نشانهٔ اینکه خودِ تیم آن پروژه هم این تناقض نام‌گذاری را حس کرده.

**۱۲ enum بدون مصرف‌کنندهٔ یافت‌شده در scope مجاز** (`TypeActivityGroup`, `CheckShowGridType`, `ElamType`, `TrialBalanceGridShowType`, `TypeInfos`, `TypeVahed`, `DebCredType`, `ConsolidateReportType`, `PersianMonths`, `SortType`, `TafsiliNo`, `TmpImportType`) به احتمال زیاد در `ApplicationUseCases` (پارامتر گزارش/فیلتر UI) مصرف می‌شوند که **خارج از scope مجاز خواندن این وظیفه بود** — رجوع بخش ۹.

---

## ۷. جدول View / Read-model (۲۰ مورد)

طبق قانون کاری شمارهٔ ۲ در `CLAUDE.md` («سمت Read باید از View/Materialized View مجزا بخواند، نه مستقیماً از مدل نوشتن»)، این‌ها دقیقاً همان الگو را در پروژهٔ مرجع نشان می‌دهند: **همگی فقط Query، هرگز Command.**

۱۹ از ۲۰ مورد صراحتاً `[Keyless]` دارند (چون از یک `VW_*` اوراکل بدون Primary Key نگاشت می‌شوند). **یک استثنا:** `TrialBalanceReport4` بدون `[Keyless]` و با `virtual AccountCode`/`Tafsili` navigation — یا نقص کدنویسی در Tamin.Core (احتمال بیشتر — بدون Key واقعی، EF بدون `[Keyless]` روی این جدول کرش می‌کند مگر جای دیگری صریحاً پیکربندی شود که در scope ما دیده نشد) یا این View در آن پروژه واقعاً یک PK دارد. **نیاز به بررسی بیشتر خارج از scope.**

| Class | جدول/View منبع | حوزه |
|---|---|---|
| `VwAttribMisMatchReport` | `VW_ATTRIBMISMATCHREPORT` | گزارش عدم تطابق attribute |
| `VwAttribMoinReport` | `VW_ATTRIBMOINREPORT` | گزارش attribute در سطح معین |
| `VwAttribValueForMoinReport` | `VW_ATTRIBVALUEBYMOINREPORT` | مقدار attribute برای معین |
| `VwSearchOnAccountCode` | `VW_SEARCHONATTRIB` | جستجوی حساب بر اساس attribute |
| `VwReportForClosingVoucher` | `VW_REPORTFORCLOSINGVOUCHER` | سند اختتامیه (تا ۷ سطح تفصیلی) |
| `VWCONSOLIDATEREPORT` | `VW_CONSOLIDATE_REPORT` | گزارش تلفیقی |
| `VW_CONSOLIDATE_REPORT_TafsiliFilter` | `vw_consoli_report_taffilter` | گزارش تلفیقی با فیلتر تفصیلی |
| `VwReportForOpeningVoucher` | `VW_REPORTFOROPENINGVOUCHER` | سند افتتاحیه |
| `VwRabetClosingAccounts` | `VW_RABETCLOSING_ACCOUNTS` | حساب‌های رابط اختتامیه |
| `VwTafsiliList` | `VWTAFSILILIST` | فهرست تفصیلی |
| `TrialBalanceReport4` ⚠️ | `VWTRIALBALANCEREPORT4` | تراز آزمایشی — **بدون `[Keyless]`، آنومالی** |
| `VWTRIALREPORTWRAPPER` | `VWTRIALREPORTWRAPPER` | Wrapper تراز |
| `VWTRIALREPORTWRAPPERTAFSILI` | `VWTRIALREPORTWRAPPERTAFSILI` | Wrapper تراز با تفصیلی |
| `VwAccountJournalReport` | `VW_ACCOUNTJOURNALREPORT` | دفتر روزنامهٔ حساب |
| `VwBalanceSheetReport` | `VW_BALANCESHEETREPORT` | ترازنامه |
| `VwLedgerReport` | `VW_LEDGERREPORT` | دفتر کل |
| `VwTafsiliGroupReport` | `Vw_TafsiliGroupReport` | گزارش گروه تفصیلی |
| `VwTafsiliReport` | `VW_TAFSILIREPORT` | گزارش تفصیلی |
| `VwVoucherReview` | `VW_VOUCHERREVIEW` | مرور سند |
| `VwCartable` | `VWCARTABLE` | کارتابل سند (گردش کار) |

---

## ۸. نمودار متنی روابط والد→فرزند (۸ مسیر خواسته‌شده)

### Voucher
```
VouchersHead (Root/Head, TB_VOUCHERSHEAD)
 └─(❌ NOT encapsulated) VouchersDetail (TB_VOUCHERSDETAIL)
     ├─(✅ CHILD) VouchersDetailLinkTafsili (TB_VOUCHERDETAIL_LINK_TAFSILI)
     ├─(✅ CHILD — ولی از VouchersDetail، نه از IdentityHead) IdentityDetail (TB_IDENTITYDETAIL)
     └─(nav مفرد، بدون کپسولگی) AttribsInVoucher (TB_ATTRIBSINVOUCHER)
 ├─(❌ not encapsulated) Attach (TB_ATTACH)
 ├─(back-ref اختیاری) TmpVoucherHead (TB_TMP_VOUCHERHEAD)
 │    └─(❌ not encapsulated) TmpVoucherDetail (TB_TMP_VOUCHERSDETAIL)
 ├─(back-ref اختیاری) ElamHead (TB_ELAMHEAD)
 └─(back-ref اختیاری) PayReciveHead (TB_PAYRECIVHEAD)
```

### AccountCode
```
AccountCode (Root, TB_ACCOUNTCODE) — self-ref Parent/Child (گروه→کل→معین)
 ├─(✅ CHILD) TbAccountLinkLevel (TB_ACCOUNT_LINK_LEVEL)
 ├─(⚠️ مبهم) RabetClosing (TB_RABET_CLOSING)
 ├─(❌ not encapsulated) TbAccountLinkTafsilGroup (TB_ACCOUNT_LINK_TAFSILGROUP) ← منبع حقیقت تفصیلی مجاز در پروژهٔ ما
 ├─(❌ not encapsulated، ۱:۱) AttribForAccountCode (TB_ATTRIBFORACCOUNTCODE)
 ├─(❌ not encapsulated) PreDescrib[] (TB_PREDESCRIB)
 ├─(❌ not encapsulated) WhiteList[] (TB_WHITELIST)
 ├─(❌ not encapsulated، ۱:۱) AccountException (TB_ACCOUNTEXCEPTION)
 └─(❌ not encapsulated، ۱:۱) Account حساب بانکی (TB_ACCOUNT)
      └─(✅ CHILD) AccountLinkTafsili (TB_ACCOUNT_LINK_TAFSILI)
```

### Tafsili
```
Tafsili (Root, TB_TAFSILI)
 ├─(✅ CHILD) TbTafsilLinkTafsilGroup (TB_TAFSIL_LINK_TAFSILGROUP) → TbTafsilGroup (مستقل، TB_TAFSIL_GROUP)
 └─(✅ CHILD) TafsiliUnitAccess (TB_TAFSILI_UNITACCESS)
```

### Elam
```
ElamHead (Root/Head, TB_ELAMHEAD)
 └─(❌ NOT encapsulated) ElamDetail (TB_ELAMDETAIL)
     └─(✅ CHILD) ElamDetailLinkTafsili (TB_ELAMDETAIL_LINK_TAFSILI)
```

### PayAndReciv
```
PayReciveHead (Root/Head, TB_PAYRECIVHEAD)
 └─(❌ NOT encapsulated) PayReciveDetail (TB_PAYRECIVDETAIL)
     └─(✅ CHILD) PayAndReciveLinkTafsili (TB_PAYRECIVDETAIL_LINK_TAFSILI)
```

### ChargeAndCost
```
ChargeAndCostHead (Root, TB_CHARGEANDCOST_HEAD)
 └─(⚠️ نیمه‌کپسوله) ChargeAndCostDetail (TB_CHARGEANDCOST_DETAIL)
     └─(⚠️ متروک/کامنت‌شده) ChargeLinkCost (TB_CHARGE_LINK_COST)
```

### Identity
```
IdentityGroups (Root lookup, TB_IDENTITYGROUP)
 ├─(❌ not encapsulated) IdentityHead (TB_IDENTITYHEAD)
 │    ├─(✅ CHILD) IdentityFixItem (TB_IDENTITYFIXITEM)
 │    └─(❌ not encapsulated از اینجا) IdentityDetail (TB_IDENTITYDETAIL) — ساخته می‌شود از VouchersDetail
 └─(❌ not encapsulated) IdentitySubGrp (TB_IDENTITYSUBGRP)
```

### Check
```
CheckBook (Root, TB_CHECKBOOK)
 └─(✅ CHILD) Check (TB_CHECK)
     ├─(❌ not encapsulated) ChechIncurrent (TB_CHEQUES_INCORRENT)
     └─(❌ not encapsulated) BankCartDetail (TB_BANKCARTDETAIL)
```

---

## ۹. واگرایی‌ها و تنش‌ها با مدل فعلی ما

1. **(از ورودی کار، بند د — تأیید و بسط داده شد)** `VouchersHead.DocsDetails` کپسوله نیست؛ و همان‌طور که بخش ۵ نشان داد، این **یک الگوی سیستماتیک** در کل `Tamin.Core` است (۶ مورد Head→Detail، هیچ‌کدام کاملاً کپسوله نیستند)، نه یک استثنای تک‌مورد. این با کشف فاز ۹ ما (cascade soft-delete `TB_VOUCHERSHEAD→TB_VOUCHERSDETAIL→TB_VOUCHERDETAIL_LINK_TAFSILI`) در تنش است. **تصمیم مرز Aggregate به `team-lead`/کاربر واگذار می‌شود.**

2. **(از ورودی کار، بند ه — تأیید و تعمیم داده شد)** `VouchersDetail.Debtor`/`.Creditor` در Tamin.Core از نوع `long` غیرnullable هستند، در حالی‌که `TB_VOUCHERSDETAIL.DEBTOR`/`CREDITOR` در پروژهٔ ما `decimal?` است. با بررسی کامل مشخص شد **این یک الگوی سیستماتیک است، نه موردی**: `BankCartDetail`, `ChechIncurrent`, `PayReciveDetail`, `TmpVoucherDetail`, `ElamDetail` هم دقیقاً همین الگو (`long`/`long?` غیرnullable‌تر از ما) را دارند؛ فقط `ChargeAndCostDetail.Debtor`/`.Creditor` به‌صورت `long?` (nullable) است. یعنی در سراسر Tamin.Core مبلغ **هرگز `decimal` نیست** — این نکته برای Command/Query جدید ما روی این جدول‌ها مهم است (احتمال از دست رفتن اعشار در Legacy، یا اینکه واحد پول ریال بدون اعشار ذخیره می‌شده).

3. **(از ورودی کار، بند و — تأیید و تعمیم داده شد)** `VouchersDetailLinkTafsili` دارای `[ForeignKey("Tafsili")]`/`[ForeignKey("LevelTafsil")]` در سطح EF است در حالی‌که در سطح خودِ Oracle (طبق کشف ما) هیچ FK ای وجود ندارد. این هم **الگوی تکراری** است: `AccountLinkTafsili`, `ExpenceLinkTafsili`, `PayAndReciveLinkTafsili`, `RevolvingLinkTafsili`, `ElamDetailLinkTafsili` همگی دقیقاً همین الگو (`[ForeignKey]` روی `TAFSILI_ID`/`LEVEL_ID` بدون FK واقعی در DB) را دارند. مرتبط با تصمیم باز 🟡 «یکپارچگی ارجاعی تفصیلی» در `CLAUDE.md`.

4. **(از ورودی کار، بند ز — تأیید مستقل شد)** `TbAccountLinkTafsilGroup` دقیقاً سه ستون (`ACCOUNT_ID`, `TAFSILGROUP_ID`, `LEVEL_ID`) دارد — تأیید مستقل کشف قبلی ما. `AccountLinkTafsili` واقعاً به `Account` (بانکی) وصل است، نه کدینگ — تلهٔ نام‌گذاری تأییدشده.

5. **⚠️ یافتهٔ جدید — `TbAccountLinkTafsilGroup` در Tamin.Core کپسوله نیست.** با اینکه در پروژهٔ ما این جدول «منبع حقیقت» تفصیلی مجاز شناخته شده، در Tamin.Core **هیچ** Aggregate Root ای آن را محافظت نمی‌کند. این ملایماً از فرضیهٔ ما پشتیبانی می‌کند که قانون «الزامی بودن تفصیلی» (تصمیم باز 🔴 در `CLAUDE.md`) احتمالاً در لایهٔ Application (خارج از scope خواندن این وظیفه) enforce می‌شده، نه در سطح Entity/DB — ولی این یک **حدس، نه اثبات**.

6. **⚠️ یافتهٔ جدید — تناقض نام `CheckStatus`.** دو enum هم‌نام با معنای متفاوت در دو namespace — رجوع بخش ۶.

7. **⚠️ یافتهٔ جدید — `IdentityDetail` والد واقعی‌اش `VouchersDetail` است، نه `IdentityHead`.** با اینکه `IdentityDetail` دارای FK به `IdentityHead` و `IdentitySubGrp` است، تنها متد سازندهٔ کپسوله‌شده (`AddIdentityDetail`) روی `VouchersDetail` تعریف شده. اگر ما بخواهیم از این الگو پیروی کنیم، `IdentityDetail` باید هم‌زمان با ردیف سند مدیریت شود، نه با سربرگ شناسنامه.

8. **⚠️ یافتهٔ جدید — سازندهٔ عجیب `PayReciveDetail`.** یک overload سازنده با ۱۴ پارامتر شامل خودِ navigation objectها (`PayReciveHead payReciveHead, AccountCode accountCode, Check check, Receipt receipt`) به‌عنوان آرگومان constructor وجود دارد — الگویی که در هیچ Entity دیگری در کل پروژه دیده نشد (بقیه فقط سازندهٔ خالی + متد `SetX` دارند). احتمال کد نیمه‌کاره/تجربی.

9. **⚠️ یافتهٔ جدید — `SampleEntity` و `TbYear` هر دو آنومالی الگو هستند.** `SampleEntity` تنها Entity با `BaseAuditableEntity<int>` (نه `Guid`) است — واضحاً کد نمونه/آموزشی، نه بخشی از دامنهٔ واقعی. `TbYear` تنها Entity **بدون** هیچ کلاس پایه است، با `[Key]` روی `int WorkingYear`. این با تصمیم قبلی ما دربارهٔ عدم تبدیل `TB_YEAR` به `Guid` (چون هیچ ستون `CHAR(36)` ندارد) کاملاً همخوان است — تأیید مستقل جالب.

10. **موارد مبهم (`RabetClosing`, `ChargeLinkCost`)** — رجوع بخش ۴؛ عمداً قطعی طبقه‌بندی نشدند.

---

## ۱۰. نیازمند بررسی بیشتر (خارج از scope مجاز خواندن)

این‌ها **حدس زده نشدند** — طبق محدودیت صریح کار، فقط `Entities/` خوانده شد:

- **۱۲ enum بدون مصرف‌کنندهٔ یافت‌شده** (`TypeActivityGroup`, `CheckShowGridType`, `ElamType`, `TrialBalanceGridShowType`, `TypeInfos`, `TypeVahed`, `DebCredType`, `ConsolidateReportType`, `PersianMonths`, `SortType`, `TafsiliNo`, `TmpImportType`) — احتمالاً پارامتر گزارش/فیلتر در `ApplicationUseCases`، نه ستون جدول.
- **صحت استنتاج کلاس‌های پایه** (`BaseEntity<Guid>`, `BaseEntityG<Guid>`, `BaseAuditableEntity<Guid>`) — خودِ این کلاس‌ها در `Domain.Common` هستند که در محدودهٔ مجاز نبود؛ استنتاج ما (بر اساس استفادهٔ `this.VahedCode`/`this.Year` در `VouchersDetail` بدون declare محلی) قوی ولی **غیرمستقیم** است.
- **دلیل عدم کپسولگی `ChargeAndCostDetail`** — آیا در `ApplicationUseCases` از طریق سرویس جداگانه ساخته می‌شود یا واقعاً کد ناقص است؟
- **مقصد واقعی `ChargeLinkCost`** — آیا در لایه‌های دیگر (خارج از scope) استفاده می‌شود یا کاملاً متروک است؟
- **آیا `TrialBalanceReport4` واقعاً یک PK دارد** (چون برخلاف ۱۹ View دیگر `[Keyless]` ندارد) — نیاز به بررسی `Infrastructure.Persistance.EF` (خارج از scope).
- **آیا `IsDeleted` واقعاً روی `BaseEntityG`/`BaseAuditableEntity` تعریف شده** — فقط استنباط غیرمستقیم از الگوی ستون‌های `[Column("ISDELETED")]` در چند View، هیچ فایل Entity این ستون را مستقیماً declare نکرده (چون از کلاس پایه می‌آید که خارج از scope است).
- **منطق واقعی enforce «الزامی بودن تفصیلی»** — همان‌طور که در بخش ۹.۵ گفته شد، عدم کپسولگی `TbAccountLinkTafsilGroup` صرفاً یک سیگنال ضعیف است؛ تأیید قطعی نیازمند بررسی `ApplicationUseCases` است.

---

## جمع‌بندی برای `backend-dotnet` (مصرف‌کنندهٔ Downstream)

قبل از ساخت هر CRUD جدید روی یکی از ۶۵ `TB_XXX` ما:
1. جدول بخش ۲ را چک کن — اگر معادلش آن‌جاست، CRUD مستقل **نساز**؛ عملیات را از طریق Aggregate Root پیاده کن.
2. اگر در جدول بخش ۴ است (`RabetClosing`, `ChargeLinkCost`) — قبل از تصمیم، از `team-lead` بپرس.
3. اگر در بخش ۵ است (Head/Detail سیستماتیک) — این سند **قصداً تصمیم نمی‌گیرد**؛ مرز Aggregate باید صریحاً از کاربر/`team-lead` گرفته شود.
4. برای بقیه (بخش ۳، ۴۲ مورد) CRUD مستقل معقول است.
