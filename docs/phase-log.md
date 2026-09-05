# آرشیو کامل لاگ فازها

این فایل **آرشیو کامل و بدون‌کم‌وکاست جزئیات هر فاز** پروژه است. تا ۲۰۲۶-۰۸-۲۸ این محتوا داخل `CLAUDE.md` بود؛ چون `CLAUDE.md` در ابتدای هر سشن و توسط هر ایجنت کامل خوانده می‌شود و هر فاز ۴۰–۸۰ خط به آن اضافه می‌کرد، جزئیات فازها به اینجا منتقل شد.

`CLAUDE.md` فقط یک خلاصهٔ یک/دوخطی از هر فاز (در بخش «وضعیت فعلی پروژه») نگه می‌دارد و برای جزئیات به این فایل ارجاع می‌دهد.

**ترتیب:** فاز جدیدتر بالاتر (۱۴ → ۵)، دقیقاً همان ترتیبی که در `CLAUDE.md` بود. در انتهای فایل، آرشیو verbatim چک‌لیست تفصیلی پیشین «وضعیت فعلی» (شامل کارهای پیش از فاز ۵ که هرگز شمارهٔ فاز نگرفتند) آمده است.

⚠️ **دربارهٔ ارجاع‌های داخلی:** متن زیر عیناً و بدون هیچ ویرایشی منتقل شده. پس:
- «فاز X بالاتر/پایین‌تر» به فازهای **همین فایل** اشاره دارد.
- هر ارجاع به «تصمیمات باز» یا «تصمیم باز پایین‌تر» حالا در `docs/open-decisions.md` است.
- ارجاع به «تصمیم معماری اول/دوم/سوم» همچنان در `CLAUDE.md` است.
- «این فایل» در متن فازها به `CLAUDE.md` اشاره می‌کند (متن در زمان نگارش آنجا بوده).

---

### فاز ۱۴ — CRUD دستهٔ سوم: ۸ Entity مستقل (۲۰۲۶-۰۸-۲۸، برنچ `EntityCRUD`، commit نشده)

اجرای مکانیکی همان الگوی فازهای ۵–۱۳ روی ۸ Entity بعدی از بخش ۳ سند `docs/tamin-core-entity-reference.md`. **هیچ تصمیم معماری جدیدی گرفته نشد.** کار بین سه ایجنت موازی `backend-dotnet` تقسیم شد (روی مجموعه‌فایل‌های مجزا)، و سه فایل مشترک (`DependencyInjection.cs`, `RepositoryRegistrationTests.cs`, `HttpVerbConventionTests.cs`) عمداً برای `team-lead` رزرو شد تا نوشتن موازی روی آن‌ها تداخل نسازد.

| Entity | جدول | Endpoint | `ISDELETED` | ۴۰۹ |
|---|---|---|---|---|
| AttribForAccountCode | `TB_ATTRIBFORACCOUNTCODE` | ۵ | `bool` | ✅ `AK_AK_ATTRIBFORMAINCO_ATTRIBFO` |
| LevelTafsil | `TB_LEVEL_TAFSIL` | ۵ | `bool` | ❌ UNIQUE ندارد |
| TafsilGroup | `TB_TAFSIL_GROUP` | ۵ | `bool` | ✅ `UK_TBTAFSILGROUP` |
| IdentityGroup | `TB_IDENTITYGROUPS` | ۵ | `bool` | ✅ `UK_IDENTITYGROUPCODE` |
| IdentitySubGroup | `TB_IDENTITYSUBGRPS` | ۵ | `bool` | ✅ `AK_AK_IDENTYSUBGRPS_IDENTYSU` |
| ChequeType | `TB_CHECK_TYPE` | ۵ | `bool` | ❌ UNIQUE ندارد |
| **VahedInfo** | `TB_VAHED_INFO` | **۴ (CRU)** | **ندارد** | ✅ `UK_VAHEDINFO` |
| WorkShop | `TB_WORKSHOP` | ۵ | **`bool?`** | ✅ `UK_WORKSHOP` |

**درس فاز ۱۲/۱۳ برای سومین بار جواب داد — هر ۸ Entity جداگانه verify شد، نه با فرض یکسان:**

- **`TB_VAHED_INFO` شدیدترین مورد تا امروز است:** نه `ISDELETED` دارد، نه **هیچ** ستون Audit ای (`ADDUSERID`/`CHANGEUSERID`/`CREATEDDATE`/`UPDATEDDATE` هیچ‌کدام وجود ندارند). پس (۱) Delete ساخته نشد، و (۲) **Handlerهایش اصلاً `ICurrentUser` را تزریق نمی‌کنند** — جایی برای مهر زدن نیست. این از `TB_PREDESCRIB` فاز ۱۳ هم یک قدم جلوتر است (آن دست‌کم یک `ADDUSERID` داشت). ⚠️ **پیامد ثبت‌شده: نوشتن روی جدول ریشهٔ سلسله‌مراتب واحد سازمانی هیچ ردّ Audit ای به‌جا نمی‌گذارد.** این یک واقعیت schema است، نه چیزی که در لایهٔ Application قابل جبران باشد؛ اختراع ستون نشد.
- **`TB_WORKSHOP.ISDELETED` از نوع `bool?` است** (مثل `TB_RABET`، برخلاف ۶ Entity دیگر این دسته)، پس هم `false` و هم `null` یعنی «حذف‌نشده» و فقط `true` صریح یعنی حذف‌شده. ردیف با `ISDELETED == null` واقعاً soft-delete می‌شود و idempotent تلقی **نمی‌شود** — با تست صریح قفل شد.
- ستون‌های Audit `TB_WORKSHOP` همگی **nullable** اند (برخلاف بقیه)، ولی همچنان فقط از `ICurrentUser` نوشته می‌شوند.

**گارد «غیاب عمدی» برای `VahedInfo` (الگوی `PreDescrib` فاز ۱۳):** دو تست جدید — `VahedInfoSchemaAssumptionsTests` (reflection: تأیید نبودِ هر ۵ ستون `ISDELETED`/`ADDUSERID`/`CHANGEUSERID`/`CREATEDDATE`/`UPDATEDDATE`) و `NoDeleteActionExistsOnVahedInfosController_...`. دلیلش در XML doc ثبت شد که چرا این جدول از `TB_PREDESCRIB` هم حساس‌تر است: ریشهٔ سلسله‌مراتب واحد سازمانی است، هم self-reference دارد (`PARENT_ID`) و هم جدول‌های دیگر (`TB_WORKSHOP.BRANCH_ID`, `TB_TAFSILI`, `TB_WHITELIST`) به آن اشاره می‌کنند — پس حذفش هم فرزندان و هم وابسته‌ها را یتیم می‌کرد.

**ریسک `sys_guid()` — رویهٔ فاز ۱۳ عیناً تکرار شد، نه رویکرد دوم.** `TB_VAHED_INFO.ID` یکی از ۸ ستون دارای `HasDefaultValueSql("sys_guid() ")` است (ریسک 🔴 ثبت‌شده: مقدار ۳۲کاراکتری بدون dash و UPPERCASE که `GuidToChar36Converter` سخت‌گیر ما هنگام خواندن رد می‌کند). **Fluent Mapping دست نخورد**؛ به‌جایش مثل `CreateRabetCommandHandler` همیشه `ID = Guid.NewGuid()` سمت Application تولید می‌شود تا DEFAULT اوراکل هرگز فعال نشود. گزینهٔ «حذف `HasDefaultValueSql`» عمداً انتخاب **نشد** تا دو رویهٔ رقیب برای یک مسئله در پروژه وجود نداشته باشد.

**self-reference:** `UpdateVahedInfoCommandValidator` قانون `Must(x => x.ParentId != x.Id)` را دارد (قاعدهٔ فاز ۸ — Update برخلاف Create می‌تواند `Id` را از قبل بداند، پس فقط این مسیر می‌تواند گرهٔ خودارجاع بسازد).

**سه فایل مشترک که `team-lead` مرکزی سیم‌کشی کرد:**
- `DependencyInjection.cs` — ۱۶ ثبت جدید (۸ write + ۸ read).
- `RepositoryRegistrationTests` — ۳۲ InlineData جدید. ضمناً تست دوم از `...MapsPhase13InterfaceToItsOwnImplementation` به `...MapsInterfaceToItsOwnImplementation` تعمیم یافت (نه یک تست موازی جدید) و XML docش توضیح می‌دهد که تست اول **جابه‌جایی** ثبت را نمی‌گیرد (descriptor وجود دارد و Scoped هم هست، فقط به repository جدول اشتباه اشاره می‌کند). جفت‌های مستعد اشتباه این دسته: `IdentityGroup`/`IdentitySubGroup` و `LevelTafsil`/`TafsilGroup`.
- `HttpVerbConventionTests` — `Phase14Controllers_UpdateAndDelete_AreHttpPost` + `AllControllerActions_IncludesEveryPhase14Controller` (محافظ در برابر سبز شدن توخالی گارد `HttpPut`/`HttpDelete`).

**نگاشت FK باز هم بدون یک خط کد جدید** — ۴ Entity این دسته FK دارند (`AttribForAccountCode`→`TB_ACCOUNTCODE`، `IdentityGroup`→`TB_TAFSILI`، `IdentitySubGroup`→`TB_IDENTITYGROUPS`، `WorkShop`→`TB_ACCOUNTCODE`/`TB_VAHED_INFO`)؛ نگاشت مرکزی ORA-02291 → 400 در `UnitOfWork` (فاز ۱۱) خودکار اعمال شد. هیچ pre-check دستی اضافه نشد.

**تست: ۴۴۴ تست جدید، مجموع ۱۳۱۳/۱۳۱۳ سبز** (۲۲ Domain + ۱۰۶۲ Application + ۱۱۶ Api + ۱۱۳ Infrastructure)، صفر رگرسیون. build ۰ خطا / ۱۸ warning پیش‌موجود NU1903 (هیچ CS). ⚠️ همگی Unit/Mock — **هیچ اتصالی به Oracle زنده و هیچ تست repository واقعی (SQLite) برای این ۸ Entity نوشته نشد** (همان شکاف باز فاز ۱۳).

**پاس `/code-review` (۲۰۲۶-۰۸-۲۸، قبل از commit، ۸ Agent موازی روی جفت‌های مختلف Entity):** ۱ باگ واقعی پیدا و رفع شد — `IdentitySubGroup.SubgrpsLen` (ستون Oracle `SUBGRPS_LEN`, `NUMBER(2)`, حداکثر ۹۹) هیچ محدودیت بالایی در Validator نداشت با اینکه نوع CLR‌ش `byte` (تا ۲۵۵) بود؛ یعنی مقدار مثلاً ۱۵۰ از FluentValidation رد می‌شد و فقط موقع `SaveChangesAsync` با یک خطای خام Oracle (`ORA-01438`) به ۵۰۰ می‌رسید، نه ۴۰۰ تمیز. یک قانون `LessThanOrEqualTo(99)` به Create و Update Validator اضافه شد + ۴ تست رگرسیون (مجموع از ۱۳۱۳ به ۱۳۱۷ رسید). بقیهٔ ۷ Agent هیچ باگ واقعی پیدا نکردند؛ فقط نکات ساختاری (تکرار الگوی repository/paging/validation در ۱۹ فایل، بدون base class مشترک — عمداً رفع نشد، هم‌راستا با تصمیم‌های قبلی پروژه دربارهٔ تکرار پذیرفته‌شده به‌جای انتزاع زودرس) و یک یادداشت دربارهٔ `UK_TBTAFSILGROUP` (شامل `ISDELETED`، یعنی soft-delete یک کد `TafsilGroup` آن کد را برای همیشه غیرقابل‌استفاده می‌کند — از قبل در XML doc همان Command مستند بود).

### فاز ۱۳ — CRUD دستهٔ دوم: ۸ Entity مستقل (۲۰۲۶-۰۸-۲۷، برنچ `EntityCRUD`، commit نشده)

ادامهٔ backlog «۴۲ Entity مستقل» از `docs/tamin-core-entity-reference.md` بخش ۳. دستهٔ اول (۹ Entity لوکاپ ساده) به **تصمیم صریح صاحب پروژه اصلاً CRUD نگرفت** — دادهٔ آن‌ها مستقیم در Oracle مدیریت می‌شود. این فاز دستهٔ دوم است: ۸ Entity.

**هیچ تصمیم معماری جدیدی گرفته نشد.** کل فاز اجرای مکانیکی الگوی تثبیت‌شدهٔ فازهای ۵ تا ۱۱ است. هیچ فایلی در `Accounting.Domain/Entity/`, `LegacyDbContext.cs`, `Program.cs`, `GlobalExceptionHandler.cs`, `Accounts/` و `Vouchers/` لمس نشد.

#### 🔴 درس فاز ۱۲ دوباره اعتبارسنجی شد — و دوباره جواب داد

طبق درس دستهٔ اول («هیچ فرضی دربارهٔ وجود `ISDELETED`/Audit درست نیست»)، schema هر ۸ Entity **جداگانه** از روی فایل Entity و Fluent Mapping واقعی بررسی شد (نه از روی سند مرجع). نتیجه: **۷ تا یکسان بودند، یکی نبود.**

| Entity | `ISDELETED` | ستون‌های Audit | خروجی | UNIQUE → ۴۰۹؟ |
|---|---|---|---|---|
| `TB_ACCOUNTCODE_INTERFACE` | `bool` | کامل | CRUD (۵ Endpoint) | ندارد |
| `TB_ACCOUNTEXCEPTION` | `bool` | کامل | CRUD (۵) | ندارد |
| `TB_BILL_LOG` | `bool` | کامل | CRUD (۵) | ندارد |
| `TB_PERSON_ACTION` | `bool` | کامل | CRUD (۵) | ✅ `UK_PERSON_ACTION` |
| **`TB_PREDESCRIB`** | **❌ ندارد** | **فقط `ADDUSERID` (nullable)** | **CRU (۴) — بدون Delete** | ندارد |
| `TB_RABET` | `bool?` | همه nullable | CRUD (۵) | ✅ `UK_RABET` |
| `TB_WHITEANDBLACKLIST` | `bool?` | کامل | CRUD (۵) | ✅ `UK_WHITEANDBLACKLIST` |
| `TB_WHITELIST` | `bool?` | کامل | CRUD (۵) | ندارد |

