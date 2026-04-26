<div align="center">

<div align="center">

<img src="media/ico.ico" width="96" height="96" alt="Dotnetify Logo" />

# Dotnetify

</div>

### Превращает любой OpenAPI (`swagger.json`) файл в запускаемый backend-проект на C# (.NET) за считанные секунды.

<p align="center">
  <img src="https://shields.dvurechensky.pro/badge/.NET-8.0-blue?logo=dotnet">
  <img src="https://shields.dvurechensky.pro/badge/OpenAPI-Swagger-green">
  <img src="https://shields.dvurechensky.pro/badge/Status-Active-brightgreen">
  <img src="https://shields.dvurechensky.pro/badge/CLI-Tool-black">
</p>

</div>

---

<div align="center" style="margin: 20px 0; padding: 10px; background: #1c1917; border-radius: 10px;">
  <strong>🌐 Язык: </strong>
  
  <span style="color: #F5F752; margin: 0 10px;">
    ✅ 🇷🇺 Русский (текущий)
  </span>
  | 
  <a href="./README.md" style="color: #0891b2; margin: 0 10px;">
    🇺🇸 English
  </a>
</div>

---

## Обзор

**Dotnetify** — CLI-инструмент, который преобразует OpenAPI / Swagger спецификацию в полностью сгенерированный **ASP.NET Core серверный проект**.

Вместо сырого output генератора Dotnetify создаёт **структурированный, готовый к запуску backend-проект**, который можно сразу собрать, запустить и дорабатывать.

Инструмент создан для разработчиков, которым нужна быстрая стартовая точка серверной части на основе уже существующей API-схемы.

> [!IMPORTANT]
> Dotnetify делает упор на создание **полезного и рабочего серверного проекта**, а не просто набора сгенерированных файлов.

---

## Что делает Dotnetify

Вход:

```text
swagger.json
```

Pipeline:

```text
OpenAPI спецификация
        ↓
Генерация кода
        ↓
Roslyn обработка
        ↓
Структурирование проекта
        ↓
Запускаемый .NET backend
```

Результат:

```text
ProjectName/
 ├── Controllers/
 ├── Models/
 ├── Enums/
 ├── Common/
 ├── Program.cs
 ├── ProjectName.csproj
```

По умолчанию `ProjectName` равен `GeneratedApi`. Его можно изменить через `--name`.

---

## Возможности

| Категория         | Описание                                                   |
| ----------------- | ---------------------------------------------------------- |
| Импорт OpenAPI    | Чтение `swagger.json` спецификаций                         |
| Controllers       | Генерация API-контроллеров по группам маршрутов            |
| Models            | Генерация DTO / entity классов                             |
| Enums             | Генерация enum-типов из схем                               |
| Mock Responses    | Автоматическая генерация тестовых ответов                  |
| Build Ready       | Проект готов к сборке                                      |
| Run Ready         | Проект можно сразу запускать                               |
| Swagger UI        | Генерируемый сервер поднимает Swagger портал               |
| Custom Name       | Можно задать имя проекта, папки и `.csproj` через `--name` |
| Custom Port       | Можно запустить API на выбранном HTTP-порту через `--port` |
| Roslyn Processing | Очистка и реструктуризация кода                            |

---

## Почему Dotnetify?

Большинство генераторов заканчивают здесь:

```text
swagger.json → сырой сгенерированный код
```

Dotnetify идёт дальше:

```text
swagger.json → рабочий backend-проект
```

Это означает:

- Быстрый старт frontend-разработки
- Быстрое прототипирование
- Удобное тестирование API
- Ускорение reverse engineering сценариев
- Готовая серверная база для доработки

---

## Быстрый старт

Перейдите в папку с собранным бинарником:

```bash
cd app\Dotnetify\bin\Debug\net8.0
```

Сгенерировать проект и запустить:

```bash
Dotnetify.exe generate Input\swagger.json --run
```

Сгенерировать проект с собственным именем:

```bash
Dotnetify.exe generate Input\swagger.json --name MyApi
```

Сгенерировать проект с собственным именем и сразу запустить:

```bash
Dotnetify.exe generate Input\swagger.json --name MyApi --run
```

Сгенерировать, запустить и указать собственный HTTP-порт:

```bash
Dotnetify.exe generate Input\swagger.json --name MyApi --run --port 5103
```

Если `--port` не указан, Dotnetify использует порт `5000`.

Будет создано:

```text
Output/MyApi/MyApi.csproj
```

Пример вывода:

```text
[Dotnetify] Running: Roslyn Split Processor
[Dotnetify] Running: Dotnet Project Processor
[Dotnetify] Running: Package Resolve Processor

Generation complete.
Starting server...
Started PID: 29296

Swagger: http://localhost:5000/swagger
```

С собственным портом:

```text
Started PID: 29296
Swagger: http://localhost:5103/swagger
```

---

## Где может использоваться

### Frontend команды

Когда backend ещё не готов, но интерфейс уже нужно разрабатывать.

### QA / API Testing

Мгновенное поднятие локального API по схеме.

### Legacy Systems

Воссоздание документированных API в виде editable .NET проекта.

### Стартапы / MVP

Быстрый запуск backend-прототипа.

---

## Пример результата

Сгенерированный контроллер:

```csharp
[HttpGet("inventory")]
public Task<IDictionary<string, int>> GetInventory()
{
    var response = new Dictionary<string, int>
    {
        { "test", 1 }
    };

    return Task.FromResult<IDictionary<string, int>>(response);
}
```

---

## Философия проекта

Dotnetify **не пытается заменить backend-разработку**.

Его задача — убрать пустую стартовую фазу.

Вместо часов ручного создания:

- controllers
- DTO
- routes
- startup boilerplate
- fake data

Вы начинаете с уже готовой основы.

---

## Roadmap

Планируемые улучшения:

- Генерация базы данных
- Генерация Repository слоя
- Генерация React admin панели
- Шаблоны авторизации
- Docker поддержка
- CI/CD шаблоны
- Несколько архитектурных режимов

---

## Текущий статус

> Активная разработка

Базовый pipeline уже работает и генерирует запускаемые проекты.

---

## Технологии

- C#
- .NET
- ASP.NET Core
- Roslyn
- NSwag
