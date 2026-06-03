# ── Build ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VetClinica.API/*.csproj ./VetClinica.API/
RUN dotnet restore VetClinica.API/VetClinica.API.csproj

COPY VetClinica.API/ ./VetClinica.API/
RUN dotnet publish VetClinica.API/VetClinica.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instala fontes para o QuestPDF gerar PDFs corretamente no Linux
RUN apt-get update && apt-get install -y \
    fontconfig \
    libfreetype6 \
    libfontconfig1 \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Railway injeta a porta via variável PORT
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "VetClinica.API.dll"]
