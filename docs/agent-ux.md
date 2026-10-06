# Agent-UX — ماژول الگوی عملیات

## هدف
کاربر غیرمالی به‌جای انتخاب کل/معین/تفصیلی، یک «عملیات» (مثل «دریافت وجه از مشتری») انتخاب می‌کند و چند اطلاعات ساده می‌دهد. سند را موتور قطعی از روی الگویی می‌سازد که حسابدار ستاد تعریف کرده است. فاز ۱ **هیچ AI ندارد**؛ Agent در فازهای بعد فقط همین Query/Commandها را صدا می‌زند.

## اصول
1. **LLM سند نمی‌زند.** درستی سند فقط به الگو (ستاد) و موتور قطعی (کد) وابسته است.
2. **پیش‌نمایش ارسالی فرانت قبول نمی‌شود.** Execute سند را دوباره از الگو می‌سازد.
3. **ثبت از مسیر همیشگی سند خودکار** (همان مخزن‌ها، `GetNextDocNumAsync`، `IVoucherTafsiliLevelGuard`، `DOCLIFE=موقت`، `ISAUTOMATIC=1`).
4. **واحد فقط از توکن/هدر** (`IVahedScoped` ← `VahedScopeBehavior`)، هرگز از بدنه.
5. **Idempotency:** `ClientRequestId` یکتا (`UK_OP_EXEC_CLIENT_REQ`)؛ کلیک دوباره همان سند قبلی را برمی‌گرداند.
6. **خطاها قابل پرسیدن‌اند:** هر خطای موتور `parameterKey` و `askPrompt` دارد.

## محل کد
| لایه | مسیر |
|---|---|
| Domain | `backend/src/Accounting.Domain/OperationTemplates/` |
| Application | `backend/src/Accounting.Application/OperationTemplates/` — `VoucherGenerationEngine`، `TemplateDefinitionValidator`، `Features` (Preview/Execute/List/Create)، `VoucherWriter` |
| Infrastructure | `backend/src/Accounting.Infrastructure/OperationTemplates/` — نگاشت (از قلاب `OnModelCreatingPartial`)، مخزن، `SubsidiaryAccountReader`، `DetailReader`، `AddOperationTemplates()` |
| API | `OperationsController` — `api/operations` |
| DDL | `backend/db/067_operation_templates.sql` + `068_operation_template_systype.sql` (نوع سند الگو) + `070_operation_template_keywords.sql` (کلمات کلیدی؛ `069` = DML نوع سند «انبار») — فقط صاحب پروژه اجرا می‌کند؛ بدون 068 همهٔ `/api/operations` خطا می‌دهد |

## جدول‌ها (DDL 067)
`TB_OP_TEMPLATE` · `TB_OP_TEMPLATE_PARAM` (FK گروه تفصیلی) · `TB_OP_TEMPLATE_LINE` (FK معین `TB_ACCOUNTCODE`) · `TB_OP_TEMPLATE_LINE_DETAIL` (FK تفصیلی ثابت، CHECK «پارامتر یا ثابت») · `TB_OP_EXECUTION` (FK سند، UK درخواست).

## API
| Method | مسیر | دسترسی |
|---|---|---|
| GET | `/api/operations/templates` | همهٔ نقش‌ها |
| POST | `/api/operations/templates` | فقط «مدیر ستاد» (`SETAD ADMIN`) |
| POST | `/api/operations/{id}/preview` | همهٔ نقش‌ها، در واحد جاری |
| POST | `/api/operations/{id}/execute` | نقش‌های ثبت عملیات، در واحد جاری |

| POST | `/api/operations/compose/preview` | همهٔ نقش‌ها — سند کامل (ویرایش‌شده یا آزاد) |
| POST | `/api/operations/compose/execute` | نقش‌های ثبت عملیات — سند کامل، اتمیک |

