ARG CI_REGISTRY_IMAGE

FROM $CI_REGISTRY_IMAGE:builder as deps

RUN mkdir -p /src/packages

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0-alpine AS builder
ENV NUGET_PACKAGES=/src/packages

COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

WORKDIR /src
COPY ["HangfireService/HangfireService.csproj", "HangfireService/"]
COPY ["HangfireService.Core/HangfireService.Core.csproj", "HangfireService.Core/"]
RUN dotnet restore -v n -r linux-x64 "./HangfireService/HangfireService.csproj"

COPY . .
WORKDIR "/src/HangfireService"
RUN dotnet build "./HangfireService.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app/publish /p:UseAppHost=false


FROM hub.lumigame.com/base/dotnet/core/aspnet:6.0-alpine AS base
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000

ENTRYPOINT ["dotnet", "HangfireService.dll"]

