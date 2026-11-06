# FlowerShopApi — frontend (адаптація до моноліту)

Цей статичний frontend адаптовано з `FrontKurs` для поточної навчальної версії **FlowerShopApi** (ASP.NET Core .NET 10, MySQL).

## Що змінено

- Єдиний API за замовчуванням: `http://localhost:5239`, **без Gateway 5000 / AuthService / CatalogService**.
- Каталог: `GET /api/products`, картка товару `GET /api/products/{id}`.
- Категорії: `GET /api/categories`; знижки: `GET /api/discounts`.
- Авторизація: `/api/auth/login`, `/api/auth/register`. Токен передається як `Authorization: Bearer ...`.
- Товари в адмінпанелі: `/api/products` (POST/PUT/DELETE); `isAvailable` тепер передається в DTO.
- Кошик: **локальний у браузері (localStorage)**, тому не потребує відсутнього `/api/cart`.
- Оформлення: `POST /api/orders` з `recipientName`, `recipientPhone`, `deliveryAddress`, `comment`, `items: [{productId, quantity}]`.
- Прибрано підміну відсутніх товарів демотоварами. Помилки API видно у консолі браузера.
- Прибрано з активного checkout оплату через відсутній `/api/Payments/sandbox`, приховано застосування промокодів (відсутні у відомому API).
- Видалено непідключені старі `app.js` і `profile.js` у корені фронтенду.

## Локальний запуск

1. Запустіть MySQL у Docker. Потім API: `dotnet run --project FlowerShopApi/FlowerShopApi.csproj`.
2. Рекомендовано розміщувати цю папку як `frontend/` **у корені репозиторію**.
3. Із `frontend/` запустіть статичний сервер, наприклад `py -m http.server 5500 --bind 127.0.0.1`.
4. Відкрийте `http://127.0.0.1:5500/index.html`.
5. Якщо backend працює за іншим URL, змініть `API_BASE` у `js/config.js`.

### CORS

Поточний `Program.cs` дозволяє `http://127.0.0.1:5500` і `http://localhost:5500` (політика `AllowFrontend`). Якщо будете розгортати frontend за іншою адресою, додайте точний origin до `WithOrigins(...)` у backend і перезберіть API. Не використовуйте `AllowAnyOrigin()` для production без необхідності.

## Що потребує звірки з фактичними backend DTO

Тут використані маршрути, відомі з поточної роботи над FlowerShopApi. Архів містив лише **frontend**, а не актуальні `.cs` контролери/DTO — тому повну інтеграцію з сервером не можна підтвердити без перевірки Swagger.

- `POST /api/auth/register`: точні обов'язкові поля реєстрації (`firstName`, `lastName`, `phone`, `email`, `password`).
- `POST /api/auth/login`: назва поля з токеном (`accessToken`/`token`/`jwtToken`).
- `GET /api/profile`, `GET /api/orders/my`: доступність і формат відповіді.
- `POST /api/orders`: структура DTO — зараз надсилаємо поля, які були в наданому фронтенді; їх треба звірити зі Swagger конкретної гілки.
- `GET /api/products/{id}/images`, `POST /api/products/{id}/images`, `DELETE /api/product-images/{id}`: конкретні маршрути зображень.
- Сторінки промокодів і sandbox платежів залишені як **непідтримувані**, поки не існує відповідних контролерів; кнопки checkout sandbox приховані.

**Увага:** кошик зберігається у браузері цього пристрою. Ціни та наявність сервер повинен перевіряти сам під час створення замовлення, а не довіряти frontend. JWT у localStorage — навчальна реалізація, для production варто оцінити ризики XSS та безпечнішу стратегію зберігання токенів.

## Git

Покладіть папку `frontend` у репозиторій і виконайте:

```powershell
git add frontend/
git commit -m "Add FlowerShopApi frontend adapted to monolith API"
git push origin quality/module-4
```

Підставте **свою фактичну назву гілки**, якщо відрізняється.
