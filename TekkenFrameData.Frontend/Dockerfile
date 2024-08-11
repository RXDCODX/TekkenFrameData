# Этап сборки (builder)
FROM node:latest as builder

WORKDIR /app

# Копируем файлы зависимостей
COPY package.json yarn.lock ./

# Устанавливаем зависимости
RUN yarn install

# Копируем все файлы проекта
COPY . .

# Собираем приложение
RUN yarn build

# Этап production (nginx)
FROM nginx:stable-alpine

# Копируем конфигурацию nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Удаляем дефолтную страницу nginx
RUN rm -rf /usr/share/nginx/html/*

# Копируем собранные файлы из этапа builder
COPY --from=builder /app/dist /usr/share/nginx/html

# Открываем порт 80
EXPOSE 80

# Команда для запуска nginx
CMD ["nginx", "-g", "daemon off;"]