# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TekkenFrameData.Backend/TekkenFrameData.Service/TekkenFrameData.Service.csproj", "TekkenFrameData.Service/"]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Cli", "TekkenFrameData.Cli/"]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Library", "TekkenFrameData.Library/"]
COPY ["Directory.Packages.Props", "."]

RUN dotnet restore "./TekkenFrameData.Service/TekkenFrameData.Service.csproj"
COPY ["TekkenFrameData.Backend/TekkenFrameData.Service/", "TekkenFrameData.Service/."]
WORKDIR "/src/TekkenFrameData.Service"
RUN dotnet build "./TekkenFrameData.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TekkenFrameData.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TekkenFrameData.Service.dll"]