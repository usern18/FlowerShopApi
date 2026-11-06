# Database Optimization — FlowerShopApi

## Мета

Дослідити виконання запитів MySQL за допомогою `EXPLAIN` та `EXPLAIN ANALYZE`, виявити можливості оптимізації доступу до даних і порівняти виконання запиту до та після створення індексу.

## Середовище та об’єкт дослідження

- Проєкт: **FlowerShopApi** (ASP.NET Core, Entity Framework Core, MySQL 8.0 у Docker).
- База: `rosemary_shop`.
- Реальна таблиця: `products` — **12 записів** на момент дослідження.
- Для перевірки впливу індексу створено окрему таблицю `products_performance_test` із **100 000 синтетичних записів**; **1 000** мають `stock_quantity = 0`.
- Використані інструменти: `SHOW INDEX`, `EXPLAIN`, `EXPLAIN ANALYZE`.

## 1. Початкова перевірка реальної таблиці `products`

```sql
SHOW INDEX FROM products;
EXPLAIN SELECT * FROM products WHERE category_id = 1;
```

Виявлено два індекси: `PRIMARY (product_id)` і `fk_products_categories (category_id)`. Для фільтрації за `category_id = 1` MySQL застосовував `type = ref`, `key = fk_products_categories`, оцінюючи читання 4 рядків. **Дублювати цей індекс немає потреби.**

Для іншого запиту, який відповідає логіці `ProductRepository.GetOutOfStockAsync()`:

```sql
EXPLAIN SELECT * FROM products WHERE stock_quantity = 0;
EXPLAIN ANALYZE SELECT * FROM products WHERE stock_quantity = 0;
SELECT COUNT(*) AS total_products,
       SUM(stock_quantity = 0) AS out_of_stock_products
FROM products;
```

Результати: `type = ALL`, `key = NULL`, повне сканування 12 рядків, **0** відповідних товарів; `EXPLAIN ANALYZE` показав час фільтрації приблизно **0,101 мс**. Для такої малої таблиці повне сканування не є доказом проблеми продуктивності.

## 2. Підготовка контрольного набору даних

Щоб виміряти різницю на суттєвішому обсязі, створено окрему тестову таблицю, не змінюючи записи основного каталогу:

```sql
CREATE TABLE products_performance_test (
    product_id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(255) NOT NULL,
    stock_quantity INT NOT NULL
);

SET SESSION cte_max_recursion_depth = 100001;

INSERT INTO products_performance_test (name, stock_quantity)
WITH RECURSIVE numbers AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM numbers WHERE n < 100000
)
SELECT CONCAT('Test Product ', n),
       CASE WHEN n % 100 = 0 THEN 0 ELSE 10 END
FROM numbers;
```

При першій спробі вставлення MySQL повернув помилку `ERROR 3636` через стандартне обмеження глибини рекурсії; після збільшення сесійного ліміту таблицю було заповнено.

**Контрольний запит:**

```sql
SELECT *
FROM products_performance_test
WHERE stock_quantity = 0;
```

Повертає **1 000** записів із **100 000**, тобто приблизно **1%** таблиці.

## 3. План і час до оптимізації

```sql
EXPLAIN ANALYZE
SELECT *
FROM products_performance_test
WHERE stock_quantity = 0;
```

Зафіксований план:

```text
Filter: stock_quantity = 0
  (actual time=0.0444..10.5 rows=1000 loops=1)
  Table scan on products_performance_test
    (actual time=0.0362..8.13 rows=100000 loops=1)
```

База переглядала **100 000 рядків** і відбирала **1 000**. Показаний загальний час вузла фільтрації — **10,5 мс**.

## 4. Створення індексу та результати

```sql
CREATE INDEX idx_products_perf_stock
ON products_performance_test(stock_quantity);

EXPLAIN
SELECT * FROM products_performance_test
WHERE stock_quantity = 0;

EXPLAIN ANALYZE
SELECT * FROM products_performance_test
WHERE stock_quantity = 0;
```

`EXPLAIN` показав `type = ref`, `key = idx_products_perf_stock`, `rows = 1000`.

Перший вимір після створення індексу:

```text
Index lookup on products_performance_test using idx_products_perf_stock
  (stock_quantity=0)
  (actual time=0.164..1.3 rows=1000 loops=1)
```

Повторний вимір:

```text
Index lookup on products_performance_test using idx_products_perf_stock
  (stock_quantity=0)
  (actual time=0.145..1.25 rows=1000 loops=1)
```

## 5. Контрольне порівняння з `IGNORE INDEX`

Щоб повторно виміряти повне сканування **без видалення індексу**:

```sql
EXPLAIN ANALYZE
SELECT *
FROM products_performance_test IGNORE INDEX (idx_products_perf_stock)
WHERE stock_quantity = 0;
```

Результат:

```text
Filter: stock_quantity = 0
  (actual time=0.0471..10.5 rows=1000 loops=1)
  Table scan on products_performance_test
    (actual time=0.0382..8.06 rows=100000 loops=1)
```

### Порівняльна таблиця

| Показник | Без індексу / `IGNORE INDEX` | З індексом |
|---|---:|---:|
| Кількість записів у таблиці | 100 000 | 100 000 |
| Записів у результаті | 1 000 | 1 000 |
| Спосіб доступу | `Table scan` | `Index lookup` |
| Індекс, що використано | — | `idx_products_perf_stock` |
| Час за повторним виміром | 10,5 мс | 1,25 мс |
| Відносне прискорення | 1× | **8,4×** |
| Скорочення часу | — | **≈88,1%** |

Розрахунки: `10,5 / 1,25 = 8,4`; `(10,5 − 1,25) / 10,5 × 100 ≈ 88,1%`.

## 6. Висновки й обмеження

1. Запит за `category_id` у реальній таблиці **вже має індекс**, тому додатковий індекс був би зайвим.
2. Запит за `stock_quantity = 0` у реальній таблиці виконував повне сканування, але лише **12 рядків** за приблизно **0,101 мс**. Підстав вважати це суттєвим вузьким місцем на поточних даних немає.
3. На окремому синтетичному наборі з **100 000 рядків** індекс за `stock_quantity` змінив план із `Table scan` на `Index lookup` і зменшив спостережуваний час із **10,5 до 1,25 мс** (приблизно **8,4 раза**).
4. Наведені значення — вимірювання окремих запусків у тестовому середовищі, а не гарантія такого ж прискорення в продакшені. На них впливають кешування, розмір таблиці, розподіл значень і навантаження.
5. Індекси займають дисковий простір і створюють додаткову роботу під час `INSERT`, `UPDATE`, `DELETE`. **Індекс на `stock_quantity` не додавали в основну таблицю `products`**; рішення про його впровадження варто приймати за реальними обсягами й частотою запитів.

## Матеріали для підтвердження

До звіту можна додати скриншоти консольних результатів `SHOW INDEX`, початкового `EXPLAIN ANALYZE`, створення індексу, повторного `EXPLAIN ANALYZE` та `IGNORE INDEX`.
