# مرحله ۴: طراحی دیتابیس Oracle

## اتصال EF Core به Oracle

```csharp
services.AddDbContext<AccountingDbContext>(options =>
    options.UseOracle(configuration.GetConnectionString("OracleDb")));
```

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=accounting;Password=***;Data Source=//host:1521/ORCLPDB1;"
  }
}
```

## جداول اصلی پیشنهادی

| جدول | نقش |
|---|---|
| `ACCOUNT_GROUP` | سطح گروه (ثابت) |
| `GENERAL_LEDGER_ACCOUNT` | سطح کل (ثابت) |
| `SUBSIDIARY_ACCOUNT` | سطح معین (ثابت) |
| `DETAIL_ACCOUNT_TYPE` | انواع تفصیلی شناور (مشتریان، پروژه‌ها، ...) |
| `SUBSIDIARY_DETAIL_TYPE_LINK` | جدول پیوند شناور بین معین و انواع تفصیلی مجاز |
| `DETAIL_ACCOUNT` | مقادیر واقعی تفصیلی |
| `VOUCHER` | سرِ سند |
| `VOUCHER_LINE` | ردیف‌های سند |
| `VOUCHER_LINE_DETAIL_VALUE` | تفصیلی‌های انتخاب‌شده در هر ردیف سند |

## نکات مخصوص Oracle

- برای شماره‌گذاری خودکار اسناد (`VOUCHER.NUMBER`) از `SEQUENCE` + `TRIGGER` یا `IDENTITY` (در Oracle 12c+) استفاده کنید.
- برای گزارش‌های سنگین سمت Read (تراز آزمایشی، دفتر کل)، از **Materialized View** با `REFRESH FAST ON COMMIT` یا Job زمان‌بندی‌شده استفاده کنید تا کوئری‌های Query-side در CQRS سریع بمانند.
- ایندکس‌گذاری روی `VOUCHER_LINE(SUBSIDIARY_ACCOUNT_ID, VOUCHER_ID)` و `VOUCHER_LINE_DETAIL_VALUE(DETAIL_ACCOUNT_ID)` برای گزارش‌گیری تفصیلی ضروری است.
- برای مانده‌گیری معین/تفصیلی از `CONNECT BY` یا `Recursive WITH` در صورت نیاز به گزارش سلسله‌مراتبی گروه→کل→معین استفاده کنید (این ساختار درختی طبیعتاً با Oracle Hierarchical Queries سازگار است).
- Migration را با `dotnet ef migrations add` بسازید ولی برای اسکریپت نهایی روی محیط Production از `dotnet ef migrations script` استفاده کنید تا DBA بتواند قبل از اجرا آن را بازبینی کند (رایج در محیط‌های سازمانی که Oracle دارند).

## گام‌های عملی

```bash
cd backend/src/Accounting.Infrastructure
dotnet ef migrations add InitialCreate -s ../Accounting.Api
dotnet ef database update -s ../Accounting.Api
```
