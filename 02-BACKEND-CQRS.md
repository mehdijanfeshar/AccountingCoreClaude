# مرحله ۳: معماری Backend با CQRS (.NET)

## لایه‌بندی پیشنهادی (Clean Architecture + CQRS)

```
backend/
└── src/
    ├── Accounting.Domain/            # Entities, Value Objects, Domain Services, Business Rules
    ├── Accounting.Application/       # CQRS: Commands / Queries / Handlers / DTOs / Validators
    │   ├── Accounts/
    │   │   ├── Commands/
    │   │   │   ├── CreateAccountGroupCommand.cs
    │   │   │   ├── CreateSubsidiaryAccountCommand.cs
    │   │   │   └── LinkDetailTypeToSubsidiaryCommand.cs
    │   │   └── Queries/
    │   │       ├── GetChartOfAccountsQuery.cs
    │   │       └── GetSubsidiaryDetailOptionsQuery.cs
    │   └── Vouchers/
    │       ├── Commands/
    │       │   ├── CreateVoucherCommand.cs
    │       │   └── PostVoucherCommand.cs
    │       └── Queries/
    │           ├── GetVoucherByIdQuery.cs
    │           └── GetTrialBalanceQuery.cs
    ├── Accounting.Infrastructure/    # EF Core + Oracle Provider, Repositories, Migrations
    └── Accounting.Api/               # Controllers/Minimal APIs -> فقط IMediator.Send() صدا می‌زند
```

## ابزارهای پیشنهادی

- **MediatR** برای پیاده‌سازی الگوی CQRS (Command/Query Handlers)
- **FluentValidation** برای اعتبارسنجی Commandها (مثلاً بررسی الزامی بودن تفصیلی‌ها)
- **Entity Framework Core + Oracle.EntityFrameworkCore (ODP.NET Core)** برای دسترسی به دیتابیس در سمت Write
- برای سمت **Read** (گزارش‌ها، تراز آزمایشی و ...): می‌توانید از Dapper با کوئری مستقیم SQL روی View/Materialized View های اوراکل استفاده کنید تا کارایی گزارش‌گیری بالا برود (جدایی کامل Read Model از Write Model، مطابق اصل CQRS)

## مثال ساختار یک Command/Handler

```csharp
public record CreateVoucherCommand(DateTime Date, List<VoucherLineDto> Lines) : IRequest<int>;

public class CreateVoucherCommandHandler : IRequestHandler<CreateVoucherCommand, int>
{
    private readonly IVoucherRepository _repo;
    private readonly IVoucherPostingValidator _validator; // قانون تفصیلی شناور اینجا چک می‌شود

    public CreateVoucherCommandHandler(IVoucherRepository repo, IVoucherPostingValidator validator)
    {
        _repo = repo;
        _validator = validator;
    }

    public async Task<int> Handle(CreateVoucherCommand request, CancellationToken ct)
    {
        var voucher = Voucher.Create(request.Date, request.Lines);
        _validator.EnsureRequiredDetailsProvided(voucher);   // اعتبارسنجی تفصیلی الزامی
        await _repo.AddAsync(voucher, ct);
        return voucher.Id;
    }
}
```

## چرا CQRS برای این پروژه مناسب است

1. **جداسازی نوشتن سند (Write) از گزارش‌گیری (Read):** سیستم حسابداری نیاز به گزارش‌های سنگین (تراز آزمایشی، دفتر کل، ترازنامه) دارد که بهتر است از View/Materialized View مجزا در Oracle خوانده شوند، بدون فشار به مدل Domain نوشتن.
2. **قابلیت افزودن قوانین شناور بدون تغییر ساختار اصلی:** چون منطق اعتبارسنجی تفصیلی در لایه Application/Domain مجزا از خواندن است، می‌توان قوانین جدید افزود بدون درگیر کردن کوئری‌های گزارش‌گیری.
3. **مقیاس‌پذیری تیمی:** هر ایجنت (backend، accounting-domain) می‌تواند مستقل روی Commandها یا Queryها کار کند بدون تداخل با یکدیگر.

## گام‌های عملی راه‌اندازی

```bash
cd backend
dotnet new sln -n Accounting
dotnet new classlib -n Accounting.Domain -o src/Accounting.Domain
dotnet new classlib -n Accounting.Application -o src/Accounting.Application
dotnet new classlib -n Accounting.Infrastructure -o src/Accounting.Infrastructure
dotnet new webapi -n Accounting.Api -o src/Accounting.Api
dotnet sln add src/**/*.csproj

cd src/Accounting.Application
dotnet add package MediatR
dotnet add package FluentValidation.DependencyInjectionExtensions

cd ../Accounting.Infrastructure
dotnet add package Oracle.EntityFrameworkCore
dotnet add package Dapper
```
