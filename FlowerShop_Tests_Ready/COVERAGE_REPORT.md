# Test Coverage Report — Orders & Discounts

## Як згенерувати

```bash
dotnet test FlowerShopApi.Tests --collect:"XPlat Code Coverage" --results-directory ./TestResults
# опційно HTML:
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./CoverageReport -reporttypes:Html
```

## Очікувані компоненти з високим покриттям

| Компонент | Методи | Очікуване покриття |
|-----------|--------|-------------------|
| OrderService | CreateAsync, GetMyOrdersAsync, GetAllAsync, GetByIdAsync, UpdateStatusAsync | ~90%+ |
| DiscountService | GetAllAsync, CreateAsync, DeleteAsync | ~85%+ |

## Що покрито тестами

- Успішне створення замовлення + підрахунок total + зменшення stock
- Отримання своїх / усіх / по ID
- Зміна статусу
- Винятки: NotFound, BadRequest (порожнє замовлення, stock, невалідна знижка)
- Mock-взаємодія з IOrderRepository
- REST endpoints + Integration через InMemory DB
- 2 E2E-сценарії

## Примітка

Високий % coverage не гарантує якість — важливо, що тести перевіряють **значущу поведінку** (бізнес-правила замовлень і знижок).