**`TB_PREDESCRIB` دقیقاً همان استثنایی بود که درس فاز ۱۲ پیش‌بینی‌اش را می‌کرد.** نه `ISDELETED` دارد، نه `CREATEDDATE`/`UPDATEDDATE`/`CHANGEUSERID`؛ تنها ستون Audit آن یک `ADDUSERID` **nullable** است. پیامدها (همگی مستند در XML doc، نه ضمنی):
- **هیچ Delete ساخته نشد** — نه Command، نه Handler، نه Endpoint. دلیل: بدون `ISDELETED` تنها گزینه‌ها حذف فیزیکی (که یکپارچگی ارجاعی Legacy را می‌شکند و برخلاف رفتار سیستم قدیمی است) یا تغییر schema اوراکل (که این پروژه هرگز انجام نمی‌دهد) بودند.
- **Update اصلاً `ICurrentUser` را تزریق نمی‌کند** — چون هیچ ستونی برای مهر زدن ندارد. این در XML doc صریحاً توضیح داده شد تا شبیه یک فراموشی به‌نظر نرسد.
- Update به `ADDUSERID` **دست نمی‌زند** (Audit ساخت، تغییرناپذیر). فقط Create آن را از `ICurrentUser` می‌نویسد.
- سمت خواندن هیچ فیلتر `ISDELETED` ندارد و `PreDescribDto` فیلد `IsDeleted` ندارد.
- شرط ۴۰۴ در Update فقط «رکورد وجود ندارد» است — حالت soft-deleted اصلاً وجود ندارد.
- ⚠️ **تلهٔ نام‌گذاری:** کلاس CLR اسمش `TB_PREDESCRIB` (مفرد) است ولی به جدول اوراکل **`TB_PREDESCRIBS` (جمع)** نگاشت می‌شود.

**گارد ساختاری برای غیبتِ عمدی Delete** (`PreDescribSchemaAssumptionsTests` + یک تست در `HttpVerbConventionTests`): با reflection قفل می‌کند که (۱) `TB_PREDESCRIB` هیچ پراپرتی `ISDELETED`/`CREATEDDATE`/`UPDATEDDATE`/`CHANGEUSERID` ندارد، (۲) هیچ تایپی به نام `DeletePreDescribCommand` وجود ندارد، (۳) `IPreDescribRepository` هیچ متدی با «Delete» در نامش ندارد، (۴) `PreDescribsController` اکشن `Delete` ندارد. **هدف: اگر روزی کسی Delete اضافه کند، تست قرمز می‌شود و او را مجبور می‌کند سؤال schema را دوباره جواب دهد، نه اینکه بی‌صدا فرض کند.**

#### Endpointها — ۳۹ عدد روی ۸ Controller

الگو برای هر Entity (همگی زیر `SetFallbackPolicy(RequireAuthenticatedUser)`):

| Verb | Route | موفق | خطاها |
|---|---|---|---|
| `POST` | `/api/{entity}` | 201 + `Location` + `{id}` | 400, 401, [409], 500 |
| `GET` | `/api/{entity}?pageNumber=&pageSize=` | 200 `PagedResult<TDto>` | 400, 401, 500 |
| `GET` | `/api/{entity}/{id:guid}` | 200 `TDto` | 400, 401, 404, 500 |
| `POST` | `/api/{entity}/{id:guid}/update` | 200 + `{id}` | 400, 401, 404, [409], 500 |
| `POST` | `/api/{entity}/{id:guid}/delete` | 200 + `{id}` | 400, 401, 404, 500 |

routeها: `/api/account-code-interfaces`, `/api/account-exceptions`, `/api/bill-logs`, `/api/person-actions`, `/api/pre-describs`, `/api/rabets`, `/api/white-and-black-lists`, `/api/white-lists`.

- **محدودیت «فقط `GET`/`POST`» فاز ۸ کاملاً رعایت شد** — صفر `[HttpPut]`/`[HttpDelete]` در کل solution (با اسکن reflection روی کل assembly قفل شده). `HttpVerbConventionTests` گسترش یافت و حالا صریحاً تأیید می‌کند مجموعهٔ اسکن‌شده **شامل هر ۸ Controller جدید** است (محافظ در برابر پاس‌شدن vacuous).
- **۴۰۹ فقط روی سه Entity ای اعلام شد که واقعاً UNIQUE index دارند** (`PersonAction`, `Rabet`, `WhiteAndBlackList`). برای پنج تای دیگر عمداً اعلام **نشد** — دقیقاً همان قضاوت فاز ۱۰ دربارهٔ `TB_VOUCHERSDETAIL`: اعلام ۴۰۹ بدون constraint متناظر، گمانه‌زنی است.
- **۴۰۱ روی هر ۳۹ اکشن** اعلام شد. **۴۰۳ عمداً هیچ‌جا اعلام نشد** (هنوز هیچ authorization مبتنی بر نقش وجود ندارد).
- **نقض FK هیچ کد جدیدی لازم نداشت** — `AccountCodeInterface`/`AccountException`/`PreDescrib`/`Rabet`/`WhiteAndBlackList`/`WhiteList` همگی FK دارند، ولی نگاشت مرکزی ORA-02291 → `ForeignKeyViolationException` → **400** در `UnitOfWork` (فاز ۱۱) به‌صورت خودکار روی همهٔ مسیرهای نوشتن جدید هم اعمال می‌شود. **هیچ pre-check دستی اضافه نشد** — که عمدی است: pre-check یک قانون کسب‌وکاری اختراع می‌کرد و شرایط رقابتی را هم درست مدیریت نمی‌کرد.

#### تصمیم‌های اجرایی (همگی تکرار الگوی موجود، نه تصمیم جدید)

- Command فقط primitive؛ `ID = Guid.NewGuid()` در Handler. **هرگز به Oracle DEFAULT تکیه نشد** — این ضمناً ریسک باز 🔴 `sys_guid()` را برای `TB_RABET`/`TB_WHITELIST`/`TB_WHITEANDBLACKLIST` (که هر سه `ID DEFAULT sys_guid()` دارند) **عملاً خنثی می‌کند**، چون آن DEFAULT هرگز اجرا نمی‌شود. در XML doc هر Create Handler ثبت شد.
- Audit فقط سمت سرور از `ICurrentUser` + `DateTime.UtcNow`؛ هیچ Command پراپرتی `AddUserId`/`ChangeUserId`/`CreatedDate`/`UpdatedDate` ندارد (با grep روی کل لایهٔ Application تأیید شد).
- Update = جایگزینی کامل (PUT semantics)؛ `Id` از route نه بدنه (یک record جدای `Update{X}Request` در لایهٔ Api که اصلاً `Id` ندارد) → تناقض id مسیر/بدنه **ساختاراً** ناممکن.
- ستون‌های تغییرناپذیر در Update: `ID`, `ADDUSERID`, `CREATEDDATE`, `ISDELETED` (با تست صریح روی هر ۸ Entity قفل شد).
- Repository فقط stage می‌کند و **هرگز** `SaveChangesAsync` صدا نمی‌زند؛ مرز تراکنش در Handler، یک‌بار. write repositoryها اصلاً متد حذف ندارند.
- `ISDELETED` سه‌مقداری: برای `Rabet`/`WhiteList`/`WhiteAndBlackList` که `bool?` اند، هم `false` و هم **`null`** یعنی «حذف‌نشده» — با تست صریح قفل شد که رکورد `ISDELETED == null` واقعاً قابل ویرایش است و واقعاً soft-delete می‌شود.
- Delete ایدمپوتنت: رکورد از قبل حذف‌شده → ۲۰۰ + `{id}` **بدون هیچ نوشتنی** و بدون `SaveChangesAsync`؛ Audit قبلی دست‌نخورده می‌ماند.

#### 🔴 گارد جدید DI — شکافی که هیچ تست دیگری نمی‌دید

`RepositoryRegistrationTests` (جدید، در `Accounting.Infrastructure.Tests`) اضافه شد. دلیل: یک repository که interface دارد، compile می‌شود، به Handler تزریق شده و **تست unit کاملش با Moq سبز است** ولی در `AddInfrastructure` ثبت نشده، توسط **هیچ‌کدام** از ۸۶۹ تست دیگر گرفته نمی‌شد — چون تست‌های Application همگی mock می‌دهند و تست‌های Api مستقیماً Controller می‌سازند. اولین نشانهٔ چنین اشتباهی، خطای runtime روی یک Endpoint زنده می‌بود.
- روی `ServiceDescriptor`ها assert می‌کند و سرویس‌ها را **resolve نمی‌کند** — عمدی، چون resolve کردن باعث ساخت `LegacyDbContext` می‌شد و این suite هرگز نباید به Oracle زنده نزدیک شود. بازرسی descriptor صفر I/O و صفر instantiation دارد.
- هم ثبت‌بودن + `Scoped` بودن هر ۲۳ سرویس را چک می‌کند، هم نگاشت هر interface به **implementation درست خودش** (تا اشتباه کپی/پیست مثل ثبت `IWhiteListRepository` روی `WhiteAndBlackListRepository` گرفته شود — محتمل‌ترین جفت اشتباه این دسته).

#### تست

**۴۵۲ تست جدید، مجموع ۸۶۹/۸۶۹ سبز** (۲۲ Domain + **۶۶۷** Application + **۹۹** Api + **۸۱** Infrastructure)، از ۴۱۷ قبلی. **صفر رگرسیون.** build ۰ خطا / ۱۸ warning پیش‌موجود NU1903 (هیچ warning نوع CS).
- Application: از ۲۷۱ به ۶۶۷ (+۳۹۶) — برای هر Entity: Handlerهای Create/Update/Delete و همهٔ Validatorها و Query Handlerها.
- Api: از ۸۲ به ۹۹ (+۱۷) — گسترش `HttpVerbConventionTests`.
- Infrastructure: از ۴۲ به ۸۱ (+۳۹) — `RepositoryRegistrationTests`.
- ⚠️ همگی Unit/Mock — **هیچ اتصالی به Oracle زنده**، و هیچ تست repository روی SQLite برای این ۸ Entity نوشته نشد (برخلاف فازهای ۹/۱۰/۱۱ که برای cascade داشتند). یعنی صحت Fluent Mapping و رفتار واقعی INSERT/UPDATE این ۸ جدول همچنان **اثبات‌نشده** است.

### فاز ۱۲ — تحلیل کسب‌وکار پروژهٔ مرجع `D:\CentralAccount` + اولین اتصال Oracle زنده (۲۰۲۶-۰۸-۲۵/۲۶، برنچ `EntityCRUD`، commit `b224db2`، push شده)

به درخواست صریح صاحب پروژه («اگه مسیر پروژه اصلی خودمو که قبلا حسابداری متمرکز با کدینگ شناور نوشتم بهت بدم میتونی فقط بخونی...»)، یک پروژهٔ حسابداری متمرکز واقعی و کامل دیگر (`D:\CentralAccount`، همان schema اوراکل `CENTRALACCOUNT`) به‌صورت **کاملاً Read-Only** تحلیل شد — همهٔ ۱۰ زیرپروژهٔ solution، هر ۱۵۶ Command، هر ۲۲۱ Query، هر ۳۷۲ Handler (سطح متفاوت عمق — رجوع پایین‌تر)، هر ۴۲ Controller، هر ۴۴ enum. خروجی در دو سند جدید: `docs/centralaccount-business-reference.md` (~۱۷۲۰ خط) و `docs/centralaccount-improvement-opportunities.md` (نقد معماری/کارایی، برای استفادهٔ بعدی).

⚠️ **این سند هم مثل `docs/tamin-core-entity-reference.md` یک مرجع طراحی است، نه منبع قانون کسب‌وکار پروژهٔ ما.** ولی چون این‌بار شامل منطق واقعی لایهٔ Application (نه فقط Entity) روی **همان** schema ماست، سیگنالش قوی‌تر است.

#### کشف اصلی — باگ سیستماتیک `bool?` روی ستون‌های چندمقداری `NUMBER(1)`

هنگام بررسی چرایی سؤال «آیا CRUD فعلی `AccountCode` بین گروه/کل/معین تمایز می‌گذارد»، مشخص شد **۱۹ ستون در ۱۳ جدول** به‌اشتباه `bool?` اسکفولد شده‌اند در حالی که واقعاً enumهای ۲ تا ۷ مقداری‌اند (احتمالاً یک heuristic نادرست ابزار Scaffold: «هر `NUMBER(1)` = بولین»، هرگز چک نشده). **کامنت‌های ستون Oracle هم قابل‌اعتماد نیستند** — ۵ مورد با کد اجراشونده در تناقضند.

دو مورد **در مسیر نوشتن فعال ما** تأیید شدند (نیاز به اصلاح، هنوز اصلاح نشده — رجوع «تصمیمات باز»):
- `TB_ACCOUNTCODE.TYPECODE` → باید `Group=1 / Kol=2 / Moin=3` باشد، نه `bool?`. سه شاهد مستقل در پروژهٔ مرجع (تعریف enum، استفاده در کوئری EF، hardcode در Handler).
- `TB_VOUCHERSHEAD.DOCLIFE` → باید ۴مقداری باشد (یادداشت/موقت/بررسی‌شده/تأیید دائم)، نه `bool?`. `UpdateVoucherHeadCommand` فعلی ما اجازه می‌دهد این فیلد آزادانه از Update عوض شود — یعنی هم نوع دادهٔ غلط هست هم عملیات تغییر وضعیت باید جدا از Update معمولی مدل شود (پروژهٔ مرجع این دو را کاملاً جدا کرده).
- خطرناک‌ترین مورد کشف‌شده (هنوز روی جدولی که ما CRUD نداریم): `TB_TAFSILI.ISACTIVE` — `DEFAULT 1` و اسم شبیه بولین‌اند، ولی enum واقعی `{IsActive=1, DeActive=2}` است و **۰ اصلاً در enum نیست**؛ یعنی مقدار غیرفعال (۲) با تبدیل `bool` فعلی احتمالاً `true` خوانده می‌شود.

#### حل سه ابهام با اولین کوئری Read-Only واقعی روی Oracle زنده (۲۰۲۶-۰۸-۲۶)

سه ابهام باقی‌مانده با خواندن کد پروژهٔ مرجع به‌تنهایی قابل‌حل نبودند (یا خودِ کد مرجع هم متناقض بود) — این **اولین بار در کل تاریخچهٔ این پروژه** بود که یک کوئری واقعی (فقط `SELECT`) روی Oracle زندهٔ `setadidevdb.tamin.org` اجرا شد. صریحاً توسط صاحب پروژه تأیید شد. هیچ credential در هیچ فایل/گزارشی درز نکرد (چک شد).