**سند کامل (Compose):** همهٔ فیلدهای فرم صدور سند — تاریخ، شرح، پیوست؛ هر ردیف: معین، تفصیلی هر سطح (`levelId`→`tafsiliId`)، بدهکار/بستانکار، شرح، چک موجود یا دسته‌چک صوری (همان `VoucherChequeService`). سرور دوباره اعتبارسنجی می‌کند (حداقل دو ردیف، یکی از بدهکار/بستانکار، معین، سطوح الزامی، گروه و قاعدهٔ B تفصیلی، چک تکراری، تراز) و با یک SaveChanges ثبت می‌کند. `sourceTemplateId` فقط برای ردپاست (`TB_OP_EXECUTION.TEMPLATE_ID` تهی‌پذیر؛ سند آزاد = `MANUAL`، کانال `Compose`). کلید خطا مسیر فیلد است: `voucherDate`، `description`، `apendix`، `lines[i].accountId|amount|description|cheque`، `lines[i].tafsili.{levelId}`.

خطای موتور ⇒ `422` ProblemDetails با `errors[]` (`code`, `message`, `parameterKey`, `askPrompt`)؛ الگوی ناموجود ⇒ `404`.
مبلغ به **ریال**، ارقام فارسی و جداکننده نرمال می‌شوند. `voucherDate` میلادی `yyyy-MM-dd`؛ بک‌اند آن را به `DATE_DOC` شمسی `yyyyMMdd` تبدیل می‌کند و سال سند از همان می‌آید. شناسه‌ها (الگو، معین، گروه تفصیلی، تفصیلی) Guid‌اند؛ مقدار پارامتر تفصیلی = شناسهٔ `TB_TAFSILI`.

نمونهٔ تعریف الگو:
```json
{
  "code": "RECEIVE_FROM_CUSTOMER",
  "title": "دریافت وجه از مشتری",
  "description": "وقتی پولی از مشتری دریافت و به بانک یا صندوق واریز شده است.",
  "voucherDescriptionPattern": "دریافت از {customer} - {note}",
  "parameters": [
    { "key": "amount", "title": "مبلغ", "type": 1, "detailGroupId": null, "isRequired": true, "askPrompt": "چه مبلغی دریافت شد؟", "sortOrder": 1 },
    { "key": "customer", "title": "مشتری", "type": 2, "detailGroupId": "<TB_TAFSIL_GROUP.ID>", "isRequired": true, "askPrompt": "از چه کسی دریافت شد؟", "sortOrder": 2 },
    { "key": "cashAccount", "title": "حساب دریافت", "type": 2, "detailGroupId": "<TB_TAFSIL_GROUP.ID>", "isRequired": true, "askPrompt": "پول به کدام حساب وارد شد؟", "sortOrder": 3 },
    { "key": "note", "title": "توضیح", "type": 3, "detailGroupId": null, "isRequired": false, "askPrompt": "توضیح بیشتری دارد؟", "sortOrder": 4 }
  ],
  "lines": [
    { "side": 1, "subsidiaryAccountId": "<معین بانک>", "amountParameterKey": "amount", "percent": 100, "isBalancingLine": false, "sortOrder": 1,
      "details": [ { "level": 1, "parameterKey": "cashAccount", "fixedDetailId": null } ] },
    { "side": 2, "subsidiaryAccountId": "<معین دریافتنی>", "amountParameterKey": "amount", "percent": 100, "isBalancingLine": false, "sortOrder": 2,
      "details": [ { "level": 1, "parameterKey": "customer", "fixedDetailId": null } ] }
  ]
}
```
`level` = `TB_LEVEL_TAFSIL.LEVEL_CODE` سطح‌های همان معین.

