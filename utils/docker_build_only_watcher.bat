@echo off
chcp 65001 > nul
cls
echo Собираем и запускаем только TekkenFrameData.Watcher и PostgreSQL...

:: Сборка образа Watcher
echo.
echo [1/4] Сборка образа TekkenFrameData.Watcher...
docker build -f "..\TekkenFrameData.Backend\TekkenFrameData.Watcher\Dockerfile" -t tekken_frame_data.watcher:dev ".."

:: Сборка образа Streamer
echo.
echo [2/4] Сборка образа TekkenFrameData.Streamer...
docker build -f "..\TekkenFrameData.Backend\TekkenFrameData.Streamer\TekkenFrameData.Streamer.Server\Dockerfile" -t tekken_frame_data.streamer:dev ".."

if %errorlevel% neq 0 (
    echo Ошибка при сборке Watcher!
    pause
    exit /b %errorlevel%
)

:: Очистка dangling-образов перед запуском
echo.
echo [3/4] Очистка неиспользуемых образов (dangling)...
docker image prune -f

:: Запуск сервисов через docker-compose
echo.
echo [4/4] Запуск PostgreSQL и Watcher...
docker compose -f "..\docker-compose.short.yml" up -d

if %errorlevel% neq 0 (
    echo Ошибка при запуске сервисов!
    pause
    exit /b %errorlevel%
)

:: Проверка статуса
echo.
echo Состояние контейнеров:
docker ps --filter "name=tfd_"

echo.
echo Watcher должен быть доступен на http://localhost:7080
echo PostgreSQL доступен на localhost:5552
echo.
pause