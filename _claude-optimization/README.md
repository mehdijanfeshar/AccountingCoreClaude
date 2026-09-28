# بهینه‌سازی مصرف توکن — AccountingCoreClaude

## چرا توکن‌ها زود تمام می‌شوند (به ترتیب اثر)

| # | مشکل | هزینهٔ تقریبی |
|---|---|---|
| ۱ | `team-lead` در «شروع هر Task» دستور دارد `open-decisions.md` (~۸۰k توکن) و `centralaccount-business-reference.md` (~۱۰۰k) را بخواند — با مدل **opus** | ۱۰۰k تا ۲۰۰k+ در هر Task |
| ۲ | `CLAUDE.md` حدود **۳۴k توکن** است (۷۴KB متن فارسی) و در سشن اصلی و هر ساب‌ایجنت بارگذاری می‌شود | ۳۴k × تعداد ایجنت‌ها |
| ۳ | هوک `SubagentStop` بعد از **هر** ایجنت کدنویس، `/code-review` چندایجنتی را اجباری می‌کند | چند ده هزار در هر مرحله |
| ۴ | `team-lead` ساب‌ایجنت است ولی ساب‌ایجنت در Claude Code **نمی‌تواند ساب‌ایجنت دیگری صدا بزند**؛ پس برنامه‌ریزی دو بار انجام می‌شود (یک بار در team-lead، یک بار در سشن اصلی) | دوبرابر کار هماهنگی |
| ۵ | پایان هر جلسه: به‌روزرسانی `ROADMAP.md` (~۷۴k) و `phase-log.md` (~۱۶۰k) که معمولاً کامل خوانده می‌شوند | ۵۰k تا ۱۰۰k+ |
| ۶ | هیچ `deny` برای `artifacts/` (DLL و XML تا ۱.۸MB)، `bin/`، `obj/` | در Grep/Glob |

## چه چیزهایی در این پوشه است

> ⚠️ پوشهٔ `dot-claude` همان `.claude` است (ابزار راه دور اجازهٔ نوشتن در `.claude` را نمی‌دهد). محتوایش را در `.claude` پروژه کپی کنید.

| فایل | کار |
|---|---|
| `CLAUDE.md` | نسخهٔ کوتاه: ~۳.۵k توکن به جای ~۳۴k (**۹۰٪ کمتر**). همهٔ قواعد الزامی حفظ شده؛ فهرست فازها فقط به `phase-log.md` ارجاع می‌دهد؛ ریسک‌ها فقط عنوان. قاعدهٔ «Grep به جای خواندن کامل docs» اضافه شد. |
| `.claude/skills/team-lead/SKILL.md` | team-lead به **اسکیل** تبدیل شد تا در سشن اصلی اجرا شود و واقعاً بتواند ایجنت‌ها را صدا بزند. خواندن کامل docs حذف شد؛ جدول Safety Gate به‌روز شد (فاز ۳۵ و ۳۸/۳۹). |
| `.claude/settings.local.json` | allowlist تمیز (مسیرهای قدیمی Downloads حذف شد)، `deny` برای artifacts/bin/obj/archive، و **حذف هوک اجباری `/code-review`**. |
| `.claude/agents/frontend-react.md` | **رفع تناقض:** قاعدهٔ `vahedCode` قدیمی بود («هرگز نفرست») در حالی که از فاز ۳۷-ب باید از هدر `X-Vahed-Code` فرستاده شود. |
| `.claude/agents/security-reviewer.md`, `performance-reviewer.md` | ابزار فقط‌خواندنی (بدون Write/Edit). |
| `.claude/agents/accounting-domain.md`, `qa-tester.md` | ارجاع Safety Gate به اسکیل جدید + Grep به جای خواندن کامل. |
| `CLAUDE.md.original` | نسخهٔ پشتیبان فایل فعلی. |

## نحوهٔ اعمال

1. commit فعلی بزنید تا راه برگشت باشد.
2. `CLAUDE.md` را روی ریشهٔ پروژه و محتوای `dot-claude/` را روی `.claude/` کپی کنید.
3. `.claude/agents/team-lead.md` را حذف کنید (یا به `agents_old/` ببرید) — حالا اسکیل است.
4. پوشهٔ `_claude-optimization` را پاک کنید.
5. Claude Code را دوباره باز کنید و `/context` بزنید؛ سهم Memory files باید از ~۳۴k به ~۴k رسیده باشد.

## عادت‌های کاری پیشنهادی

- برای Task چندلایه: `/team-lead <شرح کار>`. برای کار کوچک مستقیم: «با backend-dotnet ...».
- `/clear` بین Taskهای نامرتبط؛ `/compact` وسط Task طولانی.
- مدل سشن اصلی: Sonnet یا `opusplan`؛ Opus فقط برای تصمیم معماری.
- `/code-review` را دستی و فقط یک بار پیش از commit بزنید.
- پیشنهاد بعدی: بخش‌های تمام‌شدهٔ `ROADMAP.md` و فازهای قدیمی `phase-log.md` را به `docs/archive/` ببرید تا این دو فایل هم کوچک شوند.
