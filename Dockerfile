FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiuj pliki projektu
COPY ["FastReportService.csproj", "./"]
RUN dotnet restore "./FastReportService.csproj"

# Kopiuj cały kod źródłowy
COPY . ./

# Kopiuj folder Reports do katalogu build
COPY ["Reports/", "./Reports/"]

# Publikuj aplikację
RUN dotnet publish "FastReportService.csproj" -c Release -o /app

# Finalny obraz runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Instaluj zależności systemowe dla FastReport i grafiki
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    fontconfig \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Twórz link do libgdiplus
RUN ln -sf /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

# Aktualizuj cache czcionek
RUN fc-cache -f -v

WORKDIR /app

# Kopiuj publikowaną aplikację
COPY --from=build /app ./

# Kopiuj folder Reports do finalnego obrazu
COPY --from=build /src/Reports ./Reports

EXPOSE 80
ENTRYPOINT ["dotnet", "FastReportService.dll"]
