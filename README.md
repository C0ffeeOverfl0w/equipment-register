# Учёт компьютерной техники

Клиент-серверный модуль по тестовому заданию Middle Full-stack C# / JS. Стек: ASP.NET Core 10, EF Core 10 с Npgsql, PostgreSQL 18, MediatR, FluentValidation, JavaScript, jQuery и DevExtreme.

## Текущее состояние

Реализованы список, создание и редактирование карточек через HTTP API и диалоги DevExtreme. Работают серверные пагинация и сортировка четырёх предметных колонок, справочник типов, заполнение только пустого справочника, валидация, уникальность номера, атомарный предел 2000 записей и проверка исходной версии при PUT. Прошли 140 автоматических проверок: 56 unit, 15 архитектурных и 69 интеграционных.

Docker Compose запускает PostgreSQL, сервер запускается через .NET CLI или Visual Studio. Миграции применяются отдельной командой перед первым запуском. Повтор POST после потери ответа может вернуть 409: воспроизведение исходного ответа не реализовано.

## Требования к окружению

- Стабильный .NET SDK 10.0; global.json допускает установленную feature-версию в пределах .NET 10.
- Docker с Linux containers и Docker Compose.
- Node.js 20 или выше и npm для подготовки локальных клиентских ресурсов.

## Запуск в Windows PowerShell

Из корня репозитория подготовить клиент:

```powershell
Push-Location src/Equipment.Web
npm.cmd ci --ignore-scripts
npm.cmd run build
Pop-Location
```

Поднять локальную PostgreSQL, восстановить пакеты и отдельно применить миграции:

```powershell
docker compose up --detach --wait db
dotnet restore EquipmentRegister.sln --locked-mode
dotnet run --project src/Equipment.Web --configuration Release -- --Database:MigrateOnly=true
```

Запустить сервер:

```powershell
dotnet run --project src/Equipment.Web --configuration Release --no-restore
```

Открыть http://localhost:5080. Профиль http задаёт Development и настройки локальной БД. Без этого профиля нужно задать `ConnectionStrings__Equipment` через окружение. Учётная запись `equipment` и пароль `equipment_dev_only` предназначены только для локальной демонстрации; порт БД опубликован на loopback 55432. При изменении POSTGRES_PASSWORD или POSTGRES_PORT также изменить строку подключения.

На чистой БД список техники пуст. Обычный старт добавляет только справочник. Необязательные 45 демонстрационных карточек можно загрузить отдельной командой, пока сервер работает:

```powershell
Get-Content scripts/add-demo-data.sql -Raw -Encoding utf8 | docker compose exec -T db psql -U equipment -d equipment -v ON_ERROR_STOP=1
```

Скрипт работает только с локальной БД, повторно не создаёт те же DEMO-номера и не включён в запуск приложения. После него нажать «Обновить». Для остановки БД без удаления сохранённых данных: `docker compose stop db`.

## Запуск из Visual Studio

Открыть EquipmentRegister.sln, назначить Equipment.Web запускаемым проектом, выбрать профиль http и нажать F5. После предварительной подготовки БД, миграций и клиентских ресурсов браузер открывается автоматически на http://localhost:5080.

Порт 5080 должен быть свободен: одновременно запущенный CLI-процесс на том же порту не позволяет студии запустить второй экземпляр. Для отдельного параллельного предпросмотра использовать другой порт:

```powershell
dotnet run --project src/Equipment.Web --configuration Release -- --urls http://localhost:5081
```

## Создание и изменение

Кнопка «Добавить» открывает пустую карточку, кнопка в строке - текущую карточку для изменения. Все четыре поля обязательны. Учётный номер сохраняет регистр и начальные нули; уникальность регистрозависимая по рабочему допущению. Наименование поддерживает произвольный текст до 256 Unicode-символов; комната - целое от 1 до 1000.

В PUT передаётся полная карточка и исходная версия клиента. Сервер сам увеличивает версию и сравнивает исходную в UPDATE. При 409 пользователь сохраняет введённые данные и может явно перечитать карточку. Во время сохранения повторная отправка и закрытие диалога заблокированы; автоматических повторов POST/PUT нет. Неизвестные поля JSON отклоняются, id и новая версия не назначаются из POST.

Создание проверяет COUNT и вставляет строку в одной транзакции под общей PostgreSQL-блокировкой. Этот предел действует для путей записи модуля; прямой SQL администратора вне протокола им не ограничивается. Ошибки дубликата, лимита и версии имеют разные коды. При заполненных 2000 записях редактирование остаётся доступным.

## Проверки

```powershell
dotnet build EquipmentRegister.sln --configuration Release --no-restore
dotnet test EquipmentRegister.sln --configuration Release --no-build
dotnet format whitespace EquipmentRegister.sln --verify-no-changes --no-restore
```

Интеграционные проверки требуют Docker. Testcontainers самостоятельно создаёт и удаляет отдельную тестовую PostgreSQL; локальная БД демонстрации не очищается. Первый запуск может загрузить PostgreSQL и служебный образ Testcontainers. Изменять строки подключения для тестов не требуется.

`GET /health/live` проверяет процесс, `GET /health/ready` - доступность справочника после инициализации. После остановки БД ожидаются 200 и 503 соответственно. API и UI не выдают пользователю SQL или stack trace.

## Миграции

Обычный старт выполняет seeding, но не меняет схему. Для генерации следующих миграций:

```powershell
dotnet tool restore
dotnet ef migrations add MigrationName --project src/Equipment.Infrastructure --startup-project src/Equipment.Web
```

Версии NuGet заданы централизованно; packages.lock.json и package-lock.json входят в репозиторий. Ресурсы jQuery/DevExtreme копируются из npm в игнорируемый wwwroot/vendor. После подготовки ресурсов браузеру не нужен CDN.

## Архитектура

```text
src/
  Equipment.Domain/          # Сущности и локальные инварианты
  Equipment.Application/     # Сценарии чтения/записи, валидация, порты и DTO
  Equipment.Infrastructure/  # EF Core, PostgreSQL, миграции и seeding
  Equipment.Web/             # HTTP, composition root, статический клиент
tests/
  Equipment.UnitTests/
  Equipment.ArchitectureTests/
  Equipment.IntegrationTests/
```

Сервер использует whitelist сортировки и добавляет ID как второй ключ. Сортировка выполняется до LIMIT/OFFSET. Count и выборка выполняются последовательно на scoped DbContext; контракт не обещает фиксированный снимок при изменениях между запросами. Созданы PK, UNIQUE номера и один индекс FK; дополнительных индексов на каждую колонку нет.

## DevExtreme

Используется 30-дневный ознакомительный режим DevExtreme 26.1.6 с официальным уведомлением в интерфейсе. Лицензионные права на коммерческую поставку не заявляются; уведомление не скрывается. Передача исходников для оценки не должна представляться как лицензированная коммерческая поставка компонентов.

Официальные условия: https://js.devexpress.com/jQuery/Documentation/Guide/Common/Licensing/
