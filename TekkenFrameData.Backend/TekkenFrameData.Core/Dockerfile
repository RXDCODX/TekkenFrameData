FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TekkenFrameData.Backend/TekkenFrameData.Library/TekkenFrameData.Library.csproj", "TekkenFrameData.Backend/TekkenFrameData.Library/"]
COPY ["TekkenFrameData.Backend/TekkenFrameData.Core/TekkenFrameData.Core.csproj", "TekkenFrameData.Backend/TekkenFrameData.Core/"]
RUN dotnet restore "TekkenFrameData.Backend/TekkenFrameData.Core/TekkenFrameData.Core.csproj"
COPY . .
WORKDIR "/src/TekkenFrameData.Backend/TekkenFrameData.Core"
RUN dotnet build "TekkenFrameData.Core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TekkenFrameData.Core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TekkenFrameData.Core.dll"] 