#!/bin/bash

# Скрипт для выполнения внутри контейнера с использованием переменных окружения

# Проверяем переменные окружения
: "${DB_HOST:?Не задана переменная DB_HOST}"
: "${DB_PORT:?Не задана переменная DB_PORT}"
: "${DB_NAME:?Не задана переменная DB_NAME}"
: "${DB_USER:?Не задана переменная DB_USER}"
: "${DB_PASSWORD:?Не задана переменная DB_PASSWORD}"

WORKDIR="/app/publish"
CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

echo "Запуск миграций для БД: ${DB_USER}@${DB_HOST}:${DB_PORT}/${DB_NAME}"

dotnet ef database update \
    --connection "$CONNECTION_STRING" \
    --startup-project "${WORKDIR}/TekkenFrameData.Service.dll" \
    --project "${WORKDIR}/TekkenFrameData.Library.dll"

if [ $? -eq 0 ]; then
    echo "Миграции успешно применены!"
    exit 0
else
    echo "Ошибка применения миграций!" >&2
    exit 1
fi