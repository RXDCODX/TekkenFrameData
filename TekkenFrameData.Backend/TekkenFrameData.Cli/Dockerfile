# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проекта
COPY ["TekkenFrameData.Backend/TekkenFrameData.Cli/TekkenFrameData.Cli.csproj", "TekkenFrameData.Cli/"]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Library/TekkenFrameData.Library.csproj", "TekkenFrameData.Library/"]
COPY ["Directory.Packages.props", "."]

# Восстанавливаем зависимости
RUN dotnet restore "TekkenFrameData.Cli/TekkenFrameData.Cli.csproj"

# Копируем весь исходный код
COPY . .

# Собираем проект
WORKDIR "/src/TekkenFrameData.Cli"
RUN dotnet build "TekkenFrameData.Cli.csproj" -c Release -o /app/build

# Публикуем проект
FROM build AS publish
RUN dotnet publish "TekkenFrameData.Cli.csproj" -c Release -o /app/publish

# Используем образ .NET Runtime для запуска
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Устанавливаем инструменты EF Core
RUN dotnet tool install --global dotnet-ef --version 9.0.0

# Добавляем dotnet-ef в PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Команда для выполнения миграций
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "TekkenFrameData.Cli.csproj"]