# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy ALL project files
COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# ======================================================================
# 🔥 NAPRAWA: Instalacja pełnego pakietu graficznego dla FastReport 🔥
# libgdiplus - silnik graficzny
# libfontconfig1 - obsługa czcionek (KLUCZOWE!)
# libc6-dev - biblioteki systemowe
# fonts-liberation - zapasowe czcionki systemowe (żeby system nie był "głuchy")
# ======================================================================
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# Opcjonalne: Symlink (często wymagany, zostawiamy go)
RUN ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

WORKDIR /app
COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "FastReportService.dll"]