| ابهام | نتیجه |
|---|---|
| **`TB_ACCOUNTCODE.TYPEACTIVITY`** | ✅ **قطعی حل شد: ۱=بدهکار، ۲=بستانکار.** کامنت ستون Oracle ما («۱بستانکار۲بدهکار») **غلط/کهنه** بوده؛ enum پروژهٔ مرجع درست بود. شاهد بدون استثنا: حساب‌های بدیهی مثل «دارایی‌های جاری»/«بانک ملت» = ۱ (با گردش `DEBTOR=10, CREDITOR=0`)، «بدهی‌های جاری» = ۲. شاهد جانبی: `TYPECODE=2` (کل) در ۶۲/۶۲ ردیف `TYPEACTIVITY=NULL` دارد — نگاشت `TYPECODE` را هم مستقلاً روی دادهٔ زنده تأیید کرد. |
| **عرض enum شبیه `VAHEDTYPE`** | ⚠️ جزئی — `DATA_PRECISION=1` تأیید شد (Fluent Mapping ما از نظر عرض درست است)، ولی معنای دقیق مقادیر (داده فقط `{1,3}` داشت) با `TypeVahed` پروژهٔ مرجع قطعی نشد؛ دیتابیس dev کوچیک بود. **حدس زده نشد.** |
| **`TB_VOUCHERSHEAD.ISAUTOMATIC`** | ✅ فرضیهٔ باگ تأیید شد (با همبستگی کامل، نه اثبات کد): ۵۴ سند ماژول «حسابداری» (ثبت دستی) همگی `ISAUTOMATIC=1`؛ ۳ سند «دریافت و پرداخت» (خودکارتر) همگی `0` — دقیقاً برعکس چیزی که اسم ستون می‌گوید. |

**⚠️ ریسک جدید و پیش‌بینی‌نشده (کشف حین همین کوئری):** ۳ حساب واقعی **سطح گروه** در دادهٔ زنده `TYPEACTIVITY` برابر **۴، ۵، ۶** دارند، در حالی که Validator پروژهٔ مرجع برای سطح گروه فقط ۱..۳ را مجاز می‌داند. یعنی **دادهٔ Legacy موجود از قبل قاعده‌ای را نقض می‌کند که ممکن است بخواهیم بعداً enforce کنیم** — اگر آن Validator را در `Accounting.Application` پیاده کنیم، رکوردهای تاریخی معتبر نامعتبر می‌شوند. دلیلش حدس زده نشد (سه تفسیر ممکن در `docs/centralaccount-business-reference.md` بخش ۲۳ ثبت شده).

⚠️ **محدودیت مهم که باید در نظر داشت:** همهٔ دادهٔ این کوئری از یک دیتابیس **محیط توسعه** با حجم کم و ردیف‌های آشکارا تستی آمد، نه از دیتابیس عملیاتی. برای `TYPEACTIVITY`/`ISAUTOMATIC` مشکلی ایجاد نکرد (شواهد بدون استثنا بودند)، ولی برای `VAHEDTYPE` دقیقاً همین باعث نتیجهٔ ناقص شد.

#### یافته‌های دیگر (مستند در `docs/centralaccount-business-reference.md`، خارج از دامنهٔ فوری)

- **«الزامی بودن تفصیلی» مدل شده است** — منبع حقیقت `TB_ACCOUNT_LINK_LEVEL`، مکانیزمش «وجود یا نبودِ ردیف» است، نه یک ستون (رجوع به اصلاح جدول Accounting Safety Gate در `.claude/agents/team-lead.md`).
- **چندمستأجری (`VahedCode`) در پروژهٔ مرجع فقط روی ۱۲ Handler از ۳۷۲ اعمال می‌شود** (فقط `BankCartDetails`) — نه روی کدینگ حساب/سند/تفصیلی/دریافت‌پرداخت. درس مستقیم برای ریسک باز خودمان: اگر پیاده کنیم، باید `Behavior` سراسری باشد نه per-handler.
- سند از مسیر Kafka هم وارد می‌شود (نه فقط HTTP)؛ `AddUserId` در آن مسیر از خودِ پیام می‌آید.
- شکل کامل تراز آزمایشی ۴/۶/۸ ستونه (فرمول + View منبع) مستند شد — برای گزارش‌های آیندهٔ ما.
- `Cartable` یک Entity نیست، façade روی همان Commandهای سند است.
- تراز بدهکار/بستانکار در پروژهٔ مرجع در مسیرهای **خودکار** enforce می‌شود (نه در مسیر دستی)؛ تغییرناپذیری سند enforce می‌شود ولی با یک Command جداگانهٔ تغییر وضعیت، نه از دل Update معمولی.
- `TB_RABET_CLOSING` = حساب واسط اختتامیه (ابهام قبلی حل شد).

**پوشش نهایی:** ۱۰۰٪ ساختاری (enum/Command/Query/Handler-سطح‌بالا/Controller/زیرپروژه). عمق خط‌به‌خط فقط روی ~۴۰ Handler از ۳۷۲ (بقیه با استخراج مکانیکی، در سند با ⚙️ علامت خورده‌اند). باقی‌مانده برای دفعهٔ بعد: SQL کامل ~۲۵ گزارش در `VouchersDetailRepository.cs` (۲٬۷۶۶ خط)، ~۱۸۵ فایل DTO، `Behaviors`/`Registration.cs`، تست‌ها، مسیر SOAP/Kafka.

**همچنین در این فاز:** سه فایل ایجنت (`team-lead.md`, `database-reverse-engineer.md`, `entity-mapper.md`) اصلاح شدند تا تصریح کنند Discovery/Scaffold کل دیتابیس **کامل و بسته** است — برای CRUD روی هر Entity موجود دیگر نباید `database-reverse-engineer` صدا زده شود.

**هیچ کد `backend/` در این فاز تغییر نکرد** — فقط `docs/` و `.claude/agents/`.

### فاز ۱۱ — نگاشت خطای FK + مسیر نوشتن تفصیلی ردیف سند (۲۰۲۶-۰۸-۲۵، برنچ `EntityCRUD`، commit نشده)

دو کار مکانیکی و طبق الگوی از‌قبل‌تثبیت‌شده، هر دو از فهرست تصمیمات باز فاز ۱۰. **هیچ تصمیم معماری جدیدی گرفته نشد و هیچ ریسک باز دیگری (IDOR، تراز بدهکار/بستانکار، نوع دادهٔ مبلغ) لمس نشد.**

#### ۱. نگاشت ORA-02291 → `ForeignKeyViolationException` → **400 Bad Request** ✅

ریسک 🔴 «نقض FK به‌جز UNIQUE هیچ نگاشتی ندارد → 500 خام» بسته شد.

- `ForeignKeyViolationException` جدید در `Accounting.Application/Common/Exceptions/` — **خواهر ساختاری دقیق `DuplicateKeyException`** (بدون هیچ تایپ Oracle/EF، تا `Accounting.Api` وابستگی به Oracle نگیرد).
- `UnitOfWork.SaveChangesAsync` حالا دو `catch` دارد. شماره‌های Oracle به دو ثابت نام‌دار تبدیل شدند (`OracleUniqueConstraintViolated = 1`، `OracleParentKeyNotFound = 2291`) تا عدد جادویی در کد نماند.
- **چرا 400 و نه 409 (تصمیم صریح، نه پیش‌فرض):** ORA-00001 یک *تعارض با state موجود* است — رکوردی که می‌خواهی بسازی از قبل هست و فراخوان می‌تواند آن را reconcile کند و دوباره بفرستد؛ این دقیقاً معنای 409 است. ORA-02291 از جنس دیگری است: فراخوان شناسه‌ای فرستاده که به **هیچ چیز** اشاره نمی‌کند — یعنی *ورودی نامعتبر*، نه تعارض. **404 هم بررسی و رد شد:** منبع هدفِ درخواست (خودِ endpoint) وجود دارد، و 404 روی این routeها از قبل معنای باریک و متفاوتی دارد (سرسند ناموجود، از pre-check صریح فاز ۱۰) — دوگانه‌کردن آن معنا، تشخیص علت را برای فراخوان غیرممکن می‌کرد.
- **دامنه عمداً فقط ORA-02291.** خواهرش ORA-02292 («child record found»، هنگام حذف فیزیکی والدی که فرزند دارد) نگاشت **نشد** چون این پروژه اصلاً حذف فیزیکی ندارد — همهٔ مسیرهای حذف soft delete اند و هیچ FK ای به آن‌ها اعتراض نمی‌کند. نگاشتش گمانه‌زنی روی مسیری می‌بود که وجود ندارد (با تست قفل شد).
- **چون `UnitOfWork` مرکزی است، این نگاشت به‌صورت یکسان روی همهٔ مسیرهای نوشتن اعمال می‌شود** — Create/Update هر سه Entity، composite create، و نوشتن تفصیلی — بدون هیچ opt-in per-command که کسی یادش برود.
- **نکتهٔ ظریف قرارداد:** این تنها 400 در کل API است که بدنه‌اش `ProblemDetails` ساده است نه `HttpValidationProblemDetails` با دیکشنری `errors` — چون نسبت‌دادن خطا به یک فیلد مشخص بدون افشای نام constraint اوراکل ممکن نیست. با تست قفل شد و در XML doc ثبت شد.
- پیام کاملاً عمومی («One or more referenced records do not exist.»)؛ با تست اثبات شد که `ORA-02291`، نام constraint و نام schema به بدنهٔ پاسخ درز نمی‌کنند.

⚠️ **همچنان باز:** `TB_VOUCHERDETAIL_LINK_TAFSILI.TAFSILI_ID`/`LEVEL_ID` اصلاً FK ندارند، پس تفصیلی نامعتبر **هیچ** خطایی نمی‌دهد و بی‌صدا نوشته می‌شود. این نگاشت آن شکاف را پوشش نمی‌دهد (ریسک باز از قبل، نه چیز جدید).

#### ۲. مسیر نوشتن تفصیلی روی ردیف سند ✅

ریسک 🟡 «می‌توان ردیف سند ساخت ولی نمی‌توان به آن تفصیلی نسبت داد» بسته شد.

- record ترابری مشترک `VoucherDetailTafsiliLinkInput(Guid TafsiliId, Guid LevelId)` در `Vouchers/Commands/Common/` (پوشهٔ مشترک، چون برخلاف `CreateVoucherHeadDetailInput` دو مصرف‌کننده دارد).
- به `CreateVoucherDetailCommand` و `UpdateVoucherDetailCommand` یک پارامتر **پایانیِ اختیاری** `TafsiliLinks = null` اضافه شد → کاملاً **غیر‌breaking**.
- **`TAFSILI_ID`/`LEVEL_ID` تنها فیلدهای ورودی‌اند.** `VOUCHERSDETAIL_ID`/`VAHEDCODE`/`YEAR` همگی از **خودِ ردیف** مشتق می‌شوند — یعنی لینکی که با والدش دربارهٔ واحد/سال اختلاف داشته باشد **ساختاراً** غیرقابل‌بیان است (همان الگوی تصمیم ۳ فاز ۱۰).
- **این هنوز CRUD مستقل نیست.** قاعدهٔ تیمی «هر جدول `*_LINK_TAFSIL*`/`*_LINK_LEVEL*` برای همیشه تعبیه‌شده است» دست‌نخورده ماند: نه Controller، نه MediatR request، نه repository اختصاصی. متدهای جدید (`AddTafsiliLinkAsync`, `GetActiveTafsiliLinksAsync`) روی **repository والد** (`IVoucherDetailRepository`) و parent-scoped اند — دقیقاً همان شکل `SoftDeleteTafsiliLinksAsync` موجود. عمداً `AddAsync`/`GetForUpdateAsync` نام‌گذاری **نشدند**، چون آن دو نام در این پروژه شکل «Aggregate Root» را دارند و `NoIndependentLinkTableWritePathTests` آن‌ها را ممنوع می‌کند.
- **`NoIndependentLinkTableWritePathTests` گسترش یافت** (نه دور زده شد): دو تست جدید — یکی ممنوع می‌کند که هیچ repository interface ای مختص جدول لینک باشد (وگرنه گارد با ساختن `ITafsiliLinkRepository` قابل دور زدن بود)، و یکی **مثبت** است و تأیید می‌کند مسیر نوشتن parent-scoped واقعاً وجود دارد — تا کسی با حذف آن، این فایل را سبز نکند و بی‌صدا شکاف فاز ۱۰ را دوباره باز کند.

**تصمیم رفتار Update — جایگزینی کامل، با تفکیک عمدی `null` از `[]`:**

| ورودی | رفتار |
|---|---|
| `null` (یا اصلاً ارسال نشود) | **لینک‌ها اصلاً لمس نمی‌شوند** — نه کوئری، نه نوشتن |
| `[]` (آرایهٔ خالی) | همهٔ لینک‌های فعال soft-delete می‌شوند |
| لیست پر | diff سه‌طرفه: نبوده→insert، حذف‌شده→soft-delete، مشترک→**کاملاً دست‌نخورده** |

- **چرا جایگزینی کامل و نه append-only:** چون جدول عمداً CRUD مستقل ندارد، اگر Update فقط «افزودن» می‌بود، **حذف یک تفصیلی از یک ردیف برای همیشه از طریق API غیرممکن می‌شد**. ضمناً بقیهٔ این Command از قبل PUT کامل است.
- **چرا `null` و `[]` عمداً یکی نشدند** (تنها انحراف از PUT سختگیرانه، و دلیلش مشخص است نه سلیقه‌ای): سمت **خواندن** اصلاً تفصیلی را برنمی‌گرداند (`VoucherDetailDto` فیلد لینک ندارد)، پس فراخوان **فیزیکاً قادر به round-trip خواندن-ویرایش-نوشتن روی لینک‌ها نیست**. اگر «ارسال‌نشده» به معنی «پاک کن» بود، هر فراخوان قدیمیِ درست‌رفتار — که اصلاً از وجود این فیلد خبر ندارد — در اولین ویرایش نامرتبطِ ردیف، دادهٔ تفصیلی را بی‌صدا نابود می‌کرد. با این تفکیک، تخریب فقط **صریح** ممکن است.
- **مشترک‌ها عمداً re-stamp نمی‌شوند** (`CHANGEUSERID`/`UPDATEDDATE` دست‌نخورده) — وگرنه سیگنال Audit «چه کسی اولین بار این تفصیلی را نسبت داد» در هر ویرایش نامرتبط از بین می‌رفت.
- زوج تکراری `(TafsiliId, LevelId)` در ورودی به یک ردیف جمع می‌شود (این جدول هیچ UNIQUE ندارد، پس DB جلوی ردیف دوم را نمی‌گیرد). ولی همان تفصیلی در **دو سطح متفاوت** تکراری نیست و هر دو نوشته می‌شوند (با تست قفل شد).
- زوجی که فقط به‌صورت soft-deleted وجود دارد، **ردیف تازه** می‌گیرد نه resurrect — ردیف قدیمی با Audit اصلی خودش حذف‌شده می‌ماند.
- Audit (`ADDUSERID`/`CHANGEUSERID`) فقط از `ICurrentUser`؛ همهٔ لینک‌ها همان تک‌مقدار زمانی ردیف را می‌گیرند (یک بار `DateTime.UtcNow`).
- همه‌چیز در **یک `SaveChangesAsync`** — Repository فقط stage می‌کند، مرز تراکنش در Handler ماند.

