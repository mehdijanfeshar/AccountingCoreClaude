# ورود با Keycloak (برنچ `keycloak`)

سامانهٔ ورود قابل انتخاب است؛ پیش‌فرض همان سامانهٔ ورود سازمان (account-pilot) است و تا تنظیمات عوض نشود هیچ رفتاری تغییر نمی‌کند.

| | سامانهٔ ورود سازمان (پیش‌فرض) | Keycloak |
|---|---|---|
| بک‌اند | `Auth:Provider = Tamin` | `Auth:Provider = Keycloak` + بخش `Keycloak` |
| فرانت | `VITE_AUTH_PROVIDER` خالی یا `tamin` | `VITE_AUTH_PROVIDER=keycloak` + `VITE_KEYCLOAK_AUTHORITY` |

## Keycloak محلی (توسعه)

```
docker compose -f deploy/keycloak/docker-compose.yml up -d
```

- کنسول مدیریت: http://localhost:8180/admin (admin / admin) — Realm `accounting`.
- Realm از `deploy/keycloak/realm-accounting.json` فقط در اولین اجرا وارد می‌شود. شروع از صفر: `docker compose -f deploy/keycloak/docker-compose.yml down -v`.
- کاربران نمونه (رمز همه `Pass@123`؛ نام کاربری = کد ملی):

| نام کاربری | نقش | واحد |
|---|---|---|
| 0000000001 | SETAD ADMIN + NATIONAL | 0000 (ستاد مرکزی) |
| 0000000002 | MALI ADMIN | 1155 |
| 0000000003 | USER | 1155 |
| 0000000004 | REPORT | 1155 |
| 0000000005 | بدون نقش (برای آزمون ۴۰۳) | 1155 |

### تنظیمات توسعه

بک‌اند (`appsettings.Development.json` یا User Secrets):

```json
"Auth": { "Provider": "Keycloak" },
"Keycloak": { "Authority": "http://localhost:8180/realms/accounting", "RequireHttpsMetadata": false }
```

فرانت (`.env.development.local`):

```
VITE_AUTH_PROVIDER=keycloak
VITE_KEYCLOAK_AUTHORITY=http://localhost:8180/realms/accounting
VITE_KEYCLOAK_CLIENT_ID=accounting-ui
```

## طراحی

- **Clientها:** `accounting-ui` (عمومی، Authorization Code + PKCE، redirect به ریشهٔ برنامه) و `accounting-api` (bearer-only). Mapper «audience» روی `accounting-ui` مقدار `accounting-api` را در `aud` توکن می‌گذارد؛ بک‌اند همین را کنترل می‌کند.
- **نقش‌ها:** Client Roleهای `accounting-api` با همان نام‌های `FINANCIAL CORE …` (`AppRoles`). در توکن: `resource_access.accounting-api.roles`.
- **کد واحد:** ویژگی کاربر `vahed_code` (در User Profile تعریف شده، ۴ رقم) با Mapper به claim `vahed_code`.
- **شناسهٔ کاربر:** `preferred_username` (کد ملی) — ستون‌های `ADDUSERID`/`CHANGEUSERID` حداکثر ۱۰ نویسه‌اند و `sub` ِ Keycloak یک GUID ۳۶ نویسه‌ای است.
- **بک‌اند:** `KeycloakAuthentication.AddKeycloakJwt` (JWT Bearer با discovery/JWKS خود Keycloak، روی همان طرح Bearer) و `KeycloakClaimsTransformation` که توکن را به claimهای فعلی برنامه نگاشت می‌کند (`NameIdentifier`، `urn:tamin:jwt:claim:org`، `Role`) — پس `HttpContextCurrentUser`، `RoleAuthorizationBehavior` و بقیه دست نخوردند. `PortalRoleClaimsMiddleware` در حالت Keycloak اجرا نمی‌شود. نام claimها در بخش `Keycloak` قابل تنظیم‌اند.
- **فرانت:** `src/lib/auth/keycloak.ts` با `oidc-client-ts` (تمدید خودکار با refresh token، نشست OIDC در sessionStorage)؛ `startLogin`/`startLogout`/`bootstrapAuth` در حالت Keycloak به آن واگذار می‌کنند و access token مثل قبل در `tokenStore` می‌نشیند.
- `directAccessGrantsEnabled` روی `accounting-ui` فقط برای آزمون توسعه (گرفتن توکن با curl) روشن است؛ در Keycloak واقعی خاموش شود.

## آزمون از ماشین‌های دیگر شبکه (آدرس IP)

- `deploy/keycloak/.env` (در گیت نمی‌رود): `KC_HOSTNAME=http://<IP ماشین>:8180` — Keycloak همیشه با همین آدرس توکن می‌دهد (`iss`)، پس `Keycloak:Authority` بک‌اند و `VITE_KEYCLOAK_AUTHORITY` فرانت هم باید همین IP باشند و همه (حتی روی خود ماشین) با `http://<IP>:4200` وارد شوند، نه `localhost`.
- آدرس `http://<IP>:4200/*` باید در Redirect URIs و Web Origins کلاینت `accounting-ui` باشد.
- روی آدرس http غیر از localhost مرورگر `crypto.subtle` نمی‌دهد ⇒ فرانت بدون PKCE وارد می‌شود (`disablePKCE`) و کلاینت توسعه PKCE را اجباری نکرده است. **محیط واقعی: HTTPS و PKCE اجباری (`pkce.code.challenge.method = S256`).**
- فایروال ویندوز باید پورت 8180 را برای ورودی باز بگذارد.
- IIS (`D:\AiProj\Publish\appsettings.Development.json`): `Auth:Provider = Keycloak` و `Keycloak:Authority = http://<IP>:8180/realms/accounting`، `RequireHttpsMetadata = false`. برگشت: `Provider = Tamin`.