## تفاوت با طرح اولیهٔ تحویلی (تصمیم‌های ادغام)
- **شناسه‌ها `int` ⇒ `Guid`** (قرارداد `CHAR(36)` پروژه). `RowVersion` حذف شد (اوراکل rowversion ندارد و فاز ۱ ویرایش الگو ندارد).
- **`IUnitContext` حذف شد:** واحد = `VahedCode` از `IVahedScoped`، کاربر = `ICurrentUser.UserId`، ستاد = نقش `AppRoles.SetadAdmin` (چک در هندلر؛ `AppRoles` دست نخورد).
- **گروه تفصیلی سطح معین مجموعه است، نه یک مقدار:** در این اسکیما هر سطح به چند گروه وصل می‌شود (`TB_ACCOUNT_LINK_TAFSILGROUP`). `DetailLevelRule.DetailGroupIds` و چک موتور/Validator «عضویت» است.
- **تعلق تفصیلی به واحد:** ستون واحد روی تفصیلی نیست؛ قاعدهٔ B روی `TB_TAFSIL_LINK_TAFSILGROUP` (VAHEDCODE / VAHEDTYPE) در `DetailReader` حساب می‌شود و موتور فقط `IsVisibleToUnit` را می‌بیند.
- **`VoucherWriter` از `CreateVoucherHeadCommand` رد نمی‌شود:** آن Command تفصیلی ردیف نمی‌گیرد (ریسک #۲۱)، شماره سند را از فراخوان می‌خواهد و SaveChanges جدا می‌زند. به‌جایش همان مسیر سندهای خودکار خزانه/تنخواه (stage + یک SaveChanges) استفاده شد تا سند و ردپا اتمیک باشند.
- همهٔ سطح‌های پیکربندی‌شدهٔ معین الزامی‌اند (قاعدهٔ A پروژه)؛ معین فقط `TYPECODE=Moin`؛ «فعال» = `ISDELETED≠1` (معین) و `ISACTIVE≠غیرفعال` (تفصیلی).
- Migration EF ساخته نشد؛ پروژه DDL دستی دارد (`067`).

## فرانت — ماژول جدای «حسابیار» (ریپوی UI)
`src/features/assistant/` · مسیر `/assistant` · گروه منوی جدید «حسابیار». صفحه‌های قبلی صدور سند دست نخورده‌اند.
- **گفتگو:** کارت عملیات‌ها (جستجو) ← یک سؤال در هر نوبت با همان `askPrompt` (تاریخ پیش‌فرض امروز، مبلغ با حروف، تفصیلی از فهرست همان معین/سطح الگو با قاعدهٔ B، متن، تاریخ) ← پیش‌نمایش خودکار ← «ثبت سند» یا «ویرایش کامل». پاسخ‌ها حباب‌اند و با یک کلیک قابل تغییر؛ خطای سرور با `parameterKey` همان سؤال را دوباره می‌پرسد. نمای «همهٔ فیلدها» هم هست.
- **ویرایش کامل / سند دستی:** سرآیند (تاریخ، شمارهٔ خودکار، شرح، پیوست) + ردیف‌ها با همان `VoucherLineRow` فرم صدور سند (معین، تفصیلی پویا، مبلغ، شرح، چک/چک صوری) + «افزودن ردیف تراز‌کننده» + «بررسی سند» و «ثبت» در یک درخواست. خطاهای سرور روی همان فیلد نشانده می‌شوند.
- **آمادهٔ Agent:** کل جریان یک reducer با Actionهای سریال‌پذیر است (`assistantState.ts`)؛ Agent فاز ۳ همان Actionها را می‌فرستد و سند را همچنان موتور سرور می‌سازد و کاربر «ثبت» را می‌زند.
- پیش‌نمایش سمت راست همیشه از سرور است؛ فرانت سند نمی‌سازد.

## باز
- تست: `VoucherGenerationEngineTests` تحویلی و تست یکپارچهٔ `VoucherWriter` (تراز، یک `VoucherId`، تکرار `ClientRequestId`) هنوز نوشته نشده‌اند.
- قفل دوره/سال بسته در Execute چک نمی‌شود (سندهای خودکار خزانه هم نمی‌کنند).
- صفحهٔ تعریف الگو (`/assistant/templates`) ساخته شد: فهرست + فعال/غیرفعال، طراح سه‌مرحله‌ای (عملیات ← ردیف‌ها ← سؤال‌ها) با پیش‌نمایش «کاربر این را می‌بیند». برای هر سطح تفصیلی معین: «سؤال جدید» (گروه از لینک‌های همان معین/سطح)، سؤال موجود سازگار، یا تفصیلی ثابت. API: `GET templates/definitions`، `GET templates/{id}`، `POST templates/validate`، `POST templates/{id}/update` (جایگزینی کامل)، `POST templates/{id}/set-active` — تغییرها فقط مدیر ستاد.
- «شروع از نمونه» در طراح: خرید کالا، دریافت وجه از مشتری، پرداخت به فروشنده، پرداخت هزینه — فقط ساختار (سؤال‌ها، سمت ردیف‌ها، شرح، راهنمای معین)؛ معین و تفصیلی را حسابدار انتخاب می‌کند. سؤال تفصیلی بی‌گروه وقتی به سطحی با تنها یک گروه وصل شود، همان گروه را می‌گیرد.
- **فهم جمله (بدون AI):** کلمات کلیدی هر الگو (وزن ۳، جملهٔ نمونهٔ کامل +۳) در `intentMatch.ts`؛ و `sentenceExtract.ts` از جملهٔ کاربر مبلغ (رقم/حروف، هزار/میلیون/میلیارد، تومان⇒×۱۰)، تاریخ (امروز/دیروز/پریروز، `1404/07/08`، «۸ مهر») و تفصیلی (جستجوی همان lookup قاعدهٔ B، فقط اگر دقیقاً یک نتیجه) را برمی‌دارد و جملهٔ کامل را جواب اولین سؤال متنی می‌گذارد؛ جواب‌ها با برچسب «از جملهٔ شما» قابل تغییرند و ثبت همچنان با کلیک کاربر است.
- جستجوی حسابیار جملهٔ آزاد را با کلمه‌های عنوان/توضیح/سؤال‌های الگو تطبیق می‌دهد (`intentMatch.ts`، بدون AI)؛ تک‌نتیجهٔ روشن خودکار انتخاب می‌شود، وگرنه پیشنهاد سند دستی با شرح از جملهٔ کاربر.
- **شناسه / ویژگی / فیش ردیف (هم فرم ثبت سند، هم حسابیار):** سرویس مشترک `VoucherLineExtrasService` (`Vouchers/Commands/Common/VoucherLineExtras.cs`) در ایجاد/ویرایش ردیف (`extras`)، `ComposedVoucherBuilder` (خطا `lines[i].extras`) و `VoucherWriter`. قواعد: حساب شناسه‌دار (تعریف روی معین، وگرنه کل بالای آن، در واحد+سال) ⇒ مقدار هر شناسه الزامی و مطابق نوع/طول/کنترل؛ ویژگی وصل به حساب (`TB_ACCOUNTCODE.IDENTYGROUPS_ID`) یا به تفصیلی ردیف (`TB_IDENTITYGROUP.TAFSILI_ID`) ⇒ شناسنامهٔ همان گروه + همهٔ فیلدهای متغیر الزامی (ثابت‌ها از شناسنامه)؛ حساب بانکی (`TB_ACCOUNT` روی معین) بدهکار ⇒ فیش/حوالهٔ جدید اختیاری (شمارهٔ تکراری رد)، بستانکار ⇒ چک/چک صوری. خطا ⇒ ۴۲۲. ویرایش ردیف با `extras=null` دست نمی‌خورد (سندهای قدیمی قابل ویرایش می‌مانند). API خواندنی: `GET api/voucher-line-extras/requirements|identity-groups/{id}/heads|details/{id}`. در حسابیار، «ثبت» برای ردیف‌های لازم دیالوگ «تکمیل اطلاعات ردیف‌ها» را باز می‌کند (`lineExtras` در Execute بر اساس ترتیب ردیف)؛ طراح الگو برای چنین معینی یادداشت می‌دهد. بدون DDL.
- خطای مبلغ و کل ردیف در `VoucherLineRow` فیلد جدا ندارد؛ در فهرست خطاها با لینک به همان ردیف نشان داده می‌شود.

## اخیرها، تاریخچه، آمار، آزمایش، نوع واحد، قفل دوره (۲۰۲۶-۱۰-۰۶)
- **DDL `071`:** `TB_OP_TEMPLATE.ALLOWED_VAHED_TYPES` (کدهای `TB_VAHEDTYPE.TYPECODE` با «,»؛ NULL = همه). بدون آن همهٔ `/api/operations` خطا.
- **نوع واحد** (`AssistantUnitPolicy`): فهرست الگوها (`GET templates`، حالا vahed-scoped)، تفسیر هوش مصنوعی، Preview و Execute فقط الگوهای مجاز برای نوع واحد کاربر. طراح: انتخاب چندتایی «برای کدام نوع واحدها؟».
- **قفل دوره:** اگر دورهٔ سال سند برای واحد (یا بالادستی) در «بستن دوره» (`FsPeriodGuard`، DDL 062) قفل باشد، Preview/Execute الگو و سند کامل حسابیار خطای کلید `voucherDate` می‌دهند. ⚠️ فقط حسابیار — فرم ثبت سند معمولی طبق تصمیم قبلی صاحب پروژه محدود نمی‌شود. نبود جدول دوره ⇒ مانع نمی‌شود.
- **تاریخ سند = سؤال تاریخ الگو** (`TemplateVoucherDate`): اولین پارامتر تاریخ (معمولاً `{date1}`) تاریخ سند است؛ سؤال جدای تاریخ پرسیده نمی‌شود. تاریخ همیشه پرسیده می‌شود مگر از جمله برداشت شود (پیش‌فرض فقط در کادر).
- **`GET operations/recent`:** آخرین اجرای هر الگو توسط کاربر در واحدش با جواب‌ها (تفصیلی با عنوان، فقط اگر هنوز فعال/قابل دید) ⇒ «عملیات‌های اخیر من» + «تکرار» و دکمهٔ «مثل دفعهٔ قبل» (تاریخ تکرار نمی‌شود).
- **`GET operations/executions`** (صفحهٔ `/assistant/history` «سندهای حسابیار»): واحد جاری، فیلتر تاریخ شمسی/الگو/نوع ثبت/فقط من، صفحه‌بندی؛ کلیک ⇒ مشاهدهٔ سند.
- **`GET operations/templates/usage`:** تعداد و آخرین استفادهٔ هر الگو (مدیر ستاد: همهٔ واحدها) — روی کارت‌های صفحهٔ الگوها، کلیک ⇒ تاریخچهٔ همان الگو.
- **`POST operations/templates/test`** «آزمایش الگو»: تعریف ذخیره‌نشده + جواب نمونه ⇒ پیش‌نویس از همان موتور در واحد جاری؛ هیچ چیز ذخیره/ثبت نمی‌شود.

## گزارش‌ساز حسابیار (مرحلهٔ ۱ و ۲، ۲۰۲۶-۱۰-۰۶)
- اصل: حسابیار عدد نمی‌سازد؛ فقط گزارش و پارامتر را انتخاب می‌کند و **همان صفحهٔ گزارش موجود** را با آدرس پارامتردار باز می‌کند (`reportUrlParams.ts`: `from`/`to` شمسی، `docLife`، `variant`، `level`، `row`/`col`، `code`، `rowCode`/`colCode`، `sysType`، `desc`، `title`). چهار صفحه (تراز آزمایشی، ماتریسی، مرور حساب‌ها، مرور اسناد) فقط مقدار اولیه را از آدرس می‌گیرند؛ بدون پارامتر رفتار قبلی.
- **مرحلهٔ ۱** `/assistant/reports`: جمله ⇒ رتبه‌بندی گزارش‌ها (`reportExtract.ts`، بدون AI) + برداشت بازه (این ماه/ماه قبل/این فصل/فصل قبل/از اول سال/کل سال/نام ماه/«از … تا …»)، سطح/محورها («به تفکیک»)، ۴/۶/۸ ستونی، وضعیت اسناد ⇒ فقط پارامترهای لازم پرسیده می‌شود ⇒ «نمایش گزارش». کاتالوگ: `reportCatalog.ts`.
- **مرحلهٔ ۲** `/assistant/reports/manage` «گزارش‌های ذخیره‌شده»: **DDL `072`** (`TB_OP_SAVED_REPORT`: عنوان، کد، توضیح، کلمات کلیدی، نوع گزارش پایه، `SETTINGS_JSON` = `{fixed, ask}`، نوع واحدهای مجاز، فعال). API: `GET operations/reports` (فعال + مجاز برای نوع واحد)، `GET reports/definitions`، `GET reports/{id}`، `POST reports`، `POST reports/{id}/update`، `POST reports/{id}/set-active` — تغییر فقط مدیر ستاد. گزارش ذخیره‌شده در حسابیار جلوتر از گزارش پایه پیشنهاد می‌شود و فقط پارامترهای «هر بار بپرس» را می‌پرسد.

## فاز ۲ — هوش مصنوعی (فهم جمله)
- `POST api/operations/interpret?year=` با `{ sentence }` ⇒ `{ enabled, provider, succeeded, message, templateId, confidence, voucherDate, answers[{key,value,type}], clarification }`. بدون نوشتن.
- `IIntentInterpreter` (Application/OperationTemplates/Agent) با سه پیاده‌سازی در Infrastructure: `DisabledIntentInterpreter` (پیش‌فرض)، `ClaudeIntentInterpreter` (Messages API، خروجی با ابزار اجباری `fill_operation`)، `OllamaIntentInterpreter` (`/api/chat` با `format` = شِما، دما ۰).
- تنظیم: `Assistant:Llm` در appsettings — `Provider` (None/Claude/Ollama)، `ApiKey` (فقط Claude؛ فقط روی سرور یا متغیر محیطی `Assistant__Llm__ApiKey`، هرگز در مخزن)، `Model` (پیش‌فرض `claude-sonnet-5-5` / `qwen2.5:7b`)، `BaseUrl`، `ProxyUrl`، `TimeoutSeconds`. Claude بدون کلید = خاموش.
- سیاست داده (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۶): فقط جمله + فهرست الگوهای فعال (عنوان، توضیح، کلمات کلیدی، سؤال‌ها). نام طرف حساب را مدل به‌صورت متن برمی‌گرداند و فرانت با lookup همان معین/سطح (قاعدهٔ B) و فقط با یک نتیجهٔ یکتا تفصیلی را می‌گذارد.
- خروجی مدل در `IntentResultParser` دوباره کنترل می‌شود (کد الگو در فهرست، کلید سؤال مال همان الگو، مبلغ عدد مثبت، تاریخ شمسی معتبر). خطا/تأخیر سرویس ⇒ پیام کوتاه و ادامه با روش قاعده‌ای.
- فرانت: «بفرست» اول `interpret`؛ اگر الگو انتخاب شد همان `selectTemplate` + `prefill` (برداشت هوش مصنوعی بر برداشت قاعده‌ای مقدم، جواب دستی کاربر هرگز بازنویسی نمی‌شود، برچسب «برداشت هوش مصنوعی»)؛ سؤال روشن‌کننده در حباب دستیار. هوش مصنوعی هرگز Preview/Execute صدا نمی‌زند.

## فازهای بعد
- **۲:** ابزارهای خواندنی برای Agent (جستجوی اشخاص/بانک‌ها با نام، مانده‌ها).
- **۳:** `ILlmProvider` + Orchestrator + جدول گفتگو. Agent فقط Preview صدا می‌زند؛ Execute با کلیک کاربر.
- **۴:** رابط React (ریپوی فرانت) بر اساس پروتوتایپ «حسابیار».