**تست: ۳۸ تست جدید، مجموع ۴۱۷/۴۱۷ سبز** (۲۲ Domain + ۲۷۱ Application + ۸۲ Api + ۴۲ Infrastructure)، صفر رگرسیون. build ۰ خطا / ۱۸ warning پیش‌موجود NU1903 (هیچ CS). شامل ۵ تست جدید `UnitOfWorkTests` (ORA-02291 واقعی از طریق reflection روی constructor داخلی درایور، تفکیک از ORA-00001، عدم افشای نام constraint، و رد‌نشدن ORA-02292)، ۲ تست `GlobalExceptionHandlerTests` (400 + نبودِ دیکشنری `errors`)، و ۵ تست **واقعی repository روی SQLite in-memory** برای مسیر نوشتن تفصیلی (شامل اثبات اینکه repository فقط stage می‌کند و entityهای برگشتی tracked اند). ⚠️ همگی Unit/Mock یا SQLite — **هیچ اتصالی به Oracle زنده**.

**پاس `/code-review` (۲۰۲۶-۰۸-۲۵، قبل از commit):** یک باگ واقعی پیدا و همان‌جا رفع شد — لینک تفصیلیِ **مشترک** (هم در DB هم در درخواست) هنگام Update هرگز `VAHEDCODE`/`YEAR` را با مقدار جدید ردیف sync نمی‌کرد، یعنی اگر فراخوان همزمان `VahedCode`/`Year` ردیف را عوض می‌کرد، لینکِ نگه‌داشته‌شده بی‌صدا با والدش ناسازگار می‌ماند — دقیقاً برخلاف invariant مستندشده در `VoucherDetailTafsiliLinkInput` («لینک هرگز نباید با والدش دربارهٔ واحد/سال اختلاف داشته باشد»). اصلاح شد: لینک‌های مشترک حالا `VAHEDCODE`/`YEAR` را از ردیف (پس از overwrite) می‌گیرند، ولی ستون‌های Audit (`ADDUSERID`/`CREATEDDATE`/`CHANGEUSERID`/`UPDATEDDATE`) همچنان دست‌نخورده می‌مانند (تصمیم قبلی «عدم re-stamp» فقط دربارهٔ Audit بود، نه دربارهٔ فیلدهای مشتق‌شده). ۱ تست رگرسیون جدید اضافه شد (`Handle_KeptLink_ResyncsVahedCodeAndYear_ButLeavesAuditColumnsUntouched`) → مجموع از ۴۱۶ به ۴۱۷ رسید.

سه یافتهٔ دیگر بررسی و **عمداً رفع نشدند** (مستندسازی به‌جای حدس، طبق قاعدهٔ تیمی):
- **ناهماهنگی `[ProducesResponseType]` برای ۴۰۰:** همهٔ Endpointهای نوشتن (نه فقط `VoucherDetailsController`) فقط `HttpValidationProblemDetails` را برای ۴۰۰ اعلام می‌کنند، در حالی‌که از فاز ۱۱ به بعد `ForeignKeyViolationException` یک بدنهٔ ۴۰۰ متفاوت (`ProblemDetails` ساده، بدون دیکشنری `errors`) هم تولید می‌کند. این یک شکاف مستندسازی OpenAPI در **کل API** است (از قبلِ این Session هم بود، چون FK mapping مرکزی روی همهٔ Commandها اعمال می‌شود)، نه فقط این Controller. رفع کامل نیاز به تصمیم دارد که آیا دو `[ProducesResponseType]` با یک status code متفاوت در Swashbuckle درست render می‌شود یا باید در سطح schema به شکل دیگری مدل شود — به‌عنوان یک follow-up مستقل ثبت شد.
- تکرار منطق collapse زوج تکراری `(TafsiliId, LevelId)` بین `CreateVoucherDetailCommandHandler` و `UpdateVoucherDetailCommandHandler.ReconcileTafsiliLinksAsync` — هم‌راستا با تکرار پذیرفته‌شدهٔ مشابه بین `VoucherHeadRepository`/`VoucherDetailRepository` در فاز ۹/۱۰، همان تصمیم اعمال شد: مستند شد، رفع نشد.
- یک تست (`Handle_EmptyTafsiliLinks_SoftDeletesEveryExistingLink`) فقط audit stamp لینک اول را assert می‌کند نه دوم را — شکاف پوشش جزئی، بدون ریسک واقعی فعلی.

### فاز ۱۰ — CRUD مستقل `TB_VOUCHERSDETAIL` + composite create سند (۲۰۲۶-۰۸-۲۰، برنچ `EntityCRUD`، commit نشده)

اجرای مستقیم تصمیم صریح صاحب پروژه دربارهٔ **مرز Aggregate ترکیبی** (رجوع به «تصمیمات باز» و `docs/tamin-core-entity-reference.md` بخش ۵). نقل مستقیم: «VouchersDetail مستقل هم می‌تونه باشه ولی وقتی سندی ثبت میشه هدر و دیتیل باید با هم ذخیره بشه، و بعداً جهت ویرایش سند و اضافه کردن دیتیل جدید به‌صورت مستقل هم قابل‌فراخوانیه.»

**دو مسیر نوشتن برای یک Entity — هر دو معتبر و هم‌زیست:**

| Endpoint | Verb | موفق | خطاها |
|---|---|---|---|
| `/api/voucher-details` | `POST` | 201 + `Location` + `{id}` | 400, 401, **404**, 500 |
| `/api/voucher-details?pageNumber=&pageSize=&voucherHeadId=&year=&vahedCode=` | `GET` | 200 `PagedResult<VoucherDetailDto>` | 400, 401, 500 |
| `/api/voucher-details/{id:guid}` | `GET` | 200 `VoucherDetailDto` | 400, 401, 404, 500 |
| `/api/voucher-details/{id:guid}/update` | `POST` | 200 + `{id}` | 400, 401, 404, 500 |
| `/api/voucher-details/{id:guid}/delete` | `POST` | 200 + `{id}` | 400, 401, 404, 500 |
| `/api/voucher-heads` (گسترش‌یافته) | `POST` | 201 + `Location` + `{id}` | 400, 401, 409, 500 |

محدودیت «فقط `GET`/`POST`» فاز ۸ کاملاً رعایت شد؛ `HttpVerbConventionTests` حالا صریحاً تأیید می‌کند که مجموعهٔ اسکن‌شده **خالی نیست و شامل `VoucherDetailsController` است** (محافظ در برابر همان باگی که در `/code-review` فاز ۸ پیدا شد).

**تصمیم ۱ — Controller مستقل (`/api/voucher-details`)، نه `/api/voucher-heads/{id}/lines`.** چون صاحب پروژه `TB_VOUCHERSDETAIL` را Aggregate Root **مستقل** اعلام کرد و هر Entity در این پروژه Controller خودش را دارد. ⚠️ در نتیجه ادعای قبلی در XML doc خودِ `VoucherHeadsController` («ردیف‌های سند بعداً زیر `/{id}/lines` می‌آیند») **منسوخ شد** و با یادداشت صریح «Superseded (۲۰۲۶-۰۸-۲۰)» اصلاح شد.

**تصمیم ۲ — composite create با گزینهٔ (الف): پراپرتی اختیاری روی همان `CreateVoucherHeadCommand`**، نه یک `CreateVoucherHeadWithDetailsCommand` جدا. دلیل:
- گزینهٔ (ب) اجباراً منطق ساخت سرسند را **دو بار** می‌نوشت (یک Handler نمی‌تواند Handler دیگر را از طریق MediatR صدا بزند بدون اینکه دومی `SaveChangesAsync` خودش را بزند و invariant «یک تراکنش» را بشکند) → خطر drift بین دو نسخه.
- گزینهٔ (ب) دو route ساخت برای یک منبع می‌ساخت، که برای مصرف‌کننده مبهم است.
- گزینهٔ (الف) با پارامتر **پایانیِ دارای مقدار پیش‌فرض** (`IReadOnlyList<CreateVoucherHeadDetailInput>? InitialDetails = null`) کاملاً **غیر‌breaking** است: بدنهٔ JSON بدون `initialDetails` دقیقاً مثل قبل رفتار می‌کند (با تست قفل شد و در schema تولیدشدهٔ OpenAPI هم `nullable` و غیر‌`required` است).

**استثنای صریح و مستندِ قانون «Command فقط primitive»:** `CreateVoucherHeadDetailInput` یک record تودرتو است. قانون فاز ۵ در اصل می‌گوید «Command هرگز **Entity** نیست»؛ یک record ترابری با فیلدهای صرفاً primitive، Entity دامنه نیست — پس روحِ قانون رعایت شده. این اولین لیست تودرتو در یک Command پروژه است و استثنا **آگاهانه** ثبت شد.

**تصمیم ۳ — `CreateVoucherHeadDetailInput` عمداً `VoucherHeadId`, `VahedCode`, `Year` ندارد.** سرسند هنوز وجود ندارد (ID را Handler تولید می‌کند) و `VAHEDCODE`/`YEAR` از **خودِ سرسندِ در حال ساخت** مشتق می‌شوند. یعنی ناسازگاری هدر/ردیف **ساختاراً** ناممکن است — همان الگوی `UpdateXRequest` که `Id` ندارد.

**تصمیم ۴ — `VOUCHERSHEAD_ID` در Update تغییرناپذیر است.** جابه‌جایی یک ردیف بین دو سند یک *move* است نه یک *edit*، و هیچ تصمیم کسب‌وکاری آن را مجاز نکرده. کنار `ID`/`ADDUSERID`/`CREATEDDATE`/`ISDELETED` قفل شد (با تست).

**تصمیم ۵ — `CreateVoucherDetailCommand` وجود سرسند را pre-check می‌کند** (نبود یا `ISDELETED == true` → `NotFoundException` → 404). دلیل: کل معنای این عملیات «افزودن ردیف به سند **موجود**» است، پس بررسی وجودش خودِ semantics عملیات است نه قانون اختراعی؛ ضمناً بدون آن، FK اوراکل `FK_VOUCHERHEAD` (ORA-02291) به‌صورت خام **500** می‌داد. ⚠️ شرایط رقابتی (حذف سرسند بین check و insert) همچنان به 500 می‌رسد — ثبت‌شده به‌عنوان مورد باز.

**تصمیم ۶ — حذف ردیف سند به لینک‌های تفصیلی خودش cascade می‌کند** (`SoftDeleteTafsiliLinksAsync` روی `IVoucherDetailRepository`، در همان تک `SaveChangesAsync`). این قانون جدیدی نیست: صرفاً اعمال یک‌سطح‌پایین‌ترِ invariant فاز ۹ («پس از حذف، هیچ چیزی زیر آن فعال نمی‌ماند») به‌همراه قاعدهٔ تیمی «جدول‌های `*_LINK_TAFSIL*` همیشه تعبیه‌شده‌اند». نکتهٔ ظریف schema: `TB_VOUCHERDETAIL_LINK_TAFSILI.ISDELETED` از نوع `bool` **غیر-nullable** است، پس فیلتر سادهٔ `== false` درست است و شاخهٔ `== null` لازم ندارد (برخلاف دو جدول بالادست).

**فاز ۹ دست‌نخورده ماند** (به دستور صریح کاربر): `SoftDeleteDetailTreeAsync` و هر ۱۱ تست SQLite آن **از نظر رفتاری تغییر نکردند**؛ فقط XML doc آن به‌روز شد چون شرطی که خودش نوشته بود («اگر روزی مسیر نوشتن مستقلی ساخته شد، باید بازبینی/استخراج شود») حالا برقرار شده. استخراج/رفع تکرار به‌عنوان follow-up ثبت شد.

**۴۰۹ عمداً روی هیچ‌کدام از Endpointهای ردیف سند اعلام نشد** — `TB_VOUCHERSDETAIL` هیچ UNIQUE constraint ندارد (فقط ایندکس‌های غیریکتا `IDX_VDETAIL_ACC_VHEAD_ISDEL`, `IDX_VOUCHERSDETAIL_HEADID`, `IDX_YEAR_VAHED`). اعلامش گمانه‌زنی می‌بود. قرینه‌اش: `POST /api/voucher-heads` برعکس 409 دارد ولی 404 ندارد.

**یافتهٔ `api-contract` (شکاف واقعی، رفع شد):** پروژهٔ `Accounting.Application` اصلاً `GenerateDocumentationFile` نداشت و `Program.cs` فقط فایل XML پروژهٔ `Api` را به Swagger می‌داد — یعنی **همهٔ** XML docهای Command/DTO/Query (که در لایهٔ Application زندگی می‌کنند) در Swagger **نامرئی** بودند. با افزودن `GenerateDocumentationFile` + `NoWarn CS1591` به csproj و بارگذاری `Accounting.Application.xml` در Swagger رفع شد؛ با اجرای واقعی API و fetch کردن `/swagger/v1/swagger.json` راستی‌آزمایی شد.

**تست: ۱۰۷ تست جدید، مجموع ۳۷۹/۳۷۹ سبز** (۲۲ Domain + ۲۴۴ Application + ۷۶ Api + ۳۷ Infrastructure)، صفر رگرسیون. build ۰ خطا / ۱۸ warning پیش‌موجود NU1903 (هیچ CS). شامل:
- composite create: تک بودن `SaveChangesAsync`، ترتیب همهٔ `AddAsync` قبل از آن، اشتراک یک `CREATEDDATE`/`ADDUSERID` بین هدر و همهٔ ردیف‌ها، مشتق‌شدن `VAHEDCODE`/`YEAR` از هدر، و رفتار byte-identical وقتی `InitialDetails` غایب/خالی است.
- ۶ تست واقعی repository روی **SQLite in-memory** برای `SoftDeleteTafsiliLinksAsync` (با `LegacyDbContext` تازه در فاز assert، پس persistence واقعی اثبات می‌شود).
- ۲ محافظ رگرسیون جدید: تأیید غیرخالی‌بودن مجموعهٔ اسکن `HttpVerbConventionTests`، و یک تست reflection که قفل می‌کند **هیچ** جدول `*_LINK_TAFSIL*`/`*_LINK_LEVEL` مسیر نوشتن مستقل ندارد.
- ⚠️ همگی Unit/Mock یا SQLite — **هیچ اتصالی به Oracle زنده**.

### فاز ۹ — cascade کامل سه‌سطحی حذف نرم سند (۲۰۲۶-۰۸-۲۰، برنچ `changejWT`، commit نشده)

به درخواست صریح صاحب پروژه، در دو مرحله: اول «وقتی سند از هدر حذف بشه، دیتیل‌هاش هم باید حذف بشن»، سپس افزودن سطح سوم (`TB_VOUCHERDETAIL_LINK_TAFSILI`).

