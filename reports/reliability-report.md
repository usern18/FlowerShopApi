# Reliability Testing — FlowerShopApi

## 1. Мета

Реалізувати та перевірити механізми відмовостійкості HTTP-викликів: **Timeout**, **Retry** та **Circuit Breaker**, а також відновлення після тимчасового збою (Recovery).

## 2. Область застосування

Проєкт: **FlowerShopApi**, монолітний REST API на **ASP.NET Core / .NET 10**. Для лабораторної реалізовано окремий HTTP-клієнт `ReliabilityDemo` та демонстраційні ендпоінти `GET /api/reliability/check` і `GET /api/reliability/dependency`, доступні лише у середовищі **Development**. HTTP-клієнт звертається до контрольованої тестової залежності на тому самому локальному API. Механізми застосовано **до демонстраційного HTTP-клієнта**, а не до основних операцій товарів, замовлень, Google-автентифікації чи MySQL.

Бібліотека: `Microsoft.Extensions.Http.Resilience` з Polly. У журналах виконання зафіксовано використання стандартного конвеєра `ReliabilityDemo-standard`. Версія залежності у фактичному відновленні пакетів: **10.1.0**; попередження `NU1603` було пов'язане із запитом недоступної `10.0.1`.

## 3. Налаштування

| Параметр | Значення | Призначення |
|---|---|---|
| `TotalRequestTimeout.Timeout` | 5 с | Загальна межа тривалості запиту з повторними спробами |
| `AttemptTimeout.Timeout` | 1 с | Максимальна тривалість однієї HTTP-спроби |
| `Retry.MaxRetryAttempts` | 2 | До двох повторів після першого звернення |
| `Retry.Delay` | 200 мс | Пауза між повторами |
| `Retry.BackoffType` | Constant | Постійна затримка |
| `Retry.UseJitter` | false | Без випадкової складової затримки (для відтворюваності демонстрації) |
| `Retry.DisableForUnsafeHttpMethods()` | увімкнено | Вимкнення автоматичних повторів для небезпечних HTTP-методів |
| `CircuitBreaker.FailureRatio` | 0.5 | Поріг частки помилок 50% |
| `CircuitBreaker.MinimumThroughput` | 4 | Мінімальна кількість спостережених результатів у вікні |
| `CircuitBreaker.SamplingDuration` | 20 с | Вікно обліку результатів |
| `CircuitBreaker.BreakDuration` | 10 с | Період відкритого ланцюга |

## 4. Сценарії тестування та фактичні результати

### 4.1. Нормальна відповідь

**Запит:** `GET http://localhost:5239/api/reliability/check?mode=ok`.

**Фактична відповідь:** HTTP 200, `{"status":"success","dependencyStatus":200}`. У журналі — `Dependency request, mode=ok` і результат `200`, без повторів. **Успішно**.

### 4.2. Недоступна залежність і Retry

**Запит:** `GET http://localhost:5239/api/reliability/check?mode=fail`.

**Фактична відповідь:** HTTP 503, `{"status":"dependency_failed","dependencyStatus":503}`.

**Докази з журналу:**

```text
Dependency request, mode=fail
Execution attempt ... Attempt: '0', Result: '503'
EventName: 'OnRetry'
Dependency request, mode=fail
Execution attempt ... Attempt: '1', Result: '503'
EventName: 'OnRetry'
Dependency request, mode=fail
Execution attempt ... Attempt: '2', Result: '503'
```

Підтверджено одну початкову спробу та два повтори. **Успішно**.

### 4.3. Circuit Breaker відкривається

**Дія:** кілька послідовних `GET /api/reliability/check?mode=fail`.

**Фактичні відповіді після відкриття:** HTTP 503, `{"status":"circuit_open"}`. Шість послідовних звернень повертали цей результат без нових викликів демонстраційної залежності.

**Докази з журналу:**

```text
EventName: 'OnCircuitOpened'
Polly.CircuitBreaker.BrokenCircuitException:
The circuit is now open and is not allowing calls.
Circuit Breaker is OPEN
```

Відкритий Circuit Breaker блокує звернення до несправної залежності. **Успішно**.

### 4.4. Timeout

**Запит:** `GET http://localhost:5239/api/reliability/check?mode=slow`.

**Умови:** залежність очікує 3 с, `AttemptTimeout` дорівнює 1 с.

**Фактична відповідь:** HTTP 504, `{"status":"timeout"}`.

**Докази з журналу:**

```text
EventName: 'OnTimeout'
The operation didn't complete within the allowed timeout of '00:00:01'.
EventName: 'OnRetry'
Execution attempt ... Attempt: '2'
Dependency timeout
```

Підтверджено скасування повільних спроб та повторні виклики. **Успішно**.

### 4.5. Recovery: Half-Open → Closed

**Дії:** після відкриття Circuit Breaker виклик `mode=ok` спочатку повернув HTTP 503, оскільки ланцюг лишався відкритим. Після очікування приблизно 10 с повторний `mode=ok` повернув HTTP 200.

**Докази з журналу:**

```text
EventName: 'OnCircuitHalfOpened'
Dependency request, mode=ok
Received HTTP response headers ... - 200
EventName: 'OnCircuitClosed'
```

Після завершення періоду блокування дозволено пробний виклик; успішна відповідь повернула Circuit Breaker до стану Closed. **Успішно**.

## 5. Автоматизовані тести

Підготовлено приклад NUnit-тестів у файлі `FlowerShopApi.Tests/ReliabilityTests.cs` із чотирма сценаріями:

1. `Retry_ShouldRepeatFailedRequests` — три спроби загалом при відповіді 503.
2. `Retry_ShouldNotRepeatPostRequests` — відсутність автоматичних повторів POST.
3. `Timeout_ShouldCancelSlowRequest` — виняток `TimeoutRejectedException`.
4. `CircuitBreaker_ShouldOpenAndRecover` — блокування без фактичного HTTP-виклику та відновлення після періоду розмикання.

**Статус:** вихідний код тестів підготовлено; успішність їх виконання необхідно підтвердити результатами `dotnet test` у локальному репозиторії. Не позначати як «пройдено» до запуску.

Команди:

```powershell
dotnet test FlowerShopApi.Tests/FlowerShopApi.Tests.csproj --filter "FullyQualifiedName~ReliabilityTests"
dotnet test FlowerShopApi.sln
```

## 6. Висновки та обмеження

- **Timeout:** повільні HTTP-спроби припиняються після встановленого ліміту, API надає контрольовану відповідь 504.
- **Retry:** тимчасові помилки 503 призводять до повторних спроб; небезпечні HTTP-методи виключено з повторів.
- **Circuit Breaker:** після досягнення порога помилок припиняються нові звернення до залежності.
- **Recovery:** зафіксовано послідовність `OnCircuitOpened` → `OnCircuitHalfOpened` → `OnCircuitClosed`.
- Демонстраційні механізми **не поширюються автоматично** на EF Core/MySQL, сервіси замовлень та інші HTTP-залежності. Для production необхідно застосовувати політики саме до реальних зовнішніх викликів, оцінити ідемпотентність і налаштувати параметри для навантаження.
- Демонстраційний endpoint і клієнт мають залишатися недоступними у Production; це важливо для подальших етапів контейнеризації та deployment.
