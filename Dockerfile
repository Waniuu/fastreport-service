# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy wszystko
COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# ======================================================================
# 🔥 PANCERNA KONFIGURACJA GRAFICZNA 🔥
# Instalujemy fonts-dejavu-core - to standardowa czcionka, która ZAWSZE działa
# ======================================================================
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    fontconfig \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Symlink dla pewności
RUN ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

# Odświeżenie cache czcionek (żeby system widział DejaVu)
RUN fc-cache -f -v

WORKDIR /app
COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "FastReportService.dll"]