**زنجیرهٔ کامل cascade — هر سه سطح در یک تراکنش/یک `SaveChangesAsync`:**
`TB_VOUCHERSHEAD` → `TB_VOUCHERSDETAIL` → `TB_VOUCHERDETAIL_LINK_TAFSILI`

**تصمیم ۱ — محل متد: داخل همان `IVoucherHeadRepository`** (نه repository/interface مستقل برای هیچ‌کدام از دو جدول فرزند).
متد: `Task<int> SoftDeleteDetailTreeAsync(Guid headId, string? changeUserId, DateTime updatedDate, CancellationToken)` — که مجموع ردیف‌های حذف‌شدهٔ هر دو سطح را برمی‌گرداند.
دلیل: اجتناب از premature abstraction — هیچ Command/Query مستقلی برای `TB_VOUCHERSDETAIL` یا `TB_VOUCHERDETAIL_LINK_TAFSILI` وجود ندارد و هر دو فقط به‌عنوان بخشی از aggregate سند قابل دسترسی‌اند؛ همان قضاوتی که قبلاً برای `ITokenManager` هم اعمال شد. سطح سوم عمداً **داخل همان متد** ادغام شد (نه متد دوم) چون IDهای ردیف‌ها از قبل در حافظه هستند (بدون کوئری اضافه) و این کار ساختاراً غیرممکن می‌کند که کسی سطح ۲ را cascade کند ولی سطح ۳ را فراموش کند. نام متد از `SoftDeleteDetailLinesAsync` به `SoftDeleteDetailTreeAsync` تغییر کرد تا دامنهٔ واقعی‌اش را بیان کند. اگر روزی مسیر نوشتن مستقلی ساخته شد، باید بازبینی و استخراج شود (در XML doc ثبت شد).

**تصمیم ۳ — دامنهٔ سطح سوم عمداً وسیع‌تر از «ردیف‌هایی که همین الان حذف شدند» است.**
لینک‌های تفصیلی بر اساس **همهٔ** ردیف‌های سند (صرف‌نظر از `ISDELETED` آن‌ها) فیلتر می‌شوند، نه فقط ردیف‌هایی که در همین فراخوان حذف شدند. دلیل: اگر ردیفی قبلاً به‌تنهایی soft-delete شده بود ولی لینک‌های تفصیلی‌اش فعال مانده بودند، محدودکردن دامنه به ردیف‌های تازه‌حذف‌شده باعث می‌شد آن لینک‌های فعالِ یتیم زیر یک سند حذف‌شده باقی بمانند. این تضمین می‌کند «پس از حذف سند، هیچ چیزی زیر آن فعال نمی‌ماند». برای همین هم فقط **یک** کوئری برای ردیف‌ها زده می‌شود (بدون فیلتر `ISDELETED` در SQL) و تفکیک «نیازمند حذف» از «مجموعهٔ کامل ID» در حافظه انجام می‌شود.

**تصمیم ۲ — load+mutate، نه `ExecuteUpdateAsync`.**
با اینکه EF Core 10 از `ExecuteUpdateAsync` پشتیبانی می‌کند، عمداً استفاده **نشد**: آن متد بلافاصله و خارج از change tracker روی دیتابیس اجرا می‌شود و invariant پروژه («Repository فقط stage می‌کند؛ Handler تنها مالک مرز تراکنش است و یک‌بار `SaveChangesAsync` صدا می‌زند») را می‌شکند. بدون یک `BeginTransactionAsync` صریح، ممکن بود update ردیف‌ها commit شود ولی update سرسند بعداً شکست بخورد — یعنی **cascade نیمه‌کاره**، که برای دادهٔ حسابداری غیرقابل‌قبول است. مقیاس هم کراندار است (ده‌ها ردیف در هر سند) پس load+mutate ارزان است.

**نکتهٔ ظریف NULL — و اینکه چرا در سطح سوم فرق می‌کند:** در `TB_VOUCHERSDETAIL` فیلتر عمداً `d.ISDELETED == null || d.ISDELETED == false` نوشته شد، نه `d.ISDELETED != true`. چون `ISDELETED` آنجا `bool?` است، فرم دوم در منطق سه‌مقداری SQL ردیف‌های `NULL` را حذف می‌کرد — در حالی که Handler سرسند از قبل `null` را «حذف‌نشده» تلقی می‌کند. این با تست واقعی روی SQLite تأیید شد (ترجمهٔ SQL: `"ISDELETED" IS NULL OR NOT ("ISDELETED")`).
اما در `TB_VOUCHERDETAIL_LINK_TAFSILI` ستون `ISDELETED` از نوع `bool` **غیر-nullable** است (تفاوت واقعی schema بین این جدول و دو جدول بالادست)، پس آنجا فیلتر سادهٔ `l.ISDELETED == false` کافی و درست است و شاخهٔ `== null` لازم ندارد.

**Idempotency حفظ شد:** گارد موجود `if (entity.ISDELETED == true) return;` بالای فراخوان cascade ماند، پس سندِ از‌قبل‌حذف‌شده نه سرسند و نه هیچ ردیفی را لمس می‌کند و اصلاً به `SaveChangesAsync` نمی‌رسد. سرسند و همهٔ ردیف‌ها با **یک مقدار زمانی مشترک** (`var now = DateTime.UtcNow` یک‌بار محاسبه) و همان `ICurrentUser.UserId` مهر می‌خورند.

**تست (۱۳ تست جدید، مجموع ۲۷۲/۲۷۲ سبز):**
- ۲ تست Handler در `Accounting.Application.Tests` (mock): فراخوانی cascade با همان user/timestamp سرسند، و تضمین ترتیب cascade **قبل از** `SaveChangesAsync`.
- ۱۱ تست **واقعی repository** در `Accounting.Infrastructure.Tests` روی **SQLite in-memory** (نه InMemory provider، چون آن ترجمهٔ SQL را اثبات نمی‌کند). سطح ۲: N ردیف، صفر ردیف، ردیف‌های از‌قبل‌حذف‌شده با audit قبلی byte-identical، ردیف‌های `ISDELETED = NULL`، ایزولاسیون بین اسناد. سطح ۳: cascade کامل سه‌سطحی، ایزولاسیون لینک‌های سند دیگر، لینک از‌قبل‌حذف‌شده که دوباره لمس نمی‌شود، سند با ردیف ولی بدون لینک، صحت مقدار بازگشتی، و **تست invariant «لینک یتیم»** (ردیفِ از‌قبل‌حذف‌شده با لینک‌های فعال → لینک‌ها باز هم حذف می‌شوند، ولی audit خود ردیف دست‌نخورده می‌ماند).
- همهٔ تست‌ها فاز assert را با یک `LegacyDbContext` تازه انجام می‌دهند، پس **persistence واقعی** اثبات می‌شود نه صرفاً وضعیت change tracker.
- ⚠️ محدودیت: `EnsureCreated()` روی کل `LegacyDbContext` در SQLite شکست می‌خورد (`SQLite does not support sequences` — به‌خاطر `HasSequence("VOUCHERHEAD_SEQ")`)، پس fixture فقط همان یک جدول را با `CREATE TABLE` دستی می‌سازد. این تست‌ها رفتار Oracle-specific را اثبات **نمی‌کنند**.
- پکیج جدید فقط در پروژهٔ تست: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0 (۲ warning جدید NU1903 از وابستگی گذرای `SQLitePCLRaw`، فقط test-only و نه در API منتشرشده → مجموع warning از ۱۶ به ۱۸).

**خارج از دامنه (دست‌نخورده):** `UpdateVoucherHeadCommand` (به ردیف‌ها دست نمی‌زند) و همهٔ مسیرهای `*AccountCode*` — ریسک 🔴 «حذف گرهٔ کدینگ بدون بررسی وابستگی» همچنان کاملاً باز است.

### فاز ۸ — تکمیل CRUD: Update + Delete (۲۰۲۶-۰۸-۱۹، برنچ `changejWT`، commit نشده)

پیش از این فاز فقط Create (فاز ۵) و Read (فاز ۶) وجود داشت. حالا هر دو Entity چرخهٔ کامل CRUD دارند.

**Endpointهای جدید (همگی زیر `SetFallbackPolicy(RequireAuthenticatedUser)`):**

| Verb | Route | موفق | خطاها |
|---|---|---|---|
| `POST` | `/api/account-codes/{id:guid}/update` | 200 + `{id}` | 400, 401, 404, 409, 500 |
| `POST` | `/api/account-codes/{id:guid}/delete` | 200 + `{id}` | 400, 401, 404, 500 |
| `POST` | `/api/voucher-heads/{id:guid}/update` | 200 + `{id}` | 400, 401, 404, 409, 500 |
| `POST` | `/api/voucher-heads/{id:guid}/delete` | 200 + `{id}` | 400, 401, 404, 500 |

> ⚠️ **چرا اینجا REST استاندارد رعایت نشده — این یک محدودیت درخواستی صاحب پروژه است، نه انتخاب معماری داخلی.**
> این ۴ Endpoint ابتدا به‌صورت `PUT /{id}` و `DELETE /{id}` ساخته شدند (و سبز بودند)، ولی **صاحب پروژه صریحاً اعلام کرد که متدهای `PUT` و `DELETE` در این محیط قابل استفاده نیستند و فقط `POST` و `GET` مجازند** (دلیل فنی‌اش را نگفتند؛ احتمالاً محدودیت زیرساخت شبکه/سرور سازمان). پس همگی به `POST` با **فعل صریح در route** تبدیل شدند.
> - **چرا `/{id}/update` و نه `POST /api/account-codes/{id}` خالی:** چون `POST /api/account-codes` (ساخت) از قبل وجود دارد و `POST /api/account-codes/{id}` در کنارش «ساخت زیرمنبع تحت این id» خوانده می‌شود. حالا که از semantics استاندارد خارج شده‌ایم، صراحت بهتر از ابهام است و مسیر برای Endpointهای بعدی (مثل `/{id}/lines`) باز می‌ماند.
> - **چرا 200 با بدنه و نه 204:** (الف) همان محدودیت زیرساختی که `PUT`/`DELETE` را ممنوع کرده نشان می‌دهد لایه‌های میانی شبکه در این محیط رفتار غیرمعمول دارند و پاسخ ۲۰۰ با بدنهٔ کوچک کمتر از ۲۰۴ خالی مستعد ابهام است؛ (ب) با `POST` ساخت که از قبل بدنه برمی‌گرداند هم‌خانواده می‌شود، پس هر سه عملیات نوشتنِ هر Controller یک شکل پاسخ دارند؛ (ج) `id` برگشتی تأیید می‌کند دقیقاً کدام رکورد تحت تأثیر قرار گرفته. برای این کار **چهار record پاسخ جدید** (`UpdateAccountCodeResponse`, `DeleteAccountCodeResponse`, `UpdateVoucherHeadResponse`, `DeleteVoucherHeadResponse`) هم‌سبک `CreateAccountCodeResponse` موجود اضافه شد.
> - **قفل شده با تست:** `HttpVerbConventionTests` با reflection روی کل assembly تأیید می‌کند **هیچ اکشنی در هیچ Controllerی** `[HttpPut]` یا `[HttpDelete]` ندارد — پس بازگشت تصادفی این قاعده تست را قرمز می‌کند.
> - **`GET` (لیست و by-id) و `POST` ساخت دست‌نخورده ماندند** — محدودیت فقط روی مسیر نوشتن/حذف بود.
> - این تغییر **فقط لایهٔ Api را لمس کرد**؛ Command/Handler/Validator/Repository دست‌نخورده ماندند (Controller خودش `id` را از route دارد و بدنهٔ پاسخ را می‌سازد).

**تصمیم PUT به‌جای PATCH (تصمیم صریح، نه پیش‌فرض):** تقریباً همهٔ ستون‌های Legacy nullable اند (`bool?`, `Guid?`, `string?`)، بنابراین در PATCH جزئی هیچ راهی نیست که «فیلد ارسال‌نشده» از «فیلد صریحاً `null`» تفکیک شود مگر با wrapper نوع `Optional<T>` روی تک‌تک فیلدها — که هم قانون «Command فقط primitive دارد» را می‌شکند و هم ماشین‌آلات نامتناسبی اضافه می‌کند. PUT ضمناً با شکل `CreateXCommand` موجود قرینه است. **پیامد پذیرفته‌شده:** فراخوان باید همیشه بدنهٔ کامل بفرستد؛ فیلد جاافتاده = `null` شدن آن ستون.

**تصمیم‌های تثبیت‌شدهٔ دیگر:**
- **`Id` از route می‌آید، نه از بدنه.** برای PUT یک record جدا در لایهٔ Api تعریف شد (`UpdateAccountCodeRequest`/`UpdateVoucherHeadRequest`) که **اصلاً پراپرتی `Id` ندارد**؛ Controller خودش Command را با id مسیر می‌سازد. یعنی کل کلاسِ باگِ «تناقض id مسیر با id بدنه» **ساختاراً** ناممکن است. (این ضمناً اولین اجرای عملی پیشنهاد باز `api-contract` مبنی بر جداکردن Request از Command است — ولی فقط برای PUT؛ POST همچنان بدنه‌اش خود Command است.)
- **Audit فقط سمت سرور** — `CHANGEUSERID = _currentUser.UserId` و `UPDATEDDATE = DateTime.UtcNow`. دقیقاً همان الگوی فاز ۷؛ هیچ پراپرتی userId در هیچ‌کدام از ۴ Command وجود ندارد، پس بازگشت آسیب‌پذیری جعل Audit **خطای کامپایل** می‌دهد.
- **ستون‌های تغییرناپذیر در Update:** `ID`، `ADDUSERID`، `CREATEDDATE`، `ISDELETED`، و برای VoucherHead ستون‌های `GLOBALNUMBER` (که `ValueGeneratedOnAdd` است) و `ATTACHFILE`. در Command وجود ندارند و Handler لمسشان نمی‌کند (با تست صریح قفل شد).
- **`ISDELETED` عمداً در Update نیست** — اگر بود، Update به در پشتیِ delete/undelete تبدیل می‌شد.
- **404 مرکزی** — `NotFoundException` جدید در `Accounting.Application/Common/Exceptions/` → `GlobalExceptionHandler` → **404** `ProblemDetails` با پیام عمومی. الگو دقیقاً از `DuplicateKeyException` → 409 گرفته شد. پیام خود exception (شامل نام منبع و id) عمداً **به بدنهٔ پاسخ درز نمی‌کند** (با تست اثبات شد).
- **Repository:** یک متد `GetForUpdateAsync(Guid, CancellationToken)` به هر دو write repository اضافه شد که **عمداً tracked است** (بدون `AsNoTracking()`، برخلاف read repository) تا Handler بتواند در جا mutate کند. Repository همچنان **هرگز** `SaveChangesAsync` صدا نمی‌زند؛ مرز تراکنش در Handler ماند. `IUnitOfWork` **هیچ تغییری نکرد** — همان‌طور که در فاز ۵ طراحی شده بود.

