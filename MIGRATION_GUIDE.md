# Руководство по миграции на микросервисную архитектуру

## Обзор изменений

Проект был рефакторен с монолитной архитектуры на микросервисную с использованием gRPC для межсервисного взаимодействия.

### Новая архитектура

1. **TekkenFrameData.Core** - Основной сервис
   - Фреймдата (TekkenCharacter, TekkenMove)
   - Telegram бот
   - База данных (основная)
   - gRPC сервер для предоставления данных

2. **TekkenFrameData.TwitchService** - Twitch сервис
   - Twitch интеграция
   - Twitch бот команды
   - gRPC клиент для получения данных из Core

3. **TekkenFrameData.DiscordService** - Discord сервис
   - Discord интеграция
   - Discord slash команды
   - gRPC клиент для получения данных из Core

## Преимущества новой архитектуры

- **Разделение ответственности**: Каждый сервис отвечает за свою платформу
- **Масштабируемость**: Сервисы можно масштабировать независимо
- **Отказоустойчивость**: Проблемы в одном сервисе не влияют на другие
- **Технологическая гибкость**: Каждый сервис может использовать оптимальные технологии
- **Простота разработки**: Меньшие кодовые базы легче поддерживать

## Шаги миграции

### 1. Подготовка окружения

```bash
# Клонирование репозитория
git clone <repository-url>
cd TekkenFrameData

# Восстановление зависимостей
dotnet restore
```

### 2. Настройка конфигурации

Создайте файлы конфигурации для каждого сервиса:

#### Core Service (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tekkenframedata;Username=postgres;Password=password"
  },
  "TelegramBot": {
    "Token": "your_telegram_bot_token",
    "AdminIds": [123456789]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

#### Twitch Service (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tekkenframedata;Username=postgres;Password=password"
  },
  "CoreService": {
    "Url": "http://localhost:5000"
  },
  "Twitch": {
    "ClientId": "your_twitch_client_id",
    "ClientSecret": "your_twitch_client_secret",
    "Channel": "your_channel_name",
    "Username": "your_twitch_username",
    "OAuthToken": "your_oauth_token"
  }
}
```

#### Discord Service (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tekkenframedata;Username=postgres;Password=password"
  },
  "CoreService": {
    "Url": "http://localhost:5000"
  },
  "Discord": {
    "BotToken": "your_discord_bot_token"
  }
}
```

### 3. Запуск с Docker Compose

```bash
# Запуск всех сервисов
docker-compose -f docker-compose.microservices.yml up -d

# Просмотр логов
docker-compose -f docker-compose.microservices.yml logs -f

# Остановка сервисов
docker-compose -f docker-compose.microservices.yml down
```

### 4. Запуск в режиме разработки

```bash
# Core Service
cd TekkenFrameData.Backend/TekkenFrameData.Core
dotnet run

# Twitch Service (в новом терминале)
cd TekkenFrameData.Backend/TekkenFrameData.TwitchService
dotnet run

# Discord Service (в новом терминале)
cd TekkenFrameData.Backend/TekkenFrameData.DiscordService
dotnet run
```

## API Endpoints

### Core Service (gRPC)
- **Порт**: 5000 (HTTP), 5001 (HTTPS)
- **gRPC сервис**: FrameDataService
- **Методы**:
  - GetCharacters()
  - GetCharacter()
  - GetCharacterMoves()
  - GetMove()
  - SearchMoves()

### Twitch Service
- **Порт**: 5002 (HTTP), 5003 (HTTPS)
- **Команды**:
  - `!character <name>` - информация о персонаже
  - `!move <name> [character]` - информация о движении
  - `!search <query> [character]` - поиск движений
  - `!characters` - список персонажей

### Discord Service
- **Порт**: 5004 (HTTP), 5005 (HTTPS)
- **Slash команды**:
  - `/character <name>` - информация о персонаже
  - `/move <name> [character]` - информация о движении
  - `/search <query> [character]` - поиск движений
  - `/characters` - список персонажей

## Миграция данных

База данных остается той же, поэтому миграция данных не требуется. Просто убедитесь, что:

1. База данных PostgreSQL запущена
2. Строка подключения настроена правильно
3. Миграции применены (если необходимо)

```bash
# Применение миграций
dotnet ef database update --project TekkenFrameData.Backend/TekkenFrameData.Core
```

## Мониторинг и логирование

Каждый сервис отправляет логи в Telegram (если настроен) и консоль. Для мониторинга используйте:

```bash
# Проверка здоровья сервисов
curl http://localhost:5000/health  # Core
curl http://localhost:5002/health  # Twitch
curl http://localhost:5004/health  # Discord
```

## Отладка

### Проблемы с gRPC
1. Убедитесь, что Core сервис запущен
2. Проверьте URL в конфигурации клиентов
3. Проверьте сетевые настройки Docker

### Проблемы с Twitch
1. Проверьте токены и настройки в конфигурации
2. Убедитесь, что бот имеет необходимые права
3. Проверьте логи на наличие ошибок аутентификации

### Проблемы с Discord
1. Проверьте токен бота
2. Убедитесь, что бот добавлен на сервер
3. Проверьте права бота

## Производительность

- gRPC обеспечивает высокую производительность межсервисного взаимодействия
- Каждый сервис может быть масштабирован независимо
- Используйте кэширование для часто запрашиваемых данных

## Безопасность

- Все токены должны храниться в переменных окружения
- Используйте HTTPS в продакшене
- Настройте аутентификацию между сервисами при необходимости

## Следующие шаги

1. Настройте мониторинг (Prometheus, Grafana)
2. Добавьте кэширование (Redis)
3. Настройте CI/CD для каждого сервиса
4. Добавьте тесты для каждого сервиса
5. Настройте автоматическое масштабирование 