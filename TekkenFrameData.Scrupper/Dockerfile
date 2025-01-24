# Используем официальный образ Python 3.11
FROM python:3.14.0a4

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем файл с зависимостями
COPY requirements.txt .

# Устанавливаем зависимости
RUN pip install --no-cache-dir -r requirements.txt

# Копируем скрипт для проверки обновлений
COPY check_updates.py .

# Проверяем устаревшие пакеты
RUN python check_updates.py

# Копируем остальные файлы проекта
COPY . .

# Команда для запуска приложения (замените на свою, если нужно)
CMD ["python", "main.py"]
