# مرحله ۵: طراحی Frontend با React

## ساختار پیشنهادی (Vite + TypeScript)

```bash
cd frontend
npm create vite@latest . -- --template react-ts
npm install @tanstack/react-query axios react-router-dom zustand
```

```
frontend/
└── src/
    ├── app/                  # Routing, Providers, Layout اصلی
    ├── shared/
    │   ├── api/               # axios instance + typed API client
    │   ├── components/        # کامپوننت‌های مشترک (Table, Form, Select)
    │   └── hooks/
    └── features/
        ├── chart-of-accounts/  # مدیریت گروه/کل/معین/تفصیلی
        │   ├── components/     # مثلاً SubsidiaryDetailTypePicker (انتخاب شناور تفصیلی)
        │   ├── api/
        │   └── pages/
        └── vouchers/            # صدور سند
            ├── components/      # فرم سند که بر اساس معین انتخابی، فیلدهای تفصیلی را داینامیک می‌سازد
            ├── api/
            └── pages/
```

## نکتهٔ کلیدی UI برای کدینگ شناور

فرم صدور سند (`VoucherForm`) باید وقتی کاربر یک **معین** را انتخاب می‌کند، به‌صورت پویا فیلدهای تفصیلی مرتبط را (بر اساس `SubsidiaryDetailTypeLink` که از API خوانده می‌شود) نمایش دهد — دقیقاً همان چیزی که «شناور بودن» تفصیلی را در سطح UI پیاده می‌کند:

```tsx
function VoucherLineRow({ subsidiaryId }: { subsidiaryId: number }) {
  const { data: detailTypeLinks } = useDetailTypeLinks(subsidiaryId); // از GetSubsidiaryDetailOptionsQuery

  return (
    <>
      {detailTypeLinks?.map((link) => (
        <DetailAccountSelect
          key={link.detailAccountTypeId}
          typeId={link.detailAccountTypeId}
          required={link.isRequiredAtPosting}
        />
      ))}
    </>
  );
}
```

## ابزارهای پیشنهادی

- **React Query** برای مدیریت state سرور (کش، رفرش خودکار داده‌های حسابداری)
- **Zustand** یا **Context API** برای state سبک سمت کلاینت (مثلاً وضعیت فرم سند در حال تکمیل)
- **React Hook Form + Zod** برای فرم‌های سند با اعتبارسنجی سمت کلاینت (هماهنگ با FluentValidation سمت بک‌اند)
- **TanStack Table** برای نمایش دفتر معین/تفصیلی و تراز آزمایشی