**Soft Delete (نه حذف فیزیکی):** Command حذف فقط `ISDELETED = true` + ستون‌های audit را می‌نویسد. هیچ `Remove`/`ExecuteDelete` در کد نیست — و چون interfaceهای write repository اصلاً متد حذف **ندارند**، حذف فیزیکی از این مسیر ساختاراً ناممکن است (این را `qa-tester` با تست reflection روی interface قفل کرد). دلیل: هر دو جدول ستون `ISDELETED` دارند که Query side از قبل با `Where(x => x.ISDELETED != true)` استفاده می‌کند؛ حذف فیزیکی هم یکپارچگی ارجاعی Legacy را می‌شکند و هم برخلاف رفتار سیستم قدیمی است.

**رفتار مرزی (تصمیم‌گرفته‌شده و تست‌شده):**
- Update روی رکورد ناموجود **یا** رکورد `ISDELETED == true` → 404 (رکورد soft-deleted «منطقاً غایب» تلقی می‌شود، هم‌راستا با فیلتر Query side).
- Delete روی رکورد ناموجود → 404.
- Delete روی رکوردی که از قبل حذف شده → **200 + `{id}` بدون هیچ نوشتنی** (idempotent طبق تعریف HTTP؛ status code خودِ ۲۰۰ است، نه ۲۰۴ — طبق تصمیم «۲۰۰ با بدنه» بالا)؛ `SaveChangesAsync` اصلاً صدا زده نمی‌شود و `CHANGEUSERID`/`UPDATEDDATE` قبلی دست‌نخورده می‌ماند.
- `ISDELETED` از نوع `bool?` است: هم `false` و هم **`null`** یعنی «حذف‌نشده» — دقیقاً مثل فیلتر read side. رکورد با `ISDELETED == null` واقعاً soft-delete می‌شود و idempotent تلقی نمی‌شود (ظریف‌ترین حالت مرزی این فاز؛ با تست صریح قفل شد).
- خطای UNIQUE در PUT از همان مسیر موجود عبور می‌کند (`UnitOfWork` → `DuplicateKeyException` → 409)؛ هیچ pre-check دستی اضافه نشد، که برای شرایط رقابتی (race) رفتار درستی است.

**یافتهٔ `api-contract`:** **401 در هیچ‌کدام از Endpointها اعلام نشده بود** — نه در ۴ تای جدید، نه در ۶ تای قبلی — با اینکه همگی زیر FallbackPolicy اند و 401 کاملاً قابل‌تولید است. برای هر ۱۰ Endpoint به‌صورت یکدست اضافه شد. **403 عمداً اعلام نشد** چون هیچ authorization مبتنی بر نقش هنوز وجود ندارد و اعلامش گمانه‌زنی می‌بود.

**تست:** `qa-tester` **۸۰ تست جدید** نوشت (۶۹ Application + ۱۱ Api)، سپس مهاجرت verb (بالا) ۵ تست دیگر اضافه کرد (`HttpVerbConventionTests`) → مجموع ۲۵۵/۲۵۵.

**پاس `/code-review` نهایی (۲۰۲۶-۰۸-۲۰، قبل از commit، به درخواست صریح کاربر):** ۲ باگ واقعی + ۱ شکاف اعتبارسنجی پیدا شد و همان‌جا رفع شد:
- **باگ در تست محافظ verb:** `HttpVerbConventionTests.AllControllerActions()` به‌جای reflect کردن روی assembly واقعی `Accounting.Api`، روی `typeof(ControllerBase).Assembly` (یعنی خودِ فریم‌ورک ASP.NET Core) reflect می‌کرد — یعنی این تست **هرگز** Controllerهای این پروژه را بررسی نمی‌کرد و اگر کسی بعداً `[HttpPut]`/`[HttpDelete]` اضافه می‌کرد، بازهم سبز می‌ماند. اصلاح شد به `typeof(AccountCodesController).Assembly`.
- **تناقض مستندسازی:** این فایل (خط مربوط به رفتار مرزی Delete) هنوز می‌گفت idempotent-delete «۲۰۴» برمی‌گرداند، درحالی‌که تصمیم نهایی (بالا) و کد واقعی «۲۰۰ + بدنه» است. اصلاح شد.
- **شکاف اعتبارسنجی خودارجاعی:** نه `UpdateAccountCodeCommandValidator` و نه `UpdateVoucherHeadCommandValidator` بررسی نمی‌کردند که `ParentId`/`ParentHeadId` برابر `Id` خودِ رکورد باشد. چون Update برخلاف Create می‌تواند `Id` را از قبل بداند (از route می‌آید)، این مسیر یک گرهٔ خودارجاعی (حلقهٔ بی‌نهایت در سلسله‌مراتب) می‌ساخت که Create اصلاً نمی‌توانست تولید کند. یک قانون `Must(x => x.ParentId != x.Id)` به هر دو Validator اضافه شد + ۴ تست جدید (۲ به‌ازای هر Entity).
- **یافتهٔ چهارم (IDOR روی نوشتن/حذف) دوباره تأیید شد ولی عمداً حل نشد** — طبق تصمیم صریح کاربر، مثل بقیهٔ ریسک‌های امنیتی این فاز، فقط مستند می‌ماند (رجوع به «تصمیمات باز» پایین‌تر).

مجموع نهایی بعد از این سه اصلاح: **۲۵۹/۲۵۹ سبز** (۲۲ Domain + ۱۵۷ Application + ۶۰ Api + ۲۰ Infrastructure)، **صفر رگرسیون**. build ۰ خطا / ۱۶ warning پیش‌موجود NU1903 (هیچ CS). همگی Unit/Mock — **هیچ اتصالی به Oracle زنده**.

### فاز ۷ — Authentication/Authorization + رفع جعل‌پذیری Audit (۲۰۲۶-۰۸-۱۹، برنچ `changejWT`، commit `86e6526`، push شده)

هر دو ریسک 🔴 CRITICAL فاز ۶ حل شدند. طراحی توسط `security-reviewer`، پیاده‌سازی توسط `backend-dotnet`، تأیید توسط `qa-tester`.

#### ۱. جعل‌پذیری `ADDUSERID` — حل شد ✅

- پارامتر `AddUserId` **کاملاً حذف شد** از `CreateAccountCodeCommand` و `CreateVoucherHeadCommand` (و از Validatorهایشان). یعنی دیگر فقط «بی‌اعتماد» نیست — اصلاً بخشی از قرارداد ورودی نیست.
- هر دو Handler حالا `ADDUSERID = _currentUser.UserId` می‌نویسند؛ دقیقاً مثل `CREATEDDATE`/`ISDELETED` که از قبل درست سمت سرور ست می‌شدند.
- **این تنها تضمین ✅ باقی‌مانده در جدول Accounting Safety Gate (یعنی Audit Trail) بود که عملاً باطل شده بود؛ حالا واقعاً برقرار است.**
- ⚠️ **Breaking change در قرارداد API:** فیلد `addUserId` از بدنهٔ هر دو POST حذف شد. چون هنوز هیچ مصرف‌کنندهٔ خارجی وجود ندارد، پذیرفته شد.
- سمت **خواندن** (`AccountCodeDto`/`VoucherHeadDto`) عمداً `AddUserId` را همچنان برمی‌گرداند — آن جهت خواندن است و خارج از دامنهٔ این تغییر.

#### ۲. `ICurrentUser`

- قرارداد در `Accounting.Application/Common/Interfaces/ICurrentUser.cs` (طبق الگوی موجود `IUnitOfWork` — نه در Domain، تا `Accounting.Domain` صفر وابستگی بماند). پیاده‌سازی `HttpContextCurrentUser` در `Accounting.Api/Security/` بر پایهٔ `IHttpContextAccessor` و Claimها.
- اعضا: `IsAuthenticated`, `UserId`, `VahedCode`, `IsInRole(role)`.
- **`UserId` عمداً پرتاب می‌کند (throw) و هرگز truncate نمی‌کند** اگر کاربر احراز نشده باشد، Claim `NameIdentifier` نباشد، یا مقدار بیش از ۱۰ کاراکتر باشد (عرض ستون Legacy `ADDUSERID`). فلسفه: نوشتن یک هویت بریده‌شده در ستون Audit یک سیستم حسابداری، بدتر از خطای بلند است. (همان منطق تصمیم `ParseExact` در `GuidToChar36Converter`.)
- `VahedCode` صرفاً **در دسترس** قرار گرفت ولی **به هیچ فیلتر Query وصل نشد** — عمداً؛ رجوع به تصمیم باز پایین.

#### ۳. اسکیم احراز هویت **ورودی (inbound)**: IDP واقعی سازمان ✅

> **تاریخچه:** ابتدا (۲۰۲۶-۰۸-۱۹، صبح) یک JWT محلی‌امضا به‌عنوان راه‌حل موقت پیاده شد (گزینهٔ B از `security-reviewer`). سپس **در همان روز** صاحب پروژه پکیج رسمی سازمان را در اختیار گذاشت و مسیر محلی **کاملاً حذف و جایگزین شد**. متن زیر وضعیت نهایی است.
>
> **جزئیات آن راه‌حل موقتِ حذف‌شده** (فقط برای سابقه — دیگر در کد نیست): JWT Bearer محلی‌امضا با **fail-fast در startup** اگر کلید غایب یا کوتاه‌تر از ۳۲ بایت بود؛ یک `POST /api/dev-auth/token` که **خارج از Development مقدار 404 می‌داد** و کاربران مجاز را از یک allowlist در config می‌خواند (نه از `TB_PERSON_ACTION`). در آن نقطه شمارش تست **۱۷۱/۱۷۱** بود؛ پس از حذف `DevAuthController` و تست‌هایش به ۱۷۰ رسید. هم‌زمان **۵ سؤال باز** برای صاحب پروژه ثبت شده بود (token endpoint یا discovery URL؟ audience ورودی؟ کدام claim ≤۱۰ کاراکتر است؟ آیا inbound اصلاً باید به این IDP وصل شود؟ آیا `resource` لازم است؟) — طبق توصیهٔ `security-reviewer` هیچ‌کدام حدس زده نشد، چون یکی‌گرفتن audience خروجی و ورودی یعنی پذیرفتن توکن‌هایی که برای سرویس دیگری صادر شده‌اند. چهار سؤال اول با آمدن پکیج رسمی منتفی شدند؛ سؤال `resource` هنوز باز است (بخش ۵ پایین‌تر).

- پکیج `Tamin.Framework.Common.Security` **1.0.9** از feed داخلی سازمان (`https://nexus.tamin.ir/repository/nuget-v2-group/`، تعریف‌شده در `backend/NuGet.Config`).
- فراخوانی: `builder.Services.AddTaminJWTToken(validAudience, environment)` در `Program.cs`.
  - `validAudience` از کلید config `Tamin:Idp:Audience` خوانده می‌شود و در نبودش به مقدار پیش‌فرض برمی‌گردد. ⚠️ **مقدار فعلی به‌صراحت درخواست صاحب پروژه با پروژهٔ `Financial_Account` مشترک است** تا وقتی audience اختصاصی این سرویس در IDP سازمان ثبت شود.
  - `environment` مشروط است: `IsProduction() ? Environments.Production : Environments.Test` (این enum فقط همین دو مقدار را دارد).
- **رفتار واقعی پکیج که با reflection و اجرای واقعی تأیید شد (نه حدس):**
  - اسکیم استاندارد `Bearer` را با `JwtBearerHandler` ثبت می‌کند و `DefaultScheme` را هم ست می‌کند → `SetFallbackPolicy(RequireAuthenticatedUser)` واقعاً scheme ای برای authenticate/challenge دارد. **نباید `AddAuthentication(...)` دوم اضافه شود** چون با اسکیم پکیج رقابت می‌کند.
  - `ValidIssuer = http://idm.tamin.ir`، `IssuerSigningKey` یک **`JsonWebKey` ثابتِ جاسازی‌شده** است — **هیچ Authority/discovery/JWKS fetch ای در startup یا per-request انجام نمی‌شود**.
  - `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime`/`ValidateIssuerSigningKey` همگی `true`.
  - `NameClaimType = .../identity/claims/name` و `RoleClaimType = .../identity/claims/role`.
- **حذف‌شده (فیزیکی، طبق قانون پروژه — نه `[Obsolete]`):** `DevAuthController` + تست‌هایش، `JwtOptions`، `DevAuthOptions`، و بخش‌های `Jwt`/`DevAuth` از `appsettings.json`. دیگر هیچ توکن محلی‌امضایی در پروژه صادر نمی‌شود.
- **`RolesAllowedAttribute` و `ClaimRequirementFilter`** هم در همین پکیج هستند و برای فاز بعدی (authorization سطح نقش/رکورد — تصمیم باز IDOR) در دسترس‌اند.

- **یافتهٔ تعیین‌کننده:** در **هیچ‌کدام از ۶۵ جدول Legacy هیچ ستون password/hash/salt/token/credential وجود ندارد** → **schema Legacy اصلاً قادر به احراز هویت کسی نیست.** پس تکیه بر Legacy برای authentication ممکن نبود و تکیه بر IDP بیرونی سازمان اجتناب‌ناپذیر بود.
  - تنها کاندیدای نزدیک، `TB_PERSON_ACTION` بود (تنها جدول با `USERID` + `OPERATORROLE` + `VAHEDCODE`) — ولی بررسی نشان داد یک **registry مجوز/اپراتور** است، نه credential store؛ و چون بررسی Read-Only زندهٔ آن ناتمام ماند، عمداً به آن اعتماد **نشد**.

#### ۴. Authorization و خطاها

