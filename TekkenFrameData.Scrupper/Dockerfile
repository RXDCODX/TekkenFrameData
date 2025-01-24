# Используем официальный образ Python 3.11
FROM python:latest

# Устанавливаем рабочую директорию
WORKDIR /app

# Обновляем pip и setuptools до последних версий
RUN pip install --no-cache-dir --upgrade pip setuptools

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