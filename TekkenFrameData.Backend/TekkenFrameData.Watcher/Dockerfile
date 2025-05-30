﻿# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:latest AS base
USER root
RUN apt-get update 
RUN apt-get --yes install curl
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:latest AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TekkenFrameData.Backend/TekkenFrameData.Watcher/TekkenFrameData.Watcher.csproj", "TekkenFrameData.Watcher/"]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Library", "TekkenFrameData.Library/"]
COPY ["Directory.Packages.props", "."]

# Восстанавливаем нагет пакеты
RUN dotnet restore "TekkenFrameData.Watcher/TekkenFrameData.Watcher.csproj"

# Копируем сам проект
COPY ["TekkenFrameData.Backend/TekkenFrameData.Watcher", "TekkenFrameData.Watcher/"]
WORKDIR "/src/TekkenFrameData.Watcher"
RUN dotnet build "./TekkenFrameData.Watcher.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TekkenFrameData.Watcher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=5s --timeout=10s --retries=3 CMD curl --silent --fail http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "TekkenFrameData.Watcher.dll"]