- `AddAuthorizationBuilder().SetFallbackPolicy(RequireAuthenticatedUser)` — عمداً به‌جای `[Authorize]` روی تک‌تک Controllerها، تا Controller بعدی که کسی یادش برود، دوباره دیتابیس را باز نکند.
- **`app.UseAuthentication()` اصلاً در pipeline وجود نداشت** (خلأیی که `security-reviewer` کشف کرد) — اضافه شد، **قبل از** `app.UseAuthorization()`. بدون آن `HttpContext.User` هرگز پر نمی‌شد و policy بی‌صدا بی‌اثر می‌ماند.
- تنها استثنا: `HealthController` با `[AllowAnonymous]` (probeها نمی‌توانند bearer token حمل کنند).
- 401/403 توسط middleware تولید می‌شوند و **هرگز به `GlobalExceptionHandler` نمی‌رسند** (استثنا نیستند)؛ پس صریحاً به شکل `application/problem+json` با `traceId` و پیام عمومی درآمدند. جزئیات خطای اعتبارسنجی توکن فقط در Development برمی‌گردد.
- ⚠️ **این shaping با `Configure<JwtBearerOptions>` و به‌صورت زنجیره‌ای (chain) انجام می‌شود، نه جایگزینی.** دلیل: `AddTaminJWTToken` خودش `OnMessageReceived` و `OnAuthenticationFailed` را wire می‌کند؛ اگر `options.Events = new JwtBearerEvents{...}` می‌نوشتیم، **کل آن‌ها بی‌صدا نابود می‌شدند**. پس delegate اصلی اول `await` می‌شود و فقط اگر `!Response.HasStarted && !context.Handled` بود، ProblemDetails ما نوشته می‌شود. `OnAuthenticationFailed`/`OnTokenValidated`/`OnMessageReceived` کاملاً دست‌نخورده‌اند.
  - ⚠️ **تصحیح یک برداشت اولیه (ثبت‌شده تا دوباره تکرار نشود):** ابتدا گزارش شد «هر پنج delegate توسط پکیج ست شده‌اند» — این **مثبت کاذب** بود، چون پراپرتی‌های `JwtBearerEvents` در ASP.NET Core به‌صورت پیش‌فرض non-null اند. در عمل فقط `OnMessageReceived` و `OnAuthenticationFailed` واقعاً assign می‌شوند؛ `OnChallenge`/`OnForbidden` روی no-op پیش‌فرض می‌مانند. با این حال chain کردن باز هم الگوی درست و forward-safe است (اگر نسخهٔ بعدی پکیج آن دو را هم wire کند، کد ما نمی‌شکند).

#### ۵. `TokenManager` — مسیر **خروجی (outbound)** به IDP واقعی سازمان

صاحب پروژه الگوی `IDP.Services.TokenManager` را از پروژهٔ اصلی خودش (همان سازمان `tamin.org`) فرستاد و خواست «مثل همین» ساخته شود. پورت شد به `Accounting.Infrastructure/Idp/` با namespace `Accounting.Infrastructure.Idp`.

**نکتهٔ کلیدی معماری که باید بدانید:** این `TokenManager` یک **acquirer از نوع outbound `client_credentials`** است — یعنی این API خودش به‌عنوان *client* از IDP توکن می‌گیرد تا به *سرویس‌های دیگر* سازمان زنگ بزند. **این به‌تنهایی CRITICAL #1 (محافظت از Controllerهای خودمان در برابر فراخوان‌های ورودی) را حل نمی‌کند** — آن مسیر جداگانه‌ای است (بخش ۳ بالا).

- `ITokenManager` عمداً **داخلی به Infrastructure** ماند و به `Accounting.Application/Common/Interfaces/` اضافه **نشد**. دلیل: هیچ use case ای در Application امروز مصرف‌کنندهٔ آن نیست؛ وقتی شد، باید به یک interface هدف‌محور (مثلاً `IPersonDirectoryClient`) وابسته شود نه به این plumbing سطح‌پایین. (اجتناب از premature abstraction.)
- **۶ نقص امنیتی/همزمانی الگوی اصلی حین پورت اصلاح شد** (نه کورکورانه کپی):
  1. `HttpRequestException` بلعیده‌شده + `Console.WriteLine` → `ILogger` ساختاریافته و **انتشار خطا** با `TokenAcquisitionException`. دیگر هرگز توکن کهنه/`null` را بی‌صدا برنمی‌گرداند.
  2. `new HttpClient()` در هر refresh (با وجود تزریق بلااستفادهٔ `IHttpClientFactory`) → استفادهٔ واقعی از factory با named client (رفع socket exhaustion / کهنگی DNS).
  3. `static SemaphoreSlim` روی یک singleton → فیلد نمونه‌ای.
  4. `Dictionary` با نوشتن بدون محافظت از مسیر خواندن → `ConcurrentDictionary` (باگ واقعی خرابی متناوب زیر بار، نه آرایشی).
  5. `DateTime.Now` → `DateTime.UtcNow` در همهٔ محاسبات انقضا (drift منطقه‌زمانی/DST می‌توانست توکن منقضی را معتبر بشمارد).
  6. افشای راز در لاگ → هیچ لاگ/پیام استثنا/`ToString` هرگز `ClientSecret` یا `access_token` خام را شامل نمی‌شود (اعتبارسنجی فقط **نام** پراپرتی‌های ناقص را فهرست می‌کند).
- **نقص ۷ عمداً اصلاح نشد:** `Resources`/`External_Resources` پیکربندی می‌شود ولی در درخواست توکن **ارسال نمی‌شود** — دقیقاً مثل الگوی اصلی. حدس زده نشد؛ سؤالش برای صاحب پروژه باز است (پایین).
- **اعتبارسنجی config تنبل (lazy) است، نه در startup** — برخلاف `Jwt:SigningKey` که fail-fast است. دلیل: هنوز هیچ‌چیز در این کدبیس به سرویس دیگری زنگ نمی‌زند، پس نبودِ `Idp:*` نباید بالاآمدن API را برای کار محلی خراب کند. خطای روشن در اولین `GetAccessTokenAsync` پرتاب می‌شود.
- config با همان الگوی placeholder خالی در `appsettings.json` (`Idp:tamin:*`)؛ مقادیر واقعی فقط در User Secrets.

تست: **۱۷۰/۱۷۰ سبز** (۲۲ Domain + ۸۴ Application + ۴۴ Api + ۲۰ Infrastructure) — `qa-tester` مستقلاً همین عدد را تأیید کرد. build ۰ خطا، بدون هیچ warning نوع CS (۱۶ warning پیش‌موجود NU1903 دست‌نخورده، بدون NU1902 — نسخهٔ transitive `Microsoft.IdentityModel.*` توسط resolver NuGet به ۸.۱۴.۰/۸.۰.۱ ارتقا یافت، فراتر از بازهٔ آسیب‌پذیر). Application از ۸۵ به ۸۴ رسید (حذف تست Validator مربوط به `AddUserId`)، Api از ۲۴ به ۴۴ (تست‌های `HttpContextCurrentUser`، `TaminJwtWiringTests` و مسیر Audit؛ `DevAuthController` و تست‌هایش پس از سوییچ به IDP واقعی فیزیکاً حذف شدند). `TaminJwtWiringTests` **۵ تست** است و عمداً رفتار یک پکیج شخص‌ثالث را قفل می‌کند: ثبت اسکیم `Bearer`؛ **resolve شدن default authenticate/challenge به `Bearer`** (مهم‌ترینشان — کل کارکرد `FallbackPolicy` به آن وابسته است و یک ارتقای پکیج می‌تواند بی‌صدا خرابش کند)؛ اعتبارسنجی واقعی audience پیکربندی‌شده؛ و روشن‌بودن `ValidateIssuer`/`ValidateLifetime`/`ValidateIssuerSigningKey`، و پروژهٔ جدید `Accounting.Infrastructure.Tests` با ۲۰ تست (cache/انقضا/انتشار خطا/عدم افشای راز، همگی با `HttpMessageHandler` mock — **هیچ فراخوان شبکه‌ای واقعی به IDP انجام نشد**). همهٔ تست‌ها همچنان **Unit/Mock**؛ هیچ تست integration روی Oracle یا IDP واقعی اجرا نشد.

### فاز ۶ — لایهٔ HTTP (۲۰۲۶-۰۸-۱۸، برنچ `GetAccountCode`، commit نشده)

اولین سطح HTTP واقعی پروژه. ۶ Endpoint روی ۲ Controller:

| Method | Route | موفق | خطا |
|---|---|---|---|
| POST | `/api/account-codes` | 201 + `Location` + `{id}` | 400 / 409 / 500 |
| GET | `/api/account-codes?pageNumber=&pageSize=` | 200 `PagedResult<AccountCodeDto>` | 400 / 500 |
| GET | `/api/account-codes/{id:guid}` | 200 `AccountCodeDto` | 400 / 404 / 500 |
| POST | `/api/voucher-heads` | 201 + `Location` + `{id}` | 400 / 409 / 500 |
| GET | `/api/voucher-heads?pageNumber=&pageSize=&year=&vahedCode=` | 200 `PagedResult<VoucherHeadDto>` | 400 / 500 |
| GET | `/api/voucher-heads/{id:guid}` | 200 `VoucherHeadDto` | 400 / 404 / 500 |

paging: پیش‌فرض `pageNumber=1`, `pageSize=20`؛ سقف `MaxPageSize=200` و `MaxPageNumber=int.MaxValue/200` (برای جلوگیری از overflow در `Skip`). این سقف در Validator است و از طریق `ValidationBehavior` قبل از رسیدن به DB اعمال می‌شود.

> **تأیید شد `ValidationBehavior` روی Query هم فعال است** (نه فقط Command) — چون generic آن روی `IRequest<TResponse>` بسته شده، نه روی یک نشانگر مخصوص Command. این همان چیزی است که باعث می‌شود سقف paging بالا واقعاً اعمال شود.

تصمیم‌های تثبیت‌شده:
- **Controller کاملاً نازک است** — فقط `_mediator.Send(...)` و تنها انشعاب مجاز `null → 404`. هیچ business logic ای در Controller نیست.
- **نگاشت خطا مرکزی است** (`Accounting.Api/GlobalExceptionHandler.cs` با `IExceptionHandler` + `AddProblemDetails`)؛ هیچ Controller ای `try/catch` ندارد.
- **✅ نگاشت خطای UNIQUE حل شد:** `UnitOfWork.SaveChangesAsync` تنها جایی است که `OracleException` را می‌شناسد؛ ORA-00001 را به `DuplicateKeyException` (سطح Application) ترجمه می‌کند و `GlobalExceptionHandler` آن را به **409 Conflict** با پیام عمومی نگاشت می‌کند. **`Accounting.Api` هیچ ارجاعی به تایپ‌های Oracle ندارد** — فقط در XML doc نام Oracle آمده.
  - الگوی دقیقی که match می‌شود `DbUpdateException { InnerException: OracleException { Number: 1 } }` است — یعنی استثنای اوراکل همیشه از داخل wrapper خودِ EF Core بیرون کشیده می‌شود، نه مستقیم.
  - **با تست قفل شد که این نگاشت باریک است:** یک `ORA-00904` (ستون نامعتبر) عیناً rethrow می‌شود و به ۴۰۹ تبدیل نمی‌شود — یعنی خطاهای غیرِتکراری بی‌صدا بلعیده یا بدنگاشت نمی‌شوند.
- **هیچ متن خام Oracle/SQL/stack trace در بدنهٔ پاسخ درز نمی‌کند** (با تست صریح روی JSON سریال‌شده اثبات شد، در هر دو محیط Development و Production یکسان).
- XML doc پروژهٔ Api حالا وارد Swagger می‌شود (`GenerateDocumentationFile` + `IncludeXmlComments`).

تست: پروژهٔ جدید `backend/tests/Accounting.Api.Tests` با **۲۴ تست**. مجموع پروژه: **۱۳۱/۱۳۱ سبز** (۲۲ Domain + ۸۵ Application + ۲۴ Api). build ۰ خطا / ۱۶ warning پیش‌موجود NU1903.

### فاز ۵ — اولین مسیر نوشتن CQRS (۲۰۲۶-۰۸-۱۸، برنچ `addAccountCode`، commit نشده)

الگوی پایه‌ای که همهٔ Commandهای بعدی از روی آن ساخته می‌شوند:

`Command (فقط primitive) → ValidationBehavior (FluentValidation) → Handler (ساخت Entity + تولید Guid) → Repository (فقط stage) → IUnitOfWork.SaveChangesAsync (یک‌بار، توسط Handler)`

تصمیم‌های تثبیت‌شده:
- **محل Interfaceها: `Accounting.Application/Common/Interfaces/`** (نه Domain) — چون `Accounting.Domain` باید صفر وابستگی بماند. Implementation در `Accounting.Infrastructure`.
- **`IUnitOfWork` عمداً باریک و entity-agnostic است** — فقط `SaveChangesAsync` + `Begin/Commit/RollbackTransactionAsync`. هیچ پراپرتی per-entity ندارد. افزودن Entity بعدی **هیچ تغییری** در `IUnitOfWork`/`UnitOfWork` لازم ندارد؛ Repositoryها مستقیماً به Handler تزریق می‌شوند.
- **مرز تراکنش در Handler است** — Repository فقط `DbSet.AddAsync` می‌زند و هرگز `SaveChangesAsync` صدا نمی‌زند.
- **Command هرگز Entity نیست** — Command فقط primitive دارد و Handler خودش Entity می‌سازد.
- **ID سمت Application تولید می‌شود** (`Guid.NewGuid()`)؛ تأیید شد که `ID` هیچ‌کدام از دو جدول DB-generated نیست.
- **اعتبارسنجی عمداً فقط سطحی است** (NotEmpty/MaximumLength منطبق با Fluent Mapping) — طبق تصمیم «Legacy جایگزین کامل»، هیچ invariant حسابداری بازسازی نشد.

پکیج‌های جدید در `Accounting.Application`: `MediatR` 14.2.0، `FluentValidation` 12.1.1، `FluentValidation.DependencyInjectionExtensions` 12.1.1. در تست: `Moq` 4.20.72.

تست: پروژهٔ جدید `backend/tests/Accounting.Application.Tests` با **۴۱ تست** (شامل تأیید صریح ترتیب `AddAsync` قبل از `SaveChangesAsync`). مجموع پروژه: **۶۳/۶۳ سبز** (۲۲ Domain + ۴۱ Application). build ۰ خطا / ۱۶ warning پیش‌موجود NU1903.


---

## آرشیو verbatim — چک‌لیست تفصیلی پیشین «وضعیت فعلی پروژه»

⚠️ تا ۲۰۲۶-۰۸-۲۸ بخش «وضعیت فعلی پروژه» در `CLAUDE.md` هر آیتم را با جزئیات کامل نگه می‌داشت. در بازآرایی آن روز، چک‌لیست `CLAUDE.md` به یک/دوخطی کوتاه شد تا هزینهٔ توکن هر سشن پایین بیاید. متن کامل و اصلیِ آن چک‌لیست عیناً اینجا حفظ شده — **به‌ویژه به‌خاطر آیتم‌های پیش از فاز ۵** (راه‌اندازی solution، Reverse Engineering، اجرای Legacy-as-Domain، تبدیل `string`→`Guid`، اتصال Oracle، Query side) که هیچ‌وقت شمارهٔ فاز نگرفتند و جزئیاتشان جای دیگری ثبت نشده است.

ارجاع‌های «فاز X پایین‌تر» در متن زیر به فازهای بالای همین فایل اشاره دارند.

