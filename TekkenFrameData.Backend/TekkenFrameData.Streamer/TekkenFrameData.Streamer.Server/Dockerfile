# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:latest AS base
USER root
RUN apt-get update 
RUN apt-get --yes install curl
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:latest AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Streamer/TekkenFrameData.Streamer.Server/TekkenFrameData.Streamer.Server.csproj", "TekkenFrameData.Backend/TekkenFrameData.Streamer/TekkenFrameData.Streamer.Server/"]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Library/TekkenFrameData.Library.csproj", "TekkenFrameData.Backend/TekkenFrameData.Library/"]
RUN dotnet restore "./TekkenFrameData.Backend/TekkenFrameData.Streamer/TekkenFrameData.Streamer.Server/TekkenFrameData.Streamer.Server.csproj"
COPY . .
WORKDIR "/src/TekkenFrameData.Backend/TekkenFrameData.Streamer/TekkenFrameData.Streamer.Server"
RUN dotnet build "./TekkenFrameData.Streamer.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TekkenFrameData.Streamer.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=5s --timeout=10s --retries=3 CMD curl --silent --fail http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "TekkenFrameData.Streamer.Server.dll"]