# FlowerShopApi 🌸

## Призначення
Backend API для інтернет-магазину квітів та букетів **FlowerShopApi**, розроблений для управління каталогом товарів, категоріями, замовленнями користувачів, профільними даними та безпечною автентифікацією (JWT та Google OAuth2).

## Структура проєкту
* **`Controllers/`** — контролери для обробки HTTP-запитів та маршрутизації (`Auth`, `Products`, `Categories`, `Orders`, `Profile`, `Users` тощо).
* **`Services/`** — бізнес-логіка застосунку та сервіси обробки даних (наприклад, `AuthService`, `ProductService`, `TokenService`).
* **`Repositories/`** — репозиторії для роботи з даними через патерн Repository та Entity Framework Core.
* **`Models/`** — сутності (сутнісні класи) бази даних.
* **`DTOs/`** — моделі передачі даних (Data Transfer Objects) для запитів і відповідей.
* **`Middleware/`** — проміжне ПЗ для глобальної обробки винятків (`ExceptionHandlingMiddleware`).
* **`Data/`** — контекст бази даних (`AppDbContext`).
* **`appsettings.json`** — файл конфігурації (строка підключення до БД, налаштування JWT).
* **`docker-compose.yml`** — файл конфігурації для запуску API та бази даних у контейнерах Docker.

## Технології
* **Platform:** .NET 10 (ASP.NET Core Web API)
* **Database:** MySQL 8.0, Entity Framework Core (ORM)
* **Authentication:** JWT (JSON Web Tokens) із підтримкою Access/Refresh токенів та Google OAuth2
* **Password Hashing:** BCrypt.Net
* **Containerization:** Docker & Docker Compose
* **Documentation:** Swagger / OpenAPI

## Залежності
Основні NuGet-пакети, що використовуються в проєкті:
* `Pomelo.EntityFrameworkCore.MySql` — провайдер MySQL для Entity Framework Core.
* `Microsoft.AspNetCore.Authentication.JwtBearer` — підтримка JWT-автентифікації.
* `BCrypt.Net-Next` — бібліотека для безпечного хешування паролів користувачів.

## Запуск проєкту

### Варіант 1: Запуск через Docker Compose (Рекомендовано)
1. Переконайтеся, що на комп'ютері встановлені **Docker** та **Docker Compose**.
2. У корені проєкту виконайте команду для збирання та запуску контейнерів:
   ```bash
   docker-compose up --build