# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy wszystkie pliki (w tym folder Fonts z OpenSans-Regular.ttf)
COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Instalujemy zależności graficzne ORAZ narzędzia do czcionek (fontconfig)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    fonts-liberation \
    fontconfig \
    && rm -rf /var/lib/apt/lists/*

# Tworzymy symlink (dla pewności)
RUN ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

WORKDIR /app
COPY --from=build /app ./

# ======================================================================
# 🔥 KLUCZOWA ZMIANA: Instalacja czcionki w systemie Linux 🔥
# Kopiujemy czcionkę z folderu aplikacji do folderu systemowego
# ======================================================================
RUN mkdir -p /usr/share/fonts/truetype/custom && \
    cp /app/Fonts/OpenSans-Regular.ttf /usr/share/fonts/truetype/custom/ && \
    fc-cache -f -v

EXPOSE 8080

ENTRYPOINT ["dotnet", "FastReportService.dll"]
