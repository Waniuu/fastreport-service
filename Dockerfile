# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FastReportService.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app .
EXPOSE 8080

ENTRYPOINT ["dotnet", "FastReportService.dll"]
