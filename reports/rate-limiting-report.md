# Завдання 9 — Rate Limiting у FlowerShopApi

## Мета

Реалізувати обмеження частоти HTTP-запитів до API квіткового магазину FlowerShopApi, захистити ендпоінт каталогу від надмірного навантаження та перевірити повернення статусу `429 Too Many Requests` після перевищення ліміту.

## Використані технології

- ASP.NET Core (.NET 10)
- Вбудований middleware `Microsoft.AspNetCore.RateLimiting`
- Алгоритм `Fixed Window`
- PowerShell для ручної перевірки HTTP-відповідей

## Реалізація

Обмеження налаштовано в `FlowerShopApi/Program.cs` через `builder.Services.AddRateLimiter(...)`. Реєстрація сервісу розташована **до** `builder.Build()`, оскільки після побудови застосунку колекція сервісів доступна тільки для читання.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("products-v2", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
```

До HTTP-конвеєра додано `app.UseRateLimiter()` **після** `app.UseRouting()`:

```csharp
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Політику `products-v2` підключено атрибутом `[EnableRateLimiting("products-v2")]` до методу `GetAll()` у `FlowerShopApi/Controllers/ProductsV2Controller.cs`:

```csharp
[EnableRateLimiting("products-v2")]
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var products = await _productService.GetAllAsync();
    var result = products.Select(ToV2Dto).ToList();
    return Ok(result);
}
```

Отже, обмеження застосовується до `GET /api/v2/products`, але не поширюється автоматично на `GET /api/products`, `GET /api/v1/products` або `GET /api/v2/products/{id}`.

## Параметри політики

| Параметр | Значення | Призначення |
|---|---|---|
| Назва політики | `products-v2` | Ідентифікатор політики для атрибута контролера |
| Алгоритм | Fixed Window | Лічильник запитів у фіксованому часовому вікні |
| `PermitLimit` | `50` | Максимальна кількість дозволених запитів за вікно |
| `Window` | `10 секунд` | Тривалість вікна |
| `QueueLimit` | `0` | Запити понад ліміт не очікують у черзі |
| `AutoReplenishment` | `true` | Автоматичне поповнення дозволів у новому вікні |
| Ключ розподілу | IP клієнта | Окремий лічильник для кожної IP-адреси |
| Відповідь при перевищенні | `429 Too Many Requests` | Повідомлення клієнту про обмеження частоти |

**Примітка:** ліміт означає 50 запитів у межах одного 10-секундного вікна. Якщо запити припадають на різні вікна, 51-й запит загалом не обов'язково отримуватиме `429`.

## Ручна перевірка

API запускався командою:

```powershell
dotnet run --project FlowerShopApi/FlowerShopApi.csproj
```

Для перевірки використовувалася серія HTTP-запитів із PowerShell, наприклад:

```powershell
1..60 | ForEach-Object {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:5239/api/v2/products" `
        -SkipHttpErrorCheck
    Write-Host "Request $_ : $([int]$response.StatusCode)"
}
```

### Хід перевірки та результат

1. Після початкового налаштування серія запитів повертала `200 OK` без обмеження. У журналах було видно виконання `ProductService` і SQL-запитів для кожного звернення.
2. Під час діагностики перевірено порядок `UseRouting()` / `UseRateLimiter()`, застосування атрибута `[EnableRateLimiting("products-v2")]` та перезапуск API.
3. Після виправлення підключення політики користувач підтвердив, що обмеження **працює**. Фінальне значення `PermitLimit` змінено з початкових 5 на **50**.

**Підтверджений підсумок:** Rate Limiting працює за результатом ручної перевірки. Повний фінальний вивід HTTP-статусів після переходу на ліміт 50 у матеріалах не наведено, тому точні кількості відповідей `200` та `429` тут не заявляються.

## Принцип роботи

Після отримання запиту middleware визначає IP-адресу клієнта та знаходить відповідний лічильник. Якщо в поточному часовому вікні залишилися дозволи, запит проходить до `ProductsV2Controller` і далі до `ProductService`. Якщо дозволи вичерпано, запит відхиляється з `429`, не виконуючи бізнес-логіку каталогу. На початку наступного вікна дозволи автоматично поновлюються.

## Обмеження реалізації

- Лічильники зберігаються в пам'яті процесу. Якщо API розгорнуто у кількох екземплярах, ліміти між ними автоматично не синхронізуються.
- Якщо застосунок працює за reverse proxy, IP клієнта слід визначати через правильно налаштовані та довірені forwarded headers; інакше декілька користувачів можуть ділити один ліміт.
- Ліміт за IP не є повноцінним захистом від розподілених DDoS-атак.
- Політика застосована лише до одного маршруту; для авторизації, створення замовлень та інших чутливих операцій можуть знадобитися окремі обмеження.
- Автоматизовані тести саме для Rate Limiting **не додавалися** за рішенням виконавця. Останній підтверджений результат раніше наявного набору — **58/58 успішних тестів**, але цей результат отримано **до** змін дев'ятого завдання; повторний запуск повного набору після змін не підтверджено.

## Висновок

У FlowerShopApi реалізовано вбудоване обмеження частоти запитів на основі Fixed Window. Політика `products-v2` дозволяє до 50 запитів за 10 секунд для однієї IP-адреси та повертає HTTP `429 Too Many Requests` у разі перевищення ліміту. Обмеження локалізоване на ендпоінті `GET /api/v2/products`, тому маршрути попередніх версій API не змінюються. Працездатність механізму підтверджено ручною перевіркою.
