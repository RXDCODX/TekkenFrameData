#!/bin/sh

printf "\033c" # Очистка экрана (аналог cls)
echo "Собираем и запускаем только TekkenFrameData.Watcher и PostgreSQL..."

# Сборка образа Watcher
echo
echo "[1/3] Сборка образа TekkenFrameData.Watcher..."
docker build -f ../TekkenFrameData.Backend/TekkenFrameData.Watcher/Dockerfile -t tekken_frame_data.watcher:dev ..

if [ $? -ne 0 ]; then
    echo "Ошибка при сборке Watcher!"
    read -p "Нажмите Enter для продолжения..." # Аналог pause
    exit 1
fi

# Запуск сервисов через docker-compose
echo
echo "[2/3] Запуск PostgreSQL и Watcher..."
docker-compose -f ../docker-compose.watcher.yml up -d

if [ $? -ne 0 ]; then
    echo "Ошибка при запуске сервисов!"
    read -p "Нажмите Enter для продолжения..."
    exit 1
fi

# Проверка статуса
echo
echo "[3/3] Проверяем работу сервисов..."
echo
echo "Состояние контейнеров:"
docker ps --filter "name=tekken_"

echo
echo "Watcher должен быть доступен на http://localhost:7080"
echo "PostgreSQL доступен на localhost:5552"
echo
read -p "Нажмите Enter для продолжения..."