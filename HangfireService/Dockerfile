FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder

COPY . .
WORKDIR HangfireService
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
COPY --from=builder /app .
EXPOSE 80

ENTRYPOINT ["dotnet", "HangfireService.dll"]

