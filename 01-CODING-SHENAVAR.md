# مرحله ۲: طراحی دامنه کدینگ حسابداری شناور

## منطق کدینگ شناور

ساختار درخواستی شما دقیقاً همان الگوی متداول در نرم‌افزارهای حسابداری ایرانی است:

| سطح | نوع | توضیح |
|---|---|---|
| گروه (Group) | ثابت | بالاترین سطح، مثلاً ۱=دارایی‌ها، ۲=بدهی‌ها، ۳=حقوق صاحبان سرمایه، ۴=درآمد، ۵=هزینه |
| کل (General Ledger) | ثابت | زیرمجموعهٔ گروه، با طول کد ثابت |
| معین (Subsidiary) | ثابت | زیرمجموعهٔ کل، با طول کد ثابت، ماهیت بدهکار/بستانکار مشخص |
| تفصیلی (Detail) | **شناور** | یک یا چند «نوع تفصیلی» (مثلاً تفصیلی مشتریان، تفصیلی پروژه‌ها، تفصیلی مراکز هزینه) می‌توانند به هر معین متصل شوند؛ این اتصال از پیش در ساختار کد ثابت نیست، بلکه پویا تعریف می‌شود و مقدار واقعی تفصیلی در **زمان صدور سند** انتخاب/ثبت می‌شود |

نکتهٔ کلیدی «شناور بودن»: یک معین می‌تواند هم‌زمان به چند نوع تفصیلی وصل باشد (مثلاً معین «حساب‌های دریافتنی» به «تفصیلی مشتریان» و «تفصیلی پروژه‌ها»)، و این تعریف می‌تواند بعداً بدون تغییر ساختار کدینگ اصلی گروه/کل/معین گسترش یابد.

## مدل دامنه پیشنهادی (Domain Model)

```
AccountGroup            (گروه)
 └─ GeneralLedgerAccount (کل)
     └─ SubsidiaryAccount (معین)  -- Nature: Debit/Credit
         └─ SubsidiaryDetailTypeLink (پیوند شناور)  -- 0..N نوع تفصیلی مجاز/الزامی برای این معین
             └─ DetailAccountType   (نوع تفصیلی: مشتریان، پروژه‌ها، مراکز هزینه، ...)
                 └─ DetailAccount   (مقدار واقعی تفصیلی: "شرکت الف"، "پروژه X")

Voucher (سند حسابداری)
 └─ VoucherLine (ردیف سند)
     ├─ SubsidiaryAccountId  (معین انتخاب‌شده)
     ├─ DetailAccountSelections[]  (۰ تا N مقدار تفصیلی، بر اساس SubsidiaryDetailTypeLink همان معین)
     ├─ Debit / Credit
```

### موجودیت‌های کلیدی (به‌صورت خلاصه برای C# / EF Core)

```csharp
public class AccountGroup
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;   // طول ثابت، مثلاً 1 رقم
    public string Title { get; set; } = default!;
}

public class GeneralLedgerAccount
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;   // طول ثابت
    public string Title { get; set; } = default!;
    public int AccountGroupId { get; set; }
}

public class SubsidiaryAccount
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;   // طول ثابت
    public string Title { get; set; } = default!;
    public int GeneralLedgerAccountId { get; set; }
    public AccountNature Nature { get; set; }       // Debit / Credit
    public ICollection<SubsidiaryDetailTypeLink> DetailTypeLinks { get; set; } = new List<SubsidiaryDetailTypeLink>();
}

public class DetailAccountType     // "نوع" شناور، مثلاً مشتریان / پروژه‌ها / مراکز هزینه
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
}

public class SubsidiaryDetailTypeLink   // خودِ «شناوری»: کدام تفصیلی‌ها به کدام معین مجازند
{
    public int Id { get; set; }
    public int SubsidiaryAccountId { get; set; }
    public int DetailAccountTypeId { get; set; }
    public bool IsRequiredAtPosting { get; set; }   // آیا ثبت آن در سند اجباری است
    public int Order { get; set; }
}

public class DetailAccount        // مقدار واقعی، مثلاً "شرکت پارس" زیر نوع "مشتریان"
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int DetailAccountTypeId { get; set; }
}

public class Voucher
{
    public int Id { get; set; }
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }
    public VoucherStatus Status { get; set; }       // Draft / Posted / ...
    public ICollection<VoucherLine> Lines { get; set; } = new List<VoucherLine>();
}

public class VoucherLine
{
    public int Id { get; set; }
    public int VoucherId { get; set; }
    public int SubsidiaryAccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public ICollection<VoucherLineDetailValue> DetailValues { get; set; } = new List<VoucherLineDetailValue>();
}

public class VoucherLineDetailValue   // در زمان صدور سند پر می‌شود
{
    public int Id { get; set; }
    public int VoucherLineId { get; set; }
    public int DetailAccountTypeId { get; set; }
    public int DetailAccountId { get; set; }
}
```

### قانون کسب‌وکار مهم (Business Rule)

هنگام ثبت سند: برای هر `VoucherLine`، سیستم باید بر اساس `SubsidiaryDetailTypeLink` مربوط به آن معین، بررسی کند که همه تفصیلی‌های الزامی (`IsRequiredAtPosting = true`) پر شده باشند. این اعتبارسنجی باید در لایه Domain/Application (نه فقط UI) پیاده‌سازی شود — پیشنهاد می‌شود این منطق در قالب یک `Domain Service` به نام `VoucherPostingValidator` نوشته شود.

این قانون دقیقاً همان چیزی است که ایجنت `accounting-domain` مسئول پیاده‌سازی و نگهداری آن خواهد بود (نگاه کنید به فایل ایجنت‌ها).
