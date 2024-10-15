FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 AS builder
WORKDIR /src
COPY ["HangfireService/HangfireService.csproj", "HangfireService/"]
COPY ["HangfireService.Core/HangfireService.Core.csproj", "HangfireService.Core/"]
RUN dotnet restore "./HangfireService/HangfireService.csproj"

COPY . .
WORKDIR "/src/HangfireService"
RUN dotnet build "./HangfireService.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false


FROM hub.lumigame.com/dotnet/core/aspnet:6.0-slim-24h1.1 AS base
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000

ENTRYPOINT ["dotnet", "HangfireService.dll"]