- [x] راه‌اندازی اولیه solution و پروژه‌های .NET — `backend/Accounting.sln` با ۴ پروژه روی net10.0؛ رفرنس‌ها طبق Clean Architecture (Api → Application+Infrastructure، Infrastructure → Application، Application → Domain، Domain بدون وابستگی)؛ Swagger با Swashbuckle.AspNetCore روی `/swagger` فعال و توسط qa-tester تأیید شد. فعلاً فقط اسکلت + `HealthController` است و هیچ کد دامنه‌ای نوشته نشده.
- [ ] راه‌اندازی اولیه React (Vite) — طبق تصمیم فعلی، فرانت‌اند تا اطلاع ثانوی متوقف است؛ تمرکز روی backend/.
- [x] ~~طراحی نهایی مدل دامنه کدینگ شناور~~ — **حذف شد (۲۰۲۶-۰۸-۱۷)**. rich domain model ساخته و تست شد، سپس به تصمیم دوم کاربر کنار گذاشته و در نهایت **فیزیکاً حذف شد** (۲۲ فایل). در تاریخچهٔ git تا commit `9f760ad` قابل بازیابی است.
- [x] Reverse Engineering دیتابیس Legacy Oracle (schema `CENTRALACCOUNT`) — کشف Read-Only کامل schema (۶۵ جدول، ۷۷۴ ستون، ۸۲ FK، ۲۹ UNIQUE، ۱ sequence، ۲۸ View) و Scaffold همهٔ ۶۵ جدول به Entity + Fluent Mapping. **فقط Entity و Mapping** — هیچ Business Logic یا Repository.
- [x] اجرای تصمیم معماری Legacy-as-Domain (۲۰۲۶-۰۸-۱۷) — هر ۶۵ کلاس Entity از `Accounting.Infrastructure/Legacy/Entities/` به `Accounting.Domain/Legacy/Entities/` **منتقل** شد (نه کپی) و namespace همه به `Accounting.Domain.Legacy` تغییر کرد. `LegacyDbContext.cs` با همهٔ Fluent Mappingها در `Accounting.Infrastructure/Legacy/` باقی ماند و با `using Accounting.Domain.Legacy;` به Entityهای جدید ارجاع می‌دهد. جهت وابستگی Infrastructure → Domain (مجاز)؛ `Accounting.Domain.csproj` همچنان **صفر** وابستگی خارجی دارد. build با ۰ خطا و ۱۶ warning (همگی NU1903 از قبل موجود، بدون هیچ warning نوع CS) و ۳۳/۳۳ تست سبز. ⚠️ **این مسیر/namespace بعداً در ۲۰۲۶-۰۸-۱۸ تغییر کرد** به `Accounting.Domain/Entity/` و `Accounting.Domain.Entity` (رجوع به «تصمیم معماری سوم»)؛ متن بالا وضعیت همان روز را ثبت می‌کند.
- [x] اجرای تصمیم دوم «Legacy جایگزین کامل» (۲۰۲۶-۰۸-۱۷) — در دو مرحله: اول ۱۲ تایپ با `[Obsolete]` علامت خوردند، سپس **به درخواست صریح کاربر فیزیکاً حذف شدند** (۲۲ فایل شامل ۳ فایل Exception یتیم و ۷ فایل تست). جزئیات کامل در «تصمیم معماری دوم» بالا. اکنون `Accounting.Domain` فقط شامل `Legacy/` (۶۵ Entity) + `ValueObjects/` (۴ تایپ باقی‌مانده) + `Common/Guard.cs` + `Exceptions/` (۳ فایل) است. build ۰ خطا / ۱۶ warning پیش‌موجود (هیچ warning نوع CS)، **۱۲/۱۲ تست سبز**.
- [x] حل ابهام «منبع حقیقت تفصیلی مجاز» — `TB_ACCOUNT_LINK_TAFSILGROUP` منبع حقیقت است (FK `FK_TAFSILGOUP_ACCOUNTCODE` به `TB_ACCOUNTCODE` + UNIQUE `UK_ACCOUNTLINKTAFSILGROUP` روی `ACCOUNT_ID, LEVEL_ID, TAFSILGROUP_ID`). زنجیره: `TB_ACCOUNTCODE → TB_ACCOUNT_LINK_TAFSILGROUP → TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI`.
- [x] تبدیل شناسه‌ها از `string` به `System.Guid` (۲۰۲۶-۰۸-۱۸) — **۱۷۷ پراپرتی ID/FK** در **۶۴** از ۶۵ Entity (۱۱۵ `Guid` + ۶۲ `Guid?`). دامنهٔ تبدیل مکانیکی و قابل‌ممیزی تعیین شد: دقیقاً همان ستون‌هایی که در Fluent Mapping با `HasMaxLength(36) + IsFixedLength()` یعنی Oracle `CHAR(36)` نگاشت شده بودند. `TB_YEAR` هیچ ستون `CHAR(36)` ندارد و دست‌نخورده ماند.
  - **پیش‌شرط: verify روی دادهٔ زنده** (توسط `database-reverse-engineer`، کاملاً Read-Only، بدون DDL/DML): **۱۲۴٬۰۹۴** مقدار غیر-null در همهٔ ۱۷۷ ستون full-scan شد → ۱۰۰٪ منطبق بر `^[0-9a-f]{8}-...` یعنی dashed و **lowercase**؛ صفر مقدار خراب، صفر اختلاف طول/padding. صحت FK هم با join حساس‌به‌حروف تأیید شد (`SYSTEM_TYPE→TB_SYSTYPE` ۵۷/۵۷، `PARENTID→ID` ۱۲۲/۱۲۲).
  - **ستون فیزیکی Oracle تغییر نکرد** — همچنان `CHAR(36)`. تبدیل با `GuidToChar36Converter` (یک `ValueConverter<Guid,string>` مشترک) در `Accounting.Infrastructure/Legacy/` انجام می‌شود و صریحاً روی تک‌تک ۱۷۷ پراپرتی اعمال شده (نه Convention سراسری) تا دامنه‌اش قابل‌ممیزی بماند. `Accounting.Domain` همچنان **صفر** وابستگی خارجی دارد (`Guid` جزو BCL است).
  - ستون‌هایی که پسوند `ID` دارند ولی GUID **نیستند** عمداً `string` ماندند: `ADDUSERID`/`CHANGEUSERID`/`USERID` (طول ۱۰)، `DATE_RSID` (طول ۸)، `CONTROLID` (`NUMBER(1)`)، و کدهای کسب‌وکاری مثل `VAHEDCODE`/`ACCOUNTNUMBER`.
  - نکتهٔ ظریف: `TB_VOUCHERSHEAD.SYSTEM_TYPE` با اینکه پسوند `_ID` ندارد، `CHAR(36)` و FK واقعی به `TB_SYSTYPE` است؛ تبدیل شد (در غیر این صورت مدل EF به‌خاطر عدم تطابق نوع دو سر FK اصلاً build نمی‌شد).
  - build ۰ خطا (۱۶ warning پیش‌موجود NU1903، هیچ warning نوع CS)، **۲۲/۲۲ تست سبز** (۱۲ تست قبلی + ۱۰ تست جدید قرارداد round-trip).
- [x] اتصال به Oracle (مسیر Legacy) و طراحی مدل نوشتن مبتنی بر `Accounting.Domain.Entity` — `LegacyDbContext` با `UseOracle(...)` در DI ثبت شد؛ connection string فقط از `IConfiguration`/User Secrets خوانده می‌شود و در `appsettings.json` تنها یک placeholder خالی هست. ⚠️ اتصال واقعی به دیتابیس هنوز **اجرا/تست نشده** (هیچ تست integration روی Oracle واقعی زده نشد — عمداً، برای جلوگیری از side effect روی دیتابیس Legacy).
- [x] اولین Command روی Entityهای Legacy (الگوی پایه) — `CreateAccountCodeCommand` و `CreateVoucherHeadCommand` طبق ترتیب درخواستی صاحب پروژه (UnitOfWork → Interface → Repository → Command Service). **فقط Add؛ هیچ Query/Update/Delete و هیچ Controller ساخته نشد.** جزئیات در «فاز ۵» پایین‌تر.
- [x] Query side (خواندن) روی `Accounting.Domain.Entity` — چهار Query با paging: `GetAccountCodes`/`GetAccountCodeById`/`GetVoucherHeads`/`GetVoucherHeadById` با `PagedResult<T>` و Read Repositoryهای مجزا.
- [x] Controller/Endpoint برای هر ۶ سرویس (۲ Command + ۴ Query) — `AccountCodesController` و `VoucherHeadsController` + `GlobalExceptionHandler` مرکزی. جزئیات در «فاز ۶» پایین‌تر.
- [x] **فاز ۷ — Authentication/Authorization + رفع جعل‌پذیری `ADDUSERID`** (۲۰۲۶-۰۸-۱۹) — هر دو ریسک 🔴 CRITICAL که `security-reviewer` در Gate فاز ۶ کشف کرده بود حل شدند. اتصال inbound به IDP واقعی سازمان (`Tamin.Framework.Common.Security`) + `FallbackPolicy` + `ICurrentUser`. جزئیات در «فاز ۷» پایین‌تر. **۱۷۰/۱۷۰ تست سبز.**
- [x] **فاز ۸ — تکمیل CRUD: Update + Delete (Soft Delete)** (۲۰۲۶-۰۸-۱۹) — ۴ Command جدید و ۴ Endpoint جدید، همگی `POST` با فعل صریح در route (`PUT`/`DELETE` به‌درخواست صریح صاحب پروژه ممنوع شدند — رجوع به «فاز ۸» پایین‌تر). جزئیات در «فاز ۸» پایین‌تر. **۲۵۹/۲۵۹ تست سبز** (پس از یک پاس `/code-review` که ۲ باگ واقعی و ۱ شکاف اعتبارسنجی پیدا و رفع کرد).
- [x] **فاز ۹ — cascade کامل سه‌سطحی حذف نرم سند** (۲۰۲۶-۰۸-۲۰) — ریسک 🔴 «نبود cascade» از فاز ۸ **کاملاً** بسته شد: `TB_VOUCHERSHEAD` → `TB_VOUCHERSDETAIL` → `TB_VOUCHERDETAIL_LINK_TAFSILI`. جزئیات در «فاز ۹» پایین‌تر. **۲۷۲/۲۷۲ تست سبز.**
- [x] **فاز ۱۰ — CRUD مستقل `TB_VOUCHERSDETAIL` + composite create سند** (۲۰۲۶-۰۸-۲۰) — اجرای تصمیم «مرز Aggregate ترکیبی» صاحب پروژه. ۵ Endpoint جدید روی `VoucherDetailsController` + گسترش `CreateVoucherHeadCommand` برای ثبت هدر و دیتیل‌ها در **یک تراکنش**. جزئیات در «فاز ۱۰» پایین‌تر. **۳۷۹/۳۷۹ تست سبز.**
- [x] **فاز ۱۱ — نگاشت خطای FK + مسیر نوشتن تفصیلی ردیف سند** (۲۰۲۶-۰۸-۲۵) — دو ریسک باز فاز ۱۰ بسته شدند: 🔴 «نقض FK → 500 خام» و 🟡 «هیچ مسیر نوشتنی برای تفصیلی ردیف سند نیست». جزئیات در «فاز ۱۱» پایین‌تر. **۴۱۷/۴۱۷ تست سبز** (پس از یک پاس `/code-review` که ۱ باگ واقعی در sync لینک تفصیلیِ مشترک پیدا و رفع کرد).
- [x] **فاز ۱۲ — تحلیل کسب‌وکار پروژهٔ مرجع `D:\CentralAccount` + اولین اتصال Oracle زنده** (۲۰۲۶-۰۸-۲۵/۲۶) — خواندن Read-Only کامل یک پروژهٔ حسابداری متمرکز واقعی دیگر (همان schema `CENTRALACCOUNT`)، کشف یک الگوی باگ سیستماتیک (`bool?` روی ستون‌های چندمقداری `NUMBER(1)`) و حل سه ابهام با اولین کوئری Read-Only واقعی روی Oracle زنده. جزئیات در «فاز ۱۲» پایین‌تر.
- [x] **فاز ۱۳ — CRUD دستهٔ دوم: ۸ Entity مستقل** (۲۰۲۶-۰۸-۲۷) — `AccountCodeInterface`, `AccountException`, `BillLog`, `PersonAction`, `PreDescrib`, `Rabet`, `WhiteAndBlackList`, `WhiteList`. **۳۹ Endpoint روی ۸ Controller** (۷ تا CRUD کامل + `PreDescrib` فقط CRU). **۸۶۹/۸۶۹ تست سبز** (از ۴۱۷). جزئیات در «فاز ۱۳» پایین‌تر.
- [ ] رفع باگ‌های تأییدشدهٔ `bool?`→enum (`TB_ACCOUNTCODE.TYPECODE`, `TB_VOUCHERSHEAD.DOCLIFE`) — هر دو در مسیر نوشتن فعال‌اند، هنوز اصلاح نشده‌اند. **فاز ۱۳ سه مورد جدید به این فهرست اضافه کرد** (`TB_WHITEANDBLACKLIST.STATE` تأییدشده، `TB_ACCOUNTCODE_INTERFACE.TYPE` و `TB_PERSON_ACTION.OPERATORROLE` مشکوک) — رجوع به «فاز ۱۳». **فاز ۱۴ هفت مورد دیگر اضافه کرد** (`TB_IDENTITYSUBGRPS.SUBGRPS_TYPE` که کامنت خود اوراکل سه‌مقداری بودنش را می‌گوید، `TB_WORKSHOP.ISACTIVE` خواهرِ موردِ خطرناکِ تأییدشدهٔ `TB_TAFSILI.ISACTIVE`، و ۵ مورد دیگر) — رجوع به «فاز ۱۴». **فهرست انباشته حالا ~۱۲ ستون در مسیر نوشتن فعال است.**
- [x] **فاز ۱۴ — CRUD دستهٔ سوم: ۸ Entity مستقل** (۲۰۲۶-۰۸-۲۸) — `AttribForAccountCode`, `LevelTafsil`, `TafsilGroup`, `IdentityGroup`, `IdentitySubGroup`, `ChequeType`, `VahedInfo`, `WorkShop`. **۳۹ Endpoint روی ۸ Controller** (۷ تا CRUD کامل + `VahedInfo` فقط CRU). **۱۳۱۷/۱۳۱۷ تست سبز** (از ۸۶۹؛ پس از یک پاس `/code-review` ۸-Agent که ۱ باگ واقعی در `IdentitySubGroup.SubgrpsLen` پیدا و رفع کرد). جزئیات در «فاز ۱۴» پایین‌تر.
- [ ] فرم صدور سند با تفصیلی داینامیک
