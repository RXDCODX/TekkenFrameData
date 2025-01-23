# Используем официальный образ Node.js на основе Alpine (легковесный)
FROM node:18-alpine

# Устанавливаем yarn глобально
RUN npm install -g yarn

# Устанавливаем рабочую директорию внутри контейнера
WORKDIR /app

# Копируем package.json и yarn.lock для установки зависимостей
COPY package.json yarn.lock ./

# Устанавливаем зависимости через yarn
RUN yarn install

# Копируем все файлы проекта в контейнер
COPY . .

# Собираем проект
RUN yarn build

# Открываем порт, на котором будет работать приложение
EXPOSE 3000

# Запускаем приложение через yarn dev
CMD ["yarn", "dev"]