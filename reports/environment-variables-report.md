# Звіт до завдання «Environment Variables» (Модуль 3)

**Проєкт:** FlowerShopApi  
**Технології:** ASP.NET Core (.NET 10), Entity Framework Core, MySQL, Docker, PowerShell  
**Тема:** Керування конфігурацією застосунку за допомогою змінних середовища

## 1. Мета роботи

Навчитися відокремлювати конфігурацію та чутливі дані від вихідного коду, використовувати **Environment Variables** і **.NET User Secrets**, підготувати безпечний шаблон конфігурації та перевірити запуск API з відповідними налаштуваннями.

## 2. Початковий стан проєкту

FlowerShopApi використовує JWT-автентифікацію та базу даних MySQL. Під час попереднього завдання **API Security** чутливі параметри (секретний ключ JWT і рядок підключення до MySQL) були перенесені з `appsettings.json` до **.NET User Secrets** для локальної розробки.

У `Program.cs` значення конфігурації отримуються за ключами, а не прописуються безпосередньо в коді:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var jwtKey = builder.Configuration["Jwt:Key"];
```

Таким чином, застосунок може отримувати значення з різних джерел конфігурації без зміни вихідного коду.

## 3. Реалізація

### 3.1. .NET User Secrets

Під час локальної розробки для чутливих значень використано User Secrets. Їх можна перевірити командою:

```powershell
dotnet user-secrets list --project FlowerShopApi/FlowerShopApi.csproj
```

User Secrets зберігаються поза каталогом репозиторію. Вони призначені для локальної розробки, а не як сховище секретів для промислового середовища.

### 3.2. Environment Variables

Для перевірки запуску без профілю `launchSettings.json` було використано змінні середовища PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:5239"
$env:ConnectionStrings__DefaultConnection = Read-Host "Введи connection string MySQL"
$env:Jwt__Key = Read-Host "Введи JWT key"
```

У конфігурації ASP.NET Core подвійне підкреслення `__` відповідає роздільнику `:`. Наприклад:

| Змінна середовища | Ключ конфігурації |
|---|---|
| `Jwt__Key` | `Jwt:Key` |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` |
| `ASPNETCORE_ENVIRONMENT` | Назва середовища запуску |
| `ASPNETCORE_URLS` | Адреса прослуховування API |

Секрети не виводилися у звіт і не повинні додаватися до Git.

### 3.3. Шаблон `.env.example` та `.gitignore`

Для документації необхідних параметрів передбачено файл `.env.example`, у якому використовуються лише приклади або заповнювачі замість реальних секретів:

```dotenv
ConnectionStrings__DefaultConnection=Server=localhost;Port=3307;Database=rosemary_shop;User=root;Password=YOUR_PASSWORD;
Jwt__Key=REPLACE_WITH_YOUR_SECURE_JWT_KEY
Jwt__Issuer=YOUR_JWT_ISSUER
Jwt__Audience=YOUR_JWT_AUDIENCE
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5239
```

У `.gitignore` мають бути правила:

```gitignore
.env
.env.*
!.env.example
```

Вони призначені для виключення локальних `.env`-файлів із Git і дозволяють зберігати приклад `.env.example`.

> **Примітка:** сам файл `.env` не завантажується автоматично командою `dotnet run`; у проведеній перевірці значення задавалися як змінні середовища безпосередньо у PowerShell.

## 4. Перевірка працездатності

### 4.1. Запуск застосунку

API запускали командою:

```powershell
dotnet run --no-launch-profile --project FlowerShopApi/FlowerShopApi.csproj
```

У логах було зафіксовано:

```text
Now listening on: http://localhost:5239
Hosting environment: Production
```

Це підтверджує запуск застосунку без профілю запуску та використання середовища `Production`.

### 4.2. Перевірка доступності MySQL

Спочатку запит `GET /api/products` повертав **HTTP 500**, оскільки контейнер MySQL у Docker був вимкнений. У логах повідомлялося:

```text
MySqlConnector.MySqlException:
Unable to connect to any of the specified MySQL hosts.
```

Після запуску контейнера MySQL запит було повторено:

```powershell
Invoke-WebRequest -Uri "http://localhost:5239/api/products" -UseBasicParsing
```

**Фактичний результат:**

```text
StatusCode        : 200
StatusDescription : OK
Content-Type      : application/json; charset=utf-8
```

У відповіді отримано JSON-масив товарів із бази даних, зокрема товар «Букет Марія». Це підтверджує працездатність API та доступність MySQL у перевіреній конфігурації.

### 4.3. Перевірка файлів конфігурації

Для перевірки шаблону та правил Git використовувалися команди:

```powershell
Test-Path .env.example
Get-Content .gitignore
git check-ignore -v --no-index .env
git check-ignore -v --no-index .env.example
git ls-files .env .env.example
git status --short
```

За результатом ручної перевірки користувач повідомив, що налаштування виглядають коректними. Окремі виводи цих команд у звіті не наведено, тому факт відсутності секретів у всій історії Git не вважається незалежно підтвердженим.

## 5. Результати

| Критерій | Результат |
|---|---|
| Отримання параметрів через `builder.Configuration` | Реалізовано |
| Використання .NET User Secrets під час розробки | Реалізовано раніше |
| Запуск API у середовищі `Production` без launch profile | Підтверджено логами |
| Отримання товарів із MySQL | HTTP 200 OK, підтверджено |
| `.env.example` і правила `.gitignore` | Перевірено користувачем; детальні логи не надані |
| Окрема перевірка авторизованого JWT-запиту у `Production` | Не зафіксована |

## 6. Висновок

У FlowerShopApi використовується відокремлена від коду конфігурація: значення для JWT і підключення до MySQL отримуються через стандартну систему конфігурації ASP.NET Core, локальні секрети зберігаються в .NET User Secrets, а для запуску в іншому середовищі можуть передаватися через Environment Variables. Перевірено запуск у `Production` без профілю запуску та успішне отримання списку товарів із MySQL (`HTTP 200 OK`).

Під час перевірки виявлено, що для запитів до бази даних контейнер MySQL має бути запущений. Після його запуску API відновив коректну роботу. Використання шаблону `.env.example` разом із правилами `.gitignore` дозволяє документувати необхідні налаштування без публікації реальних секретів.
