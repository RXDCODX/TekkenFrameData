#!/bin/bash
clear
echo "Собираем и запускаем только TekkenFrameData.Watcher и PostgreSQL..."

# Сборка образа Watcher
echo
echo "[1/4] Сборка образа TekkenFrameData.Watcher..."
docker build -f "../TekkenFrameData.Backend/TekkenFrameData.Watcher/Dockerfile" -t tekken_frame_data_watcher:dev ".."

if [ $? -ne 0 ]; then
    echo "Ошибка при сборке Watcher!"
    read -p "Нажмите Enter для продолжения..."
    exit $?
fi

# Сборка образа Streamer
echo
echo "[2/4] Сборка образа TekkenFrameData.Streamer..."
docker build -f "../TekkenFrameData.Backend/TekkenFrameData.Streamer/TekkenFrameData.Streamer.Server/Dockerfile" -t tekken_frame_data.streamer:dev ".."

if [ $? -ne 0 ]; then
    echo "Ошибка при сборке Streamer!"
    read -p "Нажмите Enter для продолжения..."
    exit $?
fi

# Очистка dangling-образов перед запуском
echo
echo "[3/4] Очистка неиспользуемых образов (dangling)..."
docker image prune -f

# Запуск сервисов через docker-compose
echo
echo "[4/4] Запуск PostgreSQL и Watcher..."
docker compose -f "../docker-compose.short.yml" up -d

if [ $? -ne 0 ]; then
    echo "Ошибка при запуске сервисов!"
    read -p "Нажмите Enter для продолжения..."
    exit $?
fi

# Проверка статуса
echo
echo "Состояние контейнеров:"
docker ps --filter "name=tfd_"

echo
echo "Watcher должен быть доступен на http://localhost:7080"
echo "PostgreSQL доступен на localhost:5552"
echo
read -p "Нажмите Enter для завершения..."