# راهنمای کامل راه‌اندازی پروژه حسابداری با تیم چند-ایجنتی در Claude Code

این راهنما مراحل ایجاد یک پروژه حسابداری با معماری زیر را توضیح می‌دهد:

- **Backend:** .NET (Clean Architecture + CQRS با MediatR)
- **Frontend:** React
- **Database:** Oracle
- **کدینگ حسابداری:** کدینگ شناور (گروه–کل–معین ثابت / تفصیلی چند سطحی شناور که در زمان صدور سند تعیین می‌شود)
- **مدیریت کار:** یک تیم از ساب‌ایجنت‌های Claude Code به‌همراه یک Team Lead که وظایف را تقسیم می‌کند
- **تداوم کار:** پروژه طوری ذخیره می‌شود که هر روز بتوانید از همان‌جا ادامه دهید

---

## فهرست مراحل

1. آماده‌سازی ریپازیتوری و ساختار پوشه‌ها
2. طراحی دامنه حسابداری (کدینگ شناور)
3. طراحی معماری Backend با CQRS
4. طراحی Database در Oracle
5. طراحی Frontend با React
6. ساخت تیم ایجنت‌ها در Claude Code (`.claude/agents`)
7. نوشتن `CLAUDE.md` برای حافظه پایدار پروژه
8. گردش‌کار روزانه (Daily Workflow)
9. تأیید و تست نهایی

---

## مرحله ۱: آماده‌سازی ریپازیتوری

```bash
mkdir accounting-system && cd accounting-system
git init
mkdir -p backend frontend docs .claude/agents
```

ساختار پیشنهادی نهایی پروژه:

```
accounting-system/
├── CLAUDE.md                     # حافظه/دستورالعمل کلی پروژه برای Claude Code
├── .claude/
│   └── agents/                   # تعریف ایجنت‌های تیم
│       ├── team-lead.md
│       ├── backend-dotnet.md
│       ├── frontend-react.md
│       ├── database-oracle.md
│       ├── accounting-domain.md
│       └── qa-tester.md
├── backend/
│   ├── src/
│   │   ├── Accounting.Domain/
│   │   ├── Accounting.Application/     # CQRS: Commands, Queries, Handlers
│   │   ├── Accounting.Infrastructure/  # EF Core + Oracle
│   │   └── Accounting.Api/
│   └── tests/
├── frontend/
│   └── src/
│       ├── features/
│       ├── shared/
│       └── app/
└── docs/
    └── chart-of-accounts.md      # مستندسازی کدینگ حسابداری
```

از همان روز اول این ریپازیتوری را روی گیت‌هاب/گیت‌لب push کنید تا هر روز (حتی از سشن‌های مختلف Claude Code) با `git pull` ادامه کار امکان‌پذیر باشد. جزئیات کامل هر مرحله در فایل‌های شماره‌دار بعدی این پوشه آمده است.

---

## نکته مهم درباره تداوم روزانه کار

برای اینکه هر روز بتوانید کار را از همان‌جا ادامه دهید:

1. **`CLAUDE.md`** را در ریشه پروژه نگه دارید — Claude Code این فایل را در ابتدای هر سشن به‌صورت خودکار می‌خواند و شامل: وضعیت فعلی پروژه، تصمیمات معماری، و «کارهای باقی‌مانده» است.
2. در پایان هر روز کاری، از Team Lead بخواهید بخش «وضعیت فعلی» در `CLAUDE.md` را به‌روزرسانی کند و تغییرات را commit کند.
3. یک فایل `docs/progress-log.md` نگه دارید که هر روز یک خط به آن اضافه می‌شود (تاریخ + خلاصه کار انجام‌شده).
4. همیشه با `git commit` در پایان هر بخش کاری، کار را ذخیره کنید — این تنها راه واقعی «ذخیره‌سازی» است، چون خود سشن Claude Code پایدار نیست ولی ریپازیتوری هست.
