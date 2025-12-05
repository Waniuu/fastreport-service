# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything from THIS directory (your repo root)
COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install libgdiplus for FastReport
RUN apt-get update && apt-get install -y libgdiplus && \
    ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

WORKDIR /app

COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "FastReportService.dll"]
